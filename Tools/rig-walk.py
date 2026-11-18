"""把一张侧视立绘「上下分层 + 按相位摆动」，生成能连贯播放的行走帧。

做法（不依赖 AI）：
  1. 从四角 flood fill 抠掉背景（只抠连通的背景，白色披风/铠甲不会被挖掉）
  2. 以髋部为界线，把人物切成「下半身」和「上半身」两层
  3. 按 8 个相位：下半身来回摆 + 上半身反向轻摆 + 整体轻微上下起伏
  4. 合成 8 帧，输出横条和 GIF

用法: python rig-walk.py <立绘.png> <输出目录> [帧数]
"""
import math
import os
import sys

from PIL import Image, ImageDraw

src = sys.argv[1]
out_dir = sys.argv[2]
frames_n = int(sys.argv[3]) if len(sys.argv) > 3 else 8

os.makedirs(out_dir, exist_ok=True)

base = Image.open(src).convert("RGBA")
W, H = base.size

# ---- 1. 只抠掉连通的背景（保护角色身上的白色）----
for corner in [(0, 0), (W - 1, 0), (0, H - 1), (W - 1, H - 1)]:
    ImageDraw.floodfill(base, corner, (255, 255, 255, 0), thresh=55)

alpha_bbox = base.split()[3].getbbox()
if alpha_bbox is None:
    print("抠图后没剩下东西，检查立绘背景是否接近纯白")
    sys.exit(1)

x0, y0, x1, y1 = alpha_bbox
cx = (x0 + x1) // 2
height = y1 - y0
hip_y = y0 + int(height * 0.55)      # 髋部切线

# ---- 2. 上下分层 ----
upper = Image.new("RGBA", (W, H), (0, 0, 0, 0))
upper.paste(base.crop((0, 0, W, hip_y)), (0, 0))

lower = Image.new("RGBA", (W, H), (0, 0, 0, 0))
lower.paste(base.crop((0, hip_y, W, H)), (0, hip_y))

# ---- 3. 按相位生成帧 ----
out_frames = []
for i in range(frames_n):
    phase = 2 * math.pi * i / frames_n
    swing = math.sin(phase)                       # -1..1 前后摆
    bob = int(7 * abs(math.cos(phase)))           # 过渡帧抬得高一点
    sway = int(5 * swing)                         # 轻微左右晃

    angle_lower = 13 * swing                      # 下半身摆动（度）
    angle_upper = -6 * swing                      # 上半身反向轻摆

    rot_lower = lower.rotate(angle_lower, center=(cx, hip_y), resample=Image.BICUBIC)
    rot_upper = upper.rotate(angle_upper, center=(cx, hip_y), resample=Image.BICUBIC)

    frame = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    frame = Image.alpha_composite(frame, rot_lower)
    frame = Image.alpha_composite(frame, rot_upper)

    # 整体位移（bob 向上、sway 左右）
    moved = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    moved.paste(frame, (sway, -bob))

    # 裁到统一尺寸（以角色为中心）
    cw, ch = int(height * 0.85), int(height * 1.12)
    box = (cx - cw // 2, y0 - int(height * 0.06), cx + cw // 2, y0 - int(height * 0.06) + ch)
    cell = moved.crop(box)
    out_frames.append(cell)

    path = os.path.join(out_dir, "rig_%02d.png" % i)
    cell.save(path)

# ---- 4. 横条 + GIF ----
fw, fh = out_frames[0].size
strip = Image.new("RGBA", (fw * frames_n, fh), (255, 255, 255, 0))
for i, f in enumerate(out_frames):
    strip.paste(f, (i * fw, 0))
strip_path = os.path.join(out_dir, "rig_strip.png")
strip.save(strip_path)

gif_frames = []
for f in out_frames:
    bg = Image.new("RGB", (fw, fh), (245, 245, 248))
    bg.paste(f, mask=f.split()[3])
    gif_frames.append(bg)
gif_path = os.path.join(out_dir, "rig_preview.gif")
gif_frames[0].save(gif_path, save_all=True, append_images=gif_frames[1:],
                   duration=110, loop=0, optimize=True)

print("帧数:", frames_n, " 每帧:", fw, "x", fh)
print("横条:", strip_path)
print("GIF:", gif_path)
