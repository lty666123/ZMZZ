import glob
import os
import re
import sys

from PIL import Image


def frame_number(path):
    match = re.search(r"(\d+)\.png$", path, re.IGNORECASE)
    return int(match.group(1)) if match else 0


def main():
    if len(sys.argv) < 4:
        print("usage: frames_to_sheet.py FRAME_DIR OUTPUT.png STEP [COLUMNS] [SCALE]")
        return 1

    frame_dir = sys.argv[1]
    output_path = sys.argv[2]
    step = int(sys.argv[3])
    columns = int(sys.argv[4]) if len(sys.argv) >= 5 else 8
    scale = float(sys.argv[5]) if len(sys.argv) >= 6 else 0.5

    files = sorted(
        glob.glob(os.path.join(frame_dir, "*.png")),
        key=frame_number,
    )
    files = files[::step]

    if not files:
        print("no frames")
        return 1

    first = Image.open(files[0]).convert("RGBA")
    frame_width = max(1, int(first.width * scale))
    frame_height = max(1, int(first.height * scale))
    rows = (len(files) + columns - 1) // columns

    sheet = Image.new(
        "RGBA",
        (columns * frame_width, rows * frame_height),
        (0, 0, 0, 0),
    )

    for index, path in enumerate(files):
        with Image.open(path) as image:
            frame = image.convert("RGBA").resize(
                (frame_width, frame_height),
                Image.Resampling.LANCZOS,
            )
        x = (index % columns) * frame_width
        y = (index // columns) * frame_height
        sheet.alpha_composite(frame, (x, y))

    os.makedirs(os.path.dirname(output_path) or ".", exist_ok=True)
    sheet.save(output_path)

    print("frames={}".format(len(files)))
    print("frame={}x{}".format(frame_width, frame_height))
    print("sheet={}x{}".format(sheet.width, sheet.height))
    print(output_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
