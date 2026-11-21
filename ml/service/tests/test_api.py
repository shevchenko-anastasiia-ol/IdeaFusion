"""End-to-end API behaviour with the real model and fake MinIO, on both index backends
(in memory, and Postgres + pgvector in Docker)."""

from __future__ import annotations

import pytest
from fastapi.testclient import TestClient
from PIL import ImageOps

from app.config import Settings
from app.images import DictSource
from app.main import Services, create_app
from tests.conftest import artwork, jpeg_bytes

OWNERS = ["alice", "bob", "carol", "dave", "erin", "frank"]


@pytest.fixture()
def client(detector, store):
    images = DictSource()
    services = Services(detector, store, images, Settings(database=""))
    with TestClient(create_app(services)) as c:
        for i, owner in enumerate(OWNERS):
            images.put("posts", f"media/{i}.jpg", jpeg_bytes(artwork(i)), "image/jpeg")
            team = "team-x" if i == 5 else None
            r = c.post("/v1/index", json={"media_id": 100 + i, "post_id": 10 + i, "owner_user_id": owner,
                                          "team_id": team, "bucket": "posts", "object_name": f"media/{i}.jpg"})
            assert r.status_code == 200, r.text
        c.images = images
        yield c


def _check(client, img, **form):
    return client.post("/v1/check", files={"file": ("q.jpg", jpeg_bytes(img, 60), "image/jpeg")}, data=form).json()


def test_health_and_model(client):
    assert client.get("/health").json()["index_size"] == len(OWNERS)
    assert client.get("/v1/model").json()["features"]


def test_modified_copy_is_found_with_explanation(client):
    stolen = ImageOps.mirror(artwork(2)).crop((20, 15, 236, 230))
    r = _check(client, stolen)
    assert r["best"]["media_id"] == 102
    assert r["zone"] in ("red", "yellow")
    assert r["best"]["signals"]["keypoint_inliers"] >= 15
    assert r["best"]["reference_region"] is not None
    assert {"descriptor", "search", "total"} <= set(r["timings_ms"])


def test_own_and_team_works_are_excluded(client):
    own = _check(client, artwork(2), exclude_user_ids="carol")
    assert own["best"] is None or own["best"]["media_id"] != 102
    team = _check(client, artwork(5), exclude_team_ids="team-x")
    assert team["best"] is None or team["best"]["media_id"] != 105


def test_unrelated_image_is_green(client):
    assert _check(client, artwork(7))["zone"] == "green"  # work 7 is not in the index


def test_check_by_minio_object(client):
    client.images.put("posts", "media/new.jpg", jpeg_bytes(artwork(3), 50), "image/jpeg")
    r = client.post("/v1/check", data={"bucket": "posts", "object_name": "media/new.jpg"}).json()
    assert r["best"]["media_id"] == 103


def test_delete_post_removes_it_from_the_index(client):
    assert client.delete("/v1/index/posts/12").json() == {"deleted": 1}
    r = _check(client, artwork(2))
    assert r["best"] is None or r["best"]["media_id"] != 102


def test_moderation_queue_and_explanation(client):
    client.images.put("posts", "media/q.jpg", jpeg_bytes(artwork(1).crop((10, 10, 240, 240)), 70), "image/jpeg")
    result = client.post("/v1/check", data={"bucket": "posts", "object_name": "media/q.jpg"}).json()
    check_id = client.post("/v1/checks", json={"media_id": 500, "post_id": 50, "owner_user_id": "mallory",
                                               "bucket": "posts", "object_name": "media/q.jpg", "result": result}).json()["id"]
    queue = client.get("/v1/checks", params={"zone": ["red", "yellow"]}).json()
    assert [c["id"] for c in queue] == [check_id]
    png = client.get(f"/v1/checks/{check_id}/explanation.png")
    assert png.status_code == 200 and png.content[:4] == b"\x89PNG"
    decided = client.post(f"/v1/checks/{check_id}/decision", json={"decision": "dismissed", "moderator": "mod"}).json()
    assert decided["status"] == "dismissed"
    assert client.get("/v1/checks").json() == []  # default filter: open only


def test_bad_requests(client):
    assert client.post("/v1/check").status_code == 400
    assert client.post("/v1/check", files={"file": ("x.jpg", b"not an image", "image/jpeg")}).status_code == 422
    assert client.post("/v1/index", json={"media_id": 1, "post_id": 1, "owner_user_id": "x",
                                          "bucket": "posts", "object_name": "missing.jpg"}).status_code == 404
