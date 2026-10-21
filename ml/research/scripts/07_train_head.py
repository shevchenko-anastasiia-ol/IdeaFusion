"""Stage 3: train projection heads over frozen SSCD and compare with the baseline.

    uv run python scripts/07_train_head.py --config configs/head.yaml

Needs the cached SSCD embeddings from 05_eval_baselines.py (val/test/background) and the
training views from 06_embed_train.py. For every variant x seed: train, keep the epoch with
the best val score (recall at hard-negative FPR <= 1%, ties broken by muAP), then evaluate test
with thresholds transferred from val. Same protocol for every variant (Musgrave et al., 2020).

Output in runs/head_<dataset>/:
    <variant>/seed<k>/head.pt, history.csv, metrics.json
    summary.md, per_type.csv, false_positives_<best>.png
"""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path

import numpy as np
import pandas as pd
import torch
import yaml

from copydet.benchmark import Benchmark, thresholds_of
from copydet.descriptors import SSCD
from copydet.head import HeadConfig, apply_head, train_head
from copydet.reporting import fmt, save_false_positives

ROOT = Path(__file__).resolve().parents[1]
METRICS = [
    ("muap", "muAP"),
    ("top1", "top-1"),
    ("p99.recall", "recall@P99"),
    ("p99.fpr", "hard-neg FPR @P99"),
    ("fpr1.recall", "recall @ FPR≤1% (val thr)"),
    ("fpr1.fpr", "actual hard-neg FPR"),
    # Threshold set on test itself: equal false-accusation rate for every method, so this
    # column compares discrimination; the two before it compare the deployable system.
    ("fpr1_oracle.recall", "recall @ FPR≤1% (oracle)"),
    ("auc_copy_vs_hard_negative", "AUC"),
]


def val_score(res: dict) -> float:
    return res["operating_points"]["fpr1"]["recall"] + 0.1 * res["muap"]


def pick(res: dict, key: str) -> float:
    if "." not in key:
        return res[key]
    op, field = key.split(".")
    ops = res["operating_points"]
    op = ops[op.removesuffix("_oracle")] if op.endswith("_oracle") else ops.get(f"{op}_from_val", ops[op])
    return op["hard_negative_fpr"].get("all", float("nan")) if field == "fpr" else op[field]


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--config", type=Path, default=Path("configs/head.yaml"))
    ap.add_argument("--dataset", type=Path, default=Path("data/processed/artbench_v1"))
    ap.add_argument("--catalog", type=Path, default=Path("data/manifests/catalog.parquet"))
    ap.add_argument("--sscd-weights", type=Path, default=Path("data/weights/sscd_disc_mixup.torchscript.pt"))
    ap.add_argument("--variants", nargs="*", help="subset of variants from the config")
    ap.add_argument("--seeds", nargs="*", type=int, help="override the config's seeds")
    ap.add_argument("--epochs", type=int, help="override epochs (quick runs)")
    ap.add_argument("--out-suffix", default="", help="write to runs/head_<dataset><suffix>")
    args = ap.parse_args()
    os.chdir(ROOT)
    torch.set_num_threads(max(1, (os.cpu_count() or 2) - 2))

    cfg = yaml.safe_load(args.config.read_text(encoding="utf-8"))
    if args.seeds:
        cfg["seeds"] = args.seeds
    if args.epochs:
        cfg["defaults"]["epochs"] = args.epochs
    normalize = cfg.get("normalize", True)
    bench = Benchmark(args.dataset, args.catalog)
    emb, bg = bench.embed(SSCD(args.sscd_weights))  # cached by 05_eval_baselines.py
    val, test = bench.split("val"), bench.split("test")

    with np.load(bench.cache_dir / "train_sscd.npz", allow_pickle=False) as z:
        views, artists = z["emb"], z["artist"]
    print(f"training views {views.shape}, artists {len(np.unique(artists))}")

    def run(head) -> tuple[dict, dict, object]:
        e, b = apply_head(head, emb), apply_head(head, bg)
        r_val, _ = bench.run(val, e, b, normalize=normalize)
        r_test, retr = bench.run(test, e, b, normalize=normalize, transfer_thresholds=thresholds_of(r_val))
        return r_val, r_test, retr

    out_dir = ROOT / "runs" / f"head_{bench.dataset.name}{args.out_suffix}"
    out_dir.mkdir(parents=True, exist_ok=True)
    rows: list[dict] = []
    test_results: dict[str, list[dict]] = {}
    base_val, base_test, _ = run(None)
    test_results["sscd" + ("+norm" if normalize else "")] = [base_test]
    print(f"baseline  val score {val_score(base_val):.4f}  test R@FPR1% {pick(base_test, 'fpr1.recall'):.3f}")

    best = (-np.inf, None, None)
    for name, overrides in cfg["variants"].items():
        if args.variants and name not in args.variants:
            continue
        hc = HeadConfig(**{**cfg["defaults"], **overrides})
        for seed in cfg["seeds"]:
            def validate(head) -> float:
                r_val, _ = bench.run(val, apply_head(head, emb), apply_head(head, bg), normalize=normalize)
                return val_score(r_val)

            head, history = train_head(views, artists, hc, seed, validate)
            r_val, r_test, retr = run(head)
            run_dir = out_dir / name / f"seed{seed}"
            run_dir.mkdir(parents=True, exist_ok=True)
            torch.save({"state_dict": head.state_dict(), "config": hc.__dict__, "normalize": normalize}, run_dir / "head.pt")
            pd.DataFrame(history).to_csv(run_dir / "history.csv", index=False)
            (run_dir / "metrics.json").write_text(json.dumps({"val": r_val, "test": r_test}, indent=2, default=float), encoding="utf-8")
            test_results.setdefault(name, []).append(r_test)
            best_epoch = max(history, key=lambda h: h["val"])["epoch"]
            print(f"{name:16s} seed {seed}  best epoch {best_epoch:2d}/{len(history) - 1}  val {val_score(r_val):.4f}  "
                  f"test R@FPR1% {pick(r_test, 'fpr1.recall'):.3f} (FPR {pick(r_test, 'fpr1.fpr'):.3f})  muAP {r_test['muap']:.3f}")
            if val_score(r_val) > best[0]:
                best = (val_score(r_val), f"{name}/seed{seed}", retr)

    write_summary(test_results, out_dir, best[1])
    if best[2] is not None:
        save_false_positives(best[2], test, bench.dataset, out_dir / f"false_positives_{best[1].replace('/', '_')}.png")
    print(f"best on val: {best[1]}\n-> {out_dir}")


