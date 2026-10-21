"""Stage 5: export the deployable model to ml/models/<version>/ for the service.

    uv run python scripts/10_export_model.py --version v1

Writes:
    sscd_disc_mixup.torchscript.pt   copied weights (git-ignored; ml/models/download_weights.py fetches them)
    background_sscd.npy              background set for SSCD score normalisation
    background_clip.npy              only if the fusion model uses CLIP features
    fusion_model.joblib              calibrated fusion model (copydet.fusion.FusionModel)
    thresholds.json                  risk zones, match-type rules, number of candidates
    manifest.json                    version, data, sha256 of every file, test metrics
    MODEL_CARD.md                    what the model is, how it was evaluated, its limits
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
from datetime import datetime, timezone
from pathlib import Path

import joblib
import numpy as np

from copydet.benchmark import Benchmark
from copydet.reporting import fmt

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT.parent / "models"
SSCD_URL = "https://dl.fbaipublicfiles.com/sscd-copy-detection/sscd_disc_mixup.torchscript.pt"


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def cached_background(bench: Benchmark, name: str) -> np.ndarray:
    with np.load(bench.cache_dir / f"{name}.npz") as z:
        cache = dict(zip(z["keys"].tolist(), z["emb"]))
    return np.stack([cache[k] for k in bench.bg_keys]).astype(np.float32)


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--version", default="v1")
    ap.add_argument("--dataset", type=Path, default=Path("data/processed/artbench_v1"))
    ap.add_argument("--catalog", type=Path, default=Path("data/manifests/catalog.parquet"))
    ap.add_argument("--sscd-weights", type=Path, default=Path("data/weights/sscd_disc_mixup.torchscript.pt"))
    args = ap.parse_args()
    os.chdir(ROOT)

    out = MODELS / args.version
    out.mkdir(parents=True, exist_ok=True)
    fusion_dir = ROOT / "runs" / f"fusion_{args.dataset.name}"
    bench = Benchmark(args.dataset, args.catalog)

    shutil.copy2(args.sscd_weights, out / args.sscd_weights.name)
    np.save(out / "background_sscd.npy", cached_background(bench, "sscd"))
    model = joblib.load(fusion_dir / "fusion_model.joblib")
    uses_clip = any(f.startswith("clip_") for f in model.features)
    if uses_clip:
        np.save(out / "background_clip.npy", cached_background(bench, "clip_vitb32"))
    shutil.copy2(fusion_dir / "fusion_model.joblib", out / "fusion_model.joblib")
    thresholds = json.loads((fusion_dir / "thresholds.json").read_text(encoding="utf-8"))
    (out / "thresholds.json").write_text(json.dumps(thresholds, indent=2), encoding="utf-8")

    metrics = json.loads((fusion_dir / "metrics.json").read_text(encoding="utf-8"))
    chosen = metrics[thresholds["model"]]["test"]
    base = metrics["sscd+norm (no model)"]["test"]
    op = lambda r, n: r["operating_points"].get(f"{n}_from_val", r["operating_points"][n])
    summary = {
        "muap": chosen["muap"],
        "recall_at_fpr1_val_threshold": op(chosen, "fpr1")["recall"],
        "hard_negative_fpr_at_that_threshold": op(chosen, "fpr1")["hard_negative_fpr"].get("all"),
        "recall_at_fpr1_oracle": chosen["operating_points"]["fpr1"]["recall"],
        "auc_copy_vs_hard_negative": chosen["auc_copy_vs_hard_negative"],
        "recall_by_type_at_fpr1": op(chosen, "fpr1")["recall_by_type"],
    }

    files = sorted(p.name for p in out.iterdir() if p.is_file() and p.name not in ("manifest.json", "MODEL_CARD.md"))
    manifest = {
        "version": args.version,
        "created_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "dataset": args.dataset.name,
        "sscd_weights": args.sscd_weights.name,
        "sscd_weights_url": SSCD_URL,
        "fusion_model": thresholds["model"],
        "features": model.features,
        "uses_clip": uses_clip,
        "background_size": int(len(bench.bg_keys)),
        "files": {name: sha256(out / name) for name in files},
        "test_metrics": summary,
    }
    (out / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")
    (out / "MODEL_CARD.md").write_text(model_card(manifest, thresholds, chosen, base), encoding="utf-8")
    print(f"-> {out}")
    for name in files:
        print(f"   {name:36s} {(out / name).stat().st_size / 1e6:8.2f} MB")


def model_card(manifest: dict, thresholds: dict, chosen: dict, base: dict) -> str:
    op = lambda r, n: r["operating_points"].get(f"{n}_from_val", r["operating_points"][n])
    types = sorted(op(chosen, "fpr1")["recall_by_type"])
    zones = thresholds["zones"]
    return "\n".join([
        f"# Image copy detection model `{manifest['version']}`",
        "",
        "Finds copies of artworks already published on IdeaFusion (re-uploads, modified copies,",
        "fragments, collages, screenshots) and tells them apart from other works by the same artist.",
        "Output is advice for the author and the moderator; there are no automatic sanctions.",
        "",
        "## Pipeline",
        "",
        "1. SSCD (`sscd_disc_mixup`, Pizzi et al. 2022) 512-d descriptor, score normalised by a",
        f"   background set of {manifest['background_size']} works; top-{thresholds['candidates_k']} candidates from the index.",
        "2. For each candidate: pHash distance (with mirror), SIFT + RANSAC similarity transform",
        "   (inliers, where they lie: a shared frame matches only on the border).",
        f"3. Fusion `{manifest['fusion_model']}` fitted on train pairs, isotonic calibration on val:",
        "   P(copy) for each candidate.",
        f"4. Risk zone: red if P ≥ {fmt(zones.get('red'))} (≤1% of same-artist works reach it on val),",
        f"   yellow if P ≥ {fmt(zones.get('yellow'))} (≤10%), else green. Match type from pHash and the",
        "   transform scale: exact, modified, fragment, partial (collage/screenshot), similar composition.",
        "",
        "## Data",
        "",
        f"`{manifest['dataset']}`: public-domain ArtBench-10 works, split by artist into train/val/test;",
        "12 AugLy modification types (collage and screenshot only in test); hard negatives = other",
        "works of the same artist. See ml/research/README.md.",
        "",
        "## Test results (thresholds set on val)",
        "",
        "| | SSCD+norm alone | deployed model |",
        "|---|---|---|",
        f"| muAP | {fmt(base['muap'])} | {fmt(chosen['muap'])} |",
        f"| recall at ≤1% false accusations (val threshold) | {fmt(op(base, 'fpr1')['recall'])} | {fmt(op(chosen, 'fpr1')['recall'])} |",
        f"| actual same-artist false-match rate there | {fmt(op(base, 'fpr1')['hard_negative_fpr'].get('all'))} | {fmt(op(chosen, 'fpr1')['hard_negative_fpr'].get('all'))} |",
        f"| recall at ≤1% false accusations (oracle) | {fmt(base['operating_points']['fpr1']['recall'])} | {fmt(chosen['operating_points']['fpr1']['recall'])} |",
        f"| AUC copy vs same-artist work | {fmt(base['auc_copy_vs_hard_negative'])} | {fmt(chosen['auc_copy_vs_hard_negative'])} |",
        "",
        "Recall by modification type (val threshold):",
        "",
        "| type | recall |",
        "|---|---|",
        *(f"| {t} | {fmt(op(chosen, 'fpr1')['recall_by_type'][t])} |" for t in types),
        "",
        "## Limits",
        "",
        "* Evaluated on 256 px public-domain paintings and prints; modern digital illustration,",
        "  photography and 3D renders were not in the data.",
        "* Thresholds were set on val; the false-match rate on new data can drift (it did between",
        "  val and test). Re-check the zones on platform data before relying on them.",
        "* \"Similar composition\" is informational: a redrawn work is not a copy, a person decides.",
        "* CPU latency: ~0.1 s for the descriptor plus ~0.1 s per keypoint-verified candidate.",
        "",
    ])


if __name__ == "__main__":
    main()
