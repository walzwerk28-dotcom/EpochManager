"""
Bibliothek (Antike), 2 x 2 Felder (4 x 4 m): Lesesaal aus Kalkstein auf einem niedrigen Podium mit Freitreppe. Eingang als
Vorhalle mit zwei Säulen zwischen den Anten (Distylos in antis) unter einem Satteldach, Giebel zur Straße mit Kranz und
Palmetten (schlichter als der Tempel). Hohe schmale Fenster an den Seiten, Ziegelbänder im Mauerwerk, die offene
Bronzetür gibt den Blick auf Rollenschränke (Nischenregale mit Schriftrollen) und einen Lesetisch frei. Davor ein kleiner
Säulenhof: eine kurze Säulenreihe an jeder Seite, in der Mitte eine Statue (Philosoph) auf Sockel, Bänke und Lorbeer.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/bibliothek.py -- <blend> <fbx> [<vorschau.png>]

Vorderseite nach -Y, alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m). Die FBX enthält ein Objekt (Bibliothek_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(63)

POD = 0.30                  # Podium
CX = 1.12                   # halbe Breite der Cella (Außenkante)
YF_P, YA = -0.46, -0.03     # Vorderkante der Vorhalle (Antenköpfe), Vorderwand der Cella
YB = 1.58                   # Rückseite der Cella
WH = 1.40                   # Wandhöhe über dem Podium
Z_TOP = POD + WH            # Oberkante der Wände und Gebälk (1,70)
CH = 1.30                   # Säulenhöhe in der Vorhalle

# ---------------------------------------------------------------- Podium und Freitreppe
temple_podium(-1.34, 1.34, -0.66, 1.66, POD)
grand_stairs(0.0, -0.66, 0.80, POD, 2, 0.12)

# ---------------------------------------------------------------- Cella: Wände, Tür, Decke
DW, DH = 0.23, 0.98
front = [(-CX, POD), (-DW, POD), (-DW, POD + DH), (DW, POD + DH), (DW, POD), (CX, POD), (CX, Z_TOP), (-CX, Z_TOP)]
slab(front, "xz", YA, 0.12, PLASTER_OCHRE)
for s in (-1, 1):                                                          # Seitenwände samt Anten
    box((0.12, YB - YF_P, WH), (s * (CX - 0.06), (YF_P + YB) / 2, POD + WH / 2), material=LIME)
    box((0.16, 0.14, WH), (s * (CX - 0.08), YF_P + 0.07, POD + WH / 2), material=LIME_DARK)          # Antenkopf
    box((0.20, 0.16, 0.10), (s * (CX - 0.08), YF_P + 0.07, POD + 0.05), material=LIME_DARK)          # Antenbasis
box((2 * CX, 0.12, WH), (0, YB - 0.06, POD + WH / 2), material=LIME)                                   # Rückwand
box((2 * CX - 0.20, YB - YA, 0.06), (0, (YA + YB) / 2, Z_TOP - 0.03), material=LIME)                   # Decke
box((2 * CX - 0.24, YB - YA - 0.12, 0.012), (0, (YA + YB) / 2 + 0.03, POD + 0.006), material=PAVE_WORN)  # Fußboden innen

# Türgewände, Sturz und offene Bronzeflügel (nach außen geschwenkt)
for s in (-1, 1):
    box((0.06, 0.05, DH + 0.04), (s * (DW + 0.03), YA - 0.025, POD + (DH + 0.04) / 2), material=LIME_DARK)
box((2 * DW + 0.22, 0.06, 0.07), (0, YA - 0.03, POD + DH + 0.045), material=LIME_DARK)
box((2 * DW + 0.30, 0.04, 0.04), (0, YA - 0.045, POD + DH + 0.10), material=LIME)
for s in (-1, 1):
    hx, hy = s * (DW - 0.01), YA - 0.015
    ang = math.radians(-90 + s * 18)                                       # Flügel zeigt fast nach -y, leicht zur Seite
    ax = s * math.cos(math.radians(18)) * 0.0
    d = Vector((math.sin(math.radians(s * 22)), -math.cos(math.radians(22)), 0))
    c = Vector((hx, hy, 0)) + d * 0.115
    box((0.22, 0.03, DH - 0.02), (c.x, c.y, POD + 0.01 + (DH - 0.02) / 2),
        rot=(0, 0, math.degrees(math.atan2(d.y, d.x))), material=BRONZE)
    for k in range(3):
        e = Vector((hx, hy, 0)) + d * (0.05 + 0.06 * (k % 2) + 0.03)
        sphere(0.014, (e.x, e.y - 0.0, POD + 0.2 + k * 0.28), material=LIME_DARK, segs=5, rings=3)

# ---------------------------------------------------------------- Inneneinrichtung (durch die Tür sichtbar)
scroll_rack(0.0, YB - 0.14, 0.0, w=0.82, h=0.95, d=0.16, rows=5, cols=5, floor_z=POD)
for y in (0.30, 0.82, 1.34):
    scroll_rack(-CX + 0.18, y, 90.0, w=0.46, h=0.95, d=0.16, rows=5, cols=3, floor_z=POD)
    scroll_rack(CX - 0.18, y, -90.0, w=0.46, h=0.95, d=0.16, rows=5, cols=3, floor_z=POD)
# Lesetisch mit aufgeschlagenen Rollen und Bank
box((0.62, 0.30, 0.03), (0, 0.55, POD + 0.20), material=WOOD)
for sx in (-1, 1):
    box((0.04, 0.26, 0.19), (sx * 0.27, 0.55, POD + 0.095), material=WOOD_DARK)
scroll(-0.12, 0.55, POD + 0.232, yaw=8, length=0.18, r=0.016)
scroll(0.10, 0.50, POD + 0.232, yaw=-12, length=0.16, r=0.016)
bench(0.0, 0.88, yaw=0, length=0.50)

# ---------------------------------------------------------------- Mauerwerk: Ziegelbänder, Fenster, Sockel
brick_bands(-CX, CX, YF_P, YB, (POD + 0.10, POD + 1.30), faces=("-x", "+x"))
box((2 * CX, 0.012, 0.05), (0, YB + 0.006, POD + 0.10), material=TILE_DARK)
box((2 * CX, 0.012, 0.05), (0, YB + 0.006, POD + 1.30), material=TILE_DARK)
for y in (0.30, 0.82, 1.34):
    for s, face in ((-1, "-x"), (1, "+x")):
        window(face, s * CX, y, POD + 0.45, w=0.12, h=0.62, shutters=None, frame=LIME_DARK)
for x in (-0.55, 0.55):
    window("+y", YB, x, POD + 0.45, w=0.12, h=0.62, shutters=None, frame=LIME_DARK)
cornice(-CX, CX, YF_P, YB, Z_TOP - 0.03, material=LIME_DARK)

# ---------------------------------------------------------------- Vorhalle: Säulen, Gebälk
for s in (-1, 1):
    column(s * 0.42, YF_P + 0.08, POD + CH, r=0.058)
box((2 * CX, 0.12, 0.10), (0, YF_P + 0.02, POD + CH + 0.05), material=LIME_DARK)
box((2 * CX + 0.04, 0.04, 0.03), (0, YF_P - 0.05, POD + CH + 0.015), material=LIME_DARK)
for k in range(5):                                                                                      # Zierfries
    box((0.20, 0.012, 0.06), (-0.80 + k * 0.40, YF_P - 0.035, Z_TOP - 0.04), material=LIME_DARK)

# ---------------------------------------------------------------- Dach, Giebel
ridge = gable_roof(-CX, CX, YF_P, YB, Z_TOP, "y", TYMPANUM, overhang=0.10, pitch=22.0)
yg = YF_P - 0.005
ring(0.0, yg, Z_TOP + 0.17, 0.085, 0.013, "y", LIME, 14)
cyl(0.070, 0.012, (0.0, yg, Z_TOP + 0.17), rot=(90, 0, 0), material=LIME_DARK, verts=12)
sphere(0.03, (0.0, yg - 0.012, Z_TOP + 0.17), (1.0, 0.6, 1.0), BRONZE, segs=6, rings=4)
for sx in (-1, 1):
    for k in range(3):
        sphere(0.012, (sx * (0.11 + k * 0.045), yg, Z_TOP + 0.17 - k * 0.012), material=LEAF_DARK, segs=4, rings=2)
palmette(0.0, YF_P - 0.04, ridge - 0.02, 0.0, h=0.18)
palmette(0.0, YB + 0.04, ridge - 0.02, 180.0, h=0.18)
for sx in (-1, 1):
    palmette(sx * (CX + 0.05), YF_P - 0.04, Z_TOP, 0.0, h=0.12)

# ---------------------------------------------------------------- Säulenhof vor der Treppe
for s in (-1, 1):
    for y in (-1.70, -1.42, -1.14):
        column(s * 1.54, y, 0.82, r=0.042)
    box((0.12, 0.66 + 0.16, 0.08), (s * 1.54, -1.42, 0.86), material=LIME)
    box((0.14, 0.04, 0.05), (s * 1.54, -1.75, 0.865), material=LIME_DARK)
    bench(s * 1.30, -1.42, yaw=90, length=0.50)
path([(0.0, -1.64), (0.0, -1.06)], w=0.26)
philosopher(0.0, -1.36)
laurel_tree(-0.85, -1.15, 0.95)
laurel_tree(0.85, -1.15, 0.95)
pot_plant(-0.45, -1.70, 0.05, leaves=LAUREL)
pot_plant(0.45, -1.70, 0.05, leaves=LAUREL)
amphora(-1.00, -1.62)
herm(1.00, -1.62, yaw=0.0, h=0.36)

obj = variant("Bibliothek_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}")

export(spacing=6.0)
