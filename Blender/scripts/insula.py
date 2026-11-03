"""
Insula der Bauern (Antike), 4 x 4 Felder (8 x 8 m), zwei Varianten:
  Variante 0: dreistöckiger Mietblock (ocker, rote Sockelzone) um einen Innenhof: unten zur Straße vier Läden mit
              Theken und Markisen, in der Mitte die Hofeinfahrt; oben Fensterreihen mit Läden, ein Holzbalkon über
              die Straßenseite, Gesimse zwischen den Stockwerken, Satteldächer je Flügel; im Hof Zisterne, Holztreppe
              zum ersten Stock, Wäscheleine, Brennholz, Kisten und Amphoren.
  Variante 1: Zeile aus vier schmalen Häusern (zwei- und dreistöckig, weiß, ocker, rosa) mit dem Giebel zur Straße,
              unten je ein Laden mit Markise, dahinter getrennte Höfe (Wäsche und Brennholz; Feigenbaum und Bank;
              Beet und Hühner; Schuppen mit Karren und Backofen).

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/insula.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 7,36 x 7,36 m (Grundfläche 8 x 8 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(91)

# ---------------------------------------------------------------- Variante 0: Mietblock um einen Innenhof

X0, X1, Y0, Y1 = -3.40, 3.40, -3.20, 3.40
D = 1.60                                     # Tiefe der Flügel
CX0, CX1, CY0, CY1 = X0 + D, X1 - D, Y0 + D, Y1 - D
H = 3 * STOREY
plaster = PLASTER_OCHRE
body(X0, X1, Y0, CY0, H, plaster, socle=SOCLE_RED)             # vorn
body(X0, X1, CY1, Y1, H, plaster, socle=SOCLE_RED)             # hinten
body(X0, CX0, CY0, CY1, H, plaster, socle=SOCLE_RED)           # links
body(CX1, X1, CY0, CY1, H, plaster, socle=SOCLE_RED)           # rechts
gable_roof(X0, X1, Y0, CY0, H, "x", plaster, overhang=0.08)
gable_roof(X0, X1, CY1, Y1, H, "x", plaster, overhang=0.08)
# Seitenflügel: Dach bis zu den Firsten von vorn und hinten, damit die Dächer ineinandergreifen
gable_roof(X0, CX0, (Y0 + CY0) / 2, (CY1 + Y1) / 2, H, "y", plaster, overhang=0.0)
gable_roof(CX1, X1, (Y0 + CY0) / 2, (CY1 + Y1) / 2, H, "y", plaster, overhang=0.0)
for z in (STOREY, 2 * STOREY):
    for (a0, a1, b0, b1) in ((X0, X1, Y0, CY0), (X0, X1, CY1, Y1), (X0, CX0, CY0, CY1), (CX1, X1, CY0, CY1)):
        cornice(a0, a1, b0, b1, z)

# Straßenseite: vier Läden, Hofeinfahrt, Balkon
awnings = ((CLOTH_RED, CLOTH), (CLOTH_OCHRE, CLOTH), (PAINT_BLUE, CLOTH), (CLOTH_RED, CLOTH_OCHRE))
goods = (("jug", "amphora", "jug"), ("bread", "basket", "bread"), ("basket", "basket", "jug"), ("amphora", "jug", "basket"))
for k, u in enumerate((-2.55, -1.30, 1.30, 2.55)):
    shop_front("-y", Y0, u, w=0.62, goods=goods[k], awning=awnings[k])
door("-y", Y0, 0.0, w=0.56, h=0.78, leaf=WOOD_DARK)
box((0.008, 0.008, 0.74), (0.0, Y0 - 0.026, 0.48), material=WOOD_DARK)
balcony(Y0, -2.30, 2.30, STOREY + 0.06, depth=0.22)
for u in (-2.90, -1.95, -0.95, 0.0, 0.95, 1.95, 2.90):
    window("-y", Y0, u, STOREY + 0.38, shutters=WOOD, open_shutters=(int(u * 10) % 3 != 0))
    window("-y", Y0, u, 2 * STOREY + 0.38, shutters=WOOD, open_shutters=(int(u * 10) % 2 == 0))
# Seiten und Rückseite
for face, plane in (("-x", X0), ("+x", X1)):
    shop_front(face, plane, -2.45, w=0.56, goods=("jug", "basket", "jug"))
    for u in (-1.30, 0.0, 1.30, 2.60):
        window(face, plane, u, 0.42, shutters=WOOD, open_shutters=(u > 0))
    for u in (-2.60, -1.30, 0.0, 1.30, 2.60):
        window(face, plane, u, STOREY + 0.38, shutters=WOOD)
        window(face, plane, u, 2 * STOREY + 0.38, shutters=WOOD, open_shutters=(u < 1.0))
for u in (-2.6, -1.3, 0.0, 1.3, 2.6):
    window("+y", Y1, u, 0.42, shutters=WOOD, open_shutters=False)
    window("+y", Y1, u, STOREY + 0.38, shutters=WOOD)
    window("+y", Y1, u, 2 * STOREY + 0.38, shutters=WOOD, open_shutters=(u > 0))
# Innenhof
for face, plane, us in (("+y", CY0, (-1.1, 1.1)), ("-y", CY1, (-1.1, 0.0, 1.1)), ("+x", CX0, (0.9,)), ("-x", CX1, (-0.6, 0.9))):
    for u in us:
        window(face, plane, u, STOREY + 0.38, shutters=WOOD, open_shutters=False)
        window(face, plane, u, 2 * STOREY + 0.38, shutters=WOOD)
door("+y", CY0, 0.0, w=0.56, h=0.78, leaf=WOOD_DARK)
door("-y", CY1, -0.55)
door("-y", CY1, 0.65)
door("-x", CX1, -0.40)
stairs(CX0 + 0.18, CY0 + 0.25, 90, STOREY, width=0.26)
box((0.30, 0.40, 0.04), (CX0 + 0.17, CY0 + 0.25 + 9 * 0.11 + 0.18, STOREY), material=WOOD)   # Absatz
door("+x", CX0, CY0 + 0.25 + 9 * 0.11 + 0.18, w=0.28, h=0.56, z0=STOREY + 0.02)
cistern(0.55, 0.25)
washing_line(-1.20, 1.45, 1.40, 1.45, h=0.80)
woodpile(1.40, -1.10, length=0.40, rows=(4, 3, 2), yaw=90)
crate(1.35, 0.95, size=0.18, yaw=10)
crate(1.35, 0.95, z0=0.18, size=0.14, yaw=-8)
for x, y in ((-0.40, -1.40), (-0.27, -1.43), (-0.34, -1.30)):
    amphora(x, y)
pot_plant(-0.95, 0.55, 0.06, OLIVE_LEAF)
pot_plant(0.95, 1.65, 0.05)
variant("Insula_0")

# ---------------------------------------------------------------- Variante 1: Zeile aus vier schmalen Häusern

Y0, YB = -3.20, -0.40
houses = (
    (-3.45, -1.80, 3, PLASTER_WHITE, PAINT_GREEN, (CLOTH_RED, CLOTH)),
    (-1.75, -0.05, 2, PLASTER_OCHRE, PAINT_BLUE, (CLOTH_OCHRE, CLOTH)),
    (0.00, 1.70, 3, PLASTER_PINK, WOOD, (PAINT_BLUE, CLOTH)),
    (1.75, 3.45, 2, PLASTER_OCHRE, PAINT_GREEN, (CLOTH_RED, CLOTH_OCHRE)),
)
for i, (x0, x1, storeys, plaster, shutter, awning) in enumerate(houses):
    cx = (x0 + x1) / 2
    h = storeys * STOREY
    body(x0, x1, Y0, YB, h, plaster, socle=SOCLE_RED if i % 2 == 0 else None)
    gable_roof(x0, x1, Y0, YB, h, "y", plaster, overhang=0.06)
    for z in range(1, storeys):
        cornice(x0, x1, Y0, YB, z * STOREY)
    shop_front("-y", Y0, cx - 0.30, w=0.56, goods=(("jug", "amphora", "jug"), ("bread", "basket", "bread"),
                                                   ("basket", "jug", "basket"), ("amphora", "jug", "jug"))[i], awning=awning)
    door("-y", Y0, cx + 0.50)
    for level in range(1, storeys):
        z = level * STOREY + 0.38
        for u in (cx - 0.40, cx + 0.40):
            window("-y", Y0, u, z, shutters=shutter, open_shutters=((level + i) % 2 == 0 or u > cx))
        door("+y", YB, cx, w=0.28, h=0.56, z0=level * STOREY + 0.06) if False else None
        window("+y", YB, cx, z, shutters=shutter, open_shutters=False)
    window("-y", Y0, cx, h + 0.08, w=0.12, h=0.10, shutters=None)                       # Dachbodenluke
    door("+y", YB, cx - 0.35)
    window("+y", YB, cx + 0.40, 0.42, shutters=shutter)
    if i == 2:
        balcony(Y0, x0 + 0.20, x1 - 0.20, STOREY + 0.06, depth=0.18)
# Fenster an den Giebelwänden der Endhäuser
for face, plane in (("-x", -3.45), ("+x", 3.45)):
    for u in (-2.40, -1.20):
        window(face, plane, u, 0.42, shutters=WOOD)
        window(face, plane, u, STOREY + 0.38, shutters=WOOD, open_shutters=False)
window("-x", -3.45, -1.80, 2 * STOREY + 0.38, shutters=PAINT_GREEN)
# Höfe hinter den Häusern
fence([(-3.52, YB), (-3.52, 3.58), (3.52, 3.58), (3.52, YB)], h=0.32)
for x in (-1.775, -0.025, 1.725):
    fence([(x, YB), (x, 3.58)], h=0.32)
washing_line(-3.20, 1.00, -2.05, 1.00)
woodpile(-2.60, 2.90, length=0.40, rows=(4, 3, 2), yaw=0)
amphora(-3.25, 0.05)
fig_tree(-0.90, 1.70, 0.95)
bench(-0.90, 0.30, length=0.40)
pot_plant(-1.50, 3.25, 0.05)
pot_plant(-0.30, 3.25, 0.05, OLIVE_LEAF)
veg_bed(0.20, 1.50, 1.40, 3.30, rows_along="y")
coop(1.30, 0.40, w=0.34, d=0.26)
for x, y, yaw in ((0.40, 0.55, 30), (0.75, 0.75, 200), (0.55, 0.95, -60)):
    chicken(x, y, yaw)
open_shed(1.95, 3.30, 2.55, 3.45, 0.80)
cart(2.60, 3.00, yaw=-90, load="sacks")
oven(2.95, 0.50)
for x, y in ((2.05, 0.10), (2.18, 0.05)):
    amphora(x, y)
variant("Insula_1")

export(spacing=8.6)
