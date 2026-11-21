"""HTTP API of the copy detection service.

    POST   /v1/check                   check an image (upload or MinIO object) against the index
    POST   /v1/index                   add a published image to the index
    DELETE /v1/index/posts/{post_id}   remove a deleted/archived post from the index
    DELETE /v1/index                   empty the index (before a full rebuild)
    POST   /v1/checks                  record a check result for the moderation queue
    GET    /v1/checks                  moderation queue (filter by zone/status)
    GET    /v1/checks/{id}             one check
    POST   /v1/checks/{id}/decision    moderator's decision
    GET    /v1/checks/{id}/explanation.png   side-by-side picture of the match
    GET    /health, GET /v1/model

The service never blocks or deletes anything itself: it reports a calibrated P(copy), a match
type and a risk zone; the platform shows them to the author and the moderator (requirement B5).
"""

from __future__ import annotations

import time
from contextlib import asynccontextmanager
from typing import Literal

from fastapi import Depends, FastAPI, File, Form, HTTPException, Query, Request, Response, UploadFile
from fastapi.concurrency import run_in_threadpool
from pydantic import BaseModel, Field

from app.config import Settings, to_libpq
from app.detector import CopyDetector, render_explanation
from app.images import ImageSource, MinioSource, open_image
from app.store import CheckRecord, MemoryStore, PgStore, Store


class Services:
    def __init__(self, detector: CopyDetector, store: Store, images: ImageSource, settings: Settings):
        self.detector, self.store, self.images, self.settings = detector, store, images, settings


def create_app(services: Services | None = None) -> FastAPI:
    @asynccontextmanager
    async def lifespan(app: FastAPI):
        if services is None:
            s = Settings()
            store = PgStore(to_libpq(s.database)) if s.database else MemoryStore()
            images = MinioSource(s.minio_endpoint, s.minio_access_key, s.minio_secret_key, s.minio_secure)
            app.state.services = Services(CopyDetector(s.model_dir, s.torch_threads), store, images, s)
        else:
            app.state.services = services
        yield

    app = FastAPI(title="IdeaFusion copy detection", version="1.0.0", lifespan=lifespan)
    register_routes(app)
    return app


def get_services(request: Request) -> Services:
    return request.app.state.services


# -- schemas -------------------------------------------------------------------------


class IndexRequest(BaseModel):
    media_id: int
    post_id: int
    owner_user_id: str = Field(description="Keycloak subject of the post author")
    team_id: str | None = Field(default=None, description="team that owns the post, if a team post")
    bucket: str
    object_name: str


class RecordCheckRequest(BaseModel):
    media_id: int | None = None
    post_id: int | None = None
    owner_user_id: str | None = None
    team_id: str | None = None
    bucket: str | None = None
    object_name: str | None = None
    result: dict = Field(description="the /v1/check response, after the platform's own filtering")


class Decision(BaseModel):
    decision: Literal["confirmed_copy", "dismissed", "open"]
    moderator: str


def _split(values: str | None) -> list[str]:
    return [v.strip() for v in (values or "").split(",") if v.strip()]


def _check(svc: Services, data: bytes, exclude_users: list[str], exclude_teams: list[str]) -> dict:
    t0 = time.perf_counter()
    try:
        image = open_image(data)
    except Exception as exc:
        raise HTTPException(422, f"not a readable image: {exc}") from exc
    qa = svc.detector.analyze(image)
    t1 = time.perf_counter()
    candidates = svc.store.search(qa.embedding, svc.detector.k, exclude_users, exclude_teams)
    t2 = time.perf_counter()

    def load(c):
        return open_image(svc.images.get(c.bucket, c.object_name))

    matches, timings = svc.detector.score(qa, candidates, load)
    best = matches[0] if matches else None
    return {
        "zone": best["zone"] if best else "green",
        "score": best["p_copy"] if best else 0.0,
        "match_type": best["match_type"] if best else "none",
        "best": best,
        "matches": matches,
        "model_version": svc.detector.version,
        "index_size": svc.store.count(),
        "timings_ms": {
            "descriptor": round((t1 - t0) * 1000, 1),
            "search": round((t2 - t1) * 1000, 1),
            **{k: round(v, 1) for k, v in timings.items()},
            "total": round((time.perf_counter() - t0) * 1000, 1),
        },
        "_fingerprint": qa,
    }


def _public(result: dict) -> dict:
    return {k: v for k, v in result.items() if not k.startswith("_")}


