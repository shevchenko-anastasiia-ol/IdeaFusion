"""Merge per-source catalogs (and optional own works) into data/manifests/catalog.parquet.

    uv run python scripts/02_build_catalog.py                      # all data/raw/*/catalog.parquet
    uv run python scripts/02_build_catalog.py --custom data/raw/custom

Own works go into `--custom` as <artist>/<file>.(jpg|png|webp); only works whose authors agreed
to their use (chapter 1, table 1.3). An optional <artist>/style.txt sets the style.
"""

from __future__ import annotations

import argparse
from pathlib import Path

import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
IMAGE_EXT = {".jpg", ".jpeg", ".png", ".webp"}


def custom_catalog(directory: Path) -> pd.DataFrame:
    rows = []
    for artist_dir in sorted(p for p in directory.iterdir() if p.is_dir()):
        style_file = artist_dir / "style.txt"
        style = style_file.read_text(encoding="utf-8").strip() if style_file.exists() else None
        for i, f in enumerate(sorted(p for p in artist_dir.iterdir() if p.suffix.lower() in IMAGE_EXT)):
            rows.append(
                {
                    "image_id": f"own_{artist_dir.name}_{i:04d}",
                    "path": str(f.resolve()),
                    "artist": f"own-{artist_dir.name.lower()}",
                    "style": style,
                    "source": "own",
                    "license": "author-consent",
                }
            )
    return pd.DataFrame(rows)


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--custom", type=Path, help="folder with own works: <artist>/<image>")
    ap.add_argument("--out", type=Path, default=ROOT / "data" / "manifests" / "catalog.parquet")
    args = ap.parse_args()

    parts = [pd.read_parquet(p) for p in sorted((ROOT / "data" / "raw").glob("*/catalog.parquet"))]
    if args.custom:
        parts.append(custom_catalog(args.custom))
    if not parts:
        raise SystemExit("No sources found. Run scripts/01_download.py first.")

    cols = ["image_id", "path", "artist", "style", "source", "license"]
    catalog = pd.concat([p[cols] for p in parts], ignore_index=True)
    dup = catalog["image_id"].duplicated()
    if dup.any():
        raise SystemExit(f"Duplicate image_id values: {catalog.loc[dup, 'image_id'].head().tolist()}")
    missing = ~catalog["path"].map(lambda p: Path(p).exists())
    if missing.any():
        print(f"warning: {int(missing.sum())} files missing on disk, dropped")
        catalog = catalog[~missing]

    args.out.parent.mkdir(parents=True, exist_ok=True)
    catalog.to_parquet(args.out, index=False)
    per_artist = catalog.groupby("artist").size()
    print(f"{len(catalog)} images, {len(per_artist)} artists ({int((per_artist >= 4).sum())} with >= 4 works)")
    print(catalog.groupby("source").size().to_string())
    print(f"-> {args.out}")


if __name__ == "__main__":
    main()
