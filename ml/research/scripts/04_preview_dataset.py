"""Grid of originals with all their modifications, plus hard negatives (for eyeballing and the report).

    uv run python scripts/04_preview_dataset.py data/processed/artbench_v1 --rows 6
"""

from __future__ import annotations

import argparse
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import pandas as pd
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("dataset", type=Path)
    ap.add_argument("--rows", type=int, default=6)
    ap.add_argument("--split", default="test")
    ap.add_argument("--seed", type=int, default=0)
    args = ap.parse_args()

    ds = args.dataset if args.dataset.is_absolute() else ROOT / args.dataset
    originals = pd.read_parquet(ds / "originals.parquet")
    queries = pd.read_parquet(ds / "queries.parquet")
    picked = originals[originals["split"] == args.split].sample(args.rows, random_state=args.seed)
    types = sorted(queries.loc[~queries["is_negative"] & (queries["split"] == args.split), "aug_type"].unique())
    negs = queries[queries["is_negative"] & (queries["negative_kind"] == "same_artist")]

    cols = 1 + len(types) + 1
    fig, axes = plt.subplots(args.rows, cols, figsize=(1.6 * cols, 1.7 * args.rows))
    for r, o in enumerate(picked.itertuples()):
        cells = [("original", ds / o.path)]
        mods = queries[queries["original_id"] == o.image_id].set_index("aug_type")
        cells += [(t, ds / mods.loc[t, "path"]) if t in mods.index else (t, None) for t in types]
        neg = negs[negs["related_artist"] == o.artist]
        cells.append(("same artist\n(negative)", ds / neg.iloc[0]["path"] if len(neg) else None))
        for c, (title, path) in enumerate(cells):
            ax = axes[r, c]
            ax.axis("off")
            if path is not None:
                ax.imshow(Image.open(path))
            if r == 0:
                ax.set_title(title, fontsize=7)
    fig.tight_layout()
    out = ROOT / "runs" / f"{ds.name}_preview.png"
    out.parent.mkdir(exist_ok=True)
    fig.savefig(out, dpi=130)
    print(f"-> {out}")


if __name__ == "__main__":
    main()
