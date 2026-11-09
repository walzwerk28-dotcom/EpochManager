"""
Laden der Gewerbezone (Antike), 1 x 1 Feld (2 x 2 m), zwei Varianten:
  Variante 0: kleines einstöckiges Ladenhaus mit offener Ladenfront: gemauerte Theke mit Brot und Körben, gestreifte
              Markise, Satteldach, davor Getreidekörbe, Säcke und Bank, Aushängeschild.
  Variante 1: offener Eckstand: Vordach auf zwei Pfosten über einem Verkaufstisch mit Körben, Säcken und Amphore,
              dahinter ein kleiner Schuppen.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/laden.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 1,84 x 1,84 m, Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt (Laden_0, Laden_1).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(52)

# ---------------------------------------------------------------- Variante 0: Ladenhaus

X0, X1, Y0, Y1, H = -0.74, 0.74, -0.34, 0.80, STOREY
body(X0, X1, Y0, Y1, H, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(X0, X1, Y0, Y1, H, "x", PLASTER_WHITE, overhang=0.07)
cornice(X0, X1, Y0, Y1, H - 0.02)
shop_front("-y", Y0, -0.22, w=0.78, h=0.64, goods=("bread", "basket", "bread", "jug"), awning=(CLOTH_RED, CLOTH))
door("-y", Y0, 0.52)
sign_board("-y", Y0, 0.55, 0.70, w=0.18, h=0.12, arm=0.18, board=WOOD, mark="disc", mark_mat=SACK)
for face, plane in (("-x", X0), ("+x", X1)):
    window(face, plane, 0.15, 0.40, shutters=PAINT_GREEN)
window("+y", Y1, -0.30, 0.42, shutters=PAINT_GREEN, open_shutters=False)
door("+y", Y1, 0.35)
# Auslage vor dem Laden
basket_goods(-0.58, -0.60, 0.0, 0.075, "apples", seed=2)
basket_goods(-0.28, -0.66, 0.0, 0.075, "grain", seed=3)
sack(0.12, -0.58, 0.0, 0.075)
sack(0.28, -0.66, 0.0, 0.065)
loaf(-0.40, -0.40, 0.315, 25)
pot_plant(0.68, -0.52, 0.055)
amphora(-0.72, -0.40)
# Rückseite
sack(-0.45, 0.86, 0.0, 0.05)
sack(-0.30, 0.86, 0.0, 0.045)
barrel(0.55, 0.86, r=0.05)
variant("Laden_0")

# ---------------------------------------------------------------- Variante 1: offener Eckstand

# kleiner Schuppen hinten
SX0, SX1, SY0, SY1 = -0.74, 0.74, 0.28, 0.83
top = lean_to(SX0, SX1, SY0, SY1, 0.80, "+y", PLASTER_WHITE, pitch=14, socle=SOCLE_RED)
door("-y", SY0, 0.45)
window("-x", SX0, 0.57, 0.40, shutters=PAINT_GREEN)
window("+x", SX1, 0.57, 0.40, shutters=PAINT_GREEN, open_shutters=False)
# Vordach auf zwei Pfosten, Tuch fällt zur Straße
ZB, ZF = top - 0.02, 0.74
for x in (-0.68, 0.68):
    box((0.05, 0.05, ZF), (x, -0.62, ZF / 2), material=WOOD_DARK)
box((1.46, 0.05, 0.05), (0.0, -0.62, ZF - 0.01), material=WOOD_DARK)
striped_canopy(-0.76, 0.76, -0.70, SY0, ZB, ZF + 0.02, CLOTH_OCHRE, CLOTH)
# Verkaufstisch mit Waren
trestle_table(-0.05, -0.44, 0, w=1.10, d=0.30, h=0.30, cloth=CLOTH_RED)
basket_goods(-0.50, -0.44, 0.30, 0.060, "olives", seed=4)
basket_goods(-0.28, -0.44, 0.30, 0.060, "apples", seed=5)
basket_goods(-0.06, -0.44, 0.30, 0.060, "bread", seed=6)
loaf(0.14, -0.48, 0.306)
cheese_wheel(0.28, -0.40, 0.306)
cheese_wheel(0.28, -0.40, 0.338, r=0.034)
jug(0.44, -0.44, 0.306, 0.028)
# Boden: Körbe und Säcke, Amphore
basket_goods(-0.52, -0.80, 0.0, 0.080, "grain", seed=7)
basket_goods(-0.30, -0.84, 0.0, 0.070, "olives", seed=8)
sack(0.40, -0.78, 0.0, 0.075)
sack(0.58, -0.70, 0.0, 0.070)
crate(0.18, -0.82, 0.0, 0.14, yaw=8)
variant("Laden_1")

export(spacing=2.6)
