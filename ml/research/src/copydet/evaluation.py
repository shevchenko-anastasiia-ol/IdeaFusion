"""Evaluation protocol for copy detection (chapter 1, section 1.4.2).

Setup: every query is searched against an index of references (originals of the split plus
distractors). A positive query has exactly one correct reference (its original); a negative
query (hard negative) has none, so any match above the decision threshold is a false accusation.

Metrics
* micro AP (muAP): DISC21's official metric. All (query, reference, score) predictions of all
  queries are ranked together; it rewards scores that are comparable *across* queries.
* Top-1 accuracy per modification type: pure retrieval quality, no threshold.
* Operating points on the top-1 prediction of each query:
    - `p99`: the lowest threshold with precision >= 0.99 (a wrong top-1 match or any negative
      above the threshold is a false positive); reports recall overall and per modification type,
      and the false-match rate on hard negatives per kind;
    - `r80`: the threshold where 80% of positives are found; how many hard negatives fire there;
    - `fpr1`: the threshold where at most 1% of hard negatives fire; the recall there is the
      main number for the platform, since accusing an honest author is worse than a miss.
      (`p99` depends on the positive/negative ratio: with 10x more positives it tolerates
      ~10% of negatives firing, so it understates false accusations.)
  For the test split the threshold is also taken from val and transferred, which is what a
  deployed system does (pitfall "thresholds tuned on the test images", section 1.4.3).
* ROC-AUC of top-1 scores: correctly matched copies vs hard negatives.
"""

from __future__ import annotations

import math
from dataclasses import dataclass

import numpy as np
import pandas as pd
from sklearn.metrics import roc_auc_score

from copydet.hashing import hamming_matrix


@dataclass
class Retrieval:
    ref_idx: np.ndarray  # (n_queries, k) indices into the reference list
    scores: np.ndarray  # (n_queries, k) higher = more similar, sorted descending per row


def _topk(sim: np.ndarray, k: int) -> Retrieval:
    k = min(k, sim.shape[1])
    idx = np.argpartition(-sim, k - 1, axis=1)[:, :k]
    part = np.take_along_axis(sim, idx, axis=1)
    order = np.argsort(-part, axis=1, kind="stable")
    return Retrieval(np.take_along_axis(idx, order, axis=1), np.take_along_axis(part, order, axis=1))


def search_cosine(queries: np.ndarray, refs: np.ndarray, k: int = 10, block: int = 4096) -> Retrieval:
    parts = [_topk(queries[i : i + block] @ refs.T, k) for i in range(0, len(queries), block)]
    return Retrieval(np.concatenate([p.ref_idx for p in parts]), np.concatenate([p.scores for p in parts]))


def search_phash(queries: np.ndarray, refs: np.ndarray, k: int = 10) -> Retrieval:
    """`queries`/`refs` are (N, 2) int64: [hash, hash_of_mirror]. Score = -Hamming distance,
    the smaller of query-vs-reference and query-vs-mirrored-reference."""
    dist = np.minimum(hamming_matrix(queries[:, 0], refs[:, 0]), hamming_matrix(queries[:, 0], refs[:, 1]))
    return _topk(-dist.astype(np.float32), k)


def normalize_with_background(retr: Retrieval, queries: np.ndarray, background: np.ndarray, k_bg: int = 1, beta: float = 1.0) -> Retrieval:
    """SSCD score normalisation: subtract the similarity to the query's k-th nearest
    background image. The ranking per query is unchanged; scores become comparable across
    queries, so images that are close to everything stop producing confident matches."""
    sim_bg = queries @ background.T
    kth = -np.partition(-sim_bg, k_bg - 1, axis=1)[:, k_bg - 1]
    return Retrieval(retr.ref_idx, retr.scores - beta * kth[:, None])


def micro_ap(retr: Retrieval, gt: np.ndarray) -> float:
    """`gt[i]` is the reference index of query i's original, or -1 for negative queries."""
    n_gt = int((gt >= 0).sum())
    correct = (retr.ref_idx == gt[:, None]).ravel()
    order = np.argsort(-retr.scores.ravel(), kind="stable")
    hits = correct[order]
    precision = np.cumsum(hits) / np.arange(1, len(hits) + 1)
    return float((precision * hits).sum() / max(n_gt, 1))


