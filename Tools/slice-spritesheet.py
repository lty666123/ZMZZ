"""把一张「一行 4 帧」的精灵图切成 4 张帧，再拼出 GIF 预览。
用法: python slice-spritesheet.py <sheet_png> <out_dir> [frames]
"""
import os
import sys

from PIL import Image

sheet_path = sys.argv[1]
out_dir = sys.argv[2]
count = int(sys.argv[3]) if len(sys.argv) > 3 else 4

sheet = Image.open(sheet_path).convert("RGBA")
cell_w = sheet.width // count

frames = []
for i in range(count):
    box = (i * cell_w, 0, (i + 1) * cell_w, sheet.height)
    cell = sheet.crop(box)
    path = os.path.join(out_dir, "sheet_%02d.png" % i)
    cell.save(path)
    frames.append(cell)

# 横向条
strip = Image.new("RGBA", (cell_w * count, sheet.height), (255, 255, 255, 0))
for i, f in enumerate(frames):
    strip.paste(f, (i * cell_w, 0))
strip_path = os.path.join(out_dir, "sheet_strip.png")
strip.save(strip_path)

# GIF 预览
gif_frames = []
for f in frames:
    bg = Image.new("RGB", f.size, (255, 255, 255))
    bg.paste(f, mask=f.split()[3])
    gif_frames.append(bg)
gif_path = os.path.join(out_dir, "sheet_preview.gif")
gif_frames[0].save(
    gif_path,
    save_all=True,
    append_images=gif_frames[1:],
    duration=150,
    loop=0,
    optimize=True,
)

print("切成", count, "帧，每帧", cell_w, "x", sheet.height)
print("条:", strip_path)
print("GIF:", gif_path)
