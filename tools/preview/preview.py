"""Draws the building models the check tool dumps (dotnet run -- dump <types> <dir>) as a contact sheet of small
3/4 views, so new models can be looked over without opening Unity.

    python tools/preview/preview.py <dump dir> <out.png> [columns]
"""
import glob
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
TEX = os.path.join(ROOT, 'Assets', 'Resources', 'RealLife', 'Tex')
_avg = {}


def tex_avg(name):
    if name not in _avg:
        p = os.path.join(TEX, name + '.png')
        if os.path.exists(p):
            a = np.asarray(Image.open(p).convert('RGB').resize((32, 32)), np.float32) / 255.0
            _avg[name] = a.reshape(-1, 3).mean(0)
        else:
            _avg[name] = None
    return _avg[name]


def mat_color(name, base):
    base = np.array(base, np.float32)
    if name.endswith('_(missing)'):
        # (the check tool can't load textures: the material is named for its texture)
        name = name[:-len('_(missing)')]
        base = np.array([1, 1, 1], np.float32)
    if name.startswith('Col_') or name in ('Glow', 'Glass', '?'):
        return base
    tex = name.split('#')[0]
    a = tex_avg(tex)
    if a is None:
        return base
    c = a * base
    if '#dark#' in name:
        c = c * 0.6
    return c


def load(path):
    tris, cols = [], []
    size = (10, 10)
    cur = np.array([1, 1, 1], np.float32)
    for line in open(path):
        p = line.split()
        if not p:
            continue
        if p[0] == 'S':
            size = (float(p[1]), float(p[2]))
        elif p[0] == 'M':
            cur = mat_color(p[1], [float(x) for x in p[2:5]])
        elif p[0] == 'T':
            v = np.array([float(x) for x in p[1:10]], np.float32).reshape(3, 3)
            tris.append(v)
            cols.append(cur)
    return size, tris, cols


def render(path, px=360):
    size, tris, cols = load(path)
    w, d = size
    ext = max(w, d) * 0.5 + 6
    # camera: from the street side (-z), to the left and above
    yaw, pitch = math.radians(-35), math.radians(32)
    fwd = np.array([math.sin(yaw) * math.cos(pitch) * -1, -math.sin(pitch), math.cos(yaw) * math.cos(pitch)], np.float32)
    fwd = np.array([-math.sin(yaw) * math.cos(pitch), -math.sin(pitch), math.cos(yaw) * math.cos(pitch)], np.float32)
    right = np.cross(np.array([0, 1, 0], np.float32), fwd); right /= np.linalg.norm(right)
    up = np.cross(fwd, right)
    light = np.array([-0.4, 0.85, -0.35], np.float32); light /= np.linalg.norm(light)
    # the lot
    ground = np.array([[-w / 2, 0, -d / 2], [w / 2, 0, -d / 2], [w / 2, 0, d / 2], [-w / 2, 0, d / 2]], np.float32)
    # fit the whole thing in the frame
    pts = np.concatenate([np.array(tris).reshape(-1, 3), ground]) if tris else ground
    xs, ys = pts @ right, pts @ up
    span = max(xs.max() - xs.min(), ys.max() - ys.min(), 1.0)
    scale = px * 0.9 / span
    cx, cy = (xs.max() + xs.min()) / 2, (ys.max() + ys.min()) / 2

    ss = 2   # drawn at twice the size and scaled down, for smoother edges
    W = px * ss
    sc = scale * ss

    def proj(p):
        return (W / 2 + (float(np.dot(p, right)) - cx) * sc, W / 2 - (float(np.dot(p, up)) - cy) * sc)
    rgb = np.zeros((W, W, 3), np.float32)
    rgb[:] = np.array([206, 214, 200], np.float32)
    zbuf = np.full((W, W), np.inf, np.float32)

    def fill(v2, z, col):
        # a triangle into the depth buffer: v2 its screen corners, z their depths
        (x0, y0), (x1, y1), (x2, y2) = v2
        minx, maxx = max(int(math.floor(min(x0, x1, x2))), 0), min(int(math.ceil(max(x0, x1, x2))), W - 1)
        miny, maxy = max(int(math.floor(min(y0, y1, y2))), 0), min(int(math.ceil(max(y0, y1, y2))), W - 1)
        if minx > maxx or miny > maxy:
            return
        den = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
        if abs(den) < 1e-9:
            return
        gx, gy = np.meshgrid(np.arange(minx, maxx + 1) + 0.5, np.arange(miny, maxy + 1) + 0.5)
        a = ((y1 - y2) * (gx - x2) + (x2 - x1) * (gy - y2)) / den
        b = ((y2 - y0) * (gx - x2) + (x0 - x2) * (gy - y2)) / den
        c = 1 - a - b
        inside = (a >= -1e-4) & (b >= -1e-4) & (c >= -1e-4)
        if not inside.any():
            return
        zz = a * z[0] + b * z[1] + c * z[2]
        sub = zbuf[miny:maxy + 1, minx:maxx + 1]
        win = inside & (zz < sub)
        sub[win] = zz[win]
        rgb[miny:maxy + 1, minx:maxx + 1][win] = col
    gz = [float(np.dot(p, fwd)) + 0.02 for p in ground]
    gp = [proj(p) for p in ground]
    fill([gp[0], gp[1], gp[2]], [gz[0], gz[1], gz[2]], (150, 156, 150))
    fill([gp[0], gp[2], gp[3]], [gz[0], gz[2], gz[3]], (150, 156, 150))
    for v, c in zip(tris, cols):
        n = np.cross(v[1] - v[0], v[2] - v[0])
        ln = np.linalg.norm(n)
        if ln < 1e-6:
            continue
        n /= ln
        if np.dot(n, fwd) > 0.02:
            continue   # facing away
        shade = 0.45 + 0.55 * max(0.0, float(np.dot(n, light)))
        col = tuple(int(max(0, min(1, x * shade)) * 255) for x in c)
        fill([proj(p) for p in v], [float(np.dot(p, fwd)) for p in v], col)
    img = Image.fromarray(rgb.astype(np.uint8)).resize((px, px), Image.LANCZOS)
    return img


def main():
    src, out = sys.argv[1], sys.argv[2]
    cols = int(sys.argv[3]) if len(sys.argv) > 3 else 6
    files = sorted(glob.glob(os.path.join(src, '*.txt')))
    px = 300
    rows = (len(files) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * px, rows * (px + 22)), (40, 40, 44))
    dr = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 14)
    except OSError:
        font = None
    for i, f in enumerate(files):
        im = render(f, px)
        x, y = (i % cols) * px, (i // cols) * (px + 22)
        sheet.paste(im, (x, y + 22))
        dr.text((x + 6, y + 3), os.path.basename(f)[:-4], fill=(240, 240, 240), font=font)
    sheet.save(out)
    print('wrote', out, len(files))


if __name__ == '__main__':
    main()
