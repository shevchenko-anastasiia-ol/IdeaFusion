"""Stage 4: fusing the pipeline's signals into one calibrated decision (chapter 1, req. B5).

Raw scores of different methods are not comparable (0.8 means different things for SSCD and
CLIP), so a model maps the pair features of a (query, candidate) pair to P(copy):

1. a classifier is fitted on train pairs (logistic regression - interpretable - or gradient
   boosting);
2. its output is calibrated with isotonic regression on val pairs, so P(copy) = 0.9 means that
   about 90% of such pairs really are copies;
3. thresholds for the risk zones are set on val by the false-accusation rate on hard negatives.

The output for a query is its best candidate's probability, a match type (exact copy,
modified copy, fragment, partial: the work is only part of the upload, or similar
composition) and a risk zone. There are no automatic sanctions: the zone tells the author and
the moderator how much attention a match deserves.
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field

import numpy as np
import pandas as pd
from sklearn.ensemble import HistGradientBoostingClassifier
from sklearn.isotonic import IsotonicRegression
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import make_pipeline
from sklearn.preprocessing import StandardScaler

from copydet.evaluation import Retrieval, _threshold_at_fpr, evaluate

KP = ["kp_inliers_log", "kp_inlier_ratio", "kp_q_cov", "kp_r_cov", "kp_r_center_cov"]
FEATURE_SETS: dict[str, list[str]] = {
    "sscd": ["sscd_raw", "sscd_norm", "sscd_gap", "rank"],
    "sscd+phash": ["sscd_raw", "sscd_norm", "sscd_gap", "rank", "phash_dist"],
    "sscd+phash+kp": ["sscd_raw", "sscd_norm", "sscd_gap", "rank", "phash_dist", *KP],
    "full": ["sscd_raw", "sscd_norm", "sscd_gap", "rank", "phash_dist", *KP, "clip_raw", "clip_norm"],
}

EXACT_HAMMING = 8  # pHash distance of a re-upload / recompression (section 1.2.1)
MIN_INLIERS = 15
# Fixed a priori from what the zones mean, not tuned on data (see FusionModel.set_zones).
RED_FLOOR = 0.5
YELLOW_FLOOR = 0.1


def add_derived(pairs: pd.DataFrame) -> pd.DataFrame:
    return pairs.assign(kp_inliers_log=np.log1p(pairs["kp_inliers"]))


def make_model(kind: str):
    if kind == "lr":
        return make_pipeline(StandardScaler(), LogisticRegression(C=1.0, max_iter=5000))
    if kind == "gbm":
        return HistGradientBoostingClassifier(max_iter=300, learning_rate=0.05, max_leaf_nodes=15, l2_regularization=1.0, random_state=0)
    raise ValueError(kind)


@dataclass
class FusionModel:
    features: list[str]
    kind: str = "lr"
    model: object = None
    calibrator: IsotonicRegression | None = None
    thresholds: dict = field(default_factory=dict)

    def fit(self, train: pd.DataFrame) -> "FusionModel":
        self.model = make_model(self.kind).fit(train[self.features].to_numpy(), train["label"].to_numpy())
        return self

    def raw(self, pairs: pd.DataFrame) -> np.ndarray:
        return self.model.predict_proba(pairs[self.features].to_numpy())[:, 1]

    def calibrate(self, val: pd.DataFrame) -> "FusionModel":
        self.calibrator = IsotonicRegression(out_of_bounds="clip", y_min=0.0, y_max=1.0)
        self.calibrator.fit(self.raw(val), val["label"].to_numpy().astype(float))
        return self

    def predict(self, pairs: pd.DataFrame) -> np.ndarray:
        p = self.raw(pairs)
        if self.calibrator is None:
            return p
        # Isotonic output is a step function: ties among the top pairs would make ranking
        # arbitrary, so the raw score breaks them without changing the calibrated value much.
        return np.minimum(self.calibrator.predict(p) + 1e-6 * p, 1.0)

    def set_zones(
        self,
        val_queries: pd.DataFrame,
        red_fpr: float = 0.01,
        yellow_fpr: float = 0.10,
        red_floor: float = RED_FLOOR,
        yellow_floor: float = YELLOW_FLOOR,
    ) -> None:
        """Thresholds on the query score: red = at most `red_fpr` of hard negatives reach it
        (a confident match), yellow = at most `yellow_fpr` (worth a moderator's look).

        The floors keep the zones meaningful to a person: a calibrated model squeezes most
        honest works to P = 0, so the FPR rule alone can put "red" at P = 0.1, and telling an
        author "likely copy, 10%" is misleading. Red needs a copy to be more likely than not.
        """
        s, neg = val_queries["p_copy"].to_numpy(), val_queries["is_negative"].to_numpy()
        self.thresholds = {
            "red": max(_threshold_at_fpr(s, neg, red_fpr), red_floor),
            "yellow": max(_threshold_at_fpr(s, neg, yellow_fpr), yellow_floor),
        }


def query_table(pairs: pd.DataFrame, p: np.ndarray) -> pd.DataFrame:
    """One row per query: its best candidate by P(copy), plus that pair's features."""
    pairs = pairs.assign(p_copy=p)
    best = pairs.sort_values(["query_id", "p_copy"], ascending=[True, False]).groupby("query_id", sort=False).head(1)
    any_label = pairs.groupby("query_id")["label"].any().rename("original_in_candidates")
    return best.merge(any_label, on="query_id").reset_index(drop=True)


def as_retrieval(pairs: pd.DataFrame, p: np.ndarray) -> tuple[Retrieval, np.ndarray, pd.DataFrame]:
    """Re-rank each query's candidates by P(copy) in the shape `evaluation.evaluate` expects:
    reference index 0 = the correct candidate, 1.. = wrong ones; gt = 0 or -1 (negatives)."""
    pairs = pairs.assign(p_copy=p).sort_values(["query_id", "p_copy"], ascending=[True, False])
    groups = pairs.groupby("query_id", sort=False)
    k = int(groups.size().max())
    n = groups.ngroups
    ref_idx = np.full((n, k), -2, dtype=int)
    scores = np.full((n, k), -np.inf)
    meta = []
    for i, (_, g) in enumerate(groups):
        labels = g["label"].to_numpy()
        ref_idx[i, : len(g)] = np.where(labels, 0, 1 + np.arange(len(g)))
        scores[i, : len(g)] = g["p_copy"].to_numpy()
        first = g.iloc[0]
        meta.append({"query_id": first["query_id"], "aug_type": first["aug_type"], "is_negative": first["is_negative"],
                     "negative_kind": first["negative_kind"]})
    queries = pd.DataFrame(meta)
    gt = np.where(queries["is_negative"], -1, 0)
    return Retrieval(ref_idx, scores), gt, queries


def evaluate_pairs(pairs: pd.DataFrame, p: np.ndarray, transfer_thresholds: dict | None = None) -> dict:
    retr, gt, queries = as_retrieval(pairs, p)
    return evaluate(retr, gt, queries, transfer_thresholds=transfer_thresholds)


def match_type(row: pd.Series, thresholds: dict, clip_similar: float) -> str:
    """Kind of match for the top candidate, from the signals that explain it."""
    if row["p_copy"] < thresholds["yellow"]:
        return "similar_composition" if row["clip_norm"] >= clip_similar else "none"
    if row["phash_dist"] <= EXACT_HAMMING:
        return "exact"
    # Both images are rescaled to the same longer side before matching, so the scale of the
    # fitted similarity transform tells how much of each image the other one covers.
    if row["kp_inliers"] >= MIN_INLIERS and row["kp_scale"] > 1.5:
        return "partial"  # the work is a small part of the upload: collage, screenshot
    if row["kp_inliers"] >= MIN_INLIERS and 0 < row["kp_scale"] < 0.6:
        return "fragment"  # the upload is a blown-up piece of the work
    return "modified"


def risk_zone(p: float, thresholds: dict) -> str:
    if p >= thresholds["red"]:
        return "red"
    if p >= thresholds["yellow"]:
        return "yellow"
    return "green"


def expected_calibration_error(prob: np.ndarray, correct: np.ndarray, bins: int = 10) -> tuple[float, pd.DataFrame]:
    edges = np.linspace(0, 1, bins + 1)
    idx = np.clip(np.digitize(prob, edges[1:-1]), 0, bins - 1)
    rows, ece = [], 0.0
    for b in range(bins):
        m = idx == b
        if m.any():
            conf, acc = float(prob[m].mean()), float(correct[m].mean())
            ece += m.mean() * abs(conf - acc)
            rows.append({"bin": b, "lo": edges[b], "hi": edges[b + 1], "n": int(m.sum()), "confidence": conf, "accuracy": acc})
    return float(ece), pd.DataFrame(rows)


def finite(x: float) -> float | None:
    return x if x is not None and math.isfinite(x) else None
