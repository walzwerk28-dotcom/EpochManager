"""
Werkstättenblock der Handwerkerzone (Antike), 4 x 4 Felder (8 x 8 m), zwei Varianten (der größte Bau der Zone, damit tiefe
Blöcke ganz gefüllt werden):
  Variante 0: Geschlossener Block um einen Hof. Vorn ein zweistöckiges ockerfarbenes Haus mit zwei Läden (Töpfer und
              Tuchhändler, Markisen), zwei Haustüren und einer Hofeinfahrt mit Bogen; an den Seiten niedrigere Flügel
              mit Läden zur Außenseite; hinten ein dreistöckiges rotes Haus mit Läden und Holzbalkon zum Hof. Im Hof
              ein Brunnenbecken als Blickfang, ringsum gemeinsame Werkplätze (Zimmermannsbank mit Brettern, Amboss mit
              Trog, Töpferscheibe mit Regal, Brennholz), Karren, Bänke.
  Variante 1: Zwei Häuserzeilen vorn und hinten (je drei Häuser mit Giebel zur Straße bzw. zur Gasse, in Rot, Gelb, Blau
              und Ocker, Weiß, Gelb), dazwischen eine Werkgasse mit Zisterne, zwei Marktständen, Schuppen mit Zimmerei
              (links) und Töpferei (rechts), Karren mit Heu, Fässern und Waren; die Enden der Gasse sind mit niedrigen
              Mauern mit Durchgang abgeschlossen.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/werkstaettenblock.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 7,36 x 7,36 m (Grundfläche 8 x 8 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Werkstaettenblock_0, Werkstaettenblock_1). Hofmauern liegen mit der Außenfläche in der Ebene der Hauswand und enden 1,5 cm
im Hauskörper.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(361)

R = 3.30              # Außenfläche des Blocks (davor Platz für Markisen und Auslagen)
D = 2.00              # Innenfläche der Flügel (Hof bzw. Gasse)
EMB = 0.015
WT = 0.09
H15 = STOREY + 0.75   # eineinhalb Stockwerke
H2 = 2 * STOREY - 0.05
H3 = 3 * STOREY - 0.05
PLASTER_BLUE = mat("PutzBlau", (0.47, 0.60, 0.74))


def out_pt(face, plane, u, out):
    """Punkt (x, y) vor einer Wand: u entlang der Wand, out nach außen."""
    at = face_frame(face, plane)[0]
    p = at(u, out, 0.0)
    return p[0], p[1]


def side_wing(x_out, x_in, plaster, socle, low_side, h_low=1.45, pitch=16.0):
    """Seitenflügel zwischen Vorder- und Hinterhaus mit Pultdach, das zum Hof fällt."""
    x0, x1 = sorted((x_out, x_in))
    y0, y1 = -D - EMB, D + EMB
    body(x0, x1, y0, y1, h_low, plaster, socle=socle)
    rise = (x1 - x0) * math.tan(math.radians(pitch))
    tri = [(x0, h_low), (x1, h_low), (x0, h_low + rise)] if low_side == "+x" else [(x0, h_low), (x1, h_low), (x1, h_low + rise)]
    slab(tri, "xz", y0, y1 - y0, plaster)
    light_shed_roof(x0, x1, y0, y1, h_low + rise, low_side, overhang=0.04, pitch=pitch, eave_overhang=0.04)


def shed(x0, x1, y0, y1, zh, low_side, plaster, posts_x, wall_x):
    """Offener Schuppen an einer Mauer (wall_x = Außenfläche der Mauer), Pultdach fällt zur freien Seite."""
    light_shed_roof(x0, x1, y0, y1, zh, low_side, overhang=0.05, pitch=16.0, eave_overhang=0.05)
    sign = 1 if low_side[0] == "+" else -1
    under = lambda x: zh - abs(x - wall_x) * math.tan(math.radians(16.0))
    xa, xb = (x0, x1) if sign > 0 else (x1, x0)
    for y in (y0 + 0.05, y1 - 0.05):
        slab([(xa, 0.0), (xb, 0.0), (xb, under(xb) - 0.005), (xa, zh - 0.005)], "xz", y - 0.04, 0.08, plaster)
    for y in (y0 + 0.05, (y0 + y1) / 2, y1 - 0.05):
        box((0.07, 0.07, under(posts_x) - 0.01), (posts_x, y, (under(posts_x) - 0.01) / 2), material=WOOD_DARK)
    box((0.07, y1 - y0, 0.07), (posts_x, (y0 + y1) / 2, under(posts_x) - 0.035), material=WOOD_DARK)
    wx0, wx1 = (wall_x - WT, wall_x) if wall_x > 0 else (wall_x, wall_x + WT)
    flush_wall(wx0, wx1, y0 - 0.05, y1 + 0.05, zh - 0.01, plaster, cap=False, plinth=False)


def facade_row(face, plane, items):
    """Fenster- und Türreihe: items = [(u, z, kind, shutter material, open)]"""
    for u, z, kind, shut, opened in items:
        if kind == "w":
            window(face, plane, u, z, shutters=shut, open_shutters=opened)
        else:
            door(face, plane, u)


# ================================================================ Variante 0: geschlossener Block um einen Hof

# Flügel
body(-R, R, -R, -D, H2, PLASTER_OCHRE, socle=SOCLE_RED)                       # vorn
light_gable_roof(-R, R, -R, -D, H2, "x", PLASTER_OCHRE, overhang=0.03)
cornice(-R, R, -R, -D, STOREY)
body(-R, R, D, R, H3, PLASTER_RED, socle=None)                                # hinten
light_gable_roof(-R, R, D, R, H3, "x", PLASTER_RED, overhang=0.03)
cornice(-R, R, D, R, STOREY)
cornice(-R, R, D, R, 2 * STOREY)
side_wing(-R, -D, PLASTER_YELLOW, SOCLE_RED, "+x")
side_wing(R, D, PLASTER_BLUE, SOCLE_RED, "-x")

# Vorderseite: Töpfer, Tür, Hofeinfahrt, Tür, Tuchhändler
cx, cy = shopfront_open("-y", -R, -2.45, w=0.80, awning=(CLOTH_RED, CLOTH))
pot(cx - 0.17, cy, 0.33, 0.045)
bowl(cx + 0.17, cy, 0.33, 0.055, TERRACOTTA_LIGHT)
door("-y", -R, -1.45)
sign_board("-y", -R, -1.45, 0.88, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=TERRACOTTA)
arch_opening("-y", -R, 0.0, 0.66, 0.46, z0=0.0)
box((0.60, 0.012, 0.86), (0.0, -R - 0.002, 0.43), material=WOOD_DARK)
for s in (-1, 1):
    box((0.30, 0.02, 0.84), (s * 0.15, -R - 0.012, 0.42), material=WOOD)
    box((0.30, 0.03, 0.03), (s * 0.15, -R - 0.025, 0.25), material=IRON)
    box((0.30, 0.03, 0.03), (s * 0.15, -R - 0.025, 0.66), material=IRON)
door("-y", -R, 1.45)
sign_board("-y", -R, 1.45, 0.88, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="bar", mark_mat=CLOTH_PURPLE)
cx, cy = shopfront_open("-y", -R, 2.45, w=0.80, awning=(CLOTH_BLUE, CLOTH))
for k, cols in enumerate(((CLOTH_RED, CLOTH), (CLOTH_BLUE, CLOTH_YELLOW), (CLOTH_PURPLE, CLOTH))):
    cloth_bale(cx - 0.20 + k * 0.20, cy, 0.33, yaw=(k - 1) * 6, colors=cols)
for u in (-2.45, -1.45, 1.45, 2.45):
    window("-y", -R, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u in (-2.45, 1.45)))
for u in (-0.75, 0.75):
    window("-y", -R, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u > 0))
window("-y", -R, 0.0, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=False)
wall_banner("-y", -R, -0.75, 1.80, 0.20, 0.52, CLOTH_RED, trim=CLOTH_YELLOW)
wall_banner("-y", -R, 0.75, 1.80, 0.20, 0.52, CLOTH_BLUE, trim=CLOTH_YELLOW)
vessel_shelf(-1.45, -R - 0.26, 0, w=0.62, tiers=2, fired=True, seed=21)
amphora_row((2.95, -R - 0.26), (3.12, -R - 0.30), 2)
sack_stack(-3.00, -R - 0.24, yaw=0, per=(2, 1))
# Vorderhaus zum Hof
door("+y", -D, 0.0, w=0.34)
for u in (-2.60, -1.80, -0.90, 0.90, 1.80, 2.60):
    window("+y", -D, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u > 0))
    window("+y", -D, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u < 0))
# Hinterhaus: außen Läden, innen Balkon
cx, cy = shopfront_open("+y", R, -1.90, w=0.80, awning=(PAINT_GREEN, CLOTH))
for k in range(3):
    basket_goods(cx - 0.20 + k * 0.20, cy, 0.33, 0.062, "apples", seed=k)
door("+y", R, 0.0)
cx, cy = shopfront_open("+y", R, 1.90, w=0.80, awning=(CLOTH_YELLOW, CLOTH))
for k in range(3):
    pot(cx - 0.20 + k * 0.20, cy, 0.33, 0.04) if k != 1 else bowl(cx, cy, 0.33, 0.055, TERRACOTTA_LIGHT)
for z in (STOREY + 0.28, 2 * STOREY + 0.28):
    for u in (-2.60, -1.90, -1.20, 0.0, 1.20, 1.90, 2.60):
        window("+y", R, u, z, shutters=PAINT_GREEN, open_shutters=((u < 0) == (z < 2)))
window("+y", R, -1.20, 0.42, shutters=PAINT_GREEN, open_shutters=False)
window("+y", R, 1.20, 0.42, shutters=PAINT_GREEN, open_shutters=False)
balcony(D, -1.30, 1.30, STOREY + 0.04, depth=0.20)
door("-y", D, 0.0, w=0.34)
door("-y", D, 0.0, w=0.30, h=0.50, z0=STOREY + 0.04)
for u in (-2.40, -1.60, -0.80, 0.80, 1.60, 2.40):
    window("-y", D, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u > 0))
    window("-y", D, u, 2 * STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u < 0))
for u in (-2.40, 2.40):
    window("-y", D, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=False)
# Seitenflügel: Läden und Fenster nach außen, Fenster und Tür zum Hof
cx, cy = shopfront_open("-x", -R, -0.95, w=0.70, awning=(CLOTH_RED, CLOTH))
pot(cx, cy - 0.15, 0.33, 0.045)
pot(cx, cy + 0.15, 0.33, 0.045)
door("-x", -R, 0.60)
window("-x", -R, -0.95, STOREY + 0.18, shutters=PAINT_GREEN, open_shutters=False)
window("-x", -R, 0.60, STOREY + 0.18, shutters=PAINT_GREEN, open_shutters=True)
window("-x", -R, 1.45, 0.42, shutters=PAINT_GREEN)
cx, cy = shopfront_open("+x", R, 0.95, w=0.70, awning=(CLOTH_PURPLE, CLOTH))
for k in range(2):
    cloth_bale(cx, cy - 0.12 + k * 0.24, 0.33, yaw=90, colors=(CLOTH_GREEN, CLOTH))
door("+x", R, -0.60)
window("+x", R, 0.95, STOREY + 0.18, shutters=PAINT_BLUE, open_shutters=True)
window("+x", R, -0.60, STOREY + 0.18, shutters=PAINT_BLUE, open_shutters=False)
window("+x", R, -1.45, 0.42, shutters=PAINT_BLUE)
for v in (-1.10, 0.0, 1.10):
    window("+x", -D, v, 0.42, shutters=PAINT_GREEN, open_shutters=(v > 0))
    window("-x", D, v, 0.42, shutters=PAINT_BLUE, open_shutters=(v < 0))
door("+x", -D, 0.0, w=0.34) if False else None
# Enden von Vorder- und Hinterhaus (x-Seiten)
for face, plane, pl in (("-x", -R, PAINT_GREEN), ("+x", R, PAINT_GREEN)):
    for v in (-3.00, -2.55):
        window(face, plane, v, 0.42, shutters=pl, open_shutters=(v < -2.8))
        window(face, plane, v, STOREY + 0.28, shutters=pl, open_shutters=False)
    for v in (2.55, 3.00):
        for z in (0.42, STOREY + 0.28, 2 * STOREY + 0.28):
            window(face, plane, v, z, shutters=pl, open_shutters=(v > 2.8))
# Hof: Brunnen als Blickfang, gemeinsame Werkplätze, Bänke
fountain_basin(0.0, 0.0, r=0.42, h=0.20)
path([(0.0, -D + 0.05), (0.0, -0.55)], w=0.22)
path([(0.0, 0.55), (0.0, D - 0.05)], w=0.22)
bench(-0.95, 0.0, yaw=90, length=0.50)
bench(0.95, 0.0, yaw=90, length=0.50)
workbench(-1.45, -1.35, yaw=0)
plank_stack(-1.60, -0.55, 0.26, 0.80, 4, yaw=90, seed=7)
anvil(1.35, -1.35)
trough(1.60, -0.75, 90, length=0.40, width=0.16, h=0.13, liquid=WATER)
barrel(1.60, -1.70, 0.0, r=0.065)
potters_wheel(1.35, 1.25, yaw=0)
vessel_shelf(1.88, 0.75, -90, w=0.60, tiers=2, fired=False, seed=22)
woodpile(-1.40, 1.55, length=0.45, rows=(4, 3, 2), yaw=0)
cart(-0.65, 1.25, yaw=-25, load="sacks")
stool(-1.00, -1.35)
bpy.context.view_layer.update()
if os.environ.get("TRIS"):
    from collections import Counter
    _c = Counter()
    for _o in PARTS:
        _c[_o.name.split(".")[0]] += sum(len(q.vertices) - 2 for q in _o.data.polygons)
    print("TRIS", _c.most_common(14))
plane_report("Block 0", (PLASTER_OCHRE, PLASTER_RED, PLASTER_YELLOW, PLASTER_BLUE))
check_bounds(3.68, 3.68, "0")
variant("Werkstaettenblock_0")

# ================================================================ Variante 1: zwei Häuserzeilen mit Werkgasse

# vordere Zeile: drei Häuser mit Giebel zur Straße
front_row = ((-R, -1.10, H15, PLASTER_RED, None, PAINT_BLUE), (-1.08, 1.08, H2, PLASTER_YELLOW, SOCLE_RED, PAINT_GREEN),
             (1.10, R, H15, PLASTER_BLUE, SOCLE_RED, WOOD))
for x0, x1, h, pl, so, sh in front_row:
    body(x0, x1, -R, -D, h, pl, socle=so)
    light_gable_roof(x0, x1, -R, -D, h, "y", pl, overhang=0.03, pitch=24.0)
    cornice(x0, x1, -R, -D, STOREY)
    xc = (x0 + x1) / 2
    for u in (xc - 0.65, xc + 0.65):
        window("+y", -D, u, 0.42, shutters=sh, open_shutters=(u > xc))
        window("+y", -D, u, STOREY + (0.20 if h < 2 else 0.28), shutters=sh, open_shutters=(u < xc))
    door("+y", -D, xc)
    face, plane = ("-x", x0) if x0 < 0 else ("+x", x1)
cx, cy = shopfront_open("-y", -R, -2.55, w=0.62, awning=(CLOTH_BLUE, CLOTH))
for k in range(2):
    pot(cx - 0.10 + k * 0.20, cy, 0.33, 0.04)
door("-y", -R, -1.60)
sign_board("-y", -R, -1.60, 0.88, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=TERRACOTTA)
window("-y", -R, -2.55, STOREY + 0.18, shutters=PAINT_BLUE, open_shutters=False)
window("-y", -R, -1.60, STOREY + 0.18, shutters=PAINT_BLUE, open_shutters=True)
cx, cy = shopfront_open("-y", -R, -0.45, w=0.72, awning=(CLOTH_RED, CLOTH))
for k, cols in enumerate(((CLOTH_RED, CLOTH), (CLOTH_BLUE, CLOTH_YELLOW), (CLOTH_PURPLE, CLOTH))):
    cloth_bale(cx - 0.18 + k * 0.18, cy, 0.33, yaw=(k - 1) * 6, colors=cols)
door("-y", -R, 0.55)
sign_board("-y", -R, 0.55, 0.88, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="bar", mark_mat=CLOTH_PURPLE)
for u in (-0.45, 0.55):
    window("-y", -R, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u < 0))
wall_banner("-y", -R, 0.05, 1.80, 0.18, 0.44, CLOTH_YELLOW, trim=CLOTH_RED)
cx, cy = shopfront_open("-y", -R, 2.85, w=0.62, awning=(CLOTH_YELLOW, CLOTH))
for k in range(2):
    basket_goods(cx - 0.10 + k * 0.20, cy, 0.33, 0.058, "bread", seed=k)
door("-y", -R, 1.75)
window("-y", -R, 2.85, STOREY + 0.18, shutters=WOOD, open_shutters=True)
window("-y", -R, 1.75, STOREY + 0.18, shutters=WOOD, open_shutters=False)
for face, plane, pl in (("-x", -R, PAINT_BLUE), ("+x", R, WOOD)):
    for v in (-3.00, -2.55):
        window(face, plane, v, 0.42, shutters=pl, open_shutters=(v < -2.8))
        window(face, plane, v, STOREY + 0.18, shutters=pl, open_shutters=False)

# hintere Zeile: drei Häuser, Giebel zur Gasse, Läden zur Gasse
back_row = ((-R, -1.10, H3, PLASTER_OCHRE, SOCLE_RED, PAINT_GREEN), (-1.08, 1.08, H2, PLASTER_WHITE, SOCLE_RED, PAINT_BLUE),
            (1.10, R, H3, PLASTER_YELLOW, SOCLE_RED, WOOD))
for x0, x1, h, pl, so, sh in back_row:
    body(x0, x1, D, R, h, pl, socle=so)
    light_gable_roof(x0, x1, D, R, h, "y", pl, overhang=0.03, pitch=24.0)
    cornice(x0, x1, D, R, STOREY)
    if h > 2.5:
        cornice(x0, x1, D, R, 2 * STOREY)
    xc = (x0 + x1) / 2
    zs = (STOREY + 0.28, 2 * STOREY + 0.28) if h > 2.5 else (STOREY + 0.28,)
    for z in zs:
        for u in (xc - 0.65, xc + 0.65):
            window("-y", D, u, z, shutters=sh, open_shutters=((u < xc) == (z < 2)))
            window("+y", R, u, z, shutters=sh, open_shutters=((u > xc) == (z < 2)))
    for u in (xc - 0.65, xc + 0.65):
        window("+y", R, u, 0.42, shutters=sh)
    door("+y", R, xc)
cx, cy = shopfront_open("-y", D, -2.55, w=0.62, awning=(CLOTH_RED, CLOTH))
for k in range(3):
    jug(cx - 0.17 + k * 0.17, cy, 0.33, 0.034, TERRACOTTA_LIGHT)
door("-y", D, -1.60)
cx, cy = shopfront_open("-y", D, -0.40, w=0.72, awning=(PAINT_GREEN, CLOTH))
for k in range(3):
    basket_goods(cx - 0.20 + k * 0.20, cy, 0.33, 0.058, "olives", seed=k + 3)
door("-y", D, 0.55)
cx, cy = shopfront_open("-y", D, 2.85, w=0.62, awning=(CLOTH_PURPLE, CLOTH))
for k in range(2):
    cloth_bale(cx - 0.10 + k * 0.20, cy, 0.33, yaw=(k - 0.5) * 8, colors=(CLOTH_GREEN, CLOTH_RED))
door("-y", D, 1.75)
for face, plane, pl in (("-x", -R, PAINT_GREEN), ("+x", R, WOOD)):
    for v in (2.55, 3.00):
        for z in (0.42, STOREY + 0.28, 2 * STOREY + 0.28):
            window(face, plane, v, z, shutters=pl, open_shutters=(v > 2.8))

# Werkgasse: niedrige Mauern an den Enden mit Durchgang
for sx in (-1, 1):
    wx0, wx1 = (-R, -R + WT) if sx < 0 else (R - WT, R)
    pl = PLASTER_YELLOW if sx < 0 else PLASTER_BLUE
    flush_wall(wx0, wx1, -D - EMB, -0.60, 0.45, pl)
    flush_wall(wx0, wx1, 0.60, D + EMB, 0.45, pl)
# links: Zimmerei unter offenem Schuppen
shed(-R, -1.90, -1.40, 0.10, 1.20, "+x", PLASTER_YELLOW, -1.94, -R)
workbench(-2.75, -0.85, yaw=0)
sawhorse(-2.60, -0.20, yaw=0, log_len=0.8)
plank_stack(-3.05, -0.10, 0.22, 0.60, 4, yaw=90, seed=8)
woodpile(-2.75, 1.55, length=0.45, rows=(4, 3, 2), yaw=0)
# rechts: Töpferei unter offenem Schuppen
shed(1.90, R, 0.30, 1.80, 1.20, "-x", PLASTER_BLUE, 1.94, R)
potters_wheel(2.55, 1.05, yaw=0)
vessel_shelf(3.15, 1.05, -90, w=0.70, tiers=2, fired=False, seed=24)
amphora_row((2.20, 1.90), (2.55, 1.90), 2)
# Mitte: Zisterne, Marktstände, Karren, Waren
cistern(0.10, 0.10)
path([(-2.60, -0.55), (-0.60, -0.50), (0.10, 0.80), (2.60, 0.55)], w=0.22)
market_stall(-1.05, -1.55, 1.00, "wine", (CLOTH_RED, CLOTH))
market_stall(1.15, -1.55, 1.00, "oil", (PAINT_BLUE, CLOTH))
cart(-0.90, 1.05, yaw=-25, load="hay")
barrel(0.85, 1.20, 0.0, r=0.065)
barrel(1.02, 1.30, 0.0, r=0.065)
sack_stack(0.75, 0.85, yaw=0, per=(2, 1))
bench(-0.70, 0.55, yaw=0, length=0.50)
stool(0.55, -0.25)
bpy.context.view_layer.update()
plane_report("Block 1", (PLASTER_RED, PLASTER_YELLOW, PLASTER_BLUE, PLASTER_OCHRE, PLASTER_WHITE))
check_bounds(3.68, 3.68, "1")
variant("Werkstaettenblock_1")

export(spacing=12.0)
