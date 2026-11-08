"""
Steinbruch (Antike), 2 x 2 Felder (4 x 4 m): abgestufte Abbauwand aus hellgrauem Kalkstein im hinteren Teil (drei Stufen,
Verband aus Werksteinen mit dunklen Fugen, an den Oberkanten Reihen von Keillöchern mit eisernen Spaltkeilen und Schrotrinnen,
frisch abgespaltene Blöcke auf den Absätzen). Davor ein Dreibeinkran mit Flaschenzug, an dem ein behauener Quader hängt, und
eine Handwinde; links ein Sonnendach über dem Arbeitsblock des Steinmetzen (Schlägel, Meißel, Keile); rechts ein Ochsenkarren
mit Quadern und zwei Stapel behauener Quader; Schutthaufen am Fuß der Wand.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/steinbruch.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Steinbruch_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(94)

# ---------------------------------------------------------------- Abbauwand
quarry_block(-1.70, 0.95, 0.42, 1.70, 0.0, 0.42, faces=("-y", "+x", "-x", "+y"), seed=1)
quarry_block(-1.70, 0.50, 0.88, 1.70, 0.38, 0.92, faces=("-y", "+x", "-x", "+y"), seed=2)
quarry_block(-1.70, -0.28, 1.28, 1.70, 0.88, 1.45, faces=("-y", "+x", "-x", "+y"), seed=3)

# Absatz 1 (z = 0,42): Keilreihe, Schrotrinnen und ein frisch gespaltener Block
cut_groove((-1.62, 0.64), (0.80, 0.64), 0.42)
cut_groove((0.10, 0.46), (0.10, 0.84), 0.42)
wedge_row((-1.55, 0.64), (0.72, 0.64), 0.42, 11, seed=1)
chunky(0.20, 0.70, 0.47, 0.82, 0.42, 0.62, QUARRY_LIGHT, jitter=0.02, seed=11, top_jitter=0.01, yaw=8)
chunky(-0.42, -0.02, 0.70, 0.84, 0.42, 0.54, QUARRY_BROWN, jitter=0.02, seed=12, yaw=-5)
# Absatz 2 (z = 0,92)
cut_groove((-1.62, 1.08), (0.36, 1.08), 0.92)
cut_groove((-0.55, 0.90), (-0.55, 1.26), 0.92)
wedge_row((-1.55, 1.08), (0.30, 1.08), 0.92, 8, seed=2)
chunky(-0.50, -0.05, 0.92, 1.20, 0.92, 1.07, QUARRY_LIGHT, jitter=0.02, seed=13, yaw=6)
# Absatz 3 (z = 1,45)
cut_groove((-1.62, 1.52), (-0.40, 1.52), 1.45)
cut_groove((-0.95, 1.30), (-0.95, 1.68), 1.45)
wedge_row((-1.55, 1.52), (-0.45, 1.52), 1.45, 5, seed=3)
rubble(-1.2, 1.45, 0.22, 10, seed=4, peak=0.05, size=0.7)
for k, (x, y, z) in enumerate(((-0.40, 0.90, 0.92), (0.30, 0.55, 0.42))):
    rubble(x, y, 0.20, 7, seed=20 + k, peak=0.04, size=0.6)
# Erde und Gestrüpp oben auf den Stufen (Macchia wie im Gebirge)
for x, y, w, d, z in ((-1.40, 1.42, 0.55, 0.30, 1.45), (-0.85, 1.55, 0.50, 0.22, 1.45), (-1.45, 1.05, 0.50, 0.2, 0.92),
                      (-1.55, 0.60, 0.40, 0.2, 0.42)):
    box((w, d, 0.05), (x, y, z), material=SOIL)
for x, y, z, r, m in ((-1.55, 1.45, 1.50, 0.11, OLIVE_LEAF), (-1.30, 1.38, 1.49, 0.08, LEAF_DARK), (-0.82, 1.56, 1.50, 0.10, LEAF_DARK),
                      (-1.52, 1.06, 0.97, 0.09, OLIVE_LEAF), (-1.58, 0.60, 0.46, 0.08, LEAF_DARK)):
    sphere(r, (x, y, z), (1.0, 1.0, 0.75), m, segs=6, rings=4)
# Brecheisen an der Wand
rod((0.88, 0.40, 0.0), (0.92, 0.44, 0.50), 0.012, IRON, verts=5)

# ---------------------------------------------------------------- Schutt
rubble(-1.15, 0.28, 0.48, 40, seed=1, peak=0.20)
rubble(0.72, 0.22, 0.32, 18, seed=2, peak=0.14)
rubble(0.20, -0.30, 0.3, 10, seed=3, peak=0.02, size=0.5)

# ---------------------------------------------------------------- Dreibeinkran mit hängendem Quader
tripod_crane(-0.10, -0.55, height=1.35, spread=0.50, yaw=0, block_z=0.30, seed=1)
# Rollhölzer neben dem Kran und ein roher Block
log_piece((-0.55, -0.15, 0.04), (0.35, -0.15, 0.04), 0.04, sides=6, bark=BARK_LIGHT)
chunky(0.50, 0.82, -0.28, -0.02, 0.0, 0.22, QUARRY_MID, jitter=0.03, seed=14, yaw=-12)

# ---------------------------------------------------------------- Sonnendach über dem Steinmetzen
PX0, PX1, PY0, PY1 = -1.70, -0.90, -0.98, -0.26
for x in (PX0, PX1):
    box((0.06, 0.06, 0.90), (x, PY0, 0.45), material=WOOD_DARK)
    box((0.06, 0.06, 1.07), (x, PY1, 0.535), material=WOOD_DARK)
    bar((x, PY0, 0.60), (x + (0.20 if x < -1.3 else -0.20) * 0.0, PY0 + 0.20, 0.88), 0.035, WOOD)
for y, z in ((PY0, 0.90), (PY1, 1.07)):
    box((PX1 - PX0 + 0.10, 0.045, 0.045), ((PX0 + PX1) / 2, y, z - 0.02), material=WOOD)
sun_sail([(PX0 - 0.03, PY0 - 0.03, 0.92), (PX1 + 0.03, PY0 - 0.03, 0.92), (PX1 + 0.03, PY1 + 0.03, 1.09), (PX0 - 0.03, PY1 + 0.03, 1.09)],
         sag=0.03, bands=6, colors=(CLOTH_OCHRE, SAIL_AWNING), nu=8, nv=6)
stonecutter_block(-1.32, -0.62, yaw=10)
rubble(-1.30, -0.62, 0.38, 14, seed=5, peak=0.0, size=0.35)
bucket(-0.98, -0.40, r=0.05)
# fertig behauener kleiner Block neben dem Arbeitsblock
box((0.20, 0.14, 0.10), (-1.62, -0.42, 0.05), rot=(0, 0, 25), material=ASHLAR)

# ---------------------------------------------------------------- rechts: Ochsenkarren und Quaderstapel
ox_cart(1.20, -0.78, yaw=0, load=None)
cat = placed((1.20, -0.78), 0)
Z = 0.20 + 0.025
for v, w, h, a in ((-0.14, 0.27, 0.13, 3), (0.13, 0.27, 0.13, -4)):
    p = cat((0.0, v, 0.0))
    box((w, 0.24, h), (p[0], p[1], Z + h / 2), rot=(0, 0, a), material=ASHLAR)
p = cat((0.0, -0.01, 0.0))
box((0.22, 0.20, 0.10), (p[0], p[1], Z + 0.13 + 0.05), rot=(0, 0, 14), material=QUARRY_LIGHT)
ashlar_stack(1.38, 1.22, 2, 2, 3, yaw=0, seed=1)
ashlar_stack(1.38, 0.62, 2, 1, 2, yaw=0, seed=2, size=(0.34, 0.20, 0.15))
rubble(1.42, -0.15, 0.25, 10, seed=6, peak=0.02, size=0.5)

obj = variant("Steinbruch_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
