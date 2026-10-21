"""Stage 2: baselines without training - pHash, SSCD, CLIP (+ background score normalisation).

    uv run python scripts/05_eval_baselines.py --dataset data/processed/artbench_v1

Index per split = originals of that split + all distractors. Thresholds for the test split are
also transferred from val. Outputs go to runs/baselines_<dataset>/:
    metrics.json     full metrics per method and split
    per_type.csv     recall at precision >= 0.99 and top-1 per modification type
    summary.md       the main comparison table
    per_type.png     recall per modification type
"""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd

from copydet import descriptors as D
from copydet.evaluation import evaluate, normalize_with_background, search_cosine, search_phash

ROOT = Path(__file__).resolve().parents[1]


def load_dataset(ds: Path):
    originals = pd.read_parquet(ds / "originals.parquet")
    queries = pd.read_parquet(ds / "queries.parquet")
    distractors = pd.read_parquet(ds / "distractors.parquet")
    return originals, queries, distractors


def collage_backgrounds(queries: pd.DataFrame) -> set[str]:
    """Distractors pasted under a collage. A collage really contains a copy of its background,
    so matching it is not an error; they are kept out of that split's index instead."""
    collages = queries.loc[queries["aug_type"] == "collage", "params"].dropna()
    return {json.loads(p)["background"] for p in collages}


def background_set(catalog: Path, exclude: set[str], n: int, seed: int) -> pd.DataFrame:
    """Works never used in the dataset, for SSCD-style score normalisation."""
    cat = pd.read_parquet(catalog)
    cat = cat[~cat["image_id"].isin(exclude)].sort_values("image_id")
    return cat.sample(min(n, len(cat)), random_state=seed)


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

    ds = args.dataset
    originals, queries, distractors = load_dataset(ds)
    # Hard negatives keep their catalog image_id as query_id, so this excludes them too.
    used = set(originals["image_id"]) | set(queries["query_id"]) | set(distractors["image_id"])
    bg = background_set(args.catalog, used, args.bg_size, args.seed)
    queries = queries[queries["split"].isin(args.splits)]

    cache = ROOT / "data" / "embeddings" / ds.name
    out_dir = ROOT / "runs" / f"baselines_{ds.name}"
    out_dir.mkdir(parents=True, exist_ok=True)

    # One embedding pass per method over every image any split needs.
    ref_rows = pd.concat([originals.assign(kind="original"), distractors.assign(kind="distractor", split=None)])
    image_keys = list(dict.fromkeys(["ds:" + p for p in ref_rows["path"]] + ["ds:" + p for p in queries["path"]]))
    image_paths = [ds / k[3:] for k in image_keys]
    bg_keys = ["cat:" + i for i in bg["image_id"]]
    bg_paths = [Path(p) for p in bg["path"]]
    key_pos = {k: i for i, k in enumerate(image_keys)}

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
        emb = D.embed_cached(desc, image_keys, image_paths, cache)
        bg_emb = D.embed_cached(desc, bg_keys, bg_paths, cache) if method != "phash" else None
        ms = D.ms_per_image(desc.name, cache)

        variants = [method] if method == "phash" else [method, f"{method}+norm"]
        for variant in variants:
            thresholds_from_val: dict | None = None
            for split in args.splits:
                q = queries[queries["split"] == split].reset_index(drop=True)
                refs = ref_rows[
                    ((ref_rows["kind"] == "distractor") & ~ref_rows["image_id"].isin(collage_backgrounds(q)))
                    | (ref_rows["split"] == split)
                ].reset_index(drop=True)
                r_emb = emb[[key_pos["ds:" + p] for p in refs["path"]]]
                q_emb = emb[[key_pos["ds:" + p] for p in q["path"]]]
                ref_index = {img_id: i for i, img_id in enumerate(refs["image_id"])}
                gt = q["original_id"].map(ref_index).fillna(-1).astype(int).to_numpy()

                if method == "phash":
                    retr = search_phash(q_emb, r_emb, args.k)
                else:
                    retr = search_cosine(q_emb, r_emb, args.k)
                    if variant.endswith("+norm"):
                        retr = normalize_with_background(retr, q_emb, bg_emb)

                res = evaluate(retr, gt, q, transfer_thresholds=thresholds_from_val if split == "test" else None)
                res["ms_per_image"] = ms
                res["index_size"] = len(refs)
                results.setdefault(variant, {})[split] = res
                if split == "val":
                    thresholds_from_val = {k: v["threshold"] for k, v in res["operating_points"].items()}
                ops = res["operating_points"]
                transferred = ops.get("p99_from_val", ops["p99"])
                print(f"{variant:12s} {split:5s} muAP={res['muap']:.3f} top1={res['top1']:.3f} "
                      f"R@P99(oracle)={ops['p99']['recall']:.3f} "
                      f"R@P99(val thr)={transferred['recall']:.3f} P={transferred['precision']:.3f} "
                      f"FPR@R80={ops['r80']['hard_negative_fpr'].get('all', float('nan')):.3f}")
                if split == args.splits[-1]:
                    save_false_positives(retr, gt, q, refs, ds, out_dir / f"false_positives_{variant}.png")

    (out_dir / "metrics.json").write_text(json.dumps(results, indent=2, default=float), encoding="utf-8")
    write_reports(results, out_dir, "test" if "test" in args.splits else args.splits[-1])
    print(f"-> {out_dir}")


