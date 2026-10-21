"""Perceptual hashes packed into int64 so they can live in parquet / Postgres bigint."""

from __future__ import annotations

import imagehash
import numpy as np
from PIL import Image, ImageOps


def _to_int64(h: imagehash.ImageHash) -> int:
    bits = h.hash.flatten()
    value = 0
    for b in bits:
        value = (value << 1) | int(b)
    # Reinterpret the unsigned 64-bit value as signed so it fits int64 columns.
    return int(np.array(value, dtype=np.uint64).view(np.int64))


def phash64(img: Image.Image) -> int:
    return _to_int64(imagehash.phash(img, hash_size=8))


def phash64_with_mirror(img: Image.Image) -> tuple[int, int]:
    """pHash of the image and of its horizontal mirror (catches flipped copies cheaply)."""
    return phash64(img), phash64(ImageOps.mirror(img))


def hamming_matrix(a: np.ndarray, b: np.ndarray) -> np.ndarray:
    """Pairwise Hamming distances between two int64 hash arrays, shape (len(a), len(b))."""
    xor = np.bitwise_xor(a.astype(np.int64)[:, None], b.astype(np.int64)[None, :])
    return np.bitwise_count(xor.view(np.uint64)).astype(np.int16)
