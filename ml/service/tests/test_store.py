"""The index/moderation-log contract, run on the in-memory store and on Postgres + pgvector."""

from __future__ import annotations

import numpy as np
import pytest

from app.store import CheckRecord, Fingerprint


def _unit(rng: np.random.Generator) -> np.ndarray:
    v = rng.normal(size=512).astype(np.float32)
    return v / np.linalg.norm(v)


def _fp(media_id: int, emb: np.ndarray, owner: str = "u", team: str | None = None, post_id: int | None = None) -> Fingerprint:
    return Fingerprint(media_id=media_id, post_id=post_id or media_id, owner_user_id=owner, team_id=team,
                       bucket="posts", object_name=f"m/{media_id}.jpg", phash=-(2**62) + media_id,
                       phash_mirror=2**62 - media_id, embedding=emb, model_version="test")


def _check(media_id: int, matched: int, zone: str = "red", score: float = 0.9) -> CheckRecord:
    return CheckRecord(media_id=media_id, post_id=media_id, owner_user_id="u", team_id=None, bucket="posts",
                       object_name="x", zone=zone, match_type="modified", score=score, matched_media_id=matched,
                       matched_post_id=matched, result={"zone": zone, "best": {"media_id": matched}})


def test_upsert_search_and_exclusions(store):
    rng = np.random.default_rng(0)
    target = _unit(rng)
    near = target + 0.1 * _unit(rng)
    near /= np.linalg.norm(near)
    store.upsert(_fp(1, near, owner="alice"))
    store.upsert(_fp(2, _unit(rng), owner="bob", team="t1"))
    store.upsert(_fp(3, _unit(rng), owner="carol"))
    store.upsert(_fp(1, near, owner="alice"))  # upsert of the same media item: still one row
    assert store.count() == 3

    hits = store.search(target, 2)
    assert [h.media_id for h in hits][0] == 1
    assert hits[0].similarity == pytest.approx(float(near @ target), abs=1e-4)
    assert hits[0].similarity >= hits[1].similarity
    assert hits[0].phash == -(2**62) + 1 and hits[0].phash_mirror == 2**62 - 1  # signed 64-bit round-trip
    assert np.allclose(hits[0].embedding, near, atol=1e-5)

    assert 1 not in [h.media_id for h in store.search(target, 3, exclude_users=["alice"])]
    assert 2 not in [h.media_id for h in store.search(target, 3, exclude_teams=["t1"])]
    # A team filter never drops personal works (team_id is NULL).
    assert {h.media_id for h in store.search(target, 3, exclude_teams=["t1"])} == {1, 3}


def test_delete_post(store):
    rng = np.random.default_rng(1)
    store.upsert(_fp(10, _unit(rng), post_id=7))
    store.upsert(_fp(11, _unit(rng), post_id=7))
    store.upsert(_fp(12, _unit(rng), post_id=8))
    assert store.delete_post(7) == 2
    assert store.count() == 1
    assert store.delete_post(7) == 0
    assert store.clear() == 1
    assert store.count() == 0


def test_moderation_log(store):
    a = store.add_check(_check(1, 50, "red", 0.95))
    b = store.add_check(_check(2, 51, "yellow", 0.3))
    store.add_check(_check(3, 52, "green", 0.01))
    # The same upload against the same work again (a repeated rebuild): no second card.
    assert store.add_check(_check(1, 50, "red", 0.97)) == a

    queue = store.list_checks(["red", "yellow"], "open", 10, 0)
    assert [c.id for c in queue] == [a, b]  # highest score first
    assert store.list_checks(["red"], "open", 10, 0)[0].result["best"]["media_id"] == 50

    decided = store.decide(a, "dismissed", "mod")
    assert decided.status == "dismissed" and decided.decided_by == "mod" and decided.decided_at is not None
    assert [c.id for c in store.list_checks(["red", "yellow"], "open", 10, 0)] == [b]
    assert {c.id for c in store.list_checks(["red", "yellow"], None, 10, 0)} == {a, b}
    # A decided pair stays decided: an index rebuild must not reopen it.
    assert store.add_check(_check(1, 50)) == a
    assert store.get_check(a).status == "dismissed"
    # The same upload matching a different work is a new case.
    assert store.add_check(_check(1, 77)) not in (a, b)
    assert store.get_check(999) is None
    assert store.decide(999, "dismissed", "mod") is None


def test_pg_store_creates_its_database(pg_conninfo):
    """Aspire declares the database but does not create it: the service must."""
    from app.store import PgStore

    conninfo = pg_conninfo.replace("dbname=postgres", "dbname=imagecopydb_fresh")
    store = PgStore(conninfo)
    try:
        assert store.count() == 0
        PgStore(conninfo).pool.close()  # a second start on the existing database is fine
    finally:
        store.pool.close()
