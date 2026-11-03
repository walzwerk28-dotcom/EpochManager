"""
Bausatz für Baustellen (Bauen in Etappen): einzelne Teile, aus denen ConstructionVisuals in Unity für jede Grundfläche
und Gebäudehöhe ein Gerüst samt Baustoffen zusammensetzt. Jedes Teil ist ein eigenes Objekt in der FBX:
  Pfosten      Gerüststange, 1 m hoch, Fuß im Ursprung (Unity streckt sie in der Höhe)
  Riegel       waagerechte Stange, 1 m lang entlang x, Mitte im Ursprung (Unity streckt sie in der Länge)
  Brett        Gerüstbohle, 1 m lang entlang x, 0,16 m breit, Mitte im Ursprung
  Leiter       1 m hoch, Fuß im Ursprung
  Pflock       Absteckpflock mit Schnurrest, Fuß im Ursprung
  Schnur       Absteckschnur, 1 m lang entlang x
  Bretterstapel, Steinhaufen, Sandhaufen, Moertelkasten   Baustoffe in fester Größe (Maßstab der Häuser)

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/baustelle.py -- <blend> <fbx> [<vorschau.png>]
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(77)
SAND = mat("Sand", (0.84, 0.74, 0.55))
MORTAR = mat("Moertel", (0.80, 0.78, 0.72))

# Gerüst
box((0.04, 0.04, 1.0), (0.0, 0.0, 0.5), material=WOOD_DARK)
for z in (0.05, 0.95):                                                    # Bindungen aus Seil
    box((0.05, 0.05, 0.02), (0.0, 0.0, z), material=SACK)
variant("Pfosten")

box((1.0, 0.028, 0.028), (0.0, 0.0, 0.0), material=WOOD)
variant("Riegel")

box((1.0, 0.16, 0.02), (0.0, 0.0, 0.0), material=WOOD)
for y in (-0.04, 0.04):
    box((1.0, 0.004, 0.022), (0.0, y, 0.0), material=WOOD_DARK)
variant("Brett")

for x in (-0.07, 0.07):
    box((0.022, 0.022, 1.0), (x, 0.0, 0.5), material=WOOD_DARK)
for k in range(8):
    box((0.14, 0.016, 0.016), (0.0, 0.0, 0.08 + k * 0.12), material=WOOD)
variant("Leiter")

# Absteckung
box((0.025, 0.025, 0.22), (0.0, 0.0, 0.11), material=WOOD)
cone(0.018, 0.0, 0.05, (0.0, 0.0, 0.245), WOOD, verts=4)
box((0.03, 0.03, 0.015), (0.0, 0.0, 0.17), material=SACK)
variant("Pflock")

box((1.0, 0.006, 0.006), (0.0, 0.0, 0.0), material=SACK)
variant("Schnur")

# Baustoffe
for level in range(4):
    n = 4 - (level % 2)
    for k in range(n):
        box((0.42, 0.075, 0.022), (random.uniform(-0.01, 0.01), (k - (n - 1) / 2) * 0.08, 0.04 + level * 0.024),
            rot=(0, 0, random.uniform(-3, 3)), material=WOOD if (k + level) % 3 else WOOD_DARK)
for x in (-0.15, 0.15):
    box((0.04, 0.34, 0.03), (x, 0.0, 0.015), material=WOOD_DARK)          # Unterleger
variant("Bretterstapel")

for level, count in enumerate((3, 2, 1)):
    for k in range(count):
        box((0.13, 0.09, 0.08), ((k - (count - 1) / 2) * 0.14, random.uniform(-0.01, 0.01), 0.04 + level * 0.08),
            rot=(0, 0, random.uniform(-6, 6)), material=LIME if (k + level) % 2 else LIME_DARK)
for k in range(3):                                                        # zweite Reihe dahinter
    box((0.13, 0.09, 0.08), ((k - 1) * 0.14, 0.10, 0.04), rot=(0, 0, random.uniform(-6, 6)), material=LIME_DARK)
variant("Steinhaufen")

cone(0.22, 0.03, 0.16, (0.0, 0.0, 0.08), SAND, verts=12)
sphere(0.06, (0.05, -0.03, 0.15), (1.0, 1.0, 0.5), SAND, segs=8, rings=4)
box((0.02, 0.14, 0.012), (0.14, 0.08, 0.05), rot=(0, -30, 20), material=WOOD)  # Schaufelstiel
box((0.06, 0.05, 0.01), (0.20, 0.12, 0.015), rot=(0, -30, 20), material=IRON)
variant("Sandhaufen")

box((0.30, 0.20, 0.08), (0.0, 0.0, 0.04), material=WOOD_DARK)
box((0.27, 0.17, 0.01), (0.0, 0.0, 0.078), material=MORTAR)
box((0.03, 0.12, 0.01), (0.10, 0.0, 0.09), rot=(0, 0, 25), material=IRON)    # Kelle
variant("Moertelkasten")

export(spacing=0.9)
