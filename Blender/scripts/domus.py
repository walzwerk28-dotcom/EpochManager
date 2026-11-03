"""
Domus der Bauern (Antike), 3 x 3 Felder (6 x 6 m), zwei Varianten:
  Variante 1: Atriumhaus: zur Straße zwei Läden (Tabernae) mit Theke und Markise beiderseits des Eingangs; Flügel mit
              Pultdächern, die nach innen zum offenen Atrium mit Wasserbecken (Impluvium) abfallen; hinten ein
              zweistöckiger Hauptbau (Tablinum) mit Satteldach; dahinter ein ummauerter Säulengarten mit Brunnenschale,
              Büschen, Beeten und Bank.
  Variante 0: zweistöckiges Haus in L-Form (ocker, rote Sockelzone) mit Balkon, einem Laden und Seitenflügel; dahinter
              ein ummauerter Garten mit Weinlaube, Beeten, Oliven- und Feigenbaum, Zisterne, Bank und Weg.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/domus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 5,52 x 5,52 m (Grundfläche 6 x 6 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(81)

# ---------------------------------------------------------------- Variante 1: Atriumhaus (Name wie bisher: 0 Garten, 1 Atrium)

X0, X1 = -2.55, 2.55
YF, YA, YB, YG = -2.35, -1.35, 0.45, 1.40        # Vorderseite, Atrium vorn, Atrium hinten, Hauptbau hinten
W = 1.0                                          # Tiefe der Flügel
LOW = 1.0                                        # Traufhöhe zum Atrium
plaster = PLASTER_WHITE
# Flügel mit Pultdach nach innen
lean_to(X0, X1, YF, YA, LOW, "+y", plaster, pitch=16, overhang=0.07, eave_overhang=0.20, socle=SOCLE_RED)
lean_to(X0, X0 + W, YA, YB, LOW, "+x", plaster, pitch=16, overhang=0.0, eave_overhang=0.20, socle=SOCLE_RED)
lean_to(X1 - W, X1, YA, YB, LOW, "-x", plaster, pitch=16, overhang=0.0, eave_overhang=0.20, socle=SOCLE_RED)
# Hauptbau hinten, zweistöckig
body(X0, X1, YB, YG, 2 * STOREY, plaster, socle=SOCLE_RED)
gable_roof(X0, X1, YB, YG, 2 * STOREY, "x", plaster, overhang=0.07)
cornice(X0, X1, YB, YG, STOREY + 0.1)
# Straßenseite: Eingang zwischen zwei Läden
door("-y", YF, 0.0, w=0.36, h=0.72)
shop_front("-y", YF, -1.55, w=0.66, goods=("jug", "amphora", "jug", "basket"), awning=(CLOTH_RED, CLOTH))
shop_front("-y", YF, 1.55, w=0.66, goods=("bread", "basket", "bread", "basket"), awning=(CLOTH_OCHRE, CLOTH))
for u in (-0.55, 0.55):
    window("-y", YF, u, 0.62, w=0.14, h=0.18, shutters=None)
# Seiten und Rückseite: kleine Fenster
for face, plane in (("-x", X0), ("+x", X1)):
    for u in (-2.0, -0.8, 0.0):
        window(face, plane, u, 0.62, w=0.14, h=0.18, shutters=None)
    window(face, plane, 0.85, 0.42, shutters=WOOD)
    window(face, plane, 0.85, STOREY + 0.38, shutters=WOOD, open_shutters=False)
for u in (-1.6, -0.5, 0.6, 1.7):
    window("+y", YG, u, STOREY + 0.38, shutters=WOOD, open_shutters=(u < 0))
# Atrium: Becken, Marmortisch, Töpfe, zum Hauptbau eine breite Öffnung
impluvium(0.0, (YA + YB) / 2 - 0.05, 1.10, 0.70)
box((0.40, 0.18, 0.04), (0.0, YB - 0.30, 0.30), material=LIME)                         # Cartibulum
for x in (-0.14, 0.14):
    box((0.05, 0.12, 0.28), (x, YB - 0.30, 0.14), material=LIME_DARK)
box((1.00, 0.012, 0.80), (0.0, YB - 0.006, 0.49), material=DARK)                     # Öffnung zum Tablinum
box((1.10, 0.04, 0.06), (0.0, YB - 0.02, 0.92), material=WOOD_DARK)
for x in (-0.80, 0.80):
    pot_plant(x, YB - 0.25, 0.06, OLIVE_LEAF)
    door("-x" if x > 0 else "+x", (X1 - W) if x > 0 else (X0 + W), -0.6, w=0.28, h=0.6)
bench(-0.9, YA + 0.30, length=0.40)
# Säulengarten hinter dem Hauptbau
court_wall([(X0 - 0.05, YG), (X0 - 0.05, 2.66), (X1 + 0.05, 2.66), (X1 + 0.05, YG)], plaster=plaster)
colonnade((-2.10, 1.70), (2.10, 1.70), 6, 0.90)
fountain_bowl(0.0, 2.20, 0.17)
for x in (-1.9, -1.2, 1.2, 1.9):
    bush(x, 2.35, 0.13, LEAF if x < 0 else LEAF_DARK)
veg_bed(-1.0, -0.35, 1.95, 2.50, rows_along="x", plant=LEAF)
veg_bed(0.35, 1.0, 1.95, 2.50, rows_along="x", plant=OLIVE_LEAF)
bench(0.0, 1.55, length=0.42)
door("+y", YG, 0.0, w=0.30, h=0.62)
amphora(-2.35, -2.50)
variant("Domus_1")

# ---------------------------------------------------------------- Variante 0: L-Haus mit großem Garten

X0, X1 = -2.55, 2.55
YF, YB = -2.35, -1.15
plaster = PLASTER_OCHRE
body(X0, X1, YF, YB, 2 * STOREY, plaster, socle=SOCLE_RED)
gable_roof(X0, X1, YF, YB, 2 * STOREY, "x", plaster, overhang=0.08)
cornice(X0, X1, YF, YB, STOREY)
# Seitenflügel rechts, zweistöckig, First quer
SX0 = 1.35
body(SX0, X1, YB, 0.55, 2 * STOREY, plaster, socle=SOCLE_RED)
gable_roof(SX0, X1, YB - 0.4, 0.55, 2 * STOREY, "y", plaster, overhang=0.08)
cornice(SX0, X1, YB, 0.55, STOREY)
# Straßenseite
door("-y", YF, -0.70)
shop_front("-y", YF, 1.60, w=0.66, goods=("jug", "jug", "amphora", "basket"), awning=(PAINT_BLUE, CLOTH))
for u in (-2.0, 0.30):
    window("-y", YF, u, 0.42, shutters=PAINT_GREEN)
balcony(YF, -2.20, -0.30, STOREY + 0.06, depth=0.20)
door("-y", YF, -1.25, w=0.30, h=0.55, z0=STOREY + 0.06)
for u in (0.25, 1.05, 1.85):
    window("-y", YF, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=(u != 1.05))
window("-x", X0, -1.95, 0.42, shutters=PAINT_GREEN)
window("-x", X0, -1.95, STOREY + 0.38, shutters=PAINT_GREEN)
for u in (-1.95, -0.85, 0.05):
    window("+x", X1, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u != -0.85))
    window("+x", X1, u, STOREY + 0.38, shutters=PAINT_GREEN)
# Gartenseite
door("+y", YB, -0.70)
for u in (-2.0, 0.40):
    window("+y", YB, u, 0.42, shutters=PAINT_GREEN)
    window("+y", YB, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=False)
door("-x", SX0, -0.40)
window("-x", SX0, 0.20, STOREY + 0.38, shutters=PAINT_GREEN)
window("+y", 0.55, 1.95, STOREY + 0.38, shutters=PAINT_GREEN)
# Garten mit Mauer
court_wall([(X0 - 0.05, YB), (X0 - 0.05, 2.66), (X1 + 0.05, 2.66), (X1 + 0.05, 0.55)], plaster=plaster)
path([(-0.70, -1.25), (-0.30, -0.10), (0.20, 0.90)])
pergola(-2.30, -1.05, -1.00, 0.05, h=0.86, vine_post=1)
bench(-1.67, -0.75, length=0.46)
cistern(0.55, -0.55)
veg_bed(-2.30, -0.80, 0.95, 2.45, rows_along="y")
olive_tree(0.55, 1.75, 1.05)
fig_tree(1.85, 1.40, 0.95)
bench(1.70, 2.35, length=0.42)
for x, y in ((1.20, 0.75), (1.32, 0.80)):
    amphora(x, y)
woodpile(-0.05, 2.40, length=0.38, rows=(4, 3, 2), yaw=0)
variant("Domus_0")

export(spacing=6.6)
