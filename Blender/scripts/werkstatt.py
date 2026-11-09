"""
Werkstatt der Gewerbezone (Antike), 2 x 2 Felder (4 x 4 m), zwei Varianten (Reihenfolge wie die früheren Kenney-Modelle:
Variante 0 = Werkhalle mit Werkhof, Variante 1 = Werkstatthaus mit Laden und Anbau). Die Werkstatt verkauft Weizen, Öl
und Keramik; die Auslagen zeigen das.
  Variante 0: offene Werkhalle hinten (Pfosten, Ziegeldach, Werkbank, Werkzeugwand, Regal), davor ein ummauerter
              Werkhof mit zwei Verkaufstischen (Töpferware; Getreidekörbe und Säcke), Amphoren für Öl, Fässer, Kisten,
              Handkarren.
  Variante 1: zweistöckiges Haus mit Ladenöffnung, Markise, Tür und Schild; an der Seite ein Anbau mit Pultdach und
              großer Werkstatttür, davor Werkbank, Auslage und Fässer; hinten ein Hof mit offenem Schuppen.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/werkstatt.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 3,68 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Werkstatt_0, Werkstatt_1).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(73)


def small_shelf(x, y, yaw=0.0, w=0.80, tiers=2, fired=True, seed=0, d=0.15):
    """Schlichtes Regal mit wenigen Gefäßen (Dreiecke sparen)."""
    rnd = random.Random(seed)
    at = placed((x, y), yaw)

    def P(u, v, z):
        q = at((u, v, 0.0))
        return (q[0], q[1], z)
    hh = 0.22 * tiers + 0.06
    for su in (-1, 1):
        for sv in (-1, 1):
            c = P(su * (w / 2 - 0.015), sv * (d / 2 - 0.015), 0.0)
            box((0.03, 0.03, hh), (c[0], c[1], hh / 2), rot=(0, 0, yaw), material=WOOD_DARK)
    for t in range(tiers):
        z = 0.16 + t * 0.22
        box((w, d, 0.022), P(0.0, 0.0, z), rot=(0, 0, yaw), material=WOOD)
        n = max(2, int(w / 0.26))
        for i in range(n):
            u = -w / 2 + (i + 0.5) * w / n
            c = P(u, rnd.uniform(-0.01, 0.01), 0.0)
            mat_v = TERRACOTTA if fired else CLAY_UNFIRED
            if (i + t) % 2 == 0:
                jug(c[0], c[1], z + 0.011, 0.034, mat_v)
            else:
                bowl(c[0], c[1], z + 0.011, 0.056, mat_v)


# ---------------------------------------------------------------- Variante 0: Werkhalle mit Werkhof

X0, X1, Y0, Y1, H = -1.70, 1.70, 0.30, 1.72, STOREY
# Halle: Rückwand und Seitenwände aus Putz, vorn offen auf Pfosten
body(X0, X1, Y1 - 0.12, Y1, H, PLASTER_OCHRE, socle=SOCLE_RED)
body(X0, X0 + 0.12, Y0, Y1, H, PLASTER_OCHRE, socle=SOCLE_RED)
body(X1 - 0.12, X1, Y0, Y1, H, PLASTER_OCHRE, socle=SOCLE_RED)
gable_roof(X0, X1, Y0, Y1, H, "x", PLASTER_OCHRE, overhang=0.08)
for x in (X0 + 0.06, -0.55, 0.55, X1 - 0.06):
    box((0.07, 0.07, H), (x, Y0 + 0.07, H / 2), material=WOOD_DARK)
box((X1 - X0, 0.08, 0.08), (0.0, Y0 + 0.07, H - 0.04), material=WOOD_DARK)                  # Unterzug
for x in (-1.12, 0.0, 1.12):
    bar((x, Y0 + 0.07, H - 0.30), (x - 0.0, Y0 + 0.07, H - 0.04), 0.0001, WOOD_DARK) if False else None
# Halle innen: Werkbank, Werkzeugwand, Regal, Hocker
tool_wall(-1.50, -0.60, Y1 - 0.12, 0.30, 0.88)
small_shelf(0.70, Y1 - 0.24, 0, w=1.00, tiers=2, fired=True, seed=3)
workbench(-0.40, 1.00, yaw=90)
stool(-0.95, 1.02)
barrel(1.40, 0.62, r=0.07)
sack(-1.45, 0.52, 0.0, 0.07)
crate(1.38, 1.44, 0.0, 0.16, yaw=6)
# Werkhof: niedrige Mauer mit Toröffnung vorn
court_wall([(-1.66, Y0 - 0.02), (-1.66, -1.72), (-0.62, -1.72)], h=0.30, t=0.08, plaster=PLASTER_OCHRE)
court_wall([(0.62, -1.72), (1.66, -1.72), (1.66, Y0 - 0.02)], h=0.30, t=0.08, plaster=PLASTER_OCHRE)
for x in (-0.62, 0.62):
    box((0.12, 0.12, 0.50), (x, -1.72, 0.25), material=LIME_DARK)
    box((0.16, 0.16, 0.04), (x, -1.72, 0.52), material=LIME)
# Verkaufstische (links Keramik, rechts Weizen)
trestle_table(-0.95, -0.55, 0, w=1.10, d=0.30, h=0.30, cloth=CLOTH_OCHRE)
for i, (px, kind) in enumerate(((-1.35, "pot"), (-1.15, "bowl"), (-0.95, "pot"), (-0.75, "bowl"), (-0.55, "pot"))):
    if kind == "pot":
        pot(px, -0.55, 0.30, 0.040)
    else:
        bowl(px, -0.55, 0.30, 0.052, TERRACOTTA_LIGHT)
trestle_table(0.95, -0.55, 0, w=1.10, d=0.30, h=0.30, cloth=CLOTH)
basket_goods(0.62, -0.55, 0.30, 0.060, "grain", seed=1)
basket_goods(0.86, -0.55, 0.30, 0.060, "grain", seed=2)
sack(1.12, -0.55, 0.30, 0.060)
sack(1.30, -0.55, 0.30, 0.055)
# Öl: Amphoren am Gestell
for k, (ax, ay) in enumerate(((-0.10, -1.05), (0.10, -1.08), (0.0, -0.92))):
    amphora(ax, ay)
bar((-0.22, -1.20, 0.0), (-0.22, -1.20, 0.26), 0.03, WOOD_DARK) if False else None
# Boden: Körbe, Fässer, Kisten, Karren
cart(1.28, -1.28, yaw=-30, load="sacks")
barrel(-1.50, -1.40, r=0.075)
barrel(-1.32, -1.50, r=0.065, lying=True, yaw=20)
crate(-0.55, -1.40, 0.0, 0.16, yaw=-8)
crate(-0.55, -1.40, 0.16, 0.13, yaw=14)
basket_goods(-1.00, -1.35, 0.0, 0.075, "grain", seed=4)
pot(0.62, -1.50, 0.0, 0.050)
pot(0.80, -1.55, 0.0, 0.042, TERRACOTTA_LIGHT)
pot_plant(-1.50, -0.10, 0.055)
for u in (-1.10, 0.0, 1.10):
    window("+y", Y1, u, 0.45, shutters=PAINT_GREEN, open_shutters=(u != 0.0))
sack(1.45, -1.05, 0.0, 0.07)
sack(1.52, -0.85, 0.0, 0.065)
stool(-1.45, -0.75)
basket_goods(-0.25, -0.50, 0.0, 0.07, "grain", seed=9)
variant("Werkstatt_0")

# ---------------------------------------------------------------- Variante 1: Werkstatthaus mit Laden und Anbau

AX0, AX1, AY0, AY1, AH = -1.70, 0.20, -1.15, 0.55, 2 * STOREY
body(AX0, AX1, AY0, AY1, AH, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(AX0, AX1, AY0, AY1, AH, "y", PLASTER_WHITE, overhang=0.07)
cornice(AX0, AX1, AY0, AY1, STOREY)
shop_front("-y", AY0, -1.10, w=0.70, goods=("jug", "bread", "jug"), awning=(CLOTH_RED, CLOTH_OCHRE))
door("-y", AY0, -0.38)
sign_board("-y", AY0, -0.38, 0.80, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="bar", mark_mat=IRON)
for u in (-1.10, -0.38):
    window("-y", AY0, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=(u < -1))
window("-y", AY0, -0.75, AH + 0.08, w=0.12, h=0.10, shutters=None)
window("-x", AX0, -0.45, 0.42, shutters=PAINT_GREEN)
window("-x", AX0, -0.45, STOREY + 0.38, shutters=PAINT_GREEN)
window("-x", AX0, 0.25, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=False)
window("+y", AY1, -1.10, 0.42, shutters=PAINT_GREEN)
window("+y", AY1, -1.10, STOREY + 0.38, shutters=PAINT_GREEN)
door("+y", AY1, -0.38)
# Anbau mit Pultdach und großer Werkstatttür
BX0, BX1, BY0, BY1 = 0.20, 1.70, -0.60, 0.95
lean_to(BX0, BX1, BY0, BY1, 0.74, "+x", PLASTER_OCHRE, pitch=14, socle=SOCLE_RED)
door("-y", BY0, 0.95, w=0.56, h=0.56, open_angle=75, leaf=WOOD)
window("+x", BX1, 0.15, 0.42, shutters=PAINT_GREEN, open_shutters=False)
window("+x", BX1, 0.62, 0.42, shutters=PAINT_GREEN)
box((0.20, 0.20, 0.55), (0.60, 0.55, 1.10), material=STONE)                       # Schornstein am Haus
box((0.26, 0.26, 0.05), (0.60, 0.55, 1.40), material=LIME_DARK)
box((0.14, 0.14, 0.02), (0.60, 0.55, 1.44), material=DARK)
# vor dem Anbau: Werkbank, Auslage, Fässer
workbench(1.38, -1.15, yaw=90)
trestle_table(0.80, -1.42, 0, w=0.62, d=0.26, h=0.28, cloth=CLOTH_OCHRE)
pot(0.62, -1.42, 0.28, 0.038)
bowl(0.80, -1.42, 0.28, 0.050, TERRACOTTA_LIGHT)
pot(0.96, -1.42, 0.28, 0.034)
basket_goods(0.40, -0.98, 0.0, 0.070, "grain", seed=5)
amphora(0.55, -0.80)
barrel(1.62, -1.55, r=0.07)
crate(1.28, -1.62, 0.0, 0.15, yaw=5)
sack(-1.58, -1.45, 0.0, 0.07)
basket_goods(-0.80, -1.50, 0.0, 0.075, "apples", seed=6) if False else None
stool(-0.62, -1.50)
# Hof hinten mit offenem Schuppen
fence([(-1.6825, 0.56), (-1.6825, 1.74), (1.6825, 1.74), (1.6825, 0.95)], h=0.28)
open_shed(-1.50, -0.20, 1.12, 1.66, 0.62)
log_pile(-0.85, 1.40, 0.50, r=0.06, rows=(3, 2), yaw=0, posts=False) if False else None
beam_stack(-0.85, 1.40, 0.50, beam=0.06, rows=(3, 2), yaw=90)
cart(0.50, 1.40, yaw=90, load="sacks")
pot(1.40, 1.30, 0.0, 0.05)
pot(1.55, 1.45, 0.0, 0.042, TERRACOTTA_LIGHT)
woodpile(-1.35, 0.85, length=0.40, rows=(4, 3), yaw=0)
variant("Werkstatt_1")

export(spacing=4.2)
