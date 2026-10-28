"""Generate all Real Life textures (approach ported from Mobland).

    python tools/textures/generate.py            # everything
    python tools/textures/generate.py grass      # one set

Writes PNGs to Assets/Resources/RealLife/Tex. File suffixes tell the Unity importer what they are:
    name.png     albedo (sRGB)
    name_n.png   normal map
    name_m.png   metallic (R) + smoothness (A), linear
    name_e.png   emission (lit windows and signs at night), sRGB
The game refers to these by name (Assets/Scripts/Game/Art/Surfaces.cs). Needs numpy and Pillow.
"""
import os
import sys
import time

sys.path.insert(0, os.path.dirname(__file__))
from texlib import rgb  # noqa: E402
import facades as F  # noqa: E402
import surfaces as S  # noqa: E402
import signs as G  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'RealLife', 'Tex')

BRICK_RED = [rgb('#9b4a36'), rgb('#8c4231'), rgb('#a5533c'), rgb('#7f3c2d')]
BRICK_TAN = [rgb('#c4a27a'), rgb('#b8966f'), rgb('#ceae86'), rgb('#ad8c66')]
BRICK_BROWN = [rgb('#6e4636'), rgb('#7a4f3d'), rgb('#62402f'), rgb('#83583f')]
BRICK_GREY = [rgb('#8a8784'), rgb('#7e7b78'), rgb('#96938f'), rgb('#74716e')]


