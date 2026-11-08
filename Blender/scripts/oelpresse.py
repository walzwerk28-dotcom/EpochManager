"""
Ölpresse (Antike), 2 x 2 Felder (4 x 4 m): hinten das gemauerte Pressenhaus (Kalkstein mit Ziegelbändern, Ziegel-Satteldach
mit der Traufe zur Straße, vorn weit offen unter einem Holzsturz zwischen zwei Steinpfeilern). Darin eine Balkenpresse:
der lange Hebelbalken steckt links in einem Mauerpfeiler, drückt in der Mitte auf einen Stapel Flechtkörbe (Fiscinae) auf
dem Pressbett und trägt rechts zwischen zwei Pfosten einen hängenden Gegengewichtsstein; davor Rinne und Absetzbecken mit
goldenem Öl, Körbe mit Oliven, Amphoren an der Rückwand. Vor dem Haus steht ein Trapetum (runder Steintrog, in dem zwei
Mahlsteine an einem Balken die Oliven zu Paste walzen), daneben Dolien, Fässer und Amphoren voller Öl, ein Handkarren
und Körbe mit Oliven.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/oelpresse.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Oelpresse_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(52)

# ---------------------------------------------------------------- Pressenhaus
X0, X1, Y0, Y1 = -1.67, 1.67, 0.34, 1.67
HH = 1.16
W = 0.12                                                    # Wandstärke
box((X1 - X0, W, HH), (0, Y1 - W / 2, HH / 2), material=LIME)                                  # Rückwand
box((W, Y1 - Y0, HH), (X0 + W / 2, (Y0 + Y1) / 2, HH / 2), material=LIME)                      # linke Wand
box((W, Y1 - Y0, HH), (X1 - W / 2, (Y0 + Y1) / 2, HH / 2), material=LIME)                      # rechte Wand
box((X1 - X0 + 0.04, Y1 - Y0 + 0.04, 0.09), (0, (Y0 + Y1) / 2, 0.045), material=STONE)         # Sockel
brick_bands(X0, X1, Y1 - W, Y1, (0.30, 0.58, 0.86), faces=("+y",))
brick_bands(X0, X0 + W, Y0, Y1, (0.30, 0.58, 0.86), faces=("-x",))
brick_bands(X1 - W, X1, Y0, Y1, (0.30, 0.58, 0.86), faces=("+x",))
brick_bands(X0 + W, X1 - W, Y1 - W, Y1 - W, (0.30, 0.58, 0.86), faces=("-y",))                  # innen an der Rückwand
box((X1 - X0, W, 0.12), (0, Y1 - W / 2, 0.06), material=LIME_DARK)
# Pfeiler, Sturz und Vorderwand-Reste
for sx in (-1, 1):
    box((0.22, 0.22, 1.04), (sx * 1.55, Y0 + 0.08, 0.52), material=LIME)
    brick_bands(sx * 1.55 - 0.11, sx * 1.55 + 0.11, Y0 - 0.03, Y0 + 0.19, (0.24, 0.52, 0.80), faces=("-y",))
    box((0.26, 0.26, 0.07), (sx * 1.55, Y0 + 0.08, 0.035), material=LIME_DARK)
    box((0.26, 0.26, 0.05), (sx * 1.55, Y0 + 0.08, 1.065), material=LIME_DARK)
box((X1 - X0, 0.15, 0.13), (0, Y0 + 0.07, 1.00), material=WOOD_DARK)                           # Holzsturz
for k in range(-6, 7):
    box((0.012, 0.16, 0.13), (k * 0.24, Y0 + 0.07, 1.00), material=WOOD)                       # Fugen im Sturz
for sx in (-1, 1):
    bar((sx * 1.36, Y0 + 0.07, 0.86), (sx * 1.18, Y0 + 0.07, 1.00), 0.06, WOOD)                # Kopfbänder
ridge = gable_roof(X0, X1, Y0, Y1, HH, "x", LIME, overhang=0.09, pitch=22)

# ---------------------------------------------------------------- Balkenpresse im Haus
lever_press(1.50, 0.05, 0.55, z=0.62, u_press=0.95, axis="y")
settling_basin(0.06, 0.98, r=0.17)
box((0.20, 0.07, 0.012), (0.20, 0.96, 0.060), material=PASTE)                                  # Ablauf zur Rinne
for k in range(5):                                                                              # Öl-Spuren
    sphere(0.012, (0.12 + k * 0.03, 0.86 + k * 0.02, 0.08), (1.2, 1.0, 0.5), OIL, segs=5, rings=3)
olive_basket(-0.40, 0.72, 0.0, r=0.085, seed=7)
olive_basket(-0.15, 0.62, 0.0, r=0.08, seed=8)
olives_scatter(-0.28, 0.66, 0.5, 0.3, 9, seed=3)
dolium(-1.20, 0.66, h=0.38, r=0.16)
dolium(-0.82, 0.62, h=0.30, r=0.13)
for x in (-0.10, -0.40, -0.70):                                                                   # Amphoren an der Rückwand
    amphora(x, 1.50)
amphora(-1.38, 1.50)
amphora(1.30, 1.50)
barrel(1.40, 1.45, r=0.075)

# ---------------------------------------------------------------- Trapetum vor dem Haus
trapetum(-0.72, -0.62, yaw=8)

# ---------------------------------------------------------------- Handkarren, Körbe, Dolien, Amphoren
cart(1.15, -0.72, yaw=14, load=None)
cat = placed((1.15, -0.72), 14)
c = cat((0.0, 0.0, 0.0))
sphere(0.12, (c[0], c[1], 0.235), (1.15, 1.75, 0.58), OLIVE_FRUIT, segs=8, rings=4, rot=(0, 0, 14))
for k in range(6):
    q = cat((random.uniform(-0.07, 0.07), random.uniform(-0.12, 0.12), 0.0))
    sphere(0.014, (q[0], q[1], 0.27), material=OLIVE_FRUIT if k % 3 else OLIVE_GREEN, segs=5, rings=3)
olive_basket(0.25, -0.95, 0.0, r=0.085, seed=9)
olive_basket(0.50, -0.85, 0.0, r=0.07, seed=10)
olives_scatter(0.15, -0.15, 0.5, 0.25, 8, seed=4)
dolium(-1.45, 0.08, h=0.40, r=0.17)
dolium(-1.10, 0.06, h=0.34, r=0.145)
for x, y in ((1.35, 0.07), (1.52, 0.10), (1.43, -0.12)):
    amphora(x, y)
barrel(0.90, 0.10, r=0.075)
barrel(0.72, 0.06, r=0.07, lying=True, yaw=20)
amphora(-0.20, -1.55)
amphora(0.02, -1.58)
for gx, gy in ((-1.62, -1.5), (1.58, -1.50), (-1.55, -0.75)):
    grass_tuft(gx, gy, 0.09, seed=int(abs(gx * 10)))
pot_plant(-1.5, -1.25, 0.06, OLIVE_LEAF)

obj = variant("Oelpresse_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
