"""Latency of one copy check on CPU (requirement B3: seconds, with 100k+ published works).

    py -m uv run python benchmarks/latency.py --queries 60

Index: the test-split originals and distractors from the research cache (no recomputation).
Queries: test queries of every modification type and hard negatives. The deployed detector
is timed stage by stage. The exact (brute-force) vector search is also timed on 100k random
vectors, as an upper bound for pgvector's HNSW index, which is approximate and faster.
"""

from __future__ import annotations

import argparse
import time
from pathlib import Path

import numpy as np
import pandas as pd

from app.detector import CopyDetector
from app.store import Candidate, Fingerprint, MemoryStore
from copydet.imageio import load_rgb

ROOT = Path(__file__).resolve().parents[2]
RESEARCH = ROOT / "research"
DATASET = RESEARCH / "data" / "processed" / "artbench_v1"
CACHE = RESEARCH / "data" / "embeddings" / "artbench_v1"


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--queries", type=int, default=60)
    ap.add_argument("--threads", type=int, default=4)
    args = ap.parse_args()

    detector = CopyDetector(ROOT / "models" / "v1", torch_threads=args.threads)
    with np.load(CACHE / "sscd.npz") as z:
        sscd = dict(zip(z["keys"].tolist(), z["emb"]))
    with np.load(CACHE / "phash.npz") as z:
        phash = dict(zip(z["keys"].tolist(), z["emb"]))

    originals = pd.read_parquet(DATASET / "originals.parquet")
    distractors = pd.read_parquet(DATASET / "distractors.parquet")
    refs = pd.concat([originals[originals["split"] == "test"], distractors], ignore_index=True)
    store = MemoryStore()
    for i, r in enumerate(refs.itertuples()):
        key = "ds:" + r.path
        store.upsert(Fingerprint(i, i, f"u{i}", None, "ds", r.path, int(phash[key][0]), int(phash[key][1]), sscd[key]))

    queries = pd.read_parquet(DATASET / "queries.parquet")
    q = queries[queries["split"] == "test"].groupby("aug_type").head(max(1, args.queries // 13)).head(args.queries)
    load_ref = lambda c: load_rgb(DATASET / c.object_name)

    rows = []
    for r in q.itertuples():
        img = load_rgb(DATASET / r.path)
        t0 = time.perf_counter()
        qa = detector.analyze(img)
        t1 = time.perf_counter()
        cands = store.search(qa.embedding, detector.k)
        t2 = time.perf_counter()
        _, timings = detector.score(qa, cands, load_ref)
        t3 = time.perf_counter()
        rows.append({"type": r.aug_type, "descriptor": (t1 - t0) * 1e3, "search": (t2 - t1) * 1e3,
                     "verify_and_fuse": (t3 - t2) * 1e3, "total": (t3 - t0) * 1e3})
    df = pd.DataFrame(rows)

    rng = np.random.default_rng(0)
    big = rng.normal(size=(100_000, 512)).astype(np.float32)
    big /= np.linalg.norm(big, axis=1, keepdims=True)
    t = []
    for _ in range(20):
        v = big[rng.integers(len(big))]
        t0 = time.perf_counter()
        np.argpartition(-(big @ v), detector.k)[: detector.k]
        t.append((time.perf_counter() - t0) * 1e3)

    stats = df[["descriptor", "search", "verify_and_fuse", "total"]].describe(percentiles=[0.5, 0.95]).loc[["50%", "95%", "max"]]
    lines = [
        "# Latency of one copy check (CPU)",
        "",
        f"{len(df)} test queries (all modification types and hard negatives), index of {store.count()} works,",
        f"top-{detector.k} candidates verified with SIFT + RANSAC, torch threads = {args.threads}. Milliseconds.",
        "",
        "| | " + " | ".join(stats.columns) + " |",
        "|---|" + "---|" * len(stats.columns),
        *(f"| {i} | " + " | ".join(f"{v:.0f}" for v in row) + " |" for i, row in stats.iterrows()),
        "",
        f"Exact search over 100 000 vectors (numpy, upper bound for pgvector HNSW): median {np.median(t):.1f} ms, "
        f"max {max(t):.1f} ms.",
        "",
        "Reference images are read from local disk here; from MinIO add the transfer of five images.",
        "",
    ]
    out = Path(__file__).resolve().parent / "latency.md"
    out.write_text("\n".join(lines), encoding="utf-8")
    print("\n".join(lines))


if __name__ == "__main__":
    main()
