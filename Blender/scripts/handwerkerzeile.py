"""
Handwerkerzeile der Handwerkerzone (Antike), 2 x 2 Felder (4 x 4 m), zwei Varianten (wie die früheren Kenney-Modelle:
Variante 0 = zwei Läden nebeneinander mit Vordächern und schmalem Hof dahinter, Variante 1 = ein drei- und ein
zweistöckiges Haus mit Werkhof dahinter):
  Variante 0: Bäcker (links, rot) und Töpferwaren (rechts, ocker), je zweistöckig mit Ladenöffnung, Markise, Tür und
              Schild. Vor dem Bäcker Brotkörbe, vor dem Töpfer ein Regal mit Gefäßen und Amphoren; hinten ein Hof mit
              Hofmauern: links Backofen mit Brennholz und Brotregal, rechts Gefäßregal mit ungebrannten Töpfen,
              Amphoren und Tisch; Trennmauer und Rückwand mit Fenstern.
  Variante 1: Gerber (links, dreistöckig, gelb, Giebel zur Straße) und Sattler (rechts, zweistöckig, rot, Traufe zur Straße);
              davor Lederwaren, Felle und Schuhe; dahinter der Werkhof der Gerberei: Fellrahmen, Gerbbottiche, Felle an
              einer Trockenstange, Fässer, Bank.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/handwerkerzeile.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 3,68 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Handwerkerzeile_0, Handwerkerzeile_1). Alle Hofmauern liegen mit der Außenfläche in der Ebene der Hauswand
(x = +-W bzw. y = YB), an die sie anschließen, und enden 1,5 cm im Hauskörper.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(321)

W = 1.77              # Außenfläche der Seitenwände
EMB = 0.015
WT = 0.09
YB = 1.74             # Rückwand des Hofs
Y0 = -1.30            # Vorderwand der Häuser (davor Platz für Markisen, Auslagen)
Y1 = -0.20            # Rückwand der Häuser
MID = 0.0             # Trennung der beiden Häuser (x)

# ---------------------------------------------------------------- Variante 0: Bäcker und Töpferwaren

H2 = 2 * STOREY - 0.05
# links: Bäcker (rot, Traufe zur Straße)
body(-W, MID - 0.02, Y0, Y1, H2, PLASTER_RED, socle=None)
light_gable_roof(-W, MID - 0.02, Y0, Y1, H2, "x", PLASTER_RED, overhang=0.03)
cornice(-W, MID - 0.02, Y0, Y1, STOREY)
cx, cy = shopfront_open("-y", Y0, -1.36, w=0.70, awning=(CLOTH_RED, CLOTH))
for k in range(4):
    loaf(cx - 0.21 + k * 0.14, cy + (0.02 if k % 2 else -0.02), 0.33, 20 * k)
door("-y", Y0, -0.42)
sign_board("-y", Y0, -0.42, 0.88, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=BREAD)
for u in (-1.36, -0.42):
    window("-y", Y0, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u < -1))
window("-x", -W, -0.60, 0.42, shutters=PAINT_GREEN)
window("-x", -W, -0.60, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=False)
for u in (-1.36, -0.42):
    window("+y", Y1, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u > -1))
door("+y", Y1, -0.90)
window("+y", Y1, -1.50, 0.42, shutters=PAINT_GREEN)
basket_goods(-1.60, Y0 - 0.38, 0.0, 0.075, "bread", seed=1)
basket_goods(-1.40, Y0 - 0.46, 0.0, 0.075, "bread", seed=2)
sack(-0.80, Y0 - 0.40, 0.0, 0.07)
sack(-0.64, Y0 - 0.34, 0.0, 0.06)
# rechts: Töpferwaren (ocker, Giebel zur Straße)
body(MID + 0.02, W, Y0, Y1, H2, PLASTER_OCHRE, socle=SOCLE_RED)
light_gable_roof(MID + 0.02, W, Y0, Y1, H2, "y", PLASTER_OCHRE, overhang=0.03)
cornice(MID + 0.02, W, Y0, Y1, STOREY)
cx, cy = shopfront_open("-y", Y0, 1.36, w=0.70, awning=(PAINT_BLUE, CLOTH))
for k in range(3):
    pot(cx - 0.20 + k * 0.20, cy, 0.33, 0.04) if k != 1 else bowl(cx, cy, 0.33, 0.055, TERRACOTTA_LIGHT)
door("-y", Y0, 0.42)
sign_board("-y", Y0, 0.42, 0.88, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=TERRACOTTA)
for u in (0.42, 1.36):
    window("-y", Y0, u, STOREY + 0.28, shutters=PAINT_BLUE, open_shutters=(u > 1))
window("+x", W, -0.60, 0.42, shutters=PAINT_BLUE)
window("+x", W, -0.60, STOREY + 0.28, shutters=PAINT_BLUE, open_shutters=False)
for u in (0.42, 1.36):
    window("+y", Y1, u, STOREY + 0.28, shutters=PAINT_BLUE, open_shutters=(u < 1))
door("+y", Y1, 0.90)
window("+y", Y1, 1.50, 0.42, shutters=PAINT_BLUE)
vessel_shelf(0.60, Y0 - 0.30, 0, w=0.60, tiers=2, fired=True, seed=3)
amphora_row((1.20, Y0 - 0.34), (1.60, Y0 - 0.34), 2)
# Hof
flush_wall(-W, -W + WT, Y1 - EMB, YB, 0.40, PLASTER_RED)
flush_wall(W - WT, W, Y1 - EMB, YB, 0.40, PLASTER_OCHRE)
flush_wall(-W, 0.0, YB - WT, YB, 0.60, PLASTER_RED)
flush_wall(0.0, W, YB - WT, YB, 0.60, PLASTER_OCHRE)
flush_wall(MID - WT / 2, MID + WT / 2, Y1 - EMB, YB - WT + EMB, 0.34, PLASTER_OCHRE)
for u in (-1.25, -0.55, 0.55, 1.25):
    window("+y", YB, u, 0.30, w=0.14, h=0.20, shutters=None)
oven(-1.15, 1.20, r=0.30)
woodpile(-0.60, 1.40, length=0.40, rows=(4, 3, 2), yaw=90)
box((0.30, 0.20, 0.04), (-0.90, 0.40, 0.17), material=WOOD)
for k in range(3):
    loaf(-0.98 + k * 0.08, 0.40, 0.19, 25 * k)
trestle_table(-1.10, 0.35, 0, w=0.70, d=0.28, h=0.28, cloth=CLOTH_OCHRE)
basket_goods(-1.30, 0.35, 0.286, 0.06, "bread", seed=4)
basket_goods(-0.92, 0.35, 0.286, 0.06, "bread", seed=5)
sack_stack(-0.62, 0.55, yaw=90, per=(2, 1))
vessel_shelf(1.00, YB - WT - 0.09, 0, w=0.90, tiers=2, fired=False, seed=6)
trestle_table(0.95, 0.35, 0, w=0.80, d=0.28, h=0.28, cloth=CLOTH)
for k, kind in enumerate(("pot", "bowl", "pot", "bowl")):
    px = 0.65 + k * 0.20
    pot(px, 0.35, 0.286, 0.04) if kind == "pot" else bowl(px, 0.35, 0.286, 0.05, TERRACOTTA_LIGHT)
amphora_row((1.45, 0.25), (1.45, 0.85), 2)
stool(0.55, 0.80)
bpy.context.view_layer.update()
plane_report("Zeile 0", (PLASTER_RED, PLASTER_OCHRE))
check_bounds(1.84, 1.84, "0")
variant("Handwerkerzeile_0")

# ---------------------------------------------------------------- Variante 1: Gerber und Sattler

H3 = 3 * STOREY - 0.05
# links: Gerber, dreistöckig, Giebel zur Straße
body(-W, MID - 0.02, Y0, Y1, H3, PLASTER_YELLOW, socle=SOCLE_RED)
light_gable_roof(-W, MID - 0.02, Y0, Y1, H3, "y", PLASTER_YELLOW, overhang=0.03)
cornice(-W, MID - 0.02, Y0, Y1, STOREY)
cornice(-W, MID - 0.02, Y0, Y1, 2 * STOREY)
cx, cy = shopfront_open("-y", Y0, -1.36, w=0.70, awning=(CLOTH_YELLOW, CLOTH))
for k in range(3):
    shoe(cx - 0.20 + k * 0.20, cy, 0.33, yaw=90 + 15 * (k - 1), material=LEATHER if k % 2 else LEATHER_DARK)
door("-y", Y0, -0.42)
sign_board("-y", Y0, -0.42, 0.88, w=0.16, h=0.10, arm=0.15, board=LEATHER, mark="bar", mark_mat=CLOTH_YELLOW)
for z in (STOREY + 0.28, 2 * STOREY + 0.28):
    for u in (-1.36, -0.42):
        window("-y", Y0, u, z, shutters=WOOD, open_shutters=((u < -1) == (z < 2)))
for z in (0.42, STOREY + 0.28, 2 * STOREY + 0.28):
    window("-x", -W, -0.60, z, shutters=WOOD, open_shutters=(z > 2))
for z in (STOREY + 0.28, 2 * STOREY + 0.28):
    for u in (-1.36, -0.42):
        window("+y", Y1, u, z, shutters=WOOD, open_shutters=(u > -1))
door("+y", Y1, -0.90)
window("+y", Y1, -1.50, 0.42, shutters=WOOD)
hide_frame(-1.00, Y0 - 0.34, yaw=0, w=0.40, h=0.42, hide=LEATHER)
box((0.30, 0.26, 0.05), (-1.62, Y0 - 0.34, 0.025), material=WOOD_DARK)                  # Fellstapel auf Palette
for k in range(3):
    box((0.28 - k * 0.02, 0.22, 0.026), (-1.62, Y0 - 0.34, 0.05 + 0.013 + k * 0.026), rot=(0, 0, k * 14), material=LEATHER if k % 2 else LEATHER_DARK)
# rechts: Sattler, zweistöckig, Traufe zur Straße
body(MID + 0.02, W, Y0, Y1, H2, PLASTER_RED, socle=None)
light_gable_roof(MID + 0.02, W, Y0, Y1, H2, "x", PLASTER_RED, overhang=0.03)
cornice(MID + 0.02, W, Y0, Y1, STOREY)
cx, cy = shopfront_open("-y", Y0, 1.36, w=0.70, awning=(PAINT_GREEN, CLOTH))
for k in range(3):
    box((0.11, 0.07, 0.05), (cx - 0.20 + k * 0.20, cy, 0.355), rot=(0, 0, 8 * (k - 1)), material=LEATHER if k != 1 else LEATHER_DARK)
door("-y", Y0, 0.42)
sign_board("-y", Y0, 0.42, 0.88, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=LEATHER)
for u in (0.42, 1.36):
    window("-y", Y0, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u > 1))
window("+x", W, -0.60, 0.42, shutters=PAINT_GREEN)
window("+x", W, -0.60, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=False)
for u in (0.42, 1.36):
    window("+y", Y1, u, STOREY + 0.28, shutters=PAINT_GREEN, open_shutters=(u < 1))
door("+y", Y1, 0.90)
window("+y", Y1, 1.50, 0.42, shutters=PAINT_GREEN)
hide_frame(1.00, Y0 - 0.34, yaw=0, w=0.40, h=0.42, hide=LEATHER_DARK)
stool(0.60, Y0 - 0.42)
shoe(0.58, Y0 - 0.46, 0.0, yaw=40)
# Werkhof der Gerberei
flush_wall(-W, -W + WT, Y1 - EMB, YB, 0.40, PLASTER_YELLOW)
flush_wall(W - WT, W, Y1 - EMB, YB, 0.40, PLASTER_RED)
flush_wall(-W, 0.0, YB - WT, YB, 0.60, PLASTER_YELLOW)
flush_wall(0.0, W, YB - WT, YB, 0.60, PLASTER_RED)
for u in (-1.25, -0.55, 0.55, 1.25):
    window("+y", YB, u, 0.30, w=0.14, h=0.20, shutters=None)
dye_vat(-1.15, 0.45, r=0.20, h=0.26, liquid=LEATHER_DARK, pole=True)
dye_vat(-0.60, 0.45, r=0.20, h=0.26, liquid=DYE_WELD, pole=False)
dye_vat(-1.15, 1.00, r=0.18, h=0.24, liquid=LEATHER_DARK, pole=False)
hide_frame(0.35, 0.40, yaw=0, w=0.42, h=0.44, hide=LEATHER)
hide_frame(0.95, 0.40, yaw=0, w=0.42, h=0.44, hide=LEATHER_DARK)
cloth_rail((-0.30, 1.10), (1.62, 1.10), 0.80, 0.46, (LEATHER, LEATHER_DARK, SACK), width=0.20, seed=3)
barrel(1.42, 0.50, 0.0, r=0.07)
barrel(1.58, 0.62, 0.0, r=0.07)
trough(-0.30, 1.52, 0, length=0.60, width=0.20, h=0.14, liquid=DYE_WELD)
bench(0.45, 1.58, yaw=0, length=0.5)
sack_stack(-0.55, 0.15, yaw=0, per=(2, 1))
bpy.context.view_layer.update()
plane_report("Zeile 1", (PLASTER_YELLOW, PLASTER_RED))
check_bounds(1.84, 1.84, "1")
variant("Handwerkerzeile_1")

export(spacing=4.4)
