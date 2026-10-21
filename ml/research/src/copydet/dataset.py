"""Building the evaluation/training dataset of artwork copies (chapter 1, section 1.4).

Input: a catalog (parquet) of source artworks with columns
    image_id, path, artist, style (nullable), source, license
Output, in `output_dir`:
    originals/<image_id>.jpg       reference works that get "stolen"
    queries/<query_id>.jpg         their modifications + hard negatives (no match in the index)
    distractors/<image_id>.jpg     unrelated works: collage backgrounds, extra index noise
    originals.parquet, queries.parquet, distractors.parquet, dataset_meta.json

Rules from section 1.4.3 enforced here:
* split by artist: an artist's works are all in one of train/val/test;
* held-out modification types (collage, screenshot) appear only in test;
* hard negatives are other works of the same artist (and same style) that must NOT match;
* negatives that are near-duplicates of an original (same work scanned twice) are dropped,
  otherwise they would be counted as false positives while actually being copies.
"""

from __future__ import annotations

import json
import os
import platform
from concurrent.futures import ProcessPoolExecutor
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import pandas as pd
import yaml
from tqdm import tqdm

from copydet import augment
from copydet.hashing import hamming_matrix, phash64_with_mirror
from copydet.imageio import limit_side, load_rgb, save_jpeg

SPLITS = ("train", "val", "test")


@dataclass
class DatasetConfig:
    name: str
    catalog: str
    output_dir: str
    seed: int = 42
    n_originals: int = 1500
    split_ratios: dict[str, float] = field(default_factory=lambda: {"train": 0.6, "val": 0.2, "test": 0.2})
    min_works_per_artist: int = 4
    max_originals_per_artist: int = 5
    hard_negatives_per_artist: int = 5
    same_style_negatives: int = 300  # per val/test split, from artists not used for originals
    n_distractors: int = 1000
    train_queries_per_original: int = 6
    held_out_types: list[str] = field(default_factory=lambda: sorted(augment.HELD_OUT_TYPES))
    max_side: int = 1024
    jpeg_quality: int = 95
    near_dup_hamming: int = 6
    workers: int = 0  # 0 = min(8, cpu_count - 1)

    @classmethod
    def from_yaml(cls, path: str | Path) -> "DatasetConfig":
        with open(path, encoding="utf-8") as f:
            return cls(**yaml.safe_load(f))


# ---------------------------------------------------------------------------
# Selection (pure pandas, no image IO): what becomes an original / negative / distractor
# ---------------------------------------------------------------------------


@dataclass
class Selection:
    originals: pd.DataFrame  # catalog rows + split
    negatives: pd.DataFrame  # catalog rows + split + negative_kind + related_artist
    distractors: pd.DataFrame


def select(catalog: pd.DataFrame, cfg: DatasetConfig) -> Selection:
    rng = np.random.default_rng(cfg.seed)
    catalog = catalog.sort_values("image_id").reset_index(drop=True)

    counts = catalog.groupby("artist").size()
    eligible = counts[counts >= cfg.min_works_per_artist].index.to_numpy()
    eligible = eligible[rng.permutation(len(eligible))]

    ratios = {s: cfg.split_ratios.get(s, 0.0) for s in SPLITS}
    total = sum(ratios.values())
    target = {s: round(cfg.n_originals * r / total) for s, r in ratios.items()}
    filled = dict.fromkeys(SPLITS, 0)

    originals, negatives, used_artists = [], [], set()
    for artist in eligible:
        # Give the artist to the split that is furthest behind its target.
        deficits = {s: target[s] - filled[s] for s in SPLITS if target[s] > filled[s]}
        if not deficits:
            break
        split = max(deficits, key=lambda s: deficits[s] / max(target[s], 1))
        works = catalog[catalog["artist"] == artist]
        works = works.iloc[rng.permutation(len(works))]
        # At least one work of each artist is kept back as a hard negative.
        n_orig = min(cfg.max_originals_per_artist, len(works) - 1, deficits[split])
        if n_orig <= 0:
            continue
        orig = works.iloc[:n_orig].assign(split=split)
        neg = works.iloc[n_orig : n_orig + cfg.hard_negatives_per_artist].assign(
            split=split, negative_kind="same_artist", related_artist=artist
        )
        originals.append(orig)
        negatives.append(neg)
        filled[split] += n_orig
        used_artists.add(artist)

    originals_df = pd.concat(originals, ignore_index=True)
    rest = catalog[~catalog["artist"].isin(used_artists)]
    rest = rest.iloc[rng.permutation(len(rest))]

    # Same style, different (unused) artist: hard for content-based methods, never a copy.
    style_negs, style_taken = [], set()
    if "style" in rest and rest["style"].notna().any():
        for split in ("val", "test"):
            styles = originals_df.loc[originals_df["split"] == split, "style"].dropna().unique()
            pool = rest[rest["style"].isin(styles) & ~rest["image_id"].isin(style_taken)]
            take = pool.iloc[: cfg.same_style_negatives].assign(
                split=split, negative_kind="same_style", related_artist=None
            )
            style_taken.update(take["image_id"])
            style_negs.append(take)
    negatives_df = pd.concat(negatives + style_negs, ignore_index=True)

    taken = set(negatives_df["image_id"])
    distractors_df = rest[~rest["image_id"].isin(taken)].iloc[: cfg.n_distractors].reset_index(drop=True)
    return Selection(originals_df, negatives_df, distractors_df)


