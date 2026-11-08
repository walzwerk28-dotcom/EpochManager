"""
Schreibschule (Antike), 2 x 2 Felder (4 x 4 m): Elementarschule unter einer Pergula wie in Pompeji. Zur Straße ein
Vorplatz mit einem gespannten, rot-cremefarben gestreiften Sonnensegel an zwei Pfosten, das am Gebälk der Halle hängt;
dahinter die offene Säulenhalle (vier Säulen mit Gebälk, niedrige Brüstungen an den Seiten) unter einem Ziegelpultdach,
das nach vorn abfällt. In der Halle drei Reihen Schulbänke mit Wachstafeln, hinten ein Podium mit dem Lehrerstuhl
(Cathedra); an der verputzten Rückwand eine Tafel mit Inschrift und zwei Schränke mit Schriftrollen. Hinter der Halle
ein kleiner Hof mit niedriger Mauer, Feigenbaum, Bank, Brunnenkrügen und einer Zisterne.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/schreibschule.py -- <blend> <fbx> [<vorschau.png>]

Vorderseite nach -Y, alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m). Die FBX enthält ein Objekt (Schreibschule_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(52)

WX = 1.70                    # Hallenbreite ±WX
YF, YB = -1.10, 0.50         # Gebälk vorn, Rückwand (Außenseite)
CH = 1.28                    # Säulenhöhe
PITCH_S = 12.0
EAVE = 0.06
YR0 = YF - 0.10                # vordere Dachkante (hohe Seite) steht 0,1 m vor dem Gebälk
RUN = (YB - YR0) + EAVE
Z_HIGH = CH + 0.13               # Unterkante der Platte an der hohen Seite (vorn)


def z_under(y):
    """Unterkante des Dachs über der Halle an der Stelle y."""
    return Z_HIGH - (y - YR0) * math.tan(math.radians(PITCH_S))


# ---------------------------------------------------------------- Rückwand mit Sockel, Tafel und Schränken
WALL_H = z_under(YB) - 0.01
box((2 * WX, 0.12, WALL_H), (0, YB - 0.06, WALL_H / 2), material=PLASTER_OCHRE)
box((2 * WX + 0.04, 0.16, 0.09), (0, YB - 0.06, 0.045), material=STONE)
box((2 * WX + 0.012, 0.136, 0.24), (0, YB - 0.06, 0.20), material=SOCLE_RED)
# Seitenwände im hinteren Teil (bis unter die Dachschräge), vorn nur eine niedrige Brüstung
Y_SIDE = -0.55
for s in (-1, 1):
    x_out = s * WX
    slab([(Y_SIDE, 0.0), (YB, 0.0), (YB, WALL_H), (Y_SIDE, z_under(Y_SIDE) - 0.01)], "yz",
         x_out - 0.12 if s > 0 else x_out, 0.12, PLASTER_OCHRE)
    box((0.16, YB - Y_SIDE, 0.09), (s * (WX - 0.06) , (Y_SIDE + YB) / 2, 0.045), material=STONE)
    # Brüstung vorn
    yp0, yp1 = YF, Y_SIDE
    box((0.12, yp1 - yp0, 0.40), (s * (WX - 0.06), (yp0 + yp1) / 2, 0.20), material=PLASTER_OCHRE)
    box((0.16, yp1 - yp0 + 0.02, 0.09), (s * (WX - 0.06), (yp0 + yp1) / 2, 0.045), material=STONE)
    box((0.17, yp1 - yp0 + 0.02, 0.04), (s * (WX - 0.06), (yp0 + yp1) / 2, 0.42), material=LIME)

# ---------------------------------------------------------------- Säulen und Gebälk
CX = (-1.64, -0.55, 0.55, 1.64)
for x in CX:
    column(x, YF, CH, r=0.052)
box((2 * WX + 0.10, 0.11, 0.10), (0, YF, CH + 0.05), material=LIME)
box((2 * WX + 0.14, 0.04, 0.03), (0, YF - 0.065, CH + 0.015), material=LIME_DARK)
# Zahnschnitt unter dem Gebälk
for k in range(24):
    box((0.035, 0.02, 0.03), (-WX + 0.07 + k * (2 * WX - 0.14) / 23, YF - 0.065, CH - 0.01), material=LIME_DARK)

# ---------------------------------------------------------------- Ziegelpultdach, Traufe vorn
shed_roof(-WX, WX, YR0, YB, Z_HIGH, "+y", overhang=0.08, pitch=PITCH_S, eave_overhang=EAVE)
box((2 * WX + 0.16, 0.03, 0.07), (0, YR0 - 0.015, Z_HIGH - 0.02), material=WOOD_DARK)     # Stirnbrett vorn

# Giebelchen über dem Eingang (Fastigium) mit Giebelfeld und Akroterien: macht den Eingang von Weitem lesbar
FY = YR0
slab([(-0.62, Z_HIGH - 0.02), (0.62, Z_HIGH - 0.02), (0.0, Z_HIGH + 0.31)], "xz", FY, 0.10, LIME)
slab([(-0.50, Z_HIGH + 0.02), (0.50, Z_HIGH + 0.02), (0.0, Z_HIGH + 0.25)], "xz", FY - 0.008, 0.01, TYMPANUM)
for k in range(5):                                                              # Inschrift im Giebelfeld
    box((0.06, 0.008, 0.045), (-0.20 + k * 0.10, FY - 0.016, Z_HIGH + 0.075), material=FEATHER)
for x, z in ((-0.62, Z_HIGH - 0.02), (0.62, Z_HIGH - 0.02), (0.0, Z_HIGH + 0.31)):
    palmette(x, FY + 0.05, z, h=0.13)
# ---------------------------------------------------------------- Inneres der Halle
box((1.0, 0.30, 0.07), (0, 0.17, 0.035), material=LIME_DARK)                               # Podium
box((1.04, 0.34, 0.03), (0, 0.17, 0.085), material=LIME)
cathedra(0.0, 0.22, 0, z0=0.10)
scroll_rack(-1.28, 0.30, 0.0)
scroll_rack(1.28, 0.30, 0.0)
for i, y in enumerate((-0.90, -0.57, -0.24)):
    for j, x in enumerate((-0.82, 0.82)):
        school_bench(x, y, 0.0, length=1.0, tablets=3, seed=i * 2 + j)
# Tafel mit Inschrift an der Rückwand über dem Podium
TY = YB - 0.12 - 0.012
box((0.95, 0.02, 0.46), (0, TY, 0.78), material=WOOD_DARK)
box((0.88, 0.012, 0.39), (0, TY - 0.012, 0.78), material=PLASTER_WHITE)
rnd = random.Random(5)
for row in range(4):
    x = -0.38
    for w in range(6 - (row % 2)):
        ww = rnd.choice((0.07, 0.10, 0.06, 0.12))
        if x + ww > 0.40:
            break
        box((ww, 0.006, 0.026), (x + ww / 2, TY - 0.021, 0.91 - row * 0.075), material=SOCLE_RED if row == 0 else IRON)
        x += ww + 0.035
# Lampe auf dem Podium
cone(0.025, 0.04, 0.03, (0.40, 0.17, 0.115), TERRACOTTA, verts=8)
# Tür und Fenster in der Rückwand (zum Hof)
door("+y", YB, 1.20, w=0.30, h=0.56)
window("+y", YB, -1.20, 0.55, w=0.20, h=0.26, shutters=PAINT_GREEN)
window("+y", YB, 0.0, 0.55, w=0.20, h=0.26, shutters=PAINT_GREEN)

# ---------------------------------------------------------------- Sonnensegel an zwei Pfosten
PS = 1.14                                                                  # Pfostenhöhe
for s in (-1, 1):
    px, py = s * 1.45, -1.72
    cyl(0.022, PS, (px, py, PS / 2), material=WOOD_DARK, verts=7)
    box((0.15, 0.15, 0.05), (px, py, 0.025), material=LIME_DARK)
    sphere(0.032, (px, py, PS + 0.01), material=BRONZE, segs=7, rings=4)
    bar((px, py, PS - 0.02), (s * 1.62, YR0 - 0.03, CH + 0.06), 0.008, ROPE)           # Abspannseil nach hinten
sun_sail([(-1.58, YR0 - 0.03, CH + 0.10), (1.58, YR0 - 0.03, CH + 0.10), (1.45, -1.72, PS - 0.01), (-1.45, -1.72, PS - 0.01)],
         sag=0.07, bands=10, colors=(CLOTH_RED, None))
pot_plant(-1.15, -1.50, 0.055, leaves=LAUREL)
pot_plant(1.15, -1.50, 0.055, leaves=LAUREL)
jug(-0.35, -1.55, 0.0, 0.05)
amphora(0.35, -1.55)

# ---------------------------------------------------------------- Hof hinter der Halle
YW = 1.66
for xa, xb in ((-WX, WX),):
    box((xb - xa, 0.12, 0.42), ((xa + xb) / 2, YW, 0.21), material=PLASTER_OCHRE)
    box((xb - xa + 0.02, 0.14, 0.05), ((xa + xb) / 2, YW, 0.025), material=STONE)
    box((xb - xa + 0.04, 0.17, 0.04), ((xa + xb) / 2, YW, 0.44), material=TILE_DARK)
for s in (-1, 1):
    ya, yb = YB, YW - 0.06
    box((0.12, yb - ya, 0.40), (s * (WX - 0.06), (ya + yb) / 2, 0.20), material=PLASTER_OCHRE)
    box((0.17, yb - ya, 0.04), (s * (WX - 0.06), (ya + yb) / 2, 0.42), material=TILE_DARK)
fig_tree(-0.95, 1.10, 1.25)
bench(0.25, 1.52, yaw=0, length=0.52)
cistern(1.00, 1.05)
jug(1.30, 1.40, 0.0, 0.055)
jug(0.72, 1.42, 0.0, 0.05, material=TERRACOTTA_LIGHT)
amphora(1.45, 0.80)
pot_plant(-1.45, 0.78, 0.055, leaves=HERB_LIGHT)
pot_plant(-0.45, 1.52, 0.05, leaves=LAUREL)
veg_bed(-0.15, 0.55, 0.78, 1.15, rows_along="x")

obj = variant("Schreibschule_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}")

export(spacing=6.0)
