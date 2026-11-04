"""
Gutshof (Antike), 2 x 2 Felder (4 x 4 m): kleine Villa rustica, mehr Hof und Landwirtschaft als Wohnhaus.
Hinten links das Wohnhaus des Verwalters (ocker, einstöckig mit niedrigem Obergeschoss, Ziegel-Satteldach, kurzer
Säulengang mit zwei Säulen, Tür, Fenster mit offenen Läden); rechts die hohe Scheune (Bohlenwände auf Bruchsteinsockel,
Giebel zur Straße mit halb offenem Tor, Heu und Säcken, darüber Ladeluke mit Kranbalken); in der Mitte der Hof, zur
Straße offen mit Hoftor und niedriger Hofmauer: Dreschplatz aus gestampfter Erde mit ausgebreitetem Stroh, Dreschschlitten, Kornhaufen, Korb und Worfelgabel, Garben in Hocken am Rand, Zisterne, Karren mit
Getreidesäcken, Hühner, Fässer und Amphoren; hinten rechts ein offener Schuppen mit Pflug und Geräten, daneben
Brennholz und ein Gemüsebeet, dazu ein Oliven- und ein Feigenbaum.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/gutshof.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 3,68 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Gutshof_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(77)

THRESHING = mat("Tenne", (0.66, 0.54, 0.38), rough=0.95)       # gestampfter, hellerer Dreschplatz

# ---------------------------------------------------------------- Wohnhaus hinten links
HX0, HX1, HY0, HY1 = -1.70, -0.24, 0.55, 1.66
HH = 1.40                                       # Erdgeschoss 1 m, darüber ein niedriges Obergeschoss
body(HX0, HX1, HY0, HY1, HH, PLASTER_OCHRE, socle=SOCLE_RED)
cornice(HX0, HX1, HY0, HY1, STOREY)
gable_roof(HX0, HX1, HY0, HY1, HH, "x", PLASTER_OCHRE, overhang=0.08)
door("-y", HY0, -1.00)
window("-y", HY0, -1.60, 0.42, shutters=PAINT_GREEN)
window("-y", HY0, -0.38, 0.42, shutters=PAINT_GREEN, open_shutters=False)
for u in (-1.30, -0.70):
    window("-y", HY0, u, STOREY + 0.10, w=0.18, h=0.20, shutters=PAINT_GREEN, open_shutters=(u < -1.0))
window("-x", HX0, 1.10, 0.42, shutters=PAINT_GREEN)
window("-x", HX0, 1.10, STOREY + 0.10, w=0.18, h=0.20, shutters=PAINT_GREEN)
window("+x", HX1, 1.10, 0.42, shutters=PAINT_GREEN, open_shutters=False)
door("+y", HY1, -1.00)
window("+y", HY1, -0.45, 0.42, shutters=PAINT_GREEN)
window("+y", HY1, -1.55, STOREY + 0.10, w=0.18, h=0.20, shutters=None)
# kurzer Säulengang: zwei Säulen, Gebälk und ein flaches Ziegeldach an der Wand
PX0, PX1, PY = -1.52, -0.48, 0.27
for x in (PX0 + 0.03, PX1 - 0.03):
    column(x, PY, 0.72)
box((PX1 - PX0 + 0.10, 0.12, 0.10), ((PX0 + PX1) / 2, PY, 0.77), material=LIME)
shed_roof(PX0 - 0.04, PX1 + 0.04, HY0 - 0.28, HY0, 0.98, "-y", overhang=0.0, pitch=14.0, eave_overhang=0.02)
amphora(-1.62, 0.40)
pot_plant(-0.33, 0.40, 0.05)

# ---------------------------------------------------------------- Scheune rechts
BX0, BX1, BY0, BY1 = 0.60, 1.72, -1.50, 0.30
BH = 1.45
plank_wall(BX0, BX1, BY0, BY1, BH)
ridge = gable_roof(BX0, BX1, BY0, BY1, BH, "y", WOOD, overhang=0.08, pitch=26)
# senkrechte Bohlenfugen im Giebel zur Straße
xc, half = (BX0 + BX1) / 2, (BX1 - BX0) / 2
n = int((BX1 - BX0) / 0.10)
for k in range(1, n):
    x = BX0 + k * (BX1 - BX0) / n
    gh = (ridge - BH - 0.05) * (1 - abs(x - xc) / half)
    if gh > 0.06:
        box((0.008, 0.006, gh), (x, BY0 - 0.003, BH + gh / 2), material=WOOD_DARK)
# breites, halb offenes Tor: links ein Flügel nach außen aufgeschwenkt, rechts geschlossen mit Zierstrebe
GW, GH, GZ = 0.56, 0.84, 0.09
gu = xc
box((GW, 0.012, GH), (gu, BY0 + 0.004 - 0.0, GZ + GH / 2), material=DARK)
for s in (-1, 1):
    box((0.05, 0.03, GH + 0.02), (gu + s * (GW / 2 + 0.025), BY0 - 0.012, GZ + GH / 2), material=WOOD_DARK)
box((GW + 0.14, 0.035, 0.06), (gu, BY0 - 0.015, GZ + GH + 0.02), material=WOOD_DARK)
box((GW + 0.08, 0.09, 0.04), (gu, BY0 - 0.045, 0.02), material=STONE)
box((GW / 2 - 0.01, 0.02, GH - 0.01), (gu + GW / 4, BY0 - 0.012, GZ + GH / 2), material=WOOD)       # rechter Flügel zu
bar((gu + 0.02, BY0 - 0.026, GZ + 0.04), (gu + GW / 2 - 0.02, BY0 - 0.026, GZ + GH - 0.04), 0.022, WOOD_DARK)
for z in (GZ + 0.16, GZ + GH - 0.16):
    box((GW / 2 - 0.04, 0.006, 0.022), (gu + GW / 4, BY0 - 0.025, z), material=IRON)
a = math.radians(72)                                                    # linker Flügel offen
hinge = (gu - GW / 2, BY0 - 0.012)
d = (math.cos(a), -math.sin(a))
c = (hinge[0] + d[0] * GW / 4, hinge[1] + d[1] * GW / 4)
box((GW / 2 - 0.01, 0.02, GH - 0.01), (c[0], c[1], GZ + GH / 2), rot=(0, 0, math.degrees(math.atan2(d[1], d[0]))),
    material=WOOD)
# Heu und Säcke im Tor
hay(gu - 0.10, BY0 - 0.02, 0.13)
hay(gu + 0.04, BY0 - 0.01, 0.10)
sack(gu + 0.17, BY0 - 0.06, GZ, 0.055)
sack(gu - 0.20, BY0 - 0.07, GZ, 0.05)
# Ladeluke oben mit Kranbalken, Seil und Sack
door("-y", BY0, gu, w=0.36, h=0.34, z0=1.00, leaf=WOOD_DARK)
hoist(gu, BY0, 1.50, reach=0.24, sack_z=1.04)
# Lüftungsöffnungen und Rückseite
for face, plane in (("-x", BX0), ("+x", BX1)):
    at, size, _, _ = face_frame(face, plane)
    for u in (-0.95, -0.35, 0.15):
        box(size(0.07, 0.012, 0.16), at(u, 0.004, 1.00), material=DARK)
        box(size(0.10, 0.02, 0.03), at(u, 0.008, 1.095), material=WOOD_DARK)
door("+y", BY1, gu, w=0.36, h=0.60, z0=0.12, leaf=WOOD_DARK)

# ---------------------------------------------------------------- Hof
CX, CY = -0.55, -0.60                           # Mitte des Dreschplatzes
cyl(0.62, 0.025, (CX, CY, 0.0125), material=THRESHING, verts=24)
for k in range(16):                              # Randsteine
    t = math.radians(k * 360 / 16)
    box((0.11, 0.05, 0.035), (CX + 0.64 * math.cos(t), CY + 0.64 * math.sin(t), 0.017),
        rot=(0, 0, math.degrees(t) + 90), material=LIME_DARK)
# ausgebreitetes Stroh: flache, unregelmäßige Lagen, in der Mitte dichter
for dx, dy, r, z in ((0.02, 0.03, 0.44, 0.027), (-0.10, -0.06, 0.30, 0.031), (0.12, -0.10, 0.22, 0.033),
                     (-0.05, 0.14, 0.20, 0.033)):
    cyl(r, 0.006, (CX + dx, CY + dy, z), material=STRAW, verts=12)
# Dreschschlitten (Tribulum): Brett mit Steinen als Gewicht, Zugseil nach vorn
TYAW = 25
tb = math.radians(TYAW)
box((0.36, 0.20, 0.025), (CX - 0.12, CY + 0.04, 0.048), rot=(0, 0, TYAW), material=WOOD)
box((0.04, 0.22, 0.035), (CX - 0.12 - 0.17 * math.cos(tb), CY + 0.04 - 0.17 * math.sin(tb), 0.05),
    rot=(0, 0, TYAW), material=WOOD_DARK)                                          # aufgebogene Vorderkante
for sx, sy in ((-0.06, -0.04), (0.05, 0.05), (0.08, -0.05)):
    px = CX - 0.12 + sx * math.cos(tb) - sy * math.sin(tb)
    py = CY + 0.04 + sx * math.sin(tb) + sy * math.cos(tb)
    sphere(0.035, (px, py, 0.075), (1.0, 0.8, 0.55), STONE, segs=6, rings=4)
front = (CX - 0.12 - 0.19 * math.cos(tb), CY + 0.04 - 0.19 * math.sin(tb), 0.055)
rod(front, (front[0] - 0.22, front[1] - 0.18, 0.032), 0.006, WOOD_DARK, verts=5)
# gedroschenes Korn: Haufen am Rand, Korb und Worfelgabel daneben
cone(0.13, 0.02, 0.07, (CX + 0.30, CY - 0.22, 0.062), GRAIN, verts=12)
cyl(0.055, 0.06, (CX + 0.44, CY - 0.02, 0.055), material=WOOD, verts=10)          # Korb
cyl(0.048, 0.005, (CX + 0.44, CY - 0.02, 0.086), material=GRAIN, verts=10)
fork = ((CX + 0.10, CY - 0.42, 0.032), (CX + 0.40, CY - 0.36, 0.034))
rod(fork[0], fork[1], 0.008, WOOD, verts=5)
for off in (-0.025, 0.0, 0.025):
    rod((fork[1][0], fork[1][1] + off, 0.034), (fork[1][0] + 0.08, fork[1][1] + off * 1.6, 0.034), 0.005, WOOD,
        verts=4)
# Garben in Hocken am Rand des Platzes
stook(-1.36, -0.50, n=5, yaw=10)
stook(-0.02, -1.20, n=5, yaw=40)
stook(-1.22, 0.10, n=4, yaw=0)
# Zisterne, Karren, Fässer, Amphoren, Hühner
cistern(-1.50, -1.05)
cart(0.28, -0.35, yaw=90, load="sacks")
barrel(-1.57, -0.15, r=0.075)
barrel(-1.57, 0.05, r=0.075)
barrel(-1.45, -0.02, 0.0, r=0.075, lying=True, yaw=90)
for k in range(3):
    amphora(-0.40 + k * 0.11, 0.38)
box((0.36, 0.025, 0.025), (-0.29, 0.33, 0.17), material=WOOD_DARK)
for x, y, yaw in ((-0.10, -0.15, 30), (-0.25, 0.15, 160), (0.00, -0.95, -50), (-1.05, -0.95, 100), (-0.85, 0.12, 210)):
    chicken(x, y, yaw)
# Hofmauer mit Hoftor
court_wall([(-1.74, 0.55), (-1.74, -1.50), (-1.00, -1.50)])
court_wall([(-0.30, -1.50), (0.58, -1.50)])
for x in (-0.97, -0.33):
    box((0.11, 0.11, 0.60), (x, -1.50, 0.30), material=LIME_DARK)
    box((0.15, 0.15, 0.05), (x, -1.50, 0.625), material=LIME)
    box((0.09, 0.09, 0.05), (x, -1.50, 0.675), material=TILE_DARK)
for s, x in ((-1, -0.97), (1, -0.33)):                  # Torflügel nach innen aufgeschwenkt
    ang = math.radians(80)
    hx, hy = x - s * 0.055, -1.50                       # Angel am Pfosten, Flügel zeigt zur Mitte und in den Hof
    dx, dy = -s * math.cos(ang), math.sin(ang)
    cxl, cyl_ = hx + dx * 0.14, hy + dy * 0.14
    yaw = math.degrees(math.atan2(dy, dx))
    box((0.28, 0.025, 0.40), (cxl, cyl_, 0.28), rot=(0, 0, yaw), material=WOOD)
    for z in (0.16, 0.40):
        box((0.28, 0.03, 0.025), (cxl, cyl_, z), rot=(0, 0, yaw), material=WOOD_DARK)

# ---------------------------------------------------------------- Schuppen, Beet, Holz, Bäume hinten rechts
SX0, SX1, SY0, SY1 = 0.62, 1.72, 0.95, 1.70
open_shed(SX0, SX1, SY0, SY1, 0.85)
plough(0.78, 1.32, yaw=0)
sack(1.56, 1.30, 0.0, 0.06)
sack(1.62, 1.18, 0.0, 0.055)
crate(1.58, 1.46, size=0.17, yaw=10)
# Geräte an der Rückwand: Heugabel und Rechen
bar((1.20, 1.57, 0.03), (1.22, 1.625, 0.78), 0.016, WOOD_DARK)
bar((1.15, 1.625, 0.78), (1.29, 1.625, 0.78), 0.016, IRON)
for dx in (-0.05, 0.0, 0.05):
    bar((1.22 + dx, 1.625, 0.78), (1.22 + dx, 1.625, 0.90), 0.010, IRON)
bar((1.40, 1.57, 0.03), (1.38, 1.625, 0.74), 0.016, WOOD_DARK)
bar((1.31, 1.625, 0.74), (1.45, 1.625, 0.74), 0.016, WOOD_DARK)
for dx in (-0.06, -0.02, 0.02, 0.06):
    bar((1.38 + dx, 1.625, 0.74), (1.38 + dx, 1.625, 0.68), 0.008, IRON)
woodpile(0.48, 1.30, length=0.36, rows=(3, 2), yaw=90)
veg_bed(-0.12, 0.38, 1.12, 1.62, rows_along="y")
fig_tree(0.05, 0.72, 0.8)
olive_tree(1.30, 0.58, 0.9)
fence([(-0.20, 1.66), (-0.20, 1.10), (0.42, 1.10)], h=0.26)

obj = variant("Gutshof_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}")

export(spacing=7.0)
