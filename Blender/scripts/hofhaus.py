"""
Hofhaus der Bauern (Antike), 2 x 3 Felder (4 m Front, 6 m tief), zwei Varianten:
  Variante 0: zwei Häuser an der Straße (links zweistöckig weiß mit dem Giebel zur Straße, rechts einstöckig ocker),
              dahinter ein großer Garten mit Zaun: Olivenbaum, Feigenbaum, Gemüsebeete, Weg, Bank, Geräteschuppen.
  Variante 1: kleiner Bauernhof: vorn ein langes einstöckiges Wohnhaus mit Hoftor in der Mitte, dahinter ein
              ummauerter Hof mit Zisterne, Hühnern und Brennholz, hinten ein offener Schuppen mit Karren, Säcken und
              Kisten, daneben Heu.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/hofhaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 5,52 m (Grundfläche 4 x 6 m), Vorderseite (Schmalseite) nach -Y. Die FBX enthält je
Variante ein Objekt.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(52)

# ---------------------------------------------------------------- Variante 0: zwei Häuser und Garten

YF = -2.50
# links zweistöckig, Giebel zur Straße
X0, X1, YB = -1.70, -0.15, -0.95
body(X0, X1, YF, YB, 2 * STOREY, PLASTER_WHITE)
gable_roof(X0, X1, YF, YB, 2 * STOREY, "y", PLASTER_WHITE, overhang=0.08)
box((X1 - X0 + 0.05, YB - YF + 0.05, 0.05), ((X0 + X1) / 2, (YF + YB) / 2, STOREY), material=LIME_DARK)
door("-y", YF, -1.25)
window("-y", YF, -0.60, 0.42, shutters=PAINT_GREEN)
for u in (-1.25, -0.60):
    window("-y", YF, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=(u < -1.0))
window("-y", YF, (X0 + X1) / 2, 2 * STOREY + 0.08, w=0.12, h=0.10, shutters=None)
for u in (-2.0, -1.35):
    window("-x", X0, u, 0.42, shutters=PAINT_GREEN)
    window("-x", X0, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=(u > -1.5))
door("+y", YB, -0.95)
window("+y", YB, -0.45, STOREY + 0.38, shutters=PAINT_GREEN)
# rechts einstöckig, First parallel zur Straße
X0, X1, YB = -0.15, 1.70, -1.15
body(X0, X1, YF, YB, STOREY, PLASTER_OCHRE)
gable_roof(X0, X1, YF, YB, STOREY, "x", PLASTER_OCHRE, overhang=0.08)
door("-y", YF, 0.45)
window("-y", YF, 1.20, 0.42, shutters=WOOD)
window("+x", X1, -1.80, 0.45, shutters=WOOD)
window("+y", YB, 1.20, 0.45, shutters=WOOD, open_shutters=False)
door("+y", YB, 0.40)
bench(1.20, -2.66, length=0.42)
amphora(-1.62, -2.66)
pot_plant(-0.08, -2.64, 0.05)
# Garten
fence([(-1.78, -0.90), (-1.78, 2.72), (1.78, 2.72), (1.78, -1.10)], h=0.30)
path([(0.40, -1.00), (0.10, 0.20), (0.10, 1.60)])
olive_tree(-1.05, 1.95, 1.0)
fig_tree(1.10, 0.10, 0.95)
veg_bed(-1.55, -0.35, -0.50, 0.70, rows_along="y")
veg_bed(0.45, 1.55, 0.95, 1.65, rows_along="x", plant=OLIVE_LEAF)
bench(-0.45, 1.30, yaw=90, length=0.40)
open_shed(0.55, 1.65, 2.05, 2.62, 0.70)
woodpile(1.10, 2.35, length=0.36, rows=(4, 3), yaw=0)
amphora(0.70, 2.30)
variant("Hofhaus_0")

# ---------------------------------------------------------------- Variante 1: kleiner Bauernhof

YF, YB = -2.50, -1.20
X0, X1 = -1.70, 1.70
body(X0, X1, YF, YB, STOREY, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(X0, X1, YF, YB, STOREY, "x", PLASTER_WHITE, overhang=0.08)
door("-y", YF, 0.0, w=0.52, h=0.70, leaf=WOOD_DARK)                     # Hoftor
box((0.008, 0.008, 0.66), (0.0, YF - 0.026, 0.44), material=WOOD_DARK)  # Fuge zwischen den Torflügeln
door("-y", YF, -1.05)
for u in (-0.55, 0.65, 1.25):
    window("-y", YF, u, 0.42, shutters=WOOD, open_shutters=(u != 0.65))
window("-x", X0, -1.85, 0.42, shutters=WOOD)
window("+x", X1, -1.85, 0.42, shutters=WOOD, open_shutters=False)
door("+y", YB, 0.0, w=0.52, h=0.70, leaf=WOOD_DARK)
door("+y", YB, -1.05)
window("+y", YB, 1.10, 0.42, shutters=WOOD)
pot_plant(-1.55, -2.66, 0.05)
amphora(1.55, -2.66)
# ummauerter Hof
court_wall([(-1.75, -1.20), (-1.75, 2.66), (1.75, 2.66), (1.75, -1.20)])
path([(0.0, -1.10), (0.0, 0.40)])
cistern(-0.85, -0.15)
for x, y, yaw in ((0.55, -0.40, 40), (0.80, -0.20, 170), (0.40, 0.05, -40), (0.95, 0.20, 80), (0.65, 0.35, 250)):
    chicken(x, y, yaw)
coop(1.40, -0.60, w=0.40, d=0.30)
woodpile(-1.50, 0.60, length=0.38, rows=(4, 3, 2), yaw=90)
# Schuppen hinten
open_shed(-1.66, 0.70, 1.75, 2.58, 0.85, plaster=PLASTER_WHITE)
cart(-0.95, 2.10, yaw=-90, load="sacks")
for x, y in ((0.10, 2.35), (0.30, 2.40)):
    sack(x, y, 0.0, 0.065)
crate(-0.25, 2.40, size=0.18, yaw=8)
crate(-0.25, 2.40, z0=0.18, size=0.15, yaw=-12)
hay(1.25, 2.10, 0.26)
hay(1.30, 1.55, 0.18)
variant("Hofhaus_1")

export(spacing=4.4)
