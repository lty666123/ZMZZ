import os
import sys

from PIL import Image


def main():
    if len(sys.argv) < 4:
        print("usage: gif_to_sheet.py INPUT.gif OUTPUT.png FRAME_STEP [COLUMNS] [SCALE]")
        return 1

    input_path = sys.argv[1]
    output_path = sys.argv[2]
    frame_step = int(sys.argv[3])
    columns = int(sys.argv[4]) if len(sys.argv) >= 5 else 8
    scale = float(sys.argv[5]) if len(sys.argv) >= 6 else 0.5

    image = Image.open(input_path)
    frames = []

    for index in range(0, image.n_frames, frame_step):
        image.seek(index)
        frames.append(image.convert("RGBA"))

    if not frames:
        print("no frames")
        return 1

    frame_width = max(1, int(frames[0].width * scale))
    frame_height = max(1, int(frames[0].height * scale))
    rows = (len(frames) + columns - 1) // columns

    sheet = Image.new(
        "RGBA",
        (columns * frame_width, rows * frame_height),
        (0, 0, 0, 0),
    )

    for index, frame in enumerate(frames):
        resized = frame.resize(
            (frame_width, frame_height),
            Image.Resampling.LANCZOS,
        )
        x = (index % columns) * frame_width
        y = (index // columns) * frame_height
        sheet.alpha_composite(resized, (x, y))

    os.makedirs(os.path.dirname(output_path) or ".", exist_ok=True)
    sheet.save(output_path)

    print("frames={}".format(len(frames)))
    print("frame={}x{}".format(frame_width, frame_height))
    print("sheet={}x{}".format(sheet.width, sheet.height))
    print(output_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
