"""
Fischer (Antike), 2 x 2 Felder (4 x 4 m): römische Fischerstelle am Ufer, Vorderseite zur Straße (-Y), Arbeitsseite mit Boot
und Steg hinten (+Y) zum Wasser.
Vorn links die Fischerhütte aus Bohlen auf Bruchsteinsockel mit Ziegel-Satteldach (Tür halb offen, Fenster mit offenen
Läden); vorn rechts ein offener Schuppen mit Fässern, Fischkörben und Garum-Amphoren, vor dem getrocknete Fische hängen;
dazwischen ein Amphorenstand und zwei angelehnte Ruder. In der Mitte ein Räuchergestell mit hängenden Fischen über einer
Feuerstelle, ein Arbeitstisch zum Ausnehmen mit Messer, Fischen und Eimern und rechts ein Trockengestell mit zwei Netzen
(Kork-Schwimmer, Bleigewichte). Hinten links ein kieloben auf zwei Böcken liegendes Ruderboot, hinten rechts ein kurzer
Bohlensteg mit Reusen, Fischkorb, Eimer und Tauen; Steinanker und Taurolle vorn rechts.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/fischer.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,64 x 3,64 m (Grundfläche 4 x 4 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Fischer_0).
Bei einer Vorschau entstehen zwei Bilder: <name>.png (schräg von vorn) und <name>_hinten.png (von der Wasserseite).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(41)

# ---------------------------------------------------------------- Fischerhütte vorn links
HX0, HX1, HY0, HY1 = -1.70, -0.50, -1.45, -0.45
HH = 0.95
plank_wall(HX0, HX1, HY0, HY1, HH)
gable_roof(HX0, HX1, HY0, HY1, HH, "x", WOOD, overhang=0.09, pitch=22)
door("-y", HY0, -1.22, frame=WOOD_DARK, open_angle=65)
window("-y", HY0, -0.78, 0.42, shutters=PAINT_BLUE, frame=WOOD_DARK)
window("-x", HX0, -0.95, 0.42, shutters=PAINT_BLUE, frame=WOOD_DARK)
window("+x", HX1, -0.95, 0.42, shutters=PAINT_BLUE, open_shutters=False, frame=WOOD_DARK)
# Steinplatte vor der Tür und Taurolle am Pfosten
box((0.46, 0.20, 0.025), (-1.22, HY0 - 0.22, 0.012), rot=(0, 0, 3), material=LIME_DARK)
rope_coil(-1.62, HY0 - 0.12, 0.0, 0.055, 3)
# Amphorenstand (Garum) zwischen Hütte und Schuppen
for ax, ay in ((-0.38, -1.38), (-0.25, -1.34), (-0.32, -1.22)):
    amphora(ax, ay)
box((0.40, 0.045, 0.03), (-0.31, -1.40, 0.02), material=WOOD_DARK)
# zwei Ruder an die Hüttenwand gelehnt (Blatt unten, Griff an der Wand)
oar((-0.20, -0.82, 0.0), (-0.49, -0.82, 0.93))
oar((-0.14, -0.66, 0.0), (-0.49, -0.66, 0.91))

# ---------------------------------------------------------------- offener Schuppen vorn rechts
SX0, SX1, SY0, SY1 = 0.80, 1.74, -1.45, -0.65
open_shed(SX0, SX1, SY0, SY1, 0.78)
barrel(0.94, -0.84, r=0.07)
barrel(1.12, -0.84, r=0.07)
barrel(1.02, -1.14, 0.0, r=0.07, lying=True, yaw=80)
fish_basket(1.36, -0.88)
fish_basket(1.58, -0.84, yaw=40)
fish_basket(1.38, -1.14, yaw=10)
crate(1.62, -1.18, size=0.17, yaw=8)
crate(1.62, -1.18, z0=0.17, size=0.15, yaw=-12)
for k, fx in enumerate((0.96, 1.10, 1.24, 1.38, 1.52, 1.66)):                     # getrocknete Fische am Vorderbalken
    fish(fx, SY0 + 0.04, 0.60, yaw=(k % 3 - 1) * 10, flat=False)
# Steinanker und Tau rechts vom Schuppen
stone_anchor(0.50, -1.30, yaw=20)
rope_coil(0.22, -1.36, 0.0, 0.07, 3)
amphora(0.60, -0.48)
reuse(0.12, -0.90, yaw=20)
reuse(0.42, -1.02, yaw=-35)
fish(-0.12, -1.05, 0.0, yaw=75)

# ---------------------------------------------------------------- Räuchergestell links in der Mitte
RY = 0.30
for x in (-1.72, -0.92):
    box((0.05, 0.05, 0.70), (x, RY, 0.35), material=WOOD_DARK)
for sgn, x in ((1, -1.72), (-1, -0.92)):                                           # Streben
    bar((x, RY - 0.02, 0.42), (x + sgn * 0.14, RY - 0.20, 0.0), 0.03, WOOD_DARK)
    bar((x, RY + 0.02, 0.42), (x + sgn * 0.14, RY + 0.20, 0.0), 0.03, WOOD_DARK)
bar((-1.74, RY, 0.66), (-0.90, RY, 0.66), 0.032, WOOD)
bar((-1.74, RY, 0.50), (-0.90, RY, 0.50), 0.020, WOOD)                             # zweite, niedrigere Stange
for k in range(8):
    fish(-1.62 + k * 0.095, RY - 0.012, 0.50, yaw=(-1) ** k * 6, flat=False)         # Fischreihe an der oberen Stange
for k in range(4):
    fish(-1.57 + k * 0.17, RY + 0.012, 0.34, yaw=(-1) ** k * 8, flat=False)          # untere Reihe an der niedrigen Stange
# Feuerstelle darunter: Steinring mit Asche
for k in range(10):
    t = math.radians(k * 36)
    box((0.07, 0.05, 0.05), (-1.32 + 0.17 * math.cos(t), RY + 0.17 * math.sin(t), 0.025), rot=(0, 0, math.degrees(t) + 90),
        material=(ROCK_A, ROCK_B, ROCK_C)[k % 3])
cyl(0.14, 0.02, (-1.32, RY, 0.012), material=DARK, verts=14)
woodpile(-1.15, 0.72, length=0.30, rows=(3, 2), yaw=0)

# ---------------------------------------------------------------- Arbeitstisch zum Ausnehmen
TX, TY = -0.05, 0.42
box((0.54, 0.28, 0.03), (TX, TY, 0.405), material=WOOD)
for k in (-1, 0, 1):                                                               # Bohlenfugen
    box((0.004, 0.28, 0.004), (TX + k * 0.14, TY, 0.422), material=WOOD_DARK)
for sx in (-1, 1):
    for sy in (-1, 1):
        box((0.04, 0.04, 0.39), (TX + sx * 0.23, TY + sy * 0.10, 0.195), material=WOOD_DARK)
box((0.50, 0.03, 0.03), (TX, TY - 0.10, 0.14), material=WOOD_DARK)
box((0.50, 0.03, 0.03), (TX, TY + 0.10, 0.14), material=WOOD_DARK)
box((0.20, 0.14, 0.02), (TX - 0.10, TY, 0.43), material=WOOD_DARK)                   # Schneidebrett
fish(TX - 0.10, TY + 0.01, 0.44, yaw=15)
fish(TX + 0.10, TY - 0.06, 0.435, yaw=-25)
fish(TX + 0.14, TY + 0.07, 0.435, yaw=160)
bar((TX - 0.03, TY - 0.07, 0.445), (TX + 0.07, TY - 0.02, 0.445), 0.008, WOOD_DARK)  # Messergriff
bar((TX + 0.07, TY - 0.02, 0.445), (TX + 0.14, TY + 0.01, 0.445), 0.010, IRON)       # Klinge
bucket(TX + 0.40, TY + 0.02, fish_inside=True)
bucket(TX - 0.40, TY - 0.02, 0.0, 0.05)
fish(TX - 0.45, TY + 0.22, 0.0, yaw=70)
fish(TX + 0.30, TY - 0.28, 0.0, yaw=-40)

# ---------------------------------------------------------------- Trockengestell mit Netzen rechts in der Mitte
NY = 0.34
for x in (0.80, 1.74):
    box((0.05, 0.05, 0.88), (x, NY, 0.44), material=WOOD_DARK)
    sgn = 1 if x < 1.0 else -1
    bar((x, NY - 0.02, 0.60), (x + sgn * 0.18, NY - 0.24, 0.0), 0.03, WOOD_DARK)
    bar((x, NY + 0.02, 0.60), (x + sgn * 0.18, NY + 0.24, 0.0), 0.03, WOOD_DARK)
bar((0.78, NY, 0.84), (1.76, NY, 0.84), 0.036, WOOD)
net((0.84, NY - 0.025, 0.82), (1.28, NY - 0.025, 0.82), 0.48, sag=0.05, seed=1)
net((1.34, NY + 0.025, 0.82), (1.72, NY + 0.025, 0.82), 0.40, sag=0.04, seed=2)

# ---------------------------------------------------------------- Ruderboot hinten links
row_boat(-0.62, 1.38, length=1.90, beam=0.54, depth=0.20, z_gunwale=0.26)
woodpile(-1.65, 1.66, length=0.26, rows=(2, 1), yaw=0)
bucket(0.40, 1.15, 0.0, 0.05)
sack(-1.66, 0.98, 0.0, 0.055)

# ---------------------------------------------------------------- Bohlensteg hinten rechts
PX0, PX1, PY0, PY1 = 0.82, 1.78, 0.98, 1.80
plank_pier(PX0, PX1, PY0, PY1, z=0.15)
reuse(1.20, 1.22, yaw=90, z0=0.15)
fish_basket(1.58, 1.52, 0.15, yaw=30)
rope_coil(1.00, 1.62, 0.15, 0.06, 3)
bucket(1.12, 1.45, 0.15, 0.045)
fish(1.36, 1.10, 0.15, yaw=40)
# Poller am Stegende
cyl(0.04, 0.16, (1.30, 1.74, 0.23), material=WOOD_DARK, verts=8)
sphere(0.05, (1.30, 1.74, 0.31), (1.0, 1.0, 0.6), WOOD_DARK, segs=8, rings=4)

obj = variant("Fischer_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}")

export(spacing=8.0)

if PREVIEW:
    import bpy
    from mathutils import Vector
    scene = bpy.context.scene
    cam = bpy.data.objects["Kamera"]
    base = os.path.splitext(PREVIEW)[0]
    # von der Wasserseite, schräg von hinten oben
    cam.location = (-1.6, 4.6, 3.0)
    cam.rotation_euler = (Vector((0.0, 0.0, 0.3)) - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = base + "_hinten.png"
    bpy.ops.render.render(write_still=True)
    # von vorn links, tiefer
    cam.location = (-3.2, -4.0, 2.2)
    cam.rotation_euler = (Vector((0.0, 0.0, 0.3)) - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = base + "_links.png"
    bpy.ops.render.render(write_still=True)
