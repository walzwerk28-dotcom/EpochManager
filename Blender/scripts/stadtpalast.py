"""
Stadtpalast der Gelehrtenzone (Antike), 4 x 4 Felder (8 x 8 m), zwei Varianten, helles Kalkstein- und Marmorweiß:
  Variante 0: Palast mit Giebelportal zur Straße (zweistöckiges Vorderhaus, Portal mit zwei Säulen und Giebel,
              Marmorbalkone), dahinter das Atrium mit Atriumdach, Wasserbecken und Säulen zwischen zwei niedrigen
              Seitenflügeln, hinten das große Peristyl: Säulengang rundum unter Pultdächern mit Gehrung, langes
              Wasserbecken mit drei Schalen, Zierbeete, Lorbeer, Statuen und Hermen.
  Variante 1: vorn die Bibliothekshalle mit acht Säulen, Gebälk und Giebel mit Relief, in der Vorhalle vier
              Rollenschränke neben der Bronzetür; dahinter ein Terrassengarten auf drei Ebenen mit Treppen,
              Balustraden, Wasserbecken, Beeten, Zypressenreihen, Statuen und einer Exedra mit Philosophenstatue.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/stadtpalast.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 7,36 x 7,36 m (Grundfläche 8 x 8 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Stadtpalast_0, Stadtpalast_1). Gartenmauern liegen mit der Außenfläche in der Ebene der Hauswand.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(55)

PM = PLASTER_MARBLE


def lift(z, fn):
    """Baut mit fn() Teile und hebt sie um z (Dinge auf einer Terrasse)."""
    n = len(PARTS)
    fn()
    for o in PARTS[n:]:
        o.location.z += z


# ================================================================ Variante 0: Portal, Atrium, Peristyl
X = 3.56
YF, YM = -3.10, -2.30
YA = -0.70                     # Rückseite des Atriums
YE = 3.54
HF = 1.90
Z_OUT, Z_IN = 1.30, 1.05
WH = Z_OUT - 0.06
body(-X, X, YF, YM, HF, PM)
cornice(-X, X, YF, YM, 0.95)
cornice(-X, X, YF, YM, HF - 0.03)
gable_roof(-X, X, YF, YM, HF, "x", PM, overhang=0.06, pitch=22)
for s in (-1, 1):                                              # Giebelportal
    column(s * 0.60, YF - 0.22, 1.12, r=0.052)
entablature((-0.60, YF - 0.22), (0.60, YF - 0.22), 1.25, depth=0.17)
box((1.70, 0.40, 0.04), (0, YF - 0.22, 0.02), material=LIME_DARK)
box((1.10, 0.10, 0.025), (0, YF - 0.47, 0.0125), material=LIME_DARK)
gable_roof(-0.82, 0.82, YF - 0.38, YF + 0.02, 1.25, "y", PM, overhang=0.03, pitch=26)
classic_door("-y", YF, 0.0, w=0.50, h=0.88, pediment=False)
pilaster_row("-y", YF, [-0.78, 0.78], 1.18)
for u in (-1.30, 1.30, -2.00, 2.00, -2.70, 2.70, -3.30, 3.30):
    window("-y", YF, u, 0.42, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u > 0))
    window("-y", YF, u, 1.24, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u < 0))
marble_balcony(YF, -2.35, -1.65, 0.99, depth=0.15)
marble_balcony(YF, 1.65, 2.35, 0.99, depth=0.15)
for z in (0.42, 1.24):
    window("-x", -X, (YF + YM) / 2, z, w=0.18, h=0.28, shutters=WOOD_DARK)
    window("+x", X, (YF + YM) / 2, z, w=0.18, h=0.28, shutters=WOOD_DARK)
# Atrium mit Seitenflügeln
AX = 1.90
flush_wall(-AX, -AX + 0.10, YM - 0.015, YA, WH, PM, cap=False)
flush_wall(AX - 0.10, AX, YM - 0.015, YA, WH, PM, cap=False)
box((3.70, 1.90, 0.03), (0, (YM + YA) / 2, 0.015), material=LIME_DARK)
atrium_roof_fixed(-AX - 0.02, AX + 0.02, YM, YA, -1.15, 1.15, -1.95, -1.15, Z_OUT, Z_IN)
for x, y in ((-1.15, -1.95), (1.15, -1.95), (-1.15, -1.15), (1.15, -1.15), (0.0, -1.95), (0.0, -1.15),
             (-1.15, -1.55), (1.15, -1.55)):
    column(x, y, Z_IN - 0.04, r=0.035)
pool(-0.55, 0.55, -1.80, -1.30, h=0.10)
fountain_bowl(0.0, -1.55, r=0.10)
for s in (-1, 1):                                              # Seitenflügel
    x0, x1 = sorted((s * AX, s * X))
    body(x0, x1, YM, YA, 1.25, PM)
    cornice(x0, x1, YM, YA, 1.22)
    gable_roof(x0, x1, YM, YA, 1.25, "y", PM, overhang=0.05, pitch=22)
    f = "-x" if s < 0 else "+x"
    for u in (-2.20, -1.60, -1.00):
        window(f, s * X, u, 0.42, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u > -1.6))
    pilaster_row(f, s * X, [YM + 0.10, YA - 0.10], 1.18)
    oculus("-y", YM, 0.0, 1.0, 0.07) if False else None
brick_bands(-X, X, YM, YA, (0.30, 0.86), faces=("-x", "+x"))
# Peristyl
flush_wall(-X, -X + 0.10, YA - 0.015, YE, WH, PM, cap=False)
flush_wall(X - 0.10, X, YA - 0.015, YE, WH, PM, cap=False)
flush_wall(-X, X, YE - 0.10, YE, WH, PM, cap=False)
box((7.0, 4.2, 0.03), (0, (YA + YE) / 2, 0.015), material=LIME_DARK)
OX, OY0, OY1 = 2.70, 0.05, 2.80
atrium_roof_fixed(-X - 0.03, X + 0.03, YA, YE + 0.03, -OX, OX, OY0, OY1, Z_OUT, Z_IN)
inner_wall_paint("-y", YE - 0.10, -X + 0.10, X - 0.10, WH, doors=(-2.2, -1.1, 0.0, 1.1, 2.2))
inner_wall_paint("+x", -X + 0.10, YA, YE - 0.10, WH, doors=(0.8, 2.0))
inner_wall_paint("-x", X - 0.10, YA, YE - 0.10, WH, doors=(0.8, 2.0))
for k in range(11):
    x = -OX + k * 2 * OX / 10
    column(x, OY0, Z_IN - 0.17, r=0.040)
    column(x, OY1, Z_IN - 0.17, r=0.040)
for k in range(1, 5):
    y = OY0 + k * (OY1 - OY0) / 5
    column(-OX, y, Z_IN - 0.17, r=0.040)
    column(OX, y, Z_IN - 0.17, r=0.040)
CY = (OY0 + OY1) / 2
pool(-1.55, 1.55, CY - 0.55, CY + 0.55, h=0.10)
for x in (-0.85, 0.0, 0.85):
    fountain_bowl(x, CY, r=0.14)
path([(0.0, OY0 + 0.1), (0.0, CY - 0.62)], w=0.22)
path([(0.0, CY + 0.62), (0.0, OY1 - 0.1)], w=0.22)
for s in (-1, 1):
    xa, xb = sorted((s * 1.85, s * 2.55))
    topiary_bed(xa, xb, OY0 + 0.15, CY - 0.15, BLOOM_WHITE if s < 0 else BLOOM_YELLOW, LEAF, seed=40 + s, spheres=2)
    topiary_bed(xa, xb, CY + 0.15, OY1 - 0.15, BLOOM_PINK if s < 0 else BLOOM_RED, HERB_LIGHT, seed=44 + s, spheres=2)
    laurel_tree(s * 0.80, OY0 + 0.45, 1.0)
    laurel_tree(s * 0.80, OY1 - 0.45, 1.0)
    statue(s * 1.85, CY, h=0.50) if False else None
    herm(s * 1.35, OY0 + 0.25)
    herm(s * 1.35, OY1 - 0.25)
    bench(s * 1.65, CY - 0.8 if s < 0 else CY + 0.8, yaw=0, length=0.4) if False else None
statue(-1.75, CY, h=0.50)
statue(1.75, CY, h=0.50)
# Außenseiten Peristyl
for u in (0.30, 1.10, 1.90, 2.70):
    window("-x", -X, u, 0.45, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u > 1))
    window("+x", X, u, 0.45, w=0.20, h=0.30, shutters=WOOD_DARK, open_shutters=(u < 1))
brick_bands(-X, X, YA, YE, (0.30, 0.92), faces=("-x", "+x", "+y"))
pilaster_row("-x", -X, [YA + 0.10, 1.5, YE - 0.10], WH - 0.1)
pilaster_row("+x", X, [YA + 0.10, 1.5, YE - 0.10], WH - 0.1)
classic_door("+y", YE, 0.0, w=0.46, h=0.80)
for u in (-0.95, 0.95, -1.70, 1.70, -2.45, 2.45):
    window("+y", YE, u, 0.45, w=0.22, h=0.30, shutters=None)
pilaster_row("+y", YE, [-3.46, -1.30, 1.30, 3.46], WH - 0.1)
bpy.context.view_layer.update()
check_bounds(3.68, 3.68, "0")
variant("Stadtpalast_0")

# ================================================================ Variante 1: Bibliothekshalle, Terrassengarten
X = 3.40
YC, YW, YH, H = -3.46, -2.98, -1.60, 1.60
body(-X, X, YW, YH, H, PM)
cornice(-X, X, YW, YH, H - 0.03)
ridge = gable_roof(-X, X, YC - 0.12, YH, H, "y", PM, overhang=0.05, pitch=15)
box((6.90, 0.62, 0.05), (0, -3.24, 0.025), material=LIME_DARK)
box((5.40, 0.12, 0.03), (0, -3.60, 0.015), material=LIME_DARK)
box((5.10, 0.12, 0.05), (0, -3.52, 0.025), material=LIME_DARK)
for k in range(8):
    column(-3.10 + k * 6.20 / 7, YC, H - 0.13, r=0.062)
entablature((-3.10, YC), (3.10, YC), H, depth=0.20)
for s in (-1, 1):
    column(s * 3.10, YC + 0.24, H - 0.13, r=0.062)
    entablature((s * 3.10, YC), (s * 3.10, YW - 0.02), H, depth=0.20)
pediment_front(0.0, YC - 0.12, H, X, ridge - H)
classic_door("-y", YW, 0.0, w=0.60, h=1.00)
for sx in (-1, 1):
    scroll_rack(sx * 1.15, YW - 0.12, rows=3, cols=2, w=0.44, h=0.70)
    scroll_rack(sx * 1.75, YW - 0.12, rows=3, cols=2, w=0.44, h=0.70)
    window("-y", YW, sx * 2.50, 0.50, w=0.26, h=0.55, shutters=WOOD_DARK, open_shutters=(sx > 0))
    window("-y", YW, sx * 3.05, 0.50, w=0.20, h=0.50, shutters=None)
for u in (-2.60, -2.10, -1.80):
    window("-x", -X, u, 0.62, w=0.22, h=0.50, shutters=WOOD_DARK, open_shutters=False)
    window("+x", X, u, 0.62, w=0.22, h=0.50, shutters=WOOD_DARK, open_shutters=False)
brick_bands(-X, X, YW, YH, (0.30, 0.98), faces=("-x", "+x"))
pilaster_row("-x", -X, [YW + 0.12, YH - 0.12], H - 0.06)
pilaster_row("+x", X, [YW + 0.12, YH - 0.12], H - 0.06)
classic_door("+y", YH, 0.0, w=0.60, h=1.00)
for sx in (-1, 1):
    for u in (1.20, 1.90, 2.60):
        window("+y", YH, sx * u, 0.50, w=0.24, h=0.50, shutters=None)
oculus("+y", YH, 0.0, H + 0.45, 0.10)
pilaster_row("+y", YH, [-3.30, 3.30], H - 0.06)
# Terrassen: drei Ebenen mit deutlichen Treppen
WB = 3.50
T2, T3 = 0.24, 0.48
Y2, Y3 = 0.30, 2.00
flush_wall(-X, -X + 0.09, YH - 0.015, WB, 0.72, PM)
flush_wall(X - 0.09, X, YH - 0.015, WB, 0.72, PM)
flush_wall(-X, X, WB - 0.09, WB, 1.10, PM)
box((2 * X - 0.2, Y3 - Y2, T2), (0, (Y2 + Y3) / 2, T2 / 2), material=LIME_DARK)
box((2 * X - 0.2, WB - 0.09 - Y3, T3), (0, (Y3 + WB - 0.09) / 2, T3 / 2), material=LIME_DARK)
box((2 * X - 0.2, 0.025, T2 + 0.02), (0, Y2 - 0.0125, (T2 + 0.02) / 2), material=LIME)
box((2 * X - 0.2, 0.025, T3 + 0.02), (0, Y3 - 0.0125, (T3 + 0.02) / 2), material=LIME)


def stair(y_front, z_base, rise, width=1.2, n=3):
    """Freitreppe mit n Stufen vor einer Terrassenkante (y_front), steigt nach +y um rise."""
    for k in range(n):
        d = 0.14 * (n - k)
        h = rise * (k + 1) / (n + 1)
        box((width, d, h), (0, y_front - d / 2, z_base + h / 2), material=LIME)
        box((width + 0.02, 0.03, 0.02), (0, y_front - d - 0.005, z_base + h - 0.01), material=LIME_DARK)
    for s in (-1, 1):
        box((0.10, 0.14 * n + 0.05, rise * n / (n + 1) + 0.12), (s * (width / 2 + 0.05), y_front - 0.07 * n, z_base + (rise * n / (n + 1) + 0.12) / 2), material=LIME_DARK)
        sphere(0.05, (s * (width / 2 + 0.05), y_front - 0.14 * n + 0.02, z_base + rise * n / (n + 1) + 0.15), material=LIME, segs=8, rings=5)


box((2 * X - 0.7, Y3 - Y2 - 0.35, 0.008), (0, (Y2 + Y3) / 2 + 0.05, T2 + 0.004), material=LEAF)
box((2 * X - 0.7, WB - 0.09 - Y3 - 0.25, 0.008), (0, (Y3 + WB - 0.09) / 2 - 0.05, T3 + 0.004), material=LEAF)
for _s in (-1, 1):
    box((1.95, 1.55, 0.008), (_s * 1.85, -0.62, 0.004), material=LEAF)
stair(Y2, 0.0, T2)
stair(Y3, T2, T3 - T2)
balustrade((-X + 0.15, Y2 + 0.04), (-0.78, Y2 + 0.04), T2, h=0.16)
balustrade((0.78, Y2 + 0.04), (X - 0.15, Y2 + 0.04), T2, h=0.16)
balustrade((-X + 0.15, Y3 + 0.04), (-0.78, Y3 + 0.04), T3, h=0.16)
balustrade((0.78, Y3 + 0.04), (X - 0.15, Y3 + 0.04), T3, h=0.16)
# Ebene 1: Wasserbecken, Beete, Hecken, Zypressen
pool(-0.45, 0.45, -1.25, 0.05, h=0.10)
fountain_bowl(0.0, -0.60, r=0.13)
path([(0.0, YH + 0.05), (0.0, -1.35)], w=0.22)
for sx in (-1, 1):
    xa, xb = sorted((sx * 0.85, sx * 2.65))
    topiary_bed(xa, xb, -1.40, -0.65, BLOOM_WHITE if sx < 0 else BLOOM_YELLOW, LEAF, seed=50 + sx, spheres=4)
    topiary_bed(xa, xb, -0.40, 0.18, BLOOM_PINK if sx < 0 else BLOOM_RED, HERB_LIGHT, seed=54 + sx, spheres=4)
    hedge(sx * 3.05 - 0.06, sx * 3.05 + 0.06, YH + 0.05, 0.22, 0.24)
    for y in (-1.40, -0.85, -0.30, 0.15):
        cypress(sx * 3.22, y, 1.30, 0.11)
    statue(sx * 0.80, -1.45, h=0.45)
    bench(sx * 2.85, -0.45, yaw=90, length=0.44) if False else None
    laurel_tree(sx * 2.95, -0.45, 0.9)
# Ebene 2: Wasserlauf, Blumenbeete, Hecken
lift(T2, lambda: [
    pool(-2.30, 2.30, 1.02, 1.30, h=0.08, rim=0.05),
    fountain_bowl(-1.40, 1.16, r=0.09), fountain_bowl(0.0, 1.16, r=0.09), fountain_bowl(1.40, 1.16, r=0.09),
    topiary_bed(-2.40, -0.75, 0.42, 0.90, BLOOM_PINK, LEAF, seed=61, spheres=4),
    topiary_bed(0.75, 2.40, 0.42, 0.90, BLOOM_WHITE, HERB_LIGHT, seed=62, spheres=4),
    topiary_bed(-2.40, -0.75, 1.44, 1.92, BLOOM_YELLOW, HERB_LIGHT, seed=63, spheres=4),
    topiary_bed(0.75, 2.40, 1.44, 1.92, BLOOM_RED, LEAF, seed=64, spheres=4),
    hedge(-X + 0.15, -X + 0.27, 0.40, 1.95, 0.26), hedge(X - 0.27, X - 0.15, 0.40, 1.95, 0.26),
    statue(-0.70, 0.62, h=0.45), statue(0.70, 0.62, h=0.45),
    laurel_tree(-3.0, 0.75, 1.0), laurel_tree(3.0, 0.75, 1.0), laurel_tree(-3.0, 1.65, 1.0), laurel_tree(3.0, 1.65, 1.0),
    pedestal_urn(-0.55, 1.78), pedestal_urn(0.55, 1.78),
    cypress(-2.95, 0.45, 1.3, 0.11), cypress(2.95, 0.45, 1.3, 0.11)])
for k in range(5):
    pass
# Ebene 3: Exedra, Pergolen mit Ranken, Beete, Zypressenreihen
lift(T3, lambda: [
    exedra(0.0, 2.50, 0.85, 15, 165),
    philosopher(0.0, 2.85),
    light_pergola(-3.25, -2.30, 2.25, 3.30, h=0.78, vine_post=1),
    light_pergola(2.30, 3.25, 2.25, 3.30, h=0.78, vine_post=1),
    bench(-2.78, 3.0, yaw=0, length=0.44), bench(2.78, 3.0, yaw=0, length=0.44),
    topiary_bed(-2.15, -1.10, 2.12, 2.62, BLOOM_PINK, LEAF, seed=65, spheres=3),
    topiary_bed(1.10, 2.15, 2.12, 2.62, BLOOM_WHITE, LEAF, seed=66, spheres=3),
    topiary_bed(-2.15, -1.10, 2.78, 3.22, BLOOM_YELLOW, HERB_LIGHT, seed=67, spheres=3),
    topiary_bed(1.10, 2.15, 2.78, 3.22, BLOOM_RED, HERB_LIGHT, seed=68, spheres=3),
    herm(-0.95, 2.20), herm(0.95, 2.20),
    cypress(-1.10, 3.30, 1.3, 0.11), cypress(1.10, 3.30, 1.3, 0.11), cypress(-1.60, 3.30, 1.3, 0.11), cypress(1.60, 3.30, 1.3, 0.11),
    cypress(-2.05, 3.30, 1.3, 0.11), cypress(2.05, 3.30, 1.3, 0.11),
    cypress(-3.2, 2.12, 1.3, 0.11), cypress(3.2, 2.12, 1.3, 0.11),
    laurel_tree(-1.55, 2.55, 0.9) if False else None])
bpy.context.view_layer.update()
check_bounds(3.68, 3.68, "1")
variant("Stadtpalast_1")

export(spacing=8.0)
