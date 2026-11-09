"""
Werkhof der Handwerkerzone (Antike), 2 x 3 Felder (4 m Front, 6 m tief), zwei Varianten (wie die früheren Kenney-Modelle:
Variante 0 = Haus an der Straße mit Hof und Holzhalle dahinter, Variante 1 = Haus mit Werkhof):
  Variante 0: Schmiede. Breites zweistöckiges rotes Haus mit Traufe zur Straße (Ladenöffnung mit Eisenwaren, Markise, Tür,
              Schild, Fenster mit Läden); im Hof zwischen Hofmauern hinten links eine offene Holzhalle mit Pultdach, in der
              die Esse an der Rückwand steht (Rauchfang und Schlot ragen über das Dach, Rauch), davor Amboss, Löschtrog,
              Blasebalg; im freien Hof Werkzeugtisch, Eisenbarren, Kohlenhaufen, Pflug, Fässer, Brennholz.
  Variante 1: Färberei. Breites zweistöckiges blaues Haus (Ladenöffnung mit Stoffballen, Tür, Banner, Fenster); im Hof eine
              Reihe Färbebottiche mit Indigo, Krapp, Resede und Purpur, Rührstangen, Trog, dahinter zwei Leinen mit bunten
              Stoffen zum Trocknen, Fässer, Säcke mit Färbepflanzen, Bank.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/werkhof.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 5,52 m (Grundfläche 4 x 6 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Werkhof_0, Werkhof_1). Alle Hofmauern liegen mit der Außenfläche in der Ebene der Hauswand (x = +-W bzw. y = YB), an die
sie anschließen, und enden 1,5 cm im Hauskörper.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(331)

W = 1.77              # Außenfläche der Seitenwände
EMB = 0.015
WT = 0.09
YB = 2.70             # Rückwand des Hofs
Y0 = -2.32            # Vorderwand des Hauses
Y1 = -1.40            # Rückwand des Hauses
H2 = 2 * STOREY - 0.05
PLASTER_BLUE = mat("PutzBlau", (0.47, 0.60, 0.74))


def house(plaster, socle, shutters, front_windows):
    """Einstöckiges Haus an der Straße (niedrig, damit man in den Hof sieht), Traufe zur Straße."""
    body(-W, W, Y0, Y1, STOREY, plaster, socle=socle)
    light_gable_roof(-W, W, Y0, Y1, STOREY, "x", plaster, overhang=0.03, pitch=26.0)
    for u in front_windows:
        window("-y", Y0, u, 0.42, shutters=shutters, open_shutters=(u < 0))
    for u in (-1.35, -0.60, 0.60, 1.35):
        window("+y", Y1, u, 0.42, shutters=shutters, open_shutters=(u > 0))
    door("+y", Y1, 0.0, w=0.34)
    for face, plane in (("-x", -W), ("+x", W)):
        for v in (-2.05, -1.70):
            window(face, plane, v, 0.42, shutters=shutters, open_shutters=(v < -1.9))


def yard_walls(plaster):
    flush_wall(-W, -W + WT, Y1 - EMB, YB, 0.40, plaster)
    flush_wall(W - WT, W, Y1 - EMB, YB, 0.40, plaster)


# ---------------------------------------------------------------- Variante 0: Schmiede

house(PLASTER_RED, None, PAINT_BLUE, (0.0, 1.30))
cx, cy = shopfront_open("-y", Y0, -0.95, w=0.90, awning=(IRON, CLOTH_YELLOW))
for k in range(5):                                                                  # Eisenwaren auf der Theke
    box((0.020, 0.020, 0.16), (cx - 0.30 + k * 0.15, cy, 0.41), rot=(0, 10 * (k - 2), 0), material=IRON)
    box((0.050, 0.020, 0.040), (cx - 0.30 + k * 0.15, cy, 0.50), rot=(0, 10 * (k - 2), 0), material=IRON)
door("-y", Y0, 0.45)
sign_board("-y", Y0, 0.45, 0.88, w=0.18, h=0.10, arm=0.15, board=IRON, mark="disc", mark_mat=FLAME)
barrel(1.20, Y0 - 0.28, 0.0, r=0.07)
barrel(1.40, Y0 - 0.30, 0.0, r=0.07)
anvil(1.62, Y0 - 0.30)
yard_walls(PLASTER_RED)
flush_wall(0.60, W, YB - WT, YB, 0.60, PLASTER_RED)
# Holzhalle hinten links (Pultdach fällt nach hinten), Esse an der Rückwand
HX1, HY0, ZH = 0.60, 1.20, 1.32
light_shed_roof(-W, HX1, HY0, YB, ZH, "+y", overhang=0.05, pitch=16.0, eave_overhang=0.0)
under = lambda y: ZH - (y - HY0) * math.tan(math.radians(16.0))
slab([(HY0, 0.0), (YB, 0.0), (YB, under(YB) - 0.005), (HY0, ZH)], "yz", -W, WT, PLASTER_RED)
body(-W, HX1, YB - WT, YB, under(YB) - 0.005, PLASTER_RED, socle=None)
for x in (-W + 0.07, -0.55, HX1 - 0.05):
    box((0.07, 0.07, ZH - 0.01), (x, HY0 + 0.07, (ZH - 0.01) / 2), material=WOOD_DARK)
box((HX1 + W, 0.07, 0.07), ((HX1 - W) / 2, HY0 + 0.07, ZH - 0.035), material=WOOD_DARK)
bar((HX1 - 0.05, HY0 + 0.07, 0.30), (HX1 - 0.05, YB - 0.30, 0.25), 0.05, WOOD_DARK)
box((0.07, 0.07, under(YB - 0.30) - 0.01), (HX1 - 0.05, YB - 0.30, (under(YB - 0.30) - 0.01) / 2), material=WOOD_DARK)
for u in (-1.20, 0.10):
    window("+y", YB, u, 0.34, w=0.14, h=0.20, shutters=None)
window("-x", -W, 2.00, 0.34, w=0.14, h=0.20, shutters=None)
top = forge(-0.95, 2.28, yaw=0, z_chimney=under(2.36) + 0.30)
smoke_plume(-0.95, 2.36, top)
bellows(-1.50, 2.30, yaw=0)
tool_wall(-0.20, 0.50, YB - WT, 0.28, 0.74)
anvil(-0.90, 1.62)
trough(-0.30, 1.75, 0, length=0.40, width=0.16, h=0.13, liquid=WATER)
for k in range(3):                                                                  # Eisenbarren neben der Esse
    box((0.20, 0.040, 0.030), (-0.20, 2.05, 0.015 + k * 0.032), rot=(0, 0, 4 * k), material=IRON)
sack(-1.55, 1.55, 0.0, 0.07)
# freier Hof
trestle_table(0.95, 0.20, 0, w=0.80, d=0.30, h=0.28, cloth=None)
for k in range(4):
    rod((0.70 + k * 0.14, 0.17, 0.295), (0.70 + k * 0.14, 0.17, 0.40), 0.008, WOOD_DARK, verts=5)
    box((0.07, 0.025, 0.035), (0.70 + k * 0.14, 0.17, 0.405), material=IRON)                  # Hämmer
box((0.30, 0.05, 0.016), (1.05, 0.30, 0.298), rot=(0, 0, 10), material=IRON)
for k in range(3):                                                                  # Stapel Eisenbarren im Hof
    for j in range(3 - k):
        box((0.28, 0.045, 0.035), (-1.30, 0.40 + (j - (2 - k) / 2) * 0.05, 0.0175 + k * 0.035), rot=(0, 0, 2 * j), material=IRON)
sphere(0.20, (1.30, 1.55, 0.07), (1.0, 1.0, 0.40), SOOT, segs=8, rings=4)
sphere(0.12, (1.50, 1.30, 0.05), (1.0, 1.0, 0.45), SOOT, segs=7, rings=4)
plough(1.20, 0.90, yaw=70)
barrel(1.55, 0.35, 0.0, r=0.07)
woodpile(0.40, 1.20, length=0.40, rows=(4, 3), yaw=0)
cart(1.00, 1.95, yaw=-20, load="sacks")
crate(1.45, 1.05, 0.0, 0.18, yaw=10)
crate(1.45, 1.05, 0.18, 0.14, yaw=-8)
stool(-0.40, 0.45)
bpy.context.view_layer.update()
plane_report("Werkhof 0", (PLASTER_RED,))
check_bounds(1.84, 2.76, "0")
variant("Werkhof_0")

# ---------------------------------------------------------------- Variante 1: Färberei

house(PLASTER_BLUE, SOCLE_RED, WOOD, (-1.30, 0.0))
cx, cy = shopfront_open("-y", Y0, 0.95, w=0.90, awning=(CLOTH_PURPLE, CLOTH))
for k, cols in enumerate(((CLOTH_RED, CLOTH), (CLOTH_BLUE, CLOTH_YELLOW), (CLOTH_PURPLE, CLOTH), (CLOTH_GREEN, CLOTH_RED))):
    cloth_bale(cx - 0.27 + k * 0.17, cy, 0.33, yaw=(k - 1.5) * 5, colors=cols)
door("-y", Y0, -0.45)
sign_board("-y", Y0, -0.45, 0.88, w=0.18, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=CLOTH_PURPLE)
wall_banner("-y", Y0, 0.0, 0.90, 0.20, 0.40, CLOTH_RED, trim=CLOTH_YELLOW)
dye_vat(-1.30, Y0 - 0.24, r=0.16, h=0.24, liquid=DYE_INDIGO, pole=False)
cloth_bale(-0.90, Y0 - 0.34, 0.0, yaw=8, colors=(CLOTH_BLUE, CLOTH), w=0.20, d=0.12, h=0.12)
yard_walls(PLASTER_BLUE)
flush_wall(-W, W, YB - WT, YB, 0.62, PLASTER_BLUE)
for u in (-1.30, -0.45, 0.45, 1.30):
    window("+y", YB, u, 0.34, w=0.14, h=0.20, shutters=None)
# Färbebottiche in einer Reihe
for k, (liq, px) in enumerate(((DYE_INDIGO, -1.20), (DYE_MADDER, -0.40), (DYE_WELD, 0.40), (CLOTH_PURPLE, 1.20))):
    dye_vat(px, 0.10, r=0.24, h=0.32, liquid=liq, pole=(k % 2 == 0))
trough(0.0, 0.75, 0, length=0.60, width=0.20, h=0.14, liquid=DYE_INDIGO)
# Trockenleinen mit gefärbten Stoffen
cloth_rail((-1.55, 1.55), (1.55, 1.55), 0.84, 0.56, (CLOTH_BLUE, CLOTH_RED, CLOTH_YELLOW, CLOTH_PURPLE, CLOTH_GREEN), width=0.19, seed=1)
cloth_rail((-1.55, 2.30), (1.55, 2.30), 0.84, 0.56, (CLOTH_YELLOW, CLOTH_PURPLE, CLOTH_BLUE, CLOTH_GREEN, CLOTH_RED), width=0.19, seed=2)
barrel(1.55, 0.95, 0.0, r=0.07)
barrel(1.55, 1.12, 0.0, r=0.07)
sack(-1.58, 0.90, 0.0, 0.07)
sack(-1.58, 1.08, 0.0, 0.06)
bench(-1.45, 1.20, yaw=90, length=0.5)
cart(-0.95, 0.95, yaw=15, load="sacks")
sphere(0.16, (1.20, 0.80, 0.05), (1.0, 1.0, 0.45), LEAF_DARK, segs=8, rings=4)
sphere(0.10, (1.40, 0.95, 0.04), (1.0, 1.0, 0.45), LEAF, segs=7, rings=4)
crate(1.40, 1.45, 0.0, 0.18, yaw=5)
bpy.context.view_layer.update()
plane_report("Werkhof 1", (PLASTER_BLUE,))
check_bounds(1.84, 2.76, "1")
variant("Werkhof_1")

export(spacing=4.4)
