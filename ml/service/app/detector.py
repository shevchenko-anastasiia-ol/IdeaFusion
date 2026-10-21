"""The copy-detection pipeline as deployed: descriptor -> candidates -> pair features -> fusion.

Pair features are computed exactly as in research/scripts/08_pair_features.py, the data the
fusion model was fitted on; `tests/test_parity.py` checks this against the research code.
"""

from __future__ import annotations

import io
import json
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable, Sequence

import cv2
import joblib
import numpy as np
import pandas as pd
import torch
from PIL import Image, ImageDraw

from copydet import keypoints as K
from copydet.descriptors import SSCD
from copydet.fusion import add_derived, match_type, risk_zone
from copydet.hashing import phash64_with_mirror

from app.store import Candidate, Fingerprint


@dataclass
class QueryAnalysis:
    image: Image.Image
    embedding: np.ndarray
    sscd_bg: float
    phash: int
    phash_mirror: int
    clip: np.ndarray | None = None
    clip_bg: float | None = None
    _features: tuple | None = field(default=None, repr=False)

    def keypoints(self) -> tuple[K.Features, K.Features]:
        if self._features is None:
            self._features = K.extract_with_mirror(self.image)
        return self._features


class CopyDetector:
    def __init__(self, model_dir: Path, torch_threads: int = 4):
        torch.set_num_threads(torch_threads)
        self.model_dir = Path(model_dir)
        self.manifest = json.loads((self.model_dir / "manifest.json").read_text(encoding="utf-8"))
        self.config = json.loads((self.model_dir / "thresholds.json").read_text(encoding="utf-8"))
        self.version = self.manifest["version"]
        self.sscd = SSCD(self.model_dir / self.manifest["sscd_weights"])
        self.sscd_background = np.load(self.model_dir / "background_sscd.npy")
        self.fusion = joblib.load(self.model_dir / "fusion_model.joblib")
        self.k = int(self.config["candidates_k"])
        self.uses_clip = any(f.startswith("clip_") for f in self.fusion.features)
        self.clip = None
        if self.uses_clip:
            from copydet.descriptors import CLIP

            self.clip = CLIP()
            self.clip_background = np.load(self.model_dir / "background_clip.npy")

    # -- per-image -------------------------------------------------------------------

    def analyze(self, image: Image.Image) -> QueryAnalysis:
        emb = self.sscd.embed_images([image])[0]
        h, hm = phash64_with_mirror(image)
        qa = QueryAnalysis(image, emb, float((self.sscd_background @ emb).max()), h, hm)
        if self.clip is not None:
            c = self.clip.embed_images([image])[0]
            qa.clip, qa.clip_bg = c, float((self.clip_background @ c).max())
        return qa

    def fingerprint(self, qa: QueryAnalysis, **meta) -> Fingerprint:
        return Fingerprint(phash=qa.phash, phash_mirror=qa.phash_mirror, embedding=qa.embedding, clip=qa.clip, **meta)

    # -- matching --------------------------------------------------------------------

    def score(
        self,
        qa: QueryAnalysis,
        candidates: Sequence[Candidate],
        load_reference: Callable[[Candidate], Image.Image],
    ) -> tuple[list[dict], dict]:
        """Fused, calibrated matches for the candidates (sorted by P(copy)) and timings in ms."""
        timings = {}
        if not candidates:
            return [], timings
        t0 = time.perf_counter()
        sims = np.array([c.similarity for c in candidates])
        second = sims[1] if len(sims) > 1 else qa.sscd_bg
        rows, details = [], []
        for rank, cand in enumerate(candidates):
            ref = load_reference(cand)
            kp = K.match(qa.keypoints(), K.extract(ref), with_points=True)
            details.append((ref, kp))
            ham = min(_hamming(qa.phash, cand.phash), _hamming(qa.phash, cand.phash_mirror))
            row = {
                "sscd_raw": cand.similarity,
                "sscd_norm": cand.similarity - qa.sscd_bg,
                "sscd_gap": float(sims[0] - second),
                "rank": rank,
                "phash_dist": ham,
                **{k: v for k, v in kp.items() if not k.startswith("_")},
            }
            if self.uses_clip and cand.clip is not None:
                row["clip_raw"] = float(cand.clip @ qa.clip)
                row["clip_norm"] = row["clip_raw"] - qa.clip_bg
            rows.append(row)
        timings["keypoints_ms"] = (time.perf_counter() - t0) * 1000

        frame = add_derived(pd.DataFrame(rows))
        if self.uses_clip:
            frame = frame.fillna({"clip_raw": 0.0, "clip_norm": 0.0})
        p = self.fusion.predict(frame)
        thresholds = self.config["zones"]
        zones = {k: (v if v is not None else float("inf")) for k, v in thresholds.items()}
        clip_similar = self.config.get("clip_similar_composition", float("inf"))
        matches = []
        for i, cand in enumerate(candidates):
            row = frame.iloc[i].copy()
            row["p_copy"] = float(p[i])
            if "clip_norm" not in row:
                row["clip_norm"] = -1.0
            ref, kp = details[i]
            matches.append(
                {
                    "media_id": cand.media_id,
                    "post_id": cand.post_id,
                    "owner_user_id": cand.owner_user_id,
                    "team_id": cand.team_id,
                    "bucket": cand.bucket,
                    "object_name": cand.object_name,
                    "p_copy": float(p[i]),
                    "zone": risk_zone(float(p[i]), zones),
                    "match_type": match_type(row, zones, clip_similar),
                    "signals": {
                        "sscd_similarity": round(float(row["sscd_raw"]), 4),
                        "sscd_normalized": round(float(row["sscd_norm"]), 4),
                        "phash_distance": int(row["phash_dist"]),
                        "keypoint_inliers": int(row["kp_inliers"]),
                        "keypoint_centre_coverage": round(float(row["kp_r_center_cov"]), 3),
                        "keypoint_scale": round(float(row["kp_scale"]), 3),
                        **({"clip_normalized": round(float(row["clip_norm"]), 4)} if self.uses_clip else {}),
                    },
                    **_regions(kp, qa.image, ref),
                }
            )
        matches.sort(key=lambda m: -m["p_copy"])
        return matches, timings


