"""The service must compute the same pair features and P(copy) the model was evaluated on.

Takes a few test queries from research/runs/fusion_<dataset>/pairs.parquet, rebuilds their
candidate lists from the research embedding cache, scores them with the service detector and
compares with the research numbers. Skipped when the research data is not on this machine.
"""

from __future__ import annotations

from pathlib import Path

import numpy as np
import pandas as pd
import pytest

from app.store import Candidate
from copydet.fusion import add_derived
from copydet.imageio import load_rgb

RESEARCH = Path(__file__).resolve().parents[2] / "research"
DATASET = RESEARCH / "data" / "processed" / "artbench_v1"
PAIRS = RESEARCH / "runs" / "fusion_artbench_v1" / "pairs.parquet"
CACHE = RESEARCH / "data" / "embeddings" / "artbench_v1"


@pytest.fixture(scope="module")
def research():
    if not (PAIRS.exists() and (CACHE / "sscd.npz").exists()):
        pytest.skip("research data not available")
    pairs = pd.read_parquet(PAIRS)
    with np.load(CACHE / "sscd.npz") as z:
        sscd = dict(zip(z["keys"].tolist(), z["emb"]))
    with np.load(CACHE / "phash.npz") as z:
        phash = dict(zip(z["keys"].tolist(), z["emb"]))
    return pairs, sscd, phash


@pytest.mark.parametrize("aug_type", ["crop", "fragment", "collage", "none"])
def test_service_matches_research(detector, research, aug_type):
    pairs, sscd, phash = research
    test = pairs[(pairs["split"] == "test") & (pairs["aug_type"] == aug_type)]
    query_id = test["query_id"].iloc[0]
    rows = test[test["query_id"] == query_id].sort_values("rank")

    qa = detector.analyze(load_rgb(DATASET / rows["query_path"].iloc[0]))
    assert qa.sscd_bg == pytest.approx(rows["sscd_bg"].iloc[0], abs=2e-3)
    cands = []
    for r in rows.itertuples():
        key = "ds:" + r.ref_path
        cands.append(Candidate(
            media_id=r.rank, post_id=r.rank, owner_user_id="x", team_id=None, bucket="b", object_name=r.ref_path,
            phash=int(phash[key][0]), phash_mirror=int(phash[key][1]), embedding=sscd[key],
            similarity=float(sscd[key] @ qa.embedding),
        ))
    matches, _ = detector.score(qa, cands, lambda c: load_rgb(DATASET / c.object_name))
    by_rank = {m["media_id"]: m for m in matches}

    expected = detector.fusion.predict(add_derived(rows.reset_index(drop=True)))
    for (r, p_expected) in zip(rows.itertuples(), expected):
        m = by_rank[r.rank]
        assert m["signals"]["sscd_similarity"] == pytest.approx(r.sscd_raw, abs=2e-3)
        assert m["signals"]["phash_distance"] == r.phash_dist
        assert m["signals"]["keypoint_inliers"] == r.kp_inliers
        assert m["p_copy"] == pytest.approx(p_expected, abs=0.02)
