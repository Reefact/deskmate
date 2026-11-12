#!/usr/bin/env python3
"""Extract individual, transparent sprite frames from a chroma-key (magenta) sprite sheet.

Usage:
    python3 tools/extract_sprites.py <sheet.(webp|png)> <output-dir> [--prefix gaze] [--padding 8]

The sheet must contain frames laid out horizontally on a uniform magenta (#FF00FF)
background. Frames are detected from fully-background columns, keyed to alpha, de-spilled,
and exported on a common canvas aligned on the feet (bottom-centre) so that switching
frames does not make the character jump.

Requires Pillow and NumPy.
"""
import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image

KEY = np.array([255.0, 0.0, 255.0])


def key_alpha(rgb: np.ndarray) -> np.ndarray:
    """Return alpha in [0, 1]: 0 for pure magenta, 1 for clearly non-magenta pixels."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    # "Magenta-ness": how much both red and blue exceed green.
    magenta = np.minimum(r, b) - g
    lo, hi = 90.0, 200.0
    k = np.clip((magenta - lo) / (hi - lo), 0.0, 1.0)
    return 1.0 - k


def despill(rgb: np.ndarray, alpha: np.ndarray) -> np.ndarray:
    """Remove the magenta fringe on semi-transparent edges."""
    a = alpha[..., None]
    # Un-mix the background contribution: obs = a*fg + (1-a)*KEY
    safe = np.maximum(a, 1e-3)
    fg = (rgb - (1.0 - a) * KEY) / safe
    fg = np.clip(fg, 0, 255)
    # Limit residual magenta tint: red/blue may not exceed green by much on edges.
    edge = (alpha < 0.98)
    g = fg[..., 1]
    limit = g + 60.0
    fg[..., 0] = np.where(edge, np.minimum(fg[..., 0], limit), fg[..., 0])
    fg[..., 2] = np.where(edge, np.minimum(fg[..., 2], limit), fg[..., 2])
    return fg


def find_runs(mask_cols: np.ndarray):
    runs, start = [], None
    for x, filled in enumerate(mask_cols):
        if filled and start is None:
            start = x
        elif not filled and start is not None:
            runs.append((start, x))
            start = None
    if start is not None:
        runs.append((start, len(mask_cols)))
    return [r for r in runs if r[1] - r[0] > 10]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("sheet")
    parser.add_argument("output")
    parser.add_argument("--prefix", default="gaze")
    parser.add_argument("--padding", type=int, default=8)
    args = parser.parse_args()

    rgb = np.asarray(Image.open(args.sheet).convert("RGB")).astype(np.float64)
    alpha = key_alpha(rgb)
    alpha[alpha < 0.04] = 0.0
    fg = despill(rgb, alpha)

    solid = alpha > 0.5
    runs = find_runs(solid.sum(axis=0) > 0)
    rows = np.nonzero(solid.sum(axis=1))[0]
    top, bottom = int(rows.min()), int(rows.max()) + 1

    # Align frames on the feet: horizontal centre of the lowest 40 rows of each frame.
    feet_centres = []
    for x0, x1 in runs:
        feet = solid[bottom - 40:bottom, x0:x1]
        xs = np.nonzero(feet.sum(axis=0))[0]
        feet_centres.append(x0 + (xs.min() + xs.max() + 1) / 2.0)

    half = max(max(c - x0, x1 - c) for (x0, x1), c in zip(runs, feet_centres))
    pad = args.padding
    width = int(np.ceil(half * 2)) + 2 * pad
    height = (bottom - top) + 2 * pad

    out = Path(args.output)
    out.mkdir(parents=True, exist_ok=True)
    files = []
    rgba = np.dstack([fg, alpha * 255.0]).round().astype(np.uint8)
    for i, c in enumerate(feet_centres):
        canvas = np.zeros((height, width, 4), dtype=np.uint8)
        src_x0 = int(round(c - width / 2))
        for cx in range(width):
            sx = src_x0 + cx
            if 0 <= sx < rgba.shape[1]:
                canvas[pad:pad + (bottom - top), cx] = rgba[top:bottom, sx]
        name = f"{args.prefix}-{i}.png"
        Image.fromarray(canvas, "RGBA").save(out / name, optimize=True)
        files.append(name)

    print(json.dumps({"frameWidth": width, "frameHeight": height,
                      "feet": {"x": width / 2, "y": height - pad},
                      "files": files}, indent=2))


if __name__ == "__main__":
    main()
