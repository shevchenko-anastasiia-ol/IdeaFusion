"""Image loading/saving shared by dataset building, training and the service.

Every image entering the pipeline goes through `load_rgb`, so EXIF rotation and
transparency are handled identically everywhere.
"""

from __future__ import annotations

import io
from pathlib import Path

from PIL import Image, ImageOps

# Some artworks are large scans; PIL's bomb guard would reject them.
Image.MAX_IMAGE_PIXELS = 200_000_000


def load_rgb(source: str | Path | bytes) -> Image.Image:
    """Open an image as RGB, applying EXIF orientation and flattening alpha onto white."""
    img = Image.open(io.BytesIO(source) if isinstance(source, bytes) else source)
    img = ImageOps.exif_transpose(img)
    if img.mode in ("RGBA", "LA") or (img.mode == "P" and "transparency" in img.info):
        rgba = img.convert("RGBA")
        background = Image.new("RGB", rgba.size, (255, 255, 255))
        background.paste(rgba, mask=rgba.getchannel("A"))
        return background
    return img.convert("RGB")


def limit_side(img: Image.Image, max_side: int) -> Image.Image:
    """Downscale so the longer side is at most `max_side`; never upscales."""
    if max(img.size) <= max_side:
        return img
    scale = max_side / max(img.size)
    size = (max(1, round(img.width * scale)), max(1, round(img.height * scale)))
    return img.resize(size, Image.Resampling.LANCZOS)


def save_jpeg(img: Image.Image, path: str | Path, quality: int = 95) -> None:
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    img.convert("RGB").save(path, format="JPEG", quality=quality)
