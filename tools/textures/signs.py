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
    ('BUYMORE', '#1d4f91', '#ffd23f', '#ffe27a', FONT_BOLD),
    ('MEGAMART', '#b3202a', '#ffffff', '#ffe5e5', FONT_BOLD),
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


FONT_ITAL = '/usr/share/fonts/truetype/google-fonts/Poppins-BoldItalic.ttf'
FONT_LORA = '/usr/share/fonts/truetype/google-fonts/Lora-Italic-Variable.ttf'
FONT_MONO = '/usr/share/fonts/truetype/liberation/LiberationMono-Bold.ttf'
FONT_CALA = '/usr/share/fonts/truetype/crosextra/Caladea-Bold.ttf'

# the many kinds of business added later: (building type, text, board, letters, glow, font). Their rows follow SIGNS2 on
# signs2.png and run on through signs3.png, signs4.png and so on; tools/textures/generate.py also writes the game's table of
# them (Assets/Scripts/Game/Art/SignRows.cs).
SIGNS_MORE = [
    # farms
    ('DairyFarm', 'DAIRY FARM', '#ffffff', '#1f3a68', '#e8f0ff', FONT_SERIF),
    ('CattleRanch', 'CATTLE RANCH', '#5b3a29', '#f5deb3', '#ffe9c4', FONT_SERIF),
    ('PoultryFarm', 'POULTRY FARM', '#f2e3b3', '#8b2d1e', '#fff1cc', FONT_BOLD),
    ('TreeFarm', 'TIMBER', '#2f4f2f', '#f2f2ee', '#e8ffe8', FONT_SERIF),
    ('Orchard', 'ORCHARD', '#f6efe0', '#b3202a', '#ffe5e5', FONT_LORA),
    ('Vineyard', 'VINEYARD', '#3b1f2b', '#e8d5a0', '#ffecc0', FONT_LORA),
    ('PlantNursery', 'NURSERY', '#e8f5e0', '#2e7d32', '#e8ffe8', FONT_BOLD),
    # mills and plants
    ('Sawmill', 'SAWMILL', '#6d4c2f', '#ffe0a8', '#ffe9c4', FONT_SERIF),
    ('PaperMill', 'PAPER MILL', '#eef2f5', '#2f5d8a', '#e8f4ff', FONT_SEMI),
    ('SteelMill', 'STEEL', '#2b2b2b', '#ff8c2a', '#ffb070', FONT_BOLD),
    ('MetalShop', 'MACHINE SHOP', '#3a4a5a', '#f2f2ee', '#e8f0ff', FONT_MONO),
    ('AutoPartsPlant', 'COMPONENTS', '#0f3d6e', '#ffffff', '#cfe6ff', FONT_BOLD),
    ('ChemicalPlant', 'CHEMICALS', '#ffffff', '#00897b', '#d8fff8', FONT_BOLD),
    ('PlasticsPlant', 'POLYMERS', '#e3f2fd', '#1565c0', '#d8ecff', FONT_BOLD),
    ('TextileMill', 'TEXTILE MILL', '#7a2e2e', '#f5e6d3', '#ffe9dc', FONT_SERIF),
    ('Tannery', 'TANNERY', '#8d5524', '#fff1dc', '#ffe9c4', FONT_SERIF),
    ('Glassworks', 'GLASSWORKS', '#e0f7fa', '#006064', '#d8fbff', FONT_SEMI),
    ('Quarry', 'QUARRY', '#9e9e9e', '#212121', '#f2f2f2', FONT_BOLD),
    ('Brickworks', 'BRICKWORKS', '#a1402a', '#ffffff', '#ffe5dc', FONT_BOLD),
    ('ConcretePlant', 'CONCRETE', '#cfd8dc', '#263238', '#f2f6f8', FONT_BOLD),
    ('FlourMill', 'FLOUR MILL', '#f5ecd7', '#7b5e3b', '#fff3d6', FONT_SERIF),
    ('BreadFactory', 'BAKERIES', '#f4c26b', '#5b3a1a', '#fff0c8', FONT_ITAL),
    ('DairyPlant', 'CREAMERY', '#ffffff', '#1e88e5', '#e3f2ff', FONT_ITAL),
    ('Meatpacking', 'MEATS', '#8e1b1b', '#ffffff', '#ffe1e1', FONT_BOLD),
    ('Cannery', 'CANNING CO.', '#2e7d32', '#ffeb3b', '#fff59d', FONT_BOLD),
    ('Brewery', 'BREWING CO.', '#3e2723', '#ffc107', '#ffd54f', FONT_SERIF),
    ('Winery', 'WINERY', '#4a148c', '#f3e5f5', '#f8eaff', FONT_LORA),
    ('Distillery', 'DISTILLERY', '#1b1b1b', '#d4a24c', '#ffd27a', FONT_SERIF),
    ('BottlingPlant', 'BOTTLING', '#d32f2f', '#ffffff', '#ffe5e5', FONT_ITAL),
    ('PetFoodPlant', 'PET FOODS', '#ffb300', '#3e2723', '#fff1c4', FONT_BOLD),
    # factories and workshops
    ('ShoeFactory', 'FOOTWEAR', '#212121', '#ffffff', '#f2f2f2', FONT_ITAL),
    ('AppliancePlant', 'APPLIANCES', '#eceff1', '#37474f', '#ffffff', FONT_BOLD),
    ('ToolWorks', 'TOOL WORKS', '#f9a825', '#212121', '#fff3c2', FONT_BOLD),
    ('PaintFactory', 'PAINTS', '#ffffff', '#6a1b9a', '#f8e8ff', FONT_BOLD),
    ('PharmaLab', 'PHARMA', '#ffffff', '#00838f', '#d8fbff', FONT_SEMI),
    ('SoapWorks', 'SOAP WORKS', '#e1f5fe', '#0277bd', '#e8f6ff', FONT_ITAL),
    ('PrintingPress', 'PRESS', '#263238', '#ffffff', '#f2f2f2', FONT_SERIF),
    ('PaperGoodsPlant', 'PAPER GOODS', '#fafafa', '#3949ab', '#eef0ff', FONT_SEMI),
    ('ToyFactory', 'TOYS', '#ffeb3b', '#e53935', '#fff59d', FONT_BOLD),
    ('SportsGoodsFactory', 'ATHLETICS', '#1a237e', '#ffffff', '#e8eaff', FONT_ITAL),
    ('BicycleWorks', 'CYCLE WORKS', '#00695c', '#ffffff', '#d8fff8', FONT_BOLD),
    ('InstrumentWorkshop', 'INSTRUMENTS', '#3e2723', '#d7ccc8', '#fff0e8', FONT_LORA),
    ('JewelryWorkshop', 'GOLDSMITHS', '#1b1b1b', '#e6c068', '#ffe08a', FONT_SERIF),
    ('Pottery', 'POTTERY', '#b5651d', '#fff8e1', '#ffe9c4', FONT_LORA),
    ('MattressFactory', 'SLEEP CO.', '#283593', '#ffffff', '#e8eaff', FONT_SEMI),
    ('CabinetShop', 'CABINETRY', '#6d4c41', '#fbe9e7', '#ffe9dc', FONT_SERIF),
    # shops
    ('ShoeStore', 'SHOES', '#212121', '#ffffff', '#f2f2f2', FONT_ITAL),
    ('ApplianceStore', 'APPLIANCES', '#0d47a1', '#ffffff', '#d8ecff', FONT_BOLD),
    ('HardwareStore', 'HARDWARE', '#c62828', '#ffffff', '#ffe5e5', FONT_BOLD),
    ('Pharmacy', 'PHARMACY', '#ffffff', '#2e7d32', '#e8ffe8', FONT_BOLD),
    ('Drugstore', 'DRUGSTORE', '#e53935', '#ffffff', '#ffe5e5', FONT_BOLD),
    ('Bookstore', 'BOOKS', '#1b3a2f', '#f2e3b3', '#fff1cc', FONT_SERIF),
    ('ToyStore', 'TOYS', '#ffffff', '#ff5722', '#ffe0d0', FONT_BOLD),
    ('SportsStore', 'SPORTS', '#0d47a1', '#ffeb3b', '#fff59d', FONT_ITAL),
    ('Jeweler', 'JEWELERS', '#111111', '#e6c068', '#ffe08a', FONT_SERIF),
    ('LiquorStore', 'WINE & SPIRITS', '#4a0e1f', '#f2e3b3', '#fff1cc', FONT_SERIF),
    ('PetStore', 'PETS', '#29b6f6', '#ffffff', '#e8f8ff', FONT_BOLD),
    ('GardenCenter', 'GARDEN CENTER', '#2e7d32', '#ffffff', '#e8ffe8', FONT_BOLD),
    ('Florist', 'FLOWERS', '#fce4ec', '#ad1457', '#ffe0ee', FONT_LORA),
    ('BikeShop', 'CYCLES', '#ff6f00', '#ffffff', '#ffe9d0', FONT_ITAL),
    ('MusicStore', 'MUSIC', '#1b1b1b', '#ff4081', '#ff80ab', FONT_BOLD),
    ('StationeryStore', 'STATIONERY', '#fffde7', '#283593', '#fff8d0', FONT_SEMI),
    ('HomeGoodsStore', 'HOME GOODS', '#efebe9', '#5d4037', '#fff3e8', FONT_SEMI),
    ('Boutique', 'BOUTIQUE', '#111111', '#f5f5f5', '#ffffff', FONT_LORA),
    ('ThriftStore', 'THRIFT', '#7cb342', '#ffffff', '#efffe0', FONT_BOLD),
    ('DepartmentStore', 'DEPARTMENT STORE', '#b71c1c', '#ffffff', '#ffe5e5', FONT_SERIF),
    ('PhoneStore', 'MOBILE', '#6200ea', '#ffffff', '#efe0ff', FONT_BOLD),
    ('MattressStore', 'MATTRESSES', '#1565c0', '#ffffff', '#e3f2ff', FONT_BOLD),
    ('ConvenienceStore', 'CORNER STORE', '#ef6c00', '#ffffff', '#ffe9d0', FONT_BOLD),
    ('Butcher', 'BUTCHER', '#7f0000', '#fff8e1', '#ffe5e5', FONT_SERIF),
    ('Bakery', 'BAKERY', '#f8e1c0', '#6d3b1a', '#fff0d8', FONT_LORA),
    ('FarmersMarket', 'FARMERS MARKET', '#33691e', '#fff9c4', '#fffbd8', FONT_SERIF),
    ('FishMarket', 'FISH MARKET', '#01579b', '#ffffff', '#e1f5fe', FONT_BOLD),
    # food and drink
    ('Pizzeria', 'PIZZA', '#c62828', '#ffffff', '#ffe5e5', FONT_ITAL),
    ('BurgerJoint', 'BURGERS', '#ffc107', '#b71c1c', '#fff2b0', FONT_BOLD),
    ('Diner', 'DINER', '#e0f7fa', '#d32f2f', '#ff6b6b', FONT_ITAL),
    ('SushiBar', 'SUSHI', '#111111', '#ffffff', '#ffffff', FONT_BOLD),
    ('TacoShop', 'TACOS', '#ff6f00', '#1b5e20', '#ffe9b0', FONT_BOLD),
    ('Steakhouse', 'STEAKHOUSE', '#3e2723', '#ffcc80', '#ffd699', FONT_SERIF),
    ('NoodleBar', 'NOODLES', '#d32f2f', '#ffeb3b', '#fff59d', FONT_BOLD),
    ('ChickenShack', 'FRIED CHICKEN', '#ffffff', '#c62828', '#ffe5e5', FONT_BOLD),
    ('SandwichShop', 'SANDWICHES', '#2e7d32', '#ffeb3b', '#fff59d', FONT_BOLD),
    ('FineDining', 'RESTAURANT', '#1b1b1b', '#e6d3a3', '#fff0c8', FONT_LORA),
    ('BBQJoint', 'BBQ', '#3e2723', '#ff7043', '#ff9a70', FONT_BOLD),
    ('SeafoodRestaurant', 'SEAFOOD', '#0277bd', '#ffffff', '#e1f5fe', FONT_SERIF),
    ('CurryHouse', 'CURRY HOUSE', '#e65100', '#fff3e0', '#ffe6c8', FONT_SERIF),
    ('ThaiRestaurant', 'THAI KITCHEN', '#4a148c', '#ffd54f', '#ffe082', FONT_SERIF),
    ('TeaHouse', 'TEA HOUSE', '#e8f5e9', '#1b5e20', '#e8ffe8', FONT_LORA),
    ('JuiceBar', 'JUICE BAR', '#76ff03', '#1b5e20', '#ccff90', FONT_BOLD),
    ('DonutShop', 'DONUTS', '#f8bbd0', '#6d4c41', '#ffe0ee', FONT_BOLD),
    ('IceCreamParlor', 'ICE CREAM', '#e1f5fe', '#e91e63', '#ffb0cc', FONT_ITAL),
    ('CandyShop', 'SWEETS', '#ffffff', '#d81b60', '#ffd0e4', FONT_LORA),
    ('BakeryCafe', 'BAKEHOUSE', '#4e342e', '#ffe0b2', '#ffe9c8', FONT_LORA),
    ('SportsBar', 'SPORTS BAR', '#0d1b2a', '#4fc3f7', '#81d4fa', FONT_BOLD),
    ('WineBar', 'WINE BAR', '#3b0a1a', '#f8bbd0', '#ffc0d8', FONT_LORA),
    ('Brewpub', 'BREWPUB', '#4e342e', '#ffca28', '#ffe082', FONT_SERIF),
    ('CocktailLounge', 'LOUNGE', '#0b0b14', '#ce93d8', '#e1a8ff', FONT_LORA),
    ('Nightclub', 'CLUB', '#000000', '#00e5ff', '#40f0ff', FONT_BOLD),
    ('KaraokeBar', 'KARAOKE', '#1a0033', '#ff4081', '#ff80ab', FONT_ITAL),
    # fitness, looks, fun
    ('YogaStudio', 'YOGA', '#f3e5f5', '#6a1b9a', '#f8e8ff', FONT_LORA),
    ('BoxingGym', 'BOXING', '#b71c1c', '#ffffff', '#ffe5e5', FONT_BOLD),
    ('ClimbingGym', 'CLIMBING', '#ff6f00', '#212121', '#ffe0b0', FONT_BOLD),
    ('SwimClub', 'SWIM CLUB', '#0288d1', '#ffffff', '#e1f5fe', FONT_ITAL),
    ('MartialArtsDojo', 'DOJO', '#ffffff', '#b71c1c', '#ffe5e5', FONT_BOLD),
    ('Barbershop', 'BARBER', '#ffffff', '#c62828', '#ffe5e5', FONT_SERIF),
    ('NailSalon', 'NAILS', '#fce4ec', '#c2185b', '#ffd0e4', FONT_LORA),
    ('DaySpa', 'DAY SPA', '#e0f2f1', '#00695c', '#e0fff8', FONT_LORA),
    ('TattooParlor', 'TATTOO', '#111111', '#ff1744', '#ff6b80', FONT_SERIF),
    ('TanningSalon', 'TANNING', '#ff8f00', '#ffffff', '#ffe9c4', FONT_ITAL),
    ('Cinema', 'CINEMA', '#1a1a1a', '#ffd54f', '#ffe082', FONT_BOLD),
    ('BowlingAlley', 'BOWLING', '#0d47a1', '#ff5252', '#ff8a80', FONT_ITAL),
    ('Arcade', 'ARCADE', '#120024', '#ff00e5', '#ff70f0', FONT_MONO),
    ('PoolHall', 'BILLIARDS', '#1b5e20', '#ffffff', '#e8ffe8', FONT_SERIF),
    ('EscapeRoom', 'ESCAPE', '#212121', '#ffeb3b', '#fff59d', FONT_MONO),
    ('MiniGolf', 'MINI GOLF', '#43a047', '#ffffff', '#e8ffe8', FONT_ITAL),
    ('SkatingRink', 'SKATING', '#e3f2fd', '#1565c0', '#d8ecff', FONT_ITAL),
    ('Theater', 'THEATER', '#5d0b0b', '#ffd54f', '#ffe082', FONT_SERIF),
    ('ComedyClub', 'COMEDY', '#111111', '#ffeb3b', '#fff59d', FONT_BOLD),
    # health, household, family
    ('Dentist', 'DENTAL', '#ffffff', '#0288d1', '#e1f5fe', FONT_SEMI),
    ('Optician', 'EYECARE', '#ffffff', '#37474f', '#f2f6f8', FONT_SEMI),
    ('VetClinic', 'ANIMAL HOSPITAL', '#ffffff', '#2e7d32', '#e8ffe8', FONT_BOLD),
    ('PhysioClinic', 'PHYSIO', '#e0f7fa', '#00838f', '#d8fbff', FONT_SEMI),
    ('Laundromat', 'LAUNDROMAT', '#0288d1', '#ffffff', '#e1f5fe', FONT_BOLD),
    ('DryCleaner', 'CLEANERS', '#ffffff', '#1a237e', '#eef0ff', FONT_SERIF),
    ('Tailor', 'TAILOR', '#263238', '#eceff1', '#f2f6f8', FONT_LORA),
    ('PhoneRepair', 'PHONE REPAIR', '#ff5722', '#ffffff', '#ffe0d0', FONT_BOLD),
    ('CarWash', 'CAR WASH', '#29b6f6', '#ffffff', '#e8f8ff', FONT_ITAL),
    ('Daycare', 'DAYCARE', '#fff59d', '#1565c0', '#fffbd0', FONT_BOLD),
    ('MusicSchool', 'MUSIC SCHOOL', '#311b92', '#ffffff', '#ede7ff', FONT_LORA),
    ('DanceStudio', 'DANCE', '#ad1457', '#ffffff', '#ffe0ee', FONT_LORA),
    ('TutoringCenter', 'LEARNING CENTER', '#ffffff', '#2e7d32', '#e8ffe8', FONT_SEMI),
    ('DrivingSchool', 'DRIVING SCHOOL', '#ffeb3b', '#212121', '#fff59d', FONT_BOLD),
    # offices
    ('LawFirm', 'ATTORNEYS AT LAW', '#1b1b1b', '#e6d3a3', '#fff0c8', FONT_SERIF),
    ('AccountingFirm', 'ACCOUNTANTS', '#1d3557', '#ffffff', '#e8f0ff', FONT_SERIF),
    ('InsuranceAgency', 'INSURANCE', '#0d47a1', '#ffffff', '#d8ecff', FONT_SEMI),
    ('RealEstateAgency', 'REALTORS', '#ffffff', '#b71c1c', '#ffe5e5', FONT_BOLD),
    ('ArchitectureFirm', 'ARCHITECTS', '#eeeeee', '#212121', '#ffffff', FONT_SEMI),
    ('MarketingAgency', 'MEDIA', '#ff4081', '#ffffff', '#ffe0ee', FONT_BOLD),
    ('SoftwareStudio', 'LABS', '#0b0b0f', '#69f0ae', '#a0ffd0', FONT_MONO),
    ('CallCenter', 'CONTACT CENTER', '#0277bd', '#ffffff', '#e1f5fe', FONT_SEMI),
    ('EngineeringFirm', 'ENGINEERING', '#37474f', '#ffffff', '#f2f6f8', FONT_BOLD),
    ('DesignStudio', 'DESIGN', '#ffffff', '#111111', '#f2f2f2', FONT_LORA),
    # visitors and the rest
    ('Motel', 'MOTEL', '#b71c1c', '#ffffff', '#ff6b6b', FONT_ITAL),
    ('Hotel', 'HOTEL', '#1b1b1b', '#e6c068', '#ffe08a', FONT_SERIF),
    ('BedAndBreakfast', 'BED & BREAKFAST', '#f5ecd7', '#5d4037', '#fff3d6', FONT_LORA),
    ('FuneralHome', 'FUNERAL HOME', '#263238', '#eceff1', '#f2f6f8', FONT_SERIF),
    ('TaxiCompany', 'TAXI', '#ffd600', '#111111', '#fff59d', FONT_BOLD),
]


