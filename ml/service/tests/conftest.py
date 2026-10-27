from __future__ import annotations

import io
import shutil
import subprocess
import time
from pathlib import Path

import pytest
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
MODEL_DIR = ROOT.parent / "models" / "v1"


DATA = Path(__file__).resolve().parent / "data"


def artwork(i: int) -> Image.Image:
    """Real public-domain artworks by different artists (see data/SOURCES.json). Synthetic
    shapes are a poor test: SSCD finds all flat-colour drawings alike."""
    return Image.open(DATA / f"work{i}.jpg").convert("RGB")


def jpeg_bytes(img: Image.Image, quality: int = 90) -> bytes:
    buf = io.BytesIO()
    img.save(buf, format="JPEG", quality=quality)
    return buf.getvalue()


PG_IMAGE = "pgvector/pgvector:pg17-trixie"


@pytest.fixture(scope="session")
def pg_conninfo():
    """A throwaway Postgres with pgvector in Docker, shared by the session."""
    if shutil.which("docker") is None or subprocess.run(["docker", "info"], capture_output=True).returncode != 0:
        pytest.skip("Docker is not available")
    run = subprocess.run(
        ["docker", "run", "-d", "--rm", "-e", "POSTGRES_PASSWORD=test", "-p", "127.0.0.1::5432", PG_IMAGE],
        capture_output=True, text=True,
    )
    if run.returncode != 0:
        pytest.skip(f"cannot start {PG_IMAGE}: {run.stderr.strip()}")
    container = run.stdout.strip()
    try:
        port = subprocess.run(["docker", "port", container, "5432"], capture_output=True, text=True,
                              check=True).stdout.split(":")[-1].strip()
        conninfo = f"host=127.0.0.1 port={port} user=postgres password=test dbname=postgres"
        import psycopg

        deadline = time.time() + 60
        while True:
            try:
                psycopg.connect(conninfo, connect_timeout=2).close()
                break
            except psycopg.OperationalError:
                if time.time() > deadline:
                    raise
                time.sleep(0.5)
        yield conninfo
    finally:
        subprocess.run(["docker", "rm", "-f", container], capture_output=True)


@pytest.fixture(scope="session")
def pg_store(pg_conninfo):
    from app.store import PgStore

    s = PgStore(pg_conninfo)
    yield s
    s.pool.close()


@pytest.fixture(params=["memory", "postgres"])
def store(request):
    """Every store test and API test runs against both backends of the same contract."""
    from app.store import MemoryStore

    if request.param == "memory":
        return MemoryStore()
    s = request.getfixturevalue("pg_store")
    with s.pool.connection() as conn:
        conn.execute("TRUNCATE image_fingerprint; TRUNCATE copy_check RESTART IDENTITY")
    return s


@pytest.fixture(scope="session")
def detector():
    if not (MODEL_DIR / "manifest.json").exists():
        pytest.skip("model artefacts missing: run research/scripts/10_export_model.py")
    from app.detector import CopyDetector

    return CopyDetector(MODEL_DIR, torch_threads=4)
