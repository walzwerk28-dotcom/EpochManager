"""
Säulengarten (Antike), 2 x 2 Felder (4 x 4 m): Peristyl-Garten.
Säulengang an drei Seiten (links, rechts, hinten) mit Gebälk und flachem Ziegelpultdach nach innen, dahinter die
verputzte Außenwand mit roter Sockelzone; vorn zur Straße (-Y) offen zwischen zwei Torpfeilern mit niedrigen Mauern.
In der Mitte ein Wasserbecken mit Brunnenschale, Kiesweg in Kreuzform, vier Beete mit geschnittenen Hecken und Blumen,
zwei Bänke, eine Statue auf Sockel hinten und zwei Zypressen vorn; Ranken auf dem Gebälk.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/saeulengarten.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,56 x 3,56 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Saeulengarten_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(21)

W = 1.76                    # Außenkante (Wand) bei x = ±W und y = +W; vorn y = -W
CH = 0.78                   # Säulenhöhe
CR = 0.050
CPOS = 1.48                 # Lage der Säulenreihen
BEAM_Z = CH + 0.05
Y_FRONT_COL = -1.02         # die Seitengänge beginnen hier

# ---------------------------------------------------------------- Außenwände mit roter Sockelzone (hinten, links, rechts)
WALL_T = 0.10
WALL_H = 0.90


def wall(x0, x1, y0, y1):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    sx, sy = x1 - x0, y1 - y0
    box((sx, sy, WALL_H), (cx, cy, WALL_H / 2), material=PLASTER_OCHRE)
    box((sx + 0.02, sy + 0.02, 0.07), (cx, cy, 0.035), material=STONE)
    box((sx + 0.012, sy + 0.012, 0.26), (cx, cy, 0.20), material=SOCLE_RED)
    box((sx + 0.04, sy + 0.04, 0.035), (cx, cy, WALL_H + 0.0175), material=LIME_DARK)


wall(-W, W, W - WALL_T, W)
wall(-W, -W + WALL_T, Y_FRONT_COL - 0.1, W)
wall(W - WALL_T, W, Y_FRONT_COL - 0.1, W)

# ---------------------------------------------------------------- Säulengänge: Säulen, Gebälk, Ziegelpultdach nach innen
for k in range(6):                                                     # hinten
    column(-CPOS + k * 2 * CPOS / 5, CPOS, CH, r=CR)
for s in (-1, 1):                                                      # links und rechts
    for k in range(4):                                                 # ohne die Ecksäule, die gehört zur Rückreihe
        y = CPOS - (k + 1) * (CPOS - Y_FRONT_COL) / 4
        column(s * CPOS, y, CH, r=CR)
box((2 * CPOS + 0.16, 0.12, 0.10), (0, CPOS, BEAM_Z), material=LIME)
for s in (-1, 1):
    ya, yb = Y_FRONT_COL - 0.06, CPOS - 0.06
    box((0.12, yb - ya, 0.10), (s * CPOS, (ya + yb) / 2, BEAM_Z), material=LIME)
    box((0.14, 0.04, 0.12), (s * CPOS, Y_FRONT_COL - 0.07, BEAM_Z), material=LIME_DARK)   # Stirn des Gebälks

Z_OUT, Z_IN = 1.04, 0.915                                              # Dachfläche: außen hoch, innen niedrig
X_IN = CPOS - 0.10                                                     # Innenkante des Dachs (Traufe)


def roof_plane(top):
    """Dachplatte aus vier Punkten (außen a, außen b, innen b, innen a) plus Halbrohre quer zur Schräge."""
    prism(top, T, TILE_DARK)
    (ax, ay, az), (bx, by, bz), (cx, cy, cz), (dx, dy, dz) = top
    n = 9
    for i in range(1, n):
        t = i / n
        p0 = (ax + (bx - ax) * t, ay + (by - ay) * t, az + (bz - az) * t)
        p1 = (dx + (cx - dx) * t, dy + (cy - dy) * t, dz + (cz - dz) * t)
        rod((p0[0], p0[1], p0[2] + 0.02), (p1[0], p1[1], p1[2] + 0.02), 0.024, TILE, verts=6)


# hinten: außen bei y = W (hoch), innen bei y = X_IN
roof_plane([(-W, W, Z_OUT), (W, W, Z_OUT), (W, X_IN, Z_IN), (-W, X_IN, Z_IN)])
for s in (-1, 1):
    y_a, y_b = Y_FRONT_COL - 0.12, X_IN
    roof_plane([(s * W, y_a, Z_OUT), (s * W, y_b, Z_OUT), (s * X_IN, y_b, Z_IN), (s * X_IN, y_a, Z_IN)])

# Ranken auf dem Gebälk und an den Säulen
for k in range(11):
    x = -CPOS + k * 2 * CPOS / 10
    sphere(0.05, (x, CPOS - 0.02, BEAM_Z + 0.06), (1.2, 1.0, 0.6), LEAF if k % 2 else LEAF_DARK, segs=5, rings=3)
    if k % 4 == 1:
        sphere(0.022, (x + 0.03, CPOS - 0.07, BEAM_Z + 0.09), material=BLOOM_PINK, segs=4, rings=2)
for s in (-1, 1):
    for k in range(6):
        y = Y_FRONT_COL + k * (CPOS - Y_FRONT_COL) / 5
        sphere(0.05, (s * CPOS, y, BEAM_Z + 0.06), (1.0, 1.2, 0.6), LEAF_DARK if k % 2 else LEAF, segs=5, rings=3)
for x, y in ((-CPOS, CPOS), (CPOS, CPOS), (-CPOS, Y_FRONT_COL), (CPOS, Y_FRONT_COL)):   # Kletterrosen
    for k in range(3):
        sphere(0.04, (x + 0.03 * (-1) ** k, y, 0.18 + k * 0.2), (0.9, 0.9, 1.0), LEAF_DARK, segs=5, rings=3)
        if k == 2:
            sphere(0.022, (x + 0.05, y, 0.58), material=BLOOM_RED, segs=4, rings=2)

# ---------------------------------------------------------------- Front: niedrige Mauern, zwei Torpfeiler
FRONT = -1.68
for xa, xb in ((-W, -0.62), (0.62, W)):
    cx = (xa + xb) / 2
    box((xb - xa, 0.12, 0.30), (cx, FRONT, 0.15), material=PLASTER_OCHRE)
    box((xb - xa + 0.01, 0.14, 0.07), (cx, FRONT, 0.035), material=STONE)
    box((xb - xa + 0.03, 0.17, 0.04), (cx, FRONT, 0.32), material=LIME)
for s in (-1, 1):
    px = s * 0.52
    box((0.20, 0.20, 0.07), (px, FRONT, 0.035), material=LIME_DARK)
    box((0.15, 0.15, 0.66), (px, FRONT, 0.40), material=LIME)
    box((0.21, 0.21, 0.05), (px, FRONT, 0.755), material=LIME_DARK)
    sphere(0.065, (px, FRONT, 0.85), material=LIME, segs=8, rings=5)
pot_plant(-0.30, FRONT + 0.10, 0.05, leaves=LAUREL)
pot_plant(0.30, FRONT + 0.10, 0.05, leaves=LAUREL)

# ---------------------------------------------------------------- Wasserbecken mit Brunnenschale
cyl(0.54, 0.16, (0, 0, 0.08), material=LIME, verts=14)
cyl(0.54, 0.02, (0, 0, 0.17), material=LIME_DARK, verts=14)
cyl(0.46, 0.02, (0, 0, 0.175), material=WATER, verts=14)
cyl(0.09, 0.30, (0, 0, 0.30), material=LIME, verts=10)
cone(0.11, 0.26, 0.07, (0, 0, 0.50), LIME, verts=14)
cyl(0.23, 0.012, (0, 0, 0.54), material=WATER, verts=14)
for k in range(4):                                                     # Wasserstrahlen
    a = math.radians(45 + k * 90)
    cone(0.012, 0.004, 0.16, (0.20 * math.cos(a), 0.20 * math.sin(a), 0.22), WATER, verts=5)

# ---------------------------------------------------------------- Wege in Kreuzform
path([(0.0, -1.62), (0.0, -0.64)], w=0.20)
path([(0.0, 0.64), (0.0, 1.00)], w=0.20)
path([(-1.20, 0.0), (-0.64, 0.0)], w=0.20)
path([(0.64, 0.0), (1.20, 0.0)], w=0.20)


# ---------------------------------------------------------------- vier Beete mit Heckenrahmen
def quadrant(x0, x1, y0, y1, bloom, foliage, seed):
    t = 0.12
    hedge(x0, x1, y0, y0 + t, 0.20)
    hedge(x0, x1, y1 - t, y1, 0.20)
    hedge(x0, x0 + t, y0 + t, y1 - t, 0.20)
    hedge(x1 - t, x1, y0 + t, y1 - t, 0.20)
    flower_bed(x0 + t + 0.02, x1 - t - 0.02, y0 + t + 0.02, y1 - t - 0.02, bloom=bloom, foliage=foliage, seed=seed,
               step=0.17)


quadrant(-1.28, -0.22, -1.50, -0.26, BLOOM_YELLOW, LEAF, 31)
quadrant(0.22, 1.28, -1.50, -0.26, BLOOM_RED, LEAF_DARK, 32)
quadrant(-1.28, -0.22, 0.26, 1.10, BLOOM_PINK, HERB_LIGHT, 33)
quadrant(0.22, 1.28, 0.26, 1.10, BLOOM_WHITE, LEAF, 34)

# ---------------------------------------------------------------- Bänke, Statue, Zypressen
bench(-1.34, 0.0, yaw=90, length=0.46)
bench(1.34, 0.0, yaw=90, length=0.46)
statue(0.0, 1.22, h=0.58)
cypress(-1.44, -1.52, 1.25, 0.12)
cypress(1.44, -1.52, 1.25, 0.12)
amphora(-0.80, 1.34)
amphora(0.80, 1.34)

obj = variant("Saeulengarten_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}")

export(spacing=6.0)
