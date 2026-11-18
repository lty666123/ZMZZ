"""把 walk 目录里的逐帧图拼成：① 横向精灵图 ② 可播放的 GIF 预览。
用 ComfyUI 自带的 python（带 Pillow）运行。"""
import glob
import os
import sys

from PIL import Image

walk_dir = sys.argv[1]
out_dir = sys.argv[2]
tag = sys.argv[3] if len(sys.argv) > 3 else "walk"

files = sorted(glob.glob(os.path.join(walk_dir, tag + "_*.png")))
if not files:
    print("没有找到帧文件")
    sys.exit(1)

height = 640
frames = []
for path in files:
    img = Image.open(path).convert("RGBA")
    width = int(img.width * height / img.height)
    frames.append(img.resize((width, height), Image.LANCZOS))

# ① 横向精灵图
total = sum(f.width for f in frames)
strip = Image.new("RGBA", (total, height), (255, 255, 255, 0))
x = 0
for f in frames:
    strip.paste(f, (x, 0))
    x += f.width
strip_path = os.path.join(out_dir, tag + "_strip.png")
strip.save(strip_path)

# ② GIF 预览（白底，8 帧/秒，无限循环）
gif_frames = []
for f in frames:
    bg = Image.new("RGB", f.size, (255, 255, 255))
    bg.paste(f, mask=f.split()[3])
    gif_frames.append(bg)
gif_path = os.path.join(out_dir, tag + "_preview.gif")
gif_frames[0].save(
    gif_path,
    save_all=True,
    append_images=gif_frames[1:],
    duration=125,
    loop=0,
    optimize=True,
)

print("帧数:", len(frames))
print("精灵图:", strip_path)
print("GIF:", gif_path)