def _hamming(a: int, b: int) -> int:
    return int(np.bitwise_count(np.array([a ^ b], dtype=np.int64).view(np.uint64))[0])


def _scaled_size(img: Image.Image) -> tuple[float, float]:
    s = K.SIDE / max(img.size)
    return max(1, round(img.width * s)), max(1, round(img.height * s))


def _regions(kp: dict, query: Image.Image, ref: Image.Image) -> dict:
    """Where the match is: the upload's outline inside the reference and the reference's
    outline inside the upload, as polygons in [0, 1] image coordinates."""
    model = kp.get("_model")
    # RANSAC can return a degenerate transform (scale ~ 0) on a handful of points; it describes
    # no region, and it cannot be inverted.
    if model is None or kp.get("kp_inliers", 0) < 4 or abs(np.linalg.det(model[:, :2])) < 1e-6:
        return {"query_region": None, "reference_region": None}
    qw, qh = _scaled_size(query)
    rw, rh = _scaled_size(ref)
    A = np.vstack([model, [0, 0, 1]])
    q_corners = np.array([[0, 0, 1], [qw, 0, 1], [qw, qh, 1], [0, qh, 1]], float)
    in_ref = (A @ q_corners.T).T[:, :2] / [rw, rh]
    r_corners = np.array([[0, 0, 1], [rw, 0, 1], [rw, rh, 1], [0, rh, 1]], float)
    in_query = (np.linalg.inv(A) @ r_corners.T).T[:, :2] / [qw, qh]
    if kp.get("kp_mirrored"):
        in_query[:, 0] = 1 - in_query[:, 0]  # matched on the mirrored upload
    clip01 = lambda pts: np.clip(pts, 0, 1).round(4).tolist()
    return {"query_region": clip01(in_query), "reference_region": clip01(in_ref)}


def render_explanation(query: Image.Image, ref: Image.Image, match: dict | None = None) -> bytes:
    """Side-by-side PNG: the upload and the matched work, inlier correspondences in green,
    the matched area outlined. This is what the author and the moderator are shown."""
    qa = K.extract_with_mirror(query)
    kp = K.match(qa, K.extract(ref), with_points=True)
    qs, rs = query.resize(_scaled_size(query)), ref.resize(_scaled_size(ref))
    if kp.get("kp_mirrored"):
        qs = qs.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
    gap = 24
    canvas = Image.new("RGB", (qs.width + rs.width + gap, max(qs.height, rs.height) + 28), "white")
    canvas.paste(qs, (0, 28))
    canvas.paste(rs, (qs.width + gap, 28))
    draw = ImageDraw.Draw(canvas)
    if "_src" in kp:
        step = max(1, len(kp["_src"]) // 80)
        for (x1, y1), (x2, y2) in zip(kp["_src"][::step], kp["_dst"][::step]):
            draw.line([(x1, y1 + 28), (x2 + qs.width + gap, y2 + 28)], fill=(31, 158, 137), width=1)
        hull = cv2.convexHull(kp["_dst"].astype(np.float32)).reshape(-1, 2)
        draw.polygon([(x + qs.width + gap, y + 28) for x, y in hull], outline=(220, 50, 47), width=3)
    title = "upload  |  matched work"
    if match:
        title += f"   P(copy)={match['p_copy']:.2f}  zone={match['zone']}  type={match['match_type']}"
    draw.text((6, 6), title, fill=(0, 0, 0))
    buf = io.BytesIO()
    canvas.save(buf, format="PNG")
    return buf.getvalue()
