"""Fetch the SSCD weights that are not kept in git, and verify them against the manifest.

    python ml/models/download_weights.py            # all versions under ml/models
    python ml/models/download_weights.py v1
"""

from __future__ import annotations

import hashlib
import json
import sys
import urllib.request
from pathlib import Path

HERE = Path(__file__).resolve().parent


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def main(versions: list[str]) -> None:
    dirs = [HERE / v for v in versions] if versions else sorted(p for p in HERE.iterdir() if (p / "manifest.json").exists())
    for d in dirs:
        manifest = json.loads((d / "manifest.json").read_text(encoding="utf-8"))
        target = d / manifest["sscd_weights"]
        expected = manifest["files"][manifest["sscd_weights"]]
        if not (target.exists() and sha256(target) == expected):
            print(f"{d.name}: downloading {manifest['sscd_weights_url']}")
            urllib.request.urlretrieve(manifest["sscd_weights_url"], target)
        ok = sha256(target) == expected
        print(f"{d.name}: {target.name} {'ok' if ok else 'CHECKSUM MISMATCH'}")
        if not ok:
            sys.exit(1)


if __name__ == "__main__":
    main(sys.argv[1:])
