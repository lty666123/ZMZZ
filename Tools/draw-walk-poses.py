"""画 6 帧行走循环的 OpenPose 风格骨架图（给 ControlNet 用）。
骨架由代码计算，姿势完全可控 —— 这是让 AI 出「真动画帧」的关键。
用法: python draw-walk-poses.py <out_dir>
"""
import math
import os
import sys

from PIL import Image, ImageDraw

out_dir = sys.argv[1] if len(sys.argv) > 1 else "."
os.makedirs(out_dir, exist_ok=True)

W, H = 768, 1152
CX = W // 2
GROUND = 1060          # 脚底（原始坐标）
HIP_Y = 640            # 髋部
SHOULDER_Y = 330       # 肩部
HEAD_R = 52

# 把整个人缩小并下移，给画面留出上下边距 ——
# 否则 ControlNet 会让人物填满画布，头被切掉。
S = 0.66
TOP = 250


def T(p):
    return (CX + (p[0] - CX) * S, TOP + (p[1] - 180) * S)

# OpenPose 常用的配色（近似）
COL = {
    "torso": (255, 85, 0),
    "arm_r": (255, 170, 0),
    "arm_l": (170, 255, 0),
    "leg_r": (0, 170, 255),
    "leg_l": (85, 0, 255),
    "joint": (255, 255, 255),
    "head": (200, 200, 200),
}


def seg(d, p1, p2, color, width=14):
    d.line([T(p1), T(p2)], fill=color, width=max(2, int(width * S)))


def joint(d, p, r=9):
    q = T(p)
    rr = max(2, int(r * S))
    d.ellipse([q[0] - rr, q[1] - rr, q[0] + rr, q[1] + rr], fill=COL["joint"])


def draw_pose(path, t):
    """t = 0..1，一个完整步态周期。侧视，朝右。"""
    img = Image.new("RGB", (W, H), (0, 0, 0))
    d = ImageDraw.Draw(img)

    phase = 2 * math.pi * t
    # 腿的摆动（右腿与左腿反相）
    swing = math.sin(phase)          # -1..1

    hip = (CX, HIP_Y)
    shoulder = (CX, SHOULDER_Y)
    head = (CX, SHOULDER_Y - HEAD_R - 30)

    # 骨盆随步伐轻微上下
    bob = int(14 * abs(math.cos(phase)))
    hip = (hip[0], hip[1] - bob)
    shoulder = (shoulder[0], shoulder[1] - bob)
    head = (head[0], head[1] - bob)

    # 右腿：swing 为正时在前
    def leg(forward, color):
        knee_dx = int(70 * forward)
        ankle_dx = int(130 * forward)
        knee = (hip[0] + knee_dx, hip[1] + 210)
        ankle = (hip[0] + ankle_dx, GROUND - 20)
        seg(d, hip, knee, color)
        joint(d, knee)
        seg(d, knee, ankle, color)
        joint(d, ankle)
        # 脚
        d.line([T(ankle), T((ankle[0] + 55, ankle[1]))], fill=color, width=max(2, int(12 * S)))

    # 手臂：与同侧腿反相
    def arm(backward, color):
        elbow = (shoulder[0] + int(45 * backward), shoulder[1] + 130)
        wrist = (shoulder[0] + int(85 * backward), shoulder[1] + 250)
        seg(d, shoulder, elbow, color)
        joint(d, elbow)
        seg(d, elbow, wrist, color)
        joint(d, wrist)

    leg(+swing, COL["leg_r"])
    leg(-swing, COL["leg_l"])
    arm(-swing, COL["arm_r"])
    arm(+swing, COL["arm_l"])

    # 躯干
    seg(d, hip, shoulder, COL["torso"], width=20)
    joint(d, hip)
    joint(d, shoulder)

    # 头
    hc = T(head)
    hr = int(HEAD_R * S)
    d.ellipse(
        [hc[0] - hr, hc[1] - hr, hc[0] + hr, hc[1] + hr],
        outline=COL["head"],
        width=max(2, int(10 * S)),
    )

    gy = T((0, GROUND))[1]
    d.line([(0, gy), (W, gy)], fill=(40, 40, 40), width=4)
    img.save(path)


# 8 帧一个循环（sin 在 0 → +1 → 0 → -1 → 0 之间平滑过渡，8 帧刚好不重复）
frames = 8
for i in range(frames):
    path = os.path.join(out_dir, "pose_%02d.png" % i)
    draw_pose(path, i / frames)
    print(path)

# 顺带拼一张预览
strip = Image.new("RGB", ((W // 4) * frames, H // 4), (0, 0, 0))
for i in range(frames):
    img = Image.open(os.path.join(out_dir, "pose_%02d.png" % i)).resize((W // 4, H // 4))
    strip.paste(img, (i * (W // 4), 0))
strip_path = os.path.join(out_dir, "pose_strip.png")
strip.save(strip_path)
print("预览:", strip_path)
