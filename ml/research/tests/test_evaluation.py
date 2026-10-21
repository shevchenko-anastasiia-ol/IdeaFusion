import math

import numpy as np
import pandas as pd
import pytest

from copydet.evaluation import (
    Retrieval,
    _threshold_at_precision,
    evaluate,
    micro_ap,
    normalize_with_background,
    search_cosine,
    search_phash,
)


def _queries(aug_types, negatives=0):
    rows = [{"aug_type": t, "is_negative": False, "negative_kind": None} for t in aug_types]
    rows += [{"aug_type": "none", "is_negative": True, "negative_kind": "same_artist"}] * negatives
    return pd.DataFrame(rows)


def test_micro_ap_perfect_and_partial():
    gt = np.array([0, 1, -1])
    perfect = Retrieval(np.array([[0, 1], [1, 0], [2, 0]]), np.array([[0.9, 0.1], [0.8, 0.2], [0.3, 0.2]]))
    assert micro_ap(perfect, gt) == pytest.approx(1.0)
    # A negative query scoring above the second positive halves precision at that hit.
    worse = Retrieval(perfect.ref_idx, np.array([[0.9, 0.1], [0.5, 0.2], [0.7, 0.2]]))
    assert micro_ap(worse, gt) == pytest.approx((1 + 2 / 3) / 2)


def test_threshold_at_precision_handles_ties():
    score = np.array([3.0, 3.0, 2.0, 1.0])
    correct = np.array([True, False, True, True])
    # Every threshold includes the tied wrong match: precision never reaches 0.99.
    assert _threshold_at_precision(score, correct, 0.99) == math.inf
    assert _threshold_at_precision(score, correct, 0.5) == 1.0


def test_evaluate_operating_points_and_negatives():
    gt = np.array([0, 1, 2, -1, -1])
    retr = Retrieval(
        np.array([[0], [1], [0], [0], [1]]),
        np.array([[0.9], [0.8], [0.7], [0.75], [0.1]]),
    )
    q = _queries(["jpeg", "jpeg", "crop"], negatives=2)
    r = evaluate(retr, gt, q)
    p99 = r["operating_points"]["p99"]
    assert p99["threshold"] == pytest.approx(0.8)
    assert p99["recall"] == pytest.approx(2 / 3)
    assert p99["recall_by_type"] == {"crop": 0.0, "jpeg": 1.0}
    assert p99["hard_negative_fpr"]["same_artist"] == 0.0
    assert r["top1_by_type"] == {"crop": 0.0, "jpeg": 1.0}
    # 80% recall is unreachable (top-1 finds 2 of 3): no operating point instead of "everything fires".
    assert r["operating_points"]["r80"]["threshold"] is None
    assert math.isnan(r["operating_points"]["r80"]["recall"])
    # Transferred thresholds are applied as given.
    r2 = evaluate(retr, gt, q, transfer_thresholds={"p99": 0.5})
    assert r2["operating_points"]["p99_from_val"]["hard_negative_fpr"]["same_artist"] == 0.5


def test_search_and_normalisation():
    refs = np.eye(3, dtype=np.float32)
    queries = np.array([[0.9, 0.1, 0], [0, 0, 1]], dtype=np.float32)
    retr = search_cosine(queries, refs, k=2)
    assert retr.ref_idx[:, 0].tolist() == [0, 2]
    bg = np.array([[1, 0, 0]], dtype=np.float32)
    norm = normalize_with_background(retr, queries, bg)
    assert norm.scores[0, 0] == pytest.approx(0.0)  # query 0 is as close to background as to its match
    assert norm.scores[1, 0] == pytest.approx(1.0)


def test_search_phash_uses_mirror():
    refs = np.array([[0b1111, 0b0000], [0b1010, 0b1010]], dtype=np.int64)
    queries = np.array([[0b0000, 0b1111]], dtype=np.int64)  # equals the mirror of ref 0
    retr = search_phash(queries, refs, k=1)
    assert retr.ref_idx[0, 0] == 0 and retr.scores[0, 0] == 0
