"""Stage 4, data: features of (query, candidate) pairs for signal fusion.

    uv run python scripts/08_pair_features.py --dataset data/processed/artbench_v1

Candidates = top-K of SSCD+norm (the chosen descriptor). For each pair:
    sscd_raw, sscd_norm, sscd_gap (query's top-1 minus top-2 raw similarity), rank
    phash_dist (min over the mirrored reference hash)
    clip_raw, clip_norm (semantic, informational)
    kp_* (SIFT + RANSAC: inliers, inlier ratio, grid coverage incl. the centre of the reference)
    label = candidate is the query's original

Splits: train (fit the fusion model), val (calibration / thresholds), test (held out).
Output: runs/fusion_<dataset>/pairs.parquet
"""

from __future__ import annotations

import argparse
import os
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

import numpy as np
import pandas as pd
from tqdm import tqdm

from copydet import descriptors as D
from copydet import keypoints as K
from copydet.benchmark import Benchmark
from copydet.evaluation import normalize_with_background, search_cosine
from copydet.hashing import hamming_matrix
from copydet.imageio import load_rgb

ROOT = Path(__file__).resolve().parents[1]

_REF_CACHE: dict[str, K.Features] = {}
_REF_CACHE_MAX = 600


def _ref_features(path: str) -> K.Features:
    feats = _REF_CACHE.get(path)
    if feats is None:
        if len(_REF_CACHE) >= _REF_CACHE_MAX:
            _REF_CACHE.pop(next(iter(_REF_CACHE)))
        feats = _REF_CACHE[path] = K.extract(load_rgb(path))
    return feats


def _kp_job(job: tuple[str, list[str]]) -> list[dict]:
    query_path, ref_paths = job
    qf = K.extract_with_mirror(load_rgb(query_path))
    return [K.match(qf, _ref_features(p)) for p in ref_paths]


def kth_background(q: np.ndarray, bg: np.ndarray, k: int = 1) -> np.ndarray:
    sim = q @ bg.T
    return -np.partition(-sim, k - 1, axis=1)[:, k - 1]


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dataset", type=Path, default=Path("data/processed/artbench_v1"))
    ap.add_argument("--catalog", type=Path, default=Path("data/manifests/catalog.parquet"))
    ap.add_argument("--sscd-weights", type=Path, default=Path("data/weights/sscd_disc_mixup.torchscript.pt"))
    ap.add_argument("--splits", nargs="+", default=["train", "val", "test"])
    ap.add_argument("--k", type=int, default=5)
    ap.add_argument("--workers", type=int, default=8)
    args = ap.parse_args()
    os.chdir(ROOT)

    bench = Benchmark(args.dataset, args.catalog, splits=tuple(args.splits))
    sscd, sscd_bg = bench.embed(D.SSCD(args.sscd_weights))
    clip, clip_bg = bench.embed(D.CLIP())
    phash, _ = bench.embed(D.PHash(), with_background=False)

    frames = []
    for split in args.splits:
        view = bench.split(split)
        q_s, r_s = sscd[view.query_rows], sscd[view.ref_rows]
        retr = search_cosine(q_s, r_s, args.k)
        bg_s = kth_background(q_s, sscd_bg)
        norm = normalize_with_background(retr, q_s, sscd_bg)
        q_c, r_c = clip[view.query_rows], clip[view.ref_rows]
        bg_c = kth_background(q_c, clip_bg)
        q_h, r_h = phash[view.query_rows], phash[view.ref_rows]

        n, k = retr.ref_idx.shape
        qi = np.repeat(np.arange(n), k)
        ri = retr.ref_idx.ravel()
        clip_raw = np.einsum("nd,nd->n", q_c[qi], r_c[ri])
        ham = np.minimum(
            np.bitwise_count((q_h[qi, 0] ^ r_h[ri, 0]).view(np.uint64)),
            np.bitwise_count((q_h[qi, 0] ^ r_h[ri, 1]).view(np.uint64)),
        )
        q = view.queries
        frame = pd.DataFrame(
            {
                "split": split,
                "query_id": q["query_id"].to_numpy()[qi],
                "aug_type": q["aug_type"].to_numpy()[qi],
                "is_negative": q["is_negative"].to_numpy()[qi],
                "negative_kind": q["negative_kind"].to_numpy()[qi],
                "query_path": q["path"].to_numpy()[qi],
                "ref_id": view.refs["image_id"].to_numpy()[ri],
                "ref_path": view.refs["path"].to_numpy()[ri],
                "ref_kind": view.refs["kind"].to_numpy()[ri],
                "rank": np.tile(np.arange(k), n),
                "label": (ri == np.repeat(view.gt, k)),
                "sscd_raw": retr.scores.ravel(),
                "sscd_norm": norm.scores.ravel(),
                "sscd_bg": np.repeat(bg_s, k),
                "sscd_gap": np.repeat(retr.scores[:, 0] - retr.scores[:, 1], k),
                "phash_dist": ham.astype(np.int16),
                "clip_raw": clip_raw,
                "clip_norm": clip_raw - np.repeat(bg_c, k),
            }
        )

        jobs = [
            (str(bench.dataset / qp), [str(bench.dataset / p) for p in grp["ref_path"]])
            for qp, grp in frame.groupby("query_path", sort=False)
        ]
        # Grouping queries by their top-1 reference makes the per-worker reference cache hit.
        order = sorted(range(len(jobs)), key=lambda i: jobs[i][1][0])
        with ProcessPoolExecutor(args.workers) as ex:
            results = list(tqdm(ex.map(_kp_job, [jobs[i] for i in order], chunksize=8), total=len(jobs), desc=f"keypoints {split}"))
        kp_by_query = {jobs[i][0]: r for i, r in zip(order, results)}
        kp_rows = [kp_by_query[str(bench.dataset / qp)][j] for qp, j in zip(frame["query_path"], frame["rank"])]
        frames.append(pd.concat([frame.reset_index(drop=True), pd.DataFrame(kp_rows)], axis=1))
        print(f"{split}: {n} queries, {len(frame)} pairs, positives {int(frame['label'].sum())}")

    out_dir = ROOT / "runs" / f"fusion_{bench.dataset.name}"
    out_dir.mkdir(parents=True, exist_ok=True)
    pairs = pd.concat(frames, ignore_index=True)
    pairs.to_parquet(out_dir / "pairs.parquet", index=False)
    print(f"-> {out_dir / 'pairs.parquet'}  {pairs.shape}")


if __name__ == "__main__":
    main()
