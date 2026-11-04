"""
Kräutergarten (Antike), 1 x 1 Feld (2 x 2 m): kleiner römischer Nutz- und Ziergarten.
Niedriger Flechtzaun rundum mit kleinem Holztor zur Straße (-Y), Kiesweg in Kreuzform, vier Kräuterbeete mit Holzkante in verschiedenen
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

# ---------------------------------------------------------------- Flechtzaun mit kleinem Holztor vorn
E = 0.86
GATE = 0.17
wattle_fence([(-E, -E), (-GATE - 0.02, -E)])
wattle_fence([(GATE + 0.02, -E), (E, -E), (E, E), (-E, E), (-E, -E)])
for sx in (-1, 1):                                      # Torpfosten: etwas höher als der Zaun, mit Kappe
    px = sx * (GATE + 0.02)
    box((0.05, 0.05, 0.32), (px, -E, 0.16), material=WOOD_DARK)
    box((0.065, 0.065, 0.025), (px, -E, 0.332), material=WOOD)
# Lattentür, links angeschlagen und 70 Grad nach innen aufgeschwenkt
hx, hy = -(GATE + 0.02) + 0.02, -E
yaw = 70
dx, dy = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
LEAF_W = 0.28
for z in (0.07, 0.24):
    box((LEAF_W, 0.02, 0.025), (hx + dx * LEAF_W / 2, hy + dy * LEAF_W / 2, z), rot=(0, 0, yaw), material=WOOD_DARK)
for k in range(6):
    d = 0.02 + k * (LEAF_W - 0.03) / 5
    box((0.028, 0.014, 0.23), (hx + dx * d, hy + dy * d, 0.155), rot=(0, 0, yaw), material=WOOD)
bar((hx + dx * 0.02, hy + dy * 0.02, 0.07), (hx + dx * (LEAF_W - 0.01), hy + dy * (LEAF_W - 0.01), 0.24), 0.018, WOOD_DARK)

# ---------------------------------------------------------------- Wege: Kreuz aus Trittplatten
CX, CY = 0.0, -0.21
path([(0.0, -0.84), (0.0, 0.50)], w=0.15)
path([(-0.80, CY), (0.80, CY)], w=0.15)

# ---------------------------------------------------------------- vier Kräuterbeete
flower_bed(-0.80, -0.11, -0.80, -0.31, bloom=LAVENDER, foliage=HERB_GREY, seed=1, step=0.21, edge=WOOD_DARK)
flower_bed(0.11, 0.80, -0.80, -0.31, bloom=BLOOM_YELLOW, foliage=LEAF, seed=2, step=0.21, edge=WOOD_DARK)
flower_bed(-0.80, -0.11, -0.11, 0.38, bloom=BLOOM_WHITE, foliage=HERB_LIGHT, seed=3, step=0.21, edge=WOOD_DARK)
flower_bed(0.11, 0.80, -0.11, 0.38, bloom=LAVENDER, foliage=LEAF_DARK, seed=4, step=0.21, edge=WOOD_DARK)

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
