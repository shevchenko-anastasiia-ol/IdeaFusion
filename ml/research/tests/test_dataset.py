"""End-to-end dataset build on a synthetic catalog (no downloads needed)."""

from __future__ import annotations

from pathlib import Path

import numpy as np
import pandas as pd
import pytest
from PIL import Image, ImageDraw

from copydet import augment
from copydet.dataset import DatasetConfig, build, select


def _artwork(rng: np.random.Generator, size=(200, 160)) -> Image.Image:
    """Random shapes on a random background: distinct pHash per image, some keypoints."""
    img = Image.new("RGB", size, tuple(int(c) for c in rng.integers(0, 256, 3)))
    draw = ImageDraw.Draw(img)
    for _ in range(12):
        x0, y0 = rng.integers(0, size[0] - 20), rng.integers(0, size[1] - 20)
        x1, y1 = x0 + rng.integers(10, 80), y0 + rng.integers(10, 80)
        draw.ellipse((x0, y0, x1, y1), fill=tuple(int(c) for c in rng.integers(0, 256, 3)))
    return img


@pytest.fixture(scope="module")
def catalog(tmp_path_factory) -> pd.DataFrame:
    root = tmp_path_factory.mktemp("raw")
    rng = np.random.default_rng(0)
    rows = []
    for a in range(40):
        for i in range(6):
            path = root / f"artist{a}" / f"{a}_{i}.jpg"
            path.parent.mkdir(exist_ok=True)
            _artwork(rng).save(path)
            rows.append(
                {
                    "image_id": f"syn_{a:02d}_{i}",
                    "path": str(path),
                    "artist": f"artist-{a}",
                    "style": f"style-{a % 4}",
                    "source": "syn",
                    "license": "test",
                }
            )
    return pd.DataFrame(rows)


def _cfg(tmp_path: Path, **kw) -> DatasetConfig:
    base = dict(
        name="t",
        catalog="",
        output_dir=str(tmp_path / "ds"),
        n_originals=40,
        max_originals_per_artist=3,
        hard_negatives_per_artist=2,
        same_style_negatives=5,
        n_distractors=10,
        train_queries_per_original=3,
        workers=1,
    )
    base.update(kw)
    return DatasetConfig(**base)


def test_selection_splits_by_artist(catalog, tmp_path):
    sel = select(catalog, _cfg(tmp_path))
    assert (sel.originals.groupby("artist")["split"].nunique() == 1).all()
    assert len(sel.originals) == 40
    used = set(sel.originals["artist"])
    assert not used & set(sel.distractors["artist"])
    same_style = sel.negatives[sel.negatives["negative_kind"] == "same_style"]
    assert len(same_style) and not used & set(same_style["artist"])


def test_build_end_to_end(catalog, tmp_path):
    cfg = _cfg(tmp_path, workers=2)  # exercises the process pool on Windows too
    summary = build(cfg, catalog)
    out = Path(cfg.output_dir)
    originals = pd.read_parquet(out / "originals.parquet")
    queries = pd.read_parquet(out / "queries.parquet")

    assert summary["failed_queries"] == 0
    assert all((out / p).exists() for p in queries["path"])
    assert all((out / p).exists() for p in originals["path"])

    pos = queries[~queries["is_negative"]]
    assert set(pos.loc[pos["split"] == "test", "aug_type"]) == set(augment.ALL_TYPES)
    assert not set(pos.loc[pos["split"] != "test", "aug_type"]) & augment.HELD_OUT_TYPES
    assert pos["original_id"].isin(originals["image_id"]).all()
    assert queries["query_id"].is_unique


def test_build_is_deterministic(catalog, tmp_path):
    a = build(_cfg(tmp_path / "a"), catalog)
    b = build(_cfg(tmp_path / "b"), catalog)
    assert a == b
    qa = pd.read_parquet(tmp_path / "a" / "ds" / "queries.parquet").sort_values("query_id")
    qb = pd.read_parquet(tmp_path / "b" / "ds" / "queries.parquet").sort_values("query_id")
    assert qa["params"].tolist() == qb["params"].tolist()
