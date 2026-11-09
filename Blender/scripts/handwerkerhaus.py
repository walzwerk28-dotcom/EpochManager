"""
Handwerkerhaus der Handwerkerzone (Antike), 1 x 2 Felder (2 m Front, 4 m tief), zwei Varianten (wie die früheren
Kenney-Modelle: Variante 0 = Haus an der Straße mit offenem Werkschuppen dahinter, Variante 1 = tieferes Haus mit Werkhof):
  Variante 0: Böttcher. Zweistöckiges ockerfarbenes Haus mit Traufe zur Straße, Laden mit Markise und kleinen Fässern
              auf der Theke, Tür, Fässer vor dem Haus; Hof zwischen Hofmauern mit Fass im Aufbau (Dauben und Reifen),
              Fasspyramide und Brennholz; hinten ein offener Werkschuppen mit Pultdach, Werkbank, Werkzeugwand und
              fertigen Fässern.
  Variante 1: Wagner. Tieferes zweistöckiges grünes Haus mit Giebel zur Straße, Laden mit Markise, Tür, Räder zur
              Schau vor dem Haus; dahinter ein Werkhof zwischen Hofmauern mit Bohlenstapeln, Sägebock mit Stamm, liegenden
              Rädern, Handkarren und Brennholz, Rückwand mit Fenstern.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/handwerkerhaus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 1,84 x 3,68 m (Grundfläche 2 x 4 m), Vorderseite (Schmalseite) nach -Y. Die FBX enthält je Variante
ein Objekt (Handwerkerhaus_0, Handwerkerhaus_1). Alle Hofmauern liegen mit der Außenfläche in der Ebene der Hauswand
(x = +-W), an die sie anschließen, und enden 1,5 cm im Hauskörper.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(311)

W = 0.83              # Außenfläche der Seitenwände (Mitte im Ursprung)
EMB = 0.015
WT = 0.09
YB = 1.70             # Rückwand des Hofs

# ---------------------------------------------------------------- Variante 0: Böttcher

Y0, Y1, H = -1.45, -0.55, 1.95
body(-W, W, Y0, Y1, H, PLASTER_OCHRE, socle=SOCLE_RED)
light_gable_roof(-W, W, Y0, Y1, H, "x", PLASTER_OCHRE, overhang=0.03)
cornice(-W, W, Y0, Y1, STOREY)
cx, cy = shopfront_open("-y", Y0, -0.38, w=0.60, awning=(CLOTH_RED, CLOTH))
for k in range(3):
    barrel(cx - 0.16 + k * 0.16, cy, 0.33, r=0.036)
door("-y", Y0, 0.46)
sign_board("-y", Y0, 0.46, 0.86, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=IRON)
for u in (-0.38, 0.46):
    window("-y", Y0, u, STOREY + 0.28, shutters=WOOD, open_shutters=(u < 0))
for face, plane in (("-x", -W), ("+x", W)):
    window(face, plane, -0.80, 0.42, shutters=WOOD, open_shutters=(face == "-x"))
    window(face, plane, -0.80, STOREY + 0.28, shutters=WOOD, open_shutters=False)
door("+y", Y1, -0.40)
window("+y", Y1, 0.30, 0.42, shutters=WOOD)
for u in (-0.40, 0.30):
    window("+y", Y1, u, STOREY + 0.28, shutters=WOOD, open_shutters=(u > 0))
barrel(0.70, Y0 - 0.16, 0.0, r=0.065)
barrel(0.70, Y0 - 0.30, 0.0, r=0.065)
# Hof: Hofmauern bündig mit den Hauswänden
flush_wall(-W, -W + WT, Y1 - EMB, 0.62, 0.40, PLASTER_OCHRE)
flush_wall(W - WT, W, Y1 - EMB, 0.62, 0.40, PLASTER_OCHRE)
# Werkschuppen hinten: Pultdach, fällt nach hinten
SY0, ZH = 0.62, 1.15
light_shed_roof(-W, W, SY0, YB, ZH, "+y", overhang=0.05, pitch=16.0, eave_overhang=0.05)
under = lambda y: ZH - (y - SY0) * math.tan(math.radians(16.0))
for s in (-1, 1):
    x0 = -W if s < 0 else W - WT
    slab([(SY0, 0.0), (YB, 0.0), (YB, under(YB) - 0.005), (SY0, ZH)], "yz", x0, WT, PLASTER_OCHRE)
body(-W, W, YB - WT, YB, under(YB) - 0.005, PLASTER_OCHRE, socle=None)
for x in (-W + 0.07, 0.0, W - 0.07):
    box((0.06, 0.06, ZH - 0.01), (x, SY0 + 0.07, (ZH - 0.01) / 2), material=WOOD_DARK)
box((2 * W, 0.06, 0.06), (0.0, SY0 + 0.07, ZH - 0.03), material=WOOD_DARK)
for u in (-0.45, 0.45):
    window("+y", YB, u, 0.36, w=0.16, h=0.16, shutters=None)
for face, plane in (("-x", -W), ("+x", W)):
    window(face, plane, 1.20, 0.38, w=0.16, h=0.16, shutters=None)
tool_wall(-0.80, 0.00, YB - WT, 0.28, 0.72)
workbench(0.40, 1.18, yaw=90)
for k in range(3):
    barrel(-0.55 + k * 0.17, 1.35, 0.0, r=0.07)
barrel(-0.64, 1.18, 0.0, r=0.07)
# Hof davor: Fass im Aufbau, Fasspyramide, Brennholz
barrel_raising(-0.34, 0.10)
barrel_pyramid(0.40, 0.02, r=0.07, yaw=0)
barrel(0.60, 0.30, 0.0, r=0.07)
woodpile(-0.10, 0.40, length=0.36, rows=(4, 3), yaw=0)
stool(-0.62, 0.18)
bpy.context.view_layer.update()
check_bounds(0.92, 1.84, "0")
variant("Handwerkerhaus_0")

# ---------------------------------------------------------------- Variante 1: Wagner

Y0, Y1, H = -1.45, 0.35, 1.95
body(-W, W, Y0, Y1, H, PLASTER_GREEN, socle=SOCLE_RED)
light_gable_roof(-W, W, Y0, Y1, H, "y", PLASTER_GREEN, overhang=0.03)
cornice(-W, W, Y0, Y1, STOREY)
shopfront_open("-y", Y0, -0.36, w=0.66, awning=(PAINT_BLUE, CLOTH))
door("-y", Y0, 0.46, w=0.34)
sign_board("-y", Y0, 0.46, 0.88, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=IRON)
for u in (-0.36, 0.46):
    window("-y", Y0, u, STOREY + 0.30, shutters=PAINT_BLUE, open_shutters=(u < 0))
for face, plane in (("-x", -W), ("+x", W)):
    for v in (-0.85, -0.15):
        window(face, plane, v, 0.42, shutters=PAINT_BLUE, open_shutters=(face == "-x" and v < -0.5))
        window(face, plane, v, STOREY + 0.30, shutters=PAINT_BLUE, open_shutters=(face == "+x"))
door("+y", Y1, 0.40)
window("+y", Y1, -0.40, 0.42, shutters=PAINT_BLUE)
for u in (-0.40, 0.40):
    window("+y", Y1, u, STOREY + 0.30, shutters=PAINT_BLUE, open_shutters=(u < 0))
# Räder zur Schau vor dem Haus
wheel_any(-0.58, Y0 - 0.27, 0.20, 0.20, "y", thick=0.03)
wheel_any(-0.14, Y0 - 0.24, 0.19, 0.19, "y", thick=0.03)
wheel_any(0.66, Y0 - 0.20, 0.04, 0.17, "z", thick=0.028)
# Hofmauern bündig
flush_wall(-W, -W + WT, Y1 - EMB, YB, 0.42, PLASTER_GREEN)
flush_wall(W - WT, W, Y1 - EMB, YB, 0.42, PLASTER_GREEN)
flush_wall(-W, W, YB - WT, YB, 0.64, PLASTER_GREEN)
for u in (-0.50, 0.50):
    window("+y", YB, u, 0.34, w=0.14, h=0.20, shutters=None)
# Werkhof: Bohlen, Sägebock, Räder, Karren
plank_stack(-0.43, 0.90, 0.24, 0.80, 5, yaw=90, seed=2)
plank_stack(-0.43, 1.34, 0.24, 0.80, 3, yaw=90, seed=3)
sawhorse(0.40, 0.70, yaw=90, log_len=0.8)
wheel_any(0.10, 1.40, 0.04, 0.17, "z", thick=0.028)
wheel_any(0.10, 1.40, 0.075, 0.17, "z", thick=0.028)
cart(0.55, 1.20, yaw=-20, load=None)
woodpile(-0.20, 0.55, length=0.36, rows=(4, 3), yaw=90)
stool(0.60, 0.50)
bpy.context.view_layer.update()
check_bounds(0.92, 1.84, "1")
variant("Handwerkerhaus_1")

export(spacing=4.0)
