"""
Weinberg (Antike), 2 x 2 Felder (4 x 4 m): fünf Rebzeilen längs der Tiefe an Holzpfählen mit drei Querdrähten (Spalier),
Rebstöcke mit knorrigen, gedrehten Stämmen, dichtem Laubband in drei kräftigen Grüntönen und hängenden blauen und
violetten Trauben. Hinten und an den Seiten eine niedrige Trockenmauer, vorn ein Durchgang zwischen zwei Steinpfosten.
Zwischen den Zeilen stehen Lesekörbe voller Trauben, rechts vorn ein Handkarren mit einer Bütte voller Trauben, hinten
rechts ein offener Unterstand mit Ziegel-Pultdach (Körbe, Amphoren, Rebmesser an der Wand), dazu Grasbüschel.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/weinberg.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Weinberg_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(63)

# ---------------------------------------------------------------- Trockenmauer, Durchgang vorn
field_wall([(-1.70, -1.67), (-1.70, 1.68), (1.70, 1.68), (1.70, -1.67)], h=0.26, t=0.16, seed=5, ext_start=True, ext_end=True)
field_wall([(-1.70, -1.67), (-0.62, -1.67)], h=0.26, t=0.16, seed=8)
field_wall([(0.62, -1.67), (1.70, -1.67)], h=0.26, t=0.16, seed=9)
field_pillar(-0.50, -1.67, h=0.42, w=0.20, yaw=4, seed=1)
field_pillar(0.50, -1.67, h=0.42, w=0.20, yaw=-6, seed=2)
for k, x in enumerate((-0.20, 0.0, 0.20)):                                                   # Trittsteine im Durchgang
    box((0.20, 0.14, 0.02), (x * 0.9, -1.62 + 0.0 * k, 0.01), rot=(0, 0, 6 * (k - 1)), material=FIELD_CAP)

# ---------------------------------------------------------------- Rebzeilen
ROWS = (-1.35, -0.85, -0.35, 0.15, 0.65)
Y0, Y1 = -0.60, 1.30
for ri, rx in enumerate(ROWS):
    vine_row(rx, Y0, Y1, seed=ri + 1)

# ---------------------------------------------------------------- Gras zwischen den Zeilen
for k, (gx, gy) in enumerate(((-1.10, -0.30), (-0.60, 0.50), (-0.10, -0.45), (-1.10, 1.00), (-0.10, 1.20), (0.40, -0.55),
                              (-1.55, 0.50), (-1.55, 1.30), (0.90, 0.10), (-0.60, -0.40), (0.40, 0.60), (-0.10, 0.20),
                              (0.40, 1.30), (-1.10, 0.35), (1.58, -0.30), (-1.55, -1.45))):
    grass_tuft(gx, gy, 0.09 + 0.02 * (k % 3), seed=k)

# ---------------------------------------------------------------- Lesekörbe in den Gassen
grape_basket(-1.10, -0.50, r=0.085, seed=1)
grape_basket(-0.60, 0.15, r=0.075, seed=2)
grape_basket(-0.10, 0.85, r=0.085, seed=3)
grape_basket(0.40, -0.05, r=0.08, seed=4)
grape_basket(-1.55, -0.20, r=0.085, seed=5)
sickle(0.40, -0.35, 0.012, flat=True, yaw=35)

# ---------------------------------------------------------------- Karren mit Bütte rechts vorn
cart(1.15, -0.95, yaw=-25, load=None)
cat = placed((1.15, -0.95), -25)
c = cat((0.0, 0.0, 0.0))
vat(c[0], c[1], z0=0.2, r=0.115, h=0.19, seed=11)
grape_basket(1.50, -1.35, r=0.085, seed=6)
grape_basket(0.85, -1.40, r=0.08, seed=8)
amphora(1.52, -0.45)

# ---------------------------------------------------------------- Unterstand hinten rechts
SX0, SX1, SY0, SY1 = 0.98, 1.60, 0.50, 1.60
open_shed(SX0, SX1, SY0, SY1, 0.60)
grape_basket(1.12, 1.35, r=0.085, seed=21)
grape_basket(1.30, 1.40, r=0.08, seed=22)
grape_basket(1.45, 1.30, r=0.085, seed=23)
amphora(1.15, 0.95)
amphora(1.52, 1.10)
sickle(1.27, 1.55, 0.40)
sickle(1.43, 1.55, 0.34)
barrel(1.20, 0.70, r=0.07)
crate(1.44, 0.68, size=0.16)

obj = variant("Weinberg_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
