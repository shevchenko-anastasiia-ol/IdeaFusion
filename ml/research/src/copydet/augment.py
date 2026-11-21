"""The 12 copy-modification types of the threat model (chapter 1, table 1.1 / section 1.4.1).

Each modification is a function `(img, rng, ctx) -> (modified_img, params)`. All randomness
comes from the passed `numpy.random.Generator`, so a query is fully reproducible from its seed,
and the returned `params` are stored in the manifest to describe exactly what was done.

Types listed in `HELD_OUT_TYPES` must never be used for training (pitfall "same
modifications in train and test", section 1.4.3).
"""

from __future__ import annotations

import math
import zlib
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable, Sequence

import augly.image as imaugs
import augly.utils as augly_utils
import numpy as np
from PIL import Image

from copydet.imageio import load_rgb


@dataclass
class AugContext:
    """External material some modifications need (collage backgrounds)."""

    distractor_paths: Sequence[Path] = field(default_factory=list)


AugResult = tuple[Image.Image, dict]
AugFn = Callable[[Image.Image, np.random.Generator, AugContext], AugResult]

AUGMENTATIONS: dict[str, AugFn] = {}


def _register(name: str):
    def wrap(fn: AugFn) -> AugFn:
        AUGMENTATIONS[name] = fn
        return fn

    return wrap


def _u(rng: np.random.Generator, lo: float, hi: float) -> float:
    return round(float(rng.uniform(lo, hi)), 4)


def _text_fonts() -> list[Path]:
    # Symbol fonts render pictograms, not text; they are excluded from text/watermark overlays.
    skip = ("dingbat", "entypo", "symbol", "emoji", "icon", "webdings")
    fonts = sorted(Path(augly_utils.FONTS_DIR).glob("*.ttf"))
    return [f for f in fonts if f.with_suffix(".pkl").exists() and not any(s in f.stem.lower() for s in skip)]


def _emoji_paths() -> list[Path]:
    return sorted(Path(augly_utils.EMOJI_DIR).rglob("*.png"))


_FONTS = _text_fonts()
_EMOJIS = _emoji_paths()
_SCREENSHOT_TEMPLATES = sorted(Path(augly_utils.SCREENSHOT_TEMPLATES_DIR).glob("*.png"))


def _random_text(rng: np.random.Generator) -> list[list[int]]:
    """AugLy maps indices onto the font's charset modulo its length, so any ints work."""
    lines = int(rng.integers(1, 4))
    return [rng.integers(0, 10_000, size=int(rng.integers(4, 16))).tolist() for _ in range(lines)]


def _random_color(rng: np.random.Generator) -> tuple[int, int, int]:
    return tuple(int(c) for c in rng.integers(0, 256, size=3))


