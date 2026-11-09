"""
Speicherhof der Gewerbezone (Antike), 2 x 4 Felder (4 m Front, 8 m tief), zwei Varianten (Reihenfolge wie die früheren
Kenney-Modelle: Variante 0 = lange Halle mit Laderampe, Variante 1 = Verkaufsraum, Hof und Halle). Der Speicherhof
verkauft Weizen, Öl und Keramik.
  Variante 0: langer Speicherbau aus Kalkstein mit Ziegelbändern (Opus mixtum), Satteldach mit dem Giebel zur Straße,
              Strebepfeiler und Lüftungsschlitze an den Längsseiten, Rundfenster in beiden Giebeln, Bogentor (links zu,
              rechts offen) auf einer Laderampe mit Stufen und Säcken, Amphoren, Fässern; daneben ein Ochsenkarren.
  Variante 1: Verkaufsraum an der Straße (zwei Auslagen mit Markisen, Tür), Hof zwischen Hofmauern mit Ochsenkarren,
              Waage, Verkaufstisch, Sackstapeln, Fässern; hinten eine Lagerhalle, vorn offen unter dem Giebel: Säcke, Amphoren
              und Fässer sieht man im Inneren.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/speicherhof.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 7,36 m (Grundfläche 4 x 8 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Speicherhof_0, Speicherhof_1). Hofmauern und Hallenwände liegen mit der Außenfläche in den Ebenen x = +-XW und
enden 1,5 cm im Hauskörper.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(131)

XW = 1.64             # Außenfläche der Längswände (x = +-XW)
EMB = 0.015
WT = 0.12


def gate_leaf(hx, y, angle, w, z0, h):
    """Torflügel aus Bohlen mit Eisenbändern, am Punkt (hx, y) angeschlagen; angle = Richtung vom Scharnier (Grad um z)."""
    d = (math.cos(math.radians(angle)), math.sin(math.radians(angle)))
    n = (-d[1], d[0])
    cx, cy = hx + d[0] * w / 2, y + d[1] * w / 2
    box((w, 0.035, h), (cx, cy, z0 + h / 2), rot=(0, 0, angle), material=WOOD)
    for k in (-1, 0, 1):
        box((0.006, 0.004, h - 0.02), (cx + d[0] * k * w * 0.28 - n[0] * 0.019, cy + d[1] * k * w * 0.28 - n[1] * 0.019,
                                       z0 + h / 2), rot=(0, 0, angle), material=WOOD_DARK)
    for z in (0.16, h - 0.14):
        box((w - 0.02, 0.006, 0.03), (cx - n[0] * 0.02, cy - n[1] * 0.02, z0 + z), rot=(0, 0, angle), material=IRON)


def weighing_table(x, y, yaw=0.0):
    """Tisch mit Balkenwaage und Bleigewichten."""
    trestle_table(x, y, yaw, w=0.56, d=0.30, h=0.32, cloth=CLOTH)
    at = placed((x, y), yaw)
    p = at((0.0, 0.0, 0.0))
    scale_balance(p[0], p[1], 0.326)
    for k, u in enumerate((-0.22, -0.17, 0.19, 0.24)):
        q = at((u, -0.09, 0.0))
        cyl(0.014 + 0.003 * (k % 2), 0.018, (q[0], q[1], 0.335), material=IRON, verts=8)


# ---------------------------------------------------------------- Variante 0: lange Halle mit Laderampe

YF, YR = -2.50, 3.58
H = 1.45
PIT = 22
body(-XW, XW, YF, YR, H, LIME)
box((2 * XW + 0.04, YR - YF + 0.04, 0.20), (0.0, (YF + YR) / 2, 0.10), material=LIME_DARK)
gable_roof(-XW, XW, YF, YR, H, "y", LIME, overhang=0.04, pitch=PIT)
brick_bands(-XW, XW, YF, YR, (0.45, 0.80, 1.35), faces=("+y", "-x", "+x"))
for z in (0.45, 0.80, 1.35):
    for x0, x1 in ((-XW, -0.50), (0.50, XW)):
        box((x1 - x0, 0.012, 0.05), ((x0 + x1) / 2, YF - 0.006, z), material=TILE_DARK)
for sx in (-1, 1):                                                       # Gesims unter der Traufe
    box((0.06, YR - YF + 0.06, 0.05), (sx * (XW + 0.02), (YF + YR) / 2, H - 0.04), material=LIME_DARK)
for sx in (-1, 1):                                                       # Eckpilaster
    for y, o in ((YF, -1), (YR, 1)):
        box((0.14, 0.04, H - 0.06), (sx * (XW - 0.07), y + o * 0.02, (H - 0.06) / 2 + 0.03), material=LIME_DARK)
# Strebepfeiler und Lüftungsschlitze an den Längsseiten, Seitentür
for face, plane in (("-x", -XW), ("+x", XW)):
    for u in (-1.70, -0.45, 0.80, 2.05, 3.30):
        buttress(face, plane, u, 1.10)
    for u in (0.175, 1.425, 2.675):
        slit(face, plane, u, 0.80, h=0.28)

window("-x", -XW, -1.075, 0.30, w=0.14, h=0.20, shutters=None)
door("+x", XW, 0.175 - 1.25, w=0.34, h=0.64)
# Rückwand: Pilaster, Schlitze, Rundfenster
for x in (-0.80, 0.80):
    box((0.12, 0.04, 1.30), (x * 1.0 + (-0.0), YR + 0.02, 0.65 + 0.05), material=LIME_DARK)
    slit("+y", YR, x * 0.5, 0.80, h=0.28)
window("+y", YR, -1.15, 0.30, w=0.14, h=0.20, shutters=None)
window("+y", YR, 1.15, 0.30, w=0.14, h=0.20, shutters=None)
for y, out in ((YF, -1), (YR, 1)):
    cyl(0.13, 0.01, (0.0, y + out * 0.005, 1.78), rot=(90, 0, 0), material=TILE_DARK, verts=16)
    cyl(0.09, 0.012, (0.0, y + out * 0.008, 1.78), rot=(90, 0, 0), material=DARK, verts=16)
    for a in (0, 90):
        box((0.18, 0.01, 0.014), (0.0, y + out * 0.012, 1.78), rot=(0, a, 0), material=WOOD_DARK)
# Ladeluke mit Kranbalken links vom Tor
window("-y", YF, -1.05, 0.92, w=0.28, h=0.30, shutters=WOOD, open_shutters=True)
hoist(-1.05, YF, 1.36, reach=0.50, sack_z=0.62)
# Bogentor auf der Laderampe
PZ = 0.30
arch_opening("-y", YF, 0.0, 0.72, 0.60, z0=PZ)
box((0.80, 0.02, 0.15), (0.0, YF - 0.01, PZ + 0.60 + 0.36 + 0.14 + 0.06), material=LIME_DARK)             # Inschriftentafel
gate_leaf(-0.35, YF - 0.03, 0, 0.35, PZ, 0.58)
gate_leaf(0.35, YF - 0.03, 78, 0.35, PZ, 0.58)
# Laderampe: Plattform, Stufen, Waren
box((2.00, 0.75, PZ), (0.55, YF - 0.375 - 0.0, PZ / 2), material=LIME_DARK)
box((2.04, 0.79, 0.035), (0.55, YF - 0.375, PZ - 0.0175), material=LIME)
box((1.00, 0.16, 0.20), (0.50, YF - 0.75 - 0.08, 0.10), material=LIME_DARK)
box((1.04, 0.16, 0.10), (0.50, YF - 0.75 - 0.24, 0.05), material=LIME_DARK)
sack_stack(1.20, YF - 0.34, yaw=0, per=(3, 2, 1), z0=PZ)
sack_stack(0.78, YF - 0.34, yaw=0, per=(2, 1), z0=PZ)
barrel(1.40, YF - 0.62, PZ + 0.0, r=0.07)
crate(-0.15, YF - 0.60, PZ, 0.16, yaw=8)
# links daneben: Ochsenkarren und Amphoren
ox_cart(-0.98, YF - 0.62, yaw=-90, load="sacks")
amphora_row((-1.52, YF - 0.20), (-1.20, YF - 0.20), 3)
barrel(-1.45, YF - 1.05, r=0.07, lying=True, yaw=10)
check_bounds(1.84, 3.68, "0")
variant("Speicherhof_0")

# ---------------------------------------------------------------- Variante 1: Verkaufsraum, Hof und Halle

SY0, SY1 = -3.30, -2.10                  # Verkaufsraum
HY0, HY1 = 0.90, 3.58                    # Lagerhalle
HH = 1.25
# Verkaufsraum: ein Stockwerk, Ridge quer zur Straße, zwei Auslagen mit Markisen
body(-XW, XW, SY0, SY1, 1.0, PLASTER_OCHRE, socle=SOCLE_RED)
gable_roof(-XW, XW, SY0, SY1, 1.0, "x", PLASTER_OCHRE, overhang=0.04)
shop_front("-y", SY0, -0.92, w=0.80, goods=("basket", "jug", "basket"), awning=(CLOTH_OCHRE, CLOTH))
shop_front("-y", SY0, 0.92, w=0.80, goods=("basket", "bread", "jug"), awning=(CLOTH_RED, CLOTH))
door("-y", SY0, 0.0, w=0.40, h=0.66)
sign_board("-y", SY0, 0.0, 0.84, w=0.18, h=0.11, arm=0.15, board=WOOD, mark="disc", mark_mat=GRAIN)
for sx in (-1, 1):
    window("-x" if sx < 0 else "+x", sx * XW, -2.70, 0.42, shutters=PAINT_GREEN)
door("+y", SY1, -0.50)
window("+y", SY1, 0.50, 0.42, shutters=PAINT_GREEN)
window("+y", SY1, -1.20, 0.42, shutters=PAINT_GREEN)
window("+y", SY1, 1.30, 0.42, shutters=PAINT_GREEN)
# Hofmauern (Ebene x = +-XW), rechts mit Tor
flush_wall(-XW, -XW + WT, SY1 - EMB, HY0 + EMB, 0.45, PLASTER_OCHRE)
flush_wall(XW - WT, XW, SY1 - EMB, -1.62, 0.45, PLASTER_OCHRE)
flush_wall(XW - WT, XW, -1.06, HY0 + EMB, 0.45, PLASTER_OCHRE)
gate_leaf(XW - WT / 2, -1.62, 160, 0.28, 0.0, 0.40)
# Lagerhalle: Seitenwände und Rückwand aus Kalkstein, vorn offen unter dem Giebel
HYM = (HY0 + HY1) / 2
box((WT, HY1 - HY0, HH), (-XW + WT / 2, HYM, HH / 2), material=LIME)
box((WT, HY1 - HY0, HH), (XW - WT / 2, HYM, HH / 2), material=LIME)
box((2 * XW, WT, HH), (0.0, HY1 - WT / 2, HH / 2), material=LIME)
box((2 * XW + 0.04, HY1 - HY0 + 0.04, 0.09), (0.0, HYM, 0.045), material=STONE)
for sx in (-1, 1):
    box((0.04, HY1 - HY0 + 0.04, 0.20), (sx * (XW + 0.02), HYM, 0.10), material=LIME_DARK)
box((2 * XW + 0.04, 0.04, 0.20), (0.0, HY1 + 0.02, 0.10), material=LIME_DARK)
gable_roof(-XW, XW, HY0, HY1, HH, "y", LIME, overhang=0.04, pitch=22)
brick_bands(-XW, XW, HY0, HY1, (0.45, 0.90), faces=("+y", "-x", "+x"))
for sx in (-1, 1):
    box((0.06, HY1 - HY0 + 0.06, 0.05), (sx * (XW + 0.02), HYM, HH - 0.04), material=LIME_DARK)
for face, plane in (("-x", -XW), ("+x", XW)):
    for u in (1.35, 2.30, 3.25):
        buttress(face, plane, u, 1.00)
    for u in (1.82, 2.78):
        slit(face, plane, u, 0.70, h=0.28)
for u in (-1.0, 0.0, 1.0):
    slit("+y", HY1, u, 0.70, h=0.28)
cyl(0.12, 0.01, (0.0, HY1 + 0.005, 1.55), rot=(90, 0, 0), material=TILE_DARK, verts=16)
cyl(0.085, 0.012, (0.0, HY1 + 0.008, 1.55), rot=(90, 0, 0), material=DARK, verts=16)
# Hallenfront: Eckpfeiler, zwei Säulen, Architrav unter dem Giebeldreieck
for x in (-XW + WT / 2, XW - WT / 2):
    box((WT + 0.04, 0.16, HH), (x, HY0 + 0.08, HH / 2), material=LIME_DARK)
for x in (-0.55, 0.55):
    column(x, HY0 + 0.10, HH - 0.12, r=0.05)
box((2 * XW, 0.14, 0.12), (0.0, HY0 + 0.10, HH - 0.06), material=LIME)
# Inneres der Halle: Säcke, Amphoren, Fässer
sack_stack(-1.00, 3.28, yaw=0, per=(4, 3, 2))
sack_stack(-0.25, 3.28, yaw=0, per=(3, 2, 1))
sack_stack(0.60, 3.28, yaw=0, per=(3, 2))
amphora_row((-1.50, 1.85), (-1.50, 2.85), 4)
amphora_row((1.50, 1.85), (1.50, 2.85), 3)
for x, y in ((0.20, 1.55), (0.38, 1.60), (0.30, 1.75)):
    barrel(x, y, r=0.07)
barrel(1.05, 3.00, r=0.07)
barrel(1.15, 3.18, r=0.07)
crate(-0.60, 1.60, 0.0, 0.18, yaw=-5)
crate(-0.60, 1.60, 0.18, 0.14, yaw=8)
# Hof: Ochsenkarren, Waage, Verkaufstisch, Sackstapel, Fässer, Amphoren
ox_cart(0.30, -1.20, yaw=0, load="sacks")
weighing_table(-0.85, -1.35, yaw=0)
trestle_table(-0.75, -0.30, 0, w=0.90, d=0.30, h=0.28, cloth=CLOTH_OCHRE)
for i in range(3):
    basket_goods(-1.00 + i * 0.25, -0.30, 0.286, 0.060, "grain", seed=30 + i)
sack_stack(0.85, 0.10, yaw=0, per=(3, 2))
for x, y in ((1.35, -0.10), (1.40, 0.18), (1.20, 0.30)):
    barrel(x, y, r=0.07)
amphora_row((-1.50, -1.55), (-1.50, -0.60), 3)
crate(1.35, -0.45, 0.0, 0.18, yaw=-8)
pot_plant(-1.40, 0.62, 0.05)
check_bounds(1.84, 3.68, "1")
variant("Speicherhof_1")

export(spacing=4.4)