def _threshold_at_precision(score: np.ndarray, correct: np.ndarray, target: float) -> float:
    """Lowest threshold t such that precision of {score >= t} is >= target (ties included)."""
    order = np.argsort(-score, kind="stable")
    s_sorted, tp_cum = score[order], np.cumsum(correct[order])
    thresholds = np.unique(s_sorted)[::-1]
    n_at = np.searchsorted(-s_sorted, -thresholds, side="right")
    precision = tp_cum[n_at - 1] / n_at
    ok = np.nonzero(precision >= target)[0]
    # The point with the highest recall that still meets the precision target.
    return float(thresholds[ok.max()]) if len(ok) else math.inf


def _threshold_at_fpr(score: np.ndarray, is_negative: np.ndarray, target: float) -> float:
    """Lowest threshold at which at most `target` of the hard negatives fire."""
    neg = np.sort(score[is_negative])[::-1]
    allowed = math.floor(target * len(neg))
    if allowed >= len(neg):  # also covers "no negatives"
        return -math.inf
    # Just above the first negative that must not fire.
    return float(np.nextafter(neg[allowed], np.inf))


def _threshold_at_recall(score: np.ndarray, correct: np.ndarray, is_pos: np.ndarray, target: float) -> float:
    hits = np.sort(score[correct & is_pos])[::-1]
    need = math.ceil(target * int(is_pos.sum()))
    return float(hits[need - 1]) if 0 < need <= len(hits) else -math.inf


def _mean_by(values: np.ndarray, queries: pd.DataFrame, mask: np.ndarray, column: str) -> dict[str, float]:
    return queries.loc[mask, column].to_frame().assign(v=values[mask]).groupby(column)["v"].mean().astype(float).to_dict()


def _report_at(threshold: float, score, correct, queries: pd.DataFrame) -> dict:
    if threshold == -math.inf:
        # The target recall is unreachable: there is no operating point to report.
        nan = float("nan")
        return {"threshold": None, "precision": nan, "recall": nan, "wrong_match_rate": nan,
                "recall_by_type": {}, "hard_negative_fpr": {}}
    fire = score >= threshold
    neg = queries["is_negative"].to_numpy()
    pos = ~neg
    tp = fire & correct
    fpr = _mean_by(fire.astype(float), queries, neg, "negative_kind")
    if neg.any():
        fpr["all"] = float(fire[neg].mean())
    return {
        "threshold": threshold,
        "precision": float(tp.sum() / max(fire.sum(), 1)),
        "recall": float(tp[pos].mean()) if pos.any() else float("nan"),
        "wrong_match_rate": float((fire & ~correct)[pos].mean()) if pos.any() else float("nan"),
        "recall_by_type": _mean_by(tp.astype(float), queries, pos, "aug_type"),
        "hard_negative_fpr": fpr,
    }


def evaluate(retr: Retrieval, gt: np.ndarray, queries: pd.DataFrame, transfer_thresholds: dict | None = None) -> dict:
    """Full metric set for one method on one split. `queries` rows align with `retr`/`gt` and
    carry `is_negative`, `negative_kind`, `aug_type`."""
    queries = queries.reset_index(drop=True)
    is_pos = gt >= 0
    score = retr.scores[:, 0]
    correct = retr.ref_idx[:, 0] == gt  # always False for negatives (gt = -1)

    top1_by_type = _mean_by(correct.astype(float), queries, is_pos, "aug_type")
    neg = queries["is_negative"].to_numpy()
    auc_mask = (is_pos & correct) | neg
    auc = (
        float(roc_auc_score(correct[auc_mask], score[auc_mask]))
        if auc_mask.any() and neg.any() and (is_pos & correct).any()
        else float("nan")
    )
    thresholds = {
        "p99": _threshold_at_precision(score, correct, 0.99),
        "r80": _threshold_at_recall(score, correct, is_pos, 0.80),
        # The platform's main operating point: at most 1% of honest same-artist works accused.
        "fpr1": _threshold_at_fpr(score, neg, 0.01) if neg.any() else -math.inf,
    }
    result = {
        "n_queries": int(len(gt)),
        "n_positive": int(is_pos.sum()),
        "n_negative": int(neg.sum()),
        "muap": micro_ap(retr, gt),
        "top1": float(correct[is_pos].mean()),
        "top1_by_type": top1_by_type,
        "auc_copy_vs_hard_negative": auc,
        "operating_points": {name: _report_at(t, score, correct, queries) for name, t in thresholds.items()},
    }
    for name, t in (transfer_thresholds or {}).items():
        t = -math.inf if t is None else t
        result["operating_points"][f"{name}_from_val"] = _report_at(t, score, correct, queries)
    return result
