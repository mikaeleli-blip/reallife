"""Walls, roofs and ground for a modern town (tileable). Tile sizes in metres are noted; the game maps them by world
position so neighbouring pieces line up."""
import numpy as np
from texlib import *


# ---------------------------------------------------------------- walls (light, so the game can tint them)

def siding(out, seed=501):
    """Horizontal lap siding, 2 m tile (14 boards). Near-white so the game tints it per house."""
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    yy = np.mgrid[0:H, 0:W][0].astype(np.float32)
    bh = H / 14
    f = (yy % bh) / bh
    n = fbm(H, W, beta=1.4, lo=4, seed=seed)
    grain = np.repeat(fbm(H // 8, W, beta=0.8, lo=6, seed=seed + 1), 8, axis=0)
    c.albedo = fill(H, W, rgb('#f4f2ec')) * (0.9 + 0.1 * f - 0.25 * (f < 0.08) + 0.03 * grain + 0.04 * n)[..., None]
    c.height = 0.35 + 0.35 * f - 0.25 * (f < 0.08)
    c.smooth[:] = 0.3
    c.grime(0.05, 0.08, seed=seed + 2)
    c.save(f'{out}/wall_siding', normal_strength=5.0)


def board_batten(out, seed=511):
    """Vertical board and batten (barns, sheds), 2 m tile. Near-white for tinting."""
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    xx = np.mgrid[0:H, 0:W][1].astype(np.float32)
    bw = W / 8
    f = (xx % bw) / bw
    batten = (f < 0.12).astype(np.float32)
    grain = np.repeat(fbm(H, W // 8, beta=0.8, lo=6, seed=seed), 8, axis=1)
    n = fbm(H, W, beta=1.8, lo=2, seed=seed + 1)
    c.albedo = fill(H, W, rgb('#efe9e0')) * (0.88 + 0.08 * grain + 0.06 * n + 0.06 * batten)[..., None]
    c.height = 0.45 + 0.3 * batten + 0.05 * grain
    c.smooth[:] = 0.15
    c.grime(0.15, 0.2, seed=seed + 2)
    c.save(f'{out}/wall_board', normal_strength=5.0)


def stucco(out, seed=521):
    """Painted stucco/render, 2 m tile, near-white for tinting."""
    reseed(seed)
    H = W = 256
    c = Canvas(H, W)
    n = fbm(H, W, beta=1.8, lo=2, seed=seed)
    fine = fbm(H, W, beta=0.3, lo=60, seed=seed + 1)
    c.albedo = fill(H, W, rgb('#f2efe8')) * (0.94 + 0.05 * n + 0.04 * fine)[..., None]
    c.height = 0.5 + 0.12 * fine
    c.smooth[:] = 0.18
    c.grime(0.05, 0.1, seed=seed + 2)
    c.save(f'{out}/wall_stucco', normal_strength=3.0)


def brick_wall(name, colors, mortar, seed, out):
    """Plain brick, 2 m tile."""
    reseed(seed)
    H = W = 256
    c = Canvas(H, W)
    alb, hgt, _ = brick_field(H, W, courses=30, bricks_per_row=9, colors=colors, seed=seed, mortar_color=mortar, var=0.08)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.12
    c.grime(0.08, 0.1, seed=seed + 1)
    c.save(f'{out}/{name}', normal_strength=5.0)


def stone_wall(name, color, seed, out, rows=6, cols=3):
    reseed(seed)
    H = W = 256
    c = Canvas(H, W)
    alb, hgt = stone_blocks(H, W, rows=rows, cols=cols, joint_px=1.2, color=color, var=0.04, seed=seed,
                            joint_depth=0.08, rough=0.05)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.22
    c.grime(0.08, 0.1, seed=seed + 1)
    c.save(f'{out}/{name}', normal_strength=4.0)


def metal_panel(out, seed=531):
    """Ribbed metal, 2 m tile, light grey for tinting (cladding, sheds, roofs of barns)."""
    reseed(seed)
    H = W = 256
    c = Canvas(H, W)
    xx = np.mgrid[0:H, 0:W][1].astype(np.float32)
    rib = 0.5 + 0.5 * np.cos((xx % 32) / 32 * 2 * np.pi)
    n = fbm(H, W, beta=1.6, lo=2, seed=seed)
    c.albedo = fill(H, W, rgb('#dfe2e5')) * (0.86 + 0.14 * rib + 0.05 * n)[..., None]
    c.height = 0.45 + 0.25 * rib
    c.smooth[:] = 0.55
    c.metal[:] = 0.7
    c.grime(0.06, 0.15, seed=seed + 1)
    c.save(f'{out}/metal_panel', normal_strength=3.0)


# ---------------------------------------------------------------- roofs

def shingles(out, seed=541):
    """Asphalt shingles, 2 m tile, mid grey for tinting."""
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    rng = np.random.default_rng(seed)
    rows, cols = 16, 6
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    ch, cw = H / rows, W / cols
    row = np.floor(yy / ch)
    fy = (yy - row * ch) / ch
    shift = np.where(row % 2 == 1, cw / 2, 0)
    col = np.floor(((xx + shift) % W) / cw)
    ids = (row * 31 + col).astype(np.int64) % 997
    tone = rng.uniform(0.85, 1.12, 997)[ids]
    gran = speckle(H, W, 0.25, seed=seed + 1)
    n = fbm(H, W, beta=1.8, lo=2, seed=seed + 2)
    c.albedo = fill(H, W, rgb('#9a9a98')) * (tone * (0.85 + 0.2 * fy + 0.1 * gran + 0.05 * n))[..., None]
    gap = (((xx + shift) % cw) < 2) & (fy > 0.45)
    c.albedo[gap] *= 0.45
    c.albedo *= (1 - 0.45 * (fy < 0.06))[..., None]
    c.height = 0.35 + 0.35 * fy + 0.05 * gran - 0.2 * gap
    c.smooth[:] = 0.12
    c.save(f'{out}/roof_shingle', normal_strength=5.0)


def membrane(out, seed=551):
    """Light flat-roof membrane with seams, 6 m tile."""
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    n = fbm(H, W, beta=2.0, lo=1, seed=seed)
    fine = fbm(H, W, beta=0.4, lo=80, seed=seed + 1)
    c.albedo = fill(H, W, rgb('#c9cbc8')) * (0.88 + 0.12 * n + 0.04 * fine)[..., None]
    yy = np.mgrid[0:H, 0:W][0]
    seam = ((yy % 85) < 3).astype(np.float32)
    c.albedo *= (1 - 0.12 * seam)[..., None]
    p = fbm(H, W, beta=2.6, lo=1, seed=seed + 2)
    c.albedo = lerp(c.albedo, c.albedo * 0.8, np.clip((p - 0.7) * 3, 0, 1))
    c.height = 0.5 + 0.05 * fine + 0.2 * seam
    c.smooth[:] = 0.3
    c.save(f'{out}/roof_membrane', normal_strength=3.0)


def standing_seam(out, seed=561):
    """Standing-seam metal roof, 2 m tile, light for tinting."""
    reseed(seed)
    H = W = 256
    c = Canvas(H, W)
    xx = np.mgrid[0:H, 0:W][1].astype(np.float32)
    seam = ((xx % 64) < 4).astype(np.float32)
    n = fbm(H, W, beta=1.6, lo=2, seed=seed)
    c.albedo = fill(H, W, rgb('#d5d8db')) * (0.92 + 0.06 * n + 0.08 * seam)[..., None]
    c.height = 0.45 + 0.4 * seam
    c.smooth[:] = 0.5
    c.metal[:] = 0.6
    c.save(f'{out}/roof_metal', normal_strength=4.0)


# ---------------------------------------------------------------- ground

def grass(out, seed=571):
    """Mown grass, 8 m tile. Mid green; the game tints it by season."""
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    n = fbm(H, W, beta=1.8, lo=1, seed=seed)
    blades = fbm(H, W, beta=0.1, lo=120, seed=seed + 1)
    c.albedo = lerp(fill(H, W, rgb('#5f8f3e')), fill(H, W, rgb('#7da64a')), n)
    c.albedo *= (0.82 + 0.3 * blades)[..., None]
    clover = np.clip((fbm(H, W, beta=2.2, lo=3, seed=seed + 2) - 0.7) * 3, 0, 1)
    c.albedo = lerp(c.albedo, fill(H, W, rgb('#4e7d36')), clover * 0.6)
    c.height = 0.5 + 0.35 * blades
    c.smooth[:] = 0.08
    c.save(f'{out}/grass', normal_strength=4.0)


def asphalt(out, seed=581):
    """Road asphalt, 8 m tile: fairly new, dark, with patched cracks."""
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    n = fbm(H, W, beta=1.8, lo=1, seed=seed)
    fine = fbm(H, W, beta=0.3, lo=60, seed=seed + 1)
    c.albedo = fill(H, W, rgb('#3a3c3f')) * (0.85 + 0.2 * n + 0.15 * fine)[..., None]
    agg = speckle(H, W, 0.03, seed=seed + 2)
    c.albedo = lerp(c.albedo, fill(H, W, rgb('#7c7c78')), agg * 0.35)
    cr = fbm(H, W, beta=1.4, lo=3, hi=40, seed=seed + 3)
    crack = blur((np.abs(cr - 0.5) < 0.006).astype(np.float32), 1) * (fbm(H, W, beta=2, lo=1, seed=seed + 4) > 0.62)
    c.albedo = lerp(c.albedo, fill(H, W, rgb('#202122')), np.clip(crack * 3, 0, 1) * 0.8)
    oil = fbm(H, W, beta=2.4, lo=2, seed=seed + 6)
    c.albedo = lerp(c.albedo, fill(H, W, rgb('#26272a')), np.clip((oil - 0.72) * 3, 0, 1) * 0.5)
    c.height = 0.5 + 0.1 * fine + 0.15 * agg - 0.2 * crack
    c.smooth = (0.2 + 0.2 * np.clip((oil - 0.6) * 3, 0, 1)).astype(np.float32)
    c.save(f'{out}/asphalt', normal_strength=2.5)


def sidewalk(out, seed=591):
    """Light concrete pavement, 1.5 m slabs, 6 m tile."""
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    n = fbm(H, W, beta=1.6, lo=2, seed=seed)
    fine = fbm(H, W, beta=0.4, lo=60, seed=seed + 1)
    yy, xx = np.mgrid[0:H, 0:W]
    slab = W / 4
    joint = ((xx % slab) < 2) | ((yy % slab) < 2)
    sid = ((yy // slab) * 4 + (xx // slab)).astype(np.int64)
    tone = np.random.default_rng(seed).uniform(0.92, 1.06, 16)[sid]
    c.albedo = fill(H, W, rgb('#b9b6ae')) * (tone * (0.88 + 0.14 * n + 0.06 * fine))[..., None]
    c.albedo[joint] *= 0.65
    stains = fbm(H, W, beta=2.3, lo=2, seed=seed + 4)
    c.albedo = lerp(c.albedo, c.albedo * 0.8, np.clip((stains - 0.68) * 3, 0, 1))
    c.height = 0.55 + 0.05 * fine - 0.3 * joint
    c.smooth[:] = 0.2
    c.save(f'{out}/sidewalk', normal_strength=3.0)


def concrete(out, seed=601):
    """Plain poured concrete (driveways, yards, lots), 4 m tile with saw cuts."""
    reseed(seed)
    H = W = 256
    c = Canvas(H, W)
    n = fbm(H, W, beta=1.8, lo=1, seed=seed)
    fine = fbm(H, W, beta=0.3, lo=60, seed=seed + 1)
    yy, xx = np.mgrid[0:H, 0:W]
    cut = ((xx % 128) < 2) | ((yy % 128) < 2)
    c.albedo = fill(H, W, rgb('#a9a69f')) * (0.88 + 0.16 * n + 0.06 * fine)[..., None]
    c.albedo[cut] *= 0.7
    c.height = 0.5 + 0.08 * fine - 0.25 * cut
    c.smooth[:] = 0.22
    c.grime(0.1, 0.0, seed=seed + 2)
    c.save(f'{out}/concrete', normal_strength=2.5)


def pavers(out, seed=611):
    """Plaza pavers (herringbone-ish running bond), 3 m tile."""
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    alb, hgt, _ = brick_field(H, W, courses=32, bricks_per_row=16, colors=[rgb('#b9ab98'), rgb('#a8998a'), rgb('#c4b8a6'), rgb('#9d8f80')],
                              seed=seed, mortar_color=rgb('#7f786e'), var=0.06)
    c.albedo, c.height = alb, hgt
    c.smooth[:] = 0.2
    c.save(f'{out}/pavers', normal_strength=4.0)


def dirt(out, seed=621):
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    n = fbm(H, W, beta=1.9, lo=1, seed=seed)
    fine = fbm(H, W, beta=0.4, lo=60, seed=seed + 1)
    c.albedo = lerp(fill(H, W, rgb('#5c4a37')), fill(H, W, rgb('#86704f')), n) * (0.82 + 0.3 * fine)[..., None]
    st = np.clip(blur(speckle(H, W, 0.01, seed=seed + 2), 2) * 4, 0, 1)
    c.albedo = lerp(c.albedo, fill(H, W, rgb('#a59a88')), st)
    c.height = 0.5 + 0.2 * fine + 0.3 * st
    c.smooth[:] = 0.08
    c.save(f'{out}/dirt', normal_strength=4.0)


def gravel(out, seed=631):
    reseed(seed)
    H = W = 256
    c = Canvas(H, W)
    n = fbm(H, W, beta=0.6, lo=30, seed=seed)
    big = fbm(H, W, beta=1.8, lo=1, seed=seed + 1)
    c.albedo = lerp(fill(H, W, rgb('#7d776c')), fill(H, W, rgb('#b2aa9a')), n) * (0.9 + 0.15 * big)[..., None]
    c.height = 0.3 + 0.6 * n
    c.smooth[:] = 0.1
    c.save(f'{out}/gravel', normal_strength=5.0)


def field(name, soil, crop, height_frac, seed, out):
    """Crop rows along U, 8 m tile."""
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    yy = np.mgrid[0:H, 0:W][0].astype(np.float32)
    f = (yy % 32) / 32
    row = np.clip(1 - np.abs(f - 0.5) / (0.25 + 0.25 * height_frac), 0, 1)
    n = fbm(H, W, beta=0.5, lo=40, seed=seed)
    big = fbm(H, W, beta=1.8, lo=1, seed=seed + 1)
    s = fill(H, W, soil) * (0.85 + 0.2 * big)[..., None]
    cr = fill(H, W, crop) * (0.75 + 0.35 * n + 0.1 * big)[..., None]
    c.albedo = lerp(s, cr, np.clip(row * (0.6 + 0.6 * n) * (0.4 + height_frac), 0, 1))
    c.height = 0.3 + 0.5 * row * (0.3 + height_frac) + 0.1 * n
    c.smooth[:] = 0.08
    c.save(f'{out}/{name}', normal_strength=5.0)


def sand(out, seed=641):
    reseed(seed)
    H = W = 256
    c = Canvas(H, W)
    n = fbm(H, W, beta=1.8, lo=1, seed=seed)
    fine = fbm(H, W, beta=0.2, lo=80, seed=seed + 1)
    c.albedo = fill(H, W, rgb('#d8c69a')) * (0.9 + 0.12 * n + 0.08 * fine)[..., None]
    c.height = 0.5 + 0.15 * fine
    c.smooth[:] = 0.1
    c.save(f'{out}/sand', normal_strength=2.0)


def water(out, seed=651):
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    n = fbm(H, W, beta=2.2, lo=2, seed=seed)
    waves = fbm(H, W, beta=1.6, lo=6, hi=60, seed=seed + 1)
    c.albedo = lerp(fill(H, W, rgb('#1d4f6e')), fill(H, W, rgb('#2c6a8a')), n)
    c.height = waves
    c.smooth[:] = 0.95
    c.save(f'{out}/water', normal_strength=2.0)


def snow(out, seed=661):
    reseed(seed)
    H = W = 512
    c = Canvas(H, W)
    n = fbm(H, W, beta=2.0, lo=1, seed=seed)
    fine = fbm(H, W, beta=0.3, lo=80, seed=seed + 1)
    c.albedo = fill(H, W, rgb('#f2f5f8')) * (0.93 + 0.06 * n + 0.03 * fine)[..., None]
    c.height = 0.5 + 0.2 * n + 0.05 * fine
    c.smooth = (0.35 + 0.2 * fine).astype(np.float32)
    c.save(f'{out}/snow', normal_strength=2.0)


def wood(out, seed=671):
    """Planks (decks, fences), 2 m tile, light for tinting."""
    reseed(seed)
    H = W = 256
    c = Canvas(H, W)
    rng = np.random.default_rng(seed)
    yy = np.mgrid[0:H, 0:W][0]
    pw = 18
    gap = (yy % pw) < 2
    tone = rng.uniform(0.85, 1.1, H // pw + 1)[yy // pw]
    grain = np.repeat(fbm(H // 8, W, beta=1.0, lo=4, seed=seed), 8, axis=0)
    c.albedo = fill(H, W, rgb('#c9a57c')) * (tone * (0.85 + 0.2 * grain))[..., None]
    c.albedo[gap] *= 0.35
    c.height = np.where(gap, 0.2, 0.55 + 0.1 * grain)
    c.smooth[:] = 0.2
    c.save(f'{out}/wood', normal_strength=4.0)


def track(out, seed=681):
    """Rubber running track / playground surface, 4 m tile."""
    reseed(seed)
    H = W = 256
    c = Canvas(H, W)
    n = fbm(H, W, beta=0.4, lo=60, seed=seed)
    c.albedo = fill(H, W, rgb('#b5523b')) * (0.88 + 0.2 * n)[..., None]
    c.height = 0.5 + 0.2 * n
    c.smooth[:] = 0.15
    c.save(f'{out}/track', normal_strength=2.0)
