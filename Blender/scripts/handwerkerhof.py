"""
Handwerkerhof der Handwerkerzone (Antike), 2 x 4 Felder (4 x 8 m), zwei Varianten (je ein Vorderhaus an der Straße, ein
langer Hof und ein Hinterhaus; das tiefste Haus der Zone neben dem Werkhof mit 3 Feldern):
  Variante 0: Töpferei. Zweistöckiges ockerfarbenes Vorderhaus mit Töpferladen (Markise, Gefäße auf der Theke, Regal und
              Amphoren davor), dahinter ein langer Hof: links ein offener Schuppen mit zwei Töpferscheiben und Regal mit
              ungebrannten Gefäßen, rechts Schlämmbecken, Trockentisch, Brennofen mit Rauchfaden und Brennholz, hinten ein
              rotes Wohnhaus mit Holzbalkon.
  Variante 1: Tuchhändler. Blaues Vorderhaus mit Giebel zur Straße (Stoffballen, Wandtücher, Banner), dahinter links
              Färbebottiche und Trog, rechts ein offener Webschuppen mit zwei Webstühlen, in der Mitte Trockenleinen mit
              gefärbten Tüchern, hinten ein gelbes Hinterhaus.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/handwerkerhof.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 7,36 m (Grundfläche 4 x 8 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Handwerkerhof_0, Handwerkerhof_1). Hofmauern liegen mit der Außenfläche in der Ebene der Hauswand und enden 1,5 cm im
Hauskörper.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(351)

W = 1.77              # Außenfläche der Seitenwände
EMB = 0.015
WT = 0.09
Y0 = -3.22            # Vorderwand des Vorderhauses (davor Platz für Markisen und Auslagen)
Y1 = -2.12            # Rückwand des Vorderhauses
Y2 = 2.30             # Vorderwand des Hinterhauses
YB = 3.60             # Rückwand des Hinterhauses
PLASTER_BLUE = mat("PutzBlau", (0.47, 0.60, 0.74))
H2 = 2 * STOREY - 0.05
HB = STOREY + 0.75    # Hinterhaus: Erdgeschoss und niedriges Obergeschoss


def shed(x0, x1, y0, y1, zh, low_side, plaster, posts_x, wall_x):
    """Offener Schuppen im Hof an einer Hofmauer (wall_x = Außenfläche dieser Mauer), Pultdach fällt zur freien Seite."""
    light_shed_roof(x0, x1, y0, y1, zh, low_side, overhang=0.05, pitch=16.0, eave_overhang=0.05)
    sign = 1 if low_side[0] == "+" else -1
    under = lambda x: zh - abs(x - wall_x) * math.tan(math.radians(16.0))
    # Hofmauer unter dem Dach bis zur Dachkante hochgezogen, Stirnwände an den Enden
    xa, xb = (x0, x1) if sign > 0 else (x1, x0)
    for y in (y0 + 0.05, y1 - 0.05):
        slab([(xa, 0.0), (xb, 0.0), (xb, under(xb) - 0.005), (xa, zh - 0.005)], "xz", y - 0.04, 0.08, plaster)
    for y in (y0 + 0.05, (y0 + y1) / 2, y1 - 0.05):
        box((0.07, 0.07, under(posts_x) - 0.01), (posts_x, y, (under(posts_x) - 0.01) / 2), material=WOOD_DARK)
    box((0.07, y1 - y0, 0.07), (posts_x, (y0 + y1) / 2, under(posts_x) - 0.035), material=WOOD_DARK)


def court_walls(plaster_l, plaster_r):
    flush_wall(-W, -W + WT, Y1 - EMB, Y2 + EMB, 0.40, plaster_l)
    flush_wall(W - WT, W, Y1 - EMB, Y2 + EMB, 0.40, plaster_r)


# ---------------------------------------------------------------- Variante 0: Töpferei

body(-W, W, Y0, Y1, H2, PLASTER_OCHRE, socle=SOCLE_RED)
light_gable_roof(-W, W, Y0, Y1, H2, "x", PLASTER_OCHRE, overhang=0.03)
cornice(-W, W, Y0, Y1, STOREY)
cx, cy = shopfront_open("-y", Y0, -0.90, w=0.95, awning=(CLOTH_RED, CLOTH))
for k, px in enumerate((-0.20, 0.20)):
    if k == 1:
        bowl(cx + px, cy, 0.33, 0.06, TERRACOTTA_LIGHT)
    else:
        pot(cx + px, cy, 0.33, 0.045)
door("-y", Y0, 0.45)
sign_board("-y", Y0, 0.45, 0.88, w=0.17, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=TERRACOTTA)
for u in (-0.90, 0.45, 1.30):
    window("-y", Y0, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u < 0))
window("-y", Y0, 1.30, 0.42, shutters=PAINT_GREEN, open_shutters=False)
for face, plane in (("-x", -W), ("+x", W)):
    for v in (-2.95, -2.55):
        window(face, plane, v, 0.42, shutters=PAINT_GREEN, open_shutters=(v < -2.8))
        window(face, plane, v, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=False)
door("+y", Y1, 0.0, w=0.34)
for u in (-1.20, -0.55, 0.55, 1.20):
    window("+y", Y1, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u > 0))
    window("+y", Y1, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u < 0))
vessel_shelf(1.25, Y0 - 0.30, 0, w=0.62, tiers=2, fired=True, seed=11)
amphora_row((-1.60, Y0 - 0.30), (-1.35, Y0 - 0.36), 2)

# Hof
court_walls(PLASTER_OCHRE, PLASTER_OCHRE)
# links: offener Schuppen mit Töpferscheiben
SX1, SY0, SY1, ZH = -0.55, -0.95, 0.65, 1.15
shed(-W, SX1, SY0, SY1, ZH, "+x", PLASTER_OCHRE, SX1 - 0.04, -W)
flush_wall(-W, -W + WT, SY0 - 0.05, SY1 + 0.05, ZH - 0.01, PLASTER_OCHRE, cap=False, plinth=False)
potters_wheel(-1.05, -0.15, yaw=0)
potters_wheel(0.10, 0.20, yaw=0)
vessel_shelf(-1.62, -0.15, 90, w=0.80, tiers=2, fired=False, seed=12)
# rechts: Schlämmbecken, Trockentisch, Brennofen
clay_basin(0.85, 1.55, -1.95, -1.45)
trestle_table(0.35, -1.10, 0, w=0.70, d=0.28, h=0.28, cloth=None)
for k, px in enumerate((0.20, 0.48)):
    pot(px, -1.10, 0.286, 0.04) if k % 2 == 0 else bowl(px, -1.10, 0.286, 0.05, TERRACOTTA_LIGHT)

top = kiln(1.00, 1.15, r=0.38)
smoke_plume(1.00, 1.15, top)
woodpile(0.30, 1.55, length=0.45, rows=(4, 3, 2), yaw=0)
brushwood(0.15, 0.85, yaw=20)
cart(-0.60, 1.55, yaw=25, load="sacks")

stool(0.50, 0.30)
path([(0.0, Y1 - 0.02), (0.0, -1.6), (0.5, -0.4), (0.6, 0.55)], w=0.18)
# Hinterhaus
body(-W, W, Y2, YB, HB, PLASTER_RED, socle=None)
light_gable_roof(-W, W, Y2, YB, HB, "x", PLASTER_RED, overhang=0.03)
cornice(-W, W, Y2, YB, STOREY)
balcony(Y2, -0.95, 0.95, STOREY + 0.04, depth=0.20)
door("-y", Y2, 0.0, w=0.34)
door("-y", Y2, 0.0, w=0.30, h=0.50, z0=STOREY + 0.04)
for u in (-1.30, -0.65, 0.65, 1.30):
    window("-y", Y2, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u > 0))
for u in (-1.30, 1.30):
    window("-y", Y2, u, STOREY + 0.26, shutters=PAINT_GREEN, open_shutters=(u < 0))
for u in (-1.10, 0.0, 1.10):
    window("+y", YB, u, 0.42, shutters=PAINT_GREEN)
    window("+y", YB, u, STOREY + 0.26, shutters=PAINT_GREEN, open_shutters=(u > 0))
for face, plane in (("-x", -W), ("+x", W)):
    window(face, plane, 2.95, 0.42, shutters=PAINT_GREEN)
    window(face, plane, 2.95, STOREY + 0.26, shutters=PAINT_GREEN, open_shutters=False)
bench(0.0, Y2 - 0.30, yaw=0, length=0.50)
bpy.context.view_layer.update()
if os.environ.get("TRIS"):
    from collections import Counter
    _c = Counter()
    for _o in PARTS:
        _c[_o.name.split(".")[0]] += sum(len(q.vertices) - 2 for q in _o.data.polygons)
    print("TRIS", _c.most_common(14))
plane_report("Hof 0", (PLASTER_OCHRE, PLASTER_RED))
check_bounds(1.84, 3.68, "0")
variant("Handwerkerhof_0")

# ---------------------------------------------------------------- Variante 1: Tuchhändler

body(-W, W, Y0, Y1, H2, PLASTER_BLUE, socle=SOCLE_RED)
light_gable_roof(-W, W, Y0, Y1, H2, "y", PLASTER_BLUE, overhang=0.03, pitch=22.0)
cornice(-W, W, Y0, Y1, STOREY)
cx, cy = shopfront_open("-y", Y0, -0.80, w=1.00, awning=(CLOTH_PURPLE, CLOTH))
for k, cols in enumerate(((CLOTH_RED, CLOTH), (CLOTH_BLUE, CLOTH_YELLOW), (CLOTH_PURPLE, CLOTH), (CLOTH_GREEN, CLOTH_RED))):
    cloth_bale(cx - 0.30 + k * 0.20, cy, 0.33, yaw=(k - 1.5) * 5, colors=cols)
door("-y", Y0, 0.55)
sign_board("-y", Y0, 0.55, 0.88, w=0.17, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=CLOTH_PURPLE)
for u in (-0.80, 0.55, 1.35):
    window("-y", Y0, u, STOREY + 0.28, shutters=WOOD, open_shutters=(u > 0))
window("-y", Y0, 1.35, 0.42, shutters=WOOD, open_shutters=False)
wall_cloths("-y", Y0, -1.60, -1.15, 1.80, 0.42, (CLOTH_RED, CLOTH_YELLOW))
wall_banner("-y", Y0, 0.0, 1.76, 0.20, 0.38, CLOTH_YELLOW, trim=CLOTH_RED)
for face, plane in (("-x", -W), ("+x", W)):
    for v in (-2.95, -2.55):
        window(face, plane, v, 0.42, shutters=WOOD, open_shutters=(v < -2.8))
        window(face, plane, v, STOREY + 0.28, shutters=WOOD, open_shutters=False)
door("+y", Y1, 0.0, w=0.34)
for u in (-1.20, -0.55, 0.55, 1.20):
    window("+y", Y1, u, 0.42, shutters=WOOD, open_shutters=(u > 0))
    window("+y", Y1, u, STOREY + 0.28, shutters=WOOD, open_shutters=(u < 0))
cloth_bale(1.55, Y0 - 0.30, 0.0, yaw=8, colors=(CLOTH_BLUE, CLOTH), w=0.22, d=0.13, h=0.14)
cloth_bale(1.30, Y0 - 0.34, 0.0, yaw=-6, colors=(CLOTH_RED, CLOTH), w=0.22, d=0.13, h=0.14)
sack_stack(-1.45, Y0 - 0.34, yaw=0, per=(2, 1))
# Hof
court_walls(PLASTER_BLUE, PLASTER_BLUE)
# links: Färbebottiche und Trog
for k, (liq, py) in enumerate(((DYE_INDIGO, -1.65), (DYE_MADDER, -0.95), (DYE_WELD, -0.25))):
    dye_vat(-1.20, py, r=0.22, h=0.30, liquid=liq, pole=(k % 2 == 0))
trough(-0.40, -1.65, 0, length=0.60, width=0.20, h=0.14, liquid=DYE_INDIGO)
barrel(-1.60, 0.30, 0.0, r=0.07)
barrel(-1.60, 0.48, 0.0, r=0.07)
bench(-1.45, 0.95, yaw=90, length=0.5)
# rechts: Webschuppen
SX0, SY0, SY1, ZH = 0.55, -1.10, 0.50, 1.15
shed(SX0, W, SY0, SY1, ZH, "-x", PLASTER_BLUE, SX0 + 0.04, W)
flush_wall(W - WT, W, SY0 - 0.05, SY1 + 0.05, ZH - 0.01, PLASTER_BLUE, cap=False, plinth=False)
loom(1.35, -0.30, 90, cloth=CLOTH_RED, stripe=CLOTH_YELLOW, h=0.78)
loom(0.10, 0.45, 90, cloth=CLOTH_BLUE, stripe=CLOTH_YELLOW, h=0.78)
stool(1.00, -0.30)
stool(0.40, 0.45)
cloth_bale(0.95, 0.80, 0.0, yaw=0, colors=(CLOTH_GREEN, CLOTH), w=0.20, d=0.12, h=0.12)
# Mitte: Trockenleinen mit gefärbten Tüchern
cloth_rail((-0.55, 1.15), (0.50, 1.15), 0.84, 0.54, (CLOTH_BLUE, CLOTH_RED, CLOTH_PURPLE), width=0.19, seed=4)
cloth_rail((-1.45, 1.80), (1.45, 1.80), 0.84, 0.54, (CLOTH_YELLOW, CLOTH_PURPLE, CLOTH_BLUE, CLOTH_GREEN, CLOTH_RED), width=0.19, seed=5)
cart(0.10, -0.55, yaw=-20, load="sacks")
trestle_table(-0.75, 0.45, 0, w=0.60, d=0.26, h=0.28, cloth=CLOTH)
cloth_bale(-0.85, 0.45, 0.286, yaw=0, colors=(CLOTH_PURPLE, CLOTH), w=0.18, d=0.10, h=0.07)
cloth_bale(-0.62, 0.45, 0.286, yaw=0, colors=(CLOTH_RED, CLOTH), w=0.18, d=0.10, h=0.07)
crate(1.50, 1.45, 0.0, 0.18, yaw=5)
path([(0.0, Y1 - 0.02), (0.0, -1.2), (0.0, 0.0)], w=0.18)
# Hinterhaus
body(-W, W, Y2, YB, H2, PLASTER_YELLOW, socle=SOCLE_RED)
light_gable_roof(-W, W, Y2, YB, H2, "x", PLASTER_YELLOW, overhang=0.03)
cornice(-W, W, Y2, YB, STOREY)
door("-y", Y2, 0.0, w=0.34)
for u in (-1.30, -0.65, 0.65, 1.30):
    window("-y", Y2, u, 0.42, shutters=WOOD, open_shutters=(u > 0))
for u in (-1.30, -0.45, 0.45, 1.30):
    window("-y", Y2, u, STOREY + 0.28, shutters=WOOD, open_shutters=(u < 0))
for u in (-1.10, 0.0, 1.10):
    window("+y", YB, u, 0.42, shutters=WOOD)
    window("+y", YB, u, STOREY + 0.28, shutters=WOOD, open_shutters=(u > 0))
for face, plane in (("-x", -W), ("+x", W)):
    window(face, plane, 2.95, 0.42, shutters=WOOD)
    window(face, plane, 2.95, STOREY + 0.28, shutters=WOOD, open_shutters=False)
woodpile(-1.30, Y2 - 0.30, length=0.45, rows=(4, 3, 2), yaw=0)
bpy.context.view_layer.update()
plane_report("Hof 1", (PLASTER_BLUE, PLASTER_YELLOW))
check_bounds(1.84, 3.68, "1")
variant("Handwerkerhof_1")

export(spacing=7.0)
