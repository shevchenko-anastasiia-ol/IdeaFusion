import numpy as np
from PIL import Image, ImageDraw, ImageOps

from copydet import keypoints as K


def _shapes(rng: np.random.Generator, size=(400, 400), n=40) -> Image.Image:
    img = Image.new("RGB", size, (235, 230, 220))
    d = ImageDraw.Draw(img)
    for _ in range(n):
        x, y = rng.integers(0, size[0] - 30), rng.integers(0, size[1] - 30)
        w, h = rng.integers(8, 40, size=2)
        d.rectangle((x, y, x + w, y + h), fill=tuple(int(c) for c in rng.integers(0, 200, 3)))
    return img


def _framed(center: Image.Image, frame: Image.Image) -> Image.Image:
    out = frame.copy()
    out.paste(center.resize((240, 240)), (80, 80))
    return out


def test_crop_and_mirror_copies_match():
    img = _shapes(np.random.default_rng(1))
    ref = K.extract(img)
    crop = img.crop((60, 40, 360, 340))
    assert K.match(K.extract_with_mirror(crop), ref)["kp_inliers"] >= 20
    mirrored = K.match(K.extract_with_mirror(ImageOps.mirror(img)), ref)
    assert mirrored["kp_mirrored"] and mirrored["kp_inliers"] >= 20


def test_shared_frame_matches_only_on_the_border():
    rng = np.random.default_rng(2)
    frame = _shapes(rng, n=120)
    a, b = _framed(_shapes(rng), frame), _framed(_shapes(rng), frame)
    same_frame = K.match(K.extract_with_mirror(a), K.extract(b))
    copy = K.match(K.extract_with_mirror(a), K.extract(a))
    assert same_frame["kp_inliers"] >= 10  # the frame really matches
    assert same_frame["kp_r_center_cov"] < 0.2  # ...but the work itself does not
    assert copy["kp_r_center_cov"] > 0.5


def test_unrelated_images_do_not_match():
    for seed in range(3, 9):
        a, b = _shapes(np.random.default_rng(seed)), _shapes(np.random.default_rng(seed + 100))
        m = K.match(K.extract_with_mirror(a), K.extract(b))
        assert m["kp_inliers"] < 10
        # A reported transform is never a degenerate collapse onto one point.
        assert m["kp_inliers"] == 0 or K.MIN_SCALE <= m["kp_scale"] <= K.MAX_SCALE
