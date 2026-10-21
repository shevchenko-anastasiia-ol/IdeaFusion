from __future__ import annotations

import io
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


@pytest.fixture(scope="session")
def detector():
    if not (MODEL_DIR / "manifest.json").exists():
        pytest.skip("model artefacts missing: run research/scripts/10_export_model.py")
    from app.detector import CopyDetector

    return CopyDetector(MODEL_DIR, torch_threads=4)
