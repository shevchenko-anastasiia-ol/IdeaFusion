"""Local features with geometric verification (chapter 1, section 1.2.2): SIFT + RANSAC.

Runs only on the few candidates the descriptor retrieved. Besides the inlier count it reports
*where* the inliers are, because on artworks the dangerous false match is a shared frame or
page layout: two portraits from one engraved series match perfectly along the ornamental
border under the identity transform, while the centres (the actual work) do not match at all.
Coverage is measured on an 8x8 grid (fraction of cells holding an inlier), separately for the
inner 4x4 cells.

The query is matched as is and mirrored (SIFT is not mirror-invariant); the better wins.
"""

from __future__ import annotations

from dataclasses import dataclass

import cv2
import numpy as np
from PIL import Image, ImageOps

SIDE = 512  # images are rescaled (also up) so the longer side has this many pixels
GRID = 8
RATIO = 0.75
RANSAC_PX = 6.0
# Plausible scale of the query->reference similarity transform after both images are
# rescaled to SIDE: a 30x shrink or blow-up is not a copy, it is a degenerate fit.
MIN_SCALE, MAX_SCALE = 0.1, 10.0


@dataclass
class Features:
    pts: np.ndarray  # (N, 2) float32 pixel coords in the rescaled image
    desc: np.ndarray  # (N, 128) float32
    size: tuple[int, int]  # rescaled (width, height)


_SIFT = None


def _sift():
    global _SIFT
    if _SIFT is None:
        _SIFT = cv2.SIFT_create(nfeatures=1000)
    return _SIFT


def extract(img: Image.Image) -> Features:
    scale = SIDE / max(img.size)
    w, h = max(1, round(img.width * scale)), max(1, round(img.height * scale))
    gray = np.asarray(img.convert("L").resize((w, h), Image.Resampling.BICUBIC))
    kps, desc = _sift().detectAndCompute(gray, None)
    if desc is None:
        return Features(np.zeros((0, 2), np.float32), np.zeros((0, 128), np.float32), (w, h))
    return Features(np.array([k.pt for k in kps], np.float32), desc.astype(np.float32), (w, h))


def extract_with_mirror(img: Image.Image) -> tuple[Features, Features]:
    return extract(img), extract(ImageOps.mirror(img))


EMPTY = {
    "kp_good": 0,
    "kp_inliers": 0,
    "kp_inlier_ratio": 0.0,
    "kp_q_cov": 0.0,
    "kp_r_cov": 0.0,
    "kp_r_center_cov": 0.0,
    "kp_scale": 0.0,
    "kp_mirrored": False,
}


def _coverage(pts: np.ndarray, size: tuple[int, int]) -> tuple[float, float]:
    if len(pts) == 0:
        return 0.0, 0.0
    cells = np.zeros((GRID, GRID), dtype=bool)
    gx = np.clip((pts[:, 0] / size[0] * GRID).astype(int), 0, GRID - 1)
    gy = np.clip((pts[:, 1] / size[1] * GRID).astype(int), 0, GRID - 1)
    cells[gy, gx] = True
    q = GRID // 4
    return float(cells.mean()), float(cells[q : GRID - q, q : GRID - q].mean())


def _match_one(q: Features, r: Features, with_points: bool) -> dict:
    if len(q.desc) < 2 or len(r.desc) < 2:
        return dict(EMPTY)
    knn = cv2.BFMatcher(cv2.NORM_L2).knnMatch(q.desc, r.desc, k=2)
    good = [m for m, *rest in knn if rest and m.distance < RATIO * rest[0].distance]
    out = dict(EMPTY, kp_good=len(good))
    if len(good) < 4:
        return out
    src = q.pts[[m.queryIdx for m in good]]
    dst = r.pts[[m.trainIdx for m in good]]
    model, mask = cv2.estimateAffinePartial2D(
        src, dst, method=cv2.RANSAC, ransacReprojThreshold=RANSAC_PX, maxIters=2000, confidence=0.99
    )
    if model is None or mask is None:
        return out
    scale = float(np.hypot(model[0, 0], model[1, 0]))
    if not MIN_SCALE <= scale <= MAX_SCALE:
        # Degenerate fit: everything collapses onto (or explodes from) one spot, so any match
        # landing near it counts as an "inlier". On the dataset a third of the pairs with >= 4
        # inliers were such fits, almost all of them non-copies; real copies stay in 0.34..4.2.
        return out
    inl = mask.ravel().astype(bool)
    q_cov, _ = _coverage(src[inl], q.size)
    r_cov, r_center = _coverage(dst[inl], r.size)
    out.update(
        kp_inliers=int(inl.sum()),
        kp_inlier_ratio=float(inl.mean()),
        kp_q_cov=q_cov,
        kp_r_cov=r_cov,
        kp_r_center_cov=r_center,
        kp_scale=scale,
    )
    if with_points:
        out["_src"], out["_dst"], out["_model"] = src[inl], dst[inl], model
    return out


def match(query: tuple[Features, Features], ref: Features, with_points: bool = False) -> dict:
    """Best of matching the query as is and mirrored against the reference."""
    plain = _match_one(query[0], ref, with_points)
    mirrored = _match_one(query[1], ref, with_points)
    if mirrored["kp_inliers"] > plain["kp_inliers"]:
        mirrored["kp_mirrored"] = True
        return mirrored
    return plain
