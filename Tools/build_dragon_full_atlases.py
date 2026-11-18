"""Build full-frame dragon sprite sheets from the original numbered frames.

Each source frame is scaled by 0.5 so one sliced cell is 362 x 222 pixels.
Frames are never skipped. Attack 2 and Death are split into consecutive parts
because their complete animations do not fit in a 4096 x 4096 Unity texture.
"""

import argparse
import json
import math
import re
from pathlib import Path

from PIL import Image


FRAME_SIZE = (362, 222)
SOURCE_SIZE = (725, 445)
COLUMNS = 11

ATLASES = [
    ("dragon_attack1_full.png", "Attack 1", 1, 161),
    ("dragon_attack2_full_part1.png", "Attack 2", 1, 101),
    ("dragon_attack2_full_part2.png", "Attack 2", 102, 202),
    ("dragon_death_full_part1.png", "Death", 1, 151),
    ("dragon_death_full_part2.png", "Death", 152, 301),
    ("dragon_hurt_full.png", "Hurt", 1, 62),
    ("dragon_idle_battle_full.png", "Idle Battle", 1, 140),
    ("dragon_walking_full.png", "Walking", 1, 161),
]


def frame_number(path: Path) -> int:
    match = re.fullmatch(r"(\d+)", path.stem)
    if not match:
        raise ValueError(f"Invalid frame name: {path.name}")
    return int(match.group(1))


def collect_frames(frame_dir: Path, first: int, last: int) -> list[Path]:
    frames = sorted(frame_dir.glob("*.png"), key=frame_number)
    by_number = {frame_number(path): path for path in frames}
    expected = list(range(first, last + 1))
    missing = [number for number in expected if number not in by_number]
    if missing:
        raise ValueError(f"{frame_dir.name}: missing frames {missing[:10]}")
    return [by_number[number] for number in expected]


def build_atlas(frames: list[Path], output_path: Path) -> dict:
    rows = math.ceil(len(frames) / COLUMNS)
    sheet_size = (COLUMNS * FRAME_SIZE[0], rows * FRAME_SIZE[1])
    if sheet_size[0] > 4096 or sheet_size[1] > 4096:
        raise ValueError(f"{output_path.name}: atlas exceeds 4096 x 4096")

    sheet = Image.new("RGBA", sheet_size, (0, 0, 0, 0))
    for index, path in enumerate(frames):
        with Image.open(path) as source:
            if source.size != SOURCE_SIZE:
                raise ValueError(
                    f"{path}: expected {SOURCE_SIZE}, got {source.size}"
                )
            frame = source.convert("RGBA").resize(
                FRAME_SIZE, Image.Resampling.LANCZOS
            )
        x = (index % COLUMNS) * FRAME_SIZE[0]
        y = (index // COLUMNS) * FRAME_SIZE[1]
        sheet.alpha_composite(frame, (x, y))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path)

    return {
        "file": output_path.name,
        "animation": frames[0].parent.name,
        "first_frame": frame_number(frames[0]),
        "last_frame": frame_number(frames[-1]),
        "frame_count": len(frames),
        "cell_size": list(FRAME_SIZE),
        "columns": COLUMNS,
        "rows": rows,
        "grid_size": [COLUMNS, rows],
        "empty_cells": COLUMNS * rows - len(frames),
        "texture_size": list(sheet_size),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--source",
        type=Path,
        default=Path(
            r"E:\unity Prograss\ZMZZ\Assets\Art\Characters\Dragon\OriginalFrames"
        ),
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("Docs/asset_previews/dragon_full_atlases"),
    )
    args = parser.parse_args()

    if not args.source.is_dir():
        raise FileNotFoundError(args.source)

    manifest = []
    for output_name, directory_name, first, last in ATLASES:
        frames = collect_frames(args.source / directory_name, first, last)
        info = build_atlas(frames, args.output / output_name)
        manifest.append(info)
        print(
            "{file}: frames {first_frame}-{last_frame}, "
            "grid {columns}x{rows}, texture {w}x{h}, empty {empty_cells}".format(
                **info,
                w=info["texture_size"][0],
                h=info["texture_size"][1],
            )
        )

    manifest_path = args.output / "manifest.json"
    manifest_path.write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8"
    )
    print(f"Manifest: {manifest_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
