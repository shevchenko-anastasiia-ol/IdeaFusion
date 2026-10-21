"""The benchmark around a built dataset: which images form the index and the queries of each
split, the background set for score normalisation, and running `evaluation.evaluate` on any
embeddings. Baselines and trained heads go through this same code path.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import pandas as pd

from copydet import descriptors as D
from copydet.evaluation import Retrieval, evaluate, normalize_with_background, search_cosine, search_phash


def collage_backgrounds(queries: pd.DataFrame) -> set[str]:
    """Distractors pasted under a collage. A collage really contains a copy of its background,
    so matching it is not an error; they are kept out of that split's index instead."""
    collages = queries.loc[queries["aug_type"] == "collage", "params"].dropna()
    return {json.loads(p)["background"] for p in collages}


@dataclass
class SplitView:
    queries: pd.DataFrame
    refs: pd.DataFrame
    gt: np.ndarray  # index into refs of each query's original, -1 for negatives
    query_rows: np.ndarray  # rows of Benchmark.image_keys
    ref_rows: np.ndarray


class Benchmark:
    def __init__(
        self,
        dataset: Path,
        catalog: Path,
        splits: tuple[str, ...] = ("val", "test"),
        bg_size: int = 2000,
        seed: int = 0,
    ):
        self.dataset = Path(dataset)
        self.originals = pd.read_parquet(self.dataset / "originals.parquet")
        queries = pd.read_parquet(self.dataset / "queries.parquet")
        self.distractors = pd.read_parquet(self.dataset / "distractors.parquet")
        # Hard negatives keep their catalog image_id as query_id, so this covers them too.
        self.used_ids = set(self.originals["image_id"]) | set(queries["query_id"]) | set(self.distractors["image_id"])
        self.all_queries = queries[queries["split"].isin(splits)].reset_index(drop=True)
        cat = pd.read_parquet(catalog)
        cat = cat[~cat["image_id"].isin(self.used_ids)].sort_values("image_id")
        self.background = cat.sample(min(bg_size, len(cat)), random_state=seed)

        self.ref_table = pd.concat(
            [self.originals.assign(kind="original"), self.distractors.assign(kind="distractor", split=None)],
            ignore_index=True,
        )
        paths = list(self.ref_table["path"]) + list(self.all_queries["path"])
        self.image_keys = list(dict.fromkeys("ds:" + p for p in paths))
        self.key_pos = {k: i for i, k in enumerate(self.image_keys)}
        self.bg_keys = ["cat:" + i for i in self.background["image_id"]]

    @property
    def cache_dir(self) -> Path:
        return self.dataset.parents[1] / "embeddings" / self.dataset.name

    def image_paths(self) -> list[Path]:
        return [self.dataset / k[3:] for k in self.image_keys]

    def bg_paths(self) -> list[Path]:
        return [Path(p) for p in self.background["path"]]

    def embed(self, desc: D.Descriptor, with_background: bool = True) -> tuple[np.ndarray, np.ndarray | None]:
        emb = D.embed_cached(desc, self.image_keys, self.image_paths(), self.cache_dir)
        bg = D.embed_cached(desc, self.bg_keys, self.bg_paths(), self.cache_dir) if with_background else None
        return emb, bg

    def split(self, name: str) -> SplitView:
        q = self.all_queries[self.all_queries["split"] == name].reset_index(drop=True)
        t = self.ref_table
        refs = t[((t["kind"] == "distractor") & ~t["image_id"].isin(collage_backgrounds(q))) | (t["split"] == name)]
        refs = refs.reset_index(drop=True)
        ref_index = {img_id: i for i, img_id in enumerate(refs["image_id"])}
        gt = q["original_id"].map(ref_index).fillna(-1).astype(int).to_numpy()
        return SplitView(
            queries=q,
            refs=refs,
            gt=gt,
            query_rows=np.array([self.key_pos["ds:" + p] for p in q["path"]]),
            ref_rows=np.array([self.key_pos["ds:" + p] for p in refs["path"]]),
        )

    def run(
        self,
        view: SplitView,
        emb: np.ndarray,
        bg: np.ndarray | None = None,
        kind: str = "cosine",
        normalize: bool = False,
        k: int = 10,
        transfer_thresholds: dict | None = None,
    ) -> tuple[dict, Retrieval]:
        q_emb, r_emb = emb[view.query_rows], emb[view.ref_rows]
        if kind == "phash":
            retr = search_phash(q_emb, r_emb, k)
        else:
            retr = search_cosine(q_emb, r_emb, k)
            if normalize:
                retr = normalize_with_background(retr, q_emb, bg)
        res = evaluate(retr, view.gt, view.queries, transfer_thresholds=transfer_thresholds)
        res["index_size"] = len(view.refs)
        return res, retr


def thresholds_of(result: dict) -> dict:
    return {name: op["threshold"] for name, op in result["operating_points"].items() if not name.endswith("_from_val")}
