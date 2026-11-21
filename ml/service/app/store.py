"""Index of published images and the moderation log of copy checks.

`PgStore` keeps everything in the service's own Postgres database with pgvector (HNSW index
on the SSCD vectors); `MemoryStore` is the same contract in memory, for tests and local runs.

Composite authorship (chapter 1, section 1.1.4): one row per media item, whatever number of
portfolios a team post appears in; searches exclude the author's own works and those of the
given teams.
"""

from __future__ import annotations

import json
import threading
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from typing import Protocol, Sequence

import numpy as np


@dataclass
class Fingerprint:
    media_id: int
    post_id: int
    owner_user_id: str
    team_id: str | None
    bucket: str
    object_name: str
    phash: int
    phash_mirror: int
    embedding: np.ndarray
    clip: np.ndarray | None = None
    model_version: str = ""


@dataclass
class Candidate(Fingerprint):
    similarity: float = 0.0


@dataclass
class CheckRecord:
    media_id: int | None
    post_id: int | None
    owner_user_id: str | None
    team_id: str | None
    bucket: str | None
    object_name: str | None
    zone: str
    match_type: str
    score: float
    matched_media_id: int | None
    matched_post_id: int | None
    result: dict
    id: int | None = None
    status: str = "open"  # open | confirmed_copy | dismissed
    decided_by: str | None = None
    decided_at: datetime | None = None
    created_at: datetime = field(default_factory=lambda: datetime.now(timezone.utc))

    def to_json(self) -> dict:
        d = asdict(self)
        for k in ("created_at", "decided_at"):
            d[k] = d[k].isoformat() if d[k] else None
        return d


class Store(Protocol):
    def upsert(self, fp: Fingerprint) -> None: ...
    def delete_post(self, post_id: int) -> int: ...
    def clear(self) -> int: ...
    def search(self, embedding: np.ndarray, k: int, exclude_users: Sequence[str] = (), exclude_teams: Sequence[str] = ()) -> list[Candidate]: ...
    def count(self) -> int: ...
    def add_check(self, rec: CheckRecord) -> int: ...
    def list_checks(self, zones: Sequence[str], status: str | None, limit: int, offset: int) -> list[CheckRecord]: ...
    def get_check(self, check_id: int) -> CheckRecord | None: ...
    def decide(self, check_id: int, decision: str, moderator: str) -> CheckRecord | None: ...


class MemoryStore:
    def __init__(self):
        self._fps: dict[int, Fingerprint] = {}
        self._checks: dict[int, CheckRecord] = {}
        self._lock = threading.Lock()

    def upsert(self, fp: Fingerprint) -> None:
        with self._lock:
            self._fps[fp.media_id] = fp

    def delete_post(self, post_id: int) -> int:
        with self._lock:
            ids = [m for m, fp in self._fps.items() if fp.post_id == post_id]
            for m in ids:
                del self._fps[m]
            return len(ids)

    def clear(self) -> int:
        with self._lock:
            n = len(self._fps)
            self._fps.clear()
            return n

    def search(self, embedding, k, exclude_users=(), exclude_teams=()) -> list[Candidate]:
        with self._lock:
            fps = [
                fp for fp in self._fps.values()
                if fp.owner_user_id not in set(exclude_users) and (fp.team_id is None or fp.team_id not in set(exclude_teams))
            ]
        if not fps:
            return []
        sims = np.stack([fp.embedding for fp in fps]) @ embedding
        order = np.argsort(-sims)[:k]
        return [Candidate(**{**fps[i].__dict__}, similarity=float(sims[i])) for i in order]

    def count(self) -> int:
        return len(self._fps)

    def add_check(self, rec: CheckRecord) -> int:
        with self._lock:
            for c in self._checks.values():
                if _same_pair(c, rec):
                    return c.id
            rec.id = len(self._checks) + 1
            self._checks[rec.id] = rec
            return rec.id

    def list_checks(self, zones, status, limit, offset) -> list[CheckRecord]:
        rows = [c for c in self._checks.values() if (not zones or c.zone in zones) and (status is None or c.status == status)]
        rows.sort(key=lambda c: (-c.score, -c.id))
        return rows[offset : offset + limit]

    def get_check(self, check_id: int) -> CheckRecord | None:
        return self._checks.get(check_id)

    def decide(self, check_id: int, decision: str, moderator: str) -> CheckRecord | None:
        rec = self._checks.get(check_id)
        if rec:
            rec.status, rec.decided_by, rec.decided_at = decision, moderator, datetime.now(timezone.utc)
        return rec


