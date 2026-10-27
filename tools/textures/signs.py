"""Sign boards for shops and public buildings: 32 rows of 1024 x 64 (16:1). Letters glow at night.

The row order is the game's SignRow table (Assets/Scripts/Game/Art/Signs.cs): keep them in step.
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from texlib import save_srgb, save_normal, save_mask

FONT_BOLD = '/usr/share/fonts/truetype/google-fonts/Poppins-Bold.ttf'
FONT_SEMI = '/usr/share/fonts/truetype/google-fonts/Poppins-Medium.ttf'
FONT_SERIF = '/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf'

# text, board colour, letter colour, glow colour, font
SIGNS = [
    ('FRESH MARKET', '#1e7a3c', '#ffffff', '#e8ffe8', FONT_BOLD),
    ('GRILL & KITCHEN', '#2b2b2b', '#f2c14e', '#ffd36b', FONT_BOLD),
    ('COFFEE', '#f3ede2', '#5b3a29', '#ffcf99', FONT_BOLD),
    ('BAR & LOUNGE', '#14141a', '#ff5c8a', '#ff6fa0', FONT_BOLD),
    ('APPAREL', '#ffffff', '#222222', '#fff6e6', FONT_SEMI),
    ('ELECTRONICS', '#0f3d6e', '#ffffff', '#cfe6ff', FONT_BOLD),
    ('FURNITURE', '#f0e6d8', '#a0522d', '#ffd9b3', FONT_SERIF),
    ('FUEL', '#c0392b', '#ffffff', '#ffe0dc', FONT_BOLD),
    ('AUTO SALES', '#0a3d62', '#ffffff', '#d6ebff', FONT_BOLD),
    ('AUTO REPAIR', '#f39c12', '#1e1e1e', '#fff1cc', FONT_BOLD),
    ('HAIR SALON', '#2d2d2d', '#f7b6cf', '#ffc7dc', FONT_SEMI),
    ('FITNESS', '#111111', '#2ecc71', '#7dffb0', FONT_BOLD),
    ('BANK', '#e9e4d6', '#1d3557', '#fff3d6', FONT_SERIF),
    ('OFFICES', '#2c3e50', '#ecf0f1', '#ecf6ff', FONT_SEMI),
    ('POLICE', '#1f3a68', '#ffffff', '#dbe8ff', FONT_BOLD),
    ('FIRE DEPT.', '#9e1b1b', '#ffffff', '#ffe1e1', FONT_BOLD),
    ('CLINIC', '#ffffff', '#c0392b', '#ffe8e5', FONT_BOLD),
    ('HOSPITAL', '#ffffff', '#c0392b', '#ffe8e5', FONT_BOLD),
    ('TOWN HALL', '#e9e4d6', '#2b2b2b', '#fff3d6', FONT_SERIF),
    ('ELEMENTARY SCHOOL', '#1e3799', '#ffffff', '#e1e8ff', FONT_SEMI),
    ('HIGH SCHOOL', '#7a1f1f', '#ffffff', '#ffe5e5', FONT_SEMI),
    ('COMMUNITY COLLEGE', '#14365c', '#f2d16b', '#ffe9a8', FONT_SERIF),
    ('BUS DEPOT', '#2980b9', '#ffffff', '#e0f0ff', FONT_BOLD),
    ('LOGISTICS', '#ecf0f1', '#0a79df', '#e8f4ff', FONT_BOLD),
    ('BUILDERS', '#f1c40f', '#1e1e1e', '#fff3c2', FONT_BOLD),
    ('FOODS', '#ffffff', '#27ae60', '#e8ffe8', FONT_BOLD),
    ('APPAREL CO.', '#3d3d6b', '#ffffff', '#e8e8ff', FONT_SEMI),
    ('TECH', '#0b0b0f', '#38bdf8', '#9ee7ff', FONT_BOLD),
    ('WORKSHOP', '#5d4037', '#f5deb3', '#ffe9c4', FONT_SERIF),
    ('APARTMENTS', '#3a3a3a', '#f2f2f2', '#fff7e6', FONT_SEMI),
    ('REAL ESTATE', '#f3e2b8', '#1d3557', '#fff0cc', FONT_BOLD),
    ('OPEN', '#111111', '#ff3b3b', '#ff6b6b', FONT_BOLD),
]


# the second atlas (signs2.png): the grocery chains. Its rows are SignRow 32 onwards.
SIGNS2 = [
    ('VALLEY VALUE', '#1d4f91', '#ffd23f', '#ffe27a', FONT_BOLD),
    ('REDWOOD MARKET', '#b3202a', '#ffffff', '#ffe5e5', FONT_SERIF),
    ('BULKWORKS CLUB', '#1f2a44', '#ff7a1a', '#ff9a4a', FONT_BOLD),
]


def _fit(draw, text, path, max_w, max_h):
    size = max_h
    while size > 8:
        f = ImageFont.truetype(path, size)
        b = draw.textbbox((0, 0), text, font=f)
        if b[2] - b[0] <= max_w and b[3] - b[1] <= max_h:
            return f, b
        size -= 2
    f = ImageFont.truetype(path, 8)
    return f, draw.textbbox((0, 0), text, font=f)


def signs(out, rows=None, name='signs'):
    rows = SIGNS if rows is None else rows
    W, RH = 1024, 64
    N = 32
    alb = Image.new('RGB', (W, RH * N))
    emi = Image.new('RGB', (W, RH * N), (0, 0, 0))
    da, de = ImageDraw.Draw(alb), ImageDraw.Draw(emi)
    for i, (text, board, letter, glow, font_path) in enumerate(rows):
        y0 = i * RH
        da.rectangle([0, y0, W, y0 + RH], fill=board)
        de.rectangle([0, y0, W, y0 + RH], fill=tuple(int(int(board[k:k + 2], 16) * 0.12) for k in (1, 3, 5)))
        font, b = _fit(da, text, font_path, W - 80, RH - 18)
        tw, th = b[2] - b[0], b[3] - b[1]
        tx, ty = (W - tw) // 2 - b[0], y0 + (RH - th) // 2 - b[1]
        da.text((tx, ty), text, font=font, fill=letter)
        de.text((tx, ty), text, font=font, fill=glow)
    a = np.asarray(alb, np.float32) / 255.0
    save_srgb(f'{out}/{name}.png', a)
    save_srgb(f'{out}/{name}_e.png', np.asarray(emi, np.float32) / 255.0)
    h = np.full(a.shape[:2], 0.5, np.float32)
    save_normal(f'{out}/{name}_n.png', h, 1.0)
    save_mask(f'{out}/{name}_m.png', np.zeros(a.shape[:2], np.float32), np.full(a.shape[:2], 0.55, np.float32))
