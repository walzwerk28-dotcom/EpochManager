"""
Villa mit Garten der Gelehrtenzone (Antike), 2 x 3 Felder (4 x 6 m), zwei Varianten, helles Kalkstein- und Marmorweiß:
  Variante 0: vorn ein ummauerter Garten mit Torpfeilern, rundem Brunnenbecken mit Schale, vier Zierbeeten mit
              geschnittenen Hecken, Lorbeerbäumen, Zypressen, Hermen und Bänken; hinten der Steinbau mit sechs Säulen,
              Gebälk und Giebel mit Relief und Palmette (Satteldach, Giebel zum Garten), Ziegelbänder an den Seiten.
  Variante 1: zweistöckiges Haus an der Straße (Marmorbalkon), dahinter ein Säulenhof: links und rechts Säulengänge mit
              Pultdächern, in der Mitte ein langes Wasserbecken mit zwei Brunnenschalen, Zypressen und Lorbeer; hinten
              die Halle mit zwei Säulen und kleinem Giebel um die Tür.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/villa.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 5,52 m (Grundfläche 4 x 6 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Villa_0, Villa_1). Gartenmauern liegen mit der Außenfläche in der Ebene der Hauswand.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(53)

X = 1.72
PM = PLASTER_MARBLE
YB = 2.66

# ================================================================ Variante 0: Garten vorn, Säulenhalle hinten
YW, YC, H = 0.45, 0.0, 1.25
body(-X, X, YW, YB, H, PM)
cornice(-X, X, YW, YB, H - 0.03)
ridge = gable_roof(-X, X, YC - 0.12, YB, H, "y", PM, overhang=0.06, pitch=20)
box((3.44, 0.62, 0.045), (0, 0.16, 0.0225), material=LIME_DARK)                              # Vorhallenboden
box((2.30, 0.12, 0.025), (0, -0.20, 0.0125), material=LIME_DARK)                              # Stufen
box((2.00, 0.12, 0.045), (0, -0.14, 0.0225), material=LIME_DARK)
cols = [-1.55 + k * 0.62 for k in range(6)]
for x in cols:
    column(x, YC, H - 0.12, r=0.048)
entablature((-1.55, YC), (1.55, YC), H, depth=0.17)
for s in (-1, 1):
    column(s * 1.55, 0.225, H - 0.12, r=0.048)
    entablature((s * 1.55, YC), (s * 1.55, YW - 0.02), H, depth=0.17)
pediment_front(0.0, YC - 0.12, H, X, ridge - H)
classic_door("-y", YW, 0.0, w=0.42, h=0.78)
for u in (-0.93, 0.93, -1.40, 1.40):
    window("-y", YW, u, 0.42, w=0.20, h=0.36, shutters=WOOD_DARK, open_shutters=(u > 0))
statue(-0.62, 0.24, h=0.55)
statue(0.62, 0.24, h=0.55)
for u in (1.10, 1.75, 2.35):
    window("-x", -X, u, 0.42, w=0.20, h=0.34, shutters=WOOD_DARK, open_shutters=(u > 1.5))
    window("+x", X, u, 0.42, w=0.20, h=0.34, shutters=WOOD_DARK, open_shutters=(u < 1.5))
brick_bands(-X, X, YW, YB, (0.30, 0.86), faces=("-x", "+x", "+y"))
pilaster_row("-x", -X, [YW + 0.10, YB - 0.10], H - 0.05)
pilaster_row("+x", X, [YW + 0.10, YB - 0.10], H - 0.05)
classic_door("+y", YB, 0.0, w=0.42, h=0.78)
for u in (-0.90, 0.90, -1.40, 1.40):
    window("+y", YB, u, 0.42, w=0.20, h=0.34, shutters=None)
oculus("+y", YB, 0.0, H + 0.30, 0.09)
pilaster_row("+y", YB, [-1.65, -0.45, 0.45, 1.65], H - 0.05)
# Garten
FY = -2.64
flush_wall(-X, -X + 0.09, FY, YW - 0.015, 0.42, PM)
flush_wall(X - 0.09, X, FY, YW - 0.015, 0.42, PM)
flush_wall(-X, -0.46, FY, FY + 0.09, 0.42, PM)
flush_wall(0.46, X, FY, FY + 0.09, 0.42, PM)
gate_pier(-0.54, FY + 0.10)
gate_pier(0.54, FY + 0.10)
path([(0.0, -2.60), (0.0, -2.05)], w=0.22)
path([(0.0, -0.95), (0.0, -0.28)], w=0.22)
cyl(0.50, 0.12, (0, -1.50, 0.06), material=LIME, verts=18)
cyl(0.43, 0.02, (0, -1.50, 0.115), material=WATER, verts=18)
fountain_bowl(0.0, -1.50, r=0.17)
for sx in (-1, 1):
    xa, xb = sorted((sx * 0.72, sx * 1.58))
    topiary_bed(xa, xb, -2.50, -1.78, BLOOM_WHITE if sx < 0 else BLOOM_YELLOW, LEAF, seed=10 + sx, spheres=3)
    topiary_bed(xa, xb, -1.26, -0.54, BLOOM_PINK if sx < 0 else BLOOM_RED, HERB_LIGHT, seed=14 + sx, spheres=3)
    laurel_tree(sx * 1.15, -1.52, 1.05)
    cypress(sx * 0.55, -0.42, 1.30, 0.11)
    herm(sx * 0.90, -2.46)
    bench(sx * 1.58, -0.95, yaw=90, length=0.44)
    pot_plant(sx * 0.80, -2.60 + 0.02, 0.05, LAUREL)
amphora(-1.55, -2.55)
amphora(1.55, -2.55)
bpy.context.view_layer.update()
check_bounds(1.84, 2.76, "0")
variant("Villa_0")

# ================================================================ Variante 1: Haus vorn, Säulenhof, Halle hinten
YF, YM = -2.55, -1.55
HF = 1.90
HB = 1.45
YH = 1.50                                          # Vorderkante der hinteren Halle
Z_OUT, Z_IN = 1.30, 1.02
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
for u in (-2.20, -1.85):
    window("-x", -X, u, 0.40, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=False)
    window("+x", X, u, 0.40, w=0.18, h=0.28, shutters=WOOD_DARK)
    window("-x", -X, u, 1.22, w=0.18, h=0.28, shutters=WOOD_DARK)
    window("+x", X, u, 1.22, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=False)
classic_door("+y", YM, 0.0, w=0.34, h=0.64, leaf=WOOD_DARK, pediment=False)
for u in (-0.80, 0.80):
    window("+y", YM, u, 0.40, w=0.18, h=0.28, shutters=None)
    window("+y", YM, u, 1.22, w=0.18, h=0.28, shutters=None)
# Hof: Außenwände, Säulengänge mit Pultdächern
WH = Z_OUT - 0.01
flush_wall(-X, -X + 0.10, YM - 0.015, YB, WH, PM, cap=False)
flush_wall(X - 0.10, X, YM - 0.015, YB, WH, PM, cap=False)
box((3.30, 3.05, 0.03), (0, (YM + YH) / 2, 0.015), material=LIME_DARK)
for s in (-1, 1):
    pent_plane((s * (X + 0.03), YM), (s * (X + 0.03), YH), (s * 1.00, YM), (s * 1.00, YH), Z_OUT, Z_IN)
    for k in range(6):
        column(s * 1.10, -1.30 + k * 0.52, Z_IN - 0.17, r=0.040)
    entablature((s * 1.10, -1.45), (s * 1.10, YH - 0.05), Z_IN - 0.04, depth=0.15)
    for u in (-0.40, 0.45, 1.20):
        window("-x" if s < 0 else "+x", s * X, u, 0.45, w=0.18, h=0.30, shutters=WOOD_DARK, open_shutters=(u > 0))
    window("-x" if s < 0 else "+x", s * X, -1.00, 0.45, w=0.18, h=0.30, shutters=WOOD_DARK)
    window("-x" if s < 0 else "+x", s * X, 2.10, 0.45, w=0.18, h=0.34, shutters=WOOD_DARK)
    window("-x" if s < 0 else "+x", s * X, 2.50, 0.45, w=0.18, h=0.34, shutters=WOOD_DARK, open_shutters=False)
pool(-0.42, 0.42, -0.95, 0.80, h=0.10)
fountain_bowl(0.0, -0.45, r=0.12)
fountain_bowl(0.0, 0.30, r=0.12)
statue(0.0, 1.20, h=0.50)
cypress(-0.78, -1.30, 1.15, 0.10)
cypress(0.78, -1.30, 1.15, 0.10)
laurel_tree(-0.72, 0.0, 0.95)
laurel_tree(0.72, 0.0, 0.95)
bench(-0.80, 1.20, yaw=0, length=0.40)
bench(0.80, 1.20, yaw=0, length=0.40)
# Halle hinten mit Aedicula
body(-X, X, YH, YB, HB, PM)
cornice(-X, X, YH, YB, HB - 0.03)
gable_roof(-X, X, YH, YB, HB, "x", PM, overhang=0.06, pitch=22)
brick_bands(-X, X, YH, YB, (0.30, 0.86), faces=("-x", "+x", "+y"))
for x in (-0.55, 0.55):
    column(x, YH - 0.28, 0.98, r=0.045)
entablature((-0.55, YH - 0.28), (0.55, YH - 0.28), 1.10, depth=0.15)
gable_roof(-0.72, 0.72, YH - 0.36, YH, 1.10, "y", PM, overhang=0.03, pitch=26)
box((1.50, 0.40, 0.04), (0, YH - 0.18, 0.02), material=LIME_DARK)
classic_door("-y", YH, 0.0, w=0.40, h=0.70, pediment=False)
for u in (-1.10, 1.10):
    window("-y", YH, u, 0.45, w=0.22, h=0.36, shutters=WOOD_DARK, open_shutters=(u > 0))
pilaster_row("-y", YH, [-1.65, -1.40, 1.40, 1.65], HB - 0.06)
classic_door("+y", YB, 0.0, w=0.40, h=0.72)
for u in (-0.90, 0.90, -1.40, 1.40):
    window("+y", YB, u, 0.42, w=0.20, h=0.34, shutters=None)
pilaster_row("+y", YB, [-1.65, -0.45, 0.45, 1.65], HB - 0.06)
oculus("-x", -X, (YH + YB) / 2, HB + 0.22, 0.08)
oculus("+x", X, (YH + YB) / 2, HB + 0.22, 0.08)
bpy.context.view_layer.update()
check_bounds(1.84, 2.76, "1")
variant("Villa_1")

export(spacing=4.4)