def save_false_positives(retr, gt, queries: pd.DataFrame, refs: pd.DataFrame, ds: Path, out: Path, n: int = 8) -> None:
    """The highest-scoring wrong top-1 matches: what the method confuses with a copy."""
    from PIL import Image

    wrong = np.flatnonzero(retr.ref_idx[:, 0] != gt)
    worst = wrong[np.argsort(-retr.scores[wrong, 0])][:n]
    if len(worst) == 0:
        return
    fig, axes = plt.subplots(2, len(worst), figsize=(2.2 * len(worst), 4.8), squeeze=False)
    for c, i in enumerate(worst):
        qrow, rrow = queries.iloc[i], refs.iloc[retr.ref_idx[i, 0]]
        label = qrow["negative_kind"] if qrow["is_negative"] else qrow["aug_type"]
        for r, (path, title) in enumerate([(qrow["path"], f"query: {label}"), (rrow["path"], f"match {retr.scores[i, 0]:.3f}")]):
            axes[r, c].imshow(Image.open(ds / path))
            axes[r, c].set_title(title, fontsize=8)
            axes[r, c].axis("off")
    fig.tight_layout()
    fig.savefig(out, dpi=110)
    plt.close(fig)


def _fmt(x) -> str:
    return "—" if x is None or (isinstance(x, float) and not np.isfinite(x)) else f"{x:.3f}"


def write_reports(results: dict, out_dir: Path, split: str) -> None:
    op = "p99_from_val" if split == "test" else "p99"
    rows, type_rows = [], []
    for method, by_split in results.items():
        r = by_split[split]
        p99, r80 = r["operating_points"][op], r["operating_points"]["r80"]
        rows.append(
            "| {m} | {muap} | {top1} | {rec} | {prec} | {fpr99} | {fpr80} | {auc} | {ms} |".format(
                m=method,
                muap=_fmt(r["muap"]),
                top1=_fmt(r["top1"]),
                rec=_fmt(p99["recall"]),
                prec=_fmt(p99["precision"]),
                fpr99=_fmt(p99["hard_negative_fpr"].get("all")),
                fpr80=_fmt(r80["hard_negative_fpr"].get("all")),
                auc=_fmt(r["auc_copy_vs_hard_negative"]),
                ms="—" if r["ms_per_image"] is None else f"{r['ms_per_image']:.1f}",
            )
        )
        for t, rec in p99["recall_by_type"].items():
            type_rows.append({"method": method, "aug_type": t, "recall_p99": rec, "top1": r["top1_by_type"].get(t)})

    per_type = pd.DataFrame(type_rows)
    per_type.to_csv(out_dir / "per_type.csv", index=False)
    pivot = per_type.pivot(index="aug_type", columns="method", values="recall_p99")

    lines = [
        f"# Baselines, split `{split}`",
        "",
        f"Threshold: precision >= 0.99 ({'taken from val' if op.endswith('_from_val') else 'oracle on this split'}).",
        "FPR = share of hard negatives (other works of the same artist) matched above the threshold.",
        "",
        "| method | muAP | top-1 | recall@P99 | precision | hard-neg FPR @P99 | hard-neg FPR @R80 | AUC copy vs hard-neg | ms/img |",
        "|---|---|---|---|---|---|---|---|---|",
        *rows,
        "",
        "## Recall at precision >= 0.99 by modification type",
        "",
        "| type | " + " | ".join(pivot.columns) + " |",
        "|---|" + "---|" * len(pivot.columns),
        *("| " + t + " | " + " | ".join(_fmt(v) for v in row) + " |" for t, row in pivot.iterrows()),
        "",
    ]
    (out_dir / "summary.md").write_text("\n".join(lines), encoding="utf-8")

    ax = pivot.plot.bar(figsize=(12, 4.5), width=0.8)
    ax.set_ylabel("recall @ precision >= 0.99")
    ax.set_xlabel("")
    ax.set_ylim(0, 1)
    ax.legend(ncol=len(pivot.columns), fontsize=8, loc="upper center", bbox_to_anchor=(0.5, 1.15))
    plt.xticks(rotation=30, ha="right")
    plt.tight_layout()
    plt.savefig(out_dir / "per_type.png", dpi=130)


if __name__ == "__main__":
    main()
