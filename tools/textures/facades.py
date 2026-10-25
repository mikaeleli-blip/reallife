"""Facade atlases for a modern town.

Upper floors: 4 bays x 4 floors per texture; ground floors: 4 bays x 1 floor. Each atlas cell is one window bay of one
floor. Buildings map one cell per bay and start at a random bay/floor, so the 16 variations (blinds, curtains, plants,
lit or dark) never line up between buildings. The _e emission maps light the windows at night.

Apartment atlases put a balcony door in columns 1 and 3, so the game can hang a balcony in front of exactly those bays.
"""
import numpy as np
from texlib import *

BW = 256          # bay width in px (one bay is ~2.7 m, so ~95 px/m)
FH = 300          # floor height in px (~3.1 m)

LIGHT_WARM = rgb('#ffc27a')
LIGHT_SOFT = rgb('#ffe2b0')
LIGHT_COOL = rgb('#e8f0ff')
LIGHT_TV = rgb('#9fc3ff')

CURTAINS = [rgb('#e9e4d8'), rgb('#d8d2c2'), rgb('#9aa7b4'), rgb('#5f6f7d'), rgb('#c7b28a'), rgb('#7d8f72'), rgb('#b9806a')]
BLINDS = [rgb('#ecebe6'), rgb('#d9d6cf'), rgb('#c4c0b6')]


def pick(rng, arr):
    return arr[int(rng.integers(len(arr)))]


def lit_choice(rng, frac):
    lit = rng.random() < frac
    r = rng.random()
    light = LIGHT_WARM if r < 0.6 else (LIGHT_SOFT if r < 0.85 else (LIGHT_TV if r < 0.93 else LIGHT_COOL))
    return lit, light


# ---------------------------------------------------------------- glass and what's behind it

def glass(c, x0, y0, x1, y1, rng, lit, light, interior=None, tint=None, office=False):
    """Clear modern glass with sky reflection, an interior (blinds, curtains, plant, ceiling) and night emission."""
    x0, y0, x1, y1 = int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))
    if x1 <= x0 or y1 <= y0:
        return
    h, w = y1 - y0, x1 - x0
    t = np.linspace(0, 1, h, dtype=np.float32)[:, None] * np.ones((1, w), np.float32)
    s = np.linspace(0, 1, w, dtype=np.float32)[None, :] * np.ones((h, 1), np.float32)
    # a diagonal sky streak and a darker lower half, like a real window seen from the street
    refl = (1 - t) * 0.5 + 0.35 * np.clip(1 - np.abs((s - t * 0.7) - 0.15) * 5, 0, 1)
    sky = rgb('#8fb3cf') if tint is None else tint
    base = rgb('#1b2633') if tint is None else tint * 0.25
    alb = lerp(fill(h, w, base), fill(h, w, sky), refl * 0.55)
    emit = np.zeros((h, w, 3), np.float32)
    if lit:
        glow = (0.65 + 0.35 * (1 - t)) * (0.85 + 0.3 * rng.random())
        emit = light[None, None, :] * glow[..., None]
        alb = lerp(alb, fill(h, w, light * 0.5), 0.35)

    if interior is None:
        interior = pick(rng, ['dark', 'blinds', 'blinds', 'curtain', 'curtain', 'plant', 'sheer', 'dark'])
    if interior == 'curtain':
        col = pick(rng, CURTAINS)
        cw = int(w * rng.uniform(0.14, 0.28))
        folds = 0.8 + 0.2 * np.sin(s * w / 3.0)
        for a, b in ((0, cw), (w - cw, w)):
            alb[:, a:b] = lerp(alb[:, a:b], col * folds[:, a:b, None], 0.8)
            if lit:
                emit[:, a:b] = light * col * 0.8 * folds[:, a:b, None]
    elif interior == 'sheer':
        col = rgb('#f1efe8')
        alb = lerp(alb, fill(h, w, col) * (0.85 + 0.15 * np.sin(s * w / 2.0))[..., None], 0.5)
        if lit:
            emit = light * col * 0.9
    elif interior == 'blinds':
        col = pick(rng, BLINDS)
        bh = int(h * rng.uniform(0.25, 0.95))
        stripes = ((np.arange(bh) % 6) < 4).astype(np.float32)[:, None, None]
        alb[:bh] = lerp(alb[:bh], fill(bh, w, col) * (0.82 + 0.18 * stripes), 0.85)
        alb[bh - 3:bh] *= 0.6
        if lit:
            emit[:bh] = light * col * (0.35 + 0.65 * (1 - stripes))
    elif interior == 'plant' and h > 60 and w > 40:
        # a pot plant on the sill
        px = int(w * rng.uniform(0.2, 0.7))
        ph = int(h * 0.35)
        pot = rgb('#b5643d')
        alb[h - 14:h - 2, max(0, px - 9):px + 9] = pot
        yy, xx = np.mgrid[0:ph, 0:36]
        blob = ((xx - 18) ** 2 / 300 + (yy - ph * 0.55) ** 2 / (ph * ph * 0.22)) < 1
        y0p, x0p = h - 14 - ph, px - 18
        for (yy2, xx2) in zip(*np.nonzero(blob)):
            Y, X = y0p + yy2, x0p + xx2
            if 0 <= Y < h and 0 <= X < w:
                alb[Y, X] = rgb('#3d6b35') * (0.8 + 0.4 * ((yy2 * 7 + xx2 * 3) % 5) / 5)
        if lit:
            emit[h - 14 - ph:h - 2, max(0, px - 18):px + 18] *= 0.3
    if office:
        # ceiling light strips and the edge of desks
        for k in range(2):
            yy = int(h * (0.08 + k * 0.05))
            alb[yy:yy + 3] = lerp(alb[yy:yy + 3], fill(3, w, rgb('#d9dde2')), 0.6)
            if lit:
                emit[yy:yy + 3] = LIGHT_COOL * 1.0
        dy = int(h * 0.72)
        alb[dy:dy + 4] = lerp(alb[dy:dy + 4], fill(4, w, rgb('#3a3d42')), 0.7)

    c.rect(x0, y0, x1, y1, albedo=alb, height=0.25, smooth=0.92, metal=0.0, emit=emit)


