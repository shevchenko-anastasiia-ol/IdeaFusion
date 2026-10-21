import numpy as np
import pandas as pd
import pytest

from copydet.fusion import (
    FusionModel,
    add_derived,
    as_retrieval,
    expected_calibration_error,
    match_type,
    query_table,
    risk_zone,
)


def _pairs(rng, n_q=300, k=3):
    rows = []
    for q in range(n_q):
        negative = q % 5 == 0
        for r in range(k):
            label = (not negative) and r == 0
            rows.append(
                {
                    "query_id": f"q{q}",
                    "aug_type": "none" if negative else "jpeg",
                    "is_negative": negative,
                    "negative_kind": "same_artist" if negative else None,
                    "rank": r,
                    "label": label,
                    "sscd_norm": rng.normal(0.6 if label else 0.0, 0.15),
                    "kp_inliers": int(rng.integers(50, 300)) if label else int(rng.integers(0, 20)),
                }
            )
    return add_derived(pd.DataFrame(rows))


def test_fusion_learns_and_calibrates():
    rng = np.random.default_rng(0)
    train, val = _pairs(rng), _pairs(rng)
    m = FusionModel(["sscd_norm", "kp_inliers_log"]).fit(train).calibrate(val)
    p = m.predict(val)
    assert p[val["label"].to_numpy()].mean() > 0.9 > 0.1 > p[~val["label"].to_numpy()].mean()
    ece, _ = expected_calibration_error(p, val["label"].to_numpy())
    assert ece < 0.05


def test_as_retrieval_reranks_by_probability():
    pairs = pd.DataFrame(
        {
            "query_id": ["a", "a", "b", "b"],
            "aug_type": ["crop", "crop", "none", "none"],
            "is_negative": [False, False, True, True],
            "negative_kind": [None, None, "same_artist", "same_artist"],
            "label": [False, True, False, False],
        }
    )
    retr, gt, queries = as_retrieval(pairs, np.array([0.2, 0.9, 0.4, 0.1]))
    assert gt.tolist() == [0, -1]
    assert retr.ref_idx[0, 0] == 0 and retr.scores[0, 0] == pytest.approx(0.9)
    assert retr.scores[1, 0] == pytest.approx(0.4)
    best = query_table(pairs, np.array([0.2, 0.9, 0.4, 0.1]))
    assert best.set_index("query_id").loc["a", "label"]


def test_match_type_and_zones():
    t = {"red": 0.9, "yellow": 0.5}
    base = {"p_copy": 0.95, "phash_dist": 30, "kp_inliers": 80, "kp_scale": 1.0, "clip_norm": 0.0}
    assert match_type(pd.Series({**base, "phash_dist": 3}), t, 0.2) == "exact"
    assert match_type(pd.Series({**base, "kp_scale": 2.4}), t, 0.2) == "partial"
    assert match_type(pd.Series({**base, "kp_scale": 0.4}), t, 0.2) == "fragment"
    assert match_type(pd.Series(base), t, 0.2) == "modified"
    assert match_type(pd.Series({**base, "p_copy": 0.1, "clip_norm": 0.3}), t, 0.2) == "similar_composition"
    assert [risk_zone(p, t) for p in (0.95, 0.6, 0.1)] == ["red", "yellow", "green"]
