"""
Großes Lager (Antike), 3 x 3 Felder (6 x 6 m): Speicher mit Innenhof wie die Horrea in Ostia. Vier Flügel aus Kalkstein
mit Ziegelbändern und Satteldächern umschließen einen Hof; zur Straße ein höherer Torbau mit Giebel, Pilastern,
Inschriftentafel und großem Bohlentor, davor zwei Säulen; außen Strebepfeiler, Lüftungsschlitze und Eckpilaster; im Hof
Kammertüren rundum, Zisterne, zwei Handkarren, Stapel aus Säcken, Kisten, Fässer und Reihen von Amphoren.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/lager_gross.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 5,52 x 5,52 m (Grundfläche 6 x 6 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (LagerGross_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(111)

X0, X1, Y0, Y1 = -2.55, 2.55, -2.20, 2.55
D = 1.15
CX0, CX1, CY0, CY1 = X0 + D, X1 - D, Y0 + D, Y1 - D
H = 1.45
wings = ((X0, X1, Y0, CY0), (X0, X1, CY1, Y1), (X0, CX0, CY0, CY1), (CX1, X1, CY0, CY1))
for a0, a1, b0, b1 in wings:
    body(a0, a1, b0, b1, H, LIME)
    box((a1 - a0 + 0.05, b1 - b0 + 0.05, 0.20), ((a0 + a1) / 2, (b0 + b1) / 2, 0.10), material=LIME_DARK)
gable_roof(X0, X1, Y0, CY0, H, "x", LIME, overhang=0.07, pitch=22)
gable_roof(X0, X1, CY1, Y1, H, "x", LIME, overhang=0.07, pitch=22)
gable_roof(X0, CX0, (Y0 + CY0) / 2, (CY1 + Y1) / 2, H, "y", LIME, overhang=0.0, pitch=22)
gable_roof(CX1, X1, (Y0 + CY0) / 2, (CY1 + Y1) / 2, H, "y", LIME, overhang=0.0, pitch=22)
brick_bands(X0, X1, Y0, Y1, (0.50, 0.95))
for (x, y) in ((X0, Y0), (X1, Y0), (X0, Y1), (X1, Y1)):
    box((0.14, 0.14, H - 0.04), (x, y, (H - 0.04) / 2), material=LIME_DARK)
# außen: Strebepfeiler und Schlitze an Seiten und Rückseite
for face, plane in (("-x", X0), ("+x", X1)):
    for u in (-1.40, -0.20, 1.00, 2.10):
        buttress(face, plane, u, 1.10)
    for u in (-1.80, -0.80, 0.40, 1.55):
        slit(face, plane, u, 0.58, h=0.28)
for u in (-1.90, -0.70, 0.50, 1.70):
    buttress("+y", Y1, u, 1.10)
for u in (-2.30, -1.30, -0.10, 1.10, 2.25):
    slit("+y", Y1, u, 0.58, h=0.28)
for u in (-2.00, -1.30, 1.30, 2.00):
    slit("-y", Y0, u, 0.58, h=0.28)

# Torbau zur Straße
GX0, GX1, GY0 = -0.80, 0.80, -2.45
body(GX0, GX1, GY0, CY0, 2.05, LIME)
box((GX1 - GX0 + 0.05, CY0 - GY0 + 0.05, 0.20), (0.0, (GY0 + CY0) / 2, 0.10), material=LIME_DARK)
gable_roof(GX0, GX1, GY0, CY0 + 0.3, 2.05, "y", LIME, overhang=0.06, pitch=24)
brick_bands(GX0, GX1, GY0, CY0, (0.50, 0.95, 1.55), faces=("-y", "-x", "+x"))
door("-y", GY0, 0.0, w=0.74, h=1.05, z0=0.20, leaf=WOOD_DARK)
box((0.008, 0.008, 1.0), (0.0, GY0 - 0.026, 0.72), material=WOOD_DARK)
for s in (-1, 1):
    box((0.12, 0.05, 1.70), (s * 0.66, GY0 - 0.025, 0.85), material=LIME_DARK)          # Pilaster
    column(s * 0.66, GY0 - 0.20, 1.55, r=0.05)
box((1.50, 0.30, 0.10), (0.0, GY0 - 0.12, 1.60), material=LIME)                         # Gebälk über den Säulen
box((0.70, 0.02, 0.18), (0.0, GY0 - 0.01, 1.42), material=LIME_DARK)                    # Inschriftentafel
for k in range(3):
    box((0.50 - k * 0.12, 0.006, 0.012), (0.0, GY0 - 0.022, 1.47 - k * 0.04), material=SHADOW if "SHADOW" in globals() else STONE)
cyl(0.11, 0.01, (0.0, GY0 - 0.005, 1.90), rot=(90, 0, 0), material=TILE_DARK, verts=16)
cyl(0.075, 0.012, (0.0, GY0 - 0.008, 1.90), rot=(90, 0, 0), material=DARK, verts=16)
# Hof: Kammertüren rundum, Torweg, Zisterne, Karren und Waren
for face, plane, us in (("+y", CY0, (-1.10, -0.55, 0.55, 1.10)), ("-y", CY1, (-1.10, -0.40, 0.40, 1.10)),
                        ("+x", CX0, (-0.60, 0.15, 0.90)), ("-x", CX1, (-0.60, 0.15, 0.90))):
    for u in us:
        door(face, plane, u, w=0.30, h=0.66, leaf=WOOD)
box((0.70, 0.012, 1.0), (0.0, CY0 + 0.006, 0.60), material=DARK)                        # Torweg zum Hof
cistern(0.0, 0.35)
cart(-0.70, -0.45, yaw=-90, load="sacks")
cart(0.85, 0.95, yaw=180, load="hay")
for x, y in ((-1.10, 1.05), (-0.95, 1.12), (-1.05, 0.92), (-0.85, 0.98)):
    sack(x, y, 0.0, 0.065)
sack(-1.00, 1.02, 0.13, 0.06)
for k in range(5):
    amphora(-1.20 + k * 0.11, -0.85)
for x, y in ((0.95, -0.80), (1.12, -0.82), (1.04, -0.65)):
    barrel(x, y, r=0.07)
crate(0.55, -0.85, size=0.20, yaw=8)
crate(0.55, -0.85, z0=0.20, size=0.16, yaw=-10)
crate(1.15, 0.20, size=0.18, yaw=20)
# vor dem Tor
for x in (-1.15, -1.32):
    barrel(x, -2.48, r=0.07)
barrel(1.20, -2.48, 0.0, r=0.07, lying=True, yaw=0)
crate(1.45, -2.45, size=0.18, yaw=5)
variant("LagerGross_0")

export(spacing=6.4)