def frame_box(c, x0, y0, x1, y1, col, t=6, depth=0.48, smooth=0.4, metal=0.0):
    """A window frame: four bars round an opening."""
    c.rect(x0, y0, x1, y0 + t, albedo=col, height=depth, smooth=smooth, metal=metal)
    c.rect(x0, y1 - t, x1, y1, albedo=col, height=depth, smooth=smooth, metal=metal)
    c.rect(x0, y0, x0 + t, y1, albedo=col, height=depth, smooth=smooth, metal=metal)
    c.rect(x1 - t, y0, x1, y1, albedo=col, height=depth, smooth=smooth, metal=metal)


def casement(c, x0, y0, x1, y1, frame_col, rng, lit, light, split=True, interior=None):
    """Modern casement window: a reveal, a frame, one or two lights."""
    c.rect(x0 - 4, y0 - 4, x1 + 4, y1 + 4, albedo=rgb('#2a2b2d'), height=0.2, smooth=0.1)     # reveal shadow
    c.rect(x0, y0, x1, y1, albedo=frame_col, height=0.42, smooth=0.4)
    if interior is None:
        interior = pick(rng, ['dark', 'blinds', 'blinds', 'curtain', 'curtain', 'plant', 'sheer', 'dark'])
    f = 7
    if split:
        m = (x0 + x1) / 2
        glass(c, x0 + f, y0 + f, m - 3, y1 - f, rng, lit, light, interior)
        glass(c, m + 3, y0 + f, x1 - f, y1 - f, rng, lit, light, interior)
    else:
        glass(c, x0 + f, y0 + f, x1 - f, y1 - f, rng, lit, light, interior)
    c.albedo[int(y0 + f):int(y0 + f + 5), int(x0 + f):int(x1 - f)] *= 0.6                     # head shadow


def sill(c, x0, x1, y, color, hgt=8, shadow=9):
    c.rect(x0, y, x1, y + hgt, albedo=color, height=0.85, smooth=0.3)
    c.rect(x0, y + hgt - 2, x1, y + hgt, albedo=color * 0.7)
    c.albedo[int(y + hgt):int(y + hgt + shadow), int(x0):int(x1)] *= np.linspace(0.55, 1.0, int(shadow))[:, None, None]


def floor_cells(rows, cols):
    for j in range(rows):
        for i in range(cols):
            yield i, j, i * BW, j * FH, (i + 1) * BW, (j + 1) * FH


