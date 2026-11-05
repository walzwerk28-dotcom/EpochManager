"""
Römische Holzbrücke, aus Teilen für ein Rasterfeld (2 m) zusammensetzbar. Unity (RoadVisuals) setzt je Feld über Wasser
ein Mittelstück und je Nachbarfeld mit Straße einen Arm ein; dort, wo keine Straße anschließt, steht ein Geländerstück.
Alle Teile haben den Ursprung in der Feldmitte, die Erde liegt bei z = 0 (das Wasser knapp darüber), Fahrbahnbreite 1,3 m
wie RoadDefinition.visualWidth. Der Belag liegt 0,20 m hoch, die Straße daneben 0,05 m.
  Mitte      quadratischer Belag (1,3 x 1,3 m) ohne Geländer, Planken quer zur y-Achse, darunter ein Pfahljoch
             (zwei Pfähle, Querbalken, Streben), die Pfähle gehen ins Wasser und in die Erde
  Rand       Geländer entlang der Seite +y des Mittelstücks (Pfosten, Handlauf, Zwischenholm)
  ArmFlach   Belag von der Kante des Mittelstücks (y = 0,65) bis zum Feldrand (y = 1) nach +y, auf gleicher Höhe,
             mit Geländer an beiden Seiten; schließt an das nächste Brückenfeld an
  ArmRampe   wie ArmFlach, der Belag fällt aber bis zum Feldrand auf 0,05 m ab und stößt dort bündig an die Straße
Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/bruecke.py -- <blend> <fbx> [<vorschau.png>]
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

HALF = 0.65        # halbe Fahrbahnbreite
DECK = 0.20        # Oberkante des Belags
GROUND_ROAD = 0.05  # Oberkante der Straße am Ufer
PLANK = 0.03       # Stärke einer Planke
POST = 0.30        # Höhe der Pfosten über dem Belag
WOOD_LIGHT = mat("HolzHell", (0.70, 0.52, 0.33))
WOOD_WET = mat("HolzNass", (0.30, 0.22, 0.15))


def deck_z(y):
    """Oberkante des Belags der Rampe an der Stelle y (0,65 bis 1)."""
    return DECK - (y - HALF) * (DECK - GROUND_ROAD) / (1.0 - HALF)


def planks(y0, y1, count):
    step = (y1 - y0) / count
    for i in range(count):
        box((2 * HALF, step - 0.012, PLANK), (0.0, y0 + (i + 0.5) * step, DECK - PLANK / 2),
            material=WOOD if i % 2 == 0 else WOOD_LIGHT)


def rail(points):
    """Geländer entlang der Punkte (x, y, Belaghöhe): Pfosten an jedem Punkt, Handlauf und Zwischenholm dazwischen."""
    for x, y, z in points:
        box((0.045, 0.045, POST + 0.03), (x, y, z - 0.03 + (POST + 0.03) / 2), material=WOOD_DARK)
        box((0.06, 0.06, 0.02), (x, y, z + POST), material=WOOD)
    for a, b in zip(points, points[1:]):
        bar((a[0], a[1], a[2] + POST - 0.015), (b[0], b[1], b[2] + POST - 0.015), 0.04, WOOD)
        bar((a[0], a[1], a[2] + 0.13), (b[0], b[1], b[2] + 0.13), 0.025, WOOD_DARK)


# Mittelstück: Belag, Längsholme, Pfahljoch
planks(-HALF, HALF, 6)
for x in (-0.45, 0.45):
    box((0.07, 2 * HALF, 0.07), (x, 0.0, DECK - PLANK - 0.035), material=WOOD_DARK)
box((1.24, 0.10, 0.06), (0.0, 0.0, 0.075), material=WOOD_DARK)               # Querbalken des Jochs
for x in (-0.5, 0.5):
    cyl(0.045, 0.34, (x, 0.0, -0.06), material=WOOD_WET, verts=8)             # Pfähle, gehen in Wasser und Erde
    box((0.10, 0.11, 0.025), (x, 0.0, 0.10), material=IRON)                   # Eisenband am Pfahlkopf
bar((-0.5, 0.0, 0.0), (0.5, 0.0, 0.08), 0.03, WOOD_DARK)                      # Streben
bar((0.5, 0.0, 0.0), (-0.5, 0.0, 0.08), 0.03, WOOD_DARK)
variant("Mitte")

# Geländer entlang der Seite +y
rail([(-HALF, HALF, DECK), (0.0, HALF, DECK), (HALF, HALF, DECK)])
variant("Rand")

# Arm, eben
planks(HALF, 1.0, 2)
for x in (-0.45, 0.45):
    box((0.07, 1.0 - HALF, 0.07), (x, (HALF + 1.0) / 2, DECK - PLANK - 0.035), material=WOOD_DARK)
for x in (-HALF, HALF):
    rail([(x, HALF, DECK), (x, 1.0, DECK)])
variant("ArmFlach")

# Arm, Rampe zur Straße am Ufer
run = 1.0 - HALF
angle = math.degrees(math.atan((DECK - GROUND_ROAD) / run))
count = 3
step = run / count
for i in range(count):
    yc = HALF + (i + 0.5) * step
    box((2 * HALF, step / math.cos(math.radians(angle)) - 0.012, PLANK),
        (0.0, yc, deck_z(yc) - PLANK / 2 * math.cos(math.radians(angle))),
        rot=(-angle, 0.0, 0.0), material=WOOD if i % 2 == 0 else WOOD_LIGHT)
for x0 in (HALF - 0.03, -HALF):                                               # Wangen füllen den Keil unter dem Belag
    slab([(HALF, -0.04), (1.0, -0.04), (1.0, GROUND_ROAD - PLANK), (HALF, DECK - PLANK)], "yz", x0, 0.03, WOOD_DARK)
for x in (-HALF, HALF):
    rail([(x, HALF, DECK), (x, 1.0, GROUND_ROAD)])
variant("ArmRampe")

export(spacing=2.0)
