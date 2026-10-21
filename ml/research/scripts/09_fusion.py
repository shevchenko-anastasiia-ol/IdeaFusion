"""Stage 4: fuse signals, calibrate, and evaluate the whole pipeline on test.

    uv run python scripts/09_fusion.py --dataset data/processed/artbench_v1

Needs runs/fusion_<dataset>/pairs.parquet from 08_pair_features.py. Fusion models are fitted
on train pairs, calibrated (isotonic) and thresholded on val, and evaluated on test.

Ablation: SSCD alone -> + pHash -> + keypoints -> + CLIP, each as logistic regression and as
gradient boosting, plus the raw SSCD+norm score without any model. The model with the best
val score is the one that would be deployed; its artefacts are saved for the service.

Output in runs/fusion_<dataset>/:
    summary.md, reliability.png, explanations.png, fusion_model.joblib, thresholds.json
"""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path

import joblib
import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from PIL import Image

from copydet import keypoints as K
from copydet.benchmark import thresholds_of
from copydet.fusion import (
    EXACT_HAMMING,
    FEATURE_SETS,
    MIN_INLIERS,
    FusionModel,
    add_derived,
    evaluate_pairs,
    expected_calibration_error,
    finite,
    match_type,
    query_table,
    risk_zone,
)
from copydet.imageio import load_rgb
from copydet.reporting import fmt

ROOT = Path(__file__).resolve().parents[1]
SELECTION_TOLERANCE = 0.002


def val_score(res: dict) -> float:
    return res["operating_points"]["fpr1"]["recall"] + 0.1 * res["muap"]


def op(res: dict, name: str) -> dict:
    ops = res["operating_points"]
    return ops.get(f"{name}_from_val", ops[name])


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dataset", type=Path, default=Path("data/processed/artbench_v1"))
    args = ap.parse_args()
    os.chdir(ROOT)
    ds = args.dataset
    out_dir = ROOT / "runs" / f"fusion_{ds.name}"
    pairs = add_derived(pd.read_parquet(out_dir / "pairs.parquet"))
    train, val, test = (pairs[pairs["split"] == s].reset_index(drop=True) for s in ("train", "val", "test"))
    print(f"pairs: train {len(train)}, val {len(val)}, test {len(test)}")

    results, models = {}, {}
    # Reference: the chosen descriptor's own score, no fusion model.
    r_val = evaluate_pairs(val, val["sscd_norm"].to_numpy())
    results["sscd+norm (no model)"] = {"val": r_val, "test": evaluate_pairs(test, test["sscd_norm"].to_numpy(), thresholds_of(r_val))}
    for kind in ("lr", "gbm"):
        for name, feats in FEATURE_SETS.items():
            m = FusionModel(feats, kind).fit(train).calibrate(val)
            r_val = evaluate_pairs(val, m.predict(val))
            r_test = evaluate_pairs(test, m.predict(test), thresholds_of(r_val))
            key = f"{kind}: {name}"
            results[key], models[key] = {"val": r_val, "test": r_test}, m
            print(f"{key:24s} val {val_score(r_val):.4f}  test R@FPR1% {op(r_test, 'fpr1')['recall']:.3f} "
                  f"(FPR {op(r_test, 'fpr1')['hard_negative_fpr'].get('all', float('nan')):.3f}) muAP {r_test['muap']:.3f}")

    # Among models within noise of the best val score, deploy the one with fewest features:
    # CLIP, for instance, costs a 600 MB model and ~40 ms per image.
    top = max(val_score(results[k]["val"]) for k in models)
    close = [k for k in models if val_score(results[k]["val"]) >= top - SELECTION_TOLERANCE]
    best_key = min(close, key=lambda k: (len(models[k].features), -val_score(results[k]["val"])))
    best = models[best_key]
    print(f"best on val: {max(models, key=lambda k: val_score(results[k]['val']))}; deployed (simplest within "
          f"{SELECTION_TOLERANCE} of it): {best_key}")

    q_val = query_table(val, best.predict(val))
    best.set_zones(q_val)
    q_test = query_table(test, best.predict(test))
    neg_val_clip = q_val.loc[q_val["is_negative"], "clip_norm"]
    clip_similar = float(neg_val_clip.quantile(0.99)) if len(neg_val_clip) else float("inf")
    q_test["zone"] = [risk_zone(p, best.thresholds) for p in q_test["p_copy"]]
    q_test["match_type"] = [match_type(r, best.thresholds, clip_similar) for _, r in q_test.iterrows()]
    q_test["group"] = np.where(q_test["is_negative"], "hard negative", q_test["aug_type"])

    ece_val, rel_val = expected_calibration_error(q_val["p_copy"].to_numpy(), q_val["label"].to_numpy())
    ece_test, rel_test = expected_calibration_error(q_test["p_copy"].to_numpy(), q_test["label"].to_numpy())
    plot_reliability(rel_val, rel_test, ece_val, ece_test, out_dir / "reliability.png")
    save_explanations(q_test, ds, out_dir / "explanations.png")

    joblib.dump(best, out_dir / "fusion_model.joblib")
    thresholds = {
        "model": best_key,
        "features": best.features,
        "zones": {k: finite(v) for k, v in best.thresholds.items()},
        "clip_similar_composition": clip_similar,
        "exact_hamming": EXACT_HAMMING,
        "min_inliers": MIN_INLIERS,
        "candidates_k": int(pairs["rank"].max()) + 1,
    }
    (out_dir / "thresholds.json").write_text(json.dumps(thresholds, indent=2), encoding="utf-8")
    (out_dir / "metrics.json").write_text(json.dumps(results, indent=2, default=float), encoding="utf-8")
    write_summary(results, best_key, best, q_test, ece_val, ece_test, out_dir)
    print(f"-> {out_dir}")


