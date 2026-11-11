"""
Feld (Antike), 2 x 2 Felder (4 x 4 m), vier Wachstumsstufen als Objekte Feld_0 .. Feld_3 (feste Reihenfolge, CropStages
zeigt die passende):
  Feld_0  Stoppeln: abgeerntete Erde mit niedrigen Stoppelreihen, drei Garbenhocken auf dem Feld
  Feld_1  gesät: frisch gezogene Dämme mit kleinen Keimlingen
  Feld_2  grün: dichtes grünes Getreide in Reihen
  Feld_3  reif: goldgelbe Ähren in Reihen, an der Wegseite ein paar Garben
In allen Stufen gleich: niedriger Flechtzaun hinten und links, Feldweg (Vorgewende vorn und Streifen rechts) zur Straße.
Die Reihen laufen entlang x wie die Furchen der Bodenplatte. Getreide besteht je Reihe aus einem Kamm (Streifenfläche mit
gezackter Oberkante) statt aus Einzelhalmen.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/feld.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 3,68 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält Feld_0 bis Feld_3.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(21)

F_STUBBLE = mat("Stoppel", (0.72, 0.60, 0.34))
F_STUBBLE_TOP = mat("StoppelHell", (0.84, 0.72, 0.42))
F_RIDGE = mat("AckerDamm", (0.36, 0.25, 0.16))
F_RIDGE_TOP = mat("AckerDammHell", (0.50, 0.36, 0.23))
F_SPROUT = mat("Keimling", (0.42, 0.72, 0.20))
F_GREEN = mat("KornGruen", (0.26, 0.52, 0.14))
F_GREEN_TOP = mat("KornGruenHell", (0.46, 0.74, 0.20))
F_GREEN_B = mat("KornGruenB", (0.31, 0.57, 0.16))
F_RIPE = mat("KornReif", (0.80, 0.58, 0.16))
F_RIPE_TOP = mat("KornReifHell", (0.95, 0.78, 0.30))
F_RIPE_B = mat("KornReifB", (0.86, 0.64, 0.20))
F_PATH = mat("Feldweg", (0.70, 0.58, 0.40))
F_PATH_STONE = mat("Trittstein", (0.60, 0.57, 0.50))

ROWS = 12
ROW_Y0, ROW_STEP, ROW_W = -1.45, 0.265, 0.19
ROW_X0, ROW_X1 = -1.72, 1.40


def crop_row(y, h, body_m, top_m, seg, seed, zig=0.0, lean=0.0, wave=0.18, w=ROW_W, top=0.20, x0=ROW_X0, x1=ROW_X1):
    """Eine Reihe als Kamm entlang x: zwei Flanken in body_m, schmale Oberseite in top_m. zig lässt die Höhe von
    Querschnitt zu Querschnitt wechseln (Ähren), lean versetzt die Krone abwechselnd nach beiden Seiten."""
    rnd = random.Random(seed)
    n = max(2, round((x1 - x0) / seg))
    verts, faces, mi = [], [], []
    wt = w * top
    for i in range(n + 1):
        x = x0 + (x1 - x0) * i / n
        hh = h * (1 + rnd.uniform(-wave, wave))
        if zig and i % 2:
            hh *= 1 - zig
        off = lean * (1 if i % 2 else -1) * (0.6 + 0.4 * rnd.random())
        verts += [(x, y - w / 2, 0.0), (x, y - wt + off, hh), (x, y + wt + off, hh), (x, y + w / 2, 0.0)]
    for i in range(n):
        a, b = 4 * i, 4 * (i + 1)
        faces += [(a, b, b + 1, a + 1), (a + 1, b + 1, b + 2, a + 2), (a + 2, b + 2, b + 3, a + 3)]
        mi += [0, 1, 0]
    faces += [(0, 1, 2, 3), (4 * n + 3, 4 * n + 2, 4 * n + 1, 4 * n)]
    mi += [0, 0]
    return raw_mesh(verts, faces, mi, [body_m, top_m], "Reihe")


def rows(h, mats, seg, zig=0.0, lean=0.0, wave=0.18, top=0.20):
    for r in range(ROWS):
        body_m, top_m = mats[r % len(mats)]
        crop_row(ROW_Y0 + r * ROW_STEP, h, body_m, top_m, seg, 100 + r, zig=zig, lean=lean, wave=wave, top=top)


def common():
    """Flechtzaun hinten und links, Feldweg vorn und rechts, ein Krug am Weg."""
    wattle_fence([(1.76, 1.76), (-1.76, 1.76), (-1.76, -1.76)], h=0.22, spacing=0.45)
    box((3.52, 0.26, 0.03), (0.0, -1.66, 0.015), material=F_PATH)                   # Vorgewende zur Straße
    box((0.26, 3.20, 0.03), (1.63, 0.0, 0.015), material=F_PATH)                    # Weg am rechten Rand
    for k, (x, y) in enumerate(((1.63, -0.55), (1.60, 0.35), (1.66, 1.05), (0.9, -1.66))):
        box((0.15, 0.11, 0.045), (x, y, 0.0225), rot=(0, 0, 14 * (k - 1.5)), material=F_PATH_STONE)
    amphora(1.66, -1.30)


# ---------------------------------------------------------------- 0 Stoppeln
common()
rows(0.040, [(F_STUBBLE, F_STUBBLE_TOP)], seg=0.2, zig=0.5, lean=0.02, wave=0.35, top=0.30)
for k, (x, y) in enumerate(((0.35, -0.70), (-0.95, 0.35), (0.65, 0.95))):
    stook(x, y, n=4, yaw=30 * k)
for k, (x, y) in enumerate(((-0.2, -1.1), (-1.4, -0.4), (0.2, 0.2), (-0.5, 1.3), (1.05, -0.2))):        # liegengebliebene Halme
    box((0.20, 0.018, 0.012), (x, y, 0.05), rot=(0, 0, 17 * k + 5), material=STRAW)
check_bounds(1.84, 1.84, "Feld_0")
variant("Feld_0")

# ---------------------------------------------------------------- 1 gesät
common()
rows(0.050, [(F_RIDGE, F_RIDGE_TOP)], seg=0.2, wave=0.10, top=0.45)
for r in range(ROWS):
    y = ROW_Y0 + r * ROW_STEP
    for i in range(16):
        x = ROW_X0 + 0.10 + i * 0.2 + random.uniform(-0.02, 0.02)
        cone(0.042, 0.004, 0.10 + random.uniform(0, 0.03), (x, y + random.uniform(-0.01, 0.01), 0.05 + 0.04),
             F_SPROUT, verts=3)
check_bounds(1.84, 1.84, "Feld_1")
variant("Feld_1")

# ---------------------------------------------------------------- 2 grün
common()
rows(0.170, [(F_GREEN, F_GREEN_TOP), (F_GREEN_B, F_GREEN_TOP)], seg=0.1, zig=0.22, lean=0.022, wave=0.12, top=0.42)
check_bounds(1.84, 1.84, "Feld_2")
variant("Feld_2")

# ---------------------------------------------------------------- 3 reif
common()
rows(0.240, [(F_RIPE, F_RIPE_TOP), (F_RIPE_B, F_RIPE_TOP)], seg=0.1, zig=0.22, lean=0.026, wave=0.12, top=0.42)
stook(1.63, 1.38, n=4, yaw=10)
sheaf(1.60, 0.80, lean=10, yaw=90)
sheaf(1.69, 0.66, lean=12, yaw=70)
sheaf(1.58, -1.00, lean=8, yaw=100)
check_bounds(1.84, 1.84, "Feld_3")
variant("Feld_3")

export(spacing=5.0)