def register_routes(app: FastAPI) -> None:
    @app.get("/health")
    def health(svc: Services = Depends(get_services)):
        return {"status": "ok", "model_version": svc.detector.version, "index_size": svc.store.count()}

    @app.get("/v1/model")
    def model(svc: Services = Depends(get_services)):
        return {"manifest": svc.detector.manifest, "config": svc.detector.config, "features": svc.detector.fusion.features}

    @app.post("/v1/check")
    async def check(
        file: UploadFile | None = File(default=None),
        bucket: str | None = Form(default=None),
        object_name: str | None = Form(default=None),
        exclude_user_ids: str | None = Form(default=None, description="comma-separated owner ids to skip"),
        exclude_team_ids: str | None = Form(default=None, description="comma-separated team ids to skip"),
        svc: Services = Depends(get_services),
    ):
        if file is not None:
            data = await file.read()
            if len(data) > svc.settings.max_upload_bytes:
                raise HTTPException(413, "image too large")
        elif bucket and object_name:
            try:
                data = await run_in_threadpool(svc.images.get, bucket, object_name)
            except Exception as exc:
                raise HTTPException(404, f"cannot read {bucket}/{object_name}: {exc}") from exc
        else:
            raise HTTPException(400, "send a file or bucket + object_name")
        result = await run_in_threadpool(_check, svc, data, _split(exclude_user_ids), _split(exclude_team_ids))
        return _public(result)

    @app.post("/v1/index")
    async def index(req: IndexRequest, svc: Services = Depends(get_services)):
        try:
            data = await run_in_threadpool(svc.images.get, req.bucket, req.object_name)
        except Exception as exc:
            raise HTTPException(404, f"cannot read {req.bucket}/{req.object_name}: {exc}") from exc

        def work():
            qa = svc.detector.analyze(open_image(data))
            fp = svc.detector.fingerprint(qa, model_version=svc.detector.version, **req.model_dump())
            svc.store.upsert(fp)

        try:
            await run_in_threadpool(work)
        except HTTPException:
            raise
        except Exception as exc:
            raise HTTPException(422, f"cannot index image: {exc}") from exc
        return {"indexed": req.media_id, "index_size": svc.store.count()}

    @app.delete("/v1/index")
    def clear_index(svc: Services = Depends(get_services)):
        """Empty the index before a full rebuild, so that every work is compared only with
        the works published before it (the copy is flagged, not the original)."""
        return {"deleted": svc.store.clear()}

    @app.delete("/v1/index/posts/{post_id}")
    def delete_post(post_id: int, svc: Services = Depends(get_services)):
        return {"deleted": svc.store.delete_post(post_id)}

    @app.post("/v1/checks")
    def record_check(req: RecordCheckRequest, svc: Services = Depends(get_services)):
        best = req.result.get("best") or {}
        rec = CheckRecord(
            media_id=req.media_id, post_id=req.post_id, owner_user_id=req.owner_user_id, team_id=req.team_id,
            bucket=req.bucket, object_name=req.object_name,
            zone=req.result.get("zone", "green"), match_type=req.result.get("match_type", "none"),
            score=float(req.result.get("score", 0.0)),
            matched_media_id=best.get("media_id"), matched_post_id=best.get("post_id"), result=req.result,
        )
        return {"id": svc.store.add_check(rec)}

    @app.get("/v1/checks")
    def list_checks(
        zone: list[str] = Query(default=["red", "yellow"]),
        status: str | None = Query(default="open"),
        limit: int = Query(default=50, le=500),
        offset: int = 0,
        svc: Services = Depends(get_services),
    ):
        return [c.to_json() for c in svc.store.list_checks(zone, status or None, limit, offset)]

    @app.get("/v1/checks/{check_id}")
    def get_check(check_id: int, svc: Services = Depends(get_services)):
        rec = svc.store.get_check(check_id)
        if rec is None:
            raise HTTPException(404, "check not found")
        return rec.to_json()

    @app.post("/v1/checks/{check_id}/decision")
    def decide(check_id: int, body: Decision, svc: Services = Depends(get_services)):
        rec = svc.store.decide(check_id, body.decision, body.moderator)
        if rec is None:
            raise HTTPException(404, "check not found")
        return rec.to_json()

    @app.get("/v1/checks/{check_id}/explanation.png")
    async def explanation(check_id: int, svc: Services = Depends(get_services)):
        rec = svc.store.get_check(check_id)
        best = rec.result.get("best") if rec else None
        if not rec or not best or not rec.bucket:
            raise HTTPException(404, "no match to explain")

        def work() -> bytes:
            query = open_image(svc.images.get(rec.bucket, rec.object_name))
            ref = open_image(svc.images.get(best["bucket"], best["object_name"]))
            return render_explanation(query, ref, best)

        try:
            png = await run_in_threadpool(work)
        except Exception as exc:
            raise HTTPException(404, f"images not available: {exc}") from exc
        return Response(png, media_type="image/png")


app = create_app()
