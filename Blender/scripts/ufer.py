"""
Uferdeko für Seen, Flüsse und Küsten (Maßstab der Häuser: ein Stockwerk etwa 1 m, Schilf 0,35 bis 0,6 m). WaterVisuals
verteilt sie in Unity zufällig, aber fest an der Feldposition. Jedes Objekt hat den Ursprung am Boden.
  Schilf_A     hohes Schilf mit Rohrkolben
  Schilf_B     niedriges Gras (Seggen), breiter Büschel
  Schilf_C     gemischt, locker, mit einem Rohrkolben
  Ufersteine_A drei Steine
  Ufersteine_B ein flacher großer Stein mit Kieseln
  Seerose_A    drei Blätter, eine rosa Blüte, schwimmt auf dem Wasser (Unterseite bei z = 0)
  Seerose_B    zwei Blätter, eine weiße Blüte, eine Knospe
Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/ufer.py -- <blend> <fbx> [<vorschau.png>]
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(2026)
REED = mat("Schilf", (0.58, 0.64, 0.33))
REED_DRY = mat("SchilfTrocken", (0.76, 0.68, 0.42))
CATTAIL = mat("Rohrkolben", (0.36, 0.23, 0.14))
GRASS_A = mat("Segge", (0.40, 0.58, 0.28))


def blade(x, y, height, lean, yaw, material, width=0.014):
    """Halm als schlanke Pyramide, an der Spitze geneigt (lean in Grad) in Richtung yaw (Grad)."""
    tx = math.sin(math.radians(lean)) * height * 0.5
    cone(width, 0.0, height, (x + math.cos(math.radians(yaw)) * tx, y + math.sin(math.radians(yaw)) * tx, height / 2),
         material, verts=4,
         rot=(lean * math.sin(math.radians(yaw)), -lean * math.cos(math.radians(yaw)), 0.0))


def cattail(x, y, height, yaw):
    lean = random.uniform(2, 7)
    blade(x, y, height, lean, yaw, REED, width=0.010)
    tx = math.sin(math.radians(lean)) * height
    cx, cy = x + math.cos(math.radians(yaw)) * tx, y + math.sin(math.radians(yaw)) * tx
    cyl(0.017, 0.09, (cx, cy, height - 0.02), material=CATTAIL, verts=6)
    cone(0.017, 0.0, 0.04, (cx, cy, height + 0.045), CATTAIL, verts=6)


def cluster(count, h0, h1, spread, lean_max, materials, width=0.014):
    for i in range(count):
        a = random.uniform(0, math.tau)
        r = random.uniform(0, 1) ** 0.8 * spread
        blade(math.cos(a) * r, math.sin(a) * r, random.uniform(h0, h1), random.uniform(3, lean_max),
              math.degrees(a) + random.uniform(-40, 40), materials[i % len(materials)], width)


# Schilf
cluster(9, 0.38, 0.60, 0.07, 14, (REED, REED, REED_DRY))
for a in (20, 140, 250):
    cattail(math.cos(math.radians(a)) * 0.04, math.sin(math.radians(a)) * 0.04, random.uniform(0.50, 0.62), a)
variant("Schilf_A")

cluster(16, 0.22, 0.42, 0.10, 32, (GRASS_A, LEAF, GRASS_A, LEAF_DARK), width=0.016)
variant("Schilf_B")

cluster(8, 0.34, 0.52, 0.13, 20, (REED, REED_DRY, REED), width=0.013)
cattail(0.05, -0.03, 0.46, 300)
cluster(5, 0.18, 0.30, 0.15, 40, (GRASS_A, LEAF), width=0.015)
variant("Schilf_C")


# Steine
def stone(x, y, sx, sy, sz, material, yaw=0.0):
    sphere(1.0, (x, y, sz * 0.62), (sx, sy, sz), material, segs=7, rings=4,
           rot=(random.uniform(-8, 8), random.uniform(-8, 8), yaw))


stone(0.0, 0.0, 0.16, 0.12, 0.09, ROCK_A, 20)
stone(0.17, 0.07, 0.10, 0.09, 0.07, ROCK_B, -35)
stone(-0.10, -0.12, 0.09, 0.07, 0.05, ROCK_C, 60)
variant("Ufersteine_A")

stone(0.0, 0.0, 0.26, 0.19, 0.06, ROCK_B, 15)
stone(0.24, -0.10, 0.07, 0.06, 0.04, ROCK_A, 10)
stone(-0.20, 0.14, 0.06, 0.05, 0.035, ROCK_C, 80)
stone(-0.05, -0.22, 0.05, 0.045, 0.03, ROCK_A, 0)
variant("Ufersteine_B")


# Seerosen
def pad(x, y, r, material):
    cyl(r, 0.012, (x, y, 0.006), material=material, verts=14)


def lily(x, y, petal, size=1.0):
    for k in range(7):
        a = k * math.tau / 7
        cone(0.022 * size, 0.0, 0.07 * size,
             (x + math.cos(a) * 0.028 * size, y + math.sin(a) * 0.028 * size, 0.045 * size),
             petal, verts=4, rot=(50 * math.sin(a), -50 * math.cos(a), 0))
    sphere(0.02 * size, (x, y, 0.025 * size), (1, 1, 0.8), BLOOM_YELLOW, segs=6, rings=3)


pad(0.0, 0.0, 0.15, LEAF_DARK)
pad(0.24, 0.10, 0.12, LEAF)
pad(-0.15, 0.20, 0.10, LEAF_DARK)
lily(0.0, 0.0, BLOOM_PINK)
variant("Seerose_A")

pad(0.0, 0.0, 0.14, LEAF)
pad(-0.22, -0.13, 0.11, LEAF_DARK)
lily(0.0, 0.01, BLOOM_WHITE, 0.9)
cone(0.018, 0.0, 0.07, (-0.22, -0.13, 0.05), BLOOM_WHITE, verts=6)                      # Knospe
variant("Seerose_B")

export(spacing=0.7)
