"""
Stadthaus der Bauern (Antike), 2 x 2 Felder, zwei Varianten:
  Variante 0: Doppelhaus an der Straße: links zweistöckig ocker mit Balkon, rechts einstöckig weiß mit roter
              Sockelzone; dahinter zwei Höfe, durch einen Zaun getrennt (links Backofen und Brennholz, rechts
              Weinlaube mit Bank und Gemüsebeet).
  Variante 1: zweistöckiges rosa Haus quer zur Straße mit einem Stall als Anbau (Pultdach, breite Tür); dahinter ein
              ummauerter Hof mit Zisterne, Hühnern, Heu und Wäscheleine.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/stadthaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 3,68 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(41)

# ---------------------------------------------------------------- Variante 0: Doppelhaus mit zwei Höfen

YF, YB = -1.45, 0.05
# links zweistöckig
body(-1.70, 0.0, YF, YB, 2 * STOREY, PLASTER_OCHRE)
gable_roof(-1.70, 0.0, YF, YB, 2 * STOREY, "x", PLASTER_OCHRE, overhang=0.08)
box((1.75, YB - YF + 0.05, 0.05), (-0.85, (YF + YB) / 2, STOREY), material=LIME_DARK)       # Gesims
door("-y", YF, -1.25)
window("-y", YF, -0.50, 0.42, shutters=WOOD)
balcony(YF, -1.55, -0.70, STOREY + 0.06)
door("-y", YF, -1.12, w=0.30, h=0.55, z0=STOREY + 0.06)
window("-y", YF, -0.35, STOREY + 0.38, shutters=WOOD)
for u in (-1.0, -0.35):
    window("-x", -1.70, u, 0.42, shutters=WOOD, open_shutters=(u < -0.5))
    window("-x", -1.70, u, STOREY + 0.38, shutters=WOOD)
window("+y", YB, -1.20, 0.42, shutters=WOOD)
window("+y", YB, -0.55, STOREY + 0.38, shutters=WOOD, open_shutters=False)
door("+y", YB, -0.50)
# rechts einstöckig
body(0.0, 1.70, YF, YB, STOREY, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(0.0, 1.70, YF, YB, STOREY, "x", PLASTER_WHITE, overhang=0.08)
door("-y", YF, 0.45)
window("-y", YF, 1.20, 0.42, shutters=PAINT_GREEN)
window("+x", 1.70, -0.70, 0.45, shutters=PAINT_GREEN)
window("+y", YB, 1.20, 0.45, shutters=PAINT_GREEN)
door("+y", YB, 0.45)
bench(1.20, -1.62, length=0.42)
pot_plant(0.05, -1.60, 0.05)
amphora(-1.62, -1.62)
# Höfe hinter den Häusern
fence([(-1.78, 0.10), (-1.78, 1.80), (1.78, 1.80), (1.78, 0.10)], h=0.30)
fence([(0.0, 0.10), (0.0, 1.80)], h=0.30)
oven(-1.30, 1.30)
woodpile(-0.50, 1.55, length=0.38, rows=(4, 3, 2), yaw=0)
path([(-0.50, 0.20), (-0.80, 0.90)])
jug(-1.55, 0.35, 0.0, 0.04)
pergola(0.25, 1.45, 0.40, 1.00, h=0.84, vine_post=1)
bench(0.85, 0.62, length=0.46)
veg_bed(0.20, 1.55, 1.22, 1.68, rows_along="x")
variant("Stadthaus_0")

# ---------------------------------------------------------------- Variante 1: Haus mit Stall und Hof mit Zisterne

YF, YB = -1.45, -0.10
X0, X1 = -1.70, 0.85
body(X0, X1, YF, YB, 2 * STOREY, PLASTER_PINK, socle=SOCLE_RED)
gable_roof(X0, X1, YF, YB, 2 * STOREY, "x", PLASTER_PINK, overhang=0.08)
box((X1 - X0 + 0.05, YB - YF + 0.05, 0.05), ((X0 + X1) / 2, (YF + YB) / 2, STOREY), material=LIME_DARK)
door("-y", YF, -0.75)
for u in (-1.35, -0.10, 0.50):
    window("-y", YF, u, 0.42, shutters=PAINT_BLUE, open_shutters=(u != 0.50))
for u in (-1.30, -0.70, -0.05, 0.50):
    window("-y", YF, u, STOREY + 0.38, shutters=PAINT_BLUE, open_shutters=(u in (-0.70, 0.50)))
window("-x", X0, -0.78, 0.42, shutters=PAINT_BLUE)
window("-x", X0, -0.78, STOREY + 0.38, shutters=PAINT_BLUE)
door("+y", YB, -0.40)
for u in (-1.20, 0.40):
    window("+y", YB, u, STOREY + 0.38, shutters=PAINT_BLUE)
# Stall als Anbau mit Pultdach, breites Tor zur Straße
lean_to(X1, 1.70, YF, YB, 0.95, "+x", PLASTER_WHITE)
door("-y", YF, 1.27, w=0.44, h=0.66, leaf=WOOD_DARK)
window("+x", 1.70, -0.78, 0.55, w=0.18, h=0.14, shutters=None)
hay(1.45, -1.68, 0.14)
pot_plant(-1.60, -1.62, 0.055, OLIVE_LEAF)
pot_plant(-0.35, -1.62, 0.05)
# ummauerter Hof
court_wall([(-1.75, -0.10), (-1.75, 1.76), (1.75, 1.76), (1.75, -0.10)], plaster=PLASTER_PINK)
cistern(-0.60, 0.80)
path([(-0.40, 0.0), (-0.55, 0.55)])
for x, y, yaw in ((0.70, 0.55, 20), (0.90, 0.80, 200), (0.55, 0.95, -70), (1.10, 0.45, 110)):
    chicken(x, y, yaw)
coop(1.38, 0.30, w=0.40, d=0.30)
washing_line(-1.50, 1.45, -0.20, 1.45)
hay(1.30, 1.35, 0.22)
cart(0.55, 1.40, yaw=-90, load="hay")
amphora(-1.55, 0.10)
amphora(-1.42, 0.08)
variant("Stadthaus_1")

export(spacing=4.4)