SCHEMA = """
CREATE EXTENSION IF NOT EXISTS vector;
CREATE TABLE IF NOT EXISTS image_fingerprint (
    media_id       integer PRIMARY KEY,
    post_id        integer NOT NULL,
    owner_user_id  text NOT NULL,
    team_id        text,
    bucket         text NOT NULL,
    object_name    text NOT NULL,
    phash          bigint NOT NULL,
    phash_mirror   bigint NOT NULL,
    embedding      vector(512) NOT NULL,
    clip_embedding vector(512),
    model_version  text NOT NULL,
    created_at     timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS image_fingerprint_post_idx ON image_fingerprint (post_id);
CREATE INDEX IF NOT EXISTS image_fingerprint_embedding_idx
    ON image_fingerprint USING hnsw (embedding vector_ip_ops);
CREATE TABLE IF NOT EXISTS copy_check (
    id               serial PRIMARY KEY,
    media_id         integer,
    post_id          integer,
    owner_user_id    text,
    team_id          text,
    bucket           text,
    object_name      text,
    zone             text NOT NULL,
    match_type       text NOT NULL,
    score            double precision NOT NULL,
    matched_media_id integer,
    matched_post_id  integer,
    result           jsonb NOT NULL,
    status           text NOT NULL DEFAULT 'open',
    decided_by       text,
    decided_at       timestamptz,
    created_at       timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS copy_check_queue_idx ON copy_check (status, zone, score DESC);
"""

_FP_COLS = "media_id, post_id, owner_user_id, team_id, bucket, object_name, phash, phash_mirror, embedding, clip_embedding, model_version"
_CHECK_COLS = ("id, media_id, post_id, owner_user_id, team_id, bucket, object_name, zone, match_type, score, "
               "matched_media_id, matched_post_id, result, status, decided_by, decided_at, created_at")


def ensure_database(conninfo: str) -> None:
    """Create the service's database when it does not exist yet. Aspire's AddDatabase only
    declares it, so on a fresh (or older) Postgres volume the first start has to create it."""
    import psycopg
    from psycopg import sql
    from psycopg.conninfo import conninfo_to_dict, make_conninfo

    params = conninfo_to_dict(conninfo)
    dbname = params.get("dbname") or "postgres"
    try:
        psycopg.connect(conninfo, connect_timeout=10).close()
        return
    except psycopg.OperationalError as exc:
        if "does not exist" not in str(exc):
            raise
    with psycopg.connect(make_conninfo(conninfo, dbname="postgres"), autocommit=True, connect_timeout=10) as conn:
        try:
            conn.execute(sql.SQL("CREATE DATABASE {}").format(sql.Identifier(dbname)))
        except psycopg.errors.DuplicateDatabase:
            pass  # another replica created it in the meantime


