"""Stage 2: baselines without training - pHash, SSCD, CLIP (+ background score normalisation).

    uv run python scripts/05_eval_baselines.py --dataset data/processed/artbench_v1

Index per split = originals of that split + distractors (minus collage backgrounds). Thresholds
for the test split are also transferred from val. Outputs go to runs/baselines_<dataset>/:
    metrics.json     full metrics per method and split
    per_type.csv     recall at precision >= 0.99 and top-1 per modification type
    summary.md       the main comparison table
    per_type.png     recall per modification type
    false_positives_<method>.png   the highest-scoring wrong matches
"""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path

from copydet import descriptors as D
from copydet.benchmark import Benchmark, thresholds_of
from copydet.reporting import save_false_positives, write_reports

ROOT = Path(__file__).resolve().parents[1]


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dataset", type=Path, default=Path("data/processed/artbench_v1"))
    ap.add_argument("--catalog", type=Path, default=Path("data/manifests/catalog.parquet"))
    ap.add_argument("--sscd-weights", type=Path, default=Path("data/weights/sscd_disc_mixup.torchscript.pt"))
    ap.add_argument("--methods", nargs="+", default=["phash", "sscd", "clip"])
    ap.add_argument("--splits", nargs="+", default=["val", "test"])
    ap.add_argument("--bg-size", type=int, default=2000)
    ap.add_argument("--k", type=int, default=10)
    ap.add_argument("--seed", type=int, default=0)
    args = ap.parse_args()
    os.chdir(ROOT)

    bench = Benchmark(args.dataset, args.catalog, tuple(args.splits), args.bg_size, args.seed)
    views = {s: bench.split(s) for s in args.splits}
    out_dir = ROOT / "runs" / f"baselines_{bench.dataset.name}"
    out_dir.mkdir(parents=True, exist_ok=True)

    results: dict = {}
    for method in args.methods:
        if method == "phash":
            desc = D.PHash()
        elif method == "sscd":
            desc = D.SSCD(args.sscd_weights)
        elif method == "clip":
            desc = D.CLIP()
        else:
            raise SystemExit(f"unknown method {method}")
        emb, bg = bench.embed(desc, with_background=method != "phash")
        ms = D.ms_per_image(desc.name, bench.cache_dir)

        for variant in [method] if method == "phash" else [method, f"{method}+norm"]:
            from_val = None
            for split in args.splits:
                res, retr = bench.run(
                    views[split], emb, bg,
                    kind="phash" if method == "phash" else "cosine",
                    normalize=variant.endswith("+norm"),
                    k=args.k,
                    transfer_thresholds=from_val if split != "val" else None,
                )
                res["ms_per_image"] = ms
                results.setdefault(variant, {})[split] = res
                if split == "val":
                    from_val = thresholds_of(res)
                print_line(variant, split, res)
                if split == args.splits[-1]:
                    save_false_positives(retr, views[split], bench.dataset, out_dir / f"false_positives_{variant}.png")

    (out_dir / "metrics.json").write_text(json.dumps(results, indent=2, default=float), encoding="utf-8")
    write_reports(results, out_dir, "test" if "test" in args.splits else args.splits[-1])
    print(f"-> {out_dir}")


def print_line(variant: str, split: str, res: dict) -> None:
    ops = res["operating_points"]
    p99 = ops.get("p99_from_val", ops["p99"])
    fpr1 = ops.get("fpr1_from_val", ops["fpr1"])
    print(
        f"{variant:12s} {split:5s} muAP={res['muap']:.3f} top1={res['top1']:.3f} "
        f"R@P99={p99['recall']:.3f} (FPR {p99['hard_negative_fpr'].get('all', float('nan')):.3f}) "
        f"R@FPR1%={fpr1['recall']:.3f} (FPR {fpr1['hard_negative_fpr'].get('all', float('nan')):.3f})"
    )


if __name__ == "__main__":
    main()