def write_summary(results, best_key, best, q_test, ece_val, ece_test, out_dir: Path) -> None:
    lines = [
        "# Signal fusion, split `test`",
        "",
        "Fusion models fitted on train pairs, calibrated (isotonic) and thresholded on val; test held out.",
        "Candidates: top-5 of SSCD+norm. `val thr` = threshold from val (deployable); `oracle` = equal FPR on test.",
        "",
        "| model | muAP | top-1 | recall @ FPR≤1% (val thr) | actual hard-neg FPR | recall @ FPR≤1% (oracle) | AUC |",
        "|---|---|---|---|---|---|---|",
    ]
    for key, r in results.items():
        t = r["test"]
        f1 = op(t, "fpr1")
        mark = " **(deployed)**" if key == best_key else ""
        lines.append(
            f"| {key}{mark} | {fmt(t['muap'])} | {fmt(t['top1'])} | {fmt(f1['recall'])} | "
            f"{fmt(f1['hard_negative_fpr'].get('all'))} | {fmt(t['operating_points']['fpr1']['recall'])} | "
            f"{fmt(t['auc_copy_vs_hard_negative'])} |"
        )

    t = results[best_key]["test"]
    base = results["sscd+norm (no model)"]["test"]
    types = sorted(op(t, "fpr1")["recall_by_type"])
    lines += [
        "",
        f"## `{best_key}` vs SSCD+norm by modification type (recall @ FPR≤1%, val threshold)",
        "",
        "| type | sscd+norm | fused |",
        "|---|---|---|",
        *(f"| {ty} | {fmt(op(base, 'fpr1')['recall_by_type'].get(ty))} | {fmt(op(t, 'fpr1')['recall_by_type'][ty])} |" for ty in types),
        "",
        "## Calibration",
        "",
        f"Expected calibration error of P(copy) for the top candidate: val {ece_val:.3f}, test {ece_test:.3f} "
        "(see `reliability.png`). Test contains collages and screenshots, which val does not.",
        "",
        f"## Risk zones on test (red ≥ {fmt(best.thresholds['red'])}, yellow ≥ {fmt(best.thresholds['yellow'])})",
        "",
    ]
    zones = pd.crosstab(q_test["group"], q_test["zone"], normalize="index").reindex(columns=["red", "yellow", "green"], fill_value=0)
    lines += ["| group | red | yellow | green |", "|---|---|---|---|"]
    lines += [f"| {g} | {fmt(r['red'], 2)} | {fmt(r['yellow'], 2)} | {fmt(r['green'], 2)} |" for g, r in zones.iterrows()]

    mt = pd.crosstab(q_test["group"], q_test["match_type"], normalize="index")
    lines += [
        "",
        "## Match type on test (share of queries per group)",
        "",
        "| group | " + " | ".join(mt.columns) + " |",
        "|---|" + "---|" * len(mt.columns),
        *(f"| {g} | " + " | ".join(fmt(v, 2) for v in r) + " |" for g, r in mt.iterrows()),
    ]
    if best.kind == "lr":
        coef = best.model[-1].coef_[0]
        lines += [
            "",
            "## Logistic regression weights (standardised features)",
            "",
            "| feature | weight |",
            "|---|---|",
            *(f"| {f} | {c:+.2f} |" for f, c in sorted(zip(best.features, coef), key=lambda x: -abs(x[1]))),
        ]
    (out_dir / "summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def plot_reliability(rel_val: pd.DataFrame, rel_test: pd.DataFrame, ece_val: float, ece_test: float, out: Path) -> None:
    fig, ax = plt.subplots(figsize=(4.8, 4.5))
    ax.plot([0, 1], [0, 1], color="0.7", lw=1, ls="--")
    for rel, label in ((rel_val, f"val (ECE {ece_val:.3f})"), (rel_test, f"test (ECE {ece_test:.3f})")):
        ax.plot(rel["confidence"], rel["accuracy"], marker="o", lw=1.5, label=label)
    ax.set_xlabel("predicted P(copy)")
    ax.set_ylabel("share that really are copies")
    ax.legend(fontsize=8)
    fig.tight_layout()
    fig.savefig(out, dpi=130)
    plt.close(fig)


def save_explanations(q: pd.DataFrame, ds: Path, out: Path) -> None:
    """What an author/moderator would see: query, matched work, inlier correspondences."""
    picks = []
    for group in ("fragment", "collage", "screenshot", "hflip", "watermark"):
        g = q[(q["group"] == group) & q["label"]].sort_values("p_copy", ascending=False)
        if len(g):
            picks.append(g.iloc[len(g) // 2])  # a typical case, not the easiest one
    negs = q[q["is_negative"]].sort_values("kp_inliers", ascending=False)
    if len(negs):
        picks.append(negs.iloc[0])  # the hard negative with the most matching points
    fig, axes = plt.subplots(len(picks), 1, figsize=(8, 3.3 * len(picks)), squeeze=False)
    for ax, row in zip(axes[:, 0], picks):
        qi, ri = load_rgb(ds / row["query_path"]), load_rgb(ds / row["ref_path"])
        m = K.match(K.extract_with_mirror(qi), K.extract(ri), with_points=True)
        qs, rs = _scaled(qi), _scaled(ri)
        if m.get("kp_mirrored"):
            qs = qs.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        canvas = Image.new("RGB", (qs.width + rs.width + 20, max(qs.height, rs.height)), "white")
        canvas.paste(qs, (0, 0))
        canvas.paste(rs, (qs.width + 20, 0))
        ax.imshow(canvas)
        if "_src" in m:
            step = max(1, len(m["_src"]) // 60)
            for (x1, y1), (x2, y2) in zip(m["_src"][::step], m["_dst"][::step]):
                ax.plot([x1, x2 + qs.width + 20], [y1, y2], lw=0.6, color="#1f9e89", alpha=0.8)
        kind = "hard negative (same artist)" if row["is_negative"] else row["aug_type"]
        ax.set_title(
            f"{kind}: P(copy)={row['p_copy']:.3f}, zone {row['zone']}, type {row['match_type']}, "
            f"inliers {m['kp_inliers']}, centre coverage {m['kp_r_center_cov']:.2f}",
            fontsize=8,
        )
        ax.axis("off")
    fig.tight_layout()
    fig.savefig(out, dpi=110)
    plt.close(fig)


def _scaled(img: Image.Image) -> Image.Image:
    s = K.SIDE / max(img.size)
    return img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))))


if __name__ == "__main__":
    main()
