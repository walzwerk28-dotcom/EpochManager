"""
Säulengarten (Antike), 2 x 2 Felder (4 x 4 m): Peristyl-Garten.
Säulengang an drei Seiten (links, rechts, hinten) mit Gebälk und Ziegelpultdach (Tegulae, Imbrices, Stirnziegel an der
Traufe, 20 Grad Neigung nach innen), das an den hinteren Ecken in Graten mit Gratziegeln zusammentrifft und vorn mit
Stirnbrettern endet; dahinter die verputzte Außenwand mit roter Sockelzone. Vorn zur Straße (-Y) offen zwischen zwei
Torpfeilern mit niedrigen Mauern. In der Mitte ein kleines Wasserbecken mit Brunnenschale, breite Kieswege in Kreuzform,
vier Beete mit geschnittenen Hecken und Blumen, zwei Bänke, eine Statue auf Sockel hinten und zwei Zypressen vorn.

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

W = 1.76                    # Außenkante (Wand) bei x = ±W und y = +W
CH = 0.78                   # Säulenhöhe
CR = 0.050
CPOS = 1.48                 # Lage der Säulenreihen
BEAM_Z = CH + 0.05
Y_FRONT_COL = -1.02         # die Seitengänge beginnen hier

PITCH_R = 20.0
X_IN = CPOS - 0.10                                          # Traufkante (innen), kleiner Überstand über dem Gebälk
DEPTH = W - X_IN
Z_IN = 0.92                                                 # Oberkante der Platte an der Traufe
Z_OUT = Z_IN + DEPTH * math.tan(math.radians(PITCH_R))      # Oberkante der Platte an der Außenwand
Y_A = Y_FRONT_COL - 0.12                                    # vorderes Ende der Seitendächer

# ---------------------------------------------------------------- Außenwände mit roter Sockelzone (hinten, links, rechts)
WALL_T = 0.10
WALL_H = Z_OUT - T - 0.01


def wall(x0, x1, y0, y1):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    sx, sy = x1 - x0, y1 - y0
    box((sx, sy, WALL_H), (cx, cy, WALL_H / 2), material=PLASTER_OCHRE)
    box((sx + 0.02, sy + 0.02, 0.07), (cx, cy, 0.035), material=STONE)
    box((sx + 0.012, sy + 0.012, 0.26), (cx, cy, 0.20), material=SOCLE_RED)


wall(-W, W, W - WALL_T, W)
wall(-W, -W + WALL_T, Y_FRONT_COL - 0.1, W)
wall(W - WALL_T, W, Y_FRONT_COL - 0.1, W)

# ---------------------------------------------------------------- Säulengänge: Säulen und Gebälk
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


# ---------------------------------------------------------------- Ziegelpultdach nach innen mit Gehrung an den hinteren Ecken
def roof_plane(a0, a1, b0, b1):
    """Dachfläche: Außenkante a0 -> a1 (bei z = Z_OUT), Innenkante b0 -> b1 (Traufe, z = Z_IN). Tegulae als Platte,
    Imbrices als Halbrohre senkrecht zur Außenkante, an den Gehrungen gekürzt, Stirnziegel an der Traufe."""
    prism([(a0[0], a0[1], Z_OUT), (a1[0], a1[1], Z_OUT), (b1[0], b1[1], Z_IN), (b0[0], b0[1], Z_IN)], T, TILE)
    ax, ay = a1[0] - a0[0], a1[1] - a0[1]
    length = math.hypot(ax, ay)
    ux, uy = ax / length, ay / length
    mx, my = (a0[0] + a1[0]) / 2, (a0[1] + a1[1]) / 2
    nx, ny = -uy, ux
    if nx * (0 - mx) + ny * (0 - my) < 0:                              # Normale zeigt zur Gartenmitte
        nx, ny = -nx, -ny
    s0 = (b0[0] - a0[0]) * ux + (b0[1] - a0[1]) * uy                   # Lage der Traufecken entlang der Außenkante
    s1 = (b1[0] - a0[0]) * ux + (b1[1] - a0[1]) * uy
    count = max(2, round(length / 0.16))
    for k in range(1, count):
        s = k * length / count
        f = 1.0
        if s0 > 1e-6:
            f = min(f, s / s0)
        if length - s1 > 1e-6:
            f = min(f, (length - s) / (length - s1))
        run = DEPTH * f
        if run < 0.10:
            continue
        px, py = a0[0] + ux * s, a0[1] + uy * s
        z_end = Z_OUT + (Z_IN - Z_OUT) * f
        rod((px, py, Z_OUT + 0.025), (px + nx * run, py + ny * run, z_end + 0.025), 0.030,
            TILE if random.random() > 0.3 else TILE_DARK, verts=6)
    for d in (0.30, 0.55):                                             # Stoßfugen quer zur Schräge
        t = d / DEPTH
        if t >= 1:
            continue
        p0 = (a0[0] + (b0[0] - a0[0]) * t, a0[1] + (b0[1] - a0[1]) * t, Z_OUT + (Z_IN - Z_OUT) * t + 0.005)
        p1 = (a1[0] + (b1[0] - a1[0]) * t, a1[1] + (b1[1] - a1[1]) * t, Z_OUT + (Z_IN - Z_OUT) * t + 0.005)
        bar(p0, p1, 0.012, TILE_DARK)
    # Stirnziegel entlang der Traufe
    il = math.hypot(b1[0] - b0[0], b1[1] - b0[1])
    n = max(2, round(il / 0.14))
    yaw = math.degrees(math.atan2(b1[1] - b0[1], b1[0] - b0[0]))
    for k in range(n + 1):
        t = k / n
        box((0.06, 0.016, 0.07), (b0[0] + (b1[0] - b0[0]) * t - nx * 0.008, b0[1] + (b1[1] - b0[1]) * t - ny * 0.008,
                                  Z_IN + 0.035), rot=(0, 0, yaw), material=TILE_DARK)
    bar((b0[0], b0[1], Z_IN - 0.03), (b1[0], b1[1], Z_IN - 0.03), 0.035, WOOD_DARK)   # Traufbrett


# hinten: außen von links nach rechts, die Gehrungen laufen zu den inneren Ecken (±X_IN, X_IN)
roof_plane((-W, W), (W, W), (-X_IN, X_IN), (X_IN, X_IN))
# links und rechts: von der hinteren Ecke nach vorn
for s in (-1, 1):
    roof_plane((s * W, W), (s * W, Y_A), (s * X_IN, X_IN), (s * X_IN, Y_A))
    # Grat mit Gratziegeln
    rod((s * (W - 0.05), W - 0.05, Z_OUT - 0.05 * math.tan(math.radians(PITCH_R)) + 0.03), (s * X_IN, X_IN, Z_IN + 0.03), 0.040, TILE_DARK, verts=8)
    # Stirnbrett am vorderen Ende der Seitendächer (Ortgang)
    bar((s * W, Y_A - 0.012, Z_OUT + 0.01), (s * X_IN, Y_A - 0.012, Z_IN + 0.01), 0.045, WOOD_DARK)
    box((0.03, 0.03, 0.05), (s * W, Y_A - 0.012, Z_OUT - 0.005), material=WOOD_DARK)

# Kletterrosen an den Säulen (Ranken, die ins Dach wachsen, entfallen)
for x, y in ((-CPOS, CPOS), (CPOS, CPOS), (-CPOS, Y_FRONT_COL), (CPOS, Y_FRONT_COL)):
    for k in range(3):
        sphere(0.04, (x + 0.03 * (-1) ** k, y - 0.045, 0.18 + k * 0.2), (0.9, 0.9, 1.0), LEAF_DARK, segs=5, rings=3)
        if k == 2:
            sphere(0.022, (x + 0.05, y - 0.05, 0.58), material=BLOOM_RED, segs=4, rings=2)

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

# ---------------------------------------------------------------- kleines Wasserbecken mit Brunnenschale (rund 0,8 m)
cyl(0.40, 0.13, (0, 0, 0.065), material=LIME, verts=14)
cyl(0.40, 0.02, (0, 0, 0.14), material=LIME_DARK, verts=14)
cyl(0.33, 0.02, (0, 0, 0.145), material=WATER, verts=14)
cyl(0.055, 0.24, (0, 0, 0.25), material=LIME, verts=10)
cone(0.07, 0.17, 0.06, (0, 0, 0.40), LIME, verts=14)
cyl(0.15, 0.012, (0, 0, 0.436), material=WATER, verts=14)
for k in range(4):                                                     # Wasserstrahlen
    a = math.radians(45 + k * 90)
    cone(0.010, 0.004, 0.12, (0.13 * math.cos(a), 0.13 * math.sin(a), 0.22), WATER, verts=5)

# ---------------------------------------------------------------- Wege in Kreuzform (breiter)
path([(0.0, -1.62), (0.0, -0.52)], w=0.26)
path([(0.0, 0.52), (0.0, 1.02)], w=0.26)
path([(-1.24, 0.0), (-0.52, 0.0)], w=0.26)
path([(0.52, 0.0), (1.24, 0.0)], w=0.26)


# ---------------------------------------------------------------- vier Beete mit Heckenrahmen
def quadrant(x0, x1, y0, y1, bloom, foliage, seed):
    t = 0.12
    hedge(x0, x1, y0, y0 + t, 0.20)
    hedge(x0, x1, y1 - t, y1, 0.20)
    hedge(x0, x0 + t, y0 + t, y1 - t, 0.20)
    hedge(x1 - t, x1, y0 + t, y1 - t, 0.20)
    flower_bed(x0 + t + 0.02, x1 - t - 0.02, y0 + t + 0.02, y1 - t - 0.02, bloom=bloom, foliage=foliage, seed=seed,
               step=0.17)


quadrant(-1.30, -0.22, -1.50, -0.24, BLOOM_YELLOW, LEAF, 31)
quadrant(0.22, 1.30, -1.50, -0.24, BLOOM_RED, LEAF_DARK, 32)
quadrant(-1.30, -0.22, 0.24, 1.14, BLOOM_PINK, HERB_LIGHT, 33)
quadrant(0.22, 1.30, 0.24, 1.14, BLOOM_WHITE, LEAF, 34)

# ---------------------------------------------------------------- Bänke, Statue, Zypressen
bench(-1.38, 0.0, yaw=90, length=0.46)
bench(1.38, 0.0, yaw=90, length=0.46)
statue(0.0, 1.30, h=0.58)
cypress(-1.44, -1.52, 1.25, 0.12)
cypress(1.44, -1.52, 1.25, 0.12)
amphora(-0.80, 1.38)
amphora(0.80, 1.38)

obj = variant("Saeulengarten_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}")

export(spacing=6.0)
