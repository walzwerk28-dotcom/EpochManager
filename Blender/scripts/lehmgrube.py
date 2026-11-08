"""
Lehmgrube (Antike), 2 x 2 Felder (4 x 4 m): links hinten die Grube, dargestellt als Senke zwischen zwei Stufen aus Lehm-
wällen (unten dunkel, oben hell, darauf Mutterboden und Gras), die Grubensohle nass und rotbraun, vorn ein Zugang aus Bohlen
mit Leisten und Geländer. In der Grube Spaten, Hacke, eine Schubkarre voller Lehm und Körbe mit Lehmbrocken. Eine Holzrinne
führt den Lehm rechts in zwei Schlämmbecken mit Bohlenwänden (Lehmbrühe und klares Wasser). Hinten rechts ein kleiner
Unterstand mit Pultdach und Lehmhaufen, Trockenstapeln aus Lehmziegeln, Wasserfass und Spaten; vorn liegen Reihen geformter
Lehmziegel zum Trocknen auf dem Boden, rechts ein weiterer Haufen mit Spaten.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/lehmgrube.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Lehmgrube_0).
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(29)
rnd = random.Random(8)

# ---------------------------------------------------------------- Grube: Sohle und zwei Stufen aus Lehmwällen
PCX, PCY = -0.44, 0.42
A, B = 0.70, 0.52                                          # Halbachsen der Sohle
sphere(1.0, (PCX, PCY, 0.0), (A + 0.06, B + 0.06, 0.012), CLAY_DARK, segs=20, rings=3)             # Sohle, trocken
sphere(1.0, (PCX + 0.05, PCY - 0.02, 0.012), (A * 0.7, B * 0.65, 0.012), SLURRY, segs=16, rings=3)  # nasse Mitte
n = 20
for tier, (inner, top, mats) in enumerate(((0.0, 0.15, (CLAY_DARK, CLAY, CLAY_DARK)), (0.27, 0.30, (CLAY, CLAY_BRICK, CLAY)))):
    for i in range(n):
        th = math.tau * (i + 0.5) / n
        deg = math.degrees(th)
        if -128 < ((deg + 180) % 360) - 180 < -52:                                                # Zugang vorn
            continue
        ra, rb = A + inner + 0.14, B + inner + 0.14
        cx, cy = PCX + math.cos(th) * ra, PCY + math.sin(th) * rb
        h = top * rnd.uniform(0.92, 1.05)
        chunky(cx - 0.15, cx + 0.15, cy - 0.17, cy + 0.17, 0.0, h, rnd.choice(mats), jitter=0.035, seed=i * 7 + tier, yaw=deg)
        if tier == 1:                                                                            # Mutterboden obenauf
            chunky(cx - 0.15, cx + 0.15, cy - 0.17, cy + 0.17, h - 0.01, h + 0.03, SOIL, jitter=0.03, seed=i + 90, yaw=deg)
            if i % 3 == 0:
                grass_tuft(cx + rnd.uniform(-0.05, 0.05), cy + rnd.uniform(-0.05, 0.05), 0.09, seed=i)
# Steinlagen sichtbar machen: helle Bänder an der Innenkante der oberen Stufe
for i in range(n):
    th = math.tau * (i + 0.5) / n
    deg = math.degrees(th)
    if -128 < ((deg + 180) % 360) - 180 < -52:
        continue
    cx, cy = PCX + math.cos(th) * (A + 0.255), PCY + math.sin(th) * (B + 0.255)
    box((0.03, 0.30, 0.025), (cx, cy, 0.185), rot=(0, 0, deg), material=CLAY_BRICK)

# Zugang vorn: Bohlenweg mit Leisten und Geländer
RX = PCX
for k in range(7):
    y = -0.30 - k * 0.12
    box((0.34, 0.11, 0.025), (RX, y, 0.015 + max(0.0, 0.05 - k * 0.008)), rot=(0, 0, 0), material=WOOD)
    box((0.34, 0.016, 0.018), (RX, y + 0.04, 0.045 + max(0.0, 0.05 - k * 0.008)), material=WOOD_DARK)
for sx in (-1, 1):
    for yy in (-0.28, -0.70, -1.10):
        cyl(0.016, 0.20, (RX + sx * 0.20, yy, 0.10), material=WOOD_DARK, verts=5)
    bar((RX + sx * 0.20, -0.28, 0.20), (RX + sx * 0.20, -1.10, 0.20), 0.022, WOOD)

# In der Grube: Schubkarre mit Lehm, Spaten, Hacke, Körbe mit Lehmbrocken
wheelbarrow(PCX - 0.25, PCY + 0.05, yaw=200, load=None)
sphere(0.16, (PCX - 0.25, PCY + 0.06, 0.28), (1.0, 1.2, 0.55), CLAY, segs=8, rings=4)
for k in range(3):
    sphere(0.05, (PCX - 0.22 + 0.06 * k, PCY + 0.10, 0.32), (1.0, 1.0, 0.8), CLAY_BRICK, segs=6, rings=4)
spade((PCX + 0.35, PCY + 0.20, 0.0), (PCX + 0.50, PCY + 0.14, 0.55), across=(0.2, 1.0, 0.0))
hoe((PCX + 0.28, PCY - 0.15, 0.0), (PCX + 0.55, PCY - 0.18, 0.55), across=(0.0, 1.0, 0.0))
olive_basket(PCX + 0.42, PCY - 0.30, 0.0, r=0.085, fill=False)
for k in range(5):
    sphere(0.045, (PCX + 0.42 + rnd.uniform(-0.04, 0.04), PCY - 0.30 + rnd.uniform(-0.04, 0.04), 0.095 + 0.015 * (k % 2)),
           (1.0, 1.0, 0.8), CLAY if k % 2 else CLAY_BRICK, segs=6, rings=4)
for k in range(4):
    sphere(0.05, (PCX - 0.5 + 0.1 * k, PCY - 0.30, 0.03), (1.2, 1.0, 0.7), CLAY if k % 2 else CLAY_BRICK, segs=6, rings=4)

# ---------------------------------------------------------------- Rinne und Schlämmbecken rechts
box((0.62, 0.11, 0.03), (0.60, 0.30, 0.13), material=WOOD)                                       # Rinne (Boden)
for sy in (-1, 1):
    box((0.62, 0.02, 0.06), (0.60, 0.30 + sy * 0.055, 0.17), material=WOOD)
for x in (0.40, 0.70):
    box((0.05, 0.14, 0.12), (x, 0.30, 0.06), material=WOOD_DARK)
clay_basin(1.00, 1.58, 0.02, 0.56, fill=SLURRY, h=0.13)
clay_basin(1.00, 1.58, -0.66, -0.14, fill=WATER, h=0.11)
box((0.10, 0.18, 0.025), (1.02, -0.05, 0.075), rot=(0, 0, 0), material=WOOD)                    # Ablauf zwischen den Becken
for k in range(5):
    sphere(0.035, (1.12 + 0.09 * k, 0.30 + 0.10 * ((k % 3) - 1), 0.14), (1.0, 1.0, 0.25), CLAY_BRICK, segs=6, rings=3)   # Klumpen
bar((1.35, 0.28, 0.40), (1.0, 0.0, 0.14), 0.02, WOOD_DARK)                                       # Rührstange
box((0.04, 0.04, 0.30), (1.38, 0.30, 0.20), rot=(0, 20, 0), material=WOOD_DARK)

# ---------------------------------------------------------------- Unterstand hinten rechts
SX0, SX1, SY0, SY1 = 0.62, 1.66, 0.98, 1.66
for x in (SX0 + 0.04, SX1 - 0.04):
    for y, hh in ((SY1 - 0.04, 1.12), (SY0 + 0.04, 0.88)):
        box((0.07, 0.07, hh), (x, y, hh / 2), material=WOOD_DARK)
shed_roof(SX0, SX1, SY0, SY1, 1.16, "-y", overhang=0.06, pitch=16, eave_overhang=0.06)
bar((SX0 + 0.04, SY1 - 0.04, 0.78), (SX0 + 0.04, SY0 + 0.04, 0.70), 0.04, WOOD)                 # Aussteifungen
bar((SX1 - 0.04, SY1 - 0.04, 0.78), (SX1 - 0.04, SY0 + 0.04, 0.70), 0.04, WOOD)
box((SX1 - SX0 - 0.10, 0.04, 0.38), ((SX0 + SX1) / 2, SY1 - 0.02, 0.19), material=WOOD)          # Bretterwand hinten unten
clay_mound(1.15, 1.30, r=0.28, h=0.20, seed=3)
brick_hurdle(0.80, 1.52, yaw=0, layers=6)
brick_hurdle(0.80, 1.20, yaw=0, layers=4)
brick_hurdle(1.52, 1.14, yaw=90, layers=5)
spade((1.42, 1.44, 0.04), (1.30, 1.40, 0.62), across=(0.2, 1.0, 0.0))
barrel(1.55, 1.52, r=0.07)

# ---------------------------------------------------------------- Lehmziegel zum Trocknen vorn
mud_brick_row(-1.60, -1.62, 8, rows=2, yaw=0)
mud_brick_row(-1.60, -1.28, 6, rows=2, yaw=0)
mud_brick_row(0.15, -1.60, 5, rows=2, yaw=0)
brick_hurdle(0.95, -1.48, yaw=0, layers=5)
brick_hurdle(1.30, -1.50, yaw=90, layers=3)
# Ziegelform und Wassereimer
box((0.16, 0.10, 0.03), (0.30, -0.95, 0.015), material=WOOD)
box((0.12, 0.06, 0.03), (0.30, -0.95, 0.03), material=CLAY_DARK)
bucket(0.55, -0.98, r=0.05)

# rechts vorn: zweiter Haufen mit Spaten und Schubkarre
clay_mound(1.30, -1.00, r=0.22, h=0.16, seed=6)
spade((1.40, -0.95, 0.06), (1.52, -0.80, 0.60), across=(1.0, 0.2, 0.0))
wheelbarrow(0.55, -0.50, yaw=24, load=None)
sphere(0.13, (0.56, -0.50, 0.28), (1.0, 1.2, 0.5), CLAY, segs=8, rings=4)
for gx, gy in ((-1.65, -0.6), (1.62, -0.35), (-0.15, -1.0), (0.25, -0.25), (1.68, 0.8)):
    grass_tuft(gx, gy, 0.09, seed=int(abs(gx * 17 + gy * 5)))

obj = variant("Lehmgrube_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
