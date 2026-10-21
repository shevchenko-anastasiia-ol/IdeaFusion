"""Stage 3, data: SSCD embeddings of training images and their modifications.

    uv run python scripts/06_embed_train.py --dataset data/processed/artbench_v1

For every training image: one clean view + `--views` modified views (trainable types only,
never collage/screenshot), all embedded with frozen SSCD. The head is then trained on these
vectors in minutes, without touching images again.

Training pool (no leakage into val/test, section 1.4.3):
* train originals and train hard negatives of the dataset;
* up to `--extra` more catalog works by artists that are not in val/test, excluding the
  distractors and the background set used for score normalisation.

Output: data/embeddings/<dataset>/train_sscd.npz with
    emb (N, 1 + views, 512) float32, image_id (N,), artist (N,), origin (N,), aug_types (N, 1 + views)
Progress is checkpointed, so an interrupted run resumes.
"""

from __future__ import annotations

import argparse
import os
from pathlib import Path

import numpy as np
import pandas as pd
from tqdm import tqdm

from copydet import augment
from copydet.benchmark import Benchmark
from copydet.descriptors import SSCD
from copydet.imageio import load_rgb

ROOT = Path(__file__).resolve().parents[1]


def training_pool(bench: Benchmark, catalog: pd.DataFrame, extra: int, max_per_artist: int, seed: int) -> pd.DataFrame:
    ds = bench.dataset
    queries = pd.read_parquet(ds / "queries.parquet")
    originals = bench.originals
    eval_artists = set(originals.loc[originals["split"] != "train", "artist"])

    train_orig = originals[originals["split"] == "train"]
    train_neg = queries[(queries["split"] == "train") & queries["is_negative"]]
    pool = [
        pd.DataFrame({"image_id": train_orig["image_id"], "path": [str(ds / p) for p in train_orig["path"]],
                      "artist": train_orig["artist"], "origin": "train_original"}),
        pd.DataFrame({"image_id": train_neg["query_id"], "path": [str(ds / p) for p in train_neg["path"]],
                      "artist": train_neg["related_artist"], "origin": "train_negative"}),
    ]

    excluded = bench.used_ids | set(bench.background["image_id"])
    cand = catalog[~catalog["image_id"].isin(excluded) & ~catalog["artist"].isin(eval_artists)]
    # Several works per artist, so same-artist hard negatives appear in training batches.
    cand = cand.sample(frac=1.0, random_state=seed).groupby("artist").head(max_per_artist)
    cand = cand[cand.groupby("artist")["image_id"].transform("size") >= 2]
    cand = cand.sample(frac=1.0, random_state=seed).head(extra)
    pool.append(pd.DataFrame({"image_id": cand["image_id"], "path": cand["path"], "artist": cand["artist"], "origin": "extra"}))
    return pd.concat(pool, ignore_index=True)


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dataset", type=Path, default=Path("data/processed/artbench_v1"))
    ap.add_argument("--catalog", type=Path, default=Path("data/manifests/catalog.parquet"))
    ap.add_argument("--sscd-weights", type=Path, default=Path("data/weights/sscd_disc_mixup.torchscript.pt"))
    ap.add_argument("--views", type=int, default=5, help="modified views per image (plus one clean view)")
    ap.add_argument("--extra", type=int, default=2000)
    ap.add_argument("--max-per-artist", type=int, default=8)
    ap.add_argument("--seed", type=int, default=7)
    ap.add_argument("--checkpoint-every", type=int, default=250)
    args = ap.parse_args()
    os.chdir(ROOT)

    bench = Benchmark(args.dataset, args.catalog)
    pool = training_pool(bench, pd.read_parquet(args.catalog), args.extra, args.max_per_artist, args.seed)
    print(pool.groupby("origin").size().to_string(), f"\nartists: {pool['artist'].nunique()}")

    out = bench.cache_dir / "train_sscd.npz"
    out.parent.mkdir(parents=True, exist_ok=True)
    n_views = 1 + args.views
    emb = np.zeros((len(pool), n_views, 512), dtype=np.float32)
    aug_types = np.full((len(pool), n_views), "clean", dtype=object)
    done = 0
    if out.exists():
        with np.load(out, allow_pickle=True) as z:
            if z["emb"].shape[1] == n_views and list(z["image_id"]) == list(pool["image_id"][: len(z["image_id"])]):
                done = int(z["done"])
                emb[:done], aug_types[:done] = z["emb"][:done], z["aug_types"][:done]
                print(f"resuming at {done}/{len(pool)}")

    def save(n: int) -> None:
        np.savez(out, emb=emb[:n], aug_types=aug_types[:n].astype(str), image_id=pool["image_id"].to_numpy()[:n].astype(str),
                 artist=pool["artist"].to_numpy()[:n].astype(str), origin=pool["origin"].to_numpy()[:n].astype(str), done=n)

    sscd = SSCD(args.sscd_weights)
    trainable = list(augment.TRAINABLE_TYPES)
    for i in tqdm(range(done, len(pool)), initial=done, total=len(pool), desc="train views"):
        row = pool.iloc[i]
        img = load_rgb(row["path"])
        views = [img]
        for j in range(args.views):
            rng = augment.rng_for(args.seed, f"train:{row['image_id']}:{j}")
            t = trainable[int(rng.integers(len(trainable)))]
            views.append(augment.apply(t, img, rng)[0])
            aug_types[i, j + 1] = t
        emb[i] = sscd.embed_images(views)
        if (i + 1) % args.checkpoint_every == 0:
            save(i + 1)
    save(len(pool))
    print(f"-> {out}  {emb.shape}")


if __name__ == "__main__":
    main()
