"""Markdown tables, per-type plots and false-positive grids for benchmark results."""

from __future__ import annotations

from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
from PIL import Image

from copydet.benchmark import SplitView
from copydet.evaluation import Retrieval


def fmt(x, digits: int = 3) -> str:
    if x is None or (isinstance(x, float) and not np.isfinite(x)):
        return "—"
    return f"{x:.{digits}f}"


def _op(result: dict, name: str) -> dict:
    ops = result["operating_points"]
    return ops.get(f"{name}_from_val", ops[name])


def main_table(results: dict[str, dict], split: str) -> list[str]:
    """One row per method: the numbers the thesis compares."""
    rows = [
        "| method | muAP | top-1 | recall@P99 | hard-neg FPR @P99 | **recall @ hard-neg FPR ≤ 1%** | same, oracle threshold | AUC copy vs hard-neg | ms/img |",
        "|---|---|---|---|---|---|---|---|---|",
    ]
    for method, by_split in results.items():
        r = by_split[split]
        p99, fpr1 = _op(r, "p99"), _op(r, "fpr1")
        oracle = r["operating_points"]["fpr1"]
        ms = r.get("ms_per_image")
        rows.append(
            f"| {method} | {fmt(r['muap'])} | {fmt(r['top1'])} | {fmt(p99['recall'])} | "
            f"{fmt(p99['hard_negative_fpr'].get('all'))} | **{fmt(fpr1['recall'])}** "
            f"(FPR {fmt(fpr1['hard_negative_fpr'].get('all'))}) | {fmt(oracle['recall'])} | "
            f"{fmt(r['auc_copy_vs_hard_negative'])} | "
            f"{'—' if ms is None else f'{ms:.1f}'} |"
        )
    return rows


def per_type_frame(results: dict[str, dict], split: str, op: str = "fpr1") -> pd.DataFrame:
    rows = []
    for method, by_split in results.items():
        r = by_split[split]
        for t, rec in _op(r, op)["recall_by_type"].items():
            rows.append({"method": method, "aug_type": t, f"recall_{op}": rec, "top1": r["top1_by_type"].get(t)})
    return pd.DataFrame(rows)


def write_reports(results: dict[str, dict], out_dir: Path, split: str, title: str = "Baselines") -> None:
    transferred = split != "val"
    per_type = per_type_frame(results, split)
    per_type.to_csv(out_dir / "per_type.csv", index=False)
    pivot = per_type.pivot(index="aug_type", columns="method", values="recall_fpr1")[list(results)]

    lines = [
        f"# {title}, split `{split}`",
        "",
        f"Thresholds {'taken from val and transferred' if transferred else 'set on this split'}.",
        "Hard negatives = other works of the same artist; FPR = share of them matched above the threshold.",
        "`recall @ hard-neg FPR ≤ 1%` is the main operating point: at most 1 in 100 honest works accused.",
        "With a val threshold the actual FPR on test drifts, so methods differ in how many they accuse;",
        "the oracle column sets the threshold on test itself (equal FPR) and compares discrimination only.",
        "",
        *main_table(results, split),
        "",
        "## Recall at hard-negative FPR ≤ 1%, by modification type",
        "",
        "| type | " + " | ".join(pivot.columns) + " |",
        "|---|" + "---|" * len(pivot.columns),
        *("| " + t + " | " + " | ".join(fmt(v) for v in row) + " |" for t, row in pivot.iterrows()),
        "",
    ]
    (out_dir / "summary.md").write_text("\n".join(lines), encoding="utf-8")

    ax = pivot.plot.bar(figsize=(12, 4.5), width=0.8)
    ax.set_ylabel("recall @ hard-negative FPR ≤ 1%")
    ax.set_xlabel("")
    ax.set_ylim(0, 1)
    ax.legend(ncol=len(pivot.columns), fontsize=8, loc="upper center", bbox_to_anchor=(0.5, 1.15))
    plt.xticks(rotation=30, ha="right")
    plt.tight_layout()
    plt.savefig(out_dir / "per_type.png", dpi=130)
    plt.close()


def save_false_positives(retr: Retrieval, view: SplitView, dataset: Path, out: Path, n: int = 8) -> None:
    """The highest-scoring wrong top-1 matches: what the method confuses with a copy."""
    wrong = np.flatnonzero(retr.ref_idx[:, 0] != view.gt)
    worst = wrong[np.argsort(-retr.scores[wrong, 0])][:n]
    if len(worst) == 0:
        return
    fig, axes = plt.subplots(2, len(worst), figsize=(2.2 * len(worst), 4.8), squeeze=False)
    for c, i in enumerate(worst):
        qrow, rrow = view.queries.iloc[i], view.refs.iloc[retr.ref_idx[i, 0]]
        label = qrow["negative_kind"] if qrow["is_negative"] else qrow["aug_type"]
        cells = [(qrow["path"], f"query: {label}"), (rrow["path"], f"match {retr.scores[i, 0]:.3f}")]
        for r, (path, title) in enumerate(cells):
            axes[r, c].imshow(Image.open(dataset / path))
            axes[r, c].set_title(title, fontsize=8)
            axes[r, c].axis("off")
    fig.tight_layout()
    fig.savefig(out, dpi=110)
    plt.close(fig)
