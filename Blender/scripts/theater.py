"""
Großes Theater (Antike), 4 x 4 Felder (8 x 8 m), Wunderbau in fünf Baustufen als Objekte GrossesTheater_0 .. _4 (feste
Reihenfolge, ConstructionStages zeigt Variante i = i fertige Bauphasen):
  0  Baugrube: abgesteckt mit Pflöcken und Schnur, Erdhaufen, Baustoffe, Dreibeinkran
  1  Fundament: Halbrund aus Kalksteinmauern (Speichen und Ringe, mit Erde verfüllt), gepflasterte Orchestra, Grundmauern
     von Bühne und Bühnenhaus, Gerüste, Baustoffe
  2  Sitzränge: Ränge zum Teil gebaut, oben noch im Bau, Bühnenhaus halb hoch mit Gerüst, Tretkran, Säulentrommeln
  3  Bühnenhaus: Ränge fertig mit Säulenhalle am oberen Rand, hohe Bühnenwand mit drei Toren und Säulen, Holzbühne, Dachstuhl
  4  fertig: Marmor-Orchestra, Bühnenwand mit Säulen in zwei Geschossen und Statuen, Ziegeldach, Sonnensegel über den
     oberen Rängen, Banner, Feuerschalen, Zypressen und Lorbeer
Die Tribüne (Cavea) öffnet sich zur Straße (-Y), die Bühnenwand steht hinten (+Y). Mittelpunkt der Cavea ist (0, 0).

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/theater.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 7,36 x 7,36 m (Grundfläche 8 x 8 m). Die FBX enthält GrossesTheater_0 bis GrossesTheater_4.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(88)

# ---------------------------------------------------------------- Maße
R_OR = 1.15          # Radius der Orchestra
N_ROWS = 8           # Sitzreihen
D_ROW = 0.29         # Tiefe einer Reihe
RISE = 0.17          # Höhe einer Stufe
Z_BASE = 0.05
R_OUT = R_OR + N_ROWS * D_ROW      # 3,47
ST = 0.35            # Oberkante des Bühnensockels
ST_TOP = 0.37        # Oberkante des Bühnenbodens
WY0, WY1, XW = 2.55, 3.55, 3.55    # Bühnenwand: Vorderkante, Rückkante, halbe Breite
STAGE_X = 3.45
AISLES = (198, 234, 270, 306, 342)
DOORS = ((0.0, 0.50, 0.82), (-1.9, 0.38, 0.66), (1.9, 0.38, 0.66))   # u, Breite, Höhe
LOWER_COLS = (-3.2, -2.55, -1.25, -0.62, 0.62, 1.25, 2.55, 3.2)
H_LOWER = 1.05
Z_ENT1 = 1.55
Z_ENT2 = 2.47

PM = PLASTER_MARBLE
SOIL_D = SOIL


def top(k):
    """Oberkante der Sitzreihe k."""
    return Z_BASE + RISE * (k + 1)


def pol(r, a, cx=0.0, cy=0.0):
    t = math.radians(a)
    return (cx + r * math.cos(t), cy + r * math.sin(t))


def cavea(rows, a0=180.0, a1=360.0, marble_first=True, top_m=None):
    for k in range(rows):
        sector_ring(0.0, 0.0, R_OR + k * D_ROW, R_OR + (k + 1) * D_ROW, 0.0, top(k), a0, a1, segs=max(6, round((a1 - a0) / 7.5)),
                    top_m=(MARBLE if (k == 0 and marble_first) else (top_m or LIME)), side_m=LIME_DARK, outer=True)


def aisles(rows):
    for a in AISLES:
        for k in range(rows):
            x, y = pol(R_OR + (k + 0.5) * D_ROW, a)
            box((D_ROW - 0.008, 0.15, 0.012), (x, y, top(k) + 0.006), rot=(0, 0, a), material=MARBLE)


def orchestra(marble):
    z = 0.05
    if marble:
        rings = ((0.01, 0.55, MARBLE), (0.55, 0.72, TERRACOTTA), (0.72, 0.95, MARBLE), (0.95, R_OR, LIME_DARK))
    else:
        rings = ((0.01, 0.55, PAVE_WORN), (0.55, 0.72, LIME_DARK), (0.72, 0.95, PAVE_WORN), (0.95, R_OR, LIME_DARK))
    for r0, r1, m in rings:
        sector_ring(0.0, 0.0, r0, r1, 0.0, z, segs=24, top_m=m, side_m=m, caps=False)


def arcade(finished):
    """Blendarkaden und Gesims an der Außenwand der Cavea (zur Straße)."""
    n = 14
    for k in range(n):
        a = 180 + (k + 0.5) * 180.0 / n
        x, y = pol(R_OUT + 0.004, a)
        box((0.03, 0.38, 0.50), (x, y, 0.25), rot=(0, 0, a), material=LIME_DARK)
        cyl(0.19, 0.03, (x, y, 0.50), rot=(0, 90, a), material=LIME_DARK, verts=10)
        x2, y2 = pol(R_OUT + 0.012, a)
        box((0.03, 0.30, 0.46), (x2, y2, 0.23), rot=(0, 0, a), material=DARK)
        cyl(0.15, 0.03, (x2, y2, 0.46), rot=(0, 90, a), material=DARK, verts=10)
        x3, y3 = pol(R_OUT + 0.006, a)
        box((0.03, 0.10, 0.20), (x3, y3, 1.02), rot=(0, 0, a), material=DARK)
        if finished:
            cyl(0.05, 0.03, (x3, y3, 1.12), rot=(0, 90, a), material=DARK, verts=8)
        ap = 180 + k * 180.0 / n
        xp, yp = pol(R_OUT + 0.01, ap)
        box((0.07, 0.08, 1.25), (xp, yp, 0.625), rot=(0, 0, ap), material=LIME)
    xp, yp = pol(R_OUT + 0.01, 360)
    box((0.07, 0.08, 1.25), (xp, yp, 0.625), rot=(0, 0, 360), material=LIME)
    sector_ring(0.0, 0.0, R_OUT - 0.02, R_OUT + 0.08, 1.30, 1.36, segs=24, top_m=LIME_DARK, side_m=LIME, outer=True, caps=False)
    sector_ring(0.0, 0.0, R_OUT, R_OUT + 0.06, 0.0, 0.10, segs=24, top_m=STONE, side_m=STONE, outer=True, caps=False)


def stage_platform(planks, finished=False):
    """Bühnensockel (Oberkante ST) mit Nischen an der Vorderseite und Bohlen- oder Steinbelag."""
    box((2 * STAGE_X, 2.5, ST), (0, 1.25, ST / 2), material=LIME_DARK)
    for k in range(8):
        x = -3.0 + k * 0.857
        box((0.40, 0.04, 0.20), (x, 0.0, 0.17), material=DARK)
        box((0.50, 0.03, 0.04), (x, -0.005, 0.30), material=LIME)
    box((2 * STAGE_X + 0.04, 0.05, 0.05), (0, -0.015, ST - 0.02), material=LIME)
    if planks:
        n = 12
        for i in range(n):
            w = 2 * STAGE_X / n
            box((w - 0.012, 2.5, 0.02), (-STAGE_X + (i + 0.5) * w, 1.25, ST + 0.01), material=WOOD)
    else:
        box((2 * STAGE_X, 2.5, 0.02), (0, 1.25, ST + 0.01), material=LIME)


def skene_body(heights, with_bands=True):
    """Bühnenwand als Block aus Segmenten (x0, x1, h)."""
    for x0, x1, h in heights:
        box((x1 - x0, WY1 - WY0, h), ((x0 + x1) / 2, (WY0 + WY1) / 2, h / 2), material=LIME)
        if with_bands:
            zs = [0.5 + 0.35 * k for k in range(int((h - 0.4) / 0.35) + 1) if 0.5 + 0.35 * k < h - 0.1]
            for z in zs:
                box((x1 - x0 + 0.01, 0.012, 0.05), ((x0 + x1) / 2, WY0 - 0.006, z), material=TILE_DARK)
                box((x1 - x0 + 0.01, 0.012, 0.05), ((x0 + x1) / 2, WY1 + 0.006, z), material=TILE_DARK)
            for x in (x0, x1):
                if abs(x) >= XW - 0.01:
                    for z in zs:
                        box((0.012, WY1 - WY0, 0.05), (x + (0.006 if x > 0 else -0.006), (WY0 + WY1) / 2, z), material=TILE_DARK)
    box((2 * XW + 0.06, WY1 - WY0 + 0.06, 0.09), (0, (WY0 + WY1) / 2, 0.045), material=STONE)


def skene_doors(z0=ST_TOP, leaf=None):
    leaf = leaf or BRONZE_DOOR
    for u, w, h in DOORS:
        door("-y", WY0, u, w=w, h=h, z0=z0 + 0.02, frame=MARBLE, leaf=leaf)


def porticus(finished):
    """Säulenhalle am oberen Rand der Cavea: 13 Säulen auf dem letzten Rang mit Gebälk (nur Architrav und Kranzgesims)."""
    z0 = top(N_ROWS - 1)
    pts = []
    for k in range(13):
        a = 270 + (k - 6) * 14.0
        x, y = pol(R_OUT - 0.09, a)
        pts.append((x, y))
        lift(z0, lambda x=x, y=y: column(x, y, 0.74, r=0.04))
    zt = z0 + 0.78
    for (xa, ya), (xb, yb) in zip(pts, pts[1:]):
        bar((xa, ya, zt), (xb, yb, zt), 0.075, LIME)
        bar((xa, ya, zt + 0.045), (xb, yb, zt + 0.045), 0.045, MARBLE if finished else LIME_DARK)
    return pts, zt


# ================================================================ 0 Baugrube
def peg_line(points, z=0.24, closed=False):
    pts = points + ([points[0]] if closed else [])
    for x, y in points:
        box((0.05, 0.05, 0.30), (x, y, 0.15), rot=(0, 0, random.uniform(0, 90)), material=WOOD_FRESH)
        box((0.058, 0.058, 0.04), (x, y, 0.29), rot=(0, 0, 0), material=CLOTH_RED)
    for (xa, ya), (xb, yb) in zip(pts, pts[1:]):
        rope((xa, ya, z), (xb, yb, z), sag=0.02, thick=0.010, segs=2)


def stage0():
    sector_ring(0.0, 0.0, 0.01, R_OUT - 0.02, 0.0, 0.04, segs=24, top_m=SOIL_D, side_m=SOIL_D, caps=False, outer=True)
    box((2 * STAGE_X, 3.45, 0.04), (0, 1.72, 0.02), material=SOIL_D)
    for r, a, z in ((R_OUT + 0.06, 0, 0.0),):
        sector_ring(0.0, 0.0, 3.34, 3.56, 0.0, 0.10, segs=24, top_m=SOIL_D, side_m=SOIL_D, caps=False, outer=True)
    for a in AISLES:                                                       # Kreidelinien der Gänge
        x, y = pol((R_OR + R_OUT) / 2, a)
        box((R_OUT - R_OR - 0.1, 0.022, 0.012), (x, y, 0.046), rot=(0, 0, a), material=LIME)
    arc = [pol(R_OUT - 0.1, a) for a in range(180, 361, 15)]
    peg_line(arc + [(3.45, 3.45), (-3.45, 3.45), (-3.45, 0.0)] + [], closed=False)
    peg_line([(-3.45, 0.0), (-3.45, 1.7)])
    peg_line([pol(R_OR, a) for a in range(180, 361, 20)])
    peg_line([(-3.45, 0.0), (3.45, 0.0)])
    peg_line([(-3.45, WY0), (3.45, WY0)])
    for x, y, rx, ry, rz in ((-3.0, -2.8, 0.55, 0.45, 0.24), (3.05, -2.85, 0.6, 0.5, 0.26), (-1.9, 1.5, 0.5, 0.4, 0.2),
                             (2.0, 1.2, 0.55, 0.45, 0.22), (0.0, -3.2, 0.5, 0.3, 0.16)):
        sphere(1.0, (x, y, 0.02), (rx, ry, rz), SOIL_D, segs=8, rings=4)
    for x, y in ((-3.0, -2.8), (3.05, -2.85)):
        sx = 0.5 if x < 0 else -0.5
        spade((x + sx, y + 0.35, 0.18), (x + sx * 1.16, y + 0.42, 0.62), across=(0.7, -0.7, 0.0))
    wheelbarrow(1.1, -1.9, yaw=35, load="soil")
    wheelbarrow(-1.4, 1.2, yaw=-70, load=None)
    ashlar_stack(-2.7, 2.15, 3, 2, 3, yaw=0, seed=1)
    ashlar_stack(-1.6, 2.35, 2, 2, 2, yaw=10, seed=2)
    plank_stack(2.9, 2.1, 0.5, 1.1, 5, yaw=0, seed=3)
    beam_stack(1.7, 2.35, 1.2, yaw=90, seed=4)
    sack_stack(0.3, 2.3, yaw=0)
    barrel(3.2, 0.8, r=0.08)
    barrel(3.3, 1.05, r=0.08)
    crate(2.2, 0.55, size=0.2, yaw=20)
    rubble(-2.1, 0.7, 0.4, 14, seed=5)
    tripod_crane(-0.6, 2.1, height=1.4, spread=0.5, yaw=0, block_z=0.25, seed=2)
    log_piece((2.4, 1.6, 0.06), (2.4, 2.1, 0.06), 0.045)
    check_bounds(3.68, 3.68, "Stufe 0")
    variant("GrossesTheater_0")


# ================================================================ 1 Fundament
def stage1():
    orchestra(False)
    # Fundament des Halbrunds: Fülle aus Erde, Ringmauern und Speichenmauern aus Kalkstein
    sector_ring(0.0, 0.0, R_OR, R_OUT, 0.0, 0.13, segs=24, top_m=SOIL_D, side_m=SOIL_D, caps=False, outer=True)
    for r, h in ((R_OR + 0.03, 0.26), (1.95, 0.34), (2.6, 0.42), (R_OUT - 0.08, 0.52)):
        sector_ring(0.0, 0.0, r - 0.08, r + 0.08, 0.0, h, segs=24, top_m=LIME, side_m=LIME_DARK, outer=True)
    for k in range(13):
        a = 180 + k * 15
        x, y = pol((R_OR + R_OUT) / 2, a)
        box((R_OUT - R_OR, 0.16, 0.42), (x, y, 0.21), rot=(0, 0, a), material=LIME)
        box((R_OUT - R_OR + 0.01, 0.17, 0.04), (x, y, 0.42), rot=(0, 0, a), material=LIME_DARK)
    sector_ring(0.0, 0.0, R_OR, R_OR + D_ROW, 0.0, top(0), 200, 340, segs=14, top_m=MARBLE, side_m=LIME_DARK)
    # Bühnenfundament: Mauern ringsum und zwei Querwände, mit Erde verfüllt
    box((2 * STAGE_X, 2.5, 0.12), (0, 1.25, 0.06), material=SOIL_D)
    for x in (-STAGE_X, STAGE_X, -1.2, 1.2):
        box((0.22, 2.5, 0.34), (x, 1.25, 0.17), material=LIME)
    for y in (0.0, 1.25):
        box((2 * STAGE_X, 0.22, 0.34), (0, y, 0.17), material=LIME)
    # Grundmauern des Bühnenhauses
    for x in (-XW + 0.15, XW - 0.15, -1.2, 1.2):
        box((0.3, WY1 - WY0, 0.46), (x, (WY0 + WY1) / 2, 0.23), material=LIME)
    for y in (WY0 + 0.15, WY1 - 0.15):
        for x0, x1 in ((-XW, -2.1), (-1.7, -0.25), (0.25, 1.7), (2.1, XW)):
            box((x1 - x0, 0.3, 0.46), ((x0 + x1) / 2, y, 0.23), material=LIME)
    box((2 * XW, WY1 - WY0, 0.14), (0, (WY0 + WY1) / 2, 0.07), material=SOIL_D)
    brick_bands(-XW, XW, WY0, WY1, (0.2, 0.36), faces=("-y",))
    scaffold((-3.3, WY0), (-0.4, WY0), (0, -1), 1.0, w=0.24)
    scaffold((3.3, 0.3), (3.3, 2.3), (1, 0), 0.7, w=0.2)
    scaffold((-3.3, 2.4), (-3.3, 0.6), (-1, 0), 0.5, w=0.2)
    ashlar_stack(-2.9, -3.0, 3, 2, 3, yaw=0, seed=6)
    ashlar_stack(2.9, -3.0, 3, 2, 2, yaw=0, seed=7)
    plank_stack(1.3, 2.25, 0.5, 1.4, 4, yaw=90, seed=8)
    beam_stack(2.9, 1.5, 1.1, yaw=0, seed=9)
    sack_stack(-0.6, 1.5, yaw=20)
    barrel(-3.1, 0.35, r=0.08)
    rubble(-2.4, 1.3, 0.35, 12, seed=11)
    wheelbarrow(0.4, 1.7, yaw=100, load="soil")
    tripod_crane(-0.05, 1.15, height=1.45, spread=0.46, yaw=0, block_z=0.4, seed=4)
    check_bounds(3.68, 3.68, "Stufe 1")
    variant("GrossesTheater_1")


# ================================================================ 2 Sitzränge
def stage2():
    orchestra(False)
    cavea(6)
    # Reihe 6 bis zum Winkel 305, Reihe 7 nur bis 235; dahinter noch Erdfülle auf Höhe der Reihe 5
    sector_ring(0.0, 0.0, R_OR + 6 * D_ROW, R_OR + 7 * D_ROW, 0.0, top(6), 180, 307, segs=17, top_m=LIME, side_m=LIME_DARK, outer=True)
    sector_ring(0.0, 0.0, R_OR + 7 * D_ROW, R_OR + 8 * D_ROW, 0.0, top(7), 180, 232, segs=7, top_m=LIME, side_m=LIME_DARK, outer=True)
    sector_ring(0.0, 0.0, R_OR + 6 * D_ROW, R_OUT, 0.0, top(5) - 0.02, 307, 360, segs=7, top_m=SOIL_D, side_m=LIME_DARK, caps=False, outer=True)
    sector_ring(0.0, 0.0, R_OR + 7 * D_ROW, R_OUT, 0.0, top(6) - 0.02, 232, 307, segs=10, top_m=SOIL_D, side_m=LIME_DARK, caps=False, outer=True)
    aisles(5)
    for a, r, k in ((300, R_OR + 6.5 * D_ROW, 6), (226, R_OR + 7.5 * D_ROW, 7)):       # lose Quader am Baurand
        x, y = pol(r, a)
        box((0.30, 0.18, 0.14), (x, y, top(k) + 0.07), rot=(0, 0, a + 90), material=ASHLAR)
    # Bühnensockel (noch ohne Belag), Bühnenwand in Stufen halb hoch
    stage_platform(planks=False)
    skene_body([(-XW, -1.5, 1.45), (-1.5, 1.0, 1.15), (1.0, XW, 0.85)])
    for z in (1.0, 1.3):
        pass
    skene_doors(z0=0.0 + 0.04)
    scaffold((1.0, WY0), (3.4, WY0), (0, -1), 1.5, w=0.24)
    lift(ST, lambda: scaffold((-3.4, WY0), (-1.5, WY0), (0, -1), 1.0, w=0.24))
    lift(ST, lambda: treadwheel_crane(-2.35, 1.15, 0.0))
    # Säulentrommeln und Quader auf der Bühne
    for k, (x, y) in enumerate(((0.3, 1.5), (1.0, 1.1), (1.7, 1.55), (0.7, 0.55))):
        cyl(0.14, 0.34, (x, y, ST + 0.14), rot=(0, 90, 20 * k), material=LIME, verts=14)
    for k, (x, y) in enumerate(((2.4, 0.7), (2.9, 1.0))):
        cyl(0.14, 0.30, (x, y, ST + 0.14), rot=(0, 0, 0), material=LIME, verts=14)
        cyl(0.115, 0.30, (x, y, ST + 0.43), rot=(0, 0, 0), material=LIME_DARK, verts=14)
    lift(ST, lambda: ashlar_stack(-0.3, 0.6, 3, 2, 2, yaw=0, seed=12))
    lift(ST, lambda: rubble(-1.2, 2.1, 0.3, 10, seed=13))
    ashlar_stack(-3.0, -3.0, 3, 2, 3, yaw=0, seed=14)
    plank_stack(3.0, -3.0, 0.5, 1.0, 4, yaw=90, seed=15)
    wheelbarrow(-1.2, -2.2, yaw=20, load="soil")
    beam_stack(3.25, -1.5, 1.0, yaw=0, seed=16)
    barrel(-3.35, -1.9, r=0.08)
    sack_stack(-3.2, -2.2, yaw=70)
    # Gerüst und Leiter an der Cavea
    ladder(pol(R_OR + 6 * D_ROW + 0.05, 322) + (top(5),), pol(R_OR + 8 * D_ROW, 326) + (top(5) + 0.9,), (1, 0, 0), width=0.2)
    check_bounds(3.68, 3.68, "Stufe 2")
    variant("GrossesTheater_2")


# ================================================================ 3 Bühnenhaus
def stage3():
    orchestra(False)
    cavea(N_ROWS, marble_first=False)
    arcade(False)
    aisles(N_ROWS)
    pts, zt = porticus(False)
    stage_platform(planks=True)
    skene_body([(-XW, XW, Z_ENT2)])
    box((2 * XW - 0.2, 0.012, 0.85), (0, WY0 - 0.006, 0.92 + ST_TOP / 4), material=PLASTER_RED)
    box((2 * XW - 0.2, 0.012, 0.70), (0, WY0 - 0.006, 2.0), material=PLASTER_YELLOW)
    skene_doors()
    for u in (-1.9, 0.0, 1.9, -0.93, 0.93, -2.9, 2.9):
        window("-y", WY0, u, 1.82, w=0.26, h=0.36, shutters=None, frame=LIME)
    for x in LOWER_COLS:
        lift(ST_TOP, lambda x=x: column(x, WY0 - 0.12, H_LOWER, r=0.052))
    entablature((-3.3, WY0 - 0.12), (3.3, WY0 - 0.12), Z_ENT1, depth=0.20)
    cornice(-XW, XW, WY0, WY1, Z_ENT2 - 0.03)
    # Dachstuhl: Sparren über der Wand
    for i in range(10):
        x = -3.2 + i * 0.711
        bar((x, WY0 - 0.1, Z_ENT2 + 0.14), (x, WY1 + 0.05, Z_ENT2 - 0.12), 0.05, WOOD)
    bar((-XW, WY0 - 0.1, Z_ENT2 + 0.14), (XW, WY0 - 0.1, Z_ENT2 + 0.14), 0.06, WOOD_DARK)
    bar((-XW, WY1 + 0.05, Z_ENT2 - 0.12), (XW, WY1 + 0.05, Z_ENT2 - 0.12), 0.06, WOOD_DARK)
    lift(ST_TOP, lambda: scaffold((1.6, WY0 - 0.12), (3.4, WY0 - 0.12), (0, -1), 2.0, w=0.24))
    ashlar_stack(-2.95, -3.05, 2, 2, 2, yaw=0, seed=21)
    plank_stack(3.0, -3.0, 0.5, 1.0, 3, yaw=90, seed=22)
    cart(-3.3, -2.0, yaw=90, load="sacks")
    barrel(3.3, -2.3, r=0.08)
    lift(ST_TOP, lambda: sack_stack(-0.6, 1.0, yaw=20))
    lift(ST_TOP, lambda: crate(1.2, 1.0, size=0.2, yaw=15))
    check_bounds(3.68, 3.68, "Stufe 3")
    variant("GrossesTheater_3")


# ================================================================ 4 fertig
def stage4():
    orchestra(True)
    cavea(N_ROWS)
    arcade(True)
    aisles(N_ROWS)
    pts, zt = porticus(True)
    stage_platform(planks=True, finished=True)
    skene_body([(-XW, XW, Z_ENT2)])
    box((2 * XW - 0.2, 0.012, 0.85), (0, WY0 - 0.006, 0.92 + ST_TOP / 4), material=PLASTER_RED)
    box((2 * XW - 0.2, 0.012, 0.70), (0, WY0 - 0.006, 2.0), material=PLASTER_YELLOW)
    skene_doors()
    for u in (-1.9, 0.0, 1.9, -0.93, 0.93, -2.9, 2.9):
        window("-y", WY0, u, 1.82, w=0.26, h=0.36, shutters=None, frame=MARBLE)
    for x in LOWER_COLS:
        lift(ST_TOP, lambda x=x: corinthian_column(x, WY0 - 0.12, 0.0, H_LOWER, r=0.052))
    entablature((-3.3, WY0 - 0.12), (3.3, WY0 - 0.12), Z_ENT1, depth=0.20)
    for x in LOWER_COLS:
        lift(Z_ENT1, lambda x=x: column(x, WY0 - 0.05, 0.80, r=0.042))
    entablature((-3.3, WY0 - 0.05), (3.3, WY0 - 0.05), Z_ENT2, depth=0.14)
    for x in (-2.875, -0.935, 0.935, 2.875):
        lift(ST_TOP, lambda x=x: statue(x, WY0 - 0.14, h=0.55))
        lift(Z_ENT1, lambda x=x: statue(x, WY0 - 0.06, h=0.50))
    lift(Z_ENT1, lambda: statue(0.0, WY0 - 0.06, h=0.60))
    lift(Z_ENT1, lambda: statue(-1.9, WY0 - 0.06, h=0.50))
    lift(Z_ENT1, lambda: statue(1.9, WY0 - 0.06, h=0.50))
    cornice(-XW, XW, WY0, WY1, Z_ENT2 - 0.03)
    for i in range(68):                                                    # Zahnschnitt unter dem Dach
        box((0.05, 0.06, 0.05), (-3.35 + i * 0.1, WY0 - 0.10, Z_ENT2 - 0.065), material=PM)
    shed_roof(-XW + 0.1, XW - 0.1, WY0 - 0.2, WY1, Z_ENT2 + 0.12, "+y", overhang=0.07, pitch=12.0)
    for s in (-1, 1):
        wall_banner("-y", WY0, s * 3.42, 2.30, 0.18, 0.62, CLOTH_RED, trim=CLOTH_YELLOW)
        lift(ST_TOP, lambda s=s: brazier(s * 3.15, 0.25, 0.0, h=0.40))
    # Sonnensegel über den oberen Rängen, an Säulen der Säulenhalle befestigt
    z0 = top(N_ROWS - 1)
    zc = zt
    for k in range(1, 11, 2):
        a_lo, a_hi = 270 + (k - 6) * 14.0, 270 + (k + 2 - 6) * 14.0
        o0, o1 = pol(R_OUT - 0.09, a_lo), pol(R_OUT - 0.09, a_hi)
        i0, i1 = pol(2.55, a_lo), pol(2.55, a_hi)
        sun_sail([(o0[0], o0[1], zc + 0.04), (o1[0], o1[1], zc + 0.04), (i1[0], i1[1], z0 + 0.50), (i0[0], i0[1], z0 + 0.50)],
                 sag=0.05, bands=6, colors=(SAIL_AWNING, SAIL_RED), nu=12, nv=5)
    for (x, y), t in zip(((-3.15, -3.15), (3.15, -3.15), (-3.42, -2.3), (3.42, -2.3)), (1.3, 1.3, 1.1, 1.1)):
        cypress(x, y, h=t, r=0.15)
    for x in (-3.4, 3.4):
        laurel_tree(x, -1.45, size=0.8)
    bush(-3.4, -3.2, 0.12, LAUREL)
    bush(3.4, -3.25, 0.12, LAUREL)
    check_bounds(3.68, 3.68, "Stufe 4")
    variant("GrossesTheater_4")


stage0()
stage1()
stage2()
stage3()
stage4()

export(spacing=9.0)
