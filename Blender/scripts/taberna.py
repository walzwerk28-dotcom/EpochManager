"""
Taberna der Gewerbezone (Antike), 1 x 2 Felder (2 m Front, 4 m tief), zwei Varianten (wie die früheren Kenney-Modelle:
Variante 0 = kleiner Stand, Variante 1 = größere Halle):
  Variante 0: Imbiss (Thermopolium): einstöckiges Haus mit dem Giebel zur Straße, offene Front mit gemauerter Theke und
              eingelassenen Töpfen, Weinkrüge, Markise, Hocker und Bank; dahinter ein schmaler Hof mit Amphoren.
  Variante 1: zweistöckige Schänke mit dem Giebel zur Straße, Ladenöffnung mit Markise, Tür, Balkon mit Tür, Aushänge-
              schild; im Hof unter einer Weinlaube ein Tisch mit Bänken, Fass und Amphoren.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/taberna.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 1,84 x 3,68 m (Grundfläche 2 x 4 m), Vorderseite (Schmalseite) nach -Y. Die FBX enthält je
Variante ein Objekt (Taberna_0, Taberna_1).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(61)

# ---------------------------------------------------------------- Variante 0: Imbiss

X0, X1, Y0, Y1, H = -0.70, 0.70, -0.70, 0.62, STOREY
body(X0, X1, Y0, Y1, H, PLASTER_OCHRE, socle=SOCLE_RED)
gable_roof(X0, X1, Y0, Y1, H, "y", PLASTER_OCHRE, overhang=0.07)
# Öffnung links, Tür rechts
OU, OW, OH, OZ = -0.14, 0.86, 0.62, 0.09
box((OW, 0.012, OH), (OU, Y0 - 0.004, OZ + OH / 2), material=DARK)
box((OW + 0.12, 0.04, 0.07), (OU, Y0 - 0.015, OZ + OH + 0.03), material=WOOD_DARK)
for s in (-1, 1):
    box((0.05, 0.03, OH), (OU + s * (OW / 2 + 0.02), Y0 - 0.012, OZ + OH / 2), material=LIME_DARK)
door("-y", Y0, 0.54)
window("-y", Y0, 0.54, 0.86, w=0.12, h=0.10, shutters=None)
striped_canopy(OU - OW / 2 - 0.06, OU + OW / 2 + 0.06, Y0 - 0.42, Y0, 0.88, 0.76, CLOTH_RED, CLOTH_OCHRE)
# Thermopolium: Theke mit eingelassenen Töpfen
thermopolium(-0.52, 0.28, Y0 - 0.30, Y0 - 0.04, h=0.34, pots=3, open_mask=(1, 0, 1))
jug(-0.50, Y0 - 0.12, 0.34, 0.026)
jug(0.24, Y0 - 0.12, 0.34, 0.026)
bowl(-0.02, Y0 - 0.24, 0.34 + 0.0, 0.030)
bowl(0.05, Y0 - 0.12, 0.34, 0.026, material=TERRACOTTA_LIGHT)
for face, plane in (("-x", X0), ("+x", X1)):
    window(face, plane, -0.10, 0.42, shutters=PAINT_GREEN)
    window(face, plane, 0.30, 0.42, shutters=PAINT_GREEN, open_shutters=False)
door("+y", Y1, 0.30)
sign_board("-y", Y0, 0.54, 0.80, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=TERRACOTTA)
# Hocker und Bank vor dem Haus
stool(-0.34, Y0 - 0.55)
stool(-0.02, Y0 - 0.60)
bench(X1 + 0.12, -0.10, yaw=90, length=0.34)
amphora(0.58, Y0 - 0.20)
# schmaler Hof hinten
court_wall([(-0.66, Y1 + 0.02), (-0.66, 1.74), (0.66, 1.74), (0.66, Y1 + 0.02)], h=0.34, t=0.08, plaster=PLASTER_OCHRE)
path([(0.30, 0.74), (0.30, 1.30)])
for x, y in ((-0.46, 1.50), (-0.30, 1.58), (-0.48, 1.28)):
    amphora(x, y)
dolium(0.40, 1.52, h=0.30, r=0.13)
woodpile(0.05, 1.66, length=0.40, rows=(4, 3), yaw=0)
barrel(-0.20, 1.20, r=0.06)
pot_plant(0.52, 0.90, 0.05)
variant("Taberna_0")

# ---------------------------------------------------------------- Variante 1: zweistöckige Schänke

X0, X1, Y0, Y1, H = -0.74, 0.74, -1.15, 0.50, 2 * STOREY
body(X0, X1, Y0, Y1, H, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(X0, X1, Y0, Y1, H, "y", PLASTER_WHITE, overhang=0.07)
cornice(X0, X1, Y0, Y1, STOREY)
shop_front("-y", Y0, -0.26, w=0.72, goods=("jug", "amphora", "jug"), awning=(PAINT_BLUE, CLOTH))
door("-y", Y0, 0.46)
sign_board("-y", Y0, 0.46, 0.80, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=TERRACOTTA)
balcony(Y0, -0.62, 0.22, STOREY + 0.06, depth=0.20)
door("-y", Y0, -0.20, w=0.28, h=0.56, z0=STOREY + 0.06)
window("-y", Y0, -0.52 + 0.0, STOREY + 0.38, shutters=WOOD) if False else None
window("-y", Y0, 0.46, STOREY + 0.38, shutters=WOOD)
window("-y", Y0, 0.0, H + 0.08, w=0.12, h=0.10, shutters=None)
for face, plane in (("-x", X0), ("+x", X1)):
    for u in (-0.70, 0.10):
        window(face, plane, u, 0.42, shutters=WOOD, open_shutters=(u < 0))
        window(face, plane, u, STOREY + 0.38, shutters=WOOD, open_shutters=(u > -0.5))
door("+y", Y1, 0.30)
window("+y", Y1, -0.30, 0.42, shutters=WOOD)
window("+y", Y1, -0.30, STOREY + 0.38, shutters=WOOD, open_shutters=False)
window("+y", Y1, 0.30, STOREY + 0.38, shutters=WOOD)
# Hocker vor der Schänke
stool(-0.30, Y0 - 0.52)
stool(0.02, Y0 - 0.56)
amphora(0.62, Y0 - 0.20)
# Hof mit Weinlaube
court_wall([(-0.70, Y1 + 0.02), (-0.70, 1.74), (0.70, 1.74), (0.70, Y1 + 0.02)], h=0.34, t=0.08, plaster=PLASTER_WHITE)
light_pergola(-0.58, 0.58, 0.82, 1.50, h=0.62, vine_post=1)
trestle_table(0.0, 1.16, 0, w=0.70, d=0.24, h=0.28, cloth=CLOTH)
bench(0.0, 0.94, length=0.60)
bench(0.0, 1.38, length=0.60)
jug(-0.12, 1.16, 0.28, 0.026)
jug(0.14, 1.14, 0.28, 0.026, material=TERRACOTTA_LIGHT)
barrel(-0.50, 1.60, r=0.07)
barrel(-0.30, 1.62, r=0.06, lying=True, yaw=90)
jug(0.50, 1.62, 0.0, 0.05)
woodpile(0.15, 1.69, length=0.40, rows=(4, 3), yaw=0)
variant("Taberna_1")

export(spacing=2.4)