class PgStore:
    def __init__(self, conninfo: str, ef_search: int = 100):
        from pgvector.psycopg import register_vector
        from psycopg_pool import ConnectionPool

        ensure_database(conninfo)
        with __import__("psycopg").connect(conninfo, autocommit=True) as conn:
            conn.execute(SCHEMA)

        def configure(conn):
            register_vector(conn)
            conn.execute(f"SET hnsw.ef_search = {int(ef_search)}")
            conn.commit()

        self.pool = ConnectionPool(conninfo, min_size=1, max_size=8, configure=configure, open=True)

    def upsert(self, fp: Fingerprint) -> None:
        with self.pool.connection() as conn:
            conn.execute(
                f"INSERT INTO image_fingerprint ({_FP_COLS}) VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s) "
                "ON CONFLICT (media_id) DO UPDATE SET post_id=EXCLUDED.post_id, owner_user_id=EXCLUDED.owner_user_id, "
                "team_id=EXCLUDED.team_id, bucket=EXCLUDED.bucket, object_name=EXCLUDED.object_name, phash=EXCLUDED.phash, "
                "phash_mirror=EXCLUDED.phash_mirror, embedding=EXCLUDED.embedding, clip_embedding=EXCLUDED.clip_embedding, "
                "model_version=EXCLUDED.model_version, created_at=now()",
                (fp.media_id, fp.post_id, fp.owner_user_id, fp.team_id, fp.bucket, fp.object_name, fp.phash,
                 fp.phash_mirror, fp.embedding.astype(np.float32), None if fp.clip is None else fp.clip.astype(np.float32),
                 fp.model_version),
            )

    def delete_post(self, post_id: int) -> int:
        with self.pool.connection() as conn:
            return conn.execute("DELETE FROM image_fingerprint WHERE post_id = %s", (post_id,)).rowcount

    def clear(self) -> int:
        with self.pool.connection() as conn:
            return conn.execute("DELETE FROM image_fingerprint").rowcount

    def search(self, embedding, k, exclude_users=(), exclude_teams=()) -> list[Candidate]:
        # Vectors are L2-normalised, so the inner product is the cosine similarity.
        with self.pool.connection() as conn:
            rows = conn.execute(
                f"SELECT {_FP_COLS}, (embedding <#> %s) * -1 AS sim FROM image_fingerprint "
                "WHERE NOT (owner_user_id = ANY(%s)) AND (team_id IS NULL OR NOT (team_id = ANY(%s))) "
                "ORDER BY embedding <#> %s LIMIT %s",
                (embedding.astype(np.float32), list(exclude_users), list(exclude_teams), embedding.astype(np.float32), k),
            ).fetchall()
        return [
            Candidate(
                media_id=r[0], post_id=r[1], owner_user_id=r[2], team_id=r[3], bucket=r[4], object_name=r[5],
                phash=r[6], phash_mirror=r[7], embedding=_vec(r[8]), clip=None if r[9] is None else _vec(r[9]),
                model_version=r[10], similarity=float(r[11]),
            )
            for r in rows
        ]

    def count(self) -> int:
        with self.pool.connection() as conn:
            return conn.execute("SELECT count(*) FROM image_fingerprint").fetchone()[0]

    def add_check(self, rec: CheckRecord) -> int:
        with self.pool.connection() as conn:
            existing = conn.execute(
                "SELECT id FROM copy_check WHERE media_id IS NOT DISTINCT FROM %s "
                "AND matched_media_id IS NOT DISTINCT FROM %s ORDER BY id LIMIT 1",
                (rec.media_id, rec.matched_media_id),
            ).fetchone()
            if existing and rec.media_id is not None:
                return existing[0]
            return conn.execute(
                "INSERT INTO copy_check (media_id, post_id, owner_user_id, team_id, bucket, object_name, zone, match_type, "
                "score, matched_media_id, matched_post_id, result) VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s) RETURNING id",
                (rec.media_id, rec.post_id, rec.owner_user_id, rec.team_id, rec.bucket, rec.object_name, rec.zone,
                 rec.match_type, rec.score, rec.matched_media_id, rec.matched_post_id, json.dumps(rec.result)),
            ).fetchone()[0]

    def list_checks(self, zones, status, limit, offset) -> list[CheckRecord]:
        with self.pool.connection() as conn:
            rows = conn.execute(
                f"SELECT {_CHECK_COLS} FROM copy_check WHERE (cardinality(%s::text[]) = 0 OR zone = ANY(%s)) "
                "AND (%s::text IS NULL OR status = %s) ORDER BY score DESC, id DESC LIMIT %s OFFSET %s",
                (list(zones), list(zones), status, status, limit, offset),
            ).fetchall()
        return [_check(r) for r in rows]

    def get_check(self, check_id: int) -> CheckRecord | None:
        with self.pool.connection() as conn:
            r = conn.execute(f"SELECT {_CHECK_COLS} FROM copy_check WHERE id = %s", (check_id,)).fetchone()
        return _check(r) if r else None

    def decide(self, check_id: int, decision: str, moderator: str) -> CheckRecord | None:
        with self.pool.connection() as conn:
            r = conn.execute(
                f"UPDATE copy_check SET status = %s, decided_by = %s, decided_at = now() WHERE id = %s RETURNING {_CHECK_COLS}",
                (decision, moderator, check_id),
            ).fetchone()
        return _check(r) if r else None


def _vec(value) -> np.ndarray:
    """pgvector's psycopg adapter returns its own Vector type, not a numpy array."""
    if hasattr(value, "to_numpy"):
        value = value.to_numpy()
    return np.asarray(value, dtype=np.float32)


def _same_pair(existing: CheckRecord, new: CheckRecord) -> bool:
    """A re-check of the same upload against the same work (e.g. a repeated index rebuild)
    must not put a second card in the moderation queue, nor reopen one a moderator decided."""
    return (
        new.media_id is not None
        and existing.media_id == new.media_id
        and existing.matched_media_id == new.matched_media_id
    )


def _check(r) -> CheckRecord:
    return CheckRecord(
        id=r[0], media_id=r[1], post_id=r[2], owner_user_id=r[3], team_id=r[4], bucket=r[5], object_name=r[6],
        zone=r[7], match_type=r[8], score=r[9], matched_media_id=r[10], matched_post_id=r[11],
        result=r[12] if isinstance(r[12], dict) else json.loads(r[12]), status=r[13], decided_by=r[14],
        decided_at=r[15], created_at=r[16],
    )