# ---------------------------------------------------------------------------
# Query plan: which (original, modification) pairs to generate
# ---------------------------------------------------------------------------


def plan_queries(originals: pd.DataFrame, cfg: DatasetConfig) -> pd.DataFrame:
    held_out = set(cfg.held_out_types)
    trainable = [t for t in augment.ALL_TYPES if t not in held_out]
    rows = []
    for o in originals.itertuples():
        if o.split == "train":
            rng = augment.rng_for(cfg.seed, f"plan:{o.image_id}")
            k = min(cfg.train_queries_per_original, len(trainable))
            types = [str(t) for t in rng.choice(trainable, size=k, replace=False)]
        elif o.split == "val":
            types = trainable
        else:
            types = list(augment.ALL_TYPES)
        for t in types:
            rows.append({"query_id": f"{o.image_id}__{t}", "original_id": o.image_id, "split": o.split, "aug_type": t})
    return pd.DataFrame(rows)


# ---------------------------------------------------------------------------
# Image IO (parallel)
# ---------------------------------------------------------------------------

_WORKER: dict = {}


def _init_worker(out_dir: str, distractor_paths: list[str], seed: int, quality: int) -> None:
    _WORKER.update(
        out_dir=Path(out_dir),
        ctx=augment.AugContext([Path(p) for p in distractor_paths]),
        seed=seed,
        quality=quality,
    )


def _copy_image(args: tuple[str, str, str, int, int]) -> dict:
    """Normalise a source image (RGB, max side) into the dataset and hash it."""
    image_id, src, dst, max_side, quality = args
    img = limit_side(load_rgb(src), max_side)
    save_jpeg(img, dst, quality)
    h, hm = phash64_with_mirror(img)
    return {"image_id": image_id, "width": img.width, "height": img.height, "phash": h, "phash_mirror": hm}


def _make_query(row: dict) -> dict:
    out_dir: Path = _WORKER["out_dir"]
    img = load_rgb(out_dir / "originals" / f"{row['original_id']}.jpg")
    rng = augment.rng_for(_WORKER["seed"], row["query_id"])
    try:
        out, params = augment.apply(row["aug_type"], img, rng, _WORKER["ctx"])
    except Exception as exc:  # one broken image must not kill a 20k-query run
        return {**row, "path": None, "params": None, "error": repr(exc)}
    rel = f"queries/{row['query_id']}.jpg"
    save_jpeg(out, out_dir / rel, _WORKER["quality"])
    return {**row, "path": rel, "params": json.dumps(params), "width": out.width, "height": out.height, "error": None}


def _pmap(fn, items: list, workers: int, desc: str, initializer=None, initargs=()) -> list:
    if workers <= 1:
        if initializer:
            initializer(*initargs)
        return [fn(x) for x in tqdm(items, desc=desc)]
    with ProcessPoolExecutor(workers, initializer=initializer, initargs=initargs) as ex:
        return list(tqdm(ex.map(fn, items, chunksize=16), total=len(items), desc=desc))


# ---------------------------------------------------------------------------
# Orchestration
# ---------------------------------------------------------------------------


def _copy_set(df: pd.DataFrame, subdir: str, out: Path, cfg: DatasetConfig, workers: int) -> pd.DataFrame:
    jobs = [
        (r.image_id, str(r.path), str(out / subdir / f"{r.image_id}.jpg"), cfg.max_side, cfg.jpeg_quality)
        for r in df.itertuples()
    ]
    info = pd.DataFrame(_pmap(_copy_image, jobs, workers, f"copy {subdir}"))
    df = df.rename(columns={"path": "src_path"}).merge(info, on="image_id")
    df["path"] = subdir + "/" + df["image_id"] + ".jpg"
    return df


