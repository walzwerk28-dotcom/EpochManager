"""
Schmalhaus der Bauern (Antike), 1 x 2 Felder (2 m Front, 4 m tief), zwei Varianten:
  Variante 0: ocker, zweistöckig mit dem Giebel zur Straße, Holzbalkon (Maenianum) über der Tür, Gesims zwischen den
              Stockwerken; dahinter ein umzäunter Garten mit Gemüsebeet, Olivenbaum, Bank und Amphoren.
  Variante 1: weiß, langes einstöckiges Haus mit Laubengang auf Säulen an der Seite (Pultdach), Seitentür, darunter
              Bank und Amphoren; hinten Hof mit Backofen, Brennholz und Wäscheleine.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/schmalhaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 1,84 x 3,68 m (Grundfläche 2 x 4 m), Vorderseite (Schmalseite) nach -Y. Die FBX enthält je
Variante ein Objekt (Schmalhaus_0, Schmalhaus_1).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(33)

# ---------------------------------------------------------------- Variante 0: zweistöckig mit Balkon und Garten

X0, X1, Y0, Y1, H = -0.66, 0.66, -1.50, 0.05, 2 * STOREY
body(X0, X1, Y0, Y1, H, PLASTER_OCHRE)
gable_roof(X0, X1, Y0, Y1, H, "y", PLASTER_OCHRE, overhang=0.08)
box((X1 - X0 + 0.05, Y1 - Y0 + 0.05, 0.05), (0.0, (Y0 + Y1) / 2, STOREY), material=LIME_DARK)   # Gesims
door("-y", Y0, -0.30)
window("-y", Y0, 0.30, 0.42, shutters=WOOD)
door("-y", Y0, 0.0, w=0.30, h=0.55, z0=STOREY + 0.06)                    # Tür zum Balkon
for face, plane in (("-x", X0), ("+x", X1)):
    window(face, plane, -0.85, 0.45, shutters=WOOD)
    for u in (-1.10, -0.45):
        window(face, plane, u, STOREY + 0.38, shutters=WOOD, open_shutters=(u < -1.0))
window("+y", Y1, 0.0, 0.45, shutters=WOOD)
window("+y", Y1, 0.0, STOREY + 0.38, shutters=WOOD, open_shutters=False)
# Balkon: Bohlenboden auf drei Kragbalken, Geländer mit Stäben
BY = Y0 - 0.17
for x in (-0.50, 0.0, 0.50):
    box((0.04, 0.22, 0.05), (x, Y0 - 0.10, STOREY + 0.02), material=WOOD_DARK)
box((1.16, 0.20, 0.03), (0.0, Y0 - 0.10, STOREY + 0.06), material=WOOD)
box((1.16, 0.025, 0.025), (0.0, BY, STOREY + 0.32), material=WOOD_DARK)
for x in (-0.58, 0.58):
    box((0.025, 0.20, 0.025), (x, Y0 - 0.10, STOREY + 0.32), material=WOOD_DARK)
for k in range(13):
    x = -0.57 + k * 1.14 / 12
    box((0.012, 0.012, 0.24), (x, BY, STOREY + 0.19), material=WOOD_DARK)
for x in (-0.57, 0.57):
    for k in range(1, 4):
        box((0.012, 0.012, 0.24), (x, Y0 - 0.17 + k * 0.045, STOREY + 0.19), material=WOOD_DARK)
pot_plant(-0.40, Y0 - 0.10, 0.04)                                          # Töpfe auf dem Balkon (stehen auf dem Boden)
PARTS[-1].location.z += STOREY + 0.075
PARTS[-2].location.z += STOREY + 0.075
pot_plant(0.75, -1.62, 0.05)
amphora(-0.78, -1.64)
# Garten hinter dem Haus
fence([(-0.86, 0.12), (-0.86, 1.80), (0.86, 1.80), (0.86, 0.12)], h=0.30)
veg_bed(-0.72, 0.08, 0.42, 1.22, rows_along="y")
olive_tree(0.46, 1.36, 0.9)
bench(0.46, 0.42, length=0.40)
amphora(-0.55, 0.24)
amphora(-0.42, 0.22)
veg_bed(-0.72, 0.08, 1.38, 1.68, rows_along="x", plant=OLIVE_LEAF)
variant("Schmalhaus_0")

# ---------------------------------------------------------------- Variante 1: einstöckig mit Laubengang

X0, X1, Y0, Y1, H = -0.38, 0.78, -1.45, 0.95, STOREY
CX = (X0 + X1) / 2
body(X0, X1, Y0, Y1, H, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(X0, X1, Y0, Y1, H, "y", PLASTER_WHITE, overhang=0.08)
window("-y", Y0, CX - 0.26, 0.42, shutters=PAINT_GREEN)
window("-y", Y0, CX + 0.26, 0.42, shutters=PAINT_GREEN)
window("-y", Y0, CX, 1.07, w=0.12, h=0.10, shutters=None)
door("-x", X0, -0.05)
window("-x", X0, -0.85, 0.42, shutters=PAINT_GREEN)
window("-x", X0, 0.62, 0.42, shutters=PAINT_GREEN, open_shutters=False)
for u in (-0.95, -0.15, 0.60):
    window("+x", X1, u, 0.45, shutters=PAINT_GREEN, open_shutters=(u > -0.5))
door("+y", Y1, CX)
# Laubengang: Pultdach auf vier Säulen an der linken Seite
PZ = 0.86
shed_roof(-0.83, X0, -1.22, 0.80, PZ, "-x", overhang=0.06, pitch=14)
post_h = PZ - (X0 + 0.82) * math.tan(math.radians(14))
for y in (-1.16, -0.48, 0.16, 0.74):
    box((0.07, 0.07, 0.04), (-0.82, y, 0.02), material=LIME_DARK)
    cyl(0.025, post_h - 0.04, (-0.82, y, 0.04 + (post_h - 0.04) / 2), material=LIME, verts=10)
box((0.04, 2.0, 0.05), (-0.82, -0.21, post_h + 0.01), material=WOOD_DARK)   # Pfette
box((0.44, 2.04, 0.02), (-0.60, -0.21, 0.01), material=LIME_DARK)          # Boden des Gangs
bench(-0.60, -0.60, yaw=90, length=0.42)
amphora(-0.66, 0.40)
amphora(-0.56, 0.52)
jug(-0.70, 0.62, 0.0, 0.035)
pot_plant(-0.80, -1.50, 0.05)
# Hof hinter dem Haus
oven(0.42, 1.48)
woodpile(-0.20, 1.60, length=0.36, rows=(4, 3, 2), yaw=0)
washing_line(-0.84, 1.10, -0.84, 1.76)
fence([(-0.88, 1.82), (0.88, 1.82)], h=0.28)
sack(0.75, 1.20, 0.0, 0.05)
variant("Schmalhaus_1")

export(spacing=2.4)
