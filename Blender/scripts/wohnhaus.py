"""
Wohnhaus der Bauern (Antike), 1 x 1 Feld, drei Varianten: kleine verputzte Häuser mit Ziegeldach auf Bruchsteinsockel.
  Variante 0: weiß mit roter Sockelzone, First parallel zur Straße, grüne Läden, Steinbank unter dem Fenster,
              Trittsteine zur Straße, Amphore und Kräutertöpfe, Brennholz hinter dem Haus.
  Variante 1: ocker, blaue Läden, vor dem Haus eine Weinlaube mit Bank und Krügen, daneben Lehmbackofen,
              Brennholz und Amphoren.
  Variante 2: rosa, Giebel zur Straße mit Dachbodenluke, daneben Hühnerstall mit Zaun und Hühnern, Feigenbaum.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/wohnhaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 1,84 x 1,84 m (Grundfläche 2 x 2 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Wohnhaus_0 ...); BlenderModels macht daraus die Varianten des Prefabs.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(21)

# ---------------------------------------------------------------- Variante 0: weiß, First parallel zur Straße

X0, X1, Y0, Y1, H = -0.72, 0.72, -0.30, 0.62, STOREY
body(X0, X1, Y0, Y1, H, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(X0, X1, Y0, Y1, H, "x", PLASTER_WHITE)
door("-y", Y0, -0.30)
window("-y", Y0, 0.30, 0.42, shutters=PAINT_GREEN)
window("-x", X0, 0.16, 0.45, shutters=PAINT_GREEN)
window("+x", X1, 0.16, 0.45, shutters=PAINT_GREEN)
window("+y", Y1, 0.0, 0.45, shutters=PAINT_GREEN, open_shutters=False)
bench(0.30, -0.52, length=0.42)
for y in (-0.52, -0.74):                                                  # Trittsteine zur Straße
    box((0.20, 0.14, 0.02), (-0.30 + random.uniform(-0.02, 0.02), y, 0.01), rot=(0, 0, random.uniform(-8, 8)), material=LIME_DARK)
amphora(-0.62, -0.44)
pot_plant(-0.05, -0.42, 0.05)
pot_plant(0.66, -0.44, 0.055, OLIVE_LEAF)
woodpile(0.30, 0.82, length=0.40, rows=(3, 2), yaw=0)
jug(-0.45, 0.80, 0.0, 0.04)
variant("Wohnhaus_0")

# ---------------------------------------------------------------- Variante 1: ocker, Weinlaube, Backofen

X0, X1, Y0, Y1 = -0.83, 0.34, -0.10, 0.72
body(X0, X1, Y0, Y1, H, PLASTER_OCHRE)
gable_roof(X0, X1, Y0, Y1, H, "x", PLASTER_OCHRE, overhang=0.07)
door("-y", Y0, 0.05)
window("-y", Y0, -0.50, 0.42, shutters=PAINT_BLUE)
window("+x", X1, 0.30, 0.45, shutters=PAINT_BLUE)
window("-x", X0, 0.30, 0.45, shutters=PAINT_BLUE, open_shutters=False)
window("+y", Y1, -0.25, 0.45, shutters=PAINT_BLUE)
pergola(-0.74, 0.26, -0.74, -0.32, h=0.84, vine_post=0)
bench(-0.48, -0.20, length=0.44)
jug(-0.10, -0.62, 0.0, 0.035)
jug(-0.02, -0.66, 0.0, 0.03)
oven(0.64, 0.44)
woodpile(0.66, -0.05, length=0.36, rows=(4, 3, 2), yaw=90)
amphora(0.78, -0.56)
amphora(0.64, -0.64)
pot_plant(0.80, -0.82, 0.05)
variant("Wohnhaus_1")

# ---------------------------------------------------------------- Variante 2: rosa, Giebel zur Straße, Hühner

X0, X1, Y0, Y1 = -0.70, 0.36, -0.50, 0.70
CX = (X0 + X1) / 2
body(X0, X1, Y0, Y1, H, PLASTER_PINK)
gable_roof(X0, X1, Y0, Y1, H, "y", PLASTER_PINK, overhang=0.08)
door("-y", Y0, CX)
window("-y", Y0, CX, 1.07, w=0.12, h=0.10, shutters=None)                 # Dachbodenluke im Giebel
window("-x", X0, -0.10, 0.45, shutters=WOOD)
window("-x", X0, 0.40, 0.45, shutters=WOOD, open_shutters=False)
window("+x", X1, -0.15, 0.45, shutters=WOOD)
window("+y", Y1, CX, 0.45, shutters=WOOD)
pot_plant(CX - 0.29, -0.62, 0.05)
pot_plant(CX + 0.29, -0.62, 0.05, OLIVE_LEAF)
coop(0.66, 0.50)
fence([(0.40, -0.06), (0.88, -0.06), (0.88, 0.82), (0.40, 0.82)], h=0.24)
for x, y, yaw in ((0.58, 0.14, 30), (0.76, 0.22, 160), (0.52, 0.32, -60)):
    chicken(x, y, yaw)
fig_tree(0.64, -0.50, 0.8)
amphora(-0.82, -0.30)
sack(-0.82, 0.10, 0.0, 0.05)
variant("Wohnhaus_2")

export(spacing=2.4)
