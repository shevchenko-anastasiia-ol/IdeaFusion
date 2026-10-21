"""Image descriptors used by the pipeline (chapter 1, section 1.2) with an on-disk cache.

* `PHash`  - 64-bit perceptual hash of the image and of its mirror (stored as two int64 columns)
* `SSCD`   - copy-detection descriptor (Pizzi et al., CVPR 2022), 512-d, L2-normalised
* `CLIP`   - semantic embedding (open_clip), L2-normalised; a baseline and an informational signal

Every descriptor maps a list of image files to an (N, D) array. `embed_cached` computes only
the files missing from the cache, so re-running an evaluation is cheap.
"""

from __future__ import annotations

import json
import time
from pathlib import Path
from typing import Protocol, Sequence

import numpy as np
from tqdm import tqdm

from copydet.hashing import phash64_with_mirror
from copydet.imageio import load_rgb

IMAGENET_MEAN = (0.485, 0.456, 0.406)
IMAGENET_STD = (0.229, 0.224, 0.225)


class Descriptor(Protocol):
    name: str
    dtype: str

    def embed(self, paths: Sequence[Path]) -> np.ndarray: ...


class PHash:
    name = "phash"
    dtype = "int64"

    def embed(self, paths: Sequence[Path]) -> np.ndarray:
        return np.array([phash64_with_mirror(load_rgb(p)) for p in tqdm(paths, desc="phash")], dtype=np.int64)


class SSCD:
    """TorchScript SSCD. `small_288` resizes the shorter side to 288 keeping the aspect ratio,
    as in the authors' evaluation; it needs batch size 1 because sizes differ."""

    dtype = "float32"

    def __init__(self, weights: str | Path, name: str = "sscd"):
        import torch
        from torchvision import transforms

        self.name = name
        self._torch = torch
        self.model = torch.jit.load(str(weights), map_location="cpu").eval()
        self.transform = transforms.Compose(
            [
                transforms.Resize(288),
                transforms.ToTensor(),
                transforms.Normalize(IMAGENET_MEAN, IMAGENET_STD),
            ]
        )

    def embed(self, paths: Sequence[Path]) -> np.ndarray:
        out = np.empty((len(paths), 512), dtype=np.float32)
        for i, p in enumerate(tqdm(paths, desc=self.name)):
            out[i] = self.embed_images([load_rgb(p)])[0]
        return out

    def embed_images(self, images: Sequence) -> np.ndarray:
        """Embed in-memory PIL images (one forward each: sizes differ under small_288)."""
        out = np.empty((len(images), 512), dtype=np.float32)
        with self._torch.inference_mode():
            for i, img in enumerate(images):
                out[i] = self.model(self.transform(img).unsqueeze(0))[0].numpy()
        return _l2(out)


class CLIP:
    dtype = "float32"

    def __init__(self, model: str = "ViT-B-32", pretrained: str = "laion2b_s34b_b79k", batch_size: int = 32):
        import open_clip
        import torch

        self.name = f"clip_{model.lower().replace('-', '')}"
        self._torch = torch
        self.model, _, self.transform = open_clip.create_model_and_transforms(model, pretrained=pretrained)
        self.model.eval()
        self.batch_size = batch_size

    def embed(self, paths: Sequence[Path]) -> np.ndarray:
        chunks = [
            self.embed_images([load_rgb(p) for p in paths[i : i + self.batch_size]])
            for i in tqdm(range(0, len(paths), self.batch_size), desc=self.name)
        ]
        return np.concatenate(chunks) if chunks else np.empty((0, 0), np.float32)

    def embed_images(self, images: Sequence) -> np.ndarray:
        with self._torch.inference_mode():
            batch = self._torch.stack([self.transform(img) for img in images])
            return _l2(self.model.encode_image(batch).float().numpy())


def _l2(x: np.ndarray) -> np.ndarray:
    return x / np.maximum(np.linalg.norm(x, axis=1, keepdims=True), 1e-12)


def embed_cached(desc: Descriptor, keys: Sequence[str], paths: Sequence[Path], cache_dir: Path) -> np.ndarray:
    """Embeddings for `paths`, cached under `cache_dir/<desc.name>.npz` by `keys`.

    Keys must identify the image content (dataset-relative path or catalog id), not the
    absolute location, so the cache survives moving the data folder.
    """
    cache_dir.mkdir(parents=True, exist_ok=True)
    cache_file = cache_dir / f"{desc.name}.npz"
    meta_file = cache_dir / f"{desc.name}.json"
    cached: dict[str, np.ndarray] = {}
    if cache_file.exists():
        with np.load(cache_file, allow_pickle=False) as z:
            cached = dict(zip(z["keys"].tolist(), z["emb"]))

    missing = [i for i, k in enumerate(keys) if k not in cached]
    if missing:
        t0 = time.perf_counter()
        new = desc.embed([paths[i] for i in missing])
        ms_per_image = (time.perf_counter() - t0) * 1000 / len(missing)
        cached.update({keys[i]: e for i, e in zip(missing, new)})
        all_keys = list(cached)
        np.savez(cache_file, keys=np.array(all_keys), emb=np.stack([cached[k] for k in all_keys]))
        meta_file.write_text(json.dumps({"ms_per_image": ms_per_image, "n_measured": len(missing)}), encoding="utf-8")
    return np.stack([cached[k] for k in keys])


def ms_per_image(desc_name: str, cache_dir: Path) -> float | None:
    meta = cache_dir / f"{desc_name}.json"
    return json.loads(meta.read_text(encoding="utf-8"))["ms_per_image"] if meta.exists() else None
