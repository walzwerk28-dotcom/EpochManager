"""
Töpferei (Antike), 2 x 2 Felder (4 x 4 m): links hinten die Werkstatt (ockerfarbener Putz mit roter Sockelzone, Ziegel-
Satteldach, vorn offen zwischen Pfosten und Holzsturz). Darin die Töpferscheibe mit Fußantrieb, Hocker und Wasserschale, an der
Rückwand ein Regal mit ungebrannten, hellen Gefäßen, an der linken Wand ein Regal mit gebrannten Terrakotta-Gefäßen. Rechts neben
der Werkstatt der große Brennofen aus Ziegeln mit lehmverputzter Kuppel, Eisenreifen, Schlot, Feuerloch mit Ruß und Asche, daneben
Brennholz, Reisig und ein dünner Rauchfaden. Vor der Werkstatt stehen Reihen fertiger Amphoren, ein niedriger Tisch mit
Krügen und Schalen und rechts ein Kasten mit Lehmvorrat.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/toepferei.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Toepferei_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(37)

# ---------------------------------------------------------------- Werkstatt
X0, X1, Y0, Y1 = -1.64, 0.30, 0.40, 1.64
HH = 1.06
W = 0.12
box((X1 - X0, W, HH), ((X0 + X1) / 2, Y1 - W / 2, HH / 2), material=PLASTER_OCHRE)                  # Rückwand
box((W, Y1 - Y0, HH), (X0 + W / 2, (Y0 + Y1) / 2, HH / 2), material=PLASTER_OCHRE)                  # linke Wand
box((W, Y1 - Y0, HH), (X1 - W / 2, (Y0 + Y1) / 2, HH / 2), material=PLASTER_OCHRE)                  # rechte Wand
box((X1 - X0 + 0.04, Y1 - Y0 + 0.04, 0.09), ((X0 + X1) / 2, (Y0 + Y1) / 2, 0.045), material=STONE)  # Sockel
box((X1 - X0 + 0.012, W + 0.012, 0.18), ((X0 + X1) / 2, Y1 - W / 2, 0.18), material=SOCLE_RED)
box((W + 0.012, Y1 - Y0 + 0.012, 0.18), (X0 + W / 2, (Y0 + Y1) / 2, 0.18), material=SOCLE_RED)
box((W + 0.012, Y1 - Y0 + 0.012, 0.18), (X1 - W / 2, (Y0 + Y1) / 2, 0.18), material=SOCLE_RED)
for x in (X0 + 0.06, -0.68, X1 - 0.06):                                                             # Pfosten vorn
    box((0.13, 0.13, 0.97), (x, Y0 + 0.07, 0.485), material=WOOD_DARK)
    box((0.18, 0.18, 0.05), (x, Y0 + 0.07, 0.025), material=STONE)
box((X1 - X0, 0.15, 0.13), ((X0 + X1) / 2, Y0 + 0.07, 1.00), material=WOOD_DARK)                    # Holzsturz
for k in range(-4, 5):
    box((0.012, 0.16, 0.13), ((X0 + X1) / 2 + k * 0.22, Y0 + 0.07, 1.00), material=WOOD)
for x, s in ((-1.52, 1), (-0.80, -1), (-0.56, 1), (0.18, -1)):                                      # Kopfbänder
    bar((x, Y0 + 0.07, 0.84), (x + s * 0.17, Y0 + 0.07, 1.00), 0.05, WOOD)
ridge = gable_roof(X0, X1, Y0, Y1, HH, "x", PLASTER_OCHRE, overhang=0.09, pitch=22)

# ---------------------------------------------------------------- in der Werkstatt
potters_wheel(-0.90, 0.88, yaw=0)
vessel_shelf(-0.52, 1.45, yaw=0, w=1.30, tiers=3, fired=False, seed=2)                              # ungebrannt, hinten
vessel_shelf(-1.46, 0.98, yaw=90, w=0.90, tiers=3, fired=True, seed=3)                               # gebrannt, links
for k in range(3):                                                                                  # Tonklumpen auf dem Boden
    sphere(0.045, (-0.30 + 0.07 * k, 0.60 + 0.03 * k, 0.04), (1.0, 1.0, 0.75), CLAY, segs=6, rings=4)
box((0.34, 0.26, 0.23), (-0.12, 0.95, 0.115), material=WOOD)                                        # Bank mit Tonblock
sphere(0.12, (-0.12, 0.95, 0.24), (1.2, 0.9, 0.6), CLAY, segs=8, rings=4)
for k in range(3):
    pot(-0.30 + 0.12 * k, 1.12, 0.0, 0.05, CLAY_UNFIRED)

# ---------------------------------------------------------------- Brennofen rechts
KX, KY = 1.10, 1.08
top = kiln(KX, KY, r=0.52)
smoke_plume(KX, KY, top - 0.04, n=3)
firewood_stack(0.62, 0.12, 0.50, 0.30, 3, r=0.034, yaw=0, seed=4)
brushwood(1.18, 0.10, yaw=70, length=0.45, seed=3)
barrel(1.50, 1.52, r=0.06)
cyl(0.06, 0.02, (1.52, 1.52, 0.40), material=WOOD, verts=8)                                          # Deckel

# ---------------------------------------------------------------- fertige Ware vor der Werkstatt
for j, y in enumerate((-1.45, -1.22)):
    for i in range(5 - j):
        amphora(-1.60 + i * 0.165 + (0.08 if j else 0.0), y)
bowl(-0.55, -1.45, 0.0, 0.07)                                                                       # Schalenstapel
bowl(-0.55, -1.45, 0.03, 0.065, TERRACOTTA_LIGHT)
bowl(-0.55, -1.45, 0.06, 0.06)
bowl(-0.35, -1.50, 0.0, 0.07, TERRACOTTA_LIGHT)
bowl(-0.35, -1.50, 0.03, 0.065)
# niedriger Tisch mit Krügen und Schalen
TX, TY = -0.05, -0.95
box((0.95, 0.34, 0.03), (TX, TY, 0.25), material=WOOD)
for sx in (-1, 1):
    for sy in (-1, 1):
        box((0.04, 0.04, 0.25), (TX + sx * 0.42, TY + sy * 0.13, 0.125), material=WOOD_DARK)
box((0.85, 0.03, 0.03), (TX, TY, 0.10), material=WOOD_DARK)
for i in range(5):
    jug(TX - 0.36 + i * 0.16, TY + 0.06, 0.265, 0.040, TERRACOTTA if i % 2 else TERRACOTTA_LIGHT)
for i in range(3):
    bowl(TX - 0.25 + i * 0.25, TY - 0.08, 0.265, 0.06, TERRACOTTA)
pot(TX + 0.42, TY - 0.08, 0.265, 0.045, TERRACOTTA_LIGHT)

# Kasten mit Lehmvorrat rechts vorn
CX, CY = 0.92, -0.55
box((0.50, 0.32, 0.22), (CX, CY, 0.11), material=WOOD)
box((0.52, 0.34, 0.03), (CX, CY, 0.215), material=WOOD_DARK)
box((0.44, 0.26, 0.03), (CX, CY, 0.225), material=CLAY)
for k in range(3):
    sphere(0.055, (CX - 0.12 + 0.12 * k, CY + 0.02 * (k - 1), 0.255), (1.2, 1.0, 0.7), CLAY if k % 2 else CLAY_DARK, segs=7, rings=4)
box((0.22, 0.30, 0.02), (CX + 0.36, CY - 0.06, 0.11), rot=(0, -25, 8), material=WOOD)              # Deckelbrett lehnt
bucket(CX + 0.40, CY + 0.35, r=0.05)
for gx, gy in ((-1.62, -0.65), (1.60, -1.0), (0.50, -1.55), (1.45, -1.55)):
    grass_tuft(gx, gy, 0.08, seed=int(abs(gx * 13)))

obj = variant("Toepferei_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
