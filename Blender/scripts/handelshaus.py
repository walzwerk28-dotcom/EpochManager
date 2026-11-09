"""
Handelshaus der Gewerbezone (Antike), 4 x 4 Felder (8 x 8 m), zwei Varianten (Reihenfolge wie die früheren Kenney-Modelle:
Variante 0 = Markthalle mit Ständen davor, Variante 1 = Kaufmannshäuser um einen Hof). Das Handelshaus verkauft Weizen,
Öl, Keramik und Wein; die Stände und Waren zeigen das.
  Variante 0: große Markthalle aus Kalkstein mit Ziegelbändern, Satteldach über einer Säulenhalle (acht Säulen mit Gebälk
              und Pultdach), sieben Rundbogenöffnungen zur Halle, Rundfenster in den Giebeln; davor ein gepflasterter
              Markt mit einem Brunnen in der Mitte und zwei Reihen gestreifter Verkaufsstände (Getreide, Öl, Keramik, Wein),
              deren Ware vor dem schmalen Tuchdach auf offenen Tischen liegt.
  Variante 1: zweistöckige Kaufmannshäuser (links ocker, rechts rosa, hinten weiß) und ein niedriger Torbau zur Straße
              mit Rundbogentor, Läden mit Markisen nach außen (vorn und an den Seiten); im gepflasterten Hof ein Brunnen,
              ein Säulengang mit Pultdach vor dem Hinterhaus, Verkaufstische, Waage, Sackstapel, Fässer, Amphoren, Karren.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/handelshaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 7,36 x 7,36 m (Grundfläche 8 x 8 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Handelshaus_0, Handelshaus_1).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(141)


def weighing_table(x, y, yaw=0.0):
    """Tisch mit Balkenwaage und Bleigewichten."""
    trestle_table(x, y, yaw, w=0.56, d=0.30, h=0.32, cloth=CLOTH)
    at = placed((x, y), yaw)
    p = at((0.0, 0.0, 0.0))
    scale_balance(p[0], p[1], 0.326)
    for k, u in enumerate((-0.22, -0.17, 0.19, 0.24)):
        q = at((u, -0.09, 0.0))
        cyl(0.014 + 0.003 * (k % 2), 0.018, (q[0], q[1], 0.335), material=IRON, verts=8)


# ---------------------------------------------------------------- Variante 0: Markthalle mit Ständen

HW = 3.40                                  # Außenfläche der Hallenseiten (x = +-HW)
HY0, HY1 = 0.30, 3.50
H0 = 1.70
body(-HW, HW, HY0, HY1, H0, LIME)
box((2 * HW + 0.04, HY1 - HY0 + 0.04, 0.20), (0.0, (HY0 + HY1) / 2, 0.10), material=LIME_DARK)
gable_roof(-HW, HW, HY0, HY1, H0, "x", LIME, overhang=0.04, pitch=22)
brick_bands(-HW, HW, HY0, HY1, (0.40, 0.80, 1.20, 1.55), faces=("+y", "-x", "+x"))
box((2 * HW, 0.012, 0.05), (0.0, HY0 - 0.006, 1.60), material=TILE_DARK)                  # Fries über den Bögen
for sx in (-1, 1):                                                                          # Gesims unter der Traufe
    box((0.06, HY1 - HY0 + 0.06, 0.05), (sx * (HW + 0.02), (HY0 + HY1) / 2, H0 - 0.04), material=LIME_DARK)
for u in (-2.70, -1.80, -0.90, 0.0, 0.90, 1.80, 2.70):                                      # sieben Bögen zur Halle
    arch_opening("-y", HY0, u, 0.52, 0.52, z0=0.09)
for u in (-3.15, -2.25, -1.35, -0.45, 0.45, 1.35, 2.25, 3.15):                              # Pilaster zwischen den Bögen
    box((0.10, 0.04, 1.50), (u, HY0 - 0.02, 0.75 + 0.09), material=LIME_DARK)
# Seiten und Rückseite
for face, plane in (("-x", -HW), ("+x", HW)):
    for u in (0.75, 1.55, 2.35):
        window(face, plane, u, 0.50, shutters=PAINT_GREEN, open_shutters=(u > 1.0))
    door(face, plane, 3.05, w=0.34, h=0.66)
    cyl(0.14, 0.01, (plane + (-1 if plane < 0 else 1) * 0.005, 1.90, H0 + 0.32), rot=(0, 90, 0), material=TILE_DARK, verts=16)
    cyl(0.10, 0.012, (plane + (-1 if plane < 0 else 1) * 0.008, 1.90, H0 + 0.32), rot=(0, 90, 0), material=DARK, verts=16)
for u in (-2.80, -1.70, -0.60, 0.60, 1.70, 2.80):
    window("+y", HY1, u, 0.52, w=0.16, h=0.24, shutters=None)
    window("+y", HY1, u, 1.18, w=0.16, h=0.24, shutters=None)
# Säulenhalle davor
PC = HY0 - 0.50
CH = 1.25
for k in range(8):
    column(-3.15 + k * 0.90, PC, CH)
box((6.60, 0.12, 0.12), (0.0, PC, CH + 0.06), material=LIME)
shed_roof(-3.32, 3.32, PC, HY0, 1.525, "-y", overhang=0.04, pitch=16, eave_overhang=0.04)
# Markt: Brunnen und zwei Reihen Stände
fountain_basin(0.0, -2.05, r=0.36, h=0.20)
colors = ((CLOTH_RED, CLOTH), (CLOTH_BLUE, CLOTH), (CLOTH_GREEN, CLOTH_OCHRE), (CLOTH_OCHRE, CLOTH_RED))
for i, (x, kind) in enumerate(((-2.60, "grain"), (-1.25, "oil"), (1.25, "pots"), (2.60, "wine"))):
    market_stall(x, -1.15, 1.20, kind, colors[i])
for i, (x, kind) in enumerate(((-2.60, "pots"), (-1.25, "wine"), (1.25, "grain"), (2.60, "oil"))):
    market_stall(x, -3.05, 1.20, kind, colors[(i + 2) % 4])
sack_stack(0.0, -0.95, yaw=0, per=(3, 2))
amphora_row((-0.55, -3.05), (0.55, -3.05), 3)
barrel(-0.42, -0.65, r=0.07)
crate(0.42, -0.70, 0.0, 0.18, yaw=7)
check_bounds(3.68, 3.68, "0")
variant("Handelshaus_0")

# ---------------------------------------------------------------- Variante 1: Kaufmannshäuser um einen Hof

XP = 3.30                                  # Außenflächen links, rechts und vorn (Platz für Markisen davor)
FY = -3.30
BY1 = 3.50
IN = 1.80                                  # Innenkante der Seitenflügel
FB = -1.80                                 # Rückseite des Torbaus
BY0 = 2.00                                 # Vorderseite des Hinterhauses
H2 = 2 * STOREY - 0.05
# Torbau, einstöckig, Dach quer zur Straße
body(-XP, XP, FY, FB, 1.0, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(-XP, XP, FY, FB, 1.0, "x", PLASTER_WHITE, overhang=0.04)
# Seitenflügel zweistöckig, Dach längs; Hinterhaus zweistöckig, Dach quer
body(-XP, -IN, FB, BY0, H2, PLASTER_OCHRE, socle=SOCLE_RED)
body(IN, XP, FB, BY0, H2, PLASTER_PINK, socle=SOCLE_RED)
body(-XP, XP, BY0, BY1, H2, PLASTER_WHITE, socle=SOCLE_RED)
mid_f, mid_b = (FY + FB) / 2 + 0.75, (BY0 + BY1) / 2
gable_roof(-XP, -IN, FB, mid_b, H2, "y", PLASTER_OCHRE, overhang=0.04)
gable_roof(IN, XP, FB, mid_b, H2, "y", PLASTER_PINK, overhang=0.04)
gable_roof(-XP, XP, BY0, BY1, H2, "x", PLASTER_WHITE, overhang=0.04)
for (a0, a1, b0, b1) in ((-XP, -IN, FB, BY0), (IN, XP, FB, BY0), (-XP, XP, BY0, BY1)):
    cornice(a0, a1, b0, b1, STOREY - 0.03)
# Torbau zur Straße: Rundbogentor mit offenen Flügeln, Läden und Türen
arch_opening("-y", FY, 0.0, 0.78, 0.58, z0=0.09)
box((0.50, 0.012, 0.10), (0.0, FY - 0.004, 0.09 + 0.58 + 0.39 + 0.16), material=WOOD)
for u, awn in ((-2.55, (CLOTH_RED, CLOTH)), (-0.95, (PAINT_BLUE, CLOTH)), (0.95, (CLOTH_OCHRE, CLOTH_RED)),
               (2.55, (CLOTH_GREEN, CLOTH))):
    shop_front("-y", FY, u, w=0.64, goods=(("jug", "basket", "jug"), ("basket", "bread", "jug"), ("jug", "jug", "basket"),
                                          ("basket", "jug", "bread"))[int(abs(u) > 2) * 2 + (u > 0)], awning=awn)
door("-y", FY, -1.72, w=0.36, h=0.66)
door("-y", FY, 1.72, w=0.36, h=0.66)
# Seitenflügel: Läden nach außen, Fenster oben
for face, plane, sgn in (("-x", -XP, -1), ("+x", XP, 1)):
    shop_front(face, plane, -0.85, w=0.64, goods=("jug", "amphora", "jug") if sgn < 0 else ("basket", "bread", "basket"),
               awning=(CLOTH_RED, CLOTH) if sgn < 0 else (PAINT_BLUE, CLOTH))
    door(face, plane, -0.05, w=0.36, h=0.66)
    shop_front(face, plane, 0.95, w=0.64, goods=("basket", "jug", "basket") if sgn < 0 else ("jug", "basket", "jug"),
               awning=(CLOTH_OCHRE, CLOTH) if sgn < 0 else (CLOTH_GREEN, CLOTH))
    for u in (-1.15, -0.30, 0.55, 1.35):
        window(face, plane, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=((int(u * 10) // 8) % 2 == 0))
    window(face, plane, 1.65, 0.42, shutters=PAINT_GREEN)
    window(face, plane, 1.65, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=False)
# obere Fenster über dem Torbau (Seitenflügel-Giebelwände) und Hinterhaus
for u in (-2.55, 2.55):
    window("-y", FB, u, 1.52, w=0.18, h=0.24, shutters=PAINT_GREEN, open_shutters=False)
# Hinterhaus: Außenseiten
for u in (-2.40, -1.20, 1.20, 2.40):
    window("+y", BY1, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u > 0))
    window("+y", BY1, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=(u < 0.5))
door("+y", BY1, 0.0 + 0.6)
for face, plane in (("-x", -XP), ("+x", XP)):
    window(face, plane, 2.75, 0.42, shutters=PAINT_GREEN)
    window(face, plane, 2.75, STOREY + 0.38, shutters=PAINT_GREEN)
# Hofseiten
for u in (-0.60, 0.50):
    window("+x", -IN, u, 0.42, shutters=PAINT_GREEN)
    window("-x", IN, u, 0.42, shutters=PAINT_GREEN, open_shutters=False)
for u in (-1.00, 0.20, 1.40):
    window("+x", -IN, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=(u > 0))
    window("-x", IN, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=(u < 0))
door("+x", -IN, 1.35 - 0.0, w=0.34, h=0.62)
door("-x", IN, -1.25, w=0.34, h=0.62)
door("+y", FB, -1.20)
door("+y", FB, 1.20)
for u in (-0.4, 0.4):
    window("+y", FB, u, 0.42, shutters=PAINT_GREEN)
# Säulengang vor dem Hinterhaus
CC = BY0 - 0.48
for k in range(5):
    column(-1.50 + k * 0.75, CC, 1.0, r=0.04)
box((3.30, 0.11, 0.10), (0.0, CC, 1.05), material=LIME)
shed_roof(-1.62, 1.62, CC, BY0, 1.20, "-y", overhang=0.04, pitch=16, eave_overhang=0.04)
for u in (-1.10, 0.0, 1.10):
    arch_opening("-y", BY0, u, 0.46, 0.46, z0=0.09)
for u in (-1.10, 0.0, 1.10):
    window("-y", BY0, u, STOREY + 0.48, shutters=PAINT_GREEN, open_shutters=(u != 0.0))
# Hof
fountain_basin(0.0, 0.10, r=0.50, h=0.22)
trestle_table(-1.05, -1.20, 0, w=0.90, d=0.30, h=0.28, cloth=CLOTH_OCHRE)
for i, kind in enumerate(("pot", "bowl", "pot", "bowl")):
    px = -1.35 + i * 0.20
    if kind == "pot":
        pot(px, -1.20, 0.286, 0.040)
    else:
        bowl(px, -1.20, 0.286, 0.050, TERRACOTTA_LIGHT)
trestle_table(1.00, -1.20, 0, w=0.90, d=0.30, h=0.28, cloth=CLOTH)
for i in range(3):
    basket_goods(0.75 + i * 0.25, -1.20, 0.286, 0.060, "grain", seed=40 + i)
weighing_table(-1.15, 1.10, yaw=0)
sack_stack(1.20, 1.05, yaw=0, per=(3, 2, 1))
amphora_row((-1.62, -0.55), (-1.62, 0.35), 3)
amphora_row((1.62, -0.45), (1.62, 0.25), 2)
for x, y in ((1.40, 0.45), (1.25, 0.60)):
    barrel(x, y, r=0.07)
crate(0.60, 0.95, 0.0, 0.18, yaw=6)
cart(-0.35, 0.95, yaw=20, load="sacks")
pot_plant(-1.55, 1.50, 0.05)
check_bounds(3.68, 3.68, "1")
variant("Handelshaus_1")

export(spacing=8.4)
