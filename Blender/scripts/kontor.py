"""
Kontor der Gewerbezone (Antike), 2 x 3 Felder (4 m Front, 6 m tief), zwei Varianten (Reihenfolge wie die früheren
Kenney-Modelle: Variante 0 = Halle, Variante 1 = Hof):
  Variante 0: Handelskontor als zweistöckiger Steinbau mit dem Giebel zur Straße; davor eine Säulenhalle mit
              Pultdach, darunter Schreibpult mit Wachstafeln und Schriftrollen, Balkenwaage auf dem Tisch, Truhe,
              Säcke, Amphoren und ein Banner; Tür mit Gitterfenstern, Fensterreihe oben.
  Variante 1: Kontor mit Hof: Verwaltungshaus vorn links (zweistöckig, Balkon, Schild), daneben das Hoftor; im Hof
              Schreibtisch mit Waage, Ochsenkarren mit Säcken, Amphoren, Ballen und Kisten; hinten ein Lagerschuppen
              mit Bohlentor.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/kontor.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 5,52 m (Grundfläche 4 x 6 m), Vorderseite (Schmalseite) nach -Y. Die FBX enthält je
Variante ein Objekt (Kontor_0, Kontor_1).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(85)


def strongbox(x, y, yaw=0.0, w=0.20, d=0.13, h=0.13):
    """Truhe mit gewölbtem Deckel und Eisenbändern."""
    at = placed((x, y), yaw)
    box((w, d, h * 0.62), (x, y, h * 0.31), rot=(0, 0, yaw), material=WOOD_DARK)
    cyl(d * 0.5, w, (x, y, h * 0.62), rot=(0, 90, yaw), material=WOOD, verts=8)
    for u in (-w * 0.32, w * 0.32):
        p = at((u, 0.0, 0.0))
        box((0.016, d + 0.008, h * 0.62), (p[0], p[1], h * 0.31), rot=(0, 0, yaw), material=IRON)
        cyl(d * 0.5 + 0.004, 0.016, (p[0], p[1], h * 0.62), rot=(0, 90, yaw), material=IRON, verts=8)
    p = at((0.0, -d / 2 - 0.004, 0.0))
    box((0.025, 0.010, 0.035), (p[0], p[1], h * 0.58), rot=(0, 0, yaw), material=BRONZE)


def writing_desk(x, y, yaw=0.0):
    """Schreibpult des Verwalters: Tisch mit Wachstafeln, Schriftrollen, Tintenfass und Griffel."""
    trestle_table(x, y, yaw, w=0.70, d=0.30, h=0.32)
    at = placed((x, y), yaw)
    for k, u in enumerate((-0.22, -0.04)):
        p = at((u, 0.0, 0.0))
        box((0.10, 0.07, 0.010), (p[0], p[1], 0.325), rot=(0, 0, yaw + (k * 8 - 4)), material=WOOD_DARK)
        box((0.082, 0.056, 0.004), (p[0], p[1], 0.332), rot=(0, 0, yaw + (k * 8 - 4)), material=WAX)
    for k, v in enumerate((-0.05, 0.03, 0.10)):
        p = at((0.15, v, 0.0))
        scroll(p[0], p[1], 0.335, yaw=yaw + 90 + k * 5, length=0.12, r=0.012, tag=(k == 1))
    p = at((0.27, -0.06, 0.0))
    cyl(0.016, 0.03, (p[0], p[1], 0.335), material=INK, verts=8)


def weighing_table(x, y, yaw=0.0):
    """Tisch mit Balkenwaage und Gewichten aus Blei."""
    trestle_table(x, y, yaw, w=0.56, d=0.30, h=0.32, cloth=CLOTH)
    at = placed((x, y), yaw)
    p = at((0.0, 0.0, 0.0))
    scale_balance(p[0], p[1], 0.326)
    for k, u in enumerate((-0.22, -0.17, 0.19, 0.24)):
        q = at((u, -0.09, 0.0))
        cyl(0.014 + 0.003 * (k % 2), 0.018, (q[0], q[1], 0.335), material=IRON, verts=8)


# ---------------------------------------------------------------- Variante 0: Handelskontor mit Säulenhalle

X0, X1, Y0, Y1, H = -1.70, 1.70, -0.60, 2.70, 2 * STOREY
body(X0, X1, Y0, Y1, H, LIME, socle=SOCLE_RED)
brick_bands(X0, X1, Y0, Y1, (0.22, 0.84, 1.20, 1.88), faces=("-x", "+x", "+y"))
gable_roof(X0, X1, Y0, Y1, H, "y", LIME, overhang=0.08)
cornice(X0, X1, Y0, Y1, STOREY, material=LIME_DARK)
# Fassade zur Halle: Tür mit Gitterfenstern, Fensterreihe oben
door("-y", Y0, 0.0, w=0.44, h=0.70, open_angle=0)
for u in (-1.05, 1.05):
    window("-y", Y0, u, 0.38, w=0.26, h=0.32, shutters=None)
for u in (-1.20, -0.60, 0.0, 0.60, 1.20):
    window("-y", Y0, u, STOREY + 0.40, shutters=PAINT_GREEN, open_shutters=(abs(u) > 0.5))
window("-y", Y0, 0.0, H + 0.14, w=0.14, h=0.12, shutters=None)
for face, plane in (("-x", X0), ("+x", X1)):
    for u in (-0.80, -0.05, 0.70, 1.40):
        window(face, plane, u + 1.0, 0.42, shutters=PAINT_GREEN)
        window(face, plane, u + 1.0, STOREY + 0.40, shutters=PAINT_GREEN, open_shutters=(u > 0.5))
for u in (-1.10, 0.0, 1.10):
    window("+y", Y1, u, 0.42, shutters=PAINT_GREEN)
    window("+y", Y1, u, STOREY + 0.40, shutters=PAINT_GREEN, open_shutters=False)
door("+y", Y1, 0.55)
# Säulenhalle davor: Podium, vier Säulen, Gebälk, Pultdach
PY0, PY1 = -1.74, Y0 - 0.02
box((3.30, PY1 - PY0, 0.06), (0.0, (PY0 + PY1) / 2, 0.03), material=LIME)                       # Podium
CH = 0.90
for x in (-1.50, -0.50, 0.50, 1.50):
    column(x, PY0 + 0.16, CH)
box((3.50, 0.14, 0.12), (0.0, PY0 + 0.16, CH + 0.06), material=LIME)                              # Architrav
shed_roof(-1.62, 1.62, PY0 - 0.02, PY1 - 0.02, CH + 0.50, "-y", overhang=0.08, pitch=16, eave_overhang=0.04)
slab([(-1.62, CH + 0.12), (1.62, CH + 0.12), (1.62, CH + 0.24), (-1.62, CH + 0.24)], "xz", PY0 + 0.10, 0.12, LIME_DARK)
# Banner zwischen den mittleren Säulen
box((0.36, 0.012, 0.34), (0.0, PY0 + 0.24, CH - 0.20), material=CLOTH_RED)
box((0.40, 0.014, 0.016), (0.0, PY0 + 0.24, CH - 0.02), material=WOOD_DARK)
box((0.14, 0.004, 0.14), (0.0, PY0 + 0.233, CH - 0.20), material=CLOTH_OCHRE)
# unter der Halle
writing_desk(-0.92, -1.16)
stool(-0.92, -0.92)
weighing_table(0.92, -1.16)
strongbox(0.50, -0.78)
sack(1.40, -0.84, 0.0, 0.075)
sack(1.52, -1.00, 0.0, 0.07)
sack(1.30, -1.00, 0.0, 0.065)
amphora(-1.48, -0.84)
amphora(-1.32, -0.76)
cloth_bale(0.20, -1.40, 0.0, yaw=10)
# vor der Halle
basket_goods(-1.20, -1.74 - 0.0, 0.0, 0.07, "grain", seed=7) if False else None
amphora(1.55, -1.60)
crate(1.50, -1.28, 0.0, 0.16, yaw=-6)
variant("Kontor_0")

# ---------------------------------------------------------------- Variante 1: Kontor mit Hof

HX0, HX1, HY0, HY1, HH = -1.70, 0.10, -1.65, -0.35, 2 * STOREY
body(HX0, HX1, HY0, HY1, HH, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(HX0, HX1, HY0, HY1, HH, "y", PLASTER_WHITE, overhang=0.07)
cornice(HX0, HX1, HY0, HY1, STOREY)
shop_front("-y", HY0, -1.18, w=0.62, goods=("jug", "bread", "jug"), awning=(PAINT_BLUE, CLOTH))
door("-y", HY0, -0.46, w=0.36, h=0.66)
sign_board("-y", HY0, -0.46, 0.82, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="bar", mark_mat=IRON)
balcony(HY0, -1.45, -0.80, STOREY + 0.06, depth=0.20)
door("-y", HY0, -1.12, w=0.28, h=0.56, z0=STOREY + 0.06)
window("-y", HY0, -0.46, STOREY + 0.38, shutters=PAINT_GREEN)
window("-y", HY0, -0.80, HH + 0.08, w=0.12, h=0.10, shutters=None)
window("-x", HX0, -1.20, 0.42, shutters=PAINT_GREEN)
window("-x", HX0, -0.80, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=False)
window("-x", HX0, -1.35, STOREY + 0.38, shutters=PAINT_GREEN)
window("+x", HX1, -1.20, 0.42, shutters=PAINT_GREEN)
window("+x", HX1, -0.80, STOREY + 0.38, shutters=PAINT_GREEN)
door("+x", HX1, -0.60)
# Hoftor: zwei Pfeiler, Balken, offene Torflügel
for x in (0.38, 1.62):
    box((0.16, 0.16, 0.90), (x, -1.57, 0.45), material=LIME_DARK)
    box((0.22, 0.22, 0.05), (x, -1.57, 0.925), material=LIME)
box((1.46, 0.14, 0.07), (1.00, -1.57, 0.965), material=WOOD_DARK)
box((0.50, 0.012, 0.16), (1.00, -1.645, 1.07), material=WOOD)
box((0.20, 0.004, 0.06), (1.00, -1.653, 1.07), material=CLOTH_RED)
for sx, ang in ((0.46, 65), (1.54, -65)):
    c = (sx + (0.18 if sx < 1 else -0.18) * 0.3, -1.60 + 0.23, 0.38)
    box((0.04, 0.50, 0.70), (sx + 0.04 if sx < 1 else sx - 0.04, -1.38, 0.38), rot=(0, 0, 0), material=WOOD)
# Hofmauern
court_wall([(0.10, HY1 + 0.02), (0.10, 0.20)], h=0.38, t=0.08, plaster=PLASTER_WHITE) if False else None
court_wall([(1.66, -1.57), (1.66, 1.28)], h=0.40, t=0.08, plaster=PLASTER_WHITE)
court_wall([(-1.66, HY1 + 0.02), (-1.66, 1.28)], h=0.40, t=0.08, plaster=PLASTER_WHITE)
court_wall([(0.12, -1.61), (0.34, -1.61)], h=0.40, t=0.08, plaster=PLASTER_WHITE)
# Lagerschuppen hinten
LX0, LX1, LY0, LY1, LH = -1.70, 1.70, 1.30, 2.70, STOREY
body(LX0, LX1, LY0, LY1, LH, LIME, socle=None)
brick_bands(LX0, LX1, LY0, LY1, (0.30, 0.55, 0.80), faces=("-y", "-x", "+x", "+y"))
gable_roof(LX0, LX1, LY0, LY1, LH, "x", LIME, overhang=0.08)
door("-y", LY0, -0.55, w=0.62, h=0.64, open_angle=70)
door("-y", LY0, 0.70, w=0.44, h=0.60)
for u in (-1.35, -0.10, 0.20, 1.35):
    slit("-y", LY0, u, 0.50, h=0.28) if abs(u) > 1.0 else None
for u in (1.50, 2.30):
    slit("-x", LX0, u, 0.50, h=0.28)
    slit("+x", LX1, u, 0.50, h=0.28)
# Hof: Schreibtisch, Waage, Ochsenkarren, Ballen, Amphoren, Kisten
writing_desk(-0.95, -0.12, yaw=0)
stool(-0.95, 0.12)
weighing_table(-0.20, -0.04, yaw=0)
strongbox(0.35, 0.00)
ox_cart(0.95, 0.28, yaw=0, load="sacks")
cloth_bale(-1.45, 0.62, 0.0, yaw=0)
cloth_bale(-1.45, 0.62, 0.075, yaw=12, colors=(CLOTH_OCHRE, CLOTH))
for k, (ax, ay) in enumerate(((-0.30, 0.80), (-0.12, 0.84), (-0.22, 0.98), (-0.55, 0.96))):
    amphora(ax, ay)
crate(1.00, 1.08, 0.0, 0.18, yaw=5)
crate(1.00, 1.08, 0.18, 0.15, yaw=-12)
crate(1.35, 1.12, 0.0, 0.16, yaw=-4)
barrel(0.45, 1.05, r=0.07)
sack(-1.25, 1.10, 0.0, 0.075)
sack(-1.10, 1.14, 0.0, 0.07)
sack(-1.18, 1.00, 0.075, 0.06)
path([(0.60, -1.40), (0.60, -0.55)])
variant("Kontor_1")

export(spacing=4.4)
