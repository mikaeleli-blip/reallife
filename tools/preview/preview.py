"""Draws the building models the check tool dumps (dotnet run -- dump <types> <dir>) as a contact sheet of small
3/4 views, so models can be looked over without opening Unity. Textured faces are drawn with their textures (the dump
carries each corner's texture coordinates), tinted the way the game tints them.

    python tools/preview/preview.py <dump dir> <out.png> [columns] [--px 300] [--views front,back] [--only A,B]

Views: front (from the street, to the left and above), back (from behind, to the right), side, top.
"""
import glob
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
TEX = os.path.join(ROOT, 'Assets', 'Resources', 'RealLife', 'Tex')
_tex = {}

VIEWS = {'front': (-35, 32), 'back': (145, 32), 'side': (-90, 20), 'top': (-20, 70), 'low': (-25, 12)}


def texture(name):
    """A texture as a float array (rows from the top), or None."""
    if name not in _tex:
        p = os.path.join(TEX, name + '.png')
        if os.path.exists(p):
            im = Image.open(p).convert('RGB')
            if max(im.size) > 512:
                im = im.resize((512, 512 * im.size[1] // im.size[0]), Image.BILINEAR)
            _tex[name] = np.asarray(im, np.float32) / 255.0
        else:
            _tex[name] = None
    return _tex[name]


def material(name, base):
    """(texture array or None, colour multiplier)."""
    base = np.array(base, np.float32)
    if name.endswith('_(missing)'):
        # (the check tool can't load textures: the material is named for its texture)
        name = name[:-len('_(missing)')]
        base = np.array([1, 1, 1], np.float32)
    if name.startswith('Col_') or name in ('Glow', 'Glass', '?'):
        return None, base
    t = texture(name.split('#')[0])
    if t is None:
        return None, base
    if '#dark#' in name:
        base = base * 0.6
    return t, base


def load(path):
    tris, uvs, mats = [], [], []
    size = (10, 10)
    cur = (None, np.array([1, 1, 1], np.float32))
    for line in open(path):
        p = line.split()
        if not p:
            continue
        if p[0] == 'S':
            size = (float(p[1]), float(p[2]))
        elif p[0] == 'M':
            cur = material(p[1], [float(x) for x in p[2:5]])
        elif p[0] == 'T':
            n = [float(x) for x in p[1:]]
            tris.append(np.array(n[:9], np.float32).reshape(3, 3))
            uvs.append(np.array(n[9:15], np.float32).reshape(3, 2) if len(n) >= 15 else None)
            mats.append(cur)
    return size, tris, uvs, mats


def render(path, px=360, view='front'):
    size, tris, uvs, mats = load(path)
    w, d = size
    yaw, pitch = (math.radians(a) for a in VIEWS.get(view, VIEWS['front']))
    fwd = np.array([-math.sin(yaw) * math.cos(pitch), -math.sin(pitch), math.cos(yaw) * math.cos(pitch)], np.float32)
    right = np.cross(np.array([0, 1, 0], np.float32), fwd); right /= np.linalg.norm(right)
    up = np.cross(fwd, right)
    light = np.array([-0.45, 0.8, -0.4], np.float32); light /= np.linalg.norm(light)
    # the lot, with a margin of street around it
    m = 2.0
    ground = np.array([[-w / 2 - m, 0, -d / 2 - m], [w / 2 + m, 0, -d / 2 - m], [w / 2 + m, 0, d / 2 + m], [-w / 2 - m, 0, d / 2 + m]], np.float32)
    pts = np.concatenate([np.array(tris).reshape(-1, 3), ground]) if tris else ground
    xs, ys = pts @ right, pts @ up
    span = max(xs.max() - xs.min(), ys.max() - ys.min(), 1.0)
    scale = px * 0.94 / span
    cx, cy = (xs.max() + xs.min()) / 2, (ys.max() + ys.min()) / 2

    ss = 2   # drawn at twice the size and scaled down, for smoother edges
    W = px * ss
    sc = scale * ss

    def proj(p):
        return (W / 2 + (float(np.dot(p, right)) - cx) * sc, W / 2 - (float(np.dot(p, up)) - cy) * sc)
    rgb = np.zeros((W, W, 3), np.float32)
    rgb[:] = np.array([0.80, 0.84, 0.86], np.float32)
    zbuf = np.full((W, W), np.inf, np.float32)

    def fill(v2, z, col, tex=None, uv=None):
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
        win = inside & (zz < sub - 1e-4)
        if not win.any():
            return
        sub[win] = zz[win]
        tgt = rgb[miny:maxy + 1, minx:maxx + 1]
        if tex is not None and uv is not None:
            u = a * uv[0, 0] + b * uv[1, 0] + c * uv[2, 0]
            v = a * uv[0, 1] + b * uv[1, 1] + c * uv[2, 1]
            th, tw = tex.shape[:2]
            tx = (np.mod(u[win], 1.0) * tw).astype(np.int32) % tw
            ty = ((1.0 - np.mod(v[win], 1.0)) * th).astype(np.int32) % th
            tgt[win] = tex[ty, tx] * col
        else:
            tgt[win] = col
    # the street around the lot, then the lot
    gz = [float(np.dot(p, fwd)) + 0.03 for p in ground]
    gp = [proj(p) for p in ground]
    street = np.array([0.5, 0.52, 0.54], np.float32)
    fill([gp[0], gp[1], gp[2]], [gz[0], gz[1], gz[2]], street)
    fill([gp[0], gp[2], gp[3]], [gz[0], gz[2], gz[3]], street)
    lot = np.array([[-w / 2, 0, -d / 2], [w / 2, 0, -d / 2], [w / 2, 0, d / 2], [-w / 2, 0, d / 2]], np.float32)
    lz = [float(np.dot(p, fwd)) + 0.02 for p in lot]
    lp = [proj(p) for p in lot]
    grass = np.array([0.45, 0.58, 0.36], np.float32)
    fill([lp[0], lp[1], lp[2]], [lz[0], lz[1], lz[2]], grass)
    fill([lp[0], lp[2], lp[3]], [lz[0], lz[2], lz[3]], grass)
    for v, uv, (tex, base) in zip(tris, uvs, mats):
        n = np.cross(v[1] - v[0], v[2] - v[0])
        ln = np.linalg.norm(n)
        if ln < 1e-6:
            continue
        n /= ln
        if np.dot(n, fwd) > 0.02:
            continue   # facing away
        shade = 0.5 + 0.55 * max(0.0, float(np.dot(n, light))) + 0.08 * max(0.0, float(n[1]))
        col = np.clip(base * shade, 0, 1.4)
        fill([proj(p) for p in v], [float(np.dot(p, fwd)) for p in v], col, tex, uv)
    img = Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8)).resize((px, px), Image.LANCZOS)
    return img


def main():
    args = sys.argv[1:]
    opts = {}
    pos = []
    i = 0
    while i < len(args):
        if args[i].startswith('--'):
            opts[args[i][2:]] = args[i + 1]
            i += 2
        else:
            pos.append(args[i])
            i += 1
    src, out = pos[0], pos[1]
    cols = int(pos[2]) if len(pos) > 2 else 6
    px = int(opts.get('px', 300))
    views = opts.get('views', 'front').split(',')
    files = sorted(glob.glob(os.path.join(src, '*.txt')))
    if 'only' in opts:
        want = opts['only'].split(',')
        files = [f for f in files if os.path.basename(f)[:-4] in want]
    tile = px * len(views)
    rows = (len(files) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * tile, rows * (px + 22)), (40, 40, 44))
    dr = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 14)
    except OSError:
        font = None
    for i, f in enumerate(files):
        x, y = (i % cols) * tile, (i // cols) * (px + 22)
        for k, v in enumerate(views):
            sheet.paste(render(f, px, v), (x + k * px, y + 22))
        dr.text((x + 6, y + 3), os.path.basename(f)[:-4], fill=(240, 240, 240), font=font)
    sheet.save(out)
    print('wrote', out, len(files))


if __name__ == '__main__':
    main()
