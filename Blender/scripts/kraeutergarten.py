"""
Kräutergarten (Antike), 1 x 1 Feld (2 x 2 m): kleiner römischer Nutz- und Ziergarten.
Niedrige Steinkante rundum mit Eingang zur Straße (-Y), Kiesweg in Kreuzform, vier Kräuterbeete in verschiedenen
Grüntönen (Lavendel, Gelb, Weiß), in der Wegmitte eine kleine Sonnenuhr, hinten eine Laube mit Steinbank und
Rankpflanzen, Amphoren und Topfpflanzen in den Ecken.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/kraeutergarten.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 1,84 x 1,84 m (Grundfläche 2 x 2 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Kraeutergarten_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(5)

# ---------------------------------------------------------------- Einfassung mit Eingang vorn
E = 0.86
GATE = 0.17
for (xa, xb) in ((-E, -GATE), (GATE, E)):
    cx = (xa + xb) / 2
    box((xb - xa, 0.08, 0.12), (cx, -E, 0.06), material=LIME_DARK)
    box((xb - xa + 0.01, 0.10, 0.025), (cx, -E, 0.1325), material=LIME)
box((2 * E, 0.08, 0.12), (0, E, 0.06), material=LIME_DARK)
box((2 * E + 0.01, 0.10, 0.025), (0, E, 0.1325), material=LIME)
for s in (-1, 1):
    box((0.08, 2 * E - 0.08, 0.12), (s * E, 0, 0.06), material=LIME_DARK)
    box((0.10, 2 * E - 0.08, 0.025), (s * E, 0, 0.1325), material=LIME)
for s in (-1, 1):                                       # Torpfosten mit Topfpflanze obenauf
    box((0.09, 0.09, 0.26), (s * (GATE + 0.02), -E, 0.13), material=LIME)
    box((0.11, 0.11, 0.03), (s * (GATE + 0.02), -E, 0.275), material=LIME_DARK)
    pot_plant(s * (GATE + 0.02), -E, 0.032, leaves=LEAF)

# ---------------------------------------------------------------- Wege: Kreuz aus Trittplatten
CX, CY = 0.0, -0.21
path([(0.0, -0.84), (0.0, 0.50)], w=0.15)
path([(-0.80, CY), (0.80, CY)], w=0.15)

# ---------------------------------------------------------------- vier Kräuterbeete
flower_bed(-0.80, -0.11, -0.80, -0.31, bloom=LAVENDER, foliage=HERB_GREY, seed=1, step=0.19)
flower_bed(0.11, 0.80, -0.80, -0.31, bloom=BLOOM_YELLOW, foliage=LEAF, seed=2, step=0.19)
flower_bed(-0.80, -0.11, -0.11, 0.38, bloom=BLOOM_WHITE, foliage=HERB_LIGHT, seed=3, step=0.19)
flower_bed(0.11, 0.80, -0.11, 0.38, bloom=LAVENDER, foliage=LEAF_DARK, seed=4, step=0.19)

# ---------------------------------------------------------------- Sonnenuhr in der Wegmitte
cyl(0.05, 0.05, (CX, CY, 0.025), material=LIME_DARK, verts=10)
cyl(0.028, 0.20, (CX, CY, 0.15), material=LIME, verts=10)
cyl(0.075, 0.025, (CX, CY, 0.2625), material=LIME, verts=14)
box((0.012, 0.075, 0.05), (CX, CY + 0.012, 0.30), rot=(-35, 0, 0), material=IRON)

# ---------------------------------------------------------------- Laube mit Steinbank hinten
pergola(-0.44, 0.44, 0.50, 0.76, h=0.78, vine_post=0)
bench(0.0, 0.66, yaw=0, length=0.50)
for x in (-0.44, 0.44):                                 # Kletterpflanze an den vorderen Pfosten
    for k in range(3):
        sphere(0.04, (x, 0.50, 0.15 + k * 0.22), (0.9, 0.9, 1.0), LEAF_DARK if k % 2 else LEAF, segs=6, rings=4)
for k, x in enumerate((-0.30, 0.30)):                   # Blüten in der Laube
    sphere(0.025, (x, 0.50, 0.58 + 0.06 * k), material=BLOOM_PINK, segs=5, rings=3)

# ---------------------------------------------------------------- Amphore, Töpfe
amphora(0.72, 0.68)
amphora(0.62, 0.72)
pot_plant(-0.72, 0.68, 0.065, leaves=LAUREL)
pot_plant(-0.60, 0.72, 0.045, leaves=HERB_LIGHT)
pot_plant(0.74, -0.74, 0.05, leaves=LEAF)

obj = variant("Kraeutergarten_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}")

export(spacing=3.4)
