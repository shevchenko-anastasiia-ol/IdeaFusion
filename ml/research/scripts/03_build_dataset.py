"""Build the copy-detection dataset (originals, AugLy modifications, hard negatives).

    uv run python scripts/03_build_dataset.py --config configs/dataset.yaml
    uv run python scripts/03_build_dataset.py --config configs/dataset.yaml --n-originals 60 --name smoke
"""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path

from copydet.dataset import DatasetConfig, build

ROOT = Path(__file__).resolve().parents[1]


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--config", type=Path, default=ROOT / "configs" / "dataset.yaml")
    ap.add_argument("--n-originals", type=int, help="override for quick runs")
    ap.add_argument("--name", help="override dataset name (and output folder)")
    ap.add_argument("--workers", type=int)
    args = ap.parse_args()

    cfg = DatasetConfig.from_yaml(args.config)
    if args.n_originals:
        cfg.n_originals = args.n_originals
        cfg.n_distractors = min(cfg.n_distractors, args.n_originals)
        cfg.same_style_negatives = min(cfg.same_style_negatives, args.n_originals // 5)
    if args.name:
        cfg.name = args.name
        cfg.output_dir = str(Path(cfg.output_dir).parent / args.name)
    if args.workers is not None:
        cfg.workers = args.workers

    # Relative paths in the config are relative to ml/research, wherever the script is run from.
    os.chdir(ROOT)
    summary = build(cfg)
    print(json.dumps(summary, indent=2, ensure_ascii=False, default=str))
    print(f"-> {Path(cfg.output_dir).resolve()}")


if __name__ == "__main__":
    main()
