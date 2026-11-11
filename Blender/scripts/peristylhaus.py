"""
Peristylhaus der Gelehrtenzone (Antike), 3 x 3 Felder (6 x 6 m), zwei Varianten, helles Kalkstein- und Marmorweiß:
  Variante 0: zur Straße ein zweistöckiges Vorderhaus mit Giebelportal (zwei Säulen, Gebälk, Giebel), dahinter ein Ring
              aus drei einstöckigen Flügeln um einen Säulenhof: das Atriumdach (Grate, Gratziegel) fällt zur Öffnung ab,
              18 Säulen tragen die Traufe; im Hof ein Brunnenbecken mit Schale, vier Zierbeete, zwei Lorbeerbäume,
              Hermen und Pfade.
  Variante 1: große Halle mit sechs Säulen, Gebälk und Giebel mit Relief und Palmette zur Straße, in der Vorhalle zwei
              Rollenschränke neben der Bronzetür; dahinter die Gartenanlage: langes Wasserbecken mit zwei Schalen, vier
              Beete mit Hecken, Zypressen an den Mauern, Exedra mit Philosophenstatue, Hermen und Bänke.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/peristylhaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 5,52 x 5,52 m (Grundfläche 6 x 6 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Peristylhaus_0, Peristylhaus_1). Gartenmauern liegen mit der Außenfläche in der Ebene der Hauswand.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(54)

PM = PLASTER_MARBLE

# ================================================================ Variante 0: Vorderhaus und Ring um den Säulenhof
X = 2.64
YF, YM = -2.20, -1.40
YE = 2.60
HF = 1.95
Z_OUT, Z_IN = 1.35, 1.05
body(-X, X, YF, YM, HF, PM)
cornice(-X, X, YF, YM, 0.97)
cornice(-X, X, YF, YM, HF - 0.03)
gable_roof(-X, X, YF, YM, HF, "x", PM, overhang=0.06, pitch=22)
# Giebelportal
for s in (-1, 1):
    column(s * 0.50, YF - 0.20, 1.12, r=0.050)
entablature((-0.50, YF - 0.20), (0.50, YF - 0.20), 1.25, depth=0.17)
box((1.50, 0.40, 0.04), (0, YF - 0.20, 0.02), material=LIME_DARK)
box((0.95, 0.10, 0.025), (0, YF - 0.45, 0.0125), material=LIME_DARK)
gable_roof(-0.72, 0.72, YF - 0.36, YF + 0.02, 1.25, "y", PM, overhang=0.03, pitch=26)
classic_door("-y", YF, 0.0, w=0.46, h=0.84, pediment=False)
pilaster_row("-y", YF, [-0.66, 0.66], 1.18)
for u in (-1.00, 1.00, -1.60, 1.60, -2.20, 2.20):
    window("-y", YF, u, 0.42, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u > 0))
    window("-y", YF, u, 1.24, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u < 0))
marble_balcony(YF, -1.40, -0.70, 0.99, depth=0.15)
marble_balcony(YF, 0.70, 1.40, 0.99, depth=0.15)
window("-y", YF, 0.0, 1.30, w=0.22, h=0.34, shutters=None)
for z in (0.42, 1.24):
    window("-x", -X, (YF + YM) / 2, z, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=(z < 1))
    window("+x", X, (YF + YM) / 2, z, w=0.18, h=0.28, shutters=WOOD_DARK, open_shutters=(z > 1))
# Ring: Außenwände und Atriumdach
WH = Z_OUT - 0.06
flush_wall(-X, -X + 0.10, YM - 0.015, YE, WH, PM, cap=False)
flush_wall(X - 0.10, X, YM - 0.015, YE, WH, PM, cap=False)
flush_wall(-X, X, YE - 0.10, YE, WH, PM, cap=False)
box((5.10, 4.00, 0.03), (0, (YM + YE) / 2, 0.015), material=LIME_DARK)
OX, OY0, OY1 = 1.60, -0.45, 1.65
atrium_roof_fixed(-X - 0.03, X + 0.03, YM, YE + 0.03, -OX, OX, OY0, OY1, Z_OUT, Z_IN)
inner_wall_paint("-y", YE - 0.10, -X + 0.10, X - 0.10, WH, doors=(-1.6, -0.5, 0.5, 1.6))
inner_wall_paint("+x", -X + 0.10, YM, YE - 0.10, WH, doors=(0.2, 1.2))
inner_wall_paint("-x", X - 0.10, YM, YE - 0.10, WH, doors=(0.2, 1.2))
for k in range(6):
    x = -OX + k * 2 * OX / 5
    column(x, OY0, Z_IN - 0.17, r=0.042)
    column(x, OY1, Z_IN - 0.17, r=0.042)
for k in range(1, 4):
    y = OY0 + k * (OY1 - OY0) / 4
    column(-OX, y, Z_IN - 0.17, r=0.042)
    column(OX, y, Z_IN - 0.17, r=0.042)
# Hof
CY = (OY0 + OY1) / 2
cyl(0.46, 0.12, (0, CY, 0.06), material=LIME, verts=18)
cyl(0.39, 0.02, (0, CY, 0.115), material=WATER, verts=18)
fountain_bowl(0.0, CY, r=0.17)
for sx in (-1, 1):
    xa, xb = sorted((sx * 0.75, sx * 1.42))
    topiary_bed(xa, xb, OY0 + 0.12, CY - 0.45, BLOOM_WHITE if sx < 0 else BLOOM_YELLOW, LEAF, seed=20 + sx, spheres=2)
    topiary_bed(xa, xb, CY + 0.45, OY1 - 0.12, BLOOM_PINK if sx < 0 else BLOOM_RED, HERB_LIGHT, seed=24 + sx, spheres=2)
    laurel_tree(sx * 1.05, CY, 1.05)
    herm(sx * 0.35, OY1 - 0.18)
    herm(sx * 0.35, OY0 + 0.18)
path([(0.0, OY0 + 0.08), (0.0, CY - 0.5)], w=0.2)
path([(0.0, CY + 0.5), (0.0, OY1 - 0.08)], w=0.2)
# Außenseiten der Flügel
for u in (-0.60, 0.20, 1.00, 1.80):
    window("-x", -X, u, 0.45, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u > 0.5))
    window("+x", X, u, 0.45, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u < 0.5))
brick_bands(-X, X, YM, YE, (0.30, 0.92), faces=("-x", "+x", "+y"))
pilaster_row("-x", -X, [YM + 0.10, 0.60, YE - 0.10], WH - 0.1)
pilaster_row("+x", X, [YM + 0.10, 0.60, YE - 0.10], WH - 0.1)
classic_door("+y", YE, 0.0, w=0.46, h=0.80)
for u in (-0.95, 0.95, -1.60, 1.60, -2.20, 2.20):
    window("+y", YE, u, 0.45, w=0.22, h=0.30, shutters=None)
pilaster_row("+y", YE, [-2.54, -1.28, 1.28, 2.54], WH - 0.1)
bpy.context.view_layer.update()
check_bounds(2.76, 2.76, "0")
variant("Peristylhaus_0")

# ================================================================ Variante 1: Säulenhalle vorn, Gartenanlage hinten
X = 2.40
YC, YW, YH, H = -2.52, -2.04, -0.50, 1.55
body(-X, X, YW, YH, H, PM)
cornice(-X, X, YW, YH, H - 0.03)
ridge = gable_roof(-X, X, YC - 0.12, YH, H, "y", PM, overhang=0.05, pitch=16)
box((4.90, 0.62, 0.05), (0, -2.34, 0.025), material=LIME_DARK)                              # Vorhallenboden
box((3.80, 0.12, 0.03), (0, -2.68, 0.015), material=LIME_DARK)                               # Stufen
box((3.50, 0.12, 0.05), (0, -2.60, 0.025), material=LIME_DARK)
cols = [-2.15 + k * 0.86 for k in range(6)]
for x in cols:
    column(x, YC, H - 0.13, r=0.062)
entablature((-2.15, YC), (2.15, YC), H, depth=0.20)
for s in (-1, 1):
    column(s * 2.15, YC + 0.24, H - 0.13, r=0.062)
    entablature((s * 2.15, YC), (s * 2.15, YW - 0.02), H, depth=0.20)
pediment_front(0.0, YC - 0.12, H, X, ridge - H)
classic_door("-y", YW, 0.0, w=0.52, h=0.92)
for sx in (-1, 1):
    scroll_rack(sx * 0.98, YW - 0.12, rows=3, cols=2, w=0.44, h=0.70)
    window("-y", YW, sx * 1.65, 0.50, w=0.24, h=0.50, shutters=WOOD_DARK, open_shutters=(sx > 0))
    window("-y", YW, sx * 2.15, 0.50, w=0.20, h=0.50, shutters=None)
for u in (-1.80, -1.30, -0.80):
    window("-x", -X, u, 0.62, w=0.22, h=0.50, shutters=WOOD_DARK, open_shutters=False)
    window("+x", X, u, 0.62, w=0.22, h=0.50, shutters=WOOD_DARK, open_shutters=False)
brick_bands(-X, X, YW, YH, (0.30, 0.98), faces=("-x", "+x"))
pilaster_row("-x", -X, [YW + 0.12, YH - 0.12], H - 0.06)
pilaster_row("+x", X, [YW + 0.12, YH - 0.12], H - 0.06)
classic_door("+y", YH, 0.0, w=0.52, h=0.92)
for sx in (-1, 1):
    window("+y", YH, sx * 1.20, 0.50, w=0.24, h=0.50, shutters=None)
    window("+y", YH, sx * 1.90, 0.50, w=0.24, h=0.50, shutters=None)
oculus("+y", YH, 0.0, H + 0.40, 0.09)
pilaster_row("+y", YH, [-2.30, 2.30], H - 0.06)
# Garten
GH, GB = 0.45, 2.60
flush_wall(-X, -X + 0.09, YH - 0.015, GB, GH, PM)
flush_wall(X - 0.09, X, YH - 0.015, GB, GH, PM)
flush_wall(-X, X, GB - 0.09, GB, GH, PM)
for _s in (-1, 1):
    box((1.55, 2.35, 0.008), (_s * 1.40, 0.78, 0.004), material=LEAF)
pool(-0.55, 0.55, 0.10, 1.15, h=0.10)
fountain_bowl(0.0, 0.40, r=0.13)
fountain_bowl(0.0, 0.85, r=0.13)
path([(0.0, -0.44), (0.0, 0.02)], w=0.22)
exedra(0.0, 1.78, 0.62, 25, 155)
philosopher(0.0, 1.90)
path([(0.0, 1.22), (0.0, 1.45)], w=0.20)
for sx in (-1, 1):
    xa, xb = sorted((sx * 0.85, sx * 1.80))
    topiary_bed(xa, xb, -0.32, 0.38, BLOOM_WHITE if sx < 0 else BLOOM_PINK, LEAF, seed=30 + sx, spheres=3)
    topiary_bed(xa, xb, 0.55, 1.20, BLOOM_YELLOW if sx < 0 else BLOOM_RED, HERB_LIGHT, seed=34 + sx, spheres=3)
    topiary_bed(xa, xb, 1.40, 1.95, BLOOM_PINK if sx < 0 else BLOOM_WHITE, LEAF, seed=38 + sx, spheres=3)
    hedge(sx * 0.70 - 0.05, sx * 0.70 + 0.05, -0.30, 1.95, 0.20)
    light_pergola(*sorted((sx * 1.98, sx * 2.28)), 0.05, 1.60, h=0.70, vine_post=1)
    for y in (-0.15, 2.20):
        cypress(sx * 2.20, y, 1.30, 0.11)
    for y in (1.75, 2.15, 2.45):
        cypress(sx * 2.05, y, 1.25, 0.10) if y > 2.1 else None
    laurel_tree(sx * 1.40, 2.35, 1.0)
    herm(sx * 0.80, -0.30)
    bench(sx * 1.10, 2.45, yaw=0, length=0.42)
    statue(sx * 0.75, 1.30, h=0.45)
    pot_plant(sx * 0.62, -0.38, 0.05, LAUREL)
bpy.context.view_layer.update()
check_bounds(2.76, 2.76, "1")
variant("Peristylhaus_1")

export(spacing=6.4)
