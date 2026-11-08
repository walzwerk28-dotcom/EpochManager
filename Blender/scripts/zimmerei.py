"""
Zimmerei (Antike), 2 x 2 Felder (4 x 4 m): offene Werkhalle auf Holzpfosten mit Ziegel-Satteldach (Giebel zur Straße,
Kopfbänder an den Pfosten, Bohlenwand hinten mit Werkzeugwand). Darin links die Zimmermannsbank mit Hobel, Beitel,
Klüpfel und Schraubstock, in der Mitte die Sägegrube mit einem halb zu Brettern zersägten Stamm und der Rahmensäge, rechts
ein Balkenstapel. Vor der Halle links ein halb fertiger Dachstuhl (Sparrengebinde auf Steinmauern mit Firstpfette und
Latten); rechts ein Bretterstapel mit Zwischenhölzern, ein Balkenstapel, ein Stammstapel, vorn ein Karren mit Brettern.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/zimmerei.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Zimmerei_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(63)

# ---------------------------------------------------------------- Werkhalle
HX0, HX1, HY0, HY1 = -1.66, 0.70, 0.04, 1.66
HH = 1.30                                      # Oberkante der Pfetten (Traufe)

# Rückwand aus Bohlen mit Steinschwelle und Fugen
box((HX1 - HX0, 0.05, HH), ((HX0 + HX1) / 2, HY1 - 0.005, HH / 2), material=WOOD)
for k in range(1, int((HX1 - HX0) / 0.12)):
    box((0.008, 0.006, HH - 0.12), (HX0 + k * 0.12, HY1 - 0.033, HH / 2), material=WOOD_DARK)
box((HX1 - HX0 + 0.04, 0.07, 0.11), ((HX0 + HX1) / 2, HY1 - 0.005, 0.055), material=STONE)

# Pfosten auf Steinen (je Seite drei), Pfetten, Zangen und Kopfbänder
POST_Y = (HY0 + 0.05, (HY0 + HY1) / 2, HY1 - 0.05)
for x in (HX0 + 0.05, HX1 - 0.05):
    box((0.14, 0.14, 0.07), (x, POST_Y[0], 0.035), material=STONE)
    for y in POST_Y:
        box((0.17, 0.17, 0.07), (x, y, 0.035), material=STONE)
        box((0.10, 0.10, HH - 0.07), (x, y, 0.07 + (HH - 0.07) / 2), material=WOOD_DARK)
    box((0.10, HY1 - HY0 + 0.06, 0.10), (x, (HY0 + HY1) / 2, HH - 0.05), material=WOOD_DARK)          # Pfette
for y in POST_Y:
    box((HX1 - HX0 + 0.06, 0.09, 0.09), ((HX0 + HX1) / 2, y, HH - 0.14), material=WOOD)                # Zange quer
    for x, s in ((HX0 + 0.05, 1), (HX1 - 0.05, -1)):
        bar((x, y, HH - 0.44), (x + s * 0.34, y, HH - 0.14), 0.05, WOOD)                                # Kopfband quer
        for sy in (-1, 1):
            if (y == POST_Y[0] and sy < 0) or (y == POST_Y[2] and sy > 0):
                continue
            bar((x, y, HH - 0.44), (x, y + sy * 0.30, HH - 0.12), 0.05, WOOD)                           # Kopfband längs

# Satteldach, Giebel zur Straße; Dreieck unter dem First in Bohlen
ridge = gable_roof(HX0, HX1, HY0, HY1, HH, "y", WOOD, overhang=0.09, pitch=24)
xc = (HX0 + HX1) / 2
for k in range(-11, 12):                       # senkrechte Fugen im Giebel
    x = xc + k * 0.10
    gh = (ridge - HH - 0.05) * (1 - abs(x - xc) / ((HX1 - HX0) / 2))
    if gh > 0.06:
        box((0.008, 0.006, gh), (x, HY0 - 0.003, HH + gh / 2), material=WOOD_DARK)
cyl(0.075, 0.02, (xc, HY0 - 0.008, HH + 0.22), rot=(90, 0, 0), material=DARK, verts=12)              # Lüftungsloch
box((0.20, 0.03, 0.025), (xc, HY0 - 0.02, HH + 0.22 + 0.095), material=WOOD_DARK)

# Werkzeugwand an der Rückwand
tool_wall(-1.40, 0.40, HY1 - 0.03, 0.42, 1.18)

# ---------------------------------------------------------------- in der Halle
workbench(-1.32, 0.78, yaw=0)
sawpit(-0.28, 0.88, yaw=0, length=1.30)
beam_stack(0.46, 1.05, 0.95, rows=(2, 1), yaw=0, seed=3)
wood_chips(-0.9, 0.5, 18, 0.6, seed=2)
wood_chips(0.2, 0.4, 10, 0.4, seed=3)

# ---------------------------------------------------------------- Dachstuhl vor der Halle links
roof_frame(-0.98, -1.50, 4, 0.40, 1.30, 0.50, yaw=0, standing=3, wall=0.20)
wood_chips(-1.0, -0.85, 12, 0.5, seed=4)

# ---------------------------------------------------------------- rechts der Halle
plank_stack(1.36, 1.00, 0.64, 1.30, 13, yaw=0, seed=5)
beam_stack(1.34, 0.10, 0.82, rows=(3, 2, 1), yaw=90, seed=6)
log_pile(1.30, -0.62, 0.82, r=0.07, rows=(3, 2), yaw=90, seed=7)
wood_chips(1.30, 0.25, 10, 0.5, seed=8)

# Karren mit Brettern vorn
cart(0.30, -1.18, yaw=24, load=None)
cat = placed((0.30, -1.18), 24)
for k in range(4):
    p = cat((0.0, 0.02 + 0.0 * k, 0.0))
    box((0.30 - 0.03 * (k % 2), 0.62 + 0.04 * (k % 3), 0.016), (p[0], p[1], 0.245 + k * 0.018), rot=(0, 0, 24 + 3 * (k - 1)),
        material=WOOD_FRESH if k % 2 else WOOD)
wood_chips(0.4, -0.75, 8, 0.4, seed=9)

obj = variant("Zimmerei_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