def _drop_near_duplicates(originals: pd.DataFrame, negatives: pd.DataFrame, max_dist: int):
    """Drop originals duplicating each other, and negatives duplicating any original."""
    oh = originals["phash"].to_numpy()
    d = hamming_matrix(oh, oh)
    np.fill_diagonal(d, 64)
    dup_orig = np.triu(d <= max_dist).any(axis=0)  # keep the first of each duplicate pair
    originals = originals[~dup_orig]

    oh = originals["phash"].to_numpy()
    d_neg = np.minimum(
        hamming_matrix(negatives["phash"].to_numpy(), oh),
        hamming_matrix(negatives["phash_mirror"].to_numpy(), oh),
    )
    dup_neg = (d_neg <= max_dist).any(axis=1)
    return originals, negatives[~dup_neg], int(dup_orig.sum()), int(dup_neg.sum())


def build(cfg: DatasetConfig, catalog: pd.DataFrame | None = None) -> dict:
    out = Path(cfg.output_dir)
    out.mkdir(parents=True, exist_ok=True)
    # Capped at 8: a pool of 17 workers on an 18-core Windows machine lost its workers and hung.
    workers = cfg.workers or min(8, max(1, (os.cpu_count() or 2) - 1))
    if catalog is None:
        catalog = pd.read_parquet(cfg.catalog)

    sel = select(catalog, cfg)
    originals = _copy_set(sel.originals, "originals", out, cfg, workers)
    negatives = _copy_set(sel.negatives, "queries", out, cfg, workers)
    distractors = _copy_set(sel.distractors, "distractors", out, cfg, workers)
    originals, negatives, n_dup_orig, n_dup_neg = _drop_near_duplicates(originals, negatives, cfg.near_dup_hamming)

    plan = plan_queries(originals, cfg)
    results = _pmap(
        _make_query,
        plan.to_dict("records"),
        workers,
        "modifications",
        initializer=_init_worker,
        initargs=(str(out), [str(out / p) for p in distractors["path"]], cfg.seed, cfg.jpeg_quality),
    )
    queries = pd.DataFrame(results)
    failed = queries[queries["error"].notna()]
    queries = queries[queries["error"].isna()].drop(columns="error")
    queries["is_negative"] = False
    queries["negative_kind"] = None
    queries["related_artist"] = queries["original_id"].map(originals.set_index("image_id")["artist"])

    neg_queries = pd.DataFrame(
        {
            "query_id": negatives["image_id"],
            "original_id": None,
            "split": negatives["split"],
            "aug_type": "none",
            "path": negatives["path"],
            "params": None,
            "width": negatives["width"],
            "height": negatives["height"],
            "is_negative": True,
            "negative_kind": negatives["negative_kind"],
            "related_artist": negatives["related_artist"],
        }
    )
    queries = pd.concat([queries, neg_queries], ignore_index=True)

    originals.to_parquet(out / "originals.parquet", index=False)
    queries.to_parquet(out / "queries.parquet", index=False)
    distractors.to_parquet(out / "distractors.parquet", index=False)
    if len(failed):
        failed.to_csv(out / "failed_queries.csv", index=False)

    summary = {
        "originals_per_split": originals["split"].value_counts().to_dict(),
        "artists_per_split": originals.groupby("split")["artist"].nunique().to_dict(),
        "queries_per_split_type": queries.groupby(["split", "aug_type"]).size().unstack(fill_value=0).to_dict("index"),
        "negatives_per_split_kind": negatives.groupby(["split", "negative_kind"]).size().unstack(fill_value=0).to_dict("index"),
        "distractors": len(distractors),
        "dropped_duplicate_originals": n_dup_orig,
        "dropped_duplicate_negatives": n_dup_neg,
        "failed_queries": len(failed),
    }
    meta = {
        "config": asdict(cfg),
        "created_at": datetime.now(timezone.utc).isoformat(),
        "python": platform.python_version(),
        "augly_types": list(augment.ALL_TYPES),
        "summary": summary,
    }
    (out / "dataset_meta.json").write_text(json.dumps(meta, indent=2, ensure_ascii=False, default=str), encoding="utf-8")
    _check_invariants(originals, queries, set(cfg.held_out_types))
    return summary


def _check_invariants(originals: pd.DataFrame, queries: pd.DataFrame, held_out_types: set[str]) -> None:
    """Fail loudly if the split rules of section 1.4.3 are violated."""
    artist_splits = originals.groupby("artist")["split"].nunique()
    assert (artist_splits == 1).all(), "artist leaked across splits"
    neg_artists = queries.loc[queries["negative_kind"] == "same_artist"].groupby("related_artist")["split"].unique()
    orig_split = originals.groupby("artist")["split"].first()
    for artist, splits in neg_artists.items():
        assert set(splits) == {orig_split[artist]}, f"negatives of {artist} in another split"
    held = queries[queries["aug_type"].isin(held_out_types)]
    assert (held["split"] == "test").all(), "held-out modification outside test"