def all_more(out, cs_path):
    """signs2 (the chains, then the first of these) and as many more atlases as these need; and the game's table of rows."""
    rows = list(SIGNS2) + [r[1:] for r in SIGNS_MORE]
    atlases = []
    for i in range(0, len(rows), 32):
        name = 'signs' + str(2 + i // 32)
        signs(out, rows[i:i + 32], name)
        atlases.append(name)
    first = 32 + len(SIGNS2)
    lines = ['using System.Collections.Generic;', 'using RealLife.Simulation;', '', 'namespace RealLife.Game.Art', '{',
             '    /// <summary>Sign rows for the kinds of business added later: written by tools/textures/generate.py (signs.py), keep',
             '    /// them in step. Rows from 32 on are on signs2.png, 64 on on signs3.png and so on.</summary>',
             '    public static class SignRows', '    {',
             '        public const int Atlases = ' + str(len(atlases) + 1) + ';',
             '        static readonly Dictionary<BuildingType, int> rows = new Dictionary<BuildingType, int>',
             '        {']
    for k, r in enumerate(SIGNS_MORE):
        lines.append(f'            {{ BuildingType.{r[0]}, {first + k} }},')
    lines += ['        };', '',
              '        public static int For(BuildingType t) { int r; return rows.TryGetValue(t, out r) ? r : SignRow.Open; }',
              '    }', '}', '']
    with open(cs_path, 'w') as f:
        f.write('\n'.join(lines))
    return atlases
