"""
Stadtvilla der Gelehrtenzone (Antike), 2 x 2 Felder (4 x 4 m), zwei Varianten, helles Kalkstein- und Marmorweiß:
  Variante 0: breite Säulenvorhalle über die ganze Front (sechs Säulen, Gebälk, Pultdach aus Ziegeln, zwei Statuen
              zwischen den Säulen), dahinter der einstöckige Steinbau mit Bronzetür, Ziegelbändern und Satteldach.
  Variante 1: Ring um ein Atrium: zur Straße ein zweistöckiges Haus mit Marmorbalkon, dahinter drei einstöckige Flügel,
              deren Dächer (Atriumdach mit Graten) zu einer Öffnung abfallen; darunter Säulen, Wasserbecken und
              zwei Lorbeerbäume.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/stadtvilla.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 3,68 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Stadtvilla_0, Stadtvilla_1).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(52)

X = 1.72
PM = PLASTER_MARBLE

# ================================================================ Variante 0: Säulenvorhalle über die ganze Front
YW, YB, H = -0.90, 1.72, 1.30
body(-X, X, YW, YB, H, PM)
cornice(-X, X, YW, YB, H - 0.03)
ridge = gable_roof(-X, X, YW, YB, H, "x", PM, overhang=0.07, pitch=22)
oculus("-x", -X, (YW + YB) / 2, H + 0.22, 0.08)
oculus("+x", X, (YW + YB) / 2, H + 0.22, 0.08)
YC = -1.60                                       # Säulenreihe
CH = 0.80
cols = [-1.62 + k * 0.648 for k in range(6)]
box((3.50, 0.84, 0.04), (0, -1.28, 0.02), material=LIME_DARK)
box((1.50, 0.10, 0.03), (0, -1.76, 0.015), material=LIME_DARK)
for x in cols:
    column(x, YC, CH, r=0.040)
for s in (-1, 1):
    column(s * 1.62, -1.25, CH, r=0.040)
entablature((-1.62, YC), (1.62, YC), CH + 0.12, depth=0.15)
for s in (-1, 1):
    entablature((s * 1.62, YC), (s * 1.62, YW - 0.02), CH + 0.12, depth=0.15)
shed_roof(-X, X, YC - 0.06, YW, 1.158, "-y", overhang=0.06, pitch=16.0, eave_overhang=0.07)
classic_door("-y", YW, 0.0, w=0.40, h=0.72)
for u in (-0.65, 0.65):
    window("-y", YW, u, 0.42, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u > 0))
for u in (-1.30, 1.30):
    statue(u, -1.12, h=0.50)
for u in (-0.10, 0.60, 1.30):
    window("-x", -X, u, 0.42, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u > 0.5))
    window("+x", X, u, 0.42, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u < 0.5))
brick_bands(-X, X, YW, YB, (0.30, 0.86), faces=("-x", "+x", "+y"))
pilaster_row("-x", -X, [YW + 0.12, YB - 0.12], H - 0.05)
pilaster_row("+x", X, [YW + 0.12, YB - 0.12], H - 0.05)
classic_door("+y", YB, 0.0, w=0.40, h=0.72)
for u in (-0.85, 0.85, -1.40, 1.40):
    window("+y", YB, u, 0.42, w=0.20, h=0.30, shutters=None)
pilaster_row("+y", YB, [-1.64, -0.45, 0.45, 1.64], H - 0.05)
pot_plant(-1.62, -1.75, 0.06, LAUREL)
pot_plant(1.62, -1.75, 0.06, LAUREL)
bpy.context.view_layer.update()
check_bounds(1.84, 1.84, "0")
variant("Stadtvilla_0")

# ================================================================ Variante 1: Ring um ein Atrium
YF, YM = -1.60, -0.80
HF = 1.90
YE = 1.72
Z_OUT, Z_IN = 1.30, 1.08
body(-X, X, YF, YM, HF, PM)
cornice(-X, X, YF, YM, 0.94)
cornice(-X, X, YF, YM, HF - 0.03)
gable_roof(-X, X, YF, YM, HF, "x", PM, overhang=0.06, pitch=22)
classic_door("-y", YF, 0.0, w=0.40, h=0.72)
for u in (-0.80, 0.80, -1.35, 1.35):
    window("-y", YF, u, 0.40, w=0.20, h=0.28, shutters=WOOD_DARK, open_shutters=(u > 0))
    window("-y", YF, u, 1.22, w=0.20, h=0.28, shutters=WOOD_DARK, open_shutters=(u < 0))
marble_balcony(YF, -0.40, 0.40, 0.97, depth=0.16)
door("-y", YF, 0.0, w=0.30, h=0.54, z0=1.00, leaf=WOOD_DARK, frame=LIME)
for u in (-1.10, -0.95):
    window("-x", -X, u, 0.40, w=0.18, h=0.28, shutters=WOOD_DARK)
    window("+x", X, u, 0.40, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=False)
    window("-x", -X, u, 1.22, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=False)
    window("+x", X, u, 1.22, w=0.18, h=0.28, shutters=WOOD_DARK)
# Außenwände des Rings
WH = Z_OUT - 0.06
flush_wall(-X, -X + 0.10, YM - 0.015, YE, WH, PM, cap=False)
flush_wall(X - 0.10, X, YM - 0.015, YE, WH, PM, cap=False)
flush_wall(-X, X, YE - 0.10, YE, WH, PM, cap=False)
box((3.30, 2.40, 0.03), (0, 0.48, 0.015), material=LIME_DARK)
atrium_roof_fixed(-X - 0.03, X + 0.03, YM, YE + 0.03, -1.05, 1.05, -0.05, 1.25, Z_OUT, Z_IN)
inner_wall_paint("-y", YE - 0.10, -X + 0.10, X - 0.10, WH, doors=(-0.9, 0.0, 0.9))
inner_wall_paint("+x", -X + 0.10, YM, YE - 0.10, WH, doors=(0.1, 1.2))
inner_wall_paint("-x", X - 0.10, YM, YE - 0.10, WH, doors=(0.1, 1.2))
for x, y in ((-1.05, -0.05), (1.05, -0.05), (-1.05, 1.25), (1.05, 1.25), (-1.05, 0.60), (1.05, 0.60), (0.0, -0.05), (0.0, 1.25)):
    column(x, y, Z_IN - 0.04, r=0.035)
pool(-0.45, 0.45, 0.35, 0.85, h=0.10)
fountain_bowl(0.0, 0.60, r=0.10)
laurel_tree(-0.78, 0.60, 0.9)
laurel_tree(0.78, 0.60, 0.9)
# Fassade der Flügel
for u in (-0.30, 0.45, 1.20):
    window("-x", -X, u, 0.42, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=(u > 0))
    window("+x", X, u, 0.42, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=(u < 0))
brick_bands(-X, X, YM, YE, (0.30, 0.90), faces=("-x", "+x", "+y"))
classic_door("+y", YE, 0.0, w=0.40, h=0.72)
for u in (-0.85, 0.85, -1.40, 1.40):
    window("+y", YE, u, 0.42, w=0.20, h=0.28, shutters=None)
pilaster_row("+y", YE, [-1.64, -0.45, 0.45, 1.64], WH - 0.1)
bpy.context.view_layer.update()
check_bounds(1.84, 1.84, "1")
variant("Stadtvilla_1")

export(spacing=4.4)
