"""Download source artworks from Hugging Face and unpack them into data/raw/<source>/.

    uv run python scripts/01_download.py artbench-pd                 # ~1.5 GB, 46k public-domain works
    uv run python scripts/01_download.py wikiart --max-shards 5      # ~2.6 GB of 34 GB, has style labels

The original ArtBench server (artbench.eecs.berkeley.edu) returns 404, so the public-domain
ArtBench subset mirrored on Hugging Face is used instead. It keeps only public-domain works,
which matches the licensing requirement for the custom dataset.

Result: data/raw/<source>/images/<artist>/<image_id>.jpg and data/raw/<source>/catalog.parquet
"""

from __future__ import annotations

import argparse
import io
import re
from pathlib import Path

import pandas as pd
import pyarrow.parquet as pq
import requests
from PIL import Image
from tqdm import tqdm

ROOT = Path(__file__).resolve().parents[1]
HF = "https://huggingface.co"

SOURCES = {
    "artbench-pd": {
        "repo": "alfredplpl/artbench-pd-256x256",
        "prefix": "apd",
        "license": "public-domain",
        "artist_col": "artist",
        "style_col": None,
        "keep_cols": ["url"],
    },
    "wikiart": {
        "repo": "huggan/wikiart",
        "prefix": "wka",
        "license": "wikiart-noncommercial-research",
        "artist_col": "artist",
        "style_col": "style",
        "keep_cols": ["genre"],
    },
}


def list_parquet_files(repo: str) -> list[dict]:
    r = requests.get(f"{HF}/api/datasets/{repo}/tree/main/data", timeout=30)
    r.raise_for_status()
    return sorted((f for f in r.json() if f["path"].endswith(".parquet")), key=lambda f: f["path"])


def class_label_names(repo: str) -> dict[str, list[str]]:
    """Names of ClassLabel features (wikiart stores artist/style as ints)."""
    r = requests.get("https://datasets-server.huggingface.co/info", params={"dataset": repo}, timeout=30)
    r.raise_for_status()
    info = next(iter(r.json()["dataset_info"].values()))
    return {k: v["names"] for k, v in info["features"].items() if v.get("_type") == "ClassLabel"}


def download(url: str, dst: Path, size: int) -> None:
    if dst.exists() and dst.stat().st_size == size:
        return
    tmp = dst.with_suffix(".part")
    with requests.get(url, stream=True, timeout=60) as r:
        r.raise_for_status()
        with open(tmp, "wb") as f, tqdm(total=size, unit="B", unit_scale=True, desc=dst.name) as bar:
            for chunk in r.iter_content(1 << 20):
                f.write(chunk)
                bar.update(len(chunk))
    tmp.replace(dst)


def slug(text: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", str(text).lower()).strip("-") or "unknown"


def extract(parquet_path: Path, shard: int, spec: dict, labels: dict, out_dir: Path) -> list[dict]:
    rows = []
    pf = pq.ParquetFile(parquet_path)
    i = 0
    for batch in pf.iter_batches(batch_size=256):
        for rec in batch.to_pylist():
            image_id = f"{spec['prefix']}_{shard:03d}_{i:05d}"
            i += 1
            artist = rec[spec["artist_col"]]
            if spec["artist_col"] in labels:
                artist = labels[spec["artist_col"]][artist]
            style = rec.get(spec["style_col"]) if spec["style_col"] else None
            if spec["style_col"] in labels and style is not None:
                style = labels[spec["style_col"]][style]
            artist = slug(artist)
            if artist in ("unknown-artist", "unknown"):
                continue  # cannot be used for artist-level splits or same-artist negatives
            data = rec["image"]["bytes"]
            dst = out_dir / "images" / artist / f"{image_id}.jpg"
            dst.parent.mkdir(parents=True, exist_ok=True)
            if not dst.exists():
                img = Image.open(io.BytesIO(data))
                if img.format == "JPEG":
                    dst.write_bytes(data)
                else:
                    img.convert("RGB").save(dst, quality=95)
            row = {
                "image_id": image_id,
                "path": str(dst.resolve()),
                "artist": artist,
                "style": style,
                "source": spec["prefix"],
                "license": spec["license"],
            }
            for c in spec["keep_cols"]:
                value = rec.get(c)
                row[c] = labels[c][value] if c in labels and value is not None else value
            rows.append(row)
    return rows


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("source", choices=SOURCES)
    ap.add_argument("--max-shards", type=int, default=None, help="download only the first N parquet shards")
    ap.add_argument("--keep-parquet", action="store_true", help="keep downloaded parquet files after extraction")
    args = ap.parse_args()

    spec = SOURCES[args.source]
    out_dir = ROOT / "data" / "raw" / args.source
    (out_dir / "parquet").mkdir(parents=True, exist_ok=True)

    files = list_parquet_files(spec["repo"])[: args.max_shards]
    total_gb = sum(f["size"] for f in files) / 1e9
    print(f"{spec['repo']}: {len(files)} shard(s), {total_gb:.2f} GB")
    labels = class_label_names(spec["repo"]) if args.source == "wikiart" else {}

    rows = []
    for shard, f in enumerate(files):
        local = out_dir / "parquet" / Path(f["path"]).name
        download(f"{HF}/datasets/{spec['repo']}/resolve/main/{f['path']}", local, f["size"])
        rows += extract(local, shard, spec, labels, out_dir)
        if not args.keep_parquet:
            local.unlink()

    catalog = pd.DataFrame(rows)
    catalog.to_parquet(out_dir / "catalog.parquet", index=False)
    print(f"{len(catalog)} images, {catalog['artist'].nunique()} artists -> {out_dir / 'catalog.parquet'}")


if __name__ == "__main__":
    main()