def render(h, w, color, seed, var=0.04):
    """Smooth painted render (stucco) with a faint trowel texture."""
    n = fbm(h, w, beta=1.8, lo=2, seed=seed)
    fine = fbm(h, w, beta=0.4, lo=80, seed=seed + 1)
    alb = fill(h, w, color) * (1 - var + 2 * var * n + 0.03 * fine)[..., None]
    hgt = 0.5 + 0.06 * fine + 0.04 * n
    return alb.astype(np.float32), hgt.astype(np.float32)


def panels(h, w, color, seed, pw, ph, joint=3, var=0.03):
    """Composite or precast panels with dark joints."""
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:h, 0:w]
    ids = (yy // ph) * 97 + (xx // pw)
    tone = rng.uniform(1 - var, 1 + var, ids.max() + 1)[ids]
    n = fbm(h, w, beta=1.6, lo=2, seed=seed)
    alb = fill(h, w, color) * (tone * (0.96 + 0.06 * n))[..., None]
    j = ((xx % pw) < joint) | ((yy % ph) < joint)
    alb[j] *= 0.5
    hgt = np.where(j, 0.3, 0.55 + 0.02 * n)
    return alb.astype(np.float32), hgt.astype(np.float32)


# ---------------------------------------------------------------- apartments

def apartment(name, wall, frame_col, seed, out, brick=None, trim=None):
    """Mid-rise apartment block: a wide window per bay, balcony doors in columns 1 and 3."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    H, W = FH * 4, BW * 4
    c = Canvas(H, W)
    if brick is not None:
        alb, hgt, _ = brick_field(H, W, courses=4 * 40, bricks_per_row=4 * 11, colors=brick, seed=seed,
                                  mortar_color=rgb('#b7b0a4'), var=0.08)
    else:
        alb, hgt = render(H, W, wall, seed)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.14
    trim = wall * 1.15 if trim is None else trim
    for i, j, x0, y0, x1, y1 in floor_cells(4, 4):
        cx = (x0 + x1) / 2
        lit, light = lit_choice(rng, 0.5)
        # a slab edge line at every floor
        c.rect(x0, y1 - 10, x1, y1, albedo=trim * 0.95, height=0.62)
        if i % 2 == 1:
            # balcony door: full-height sliding glass, with a narrow fixed light beside it
            dw, dt, db = 150, y0 + 40, y1 - 14
            c.rect(cx - dw / 2 - 5, dt - 5, cx + dw / 2 + 5, db, albedo=rgb('#2a2b2d'), height=0.2)
            c.rect(cx - dw / 2, dt, cx + dw / 2, db, albedo=frame_col, height=0.42, smooth=0.4)
            inter = pick(rng, ['curtain', 'sheer', 'blinds', 'dark'])
            glass(c, cx - dw / 2 + 7, dt + 7, cx - 3, db - 4, rng, lit, light, inter)
            glass(c, cx + 3, dt + 7, cx + dw / 2 - 7, db - 4, rng, lit, light, inter)
            c.rect(cx - 3, dt, cx + 3, db, albedo=frame_col * 0.9, height=0.46)
        else:
            ww, wt, wb = 140, y0 + 70, y1 - 82
            casement(c, cx - ww / 2, wt, cx + ww / 2, wb, frame_col, rng, lit, light, split=True)
            sill(c, cx - ww / 2 - 8, cx + ww / 2 + 8, wb, trim)
    c.grime(amount=0.08, streaks=0.06, seed=seed + 11)
    c.save(f'{out}/{name}', normal_strength=4.0, save_emit=True)


def apartment_base(name, wall, frame_col, seed, out):
    """Ground floor of an apartment block: a stone base, windows, and the entrance in column 2."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    G = 360
    H, W = G, BW * 4
    c = Canvas(H, W)
    alb, hgt = stone_blocks(H, W, rows=6, cols=4 * 2, joint_px=2, color=wall, var=0.03, seed=seed,
                            joint_depth=0.12, rough=0.04)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.2
    for i in range(4):
        x0, x1 = i * BW, (i + 1) * BW
        cx = (x0 + x1) / 2
        lit, light = lit_choice(rng, 0.55)
        if i == 2:
            c.rect(cx - 74, 54, cx + 74, H, albedo=rgb('#2a2b2d'), height=0.2)
            c.rect(cx - 70, 58, cx + 70, H, albedo=frame_col, height=0.42, smooth=0.45, metal=0.3)
            glass(c, cx - 62, 66, cx + 62, 120, rng, True, LIGHT_SOFT, 'dark')
            glass(c, cx - 62, 128, cx - 3, H - 8, rng, True, LIGHT_SOFT, 'dark')
            glass(c, cx + 3, 128, cx + 62, H - 8, rng, True, LIGHT_SOFT, 'dark')
            c.rect(cx - 12, 220, cx - 6, 300, albedo=rgb('#c9ccd0'), metal=0.9, smooth=0.7, height=0.6)
            c.rect(cx + 6, 220, cx + 12, 300, albedo=rgb('#c9ccd0'), metal=0.9, smooth=0.7, height=0.6)
        else:
            casement(c, cx - 70, 110, cx + 70, 262, frame_col, rng, lit, light, split=True)
            sill(c, cx - 78, cx + 78, 262, wall * 1.15)
    c.rect(0, H - 12, W, H, albedo=wall * 0.7, height=0.6)
    c.grime(amount=0.12, streaks=0.05, seed=seed + 11)
    c.save(f'{out}/{name}', normal_strength=4.0, save_emit=True)


# ---------------------------------------------------------------- offices

def curtain_wall(name, glass_tint, frame_col, spandrel, seed, out):
    """A glass office tower: full-height glazing in every bay, mullions and a spandrel band at each floor."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    H, W = FH * 4, BW * 4
    c = Canvas(H, W)
    c.albedo = fill(H, W, frame_col)
    c.height[:] = 0.55
    c.smooth[:] = 0.6
    c.metal[:] = 0.6
    for i, j, x0, y0, x1, y1 in floor_cells(4, 4):
        lit, light = lit_choice(rng, 0.55)
        light = LIGHT_COOL if rng.random() < 0.8 else LIGHT_SOFT
        sp = 70                                          # spandrel height
        c.rect(x0, y1 - sp, x1, y1, albedo=spandrel, height=0.5, smooth=0.75, metal=0.2)
        inter = pick(rng, ['dark', 'blinds', 'dark', 'blinds', 'dark'])
        mid = (x0 + x1) / 2
        for a, b in ((x0 + 5, mid - 3), (mid + 3, x1 - 5)):
            glass(c, a, y0 + 4, b, y1 - sp - 3, rng, lit, light, inter, tint=glass_tint, office=True)
        c.rect(mid - 3, y0, mid + 3, y1 - sp, albedo=frame_col * 0.9, height=0.62, metal=0.7, smooth=0.6)
    c.save(f'{out}/{name}', normal_strength=2.5, save_emit=True)


def office_precast(name, color, frame_col, seed, out):
    """Precast concrete offices with ribbon windows."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    H, W = FH * 4, BW * 4
    c = Canvas(H, W)
    alb, hgt = panels(H, W, color, seed, BW, FH, joint=3)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.25
    for i, j, x0, y0, x1, y1 in floor_cells(4, 4):
        lit, light = lit_choice(rng, 0.5)
        light = LIGHT_COOL
        wt, wb = y0 + 60, y1 - 90
        c.rect(x0, wt - 4, x1, wb + 4, albedo=rgb('#2a2b2d'), height=0.2)
        c.rect(x0, wt, x1, wb, albedo=frame_col, height=0.42, metal=0.6, smooth=0.5)
        for a in range(4):
            gx0 = x0 + a * BW / 4
            glass(c, gx0 + 4, wt + 5, gx0 + BW / 4 - 4, wb - 5, rng, lit, light, pick(rng, ['dark', 'blinds']), office=True)
        sill(c, x0, x1, wb + 4, color * 1.06, hgt=6, shadow=6)
    c.grime(amount=0.12, streaks=0.18, seed=seed + 11)
    c.save(f'{out}/{name}', normal_strength=3.0, save_emit=True)


# ---------------------------------------------------------------- public buildings

def civic(name, brick, stone, frame_col, seed, out):
    """Schools, stations, town hall wings: brick with tall windows and stone heads."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    H, W = FH * 4, BW * 4
    c = Canvas(H, W)
    alb, hgt, _ = brick_field(H, W, courses=4 * 40, bricks_per_row=4 * 11, colors=brick, seed=seed,
                              mortar_color=rgb('#c4bcae'), var=0.08)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.13
    for i, j, x0, y0, x1, y1 in floor_cells(4, 4):
        cx = (x0 + x1) / 2
        lit, light = lit_choice(rng, 0.35)
        light = LIGHT_COOL if rng.random() < 0.6 else LIGHT_SOFT
        ww, wt, wb = 170, y0 + 50, y1 - 70
        c.rect(cx - ww / 2 - 4, wt - 4, cx + ww / 2 + 4, wb + 4, albedo=rgb('#2a2b2d'), height=0.2)
        c.rect(cx - ww / 2, wt, cx + ww / 2, wb, albedo=frame_col, height=0.42, smooth=0.4)
        tb = wt + 50
        for k in range(3):
            a = cx - ww / 2 + 7 + k * (ww - 14) / 3
            b = a + (ww - 14) / 3
            glass(c, a + 2, wt + 7, b - 2, tb - 3, rng, lit, light, 'dark')
            glass(c, a + 2, tb + 3, b - 2, wb - 7, rng, lit, light, pick(rng, ['blinds', 'dark', 'blinds']))
        c.rect(cx - ww / 2 - 10, wt - 24, cx + ww / 2 + 10, wt - 4, albedo=stone, height=0.78, smooth=0.25)
        sill(c, cx - ww / 2 - 10, cx + ww / 2 + 10, wb, stone)
    c.grime(amount=0.1, streaks=0.1, seed=seed + 11)
    c.save(f'{out}/{name}', normal_strength=4.0, save_emit=True)


def clinic(name, panel_col, accent, frame_col, seed, out):
    """White composite-panel health building with grouped windows and coloured fins."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    H, W = FH * 4, BW * 4
    c = Canvas(H, W)
    alb, hgt = panels(H, W, panel_col, seed, BW // 2, FH // 2, joint=3, var=0.015)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.45
    for i, j, x0, y0, x1, y1 in floor_cells(4, 4):
        lit, light = lit_choice(rng, 0.55)
        light = LIGHT_COOL
        wt, wb = y0 + 55, y1 - 75
        casement(c, x0 + 30, wt, x1 - 50, wb, frame_col, rng, lit, light, split=True, interior=pick(rng, ['blinds', 'blinds', 'dark']))
        c.rect(x1 - 34, y0, x1 - 14, y1, albedo=accent, height=0.8, smooth=0.5)
    c.save(f'{out}/{name}', normal_strength=3.0, save_emit=True)


# ---------------------------------------------------------------- industry

def metal_cladding(name, color, seed, out, windows=True):
    """Vertical ribbed metal cladding with a strip of clerestory windows high on each floor."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    H, W = FH * 4, BW * 4
    c = Canvas(H, W)
    xx = np.mgrid[0:H, 0:W][1].astype(np.float32)
    rib = 0.5 + 0.5 * np.cos((xx % 32) / 32 * 2 * np.pi)
    n = fbm(H, W, beta=1.6, lo=2, seed=seed)
    c.albedo = fill(H, W, color) * (0.86 + 0.14 * rib + 0.06 * n)[..., None]
    c.height = 0.45 + 0.2 * rib
    c.smooth[:] = 0.5
    c.metal[:] = 0.65
    for i, j, x0, y0, x1, y1 in floor_cells(4, 4):
        c.rect(x0, y1 - 6, x1, y1, albedo=color * 0.6, height=0.7)              # floor flashing
        if windows and j % 2 == 0:
            lit, light = lit_choice(rng, 0.45)
            light = LIGHT_COOL
            wt, wb = y0 + 40, y0 + 110
            c.rect(x0, wt - 4, x1, wb + 4, albedo=rgb('#2a2b2d'), height=0.2)
            for a in range(2):
                gx = x0 + a * BW / 2
                glass(c, gx + 6, wt, gx + BW / 2 - 6, wb, rng, lit, light, 'dark')
    c.grime(amount=0.12, streaks=0.3, seed=seed + 11)
    c.save(f'{out}/{name}', normal_strength=3.0, save_emit=True)


def tiltup(name, color, accent, seed, out):
    """Concrete tilt-up panels with a painted band and a few small windows."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    H, W = FH * 4, BW * 4
    c = Canvas(H, W)
    alb, hgt = panels(H, W, color, seed, BW, FH * 2, joint=4, var=0.02)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.22
    for i, j, x0, y0, x1, y1 in floor_cells(4, 4):
        if j % 2 == 0:
            c.rect(x0, y0 + 30, x1, y0 + 46, albedo=accent, height=0.5)
        if j % 2 == 1 and i % 2 == 0:
            lit, light = lit_choice(rng, 0.4)
            casement(c, x0 + 70, y0 + 80, x1 - 70, y1 - 90, rgb('#3b3e42'), rng, lit, LIGHT_COOL, split=False, interior='blinds')
    c.grime(amount=0.15, streaks=0.3, seed=seed + 11)
    c.save(f'{out}/{name}', normal_strength=3.0, save_emit=True)


# ---------------------------------------------------------------- ground floors (4 bays x 1 floor, ~4 m)

def storefront(name, frame_col, base_col, seed, out):
    """Modern aluminium shopfront: big panes with goods on display, a glass door in column 1."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    G = 400
    H, W = G, BW * 4
    c = Canvas(H, W)
    c.albedo = fill(H, W, base_col)
    c.height[:] = 0.55
    c.smooth[:] = 0.35
    for i in range(4):
        x0, x1 = i * BW, (i + 1) * BW
        lit = rng.random() < 0.9
        light = LIGHT_SOFT if rng.random() < 0.6 else LIGHT_COOL
        c.rect(x0, 30, x0 + 14, H, albedo=frame_col, height=0.7, metal=0.6, smooth=0.55)       # mullion
        gx0, gx1 = x0 + 14, x1
        c.rect(gx0, 30, gx1, 44, albedo=frame_col, height=0.62, metal=0.6, smooth=0.55)          # head
        if i == 1:
            dx0, dx1 = gx0 + 50, gx1 - 50
            glass(c, gx0 + 4, 48, dx0 - 4, H - 40, rng, lit, light, 'dark')
            glass(c, dx1 + 4, 48, gx1 - 4, H - 40, rng, lit, light, 'dark')
            c.rect(dx0, 48, dx1, H, albedo=frame_col, height=0.6, metal=0.6, smooth=0.55)
            glass(c, dx0 + 8, 56, dx1 - 8, H - 8, rng, lit, light, 'dark')
            c.rect((dx0 + dx1) / 2 - 2, 56, (dx0 + dx1) / 2 + 2, H - 8, albedo=frame_col)
            c.rect(dx0 + 20, 210, dx0 + 26, 300, albedo=rgb('#d9dce0'), metal=0.9, smooth=0.8, height=0.7)
            c.rect(dx1 - 26, 210, dx1 - 20, 300, albedo=rgb('#d9dce0'), metal=0.9, smooth=0.8, height=0.7)
        else:
            glass(c, gx0 + 4, 48, gx1 - 4, H - 40, rng, lit, light, 'dark')
            # goods on shelves and a display table
            for k in range(3):
                sy = 110 + k * 70
                c.rect(gx0 + 20, sy, gx1 - 20, sy + 4, albedo=rgb('#d0d0d0'), height=0.3,
                       emit=light * 0.9 if lit else None)
                for _ in range(int(rng.integers(4, 9))):
                    bx = rng.uniform(gx0 + 22, gx1 - 40)
                    bh = rng.uniform(14, 40)
                    col = pick(rng, [rgb('#c0392b'), rgb('#2980b9'), rgb('#f1c40f'), rgb('#27ae60'), rgb('#ecf0f1'),
                                     rgb('#8e44ad'), rgb('#e67e22'), rgb('#34495e')])
                    c.rect(bx, sy - bh, bx + rng.uniform(10, 24), sy, albedo=col, height=0.28, smooth=0.3,
                           emit=(light * col * 0.9) if lit else None)
            c.rect(gx0 + 4, H - 40, gx1 - 4, H, albedo=base_col * 0.85, height=0.6)          # stall riser
    c.rect(0, 0, W, 30, albedo=base_col * 1.05, height=0.65)                                     # fascia
    c.grime(amount=0.06, streaks=0.03, seed=seed + 11)
    c.save(f'{out}/{name}', normal_strength=3.0, save_emit=True)


def lobby(name, frame_col, stone, seed, out):
    """Office lobby: double-height glass in bronze or steel frames, a revolving door in column 0."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    G = 460
    H, W = G, BW * 4
    c = Canvas(H, W)
    alb, hgt = stone_blocks(H, W, rows=5, cols=8, joint_px=1.5, color=stone, var=0.02, seed=seed, joint_depth=0.06, rough=0.02)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.55
    for i in range(4):
        x0, x1 = i * BW, (i + 1) * BW
        cx = (x0 + x1) / 2
        c.rect(cx - 110, 30, cx + 110, H - 20, albedo=frame_col, height=0.45, smooth=0.6, metal=0.8)
        glass(c, cx - 104, 36, cx + 104, 110, rng, True, LIGHT_SOFT, 'dark')
        if i == 0:
            c.rect(cx - 104, 118, cx + 104, H - 26, albedo=rgb('#141516'), height=0.2)
            glass(c, cx - 80, 128, cx + 80, H - 30, rng, True, LIGHT_SOFT, 'dark')
            c.rect(cx - 2, 128, cx + 2, H - 30, albedo=frame_col * 1.3, metal=0.9, smooth=0.7)
        else:
            glass(c, cx - 104, 118, cx - 2, H - 26, rng, True, LIGHT_SOFT, 'dark')
            glass(c, cx + 2, 118, cx + 104, H - 26, rng, True, LIGHT_SOFT, 'dark')
    c.save(f'{out}/{name}', normal_strength=2.5, save_emit=True)


def rollup(name, wall, seed, out):
    """Industrial ground floor: sectional roll-up doors in assorted colours, a man-door, a light over each."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    G = 450
    H, W = G, BW * 4
    c = Canvas(H, W)
    alb, hgt = panels(H, W, wall, seed, BW, G, joint=4, var=0.02)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.25
    door_cols = [rgb('#e9e9e4'), rgb('#9aa3ab'), rgb('#2f5d8a'), rgb('#c0392b'), rgb('#3d6b45')]
    for i in range(4):
        x0, x1 = i * BW, (i + 1) * BW
        cx = (x0 + x1) / 2
        if i == 3:
            casement(c, cx - 40, 150, cx + 40, H, rgb('#3b3e42'), rng, rng.random() < 0.6, LIGHT_COOL, split=False, interior='dark')
            continue
        dw = 210
        col = pick(rng, door_cols)
        c.rect(cx - dw / 2 - 6, 70, cx + dw / 2 + 6, H, albedo=rgb('#3b3e42'), height=0.5, metal=0.6)
        yy = np.arange(H - 76)
        sect = ((yy % 46) < 3).astype(np.float32)
        door = fill(H - 76, dw, col) * (1 - 0.35 * sect)[:, None, None]
        c.rect(cx - dw / 2, 76, cx + dw / 2, H, albedo=door, height=0.42, smooth=0.35, metal=0.3)
        if rng.random() < 0.4:
            glass(c, cx - dw / 2 + 12, 92, cx + dw / 2 - 12, 118, rng, False, LIGHT_COOL, 'dark')
        c.rect(cx - 16, 38, cx + 16, 56, albedo=rgb('#2d2f33'), height=0.7)
        c.rect(cx - 12, 54, cx + 12, 60, albedo=rgb('#fff2cc'), height=0.6, emit=rgb('#ffe6a8'))
    c.grime(amount=0.12, streaks=0.2, seed=seed + 11)
    c.save(f'{out}/{name}', normal_strength=3.0, save_emit=True)


# ---------------------------------------------------------------- house parts

def house_windows(name, trim, seed, out):
    """4 x 4 atlas of house windows, each cell one window with its white trim: double-hung, grilles, lit or dark."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    CW, CH = 128, 160
    H, W = CH * 4, CW * 4
    c = Canvas(H, W)
    c.albedo = fill(H, W, trim)
    c.height[:] = 0.6
    c.smooth[:] = 0.35
    for j in range(4):
        for i in range(4):
            x0, y0 = i * CW, j * CH
            lit, light = lit_choice(rng, 0.55)
            # trim (casing), then the sashes
            c.rect(x0, y0, x0 + CW, y0 + CH, albedo=trim, height=0.75)
            ix0, iy0, ix1, iy1 = x0 + 14, y0 + 14, x0 + CW - 14, y0 + CH - 18
            c.rect(ix0 - 2, iy0 - 2, ix1 + 2, iy1 + 2, albedo=rgb('#3a3b3d'), height=0.3)
            mid = (iy0 + iy1) / 2
            inter = pick(rng, ['dark', 'curtain', 'curtain', 'sheer', 'blinds', 'plant'])
            glass(c, ix0 + 5, iy0 + 5, ix1 - 5, mid - 3, rng, lit, light, inter)
            glass(c, ix0 + 5, mid + 3, ix1 - 5, iy1 - 5, rng, lit, light, inter)
            frame_box(c, ix0, iy0, ix1, mid + 3, trim * 0.97, t=5)
            frame_box(c, ix0, mid - 3, ix1, iy1, trim * 0.97, t=5)
            if (i + j) % 3 == 0:
                # colonial grilles in the upper sash
                for k in (1, 2):
                    gx = ix0 + k * (ix1 - ix0) / 3
                    c.rect(gx - 1.5, iy0 + 5, gx + 1.5, mid - 3, albedo=trim, height=0.6)
                c.rect(ix0 + 5, (iy0 + mid) / 2 - 1.5, ix1 - 5, (iy0 + mid) / 2 + 1.5, albedo=trim, height=0.6)
            # sill with its shadow
            c.rect(x0 + 6, y0 + CH - 18, x0 + CW - 6, y0 + CH - 8, albedo=trim * 1.02, height=0.9)
            c.albedo[int(y0 + 14):int(y0 + 19), int(ix0):int(ix1)] *= 0.6
    c.save(f'{out}/{name}', normal_strength=3.0, save_emit=True)


def house_doors(name, seed, out):
    """4 front doors (cells 256 x 512 px, ~1.0 x 2.2 m with frame): painted panel doors with glass, a porch light."""
    reseed(seed)
    rng = np.random.default_rng(seed)
    CW, CH = 256, 512
    H, W = CH, CW * 4
    c = Canvas(H, W)
    trim = rgb('#f2f0ea')
    c.albedo = fill(H, W, trim)
    c.height[:] = 0.7
    paints = [rgb('#7a1f1f'), rgb('#1f3a5f'), rgb('#2e4d3a'), rgb('#2b2b2b')]
    for i in range(4):
        x0 = i * CW
        col = paints[i]
        dx0, dx1, dy0 = x0 + 40, x0 + CW - 40, 40
        c.rect(dx0 - 6, dy0 - 6, dx1 + 6, H, albedo=rgb('#3a3b3d'), height=0.35)
        c.rect(dx0, dy0, dx1, H, albedo=col, height=0.5, smooth=0.45)
        # raised panels
        for (py0, py1) in ((dy0 + 170, dy0 + 300), (dy0 + 320, H - 30)):
            for (px0, px1) in ((dx0 + 18, (dx0 + dx1) / 2 - 8), ((dx0 + dx1) / 2 + 8, dx1 - 18)):
                c.rect(px0, py0, px1, py1, albedo=col * 1.08, height=0.58)
                c.rect(px0, py1 - 3, px1, py1, albedo=col * 0.7)
        lit = rng.random() < 0.7
        glass(c, dx0 + 22, dy0 + 22, dx1 - 22, dy0 + 150, rng, lit, LIGHT_WARM, 'sheer')
        c.rect(dx1 - 34, dy0 + 290, dx1 - 22, dy0 + 304, albedo=rgb('#c9a24a'), metal=0.9, smooth=0.7, height=0.75)
    c.save(f'{out}/{name}', normal_strength=3.0, save_emit=True)


def garage_door(name, color, seed, out):
    """Sectional garage door, 1 x 1 tile (~3 x 2.4 m)."""
    reseed(seed)
    W, H = 320, 256
    c = Canvas(H, W)
    yy = np.mgrid[0:H, 0:W][0]
    sect = ((yy % 64) < 3).astype(np.float32)
    n = fbm(H, W, beta=1.8, lo=2, seed=seed)
    c.albedo = fill(H, W, color) * (0.97 + 0.04 * n - 0.3 * sect)[..., None]
    c.height = 0.5 - 0.2 * sect
    for k in range(4):
        for row in range(4):
            px0, py0 = 16 + k * 76, 10 + row * 64
            c.rect(px0, py0, px0 + 66, py0 + 44, add_height=0.06, albedo=color * 0.95)
    for k in range(4):
        glass(c, 18 + k * 76, 12, 80 + k * 76, 52, np.random.default_rng(seed + k), False, LIGHT_WARM, 'dark')
    c.smooth[:] = 0.35
    c.save(f'{out}/{name}', normal_strength=3.0)