def main():
    os.makedirs(OUT, exist_ok=True)
    only = set(sys.argv[1:])
    jobs = [
        # apartments
        ('fac_apt_brick', lambda: F.apartment('fac_apt_brick', rgb('#9b4a36'), rgb('#3b3e42'), 11, OUT, brick=BRICK_RED, trim=rgb('#d8d2c4'))),
        ('fac_apt_tan', lambda: F.apartment('fac_apt_tan', rgb('#c4a27a'), rgb('#f2f2ee'), 12, OUT, brick=BRICK_TAN, trim=rgb('#ece6da'))),
        ('fac_apt_cream', lambda: F.apartment('fac_apt_cream', rgb('#e8dfcc'), rgb('#3b3e42'), 13, OUT)),
        ('fac_apt_grey', lambda: F.apartment('fac_apt_grey', rgb('#b9bcbf'), rgb('#f2f2ee'), 14, OUT)),
        ('gf_apt_base', lambda: F.apartment_base('gf_apt_base', rgb('#a7a39b'), rgb('#3b3e42'), 15, OUT)),
        # offices
        ('fac_glass_blue', lambda: F.curtain_wall('fac_glass_blue', rgb('#6f9cc0'), rgb('#3b4450'), rgb('#26313d'), 21, OUT)),
        ('fac_glass_green', lambda: F.curtain_wall('fac_glass_green', rgb('#7fb0a6'), rgb('#c9ced3'), rgb('#55606a'), 22, OUT)),
        ('fac_precast', lambda: F.office_precast('fac_precast', rgb('#d6d1c6'), rgb('#3b3e42'), 23, OUT)),
        ('gf_lobby', lambda: F.lobby('gf_lobby', rgb('#4a4f57'), rgb('#6d6a66'), 24, OUT)),
        # shops
        ('gf_store_dark', lambda: F.storefront('gf_store_dark', rgb('#2e3236'), rgb('#3b3f44'), 31, OUT)),
        ('gf_store_light', lambda: F.storefront('gf_store_light', rgb('#e9eaea'), rgb('#cfcac0'), 32, OUT)),
        ('gf_store_wood', lambda: F.storefront('gf_store_wood', rgb('#5b3a29'), rgb('#6e4a35'), 33, OUT)),
        # public
        ('fac_civic_brick', lambda: F.civic('fac_civic_brick', BRICK_RED, rgb('#e2dccd'), rgb('#f2f2ee'), 41, OUT)),
        ('fac_civic_brown', lambda: F.civic('fac_civic_brown', BRICK_BROWN, rgb('#d8cfbd'), rgb('#3b3e42'), 42, OUT)),
        ('fac_clinic', lambda: F.clinic('fac_clinic', rgb('#eef0f1'), rgb('#2e86de'), rgb('#3b3e42'), 43, OUT)),
        # industry
        ('fac_metal_grey', lambda: F.metal_cladding('fac_metal_grey', rgb('#b9bec4'), 51, OUT)),
        ('fac_metal_blue', lambda: F.metal_cladding('fac_metal_blue', rgb('#4f7396'), 52, OUT)),
        ('fac_metal_beige', lambda: F.metal_cladding('fac_metal_beige', rgb('#cdbf9f'), 53, OUT)),
        ('fac_tiltup', lambda: F.tiltup('fac_tiltup', rgb('#c9c4ba'), rgb('#2e86de'), 54, OUT)),
        ('gf_rollup', lambda: F.rollup('gf_rollup', rgb('#bfbab0'), 55, OUT)),
        # houses
        ('win_house', lambda: F.house_windows('win_house', rgb('#f6f5f1'), 61, OUT)),
        ('door_house', lambda: F.house_doors('door_house', 62, OUT)),
        ('garage_door', lambda: F.garage_door('garage_door', rgb('#efefeb'), 63, OUT)),
        ('wall_siding', lambda: S.siding(OUT)),
        ('wall_board', lambda: S.board_batten(OUT)),
        ('wall_stucco', lambda: S.stucco(OUT)),
        ('wall_brick_red', lambda: S.brick_wall('wall_brick_red', BRICK_RED, rgb('#c4bcae'), 71, OUT)),
        ('wall_brick_tan', lambda: S.brick_wall('wall_brick_tan', BRICK_TAN, rgb('#e2dccd'), 72, OUT)),
        ('wall_brick_brown', lambda: S.brick_wall('wall_brick_brown', BRICK_BROWN, rgb('#c4bcae'), 73, OUT)),
        ('wall_brick_grey', lambda: S.brick_wall('wall_brick_grey', BRICK_GREY, rgb('#d0ccc4'), 74, OUT)),
        ('wall_stone', lambda: S.stone_wall('wall_stone', rgb('#e3dccb'), 75, OUT)),
        ('metal_panel', lambda: S.metal_panel(OUT)),
        # roofs
        ('roof_shingle', lambda: S.shingles(OUT)),
        ('roof_membrane', lambda: S.membrane(OUT)),
        ('roof_metal', lambda: S.standing_seam(OUT)),
        # ground
        ('grass', lambda: S.grass(OUT)),
        ('asphalt', lambda: S.asphalt(OUT)),
        ('sidewalk', lambda: S.sidewalk(OUT)),
        ('concrete', lambda: S.concrete(OUT)),
        ('pavers', lambda: S.pavers(OUT)),
        ('dirt', lambda: S.dirt(OUT)),
        ('gravel', lambda: S.gravel(OUT)),
        ('field_young', lambda: S.field('field_young', rgb('#6b5236'), rgb('#7fbf4f'), 0.15, 81, OUT)),
        ('field_green', lambda: S.field('field_green', rgb('#6b5236'), rgb('#5f9a3a'), 0.75, 82, OUT)),
        ('field_ripe', lambda: S.field('field_ripe', rgb('#7a6040'), rgb('#d9b65a'), 0.85, 83, OUT)),
        ('sand', lambda: S.sand(OUT)),
        ('water', lambda: S.water(OUT)),
        ('snow', lambda: S.snow(OUT)),
        ('wood', lambda: S.wood(OUT)),
        ('track', lambda: S.track(OUT)),
        ('signs', lambda: G.signs(OUT)),
        ('signs2', lambda: G.all_more(OUT, os.path.join(ROOT, 'Assets', 'Scripts', 'Game', 'Art', 'SignRows.cs'))),
    ]
    for name, fn in jobs:
        if only and name not in only:
            continue
        t = time.time()
        fn()
        print(f'{name:22s} {time.time() - t:5.1f}s', flush=True)


if __name__ == '__main__':
    main()
