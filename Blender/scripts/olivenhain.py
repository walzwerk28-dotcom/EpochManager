"""
Olivenhain (Antike), 2 x 2 Felder (4 x 4 m): ein kleiner Hain aus acht alten, knorrigen Olivenbäumen (gedrehte, gespaltene
Stämme, silbrig graugrünes Laub), in versetzten Reihen. Unter dem rechten Baum der Mitte liegt ein ausgebreitetes Erntenetz
mit heruntergeschlagenen Oliven, an den vorderen Bäumen lehnt eine Leiter; vorn steht ein Esel mit zwei Tragkörben voller
Oliven, daneben Erntekörbe. Hinten und links begrenzt eine niedrige Trockenmauer den Hain, dazwischen Grasbüschel.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/olivenhain.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Olivenhain_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(41)

# ---------------------------------------------------------------- Trockenmauer hinten und links
dry_stone_wall((-1.70, 1.65), (1.70, 1.65), h=0.15, gaps=((0.42, 0.50),), seed=3)
dry_stone_wall((-1.67, -0.75), (-1.67, 1.65), h=0.15, gaps=(), seed=4)

# ---------------------------------------------------------------- Bäume in versetzten Reihen
TREES = [
    (-1.15, 1.22, 1.05, 11, 0.10), (0.05, 1.28, 1.15, 12, -0.08), (1.20, 1.20, 1.00, 13, 0.06),     # hinten
    (-0.62, 0.22, 1.10, 14, -0.05), (0.72, 0.30, 1.20, 15, 0.08),                                    # Mitte
    (-1.20, -0.62, 0.95, 16, 0.10), (1.18, -0.72, 1.00, 17, -0.07), (0.30, -0.55, 0.85, 18, 0.04),  # vorn
]
for tx, ty, ts, sd, ln in TREES:
    gnarled_olive(tx, ty, ts, seed=sd, lean=ln)

# ---------------------------------------------------------------- Erntenetz unter dem rechten Baum der Mitte
harvest_net(0.72, 0.28, 0.95, 0.80, yaw=8, olives=52, seed=2)
olive_basket(1.30, 0.58, 0.0, r=0.085, seed=1)
olive_basket(1.12, 0.62, 0.0, r=0.075, seed=2)

# ---------------------------------------------------------------- Leiter an den linken vorderen Baum
ladder((-1.02, -0.90, 0.0), (-1.20, -0.62, 1.10), (0.96, -0.28, 0.0), width=0.22)
olive_basket(-0.88, -0.98, 0.0, r=0.08, seed=3)
olives_scatter(-0.7, -0.95, 0.3, 0.2, 7, seed=5)

# ---------------------------------------------------------------- Esel mit Tragkörben vorn
donkey(0.45, -1.30, yaw=172)
olive_basket(1.05, -1.42, 0.0, r=0.09, seed=4)
olive_basket(-0.12, -1.55, 0.0, r=0.08, seed=5)

# ---------------------------------------------------------------- Gras
for k, (gx, gy) in enumerate(((-1.5, 0.5), (-0.3, 0.85), (1.45, 0.05), (0.2, -0.05), (-0.9, -1.4), (1.5, -1.6),
                              (-1.55, -1.5), (0.95, 0.95), (-0.25, 1.52), (1.6, 0.65))):
    grass_tuft(gx, gy, 0.10 + 0.02 * (k % 3), seed=k)

obj = variant("Olivenhain_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
