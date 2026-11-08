"""
Holzhacker (Antike), 2 x 2 Felder (4 x 4 m): offener Brennholzschuppen. Hinten ein langer Schuppen auf Holzpfosten mit
Ziegel-Pultdach (Tegulae und Imbrices, Stirnziegel an der Traufe), darunter zwei Klafter Brennholz bis fast unters Dach
(Stirnseiten der Scheite sichtbar, der rechte Stapel ist angebrochen und abgetragen). Davor Hackklotz mit eingeschlagener
Axt, verstreute Scheite und Späne, ein Sägebock mit Säge und ein Handkarren mit Scheiten; rechts ein Stammstapel als
Nachschub und drei Reisigbündel.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/holzhacker.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Holzhacker_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(31)

# ---------------------------------------------------------------- Schuppen
SX0, SX1, SY0, SY1 = -1.68, 1.02, 0.20, 1.50
Z_FRONT, Z_BACK = 1.62, 1.29                   # Oberkante der Rähme vorn (hoch) und hinten (niedrig)

# Rückwand aus Bohlen, damit man von hinten nicht ins Holz sieht
box((SX1 - SX0, 0.05, Z_BACK), ((SX0 + SX1) / 2, SY1 - 0.01, Z_BACK / 2), material=WOOD)
for k in range(1, int((SX1 - SX0) / 0.12)):
    box((0.008, 0.006, Z_BACK - 0.1), (SX0 + k * 0.12, SY1 - 0.038, Z_BACK / 2), material=WOOD_DARK)
box((SX1 - SX0 + 0.04, 0.06, 0.11), ((SX0 + SX1) / 2, SY1 - 0.01, 0.055), material=STONE)                # Steinschwelle

# Pfosten auf Steinen: vorn drei, hinten drei
POST_X = (SX0 + 0.04, -0.36, SX1 - 0.04)
for x in POST_X:
    box((0.17, 0.17, 0.07), (x, SY0 + 0.05, 0.035), material=STONE)
    box((0.10, 0.10, Z_FRONT - 0.07), (x, SY0 + 0.05, 0.07 + (Z_FRONT - 0.07) / 2), material=WOOD_DARK)
    box((0.10, 0.10, Z_BACK - 0.07), (x, SY1 - 0.05, 0.07 + (Z_BACK - 0.07) / 2), material=WOOD_DARK)
# Rähme (vorn und hinten), Pfetten an den Seiten
box((SX1 - SX0 + 0.06, 0.12, 0.11), ((SX0 + SX1) / 2, SY0 + 0.05, Z_FRONT - 0.055), material=WOOD_DARK)
box((SX1 - SX0 + 0.06, 0.12, 0.11), ((SX0 + SX1) / 2, SY1 - 0.05, Z_BACK - 0.055), material=WOOD_DARK)
for x in (SX0 + 0.04, SX1 - 0.04):
    bar((x, SY0 + 0.05, Z_FRONT - 0.04), (x, SY1 - 0.05, Z_BACK - 0.04), 0.09, WOOD_DARK)
# Kopfbänder vorn: schräge Streben von den Pfosten zum Rähm, nach beiden Seiten
for x in POST_X:
    for s in (-1, 1):
        if (x < -1.0 and s < 0) or (x > 0.9 and s > 0):
            continue
        bar((x, SY0 + 0.05, Z_FRONT - 0.42), (x + s * 0.30, SY0 + 0.05, Z_FRONT - 0.10), 0.055, WOOD)

# Dach: Pultdach, hohe Seite vorn (man sieht von oben unters Dach), Traufe hinten mit Überstand
shed_roof(SX0, SX1, SY0, SY1, 1.64, "+y", overhang=0.10, pitch=14.0, eave_overhang=0.22)

# ---------------------------------------------------------------- zwei Klafter Brennholz
STACK_Y0, STACK_D = 0.50, 0.90                 # Vorderkante und Tiefe der Stapel
pitch_u = 2 * 0.034 * 1.03
n_cols = 17
w_stack = n_cols * pitch_u
ax = SX0 + 0.06 + w_stack / 2
firewood_stack(ax, STACK_Y0 + STACK_D / 2, w_stack, STACK_D, 21, r=0.034, seed=1)
bx0 = -0.28
firewood_stack(bx0 + w_stack / 2, STACK_Y0 + STACK_D / 2, w_stack, STACK_D, 21, r=0.034, seed=2,
               profile=[21] * 6 + [19, 17, 16, 14, 12, 10, 8, 6, 5, 4, 3][: n_cols - 6])
# Stapelbretter zwischen den Klaftern und Pfosten gegen das Kippen
for x in (ax - w_stack / 2 - 0.03, ax + w_stack / 2 + 0.01):
    box((0.04, STACK_D, 1.0), (x, STACK_Y0 + STACK_D / 2, 0.5), material=WOOD_DARK)
# Bruchstücke und Rinde vor den Stapeln unter dem Dach
split_logs(-0.9, 0.30, 4, 0.0, 0.45, seed=3)
wood_chips(-0.2, 0.30, 14, 0.9, seed=4)

# ---------------------------------------------------------------- Hackplatz vor dem Schuppen
chopping_block(-0.55, -0.55, yaw=0)
split_logs(-0.55, -0.55, 9, 0.22, 0.55, seed=5)
wood_chips(-0.55, -0.55, 40, 0.8, seed=6)

# Sägebock mit Stamm und Säge
sawhorse(0.60, -0.62, yaw=72)
wood_chips(0.60, -0.62, 14, 0.5, seed=7)
split_logs(0.85, -1.0, 3, 0.1, 0.3, seed=8)

# Handkarren mit Scheiten
cart(-1.35, -0.95, yaw=62, load=None)
cat = placed((-1.35, -0.95), 62)
for k, (u, v, z, a) in enumerate(((-0.05, -0.04, 0.265, 14), (0.04, 0.03, 0.265, -8), (-0.02, 0.07, 0.31, 20),
                                  (0.06, -0.05, 0.31, -4), (0.0, 0.0, 0.355, 10))):
    p = cat((u * 1.2, v * 1.2, 0.0))
    box((0.19, 0.05, 0.045), (p[0], p[1], z), rot=(0, 0, 62 + 90 + a), material=END_GRAIN if k % 2 else WOOD_FRESH)
split_logs(-1.35, -1.35, 4, 0.0, 0.3, seed=9)

# ---------------------------------------------------------------- rechts: Stammstapel und Reisig
log_pile(1.45, 0.90, 1.20, r=0.075, rows=(3, 2), yaw=0.0, seed=11)
brushwood(1.30, -0.20, yaw=95, seed=1)
brushwood(1.34, -0.46, yaw=82, seed=2)
brushwood(1.32, -0.33, z0=0.10, yaw=88, seed=3)
split_logs(1.45, -0.95, 5, 0.1, 0.35, seed=12)
cyl(0.16, 0.16, (-1.45, -1.60, 0.08), material=BARK, verts=10)                        # Baumstumpf als Sitz
cyl(0.15, 0.008, (-1.45, -1.60, 0.162), material=END_GRAIN, verts=10)

obj = variant("Holzhacker_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
