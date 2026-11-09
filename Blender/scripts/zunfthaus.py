"""
Zunfthaus der Handwerkerzone (Antike), 3 x 3 Felder (6 x 6 m), zwei Varianten (wie die früheren Kenney-Modelle:
Variante 0 = Steinbau mit Bogentor, Balkon und Bannern, Werkhof dahinter; Variante 1 = drei Läden um einen Hof mit Brunnen):
  Variante 0: zweistöckige Zunfthalle aus Kalkstein mit Ziegelbändern, Satteldach (Traufe zur Straße); in der Mitte ein
              Bogentor, darüber ein Holzbalkon mit Tür, links und rechts rote und blaue Banner, Fenster mit Läden; dahinter
              ein Werkhof zwischen Hofmauern mit Statue der Schutzgöttin auf einem Podest, Bänken, Lorbeer, einem offenen
              Werkschuppen links (Werkbänke, Fässer), Bohlen, Sägebock und Karren rechts, Rückwand mit Tor und Fenstern.
  Variante 1: drei Läden nebeneinander (links Weber mit gelbem Haus, in der Mitte der Schuster in einem dreistöckigen roten
              Haus mit Giebel zur Straße, rechts Töpfer mit blauem Haus), jeder mit Markise, Tür, Schild und Auslage; dahinter
              ein Hof mit Brunnen, Bänken, Lorbeer und Herme, hinten rechts ein Werkschuppen, links Karren und Bohlen.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/zunfthaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 5,52 x 5,52 m (Grundfläche 6 x 6 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Zunfthaus_0, Zunfthaus_1). Alle Hofmauern liegen mit der Außenfläche in der Ebene der Hauswand (x = +-W bzw. y = YB), an die
sie anschließen, und enden 1,5 cm im Hauskörper.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(341)

W = 2.65              # Außenfläche der Seitenwände
EMB = 0.015
WT = 0.10
YB = 2.65             # Rückwand des Hofs
H2 = 2 * STOREY - 0.05
PLASTER_BLUE = mat("PutzBlau", (0.47, 0.60, 0.74))

# ---------------------------------------------------------------- Variante 0: Zunfthalle mit Bogentor

Y0, Y1 = -2.45, -1.05
body(-W, W, Y0, Y1, H2, LIME, socle=None)
box((2 * W + 0.04, Y1 - Y0 + 0.04, 0.16), (0.0, (Y0 + Y1) / 2, 0.08), material=LIME_DARK)
brick_bands(-W, W, Y0, Y1, (0.30, 1.15, 1.80))
light_gable_roof(-W, W, Y0, Y1, H2, "x", LIME, overhang=0.03)
cornice(-W, W, Y0, Y1, STOREY)
arch_opening("-y", Y0, 0.0, 0.66, 0.46, z0=0.0)
box((0.60, 0.012, 0.86), (0.0, Y0 - 0.002, 0.43), material=WOOD_DARK)
for s in (-1, 1):
    box((0.30, 0.02, 0.84), (s * 0.15, Y0 - 0.012, 0.42), material=WOOD)
    box((0.30, 0.03, 0.03), (s * 0.15, Y0 - 0.025, 0.25), material=IRON)
    box((0.30, 0.03, 0.03), (s * 0.15, Y0 - 0.025, 0.66), material=IRON)
balcony(Y0, -0.55, 0.55, STOREY + 0.04, depth=0.20)
door("-y", Y0, 0.0, w=0.30, h=0.56, z0=STOREY + 0.04)
for u in (-1.30, 1.30, -2.10, 2.10):
    window("-y", Y0, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u in (-1.30, 2.10)))
    window("-y", Y0, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u in (1.30, -2.10)))
wall_banner("-y", Y0, -0.95, 1.80, 0.22, 0.56, CLOTH_RED, trim=CLOTH_YELLOW)
wall_banner("-y", Y0, 0.95, 1.80, 0.22, 0.56, CLOTH_BLUE, trim=CLOTH_YELLOW)
for face, plane in (("-x", -W), ("+x", W)):
    for v in (-2.05, -1.45):
        window(face, plane, v, 0.42, shutters=PAINT_GREEN, open_shutters=False)
        window(face, plane, v, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(v < -1.8))
door("+y", Y1, 0.0, w=0.34)
for u in (-1.80, -0.90, 0.90, 1.80):
    window("+y", Y1, u, 0.42, shutters=PAINT_GREEN)
    window("+y", Y1, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u > 0))
# Hofmauern bündig mit den Wänden der Halle
flush_wall(-W, -W + WT, Y1 - EMB, YB, 0.50, LIME)
flush_wall(W - WT, W, Y1 - EMB, YB, 0.50, LIME)
flush_wall(-W, W, YB - WT, YB, 0.90, LIME)
arch_opening("+y", YB, 0.0, 0.62, 0.50, z0=0.0)
for u in (-1.80, -1.00, 1.00, 1.80):
    window("+y", YB, u, 0.44, w=0.16, h=0.22, shutters=None)
# Statue der Schutzgöttin auf Podest, Bänke, Lorbeer
box((0.60, 0.60, 0.14), (0.0, 0.70, 0.07), material=LIME_DARK)
box((0.50, 0.50, 0.04), (0.0, 0.70, 0.16), material=LIME)
statue(0.0, 0.70, yaw=0, h=0.70)
path([(0.0, -0.95), (0.0, 0.38)], w=0.22)
bench(-0.85, 0.70, yaw=90, length=0.50)
bench(0.85, 0.70, yaw=90, length=0.50)
laurel_tree(-1.10, 1.50, 1.1)
laurel_tree(1.10, 1.50, 1.1)
# offener Werkschuppen links, Pultdach fällt zum Hof
SX1, SY0, SY1, ZH = -1.30, 0.20, 2.55, 1.20
light_shed_roof(-W, SX1, SY0, SY1, ZH, "+x", overhang=0.05, pitch=16.0, eave_overhang=0.05)
under = lambda x: ZH - (x + W) * math.tan(math.radians(16.0))
for y in (SY0 + 0.05, SY1 - 0.05):
    slab([(-W, 0.0), (SX1, 0.0), (SX1, under(SX1) - 0.005), (-W, ZH - 0.005)], "xz", y - 0.04, 0.08, LIME)
for y in (SY0 + 0.05, (SY0 + SY1) / 2, SY1 - 0.05):
    box((0.07, 0.07, under(SX1) - 0.01), (SX1 - 0.04, y, (under(SX1) - 0.01) / 2), material=WOOD_DARK)
box((0.07, SY1 - SY0, 0.07), (SX1 - 0.04, (SY0 + SY1) / 2, under(SX1) - 0.035), material=WOOD_DARK)
workbench(-2.15, 0.85, yaw=0)
workbench(-2.15, 1.85, yaw=0)
for k in range(3):
    barrel(-1.80 + k * 0.0, 1.20 + k * 0.17, 0.0, r=0.065)
vessel_shelf(-2.10, 2.42, 0, w=0.90, tiers=2, fired=False, seed=8)
stool(-1.70, 0.85)
# rechts: Bohlen, Sägebock, Karren, Brennholz
plank_stack(1.95, 1.00, 0.26, 1.00, 5, yaw=0, seed=5)
sawhorse(1.90, 1.95, yaw=0, log_len=0.9)
cart(1.30, 0.20, yaw=-30, load="sacks")
woodpile(1.60, 2.40, length=0.50, rows=(4, 3, 2), yaw=90)
bpy.context.view_layer.update()
plane_report("Zunfthaus 0", (LIME,))
check_bounds(2.76, 2.76, "0")
variant("Zunfthaus_0")

# ---------------------------------------------------------------- Variante 1: drei Läden um einen Hof mit Brunnen

Y0, Y1 = -2.30, -1.15
XA, XB, XC = -0.88, 0.88, W            # Trennlinien: A = [-W, XA - 0.01], B = [XA + 0.01, XB - 0.01], C = [XB + 0.01, W]
H3 = 3 * STOREY - 0.05
# links: Weber (gelb)
body(-W, XA - 0.01, Y0, Y1, H2, PLASTER_YELLOW, socle=SOCLE_RED)
light_gable_roof(-W, XA - 0.01, Y0, Y1, H2, "x", PLASTER_YELLOW, overhang=0.03)
cornice(-W, XA - 0.01, Y0, Y1, STOREY)
cx, cy = shopfront_open("-y", Y0, -2.12, w=0.64, awning=(CLOTH_BLUE, CLOTH))
for k, cols in enumerate(((CLOTH_RED, CLOTH), (CLOTH_BLUE, CLOTH_YELLOW), (CLOTH_PURPLE, CLOTH))):
    cloth_bale(cx - 0.15 + k * 0.15, cy, 0.33, yaw=(k - 1) * 6, colors=cols)
door("-y", Y0, -1.28)
sign_board("-y", Y0, -1.28, 0.86, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=CLOTH_PURPLE)
for u in (-2.12, -1.28):
    window("-y", Y0, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u < -2))
wall_cloths("-y", Y0, -2.25, -1.60, 1.80, 0.46, (CLOTH_RED, CLOTH_YELLOW))
window("-x", -W, -1.75, 0.42, shutters=PAINT_GREEN)
window("-x", -W, -1.75, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=False)
for u in (-2.12, -1.28):
    window("+y", Y1, u, 0.42, shutters=PAINT_GREEN)
    window("+y", Y1, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u < -2))
stool(-0.98, Y0 - 0.30)
loom(-2.00, Y0 - 0.34, 0, cloth=CLOTH_RED, stripe=CLOTH_YELLOW, h=0.74)
# Mitte: Schuster (rot, dreistöckig, Giebel zur Straße)
body(XA + 0.01, XB - 0.01, Y0, Y1, H3, PLASTER_RED, socle=None)
light_gable_roof(XA + 0.01, XB - 0.01, Y0, Y1, H3, "y", PLASTER_RED, overhang=0.03)
cornice(XA + 0.01, XB - 0.01, Y0, Y1, STOREY)
cornice(XA + 0.01, XB - 0.01, Y0, Y1, 2 * STOREY)
cx, cy = shopfront_open("-y", Y0, 0.28, w=0.60, awning=(CLOTH_YELLOW, CLOTH))
for k, yaw in enumerate((20, -15, 10, -25)):
    shoe(cx - 0.18 + k * 0.12, cy + (0.02 if k % 2 else -0.02), 0.33, yaw=90 + yaw, material=LEATHER if k % 2 else LEATHER_DARK)
door("-y", Y0, -0.45)
sign_board("-y", Y0, -0.45, 0.86, w=0.16, h=0.10, arm=0.15, board=LEATHER, mark="bar", mark_mat=CLOTH_YELLOW)
for z in (STOREY + 0.28, 2 * STOREY + 0.28):
    for u in (-0.45, 0.28):
        window("-y", Y0, u, z, shutters=PAINT_BLUE, open_shutters=((u < 0) == (z < 2)))
for u in (-0.45, 0.28):
    for z in (0.42, STOREY + 0.28, 2 * STOREY + 0.28):
        window("+y", Y1, u, z, shutters=PAINT_BLUE, open_shutters=(z > 2))
trestle_table(0.30, Y0 - 0.36, 0, w=0.44, d=0.20, h=0.26)
shoe(0.22, Y0 - 0.36, 0.268, yaw=80)
shoe(0.36, Y0 - 0.34, 0.268, yaw=100, material=LEATHER_DARK)
hide_frame(-0.55, Y0 - 0.30, yaw=0, w=0.36, h=0.38, hide=LEATHER)
# rechts: Töpfer (blau)
body(XB + 0.01, W, Y0, Y1, H2, PLASTER_BLUE, socle=SOCLE_RED)
light_gable_roof(XB + 0.01, W, Y0, Y1, H2, "x", PLASTER_BLUE, overhang=0.03)
cornice(XB + 0.01, W, Y0, Y1, STOREY)
cx, cy = shopfront_open("-y", Y0, 1.45, w=0.64, awning=(CLOTH_RED, CLOTH))
for k in range(3):
    pot(cx - 0.20 + k * 0.20, cy, 0.33, 0.04) if k != 1 else bowl(cx, cy, 0.33, 0.055, TERRACOTTA_LIGHT)
door("-y", Y0, 2.28)
sign_board("-y", Y0, 2.28, 0.86, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=TERRACOTTA)
for u in (1.45, 2.28):
    window("-y", Y0, u, STOREY + 0.28, shutters=WOOD, open_shutters=(u > 2))
window("+x", W, -1.75, 0.42, shutters=WOOD)
window("+x", W, -1.75, STOREY + 0.28, shutters=WOOD, open_shutters=False)
for u in (1.45, 2.28):
    window("+y", Y1, u, 0.42, shutters=WOOD)
    window("+y", Y1, u, STOREY + 0.28, shutters=WOOD, open_shutters=(u < 2))
vessel_shelf(1.55, Y0 - 0.34, 0, w=0.60, tiers=2, fired=True, seed=9)
amphora(2.22, Y0 - 0.30)
amphora(2.40, Y0 - 0.38)
# Hof
flush_wall(-W, -W + WT, Y1 - EMB, YB, 0.50, PLASTER_YELLOW)
flush_wall(W - WT, W, Y1 - EMB, YB, 0.50, PLASTER_BLUE)
flush_wall(-W, W, YB - WT, YB, 0.80, PLASTER_OCHRE)
for u in (-2.00, -1.20, 1.20, 2.00):
    window("+y", YB, u, 0.44, w=0.16, h=0.22, shutters=None)
fountain_basin(0.0, 0.40, r=0.50, h=0.20)
bench(-0.95, 0.40, yaw=90, length=0.50)
bench(0.95, 0.40, yaw=90, length=0.50)
laurel_tree(-1.40, 1.30, 1.1)
laurel_tree(1.40, 1.30, 1.1)
herm(0.0, 1.50, 0, h=0.45)
path([(0.0, -0.95), (0.0, -0.20)], w=0.22)
# Werkschuppen hinten rechts, Pultdach fällt nach hinten
SX0, SY0, ZH = 0.90, 1.70, 1.15
light_shed_roof(SX0, W, SY0, YB, ZH, "+y", overhang=0.05, pitch=16.0, eave_overhang=0.0)
under = lambda y: ZH - (y - SY0) * math.tan(math.radians(16.0))
slab([(SY0, 0.0), (YB, 0.0), (YB, under(YB) - 0.005), (SY0, ZH)], "yz", W - WT, WT, PLASTER_BLUE)
slab([(SY0, 0.0), (YB, 0.0), (YB, under(YB) - 0.005), (SY0, ZH)], "yz", SX0 - 0.04, 0.06, PLASTER_BLUE)
body(SX0, W, YB - WT, YB, under(YB) - 0.005, PLASTER_OCHRE, socle=None)
for x in (SX0 + 0.04, 1.80, W - 0.07):
    box((0.07, 0.07, ZH - 0.01), (x, SY0 + 0.07, (ZH - 0.01) / 2), material=WOOD_DARK)
box((W - SX0, 0.07, 0.07), ((SX0 + W) / 2, SY0 + 0.07, ZH - 0.035), material=WOOD_DARK)
workbench(1.45, 2.15, yaw=90)
barrel(2.20, 2.00, 0.0, r=0.065)
barrel(2.20, 2.20, 0.0, r=0.065)
# links: Karren, Bohlen, Brennholz
plank_stack(-1.95, 1.90, 0.26, 0.90, 4, yaw=90, seed=6)
cart(-1.20, 0.90, yaw=25, load="hay")
woodpile(-1.80, 2.40, length=0.50, rows=(4, 3, 2), yaw=0)
bpy.context.view_layer.update()
plane_report("Zunfthaus 1", (PLASTER_YELLOW, PLASTER_BLUE, PLASTER_RED))
check_bounds(2.76, 2.76, "1")
variant("Zunfthaus_1")

export(spacing=6.4)
