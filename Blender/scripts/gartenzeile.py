"""
Gartenzeile ("Hausreihe mit Gärten") der Gelehrtenzone (Antike), 2 x 4 Felder (4 x 8 m), zwei Varianten, helles
Kalkstein- und Marmorweiß:
  Variante 0: vornehmes zweistöckiges Vorderhaus (Bronzetür, Marmorbalkon), dahinter ein langer Garten mit Weg, Wasser-
              becken, Säulenreihen mit Gebälk an den Seiten, Zierbeeten, Lorbeer und Zypressen; hinten ein einstöckiges
              Hinterhaus mit Säulenvorbau und kleinem Giebel.
  Variante 1: zwei schmale Häuser mit Giebel zur Straße (links zweistöckig mit Balkon, rechts einstöckig), getrennt
              durch eine Mauer, dahinter zwei Gärten: links Zierbeete, Lorbeer, Herme, Bank; rechts Wasserbecken mit
              Schale, Olivenbaum, Statue, Zypressen.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/gartenzeile.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 7,36 m (Grundfläche 4 x 8 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Gartenzeile_0, Gartenzeile_1). Gartenmauern liegen mit der Außenfläche in der Ebene der Hauswand.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(56)

X = 1.72
PM = PLASTER_MARBLE

# ================================================================ Variante 0: Vorderhaus, langer Garten, Hinterhaus
YF, YM = -3.48, -2.48
HF = 1.90
YH, YB, HB = 1.60, 3.50, 1.25
body(-X, X, YF, YM, HF, PM)
cornice(-X, X, YF, YM, 0.95)
cornice(-X, X, YF, YM, HF - 0.03)
gable_roof(-X, X, YF, YM, HF, "x", PM, overhang=0.06, pitch=22)
classic_door("-y", YF, 0.0, w=0.40, h=0.72)
for u in (-0.80, 0.80, -1.35, 1.35):
    window("-y", YF, u, 0.40, w=0.20, h=0.28, shutters=WOOD_DARK, open_shutters=(u > 0))
    window("-y", YF, u, 1.22, w=0.20, h=0.28, shutters=WOOD_DARK, open_shutters=(u < 0))
marble_balcony(YF, -0.40, 0.40, 0.97, depth=0.16)
door("-y", YF, 0.0, w=0.30, h=0.54, z0=1.00, leaf=WOOD_DARK, frame=LIME)
for u in (-3.10, -2.80):
    window("-x", -X, u, 0.40, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=False)
    window("+x", X, u, 0.40, w=0.18, h=0.28, shutters=WOOD_DARK)
    window("-x", -X, u, 1.22, w=0.18, h=0.28, shutters=WOOD_DARK)
    window("+x", X, u, 1.22, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=False)
classic_door("+y", YM, 0.0, w=0.34, h=0.64, leaf=WOOD_DARK, pediment=False)
for u in (-0.80, 0.80):
    window("+y", YM, u, 0.40, w=0.18, h=0.28, shutters=None)
    window("+y", YM, u, 1.22, w=0.18, h=0.28, shutters=None)
# Garten
flush_wall(-X, -X + 0.09, YM - 0.015, YH, 0.45, PM)
flush_wall(X - 0.09, X, YM - 0.015, YH, 0.45, PM)
path([(0.0, -2.40), (0.0, -1.70)], w=0.20)
path([(0.0, -0.30), (0.0, 0.40)], w=0.20)
path([(0.0, 1.20), (0.0, 1.45)], w=0.20)
box((1.45, 4.0, 0.008), (0.0, -0.40, 0.004), material=LEAF)
pool(-0.35, 0.35, -1.60, -0.40, h=0.10)
fountain_bowl(0.0, -1.00, r=0.12)
for s in (-1, 1):
    for k in range(5):
        column(s * 1.28, -2.05 + k * 0.80, 0.78, r=0.038)
    entablature((s * 1.28, -2.20), (s * 1.28, 1.20), 0.90, depth=0.14)
    xa, xb = sorted((s * 0.55, s * 1.05))
    topiary_bed(xa, xb, -2.15, -1.55, BLOOM_WHITE if s < 0 else BLOOM_PINK, LEAF, seed=70 + s, spheres=2)
    topiary_bed(xa, xb, 0.05, 0.85, BLOOM_YELLOW if s < 0 else BLOOM_RED, HERB_LIGHT, seed=74 + s, spheres=2)
    laurel_tree(s * 0.80, -0.30, 1.0)
    cypress(s * 1.50, 1.30, 1.2, 0.10)
    cypress(s * 1.50, -2.30, 1.2, 0.10)
    herm(s * 0.50, 1.25)
bench(-0.80, -0.85, yaw=90, length=0.40)
bench(0.80, -0.85, yaw=90, length=0.40)
statue(0.0, 0.70, h=0.45)
# Hinterhaus mit Säulenvorbau
body(-X, X, YH, YB, HB, PM)
cornice(-X, X, YH, YB, HB - 0.03)
gable_roof(-X, X, YH, YB, HB, "y", PM, overhang=0.06, pitch=22)
for x in (-0.55, 0.55):
    column(x, YH - 0.26, 0.98, r=0.045)
entablature((-0.55, YH - 0.26), (0.55, YH - 0.26), 1.10, depth=0.15)
gable_roof(-0.72, 0.72, YH - 0.34, YH, 1.10, "y", PM, overhang=0.03, pitch=26)
box((1.50, 0.38, 0.04), (0, YH - 0.17, 0.02), material=LIME_DARK)
classic_door("-y", YH, 0.0, w=0.40, h=0.70, pediment=False)
for u in (-1.10, 1.10):
    window("-y", YH, u, 0.45, w=0.22, h=0.34, shutters=WOOD_DARK, open_shutters=(u > 0))
for u in (2.10, 2.70, 3.20):
    window("-x", -X, u, 0.45, w=0.20, h=0.32, shutters=WOOD_DARK, open_shutters=(u > 2.5))
    window("+x", X, u, 0.45, w=0.20, h=0.32, shutters=WOOD_DARK, open_shutters=(u < 2.5))
brick_bands(-X, X, YH, YB, (0.30, 0.86), faces=("-x", "+x", "+y"))
pilaster_row("-x", -X, [YH + 0.10, YB - 0.10], HB - 0.06)
pilaster_row("+x", X, [YH + 0.10, YB - 0.10], HB - 0.06)
classic_door("+y", YB, 0.0, w=0.40, h=0.72)
for u in (-0.85, 0.85, -1.40, 1.40):
    window("+y", YB, u, 0.42, w=0.20, h=0.32, shutters=None)
pilaster_row("+y", YB, [-1.64, -0.45, 0.45, 1.64], HB - 0.06)
oculus("-x", -X, (YH + YB) / 2, HB + 0.22, 0.08)
oculus("+x", X, (YH + YB) / 2, HB + 0.22, 0.08)
bpy.context.view_layer.update()
check_bounds(1.84, 3.68, "0")
variant("Gartenzeile_0")

# ================================================================ Variante 1: zwei schmale Häuser, zwei Gärten
YF, YM = -3.48, -2.30
GB = 3.52
brick = None
for s, hh, label in ((-1, 1.85, "L"), (1, 1.40, "R")):
    x0, x1 = (-X, -0.07) if s < 0 else (0.07, X)
    xc = (x0 + x1) / 2
    body(x0, x1, YF, YM, hh, PM)
    cornice(x0, x1, YF, YM, hh - 0.03)
    gable_roof(x0, x1, YF, YM, hh, "y", PM, overhang=0.05, pitch=24)
    classic_door("-y", YF, xc, w=0.36, h=0.68)
    window("-y", YF, xc - 0.55, 0.40, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=(s > 0))
    window("-y", YF, xc + 0.55, 0.40, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=(s < 0))
    if s < 0:
        cornice(x0, x1, YF, YM, 0.93)
        marble_balcony(YF, xc - 0.38, xc + 0.38, 0.95, depth=0.15)
        door("-y", YF, xc, w=0.28, h=0.52, z0=0.98, leaf=WOOD_DARK, frame=LIME)
        window("-y", YF, xc - 0.55, 1.20, w=0.18, h=0.28, shutters=WOOD_DARK)
        window("-y", YF, xc + 0.55, 1.20, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=False)
    else:
        oculus("-y", YF, xc, hh + 0.18, 0.07)
    f = "-x" if s < 0 else "+x"
    for u in (-3.10, -2.70):
        window(f, x0 if s < 0 else x1, u, 0.40, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=(u > -2.9))
        if s < 0:
            window(f, x0, u, 1.20, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=False)
    classic_door("+y", YM, xc, w=0.32, h=0.60, leaf=WOOD_DARK, pediment=False)
flush_wall(-0.07, 0.07, YF + 0.02, GB, 0.55, PM)
flush_wall(-X, -X + 0.09, YM - 0.015, GB, 0.45, PM)
flush_wall(X - 0.09, X, YM - 0.015, GB, 0.45, PM)
flush_wall(-X, X, GB - 0.09, GB, 0.70, PM)
brick_bands(-X, X, YM, GB, (0.30,), faces=("-x", "+x", "+y"))
box((1.50, 5.0, 0.008), (-0.80, 0.50, 0.004), material=LEAF)
box((1.50, 5.0, 0.008), (0.80, 0.50, 0.004), material=LEAF)
# linker Garten
topiary_bed(-1.58, -0.85, -2.05, -1.35, BLOOM_WHITE, LEAF, seed=81, spheres=2)
topiary_bed(-1.58, -0.85, -0.85, -0.15, BLOOM_PINK, HERB_LIGHT, seed=82, spheres=2)
topiary_bed(-1.58, -0.85, 0.35, 1.05, BLOOM_YELLOW, LEAF, seed=83, spheres=2)
topiary_bed(-1.58, -0.85, 1.35, 1.95, BLOOM_RED, HERB_LIGHT, seed=85, spheres=2)
path([(-0.45, -2.20), (-0.45, 2.40)], w=0.22)
hedge(-0.27, -0.17, -2.15, 2.30, 0.22)
laurel_tree(-1.20, 2.30, 1.1)
herm(-0.35, -1.30)
herm(-0.35, 0.50)
light_pergola(-1.60, -0.30, 2.62, 3.36, h=0.72, vine_post=1)
bench(-0.95, 3.12, yaw=0, length=0.46)
cypress(-1.55, 2.55, 1.2, 0.10)
cypress(-1.55, -2.30, 1.2, 0.10)
cypress(-1.55, 0.00, 1.2, 0.10)
pot_plant(-0.80, -2.10, 0.05, LAUREL)
# rechter Garten
pool(0.50, 1.35, -1.60, -0.55, h=0.10)
fountain_bowl(0.925, -1.075, r=0.12)
path([(0.45, -2.20), (0.45, -1.70)], w=0.22)
path([(0.45, -0.45), (0.45, 2.40)], w=0.22)
hedge(0.17, 0.27, -2.15, 2.30, 0.22)
olive_tree(1.15, 0.30, 1.2)
statue(1.20, 1.45, h=0.45)
topiary_bed(0.95, 1.58, -2.15, -1.70, BLOOM_RED, LEAF, seed=84, spheres=2)
topiary_bed(0.75, 1.20, 0.85, 1.20, BLOOM_WHITE, HERB_LIGHT, seed=86, spheres=2) if False else None
topiary_bed(0.80, 1.58, 1.90, 2.40, BLOOM_PINK, LEAF, seed=87, spheres=3)
light_pergola(0.30, 1.60, 2.62, 3.36, h=0.72, vine_post=1)
bench(0.95, 3.12, yaw=0, length=0.46)
cypress(1.55, 2.55, 1.2, 0.10)
cypress(1.55, -0.55, 1.2, 0.10)
cypress(1.55, 0.95, 1.2, 0.10)
amphora(1.50, -0.25)
bpy.context.view_layer.update()
check_bounds(1.84, 3.68, "1")
variant("Gartenzeile_1")

export(spacing=4.4)
