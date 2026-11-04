"""
Heiliger Hain (Antike), 2 x 2 Felder (4 x 4 m): Hain aus Oliven, Lorbeer und Zypressen im Kreis um eine Lichtung.
In der Mitte ein kleiner Altar aus Kalkstein auf zwei Stufen mit Opferschale und Girlande, davor eine Steinbank;
Trockenmauer aus grauen Bruchsteinen mit Lücken (Büsche) und Durchgang zur Straße (-Y) zwischen zwei Steinpfosten, Trittplatten vom Eingang zum Altar, Blumenbeete, Büsche, Blumen
am Fuß der Bäume und eine Herme in der vorderen Ecke.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/heiliger_hain.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 3,68 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (HeiligerHain_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(9)

AX, AY = 0.0, 0.10                       # Altar in der Mitte der Lichtung

# ---------------------------------------------------------------- niedrige Trockenmauer, an mehreren Stellen offen
E = 1.62
GATE = 0.34
dry_stone_wall((-E, -E), (-GATE - 0.12, -E), seed=1)
dry_stone_wall((GATE + 0.12, -E), (E, -E), gaps=[(0.55, 0.78)], seed=2)
dry_stone_wall((-E, E), (E, E), gaps=[(0.66, 0.84)], seed=3)
dry_stone_wall((-E, -E), (-E, E), gaps=[(0.46, 0.62), (0.80, 0.90)], seed=4)
dry_stone_wall((E, -E), (E, E), gaps=[(0.24, 0.40)], seed=5)
stone_post(-GATE - 0.04, -E, 0.36)
stone_post(GATE + 0.04, -E, 0.36)
# Büsche in den Lücken und davor
bush(1.10, 1.64, 0.17, LEAF)
bush(-1.64, 0.30, 0.17, LEAF_DARK)
bush(-1.66, 1.20, 0.14, LAUREL)
bush(1.66, -1.00, 0.16, LEAF)
bush(1.36, -1.64, 0.13, LAUREL)

# ---------------------------------------------------------------- Altar auf zwei Stufen
box((1.00, 1.00, 0.06), (AX, AY, 0.03), material=LIME_DARK)
box((0.78, 0.78, 0.06), (AX, AY, 0.09), material=LIME)
box((0.34, 0.34, 0.42), (AX, AY, 0.33), material=LIME)
box((0.42, 0.42, 0.04), (AX, AY, 0.14 + 0.0), material=LIME_DARK)          # Sockelleiste
box((0.44, 0.44, 0.05), (AX, AY, 0.565), material=LIME_DARK)               # Deckplatte
for sx in (-1, 1):                                                           # Voluten an den Ecken der Platte
    for sy in (-1, 1):
        cyl(0.035, 0.07, (AX + sx * 0.20, AY + sy * 0.20, 0.62), rot=(0, 90, 0), material=LIME, verts=8)
# Opferschale mit Glut
cone(0.07, 0.13, 0.06, (AX, AY, 0.645), TERRACOTTA, verts=12)
cyl(0.115, 0.012, (AX, AY, 0.678), material=COMB, verts=12)
for k in range(3):
    cone(0.03, 0.0, 0.10 + 0.03 * k, (AX + 0.03 * (k - 1), AY + 0.02 * (k % 2), 0.73 + 0.015 * k), BLOOM_YELLOW, verts=5)
# Girlande um den Altarkörper: Blätter und rote Blüten hängen in Bögen zwischen den Ecken
ring = [(-0.19, -0.19), (0.19, -0.19), (0.19, 0.19), (-0.19, 0.19)]
for i in range(4):
    (xa, ya), (xb, yb) = ring[i], ring[(i + 1) % 4]
    for k in range(7):
        t = k / 6
        sag = 0.09 * math.sin(math.pi * t)
        x, y = AX + xa + (xb - xa) * t, AY + ya + (yb - ya) * t
        out = 0.025
        nx, ny = (yb - ya), -(xb - xa)
        ln = math.hypot(nx, ny)
        x += nx / ln * out
        y += ny / ln * out
        sphere(0.028, (x, y, 0.50 - sag), (1, 1, 0.8), LEAF if k % 2 else LEAF_DARK, segs=5, rings=3)
        if k % 3 == 1:
            sphere(0.02, (x + nx / ln * 0.02, y + ny / ln * 0.02, 0.50 - sag), material=BLOOM_RED, segs=4, rings=2)
bench(AX, -0.58, yaw=0, length=0.62)

# ---------------------------------------------------------------- Weg vom Eingang zum Altar
path([(0.0, -1.72), (0.0, -0.72)], w=0.22)

# ---------------------------------------------------------------- Bäume im Kreis um die Lichtung
R = 1.28
trees = [
    (-50, "laurel"), (-130, "cypress"), (-8, "cypress"), (30, "olive"), (68, "laurel"),
    (110, "cypress"), (150, "olive"), (188, "cypress"),
]
for ang, kind in trees:
    tx, ty = AX + R * math.cos(math.radians(ang)), AY + R * math.sin(math.radians(ang)) + 0.08
    if kind == "olive":
        olive_tree(tx, ty, 1.35)
    elif kind == "laurel":
        laurel_tree(tx, ty, 1.15 if ang < 0 else 1.45)
    else:
        cypress(tx, ty, 1.0 if ang < 0 else 1.5, 0.12 if ang < 0 else 0.16)
    for k, c in enumerate((BLOOM_WHITE, BLOOM_YELLOW, BLOOM_PINK, LAVENDER)):    # Blumen am Fuß
        a = math.radians(ang + 90 + k * 55 - 80)
        sphere(0.03, (tx + 0.26 * math.cos(a), ty + 0.26 * math.sin(a), 0.05), (1, 1, 0.8), HERB_LIGHT, segs=5, rings=3)
        sphere(0.02, (tx + 0.26 * math.cos(a), ty + 0.26 * math.sin(a), 0.085), material=c, segs=4, rings=2)

# ---------------------------------------------------------------- Beete, Büsche, Herme
flower_bed(-0.80, -0.30, -1.30, -1.00, bloom=BLOOM_YELLOW, foliage=LEAF, seed=11, step=0.14)
flower_bed(0.30, 0.80, -1.30, -1.00, bloom=LAVENDER, foliage=HERB_GREY, seed=12, step=0.14)
bush(-1.52, 1.48, 0.17, LEAF)
bush(1.52, 1.48, 0.17, LEAF_DARK)
bush(0.0, 1.58, 0.16, LAUREL)

herm(-1.50, -1.32, h=0.46)
pot_plant(-0.30, -1.45, 0.06, leaves=LAUREL)
pot_plant(0.30, -1.45, 0.06, leaves=LAUREL)

obj = variant("HeiligerHain_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}")

export(spacing=6.0)
