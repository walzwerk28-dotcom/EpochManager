"""
Werkstatthaus der Handwerkerzone (Antike), 1 x 1 Feld (2 x 2 m), zwei Varianten (wie die früheren Kenney-Modelle:
Variante 0 = Laden links, Tür rechts; Variante 1 = Tür links, anderes Dach):
  Variante 0: Schuster. Zweistöckiges rotes Haus mit Giebel zur Straße: unten Schusterladen mit Markise und Schuhen auf
              der Theke, Tür mit Lederschild; davor eine Schusterbank mit Leisten, Hammer, Schuhen und Hocker.
  Variante 1: Weber. Zweistöckiges gelbes Haus mit Traufe zur Straße: unten Laden mit Markise und Stoffballen, Tür; oben
              hängen bunte Stoffbahnen an einer Stange zwischen den Fenstern; davor ein Gewichtswebstuhl und ein
              Hocker mit Garnknäueln.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/werkstatthaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 1,84 x 1,84 m (Grundfläche 2 x 2 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Werkstatthaus_0, Werkstatthaus_1).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(301)

# ---------------------------------------------------------------- Variante 0: Schuster

X0, X1, Y0, Y1, H = -0.76, 0.76, -0.40, 0.80, 1.95
body(X0, X1, Y0, Y1, H, PLASTER_RED, socle=None)
light_gable_roof(X0, X1, Y0, Y1, H, "y", PLASTER_RED, overhang=0.07)
cornice(X0, X1, Y0, Y1, STOREY)
cx, cy = shopfront_open("-y", Y0, -0.32, w=0.60, awning=(CLOTH_YELLOW, CLOTH))
for k, yaw in enumerate((20, -15, 10, -25)):                      # Schuhe auf der Theke
    shoe(cx - 0.18 + k * 0.12, cy + (0.02 if k % 2 else -0.02), 0.33, yaw=90 + yaw, material=LEATHER if k % 2 else LEATHER_DARK)
door("-y", Y0, 0.38)
sign_board("-y", Y0, 0.38, 0.86, w=0.16, h=0.10, arm=0.15, board=LEATHER, mark="bar", mark_mat=CLOTH_YELLOW)
for u in (-0.32, 0.38):
    window("-y", Y0, u, STOREY + 0.28, shutters=PAINT_BLUE, open_shutters=(u < 0))
for face, plane in (("-x", X0), ("+x", X1)):
    window(face, plane, -0.10, 0.42, shutters=PAINT_BLUE)
    window(face, plane, 0.35, 0.42, shutters=PAINT_BLUE, open_shutters=False)
    window(face, plane, 0.12, STOREY + 0.28, shutters=PAINT_BLUE, open_shutters=(face == "-x"))
door("+y", Y1, 0.30)
window("+y", Y1, -0.30, 0.42, shutters=PAINT_BLUE)
for u in (-0.30, 0.30):
    window("+y", Y1, u, STOREY + 0.28, shutters=PAINT_BLUE, open_shutters=(u > 0))
# Schusterbank vor der Tür
trestle_table(0.45, -0.70, 0, w=0.46, d=0.20, h=0.26)
box((0.30, 0.14, 0.008), (0.42, -0.70, 0.264), material=LEATHER)                       # Lederstück
shoe(0.52, -0.69, 0.268, yaw=75, material=LEATHER_DARK)
box((0.030, 0.016, 0.050), (0.34, -0.72, 0.293), rot=(0, 0, 20), material=WOOD)           # Leisten
box((0.050, 0.016, 0.020), (0.36, -0.72, 0.268 + 0.010), rot=(0, 0, 20), material=WOOD)
rod((0.30, -0.66, 0.272), (0.30, -0.66, 0.36), 0.008, WOOD_DARK, verts=5)                  # Hammer
box((0.040, 0.016, 0.020), (0.30, -0.66, 0.37), material=IRON)
stool(0.72, -0.60)
shoe(0.12, -0.60, 0.0, yaw=30)
shoe(0.17, -0.55, 0.0, yaw=-20, material=LEATHER_DARK)
bpy.context.view_layer.update()
check_bounds(0.92, 0.92, "0")
variant("Werkstatthaus_0")

# ---------------------------------------------------------------- Variante 1: Weber

X0, X1, Y0, Y1, H = -0.76, 0.76, -0.20, 0.82, 1.95
body(X0, X1, Y0, Y1, H, PLASTER_YELLOW, socle=SOCLE_RED)
light_gable_roof(X0, X1, Y0, Y1, H, "x", PLASTER_YELLOW, overhang=0.04)
cornice(X0, X1, Y0, Y1, STOREY)
cx, cy = shopfront_open("-y", Y0, -0.42, w=0.58, awning=(CLOTH_BLUE, CLOTH))
for k, cols in enumerate(((CLOTH_RED, CLOTH), (CLOTH_BLUE, CLOTH_YELLOW), (CLOTH_PURPLE, CLOTH))):
    cloth_bale(cx - 0.15 + k * 0.15, cy, 0.33, yaw=(k - 1) * 6, colors=cols)
door("-y", Y0, 0.50, w=0.32)
sign_board("-y", Y0, 0.50, 0.86, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=CLOTH_PURPLE)
for u in (-0.50, 0.50):
    window("-y", Y0, u, STOREY + 0.30, shutters=PAINT_GREEN, open_shutters=(u < 0))
wall_cloths("-y", Y0, -0.26, 0.26, 1.74, 0.52, (CLOTH_RED, CLOTH_YELLOW, CLOTH_BLUE))
for face, plane in (("-x", X0), ("+x", X1)):
    window(face, plane, 0.05, 0.42, shutters=PAINT_GREEN, open_shutters=(face == "+x"))
    window(face, plane, 0.50, 0.42, shutters=PAINT_GREEN, open_shutters=False)
    window(face, plane, 0.28, STOREY + 0.30, shutters=PAINT_GREEN, open_shutters=(face == "-x"))
door("+y", Y1, -0.30)
window("+y", Y1, 0.30, 0.42, shutters=PAINT_GREEN)
for u in (-0.30, 0.30):
    window("+y", Y1, u, STOREY + 0.30, shutters=PAINT_GREEN, open_shutters=(u < 0))
loom(0.10, -0.62, 0, cloth=CLOTH_BLUE, stripe=CLOTH_RED, h=0.74)
stool(-0.72, -0.64)
for k, m in enumerate((CLOTH_RED, CLOTH_YELLOW, CLOTH_PURPLE)):
    sphere(0.040, (-0.72 + (k - 1) * 0.045, -0.64 + (0.02 if k == 1 else -0.02), 0.17 + 0.036), (1, 1, 0.95), m, segs=6, rings=4)
basket_goods(-0.45, -0.82, 0.0, 0.065, "grain", seed=5)
bpy.context.view_layer.update()
check_bounds(0.92, 0.92, "1")
variant("Werkstatthaus_1")

export(spacing=2.6)
