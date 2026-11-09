"""
Werkstatthof der Gewerbezone (Antike), 3 x 3 Felder (6 x 6 m), zwei Varianten (Reihenfolge wie die früheren Kenney-Modelle:
Variante 0 = Reihe an der Straße mit Werkhof dahinter, Variante 1 = drei Flügel um einen Hof). Der Werkstatthof verkauft
Weizen, Öl und Keramik; Auslagen und Waren zeigen das.
  Variante 0: drei Werkstatthäuser Seite an Seite an der Straße (ocker einstöckig, weiß zweistöckig, rosa einstöckig mit
              Dach quer), jedes mit Ladenöffnung, Markise, Tür und Schild; dahinter ein gemeinsamer Werkhof zwischen
              Hofmauern: Töpferscheibe, Brennofen an der Rückwand, Verkaufstische mit Keramik, Regal, Amphoren,
              Fässer, Karren, Bretterstapel; hinten rechts eine offene Werkhalle mit Pultdach, Werkzeugwand, Werkbank.
  Variante 1: Tor zur Straße zwischen Mauerstücken; links und rechts offene Werkhallen (Pultdach nach außen fallend, Pfosten
              zum Hof), hinten ein zweistöckiges Haus mit Läden, Balkon und Werkzeugwand; im Hof eine Zisterne mit
              Trog, Karren, Verkaufstische, Fässer, Amphoren, Brennholz.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/werkstatthof.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 5,48 x 5,48 m (Grundfläche 6 x 6 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt
(Werkstatthof_0, Werkstatthof_1). Alle Hofmauern liegen mit der Außenfläche in der Ebene der Hauswand, an die sie
anschließen (x = +-W bzw. y = +-W) und enden 1,5 cm im Hauskörper.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(120)

W = 2.68              # Außenfläche der Wände (Mitte im Ursprung)
EMB = 0.015           # Mauerende im Hauskörper
WT = 0.10             # Stärke der Hofmauern
FY = -2.36            # Vorderwand der Häuser in Variante 0 (davor Platz für Theken und Markisen)


def wall_x(x_out, sign, y0, y1, h, plaster):
    """Mauer an der Seitenebene x = sign * W (Außenfläche), von y0 bis y1."""
    if sign < 0:
        flush_wall(-W, -W + WT, y0, y1, h, plaster)
    else:
        flush_wall(W - WT, W, y0, y1, h, plaster)


# ---------------------------------------------------------------- Variante 0: Reihe an der Straße mit Werkhof

YB = -1.05
# Häuser
body(-W, -0.92, FY, YB, 1.0, PLASTER_OCHRE, socle=SOCLE_RED)
gable_roof(-W, -0.92, FY, YB, 1.0, "y", PLASTER_OCHRE, overhang=0.04)
body(-0.92, 0.92, FY, YB, 1.0, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(-0.92, 0.92, FY, YB, 1.0, "y", PLASTER_WHITE, overhang=0.04)
body(0.92, W, FY, YB, 1.0, PLASTER_PINK, socle=None)
gable_roof(0.92, W, FY, YB, 1.0, "x", PLASTER_PINK, overhang=0.04)
# Straßenseite
shop_front("-y", FY, -2.15, w=0.64, goods=("jug", "bread", "jug"), awning=(CLOTH_RED, CLOTH))
door("-y", FY, -1.50)
sign_board("-y", FY, -1.50, 0.84, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="disc", mark_mat=TERRACOTTA)
window("-y", FY, -1.80, 1.10, w=0.12, h=0.10, shutters=None)
shop_front("-y", FY, -0.50, w=0.62, goods=("basket", "jug", "basket"), awning=(PAINT_BLUE, CLOTH))
door("-y", FY, 0.22, w=0.36, h=0.66)
sign_board("-y", FY, 0.22, 0.86, w=0.16, h=0.10, arm=0.15, board=WOOD, mark="bar", mark_mat=IRON)
for u in (-0.30, 0.30):
    window("-y", FY, u, 1.06, w=0.12, h=0.10, shutters=None)
shop_front("-y", FY, 1.45, w=0.62, goods=("basket", "bread", "jug"), awning=(CLOTH_OCHRE, CLOTH_RED))
door("-y", FY, 2.18)
# Seiten (außen)
for u in (-2.30, -1.60):
    window("-x", -W, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u > -2.0))
for u in (-2.30, -1.60):
    window("+x", W, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u < -2.0))
# Hofseite der Häuser
door("+y", YB, -1.30)
window("+y", YB, -2.10, 0.42, shutters=PAINT_GREEN)
for u in (-0.50, 0.50):
    window("+y", YB, u, 0.42, shutters=PAINT_GREEN)
door("+y", YB, 0.0, w=0.34, h=0.62)
door("+y", YB, 1.50)
window("+y", YB, 2.20, 0.42, shutters=PAINT_GREEN)

# Hofmauern: Außenflächen in den Ebenen x = -W und x = W (wie die Hauswände davor)
flush_wall(-W, -W + WT, YB - EMB, W - 0.02, 0.40, PLASTER_OCHRE)
flush_wall(W - WT, W, YB - EMB, 1.55 + EMB, 0.40, PLASTER_PINK)
# Rückwand links (Ebene y = W) mit zwei kleinen Fenstern
flush_wall(-W, 0.0 + EMB, W - WT, W, 0.64, PLASTER_OCHRE)
for u in (-2.05, -0.90):
    window("+y", W, u, 0.32, w=0.14, h=0.20, shutters=None)

# Werkhalle hinten rechts: Pultdach nach hinten fallend
HX0, HY0 = 0.0, 1.55
ZH = 1.20
PITCH_H = 16.0
eave_z = shed_roof(HX0, W, HY0, W, ZH, "+y", overhang=0.04, pitch=PITCH_H, eave_overhang=0.04)
under = lambda y: ZH - (y - HY0) * math.tan(math.radians(PITCH_H))
body(HX0, W, W - WT, W, under(W) - 0.005, PLASTER_OCHRE, socle=None)
slab([(HY0, 0.0), (W, 0.0), (W, under(W) - 0.005), (HY0, ZH)], "yz", W - WT, WT, PLASTER_PINK)
for y in (HY0 + 0.07, 2.10, W - 0.20):
    hz = under(y) - 0.01
    box((0.07, 0.07, hz), (0.07, y, hz / 2), material=WOOD_DARK)
bar((0.07, HY0 + 0.07, under(HY0 + 0.07) - 0.035), (0.07, W - 0.20, under(W - 0.20) - 0.035), 0.07, WOOD_DARK)
for x in (1.40,):
    box((0.07, 0.07, ZH - 0.01), (x, HY0 + 0.07, (ZH - 0.01) / 2), material=WOOD_DARK)
box((W - HX0, 0.07, 0.07), ((HX0 + W) / 2, HY0 + 0.07, ZH - 0.035), material=WOOD_DARK)
tool_wall(0.35, 1.35, W - WT, 0.28, 0.80)
vessel_shelf(1.95, W - WT - 0.09, 0, w=0.90, tiers=2, seed=4)
workbench(0.85, 2.12, yaw=90)
stool(0.85, 1.80)
barrel(2.25, 1.78, r=0.07)

# Brennofen und Holz an der Rückwand links
top = kiln(-1.55, 2.02, r=0.40)
smoke_plume(-1.55, 2.02, top)
woodpile(-0.50, 2.36, length=0.40, rows=(4, 3, 2), yaw=0)

# Werkhof: Töpferscheibe, Tische, Regal, Amphoren, Fässer, Karren
potters_wheel(-1.30, 0.40)
vessel_shelf(0.0, YB + 0.13, 0, w=1.20, tiers=2, fired=True, seed=2)
trestle_table(-0.25, 0.42, 0, w=0.96, d=0.30, h=0.28, cloth=CLOTH_OCHRE)
for i, kind in enumerate(("pot", "bowl", "pot", "bowl", "pot")):
    px = -0.60 + i * 0.18
    if kind == "pot":
        pot(px, 0.42, 0.286, 0.040)
    else:
        bowl(px, 0.42, 0.286, 0.050, TERRACOTTA_LIGHT)
trestle_table(1.35, -0.45, 0, w=1.0, d=0.30, h=0.28, cloth=CLOTH)
for i in range(3):
    basket_goods(1.05 + i * 0.30, -0.45, 0.286, 0.062, "grain", seed=10 + i)
amphora_row((-2.38, -0.40), (-2.38, 0.70), 4)
for x, y in ((2.30, 0.20), (2.42, 0.38), (2.28, 0.50)):
    barrel(x, y, r=0.07)
barrel(2.30, 0.95, r=0.07, lying=True, yaw=15)
cart(0.95, 0.65, yaw=-25, load="sacks")
crate(-0.10, -0.40, 0.0, 0.18, yaw=8)
crate(-0.10, -0.40, 0.18, 0.14, yaw=-12)
plank_stack(-0.55, 1.28, 0.22, 0.80, 4, yaw=90, seed=3)
sack_stack(1.55, 1.15, yaw=0, per=(3, 2))
pot(-0.75, -0.35, 0.0, 0.050)
pot(-0.62, -0.20, 0.0, 0.042, TERRACOTTA_LIGHT)
pot_plant(-2.30, -0.95, 0.055)
stool(-1.30, 0.00)
check_bounds(2.76, 2.76, "0")
variant("Werkstatthof_0")

# ---------------------------------------------------------------- Variante 1: drei Flügel um einen Hof

IW = 1.45             # Innenkante der Seitenflügel (x = +-IW)
BY = 0.95             # Vorderseite des Hauptflügels hinten
ZH1 = 1.15
PITCH_W = 14.0
tanw = math.tan(math.radians(PITCH_W))
for s, plaster in ((-1, PLASTER_OCHRE), (1, PLASTER_PINK)):
    xo, xi = (-W, -IW) if s < 0 else (IW, W)             # Außen- und Innenkante des Flügels (x0 < x1 sortieren)
    x0, x1 = min(xo, xi), max(xo, xi)
    low_side = "-x" if s < 0 else "+x"
    shed_roof(x0, x1, -W, BY, ZH1, low_side, overhang=0.04, pitch=PITCH_W, eave_overhang=0.04)
    # Höhe der Plattenunterseite in Abstand d von der Innenkante
    under_w = lambda d: ZH1 - d * tanw
    d_out = x1 - x0
    h_out = under_w(d_out) - 0.005
    # Außenwand (Ebene x = s * W), volle Länge des Flügels
    if s < 0:
        body(-W, -W + WT, -W, BY + EMB, h_out, plaster, socle=None)
    else:
        body(W - WT, W, -W, BY + EMB, h_out, plaster, socle=None)
    # Stirnwand zur Straße (Ebene y = -W): Trapez unter dem Pultdach
    if s < 0:
        pts = [(-W, 0.0), (-IW, 0.0), (-IW, ZH1), (-W, h_out)]
    else:
        pts = [(IW, 0.0), (W, 0.0), (W, h_out), (IW, ZH1)]
    slab(pts, "xz", -W, WT, plaster)
    # Pfosten zum Hof und Pfette
    for y in (-W + 0.16, -1.45, -0.20, BY - 0.20):
        box((0.07, 0.07, ZH1 - 0.01), (s * IW, y, (ZH1 - 0.01) / 2), material=WOOD_DARK)
    box((0.08, BY + W - 0.20, 0.07), (s * IW, (-W + 0.16 + BY - 0.20) / 2, ZH1 - 0.035), material=WOOD_DARK)
    # Fenster in der Außenwand und der Stirnwand
    for u in (-1.90, -0.80, 0.25):
        window("-x" if s < 0 else "+x", s * W, u, 0.34, w=0.16, h=0.22, shutters=None)
    window("-y", -W, s * 2.06, 0.34, w=0.16, h=0.22, shutters=PAINT_GREEN, open_shutters=(s > 0))

# Hauptflügel hinten, zweistöckig
body(-W, W, BY, W, 2 * STOREY - 0.05, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(-W, W, BY, W, 2 * STOREY - 0.05, "x", PLASTER_WHITE, overhang=0.04)
cornice(-W, W, BY, W, STOREY - 0.03)
shop_front("-y", BY, -1.70, w=0.64, goods=("jug", "bread", "jug"), awning=(CLOTH_RED, CLOTH))
shop_front("-y", BY, 1.70, w=0.64, goods=("amphora", "basket", "jug"), awning=(PAINT_BLUE, CLOTH))
door("-y", BY, -0.95, w=0.36, h=0.66)
door("-y", BY, 0.95, w=0.36, h=0.66)
arch_opening("-y", BY, 0.0, 0.50, 0.50, z0=0.09)
balcony(BY, -1.20, 1.20, STOREY + 0.04, depth=0.20)
for u in (-2.10, -1.40, 1.40, 2.10):
    window("-y", BY, u, STOREY + 0.36, shutters=PAINT_GREEN, open_shutters=(u < 0))
door("-y", BY, 0.0, w=0.28, h=0.56, z0=STOREY + 0.04)
for u in (-1.90, -0.95, 0.0, 0.95, 1.90):
    window("+y", W, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u > 0))
    window("+y", W, u, STOREY + 0.36, shutters=PAINT_GREEN, open_shutters=(u < 0.5))
for face, plane in (("-x", -W), ("+x", W)):
    for u in (1.60, 2.30):
        window(face, plane, u, 0.42, shutters=PAINT_GREEN)
        window(face, plane, u, STOREY + 0.36, shutters=PAINT_GREEN, open_shutters=False)

# Straße: Mauerstücke und Tor
flush_wall(-IW - EMB, -0.50, -W, -W + WT, 0.55, PLASTER_WHITE)
flush_wall(0.50, IW + EMB, -W, -W + WT, 0.55, PLASTER_WHITE)
for s in (-1, 1):
    box((0.12, WT, 0.98), (s * 0.44, -W + WT / 2, 0.49), material=LIME_DARK)
    box((0.15, WT, 0.04), (s * 0.44, -W + WT / 2, 1.00), material=LIME)
box((0.88, WT, 0.08), (0.0, -W + WT / 2, 1.06), material=WOOD_DARK)
for s in (-1, 1):
    box((0.04, 0.46, 0.72), (s * 0.39, -W + WT + 0.25, 0.40), material=WOOD)
    for z in (0.20, 0.60):
        box((0.05, 0.40, 0.03), (s * 0.39, -W + WT + 0.25, z), material=IRON)

# Hof: Zisterne mit Trog, Tische, Karren, Fässer, Amphoren
cistern(0.0, -0.10)
box((0.52, 0.20, 0.16), (0.55, -0.10, 0.08), material=LIME_DARK)
box((0.44, 0.12, 0.012), (0.55, -0.10, 0.152), material=WATER)
bucket(0.33, -0.34, 0.0, 0.05)
trestle_table(-0.55, 0.45, 0, w=0.86, d=0.30, h=0.28, cloth=CLOTH_OCHRE)
for i, kind in enumerate(("pot", "bowl", "pot", "bowl")):
    px = -0.85 + i * 0.20
    if kind == "pot":
        pot(px, 0.45, 0.286, 0.040)
    else:
        bowl(px, 0.45, 0.286, 0.050, TERRACOTTA_LIGHT)
trestle_table(0.65, 0.45, 0, w=0.86, d=0.30, h=0.28, cloth=CLOTH)
for i in range(3):
    basket_goods(0.38 + i * 0.27, 0.45, 0.286, 0.060, "grain", seed=20 + i)
cart(-0.85, -1.50, yaw=25, load="sacks")
cart(0.90, -1.35, yaw=-30, load="hay")
for x, y in ((-0.20, -1.95), (-0.04, -2.02), (0.12, -1.95)):
    barrel(x, y, r=0.07)
amphora_row((-1.28, -0.15), (-1.28, -1.05), 3)
sack_stack(0.0, 0.75, yaw=0, per=(3, 2, 1))
crate(0.95, -0.70, 0.0, 0.18, yaw=6)
pot_plant(-1.15, 0.80, 0.05)
# unter den offenen Hallen
potters_wheel(-2.00, -1.50, yaw=90)
vessel_shelf(-W + WT + 0.09, -0.35, 90, w=0.95, tiers=2, fired=False, seed=5)
workbench(2.05, -1.15, yaw=0)
plank_stack(2.20, 0.20, 0.22, 0.75, 4, yaw=0, seed=4)
woodpile(1.85, -2.20, length=0.40, rows=(4, 3), yaw=90)
stool(2.00, -0.55)
kiln(-1.95, 0.30, r=0.34)
check_bounds(2.76, 2.76, "1")
variant("Werkstatthof_1")

export(spacing=6.2)
