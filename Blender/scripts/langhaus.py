"""
Langhaus der Bauern (Antike), 2 x 4 Felder (4 m Front, 8 m tief), zwei Varianten:
  Variante 0: zweistöckiges Vorderhaus (ocker, Balkon über der Tür), dahinter ein Hof zwischen Mauern mit Weinlaube,
              Bank, Zisterne und Beet, hinten ein einstöckiges Hinterhaus (weiß).
  Variante 1: zwei schmale lange Häuser nebeneinander mit dem Giebel zur Straße (links einstöckig ocker, rechts
              zweistöckig weiß mit Balkon), dahinter je ein Garten mit Olivenbaum, Feigenbaum, Beeten und Zaun.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/langhaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 7,36 m (Grundfläche 4 x 8 m), Vorderseite (Schmalseite) nach -Y. Die FBX enthält je
Variante ein Objekt.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(63)

# ---------------------------------------------------------------- Variante 0: Vorderhaus, Hof, Hinterhaus

YF, YB = -3.45, -1.85
X0, X1 = -1.65, 1.65
body(X0, X1, YF, YB, 2 * STOREY, PLASTER_OCHRE)
gable_roof(X0, X1, YF, YB, 2 * STOREY, "x", PLASTER_OCHRE, overhang=0.08)
box((X1 - X0 + 0.05, YB - YF + 0.05, 0.05), (0.0, (YF + YB) / 2, STOREY), material=LIME_DARK)
door("-y", YF, -0.20)
for u in (-1.20, 0.55, 1.20):
    window("-y", YF, u, 0.42, shutters=WOOD, open_shutters=(u != 1.20))
balcony(YF, -0.75, 0.35, STOREY + 0.06)
door("-y", YF, -0.20, w=0.30, h=0.55, z0=STOREY + 0.06)
for u in (-1.25, 0.85):
    window("-y", YF, u, STOREY + 0.38, shutters=WOOD, open_shutters=(u < 0))
for face, plane in (("-x", X0), ("+x", X1)):
    window(face, plane, -2.65, 0.42, shutters=WOOD)
    window(face, plane, -2.65, STOREY + 0.38, shutters=WOOD, open_shutters=False)
door("+y", YB, 0.40)
for u in (-1.0, 1.10):
    window("+y", YB, u, 0.42, shutters=WOOD)
    window("+y", YB, u, STOREY + 0.38, shutters=WOOD)
amphora(-1.55, -3.60)
pot_plant(1.55, -3.60, 0.055, OLIVE_LEAF)
pot_plant(0.15, -3.60, 0.05)
# Hof zwischen Mauern
court_wall([(-1.72, -1.85), (-1.72, 1.55)], plaster=PLASTER_OCHRE)
court_wall([(1.72, -1.85), (1.72, 1.55)], plaster=PLASTER_OCHRE)
pergola(-1.45, -0.25, -1.45, -0.35, h=0.86, vine_post=2)
bench(-0.85, -0.95, length=0.48)
cistern(0.85, -0.55)
path([(0.40, -1.75), (0.15, -0.20), (0.05, 1.20)])
veg_bed(-1.50, -0.30, 0.20, 1.20, rows_along="y")
fig_tree(1.05, 0.75, 0.9)
amphora(1.50, -1.55)
amphora(1.38, -1.62)
# Hinterhaus
YF2, YB2 = 1.55, 3.40
body(X0, X1, YF2, YB2, STOREY, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(X0, X1, YF2, YB2, STOREY, "x", PLASTER_WHITE, overhang=0.08)
door("-y", YF2, 0.05)
for u in (-1.05, 0.75):
    window("-y", YF2, u, 0.42, shutters=PAINT_GREEN)
window("-x", X0, 2.45, 0.42, shutters=PAINT_GREEN)
window("+x", X1, 2.45, 0.42, shutters=PAINT_GREEN, open_shutters=False)
for u in (-0.80, 0.80):
    window("+y", YB2, u, 0.45, shutters=PAINT_GREEN, open_shutters=False)
woodpile(-1.00, 1.38, length=0.40, rows=(3, 2), yaw=0)
variant("Langhaus_0")

# ---------------------------------------------------------------- Variante 1: zwei schmale lange Häuser mit Gärten

YF = -3.45
# links einstöckig ocker
X0, X1, YB = -1.70, -0.12, 0.10
CX = (X0 + X1) / 2
body(X0, X1, YF, YB, STOREY, PLASTER_OCHRE)
gable_roof(X0, X1, YF, YB, STOREY, "y", PLASTER_OCHRE, overhang=0.08)
door("-y", YF, CX - 0.30)
window("-y", YF, CX + 0.35, 0.42, shutters=PAINT_BLUE)
window("-y", YF, CX, 1.08, w=0.12, h=0.10, shutters=None)
for u in (-2.90, -2.0, -1.10, -0.30):
    window("-x", X0, u, 0.42, shutters=PAINT_BLUE, open_shutters=(u in (-2.0, -0.30)))
door("+y", YB, CX)
# rechts zweistöckig weiß
X0, X1, YB = 0.12, 1.70, -1.20
CX2 = (X0 + X1) / 2
body(X0, X1, YF, YB, 2 * STOREY, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(X0, X1, YF, YB, 2 * STOREY, "y", PLASTER_WHITE, overhang=0.08)
box((X1 - X0 + 0.05, YB - YF + 0.05, 0.05), (CX2, (YF + YB) / 2, STOREY), material=LIME_DARK)
door("-y", YF, CX2 + 0.30)
window("-y", YF, CX2 - 0.35, 0.42, shutters=PAINT_GREEN)
balcony(YF, CX2 - 0.60, CX2 + 0.60, STOREY + 0.06)
door("-y", YF, CX2, w=0.30, h=0.55, z0=STOREY + 0.06)
window("-y", YF, CX2, 2 * STOREY + 0.08, w=0.12, h=0.10, shutters=None)
for u in (-2.85, -1.80):
    window("+x", X1, u, 0.42, shutters=PAINT_GREEN)
    window("+x", X1, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=(u > -2.0))
door("+y", YB, CX2)
window("+y", YB, CX2, STOREY + 0.38, shutters=PAINT_GREEN)
amphora(-1.62, -3.60)
pot_plant(0.0, -3.58, 0.05)
# Gärten
fence([(-1.78, 0.15), (-1.78, 3.62), (1.78, 3.62), (1.78, -1.15)], h=0.30)
fence([(0.0, -3.30), (0.0, 3.62)], h=0.30)
olive_tree(-1.20, 2.90, 1.0)
veg_bed(-1.55, -0.40, 0.55, 1.75, rows_along="y")
bench(-0.45, 2.35, yaw=90, length=0.40)
oven(-1.30, 0.40)
fig_tree(1.20, -0.55, 0.9)
pergola(0.30, 1.50, 0.65, 1.45, h=0.84, vine_post=3)
bench(0.90, 1.05, length=0.46)
veg_bed(0.30, 1.55, 2.00, 3.35, rows_along="x", plant=OLIVE_LEAF)
woodpile(0.45, -0.75, length=0.36, rows=(3, 2), yaw=90)
variant("Langhaus_1")

export(spacing=4.4)
