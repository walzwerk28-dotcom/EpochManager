"""
Kelterei (Antike), 2 x 2 Felder (4 x 4 m): hinten in der Hofhälfte das Kelterhaus aus Kalkstein mit Ziegelbändern und
flachem Ziegel-Satteldach (Traufe zur Straße), vorn zwischen zwei Eckpfeilern ganz offen, damit man hineinsieht. Darin
eine Baumpresse: der lange Hebelbalken steckt links in einem Mauerpfeiler, drückt in der Mitte auf einen Stapel Körbe mit
Maische und trägt rechts einen hängenden Gegengewichtsstein; davor ein Auffangbecken mit Most, hinten Amphoren und Fässer.
Im Hof unter freiem Himmel: links das gemauerte Tretbecken (Calcatorium) voller roter und violetter Trauben mit Haltebalken
darüber, sein Ausguss läuft in ein tiefer liegendes Mostbecken mit dunkelrotem Most; rechts eine Reihe eingegrabener Dolien
(nur Hals und Deckel ragen heraus), davor Reihen von Amphoren, Fässer, ein Handkarren mit Trauben, vorn rechts eine Weinlaube
mit Steinbank. Vorn niedrige Hofmauern mit Durchgang zwischen zwei Steinpfosten.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/kelterei.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Kelterei_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(71)

# ---------------------------------------------------------------- Kelterhaus
X0, X1, Y0, Y1 = -1.67, 1.67, 0.55, 1.67
HH = 1.20
W = 0.12
box((X1 - X0, W, HH), (0, Y1 - W / 2, HH / 2), material=LIME)                                  # Rückwand
box((W, Y1 - Y0, HH), (X0 + W / 2, (Y0 + Y1) / 2, HH / 2), material=LIME)                      # linke Wand
box((W, Y1 - Y0, HH), (X1 - W / 2, (Y0 + Y1) / 2, HH / 2), material=LIME)                      # rechte Wand
box((X1 - X0 + 0.04, Y1 - Y0 + 0.04, 0.09), (0, (Y0 + Y1) / 2, 0.045), material=STONE)         # Sockel
brick_bands(X0, X1, Y1 - W, Y1, (0.30, 0.60, 0.90), faces=("+y",))
brick_bands(X0, X0 + W, Y0, Y1, (0.30, 0.60, 0.90), faces=("-x",))
brick_bands(X1 - W, X1, Y0, Y1, (0.30, 0.60, 0.90), faces=("+x",))
brick_bands(X0 + W, X1 - W, Y1 - W, Y1 - W, (0.30, 0.60, 0.90), faces=("-y",))                  # innen an der Rückwand
box((X1 - X0, W, 0.12), (0, Y1 - W / 2, 0.06), material=LIME_DARK)
# zwei Eckpfeiler und ein schmaler Architrav oben: sonst ist die Front ganz offen
for sx in (-1, 1):
    box((0.22, 0.22, HH), (sx * 1.56, Y0 + 0.07, HH / 2), material=LIME)
    brick_bands(sx * 1.56 - 0.11, sx * 1.56 + 0.11, Y0 - 0.04, Y0 + 0.18, (0.30, 0.60, 0.90), faces=("-y",))
    box((0.26, 0.26, 0.07), (sx * 1.56, Y0 + 0.07, 0.035), material=LIME_DARK)
    box((0.26, 0.26, 0.05), (sx * 1.56, Y0 + 0.07, HH + 0.025), material=LIME_DARK)
box((X1 - X0, 0.14, 0.09), (0, Y0 + 0.07, HH - 0.045), material=WOOD_DARK)                     # Sturzbalken
gable_roof(X0, X1, Y0, Y1, HH, "x", LIME, overhang=0.09, pitch=18)

# ---------------------------------------------------------------- Baumpresse im Haus
lever_press(-1.50, 0.55, 1.07, z=0.62, u_press=-0.55, axis="x", fill=GRAPE_MASS)
box((0.40, 0.05, 0.012), (-0.55, 0.86, 0.076), material=MUST)                                    # Rinne mit Most
must_basin(-0.80, -0.30, 0.64, 0.82, h=0.12)
for k in range(3):
    grape_basket(0.05 + k * 0.30, 0.72, r=0.075, seed=30 + k)                                    # Körbe mit Trauben
amphora(1.45, 1.50)
amphora(1.22, 1.52)
barrel(1.38, 0.90, r=0.075)
barrel(1.40, 0.70, r=0.07, lying=True, yaw=10)

# ---------------------------------------------------------------- Tretbecken links im Hof, Haltebalken, Mostbecken
sp = tread_basin(-1.36, -0.40, -0.40, 0.30, h=0.30, wall=0.08, spout_x=-0.88, seed=3)
must_basin(-1.28, -0.48, -1.06, -0.60, h=0.14)
rod((sp[0], sp[1] + 0.02, sp[2]), (sp[0], -0.74, 0.17), 0.011, MUST, verts=5)                    # Moststrahl
for sx in (-1.30, -0.46):                                                                          # Haltebalken zum Treten
    box((0.05, 0.05, 0.62), (sx, -0.32, 0.31), material=WOOD_DARK)
box((0.95, 0.05, 0.05), (-0.88, -0.32, 0.64), material=WOOD_DARK)
rope_coil(-1.62, -0.12, r=0.06, layers=3)

# ---------------------------------------------------------------- eingegrabene Dolien
for k, x in enumerate((0.30, 0.74, 1.18, 1.58)):
    buried_dolium(x, 0.14, r=0.17 if k != 3 else 0.15, open_lid=(k == 1), yaw=20)

# ---------------------------------------------------------------- Amphorenreihen, Fässer
for k in range(4):
    amphora(0.42 + k * 0.21, -0.38)
for k in range(3):
    amphora(0.52 + k * 0.21, -0.60)
barrel(-1.57, -0.95, r=0.075)
barrel(-1.56, -1.22, r=0.07)
barrel(-1.28, -1.45, r=0.07, lying=True, yaw=15)

# ---------------------------------------------------------------- Karren mit Trauben
cart(-0.25, -1.05, yaw=18, load=None)
cat = placed((-0.25, -1.05), 18)
cc = cat((0.0, 0.0, 0.0))
sphere(0.12, (cc[0], cc[1], 0.24), (1.15, 1.75, 0.55), GRAPE_MASS, segs=8, rings=4, rot=(0, 0, 18))
grape_pile(cc[0], cc[1], 0.22, 0.26, 0.40, 22, seed=5)
grape_basket(0.20, -1.40, r=0.08, seed=41)
sickle(0.30, -0.95, 0.012, flat=True, yaw=-20)

# ---------------------------------------------------------------- Weinlaube mit Bank vorn rechts
pergola(1.05, 1.60, -1.60, -1.18, h=0.62, vine_post=1)
bench(1.33, -1.39, yaw=0, length=0.44)
jug(1.12, -1.39, 0.19, 0.03)
jug(1.54, -1.39, 0.19, 0.03)

# ---------------------------------------------------------------- Hofmauer vorn mit Durchgang
dry_stone_wall((-1.70, -1.65), (-0.62, -1.65), h=0.15, seed=11)
dry_stone_wall((0.60, -1.65), (0.84, -1.65), h=0.15, seed=12)
stone_post(-0.58, -1.65, h=0.36, yaw=5)
stone_post(0.58, -1.65, h=0.36, yaw=-8)
dry_stone_wall((-1.67, -1.62), (-1.67, -1.20), h=0.15, seed=13)
for gx, gy in ((-1.55, 0.50), (0.0, -0.05), (1.62, -0.10), (-0.55, 0.40), (0.0, -1.60)):
    grass_tuft(gx, gy, 0.09, seed=int(abs(gx * 10)) + 1)

obj = variant("Kelterei_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