def write_summary(results: dict[str, list[dict]], out_dir: Path, best: str | None) -> None:
    def cell(values: list[float]) -> str:
        if len(values) == 1:
            return fmt(values[0])
        return f"{fmt(float(np.mean(values)))} ± {fmt(float(np.std(values)))}"

    lines = [
        "# Projection head over frozen SSCD, split `test`",
        "",
        "Thresholds from val, transferred to test. Heads: mean ± std over seeds.",
        f"Best run on val: `{best}`.",
        "",
        "| method | runs | " + " | ".join(label for _, label in METRICS) + " |",
        "|---|---|" + "---|" * len(METRICS),
    ]
    for name, runs in results.items():
        lines.append(f"| {name} | {len(runs)} | " + " | ".join(cell([pick(r, k) for r in runs]) for k, _ in METRICS) + " |")

    type_rows = []
    for name, runs in results.items():
        for r in runs:
            for t, v in pick_op(r, "fpr1")["recall_by_type"].items():
                type_rows.append({"method": name, "aug_type": t, "recall": v})
    per_type = pd.DataFrame(type_rows).groupby(["aug_type", "method"])["recall"].mean().unstack()[list(results)]
    per_type.to_csv(out_dir / "per_type.csv")
    lines += [
        "",
        "## Recall at hard-negative FPR ≤ 1% by modification type (mean over seeds)",
        "",
        "Collage and screenshot never appear in training: they show whether robustness was kept.",
        "",
        "| type | " + " | ".join(per_type.columns) + " |",
        "|---|" + "---|" * len(per_type.columns),
        *("| " + t + " | " + " | ".join(fmt(v) for v in row) + " |" for t, row in per_type.iterrows()),
        "",
    ]
    (out_dir / "summary.md").write_text("\n".join(lines), encoding="utf-8")


def pick_op(res: dict, name: str) -> dict:
    ops = res["operating_points"]
    return ops.get(f"{name}_from_val", ops[name])


if __name__ == "__main__":
    main()
