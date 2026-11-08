"""
Förster (Antike), 2 x 2 Felder (4 x 4 m): vorn links eine kleine Bohlenhütte mit Ziegeldach (Spaten und Hacke an der
Wand neben der Tür, Fenster mit offenen Läden), dahinter ein Erdhaufen, eine Schubkarre mit Erde und ein Wasserfass mit
Krug. Rechts davon der Pflanzgarten hinter einem Flechtzaun mit Tor: vorn Setzlinge in Körben und eine Reihe frisch
gesetzter Keimlinge, dahinter Reihen junger Bäume in drei Größen (Pinie, Zypresse, Steineiche).

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/foerster.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Foerster_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(52)

# ---------------------------------------------------------------- Hütte vorn links
HX0, HX1, HY0, HY1 = -1.68, -0.46, -1.40, -0.40
HH = 0.98
plank_wall(HX0, HX1, HY0, HY1, HH)
ridge = gable_roof(HX0, HX1, HY0, HY1, HH, "x", WOOD, overhang=0.09, pitch=24)
# Fugen im Giebel an den Schmalseiten
for plane, face in ((HX0, "-x"), (HX1, "+x")):
    at, size, _, _ = face_frame(face, plane)
    ycm = (HY0 + HY1) / 2
    for k in range(-4, 5):
        gh = (ridge - HH - 0.05) * (1 - abs(k * 0.10) / ((HY1 - HY0) / 2))
        if gh > 0.06:
            box(size(0.008, 0.006, gh), at(ycm + k * 0.10, 0.003, HH + gh / 2), material=WOOD_DARK)
door("-y", HY0, -1.24, w=0.30, h=0.62, z0=0.12, frame=WOOD_DARK, leaf=WOOD, open_angle=0)
window("+x", HX1, -0.98, 0.42, w=0.20, h=0.26, shutters=WOOD, open_shutters=True, frame=WOOD_DARK)
window("-x", HX0, -0.98, 0.42, w=0.20, h=0.26, shutters=WOOD, open_shutters=False, frame=WOOD_DARK)
window("+y", HY1, -1.20, 0.42, w=0.20, h=0.26, shutters=None, frame=WOOD_DARK)
# Spaten und Hacke lehnen rechts neben der Tür an der Wand
spade((-0.80, HY0 - 0.045, 0.0), (-0.80, HY0 - 0.016, 0.82))
hoe((-0.64, HY0 - 0.045, 0.0), (-0.64, HY0 - 0.016, 0.80))
# Trittsteine zur Tür
for k, (dx, dy) in enumerate(((0.0, -0.16),)):
    box((0.17, 0.12, 0.03), (-1.24 + dx, HY0 + dy - 0.02, 0.012), rot=(0, 0, 8 * (k - 1)), material=STONE)

# ---------------------------------------------------------------- hinter der Hütte
# Erdhaufen mit eingesteckter Schaufel
sphere(0.32, (-1.15, 0.80, 0.07), (1.0, 0.85, 0.5), SOIL, segs=9, rings=4)
sphere(0.20, (-0.82, 0.55, 0.03), (1.0, 1.0, 0.4), SOIL, segs=7, rings=3)
spade((-1.00, 0.62, 0.12), (-0.86, 0.46, 0.62), across=(0.7, 0.7, 0.0))
# Schubkarre mit Erde
wheelbarrow(-1.28, 0.10, yaw=70, load="soil")
# Wasserfass mit Krug und Eimer
barrel(-0.80, -0.14, r=0.085)
jug(-0.80, -0.14, 0.22, 0.04)
bucket(-0.98, -0.20, r=0.045)
# Setzholz und ein Bündel Pfähle an der Rückseite der Hütte
for k in range(5):
    rod((-1.65 + k * 0.035, -0.31, 0.0), (-1.62 + k * 0.035, -0.37, 0.62 - 0.03 * k), 0.012, WOOD_DARK, verts=5)

# ---------------------------------------------------------------- Pflanzgarten
GX0, GX1, GY0, GY1 = -0.30, 1.74, -1.50, 1.74
wattle_fence([(GX0, -1.18), (GX0, GY1), (GX1, GY1), (GX1, GY0), (0.52, GY0)], h=0.22, spacing=0.30)
wattle_fence([(0.10, GY0), (GX0, GY0), (GX0, -1.18)], h=0.22, spacing=0.30)
for x in (0.10, 0.52):                                                       # Torpfosten
    box((0.06, 0.06, 0.34), (x, GY0, 0.17), material=WOOD_DARK)
    box((0.08, 0.08, 0.03), (x, GY0, 0.35), material=WOOD)


def bed(y, depth, x0=-0.18, x1=1.62):
    """Pflanzbeet: Erdstreifen mit Brettkante."""
    box((x1 - x0, depth, 0.05), ((x0 + x1) / 2, y, 0.025), material=SOIL)
    for s in (-1, 1):
        box((x1 - x0 + 0.03, 0.025, 0.07), ((x0 + x1) / 2, y + s * depth / 2, 0.035), material=WOOD_DARK)
    for x in (x0, x1):
        box((0.025, depth, 0.07), (x, y, 0.035), material=WOOD_DARK)


kinds = ("pine", "cypress", "oak")
# Reihe 1: frisch gesetzte Keimlinge
bed(-0.92, 0.26)
for k in range(8):
    seedling(-0.05 + k * 0.215, -0.92, seed=k)
# Reihe 2: Setzlinge etwa 0,5 m mit Stützstab
bed(-0.30, 0.34)
for k in range(4):
    x = 0.10 + k * 0.45
    young_tree(x, -0.30, 0.05, 0.50, kinds[(k + 0) % 3], seed=10 + k)
    rod((x + 0.07, -0.26, 0.04), (x + 0.07, -0.26, 0.40), 0.006, WOOD_DARK, verts=4)
# Reihe 3: junge Bäume etwa 0,85 m
bed(0.50, 0.40)
for k in range(4):
    young_tree(0.10 + k * 0.45, 0.50, 0.05, 0.85, kinds[(k + 1) % 3], seed=20 + k)
# Reihe 4: größte, etwa 1,25 m
bed(1.32, 0.50)
for k in range(3):
    young_tree(0.28 + k * 0.50, 1.32, 0.05, 1.22, kinds[(k + 2) % 3], seed=30 + k)

# Setzlinge in Körben vorn im Garten
basket_with_sapling(0.78, -1.28, 0.26, "pine")
basket_with_sapling(1.06, -1.30, 0.30, "oak")
basket_with_sapling(1.34, -1.26, 0.24, "cypress")
box((0.18, 0.12, 0.03), (0.30, -1.20, 0.012), rot=(0, 0, 5), material=STONE)                # Trittsteine ins Tor
box((0.17, 0.12, 0.03), (0.32, -1.02, 0.012), rot=(0, 0, -6), material=STONE)

obj = variant("Foerster_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