@_register("jpeg")
def jpeg(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    quality = int(rng.integers(8, 45))
    return imaugs.encoding_quality(img, quality=quality), {"quality": quality}


@_register("resize")
def resize(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    factor = _u(rng, 0.25, 0.6)
    return imaugs.scale(img, factor=factor), {"factor": factor}


@_register("crop")
def crop(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    # Keeps 45-80% of the area: a noticeable crop, but still mostly the whole work.
    w, h = _u(rng, 0.6, 0.9), _u(rng, 0.6, 0.9)
    x1, y1 = _u(rng, 0, 1 - w), _u(rng, 0, 1 - h)
    box = {"x1": x1, "y1": y1, "x2": round(x1 + w, 4), "y2": round(y1 + h, 4)}
    return imaugs.crop(img, **box), box


@_register("hflip")
def hflip(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    return imaugs.hflip(img), {}


@_register("rotate")
def rotate(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    degrees = _u(rng, 5, 30) * (1 if rng.random() < 0.5 else -1)
    return imaugs.rotate(img, degrees=degrees), {"degrees": round(degrees, 2)}


@_register("color")
def color(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    params = {
        "brightness_factor": _u(rng, 0.5, 1.5),
        "contrast_factor": _u(rng, 0.5, 1.6),
        "saturation_factor": _u(rng, 0.0, 2.0),
    }
    return imaugs.color_jitter(img, **params), params


@_register("filter")
def filter_(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    kind = str(rng.choice(["blur", "sharpen", "grayscale", "pixelization"]))
    if kind == "blur":
        radius = _u(rng, 1.5, 4.0)
        return imaugs.blur(img, radius=radius), {"kind": kind, "radius": radius}
    if kind == "sharpen":
        factor = _u(rng, 3.0, 10.0)
        return imaugs.sharpen(img, factor=factor), {"kind": kind, "factor": factor}
    if kind == "grayscale":
        return imaugs.grayscale(img).convert("RGB"), {"kind": kind}
    ratio = _u(rng, 0.2, 0.5)
    return imaugs.pixelization(img, ratio=ratio), {"kind": kind, "ratio": ratio}


@_register("text_overlay")
def text_overlay(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    font = _FONTS[int(rng.integers(len(_FONTS)))]
    params = {
        "font": font.name,
        "font_size": _u(rng, 0.05, 0.14),
        "opacity": _u(rng, 0.8, 1.0),
        "color": _random_color(rng),
        "x_pos": _u(rng, 0.0, 0.4),
        "y_pos": _u(rng, 0.0, 0.75),
    }
    out = imaugs.overlay_text(
        img,
        text=_random_text(rng),
        font_file=str(font),
        font_size=params["font_size"],
        opacity=params["opacity"],
        color=params["color"],
        x_pos=params["x_pos"],
        y_pos=params["y_pos"],
    )
    return out, params


@_register("watermark")
def watermark(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    """Semi-transparent text or logo, the way people "sign" a stolen work."""
    if rng.random() < 0.5:
        font = _FONTS[int(rng.integers(len(_FONTS)))]
        params = {
            "kind": "text",
            "font": font.name,
            "font_size": _u(rng, 0.12, 0.25),
            "opacity": _u(rng, 0.25, 0.55),
            "color": (255, 255, 255) if rng.random() < 0.5 else (0, 0, 0),
            "x_pos": _u(rng, 0.05, 0.3),
            "y_pos": _u(rng, 0.3, 0.6),
        }
        out = imaugs.overlay_text(
            img,
            text=[rng.integers(0, 10_000, size=int(rng.integers(5, 10))).tolist()],
            font_file=str(font),
            font_size=params["font_size"],
            opacity=params["opacity"],
            color=params["color"],
            x_pos=params["x_pos"],
            y_pos=params["y_pos"],
        )
        return out, params
    emoji = _EMOJIS[int(rng.integers(len(_EMOJIS)))]
    size = _u(rng, 0.15, 0.35)
    params = {
        "kind": "logo",
        "emoji": f"{emoji.parent.name}/{emoji.name}",
        "emoji_size": size,
        "opacity": _u(rng, 0.3, 0.7),
        "x_pos": _u(rng, 0.0, 1 - size),
        "y_pos": _u(rng, 0.0, 1 - size),
    }
    out = imaugs.overlay_emoji(
        img,
        emoji_path=str(emoji),
        emoji_size=size,
        opacity=params["opacity"],
        x_pos=params["x_pos"],
        y_pos=params["y_pos"],
    )
    return out, params


@_register("screenshot")
def screenshot(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    template = _SCREENSHOT_TEMPLATES[int(rng.integers(len(_SCREENSHOT_TEMPLATES)))]
    crop_to_fit = bool(rng.random() < 0.5)
    out = imaugs.overlay_onto_screenshot(img, template_filepath=str(template), crop_src_to_fit=crop_to_fit)
    return out.convert("RGB"), {"template": template.name, "crop_src_to_fit": crop_to_fit}


@_register("collage")
def collage(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    """The work pasted onto someone else's image, occupying 35-60% of its height."""
    if not ctx.distractor_paths:
        raise ValueError("collage needs AugContext.distractor_paths")
    bg_path = Path(ctx.distractor_paths[int(rng.integers(len(ctx.distractor_paths)))])
    background = load_rgb(bg_path)
    # Make the background comparable in size to the work, otherwise a 256px work pasted
    # onto a 4k background would vanish.
    target = max(img.size) * _u(rng, 1.0, 1.5)
    scale = target / max(background.size)
    background = background.resize(
        (max(1, round(background.width * scale)), max(1, round(background.height * scale))),
        Image.Resampling.LANCZOS,
    )
    size = _u(rng, 0.35, 0.6)
    params = {
        "background": bg_path.stem,
        "overlay_size": size,
        "x_pos": _u(rng, 0.0, 1 - size),
        "y_pos": _u(rng, 0.0, 1 - size),
    }
    out = imaugs.overlay_onto_background_image(
        img,
        background_image=background,
        overlay_size=size,
        x_pos=params["x_pos"],
        y_pos=params["y_pos"],
    )
    return out.convert("RGB"), params


@_register("fragment")
def fragment(img: Image.Image, rng: np.random.Generator, ctx: AugContext) -> AugResult:
    """A small piece (10-30% of the area) cut out and blown up to the original width."""
    area = _u(rng, 0.10, 0.30)
    aspect = _u(rng, 0.6, 1.6)
    w = min(0.95, math.sqrt(area * aspect))
    h = min(0.95, area / w)
    x1, y1 = _u(rng, 0, 1 - w), _u(rng, 0, 1 - h)
    box = {"x1": x1, "y1": y1, "x2": round(x1 + w, 4), "y2": round(y1 + h, 4)}
    piece = imaugs.crop(img, **box)
    scale = img.width / piece.width
    out = imaugs.resize(piece, width=img.width, height=max(1, round(piece.height * scale)))
    return out, {**box, "area": area}


ALL_TYPES: tuple[str, ...] = tuple(AUGMENTATIONS)
HELD_OUT_TYPES: frozenset[str] = frozenset({"collage", "screenshot"})
TRAINABLE_TYPES: tuple[str, ...] = tuple(t for t in ALL_TYPES if t not in HELD_OUT_TYPES)


def rng_for(seed: int, key: str) -> np.random.Generator:
    """Deterministic generator per item, independent of processing order or worker count."""
    return np.random.default_rng([seed, zlib.crc32(key.encode("utf-8"))])


def apply(aug_type: str, img: Image.Image, rng: np.random.Generator, ctx: AugContext | None = None) -> AugResult:
    out, params = AUGMENTATIONS[aug_type](img, rng, ctx or AugContext())
    return out.convert("RGB"), params
