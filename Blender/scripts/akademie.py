"""
Akademie (Antike), 2 x 3 Felder (4 x 6 m): Gymnasion-artige Anlage. Vorn zur Straße ein Torbau (kleine Vorhalle mit zwei
Säulen und Giebel). Dahinter ein Peristylhof mit Säulengang an drei Seiten (links, rechts und vorn hinter dem Tor) unter
Ziegelpultdächern, die an den Ecken sauber auf Gehrung zusammenlaufen (wie beim Säulengarten); in den Gängen seitlich
Nischen mit Rollenregal und eine Herme. Im Hof ein kleines Brunnenbecken, eine Sonnenuhr auf Säule, Lorbeerbäume, Bänke
und ein Steinplattenweg. Hinten der Hörsaal: offene Halle mit zwei Säulen zwischen Anten und Giebeldach, darin eine
halbrunde Marmorbank (Exedra) und davor der Lehrerstuhl; in den Seitengärten Zypressen und Büsche.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/akademie.py -- <blend> <fbx> [<vorschau.png>]

Vorderseite nach -Y, alles innerhalb von 3,61 x 5,41 m (Grundfläche 4 x 6 m). Die FBX enthält ein Objekt (Akademie_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403
import roemisch  # noqa: E402

random.seed(74)

# ---------------------------------------------------------------- Peristylhof (in eigenem Rahmen gebaut, dann gedreht)
# Rahmen: Hofmitte im Ursprung, Außenwand "hinten" bei y = +HL (wird zur Straßenseite), offenes Ende bei y = -HL.
W = 1.72                     # Außenkante der Seitenwände
HL = 1.25                    # halbe Hoflänge
CPX = 1.44                   # Säulenreihe seitlich
CPY = HL - 0.28              # Säulenreihe an der Straßenseite
CH = 0.78
BEAM_Z = CH + 0.05
Y_COL_END = -1.10            # letzte Säule an den Seiten
Y_A = Y_COL_END - 0.12       # offenes Ende der Seitendächer
PITCH_R = 20.0
X_IN, Y_IN = CPX - 0.10, CPY - 0.10
DEPTH = W - X_IN
Z_IN = 0.92
Z_OUT = Z_IN + DEPTH * math.tan(math.radians(PITCH_R))
WALL_T = 0.10
WALL_H = Z_OUT - T - 0.01
GATE_HW = 0.30               # halbe Breite der Toröffnung

n_court = len(roemisch.PARTS)


def wall(x0, x1, y0, y1):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    sx, sy = x1 - x0, y1 - y0
    box((sx, sy, WALL_H), (cx, cy, WALL_H / 2), material=PLASTER_OCHRE)
    box((sx + 0.02, sy + 0.02, 0.07), (cx, cy, 0.035), material=STONE)
    box((sx + 0.012, sy + 0.012, 0.26), (cx, cy, 0.20), material=SOCLE_RED)


wall(-W, -GATE_HW, HL - WALL_T, HL)                        # Straßenwand mit Toröffnung
wall(GATE_HW, W, HL - WALL_T, HL)
wall(-W, -W + WALL_T, Y_COL_END - 0.1, HL)
wall(W - WALL_T, W, Y_COL_END - 0.1, HL)

for k in range(6):                                          # Säulen an der Straßenseite
    column(-CPX + k * 2 * CPX / 5, CPY, CH, r=0.050)
for s in (-1, 1):
    for k in range(4):
        column(s * CPX, CPY - (k + 1) * (CPY - Y_COL_END) / 4, CH, r=0.050)
box((2 * CPX + 0.16, 0.12, 0.10), (0, CPY, BEAM_Z), material=LIME)
for s in (-1, 1):
    ya, yb = Y_COL_END - 0.06, CPY - 0.06
    box((0.12, yb - ya, 0.10), (s * CPX, (ya + yb) / 2, BEAM_Z), material=LIME)
    box((0.14, 0.04, 0.12), (s * CPX, Y_COL_END - 0.07, BEAM_Z), material=LIME_DARK)


def roof_plane(a0, a1, b0, b1):
    """Dachfläche: Außenkante a0 -> a1 (bei z = Z_OUT), Innenkante b0 -> b1 (Traufe, z = Z_IN). Tegulae als Platte,
    Imbrices als Halbrohre senkrecht zur Außenkante, an den Gehrungen gekürzt, Stirnziegel an der Traufe."""
    prism([(a0[0], a0[1], Z_OUT), (a1[0], a1[1], Z_OUT), (b1[0], b1[1], Z_IN), (b0[0], b0[1], Z_IN)], T, TILE)
    ax, ay = a1[0] - a0[0], a1[1] - a0[1]
    length = math.hypot(ax, ay)
    ux, uy = ax / length, ay / length
    mx, my = (a0[0] + a1[0]) / 2, (a0[1] + a1[1]) / 2
    nx, ny = -uy, ux
    if nx * (0 - mx) + ny * (0 - my) < 0:                              # Normale zeigt zur Hofmitte
        nx, ny = -nx, -ny
    s0 = (b0[0] - a0[0]) * ux + (b0[1] - a0[1]) * uy
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
    for d in (0.30,):                                                  # Stoßfuge quer zur Schräge
        t = d / DEPTH
        p0 = (a0[0] + (b0[0] - a0[0]) * t, a0[1] + (b0[1] - a0[1]) * t, Z_OUT + (Z_IN - Z_OUT) * t + 0.005)
        p1 = (a1[0] + (b1[0] - a1[0]) * t, a1[1] + (b1[1] - a1[1]) * t, Z_OUT + (Z_IN - Z_OUT) * t + 0.005)
        bar(p0, p1, 0.012, TILE_DARK)
    il = math.hypot(b1[0] - b0[0], b1[1] - b0[1])
    n = max(2, round(il / 0.14))
    yaw = math.degrees(math.atan2(b1[1] - b0[1], b1[0] - b0[0]))
    for k in range(n + 1):
        t = k / n
        box((0.06, 0.016, 0.07), (b0[0] + (b1[0] - b0[0]) * t - nx * 0.008, b0[1] + (b1[1] - b0[1]) * t - ny * 0.008,
                                  Z_IN + 0.035), rot=(0, 0, yaw), material=TILE_DARK)
    bar((b0[0], b0[1], Z_IN - 0.03), (b1[0], b1[1], Z_IN - 0.03), 0.035, WOOD_DARK)   # Traufbrett


roof_plane((-W, HL), (W, HL), (-X_IN, Y_IN), (X_IN, Y_IN))
for s in (-1, 1):
    roof_plane((s * W, HL), (s * W, Y_A), (s * X_IN, Y_IN), (s * X_IN, Y_A))
    rod((s * (W - 0.05), HL - 0.05, Z_OUT - 0.05 * math.tan(math.radians(PITCH_R)) + 0.03), (s * X_IN, Y_IN, Z_IN + 0.03),
        0.040, TILE_DARK, verts=8)                                                     # Grat mit Gratziegeln
    bar((s * W, Y_A - 0.012, Z_OUT + 0.01), (s * X_IN, Y_A - 0.012, Z_IN + 0.01), 0.045, WOOD_DARK)   # Stirnbrett
    box((0.03, 0.03, 0.05), (s * W, Y_A - 0.012, Z_OUT - 0.005), material=WOOD_DARK)

# in den Seitengängen: Nischen mit Rollenregal (links) und Herme (rechts), im Rahmen des Hofs
for s, y in ((-1, 0.10), (1, 0.10)):
    xw = s * (W - WALL_T)                                    # Innenseite der Seitenwand
    box((0.03, 0.62, 0.62), (xw - s * 0.015, y, 0.31 + 0.03), material=LIME_DARK)       # Nischenrahmen
    box((0.04, 0.70, 0.04), (xw - s * 0.02, y, 0.70), material=LIME)
    box((0.04, 0.04, 0.66), (xw - s * 0.02, y - 0.34, 0.33), material=LIME)
    box((0.04, 0.04, 0.66), (xw - s * 0.02, y + 0.34, 0.33), material=LIME)
scroll_rack(-W + WALL_T + 0.10, 0.10, 90.0, w=0.46, h=0.58, d=0.16, rows=4, cols=3)   # frontal zur Mitte (+x)
herm(W - WALL_T - 0.08, 0.10, yaw=0.0, h=0.40)
amphora(-CPX + 0.10, -0.55)
amphora(CPX - 0.10, -0.55)

# --- Hof drehen: Straßenwand nach -y, offenes Ende nach +y; Hofmitte auf world y = -0.80
bpy.context.view_layer.update()
YC = -0.80
M = Matrix.Translation((0.0, YC, 0.0)) @ Matrix.Rotation(math.radians(180.0), 4, "Z")
for o in roemisch.PARTS[n_court:]:
    o.matrix_world = M @ o.matrix_world
bpy.context.view_layer.update()

# ---------------------------------------------------------------- Torbau (kleine Vorhalle mit Giebel)
Y_G0, Y_G1 = -2.58, -2.06         # Vorderkante, Rückseite
GZ = 1.02                         # Höhe der Pfeiler
for s in (-1, 1):
    box((0.18, Y_G1 - Y_G0, GZ), (s * 0.55, (Y_G0 + Y_G1) / 2, GZ / 2), material=LIME)
    box((0.22, Y_G1 - Y_G0 + 0.04, 0.09), (s * 0.55, (Y_G0 + Y_G1) / 2, 0.045), material=LIME_DARK)
    column(s * 0.30, Y_G0 + 0.08, GZ, r=0.050)
box((1.44, 0.16, 0.10), (0, Y_G0 + 0.08, GZ + 0.05), material=LIME_DARK)                    # Architrav vorn
box((1.44, Y_G1 - Y_G0, 0.10), (0, (Y_G0 + Y_G1) / 2, GZ + 0.05), material=LIME)             # Decke über dem Durchgang
for k in range(4):
    box((0.20, 0.012, 0.06), (-0.54 + k * 0.36, Y_G0 + 0.002, GZ + 0.05), material=LIME_DARK)
ridge_g = gable_roof(-0.72, 0.72, Y_G0, Y_G1, GZ + 0.10, "y", TYMPANUM, overhang=0.09, pitch=24.0)
ring(0.0, Y_G0 - 0.005, GZ + 0.10 + 0.14, 0.07, 0.011, "y", LIME, 12)
sphere(0.03, (0.0, Y_G0 - 0.012, GZ + 0.24), (1.0, 0.6, 1.0), BRONZE, segs=6, rings=4)
palmette(0.0, Y_G0 - 0.03, ridge_g - 0.02, 0.0, h=0.15)
for sx in (-1, 1):
    palmette(sx * 0.73, Y_G0 - 0.03, GZ + 0.10, 0.0, h=0.11)
for sx in (-1, 1):
    pot_plant(sx * 0.80, -2.50, 0.06, leaves=LAUREL)
# Tafel mit Aufschrift ACADEMIA neben dem Tor und ein Steinblock als Schwelle
box((0.9, 0.12, 0.03), (0, -2.64, 0.015), material=LIME_DARK)
box((0.7, 0.10, 0.03), (0, -2.64, 0.045), material=LIME)

# ---------------------------------------------------------------- Hörsaal hinten
HX = 1.0                          # halbe Breite (Außenkante)
HY0, HY1 = 0.45, 2.60
H_COL = 1.12
Z_HALL = H_COL + 0.10
box((2 * HX + 0.12, HY1 - HY0 + 0.06, 0.10), (0, (HY0 + HY1) / 2, 0.05), material=LIME_DARK)        # niedrige Plattform
box((2 * HX, HY1 - HY0, 0.03), (0, (HY0 + HY1) / 2, 0.115), material=PAVE_WORN)
for s in (-1, 1):
    box((0.12, HY1 - HY0, Z_HALL), (s * (HX - 0.06), (HY0 + HY1) / 2, Z_HALL / 2), material=LIME)
    box((0.16, 0.14, Z_HALL), (s * (HX - 0.07), HY0 + 0.07, Z_HALL / 2), material=LIME_DARK)       # Antenkopf
box((2 * HX, 0.12, Z_HALL), (0, HY1 - 0.06, Z_HALL / 2), material=LIME)
box((2 * HX - 0.2, HY1 - HY0, 0.06), (0, (HY0 + HY1) / 2, Z_HALL - 0.03), material=LIME)            # Decke
for s in (-1, 1):
    column(s * 0.42, HY0 + 0.12, H_COL, r=0.055)
box((2 * HX, 0.12, 0.10), (0, HY0 + 0.06, H_COL + 0.05), material=LIME_DARK)
for k in range(5):
    box((0.18, 0.012, 0.06), (-0.80 + k * 0.40, HY0 - 0.002, H_COL + 0.05), material=LIME_DARK)
cornice(-HX, HX, HY0, HY1, Z_HALL - 0.02, material=LIME_DARK)
brick_bands(-HX, HX, HY0, HY1, (0.30, 0.60), faces=("-x", "+x"))
box((2 * HX, 0.012, 0.05), (0, HY1 + 0.006, 0.30), material=TILE_DARK)
box((2 * HX, 0.012, 0.05), (0, HY1 + 0.006, 0.60), material=TILE_DARK)
for y in (1.00, 1.65, 2.25):
    for s, face in ((-1, "-x"), (1, "+x")):
        window(face, s * HX, y, 0.72, w=0.14, h=0.30, shutters=None, frame=LIME_DARK)
ridge = gable_roof(-HX, HX, HY0, HY1, Z_HALL, "y", TYMPANUM, overhang=0.09, pitch=22.0)
yg = HY0 - 0.005
ring(0.0, yg, Z_HALL + 0.15, 0.08, 0.012, "y", LIME, 14)
cyl(0.065, 0.012, (0.0, yg, Z_HALL + 0.15), rot=(90, 0, 0), material=LIME_DARK, verts=12)
sphere(0.03, (0.0, yg - 0.012, Z_HALL + 0.15), (1.0, 0.6, 1.0), BRONZE, segs=6, rings=4)
palmette(0.0, HY0 - 0.04, ridge - 0.02, 0.0, h=0.17)
palmette(0.0, HY1 + 0.04, ridge - 0.02, 180.0, h=0.17)
for sx in (-1, 1):
    palmette(sx * (HX + 0.05), HY0 - 0.04, Z_HALL, 0.0, h=0.11)
# Inneres: Exedra aus Marmor, Lehrerstuhl, Podest, Tafel an der Rückwand
exedra(0.0, 1.80, 0.62, 0, 180, seat_z=0.30)
box((0.50, 0.30, 0.04), (0, 1.15, 0.13), material=LIME)
cathedra(0.0, 1.15, 180.0, z0=0.15)
box((0.50, 0.012, 0.28), (0, HY1 - 0.126, 0.72), material=PLASTER_WHITE)
scroll(0.62, 1.15, 0.24, yaw=70, length=0.14, r=0.016)

# ---------------------------------------------------------------- Hofinhalt
laurel_tree(-0.95, -0.55, 1.5)
laurel_tree(0.98, -1.20, 1.4)
fountain_bowl(0.0, -0.65, r=0.20)
water_jet((0.0, -0.65, 0.34), (0.0, -0.65, 0.19), thick=0.016)
sundial(0.70, -0.20, h=0.52)
bench(-1.16, -1.25, yaw=90, length=0.50)
bench(1.16, -0.20, yaw=90, length=0.50)
path([(0.0, -1.95), (0.0, -1.10)], w=0.22)
path([(0.0, 0.05), (0.0, 0.40)], w=0.22)
for sx in (-1, 1):                                                    # Seitengärten neben dem Hörsaal
    cypress(sx * 1.38, 1.20, 1.05, 0.11)
    cypress(sx * 1.38, 2.10, 1.15, 0.12)
    bush(sx * 1.38, 1.65, 0.14, leaves=LAUREL)
    bush(sx * 1.30, 0.70, 0.12, leaves=LEAF_DARK)
    box((0.12, 2.10, 0.30), (sx * 1.64, 1.55, 0.15), material=PLASTER_OCHRE)               # Gartenmauer
    box((0.17, 2.14, 0.04), (sx * 1.64, 1.55, 0.32), material=TILE_DARK)
    box((0.14, 2.14, 0.07), (sx * 1.64, 1.55, 0.035), material=STONE)

obj = variant("Akademie_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}")

export(spacing=8.0)
