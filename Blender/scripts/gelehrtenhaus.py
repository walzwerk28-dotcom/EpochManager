"""
Gelehrtenhaus der Gelehrtenzone (Antike), 1 x 2 Felder (2 x 4 m), zwei Varianten, helles Kalkstein- und Marmorweiß:
  Variante 0: einstöckiger Steinbau mit Säulenvorhalle (vier Säulen, Gebälk, Giebel mit Relief und Palmette) zur
              Straße, Bronzetür; dahinter ein ummauerter Garten mit Lorbeerbaum, Zierbeeten, Herme, Zypressen und Bank.
  Variante 1: zweistöckiges Haus an der Straße (Marmorbalkon mit Balustrade, Bronzetür), dahinter ein Gartenhof mit
              Wasserbecken und Brunnenschale, Hecken, Statue, Zypressen, Lorbeer und Bank.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/gelehrtenhaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 1,84 x 3,68 m (Grundfläche 2 x 4 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Gelehrtenhaus_0, Gelehrtenhaus_1). Gartenmauern liegen mit der Außenfläche in der Ebene der Hauswand.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(51)

X = 0.78          # Außenfläche der Wände
GY = 1.78         # Rückseite des Gartens
PM = PLASTER_MARBLE


def garden_walls(y0, h=0.42):
    flush_wall(-X, -X + 0.09, y0, GY, h, PM)
    flush_wall(X - 0.09, X, y0, GY, h, PM)
    flush_wall(-X, X, GY - 0.09, GY, h, PM)


# ================================================================ Variante 0: Haus mit Säulenvorhalle, Garten dahinter
YF, YW, YB, H = -1.62, -1.30, -0.15, 0.95
body(-X, X, YW, YB, H, PM)
cornice(-X, X, YW, YB, H - 0.03)
ridge = gable_roof(-X, X, YF - 0.12, YB, H, "y", PM, overhang=0.05, pitch=22)
box((1.62, 0.50, 0.04), (0, -1.55, 0.02), material=LIME_DARK)                                   # Vorhallenboden
box((1.0, 0.10, 0.025), (0, -1.76, 0.0125), material=LIME_DARK)                                  # Stufe
cols = [-0.70, -0.233, 0.233, 0.70]
for x in cols:
    column(x, YF, H - 0.13, r=0.040)
entablature((-0.70, YF), (0.70, YF), H, depth=0.15)
for s in (-1, 1):
    entablature((s * 0.70, YF), (s * 0.70, YW + 0.02), H, depth=0.15)
classic_door("-y", YW, 0.0, w=0.32, h=0.62)
for u in (-0.52, 0.52):
    window("-y", YW, u, 0.40, w=0.18, h=0.24, shutters=WOOD_DARK, frame=LIME)
pediment_front(0.0, YF - 0.12, H, X, ridge - H)
for u in (-0.95, -0.50):
    window("-x", -X, u, 0.40, w=0.18, h=0.24, shutters=WOOD_DARK, open_shutters=False)
    window("+x", X, u, 0.40, w=0.18, h=0.24, shutters=WOOD_DARK, open_shutters=False)
pilaster_row("-x", -X, [-1.25, -0.20], H - 0.05)
pilaster_row("+x", X, [-1.25, -0.20], H - 0.05)
classic_door("+y", YB, 0.0, w=0.30, h=0.60, leaf=WOOD_DARK, pediment=False)
for u in (-0.48, 0.48):
    window("+y", YB, u, 0.40, w=0.18, h=0.24, shutters=None)
# Garten
garden_walls(YB - 0.015)
path([(0.0, 0.05), (0.0, 0.50), (0.0, 1.20)], w=0.17)
topiary_bed(-0.66, -0.20, 0.40, 0.88, BLOOM_WHITE, LEAF, seed=3)
topiary_bed(0.20, 0.66, 0.40, 0.88, BLOOM_PINK, HERB_LIGHT, seed=4)
laurel_tree(-0.40, 1.22, 1.25)
herm(0.44, 1.20)
bench(0.0, 1.57, length=0.50)
cypress(-0.56, 1.52, 1.15, 0.10)
cypress(0.56, 1.52, 1.15, 0.10)
pot_plant(-0.28, 1.57, 0.05, LAUREL)
pot_plant(0.28, 1.57, 0.05, LAUREL)
amphora(0.60, 0.00)
bpy.context.view_layer.update()
check_bounds(0.92, 1.84, "0")
variant("Gelehrtenhaus_0")

# ================================================================ Variante 1: zweistöckiges Haus, Gartenhof mit Brunnen
YF, YB = -1.62, -0.62
H2 = 1.90
body(-X, X, YF, YB, H2, PM)
cornice(-X, X, YF, YB, 0.95)
cornice(-X, X, YF, YB, H2 - 0.03)
gable_roof(-X, X, YF, YB, H2, "x", PM, overhang=0.06, pitch=22)
classic_door("-y", YF, 0.0, w=0.32, h=0.62)
for u in (-0.52, 0.52):
    window("-y", YF, u, 0.40, w=0.18, h=0.24, shutters=WOOD_DARK, open_shutters=(u > 0))
    window("-y", YF, u, 1.22, w=0.18, h=0.24, shutters=WOOD_DARK, open_shutters=(u < 0))
marble_balcony(YF, -0.30, 0.30, 0.97)
door("-y", YF, 0.0, w=0.26, h=0.52, z0=1.00, leaf=WOOD_DARK, frame=LIME)
for u in (-1.40, -0.95):
    window("-x", -X, u, 0.40, w=0.18, h=0.24, shutters=WOOD_DARK, open_shutters=False)
    window("-x", -X, u, 1.22, w=0.18, h=0.24, shutters=WOOD_DARK, open_shutters=True)
    window("+x", X, u, 0.40, w=0.18, h=0.24, shutters=WOOD_DARK, open_shutters=True)
    window("+x", X, u, 1.22, w=0.18, h=0.24, shutters=WOOD_DARK, open_shutters=False)
classic_door("+y", YB, 0.0, w=0.30, h=0.60, leaf=WOOD_DARK, pediment=False)
for u in (-0.48, 0.48):
    window("+y", YB, u, 0.40, w=0.18, h=0.24, shutters=None)
    window("+y", YB, u, 1.22, w=0.18, h=0.24, shutters=WOOD_DARK, open_shutters=False)
# Gartenhof
garden_walls(YB - 0.015)
pool(-0.34, 0.34, 0.28, 0.90)
fountain_bowl(0.0, 0.59, r=0.13)
path([(0.0, -0.52), (0.0, 0.16)], w=0.17)
path([(0.0, 1.02), (0.0, 1.30)], w=0.17)
hedge(-0.69, -0.58, -0.30, 1.35, 0.20)
hedge(0.58, 0.69, -0.30, 1.35, 0.20)
statue(0.0, 1.40, h=0.42)
bench(0.0, 1.60, length=0.50)
laurel_tree(-0.40, 0.95, 0.85)
laurel_tree(0.40, 0.95, 0.85)
cypress(-0.54, 1.52, 1.15, 0.10)
cypress(0.54, 1.52, 1.15, 0.10)
herm(-0.40, -0.15)
herm(0.40, -0.15)
bpy.context.view_layer.update()
check_bounds(0.92, 1.84, "1")
variant("Gelehrtenhaus_1")

export(spacing=3.0)
