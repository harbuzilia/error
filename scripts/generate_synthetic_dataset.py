#!/usr/bin/env python3
"""Generate a small synthetic object-detection dataset.

The resulting dataset mimics a two-class problem with simple geometric shapes.
Images and labels are created under ``data/raw`` so they can be consumed by
Ultralytics YOLO configurations.
"""

from __future__ import annotations

import random
from dataclasses import dataclass
from pathlib import Path
from typing import Tuple

from PIL import Image, ImageDraw

# Repository-relative locations
REPO_ROOT = Path(__file__).resolve().parents[1]
DATA_ROOT = REPO_ROOT / "data" / "raw"

IMAGE_SIZE = (320, 320)  # width, height in pixels
TRAIN_SIZE = 17
VAL_SIZE = 8
CLASSES = ["square_target", "circle_target"]
RNG_SEED = 1337


def ensure_dirs() -> None:
    for split in ("train", "val"):
        img_dir = DATA_ROOT / split / "images"
        lbl_dir = DATA_ROOT / split / "labels"
        img_dir.mkdir(parents=True, exist_ok=True)
        lbl_dir.mkdir(parents=True, exist_ok=True)


@dataclass
class Box:
    """Normalized bounding box specification."""

    cx: float
    cy: float
    w: float
    h: float

    def to_pixel_bbox(self) -> Tuple[int, int, int, int]:
        width, height = IMAGE_SIZE
        half_w = (self.w * width) / 2
        half_h = (self.h * height) / 2
        center_x = self.cx * width
        center_y = self.cy * height
        x1 = int(max(center_x - half_w, 0))
        y1 = int(max(center_y - half_h, 0))
        x2 = int(min(center_x + half_w, width))
        y2 = int(min(center_y + half_h, height))
        return x1, y1, x2, y2


def random_box(rng: random.Random) -> Box:
    w = rng.uniform(0.18, 0.38)
    h = rng.uniform(0.18, 0.38)
    cx = rng.uniform(w / 2 + 0.02, 1 - (w / 2 + 0.02))
    cy = rng.uniform(h / 2 + 0.02, 1 - (h / 2 + 0.02))
    return Box(cx=cx, cy=cy, w=w, h=h)


def draw_shape(draw: ImageDraw.ImageDraw, cls_id: int, bbox: Box) -> None:
    x1, y1, x2, y2 = bbox.to_pixel_bbox()
    fill_color = (255, 180, 64) if cls_id == 0 else (64, 164, 255)
    outline = (20, 20, 20)
    if cls_id == 0:
        draw.rectangle([x1, y1, x2, y2], fill=fill_color, outline=outline, width=3)
    else:
        draw.ellipse([x1, y1, x2, y2], fill=fill_color, outline=outline, width=3)


def write_label(label_path: Path, entries: list[tuple[int, Box]]) -> None:
    lines = [
        f"{cls} {box.cx:.6f} {box.cy:.6f} {box.w:.6f} {box.h:.6f}" for cls, box in entries
    ]
    label_path.write_text("\n".join(lines) + ("\n" if lines else ""), encoding="utf-8")


def create_sample(split: str, idx: int, rng: random.Random) -> None:
    img_path = DATA_ROOT / split / "images" / f"{split}_{idx:02d}.jpg"
    lbl_path = DATA_ROOT / split / "labels" / f"{split}_{idx:02d}.txt"

    background = (rng.randint(150, 255), rng.randint(150, 255), rng.randint(150, 255))
    image = Image.new("RGB", IMAGE_SIZE, color=background)
    draw = ImageDraw.Draw(image)

    num_objects = rng.randint(1, 3)
    entries: list[tuple[int, Box]] = []
    for _ in range(num_objects):
        cls_id = rng.randint(0, len(CLASSES) - 1)
        box = random_box(rng)
        draw_shape(draw, cls_id, box)
        entries.append((cls_id, box))

    image.save(img_path, format="JPEG", quality=95)
    write_label(lbl_path, entries)


def build_split(split: str, count: int, base_seed: int) -> None:
    rng = random.Random(base_seed)
    for idx in range(count):
        create_sample(split, idx, rng)


def main() -> None:
    ensure_dirs()
    build_split("train", TRAIN_SIZE, RNG_SEED)
    build_split("val", VAL_SIZE, RNG_SEED + 99)
    names_path = DATA_ROOT / "classes.txt"
    names_path.write_text("\n".join(CLASSES) + "\n", encoding="utf-8")
    print(f"Generated dataset with classes: {', '.join(CLASSES)}")


if __name__ == "__main__":
    main()
