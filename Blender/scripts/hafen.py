"""
Hafen (Antike), 3 x 3 Felder (6 x 6 m): römischer Kleinhafen. Vorderseite zur Straße (-Y), Kaiseite hinten (+Y).
Vorn zwei Torpfeiler mit Giebel, offenen Torflügeln und einem Schild; links daneben das Zollhaus (verputzt, rote
Sockelzone, Ziegeldach, Tür zum Hof, Fenster mit Läden) mit einem Wiegetisch samt Balkenwaage und einer Zollkiste; rechts
eine niedrige Hofmauer. In der Mitte links der Speicher (Horreum) aus Kalkstein mit Ziegelbändern, Satteldach (Tegulae,
Imbrices), Strebepfeilern und Lüftungsschlitzen; zur Kaiseite eine Ladetür mit Laderampe und darüber eine Ladeluke mit
Kranbalken. Rechts davon ein offener Schuppen mit Tauen, Rudern, Körben und Fässern; im Hof davor ein Handkarren,
Fässer, Kisten und ein Amphorengestell.
Hinten der Kai aus Quadersteinen über die ganze Breite (Randkante mit abfallender Außenseite), Poller mit Tauen, zwei
kurze Bohlenstege, ein Tretkran mit Laufrad, Ausleger und hängendem Lastnetz mit Amphoren, Gestelle mit Amphoren,
Kisten, Taurollen und ein Signalfeuer.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/hafen.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 5,40 x 5,40 m (Grundfläche 6 x 6 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Hafen_0).
Bei einer Vorschau entstehen mehrere Bilder: <name>.png (schräg von vorn), _hinten, _kran, _hof, _spiel.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(73)

QZ = 0.16          # Oberkante der Kaiplatte
_last = [0]


def count(label):
    """Gibt die Dreiecke seit dem letzten Aufruf aus (zum Abwägen beim Bauen)."""
    import roemisch
    n = sum(len(q.vertices) - 2 for o in roemisch.PARTS for q in o.data.polygons)
    print(f"  {label}: {n - _last[0]} Dreiecke")
    _last[0] = n


def wall_run(x0, x1, y, h=0.55, t=0.22):
    """Niedrige Hofmauer aus Kalkstein mit Ziegelband und Abdeckung."""
    box((x1 - x0, t, h), ((x0 + x1) / 2, y, h / 2), material=LIME)
    box((x1 - x0, t + 0.012, 0.045), ((x0 + x1) / 2, y, h * 0.45), material=TILE_DARK)
    box((x1 - x0 + 0.02, t + 0.05, 0.035), ((x0 + x1) / 2, y, h + 0.0175), material=LIME_DARK)


# ---------------------------------------------------------------- Zollhaus vorn links
ZX0, ZX1, ZY0, ZY1, ZH = -2.52, -1.40, -2.50, -1.55, 1.0
body(ZX0, ZX1, ZY0, ZY1, ZH, PLASTER_WHITE, socle=SOCLE_RED)
gable_roof(ZX0, ZX1, ZY0, ZY1, ZH, "y", PLASTER_WHITE, overhang=0.08, pitch=24)
door("+x", ZX1, -2.05, frame=LIME_DARK, leaf=WOOD, open_angle=55)
window("-y", ZY0, -2.12, 0.34, shutters=PAINT_GREEN)
window("-x", ZX0, -2.05, 0.34, shutters=PAINT_GREEN)
window("+x", ZX1, -1.72, 0.34, shutters=PAINT_GREEN, open_shutters=False)
box((0.40, 0.02, 0.15), (-1.92, ZY0 - 0.012, 0.76), material=LIME)                       # Inschrifttafel
for k in range(3):
    box((0.30 - k * 0.05, 0.006, 0.014), (-1.92, ZY0 - 0.024, 0.80 - k * 0.04), material=DARK)
# Zollkiste und Bank an der Hofseite
box((0.20, 0.13, 0.12), (-1.24, -2.42, 0.06), material=WOOD)
box((0.21, 0.02, 0.12), (-1.24, -2.42, 0.07), material=IRON)
box((0.02, 0.14, 0.12), (-1.24, -2.42, 0.07), material=IRON)
bench(-1.26, -1.68, yaw=90, length=0.28)

count("Zollhaus")

# ---------------------------------------------------------------- Wiegetisch mit Balkenwaage
TX, TY = -0.96, -2.05
box((0.46, 0.26, 0.03), (TX, TY, 0.30), material=WOOD)
for sx in (-1, 1):
    for sy in (-1, 1):
        box((0.04, 0.04, 0.29), (TX + sx * 0.19, TY + sy * 0.10, 0.145), material=WOOD_DARK)
box((0.40, 0.03, 0.03), (TX, TY - 0.10, 0.10), material=WOOD_DARK)
scale_balance(TX, TY, 0.315)
sack(TX - 0.17, TY, 0.332, 0.034)                                                          # Ware in der linken Schale
for k, (wx, wy) in enumerate(((0.13, 0.0), (0.18, 0.03), (0.16, -0.04))):                # Gewichte rechts und daneben
    cyl(0.017, 0.022, (TX + wx, TY + wy, 0.465 if k == 0 else 0.326), material=IRON, verts=8)
amphora(TX + 0.30, TY + 0.02)
box((0.14, 0.10, 0.012), (TX - 0.33, TY + 0.02, 0.006), rot=(0, 0, 12), material=WOOD)      # Schreibtafel am Boden

count("Wiegetisch")

# ---------------------------------------------------------------- Tor vorn
GY = -2.50
for px in (-0.65, 0.65):
    box((0.34, 0.34, 0.10), (px, GY, 0.05), material=LIME_DARK)
    box((0.28, 0.28, 1.30), (px, GY, 0.65), material=LIME)
    brick_bands(px - 0.14, px + 0.14, GY - 0.14, GY + 0.14, (0.40, 0.70, 1.00, 1.22))
    box((0.36, 0.36, 0.05), (px, GY, 1.325), material=LIME_DARK)
    cone(0.25, 0.0, 0.18, (px, GY, 1.44), TILE_DARK, verts=4, rot=(0, 0, 45))
box((1.14, 0.22, 0.13), (0.0, GY, 1.075), material=WOOD_DARK)                              # Sturzbalken
for k in range(5):
    box((0.012, 0.226, 0.13), (-0.40 + k * 0.20, GY, 1.075), material=WOOD)
slab([(-0.55, 1.14), (0.55, 1.14), (0.0, 1.42)], "xz", GY - 0.07, 0.14, LIME)                # Giebelfeld
cyl(0.11, 0.02, (0.0, GY - 0.075, 1.24), rot=(90, 0, 0), material=TERRACOTTA, verts=14)     # Schild
cyl(0.045, 0.026, (0.0, GY - 0.08, 1.24), rot=(90, 0, 0), material=BRONZE, verts=10)
for s in (-1, 1):                                                                          # Torflügel, nach innen offen
    a = math.radians(72)
    hinge = Vector((s * 0.51, GY + 0.02))
    d = Vector((-s * math.cos(a), math.sin(a)))
    c = hinge + d * 0.25
    yaw = math.degrees(math.atan2(d.y, d.x))
    box((0.50, 0.04, 0.98), (c.x, c.y, 0.50), rot=(0, 0, yaw), material=WOOD)
    for z in (0.16, 0.50, 0.84):
        box((0.50, 0.046, 0.06), (c.x, c.y, z), rot=(0, 0, yaw), material=WOOD_DARK)
    box((0.04, 0.05, 0.99), (hinge.x, hinge.y, 0.50), material=IRON)
wall_run(-1.40, -0.82, GY, h=0.45)
wall_run(0.82, 2.64, GY, h=0.40)

count("Tor")

# ---------------------------------------------------------------- Speicher (Horreum)
X0, X1, Y0, Y1 = -2.25, 0.35, -0.55, 1.25
H = 1.75
body(X0, X1, Y0, Y1, H, LIME)
box((X1 - X0 + 0.06, Y1 - Y0 + 0.06, 0.28), ((X0 + X1) / 2, (Y0 + Y1) / 2, 0.14), material=LIME_DARK)   # Sockel
ridge = gable_roof(X0, X1, Y0, Y1, H, "x", LIME, overhang=0.08, pitch=22)
brick_bands(X0, X1, Y0, Y1, (0.42, 0.72, 1.30, 1.62))
cornice(X0, X1, Y0, Y1, 1.06)
for (x, y) in ((X0, Y0), (X1, Y0), (X0, Y1), (X1, Y1)):
    box((0.14, 0.14, H - 0.05), (x, y, (H - 0.05) / 2), material=LIME_DARK)
# Hofseite (-y): Strebepfeiler, Schlitze, Tür
for u in (-1.90, -0.05):
    buttress("-y", Y0, u, 1.55)
for u in (-1.45, -0.45):
    slit("-y", Y0, u, 0.62)
    slit("-y", Y0, u, 1.16)
door("-y", Y0, -0.95, w=0.36, h=0.62, z0=0.28)
for u in (-1.45, -0.45):
    at, size, _, _ = face_frame("-y", Y0)
    box(size(0.08, 0.012, 0.07), at(u, 0.035, 0.15), material=DARK)
# Kaiseite (+y): Ladetür, Ladeluke mit Kranbalken, Strebepfeiler
for u in (-1.90, -0.05):
    buttress("+y", Y1, u, 1.55)
for u in (-1.52, -0.38):
    slit("+y", Y1, u, 0.62)
    slit("+y", Y1, u, 1.16)
door("+y", Y1, -0.95, w=0.66, h=0.72, z0=0.30, leaf=WOOD_DARK, open_angle=0)
box((0.008, 0.008, 0.68), (-0.95, Y1 + 0.026, 0.66), material=WOOD_DARK)
for a in range(0, 181, 20):                                                                # Ziegelbogen über der Ladetür
    t = math.radians(a)
    box((0.07, 0.015, 0.05), (-0.95 + 0.40 * math.cos(t), Y1 + 0.008, 1.03 + 0.07 * math.sin(t)), rot=(0, -a * 0.25, 0),
        material=TILE_DARK)
door("+y", Y1, -0.95, w=0.40, h=0.38, z0=1.16, leaf=WOOD)                                   # Ladeluke
hoist_out(-0.95, Y1, 1.62, reach=0.50, sign=1, sack_z=0.58)
# Giebelseiten: Rundfenster
for xf, rot in ((X0 - 0.005, (0, 90, 0)), (X1 + 0.005, (0, 90, 0))):
    cyl(0.10, 0.01, (xf, 0.35, 1.97), rot=rot, material=TILE_DARK, verts=16)
    cyl(0.07, 0.012, (xf + (-0.003 if xf < 0 else 0.003), 0.35, 1.97), rot=rot, material=DARK, verts=16)
for yy in (0.62, 0.08):                                                                    # kleine Schlitze im Sockel
    at, size, _, _ = face_frame("-x", X0)
    slit("-x", X0, 0.35 + (yy - 0.35) * 2.2, 0.62)
door("+x", X1, 0.35, w=0.36, h=0.62, z0=0.28)
slit("+x", X1, -0.25, 0.62)
slit("+x", X1, 0.95, 0.62)
# Laderampe an der Ladetür
box((1.12, 0.40, 0.30), (-0.95, Y1 + 0.20, 0.15), material=LIME_DARK)
box((1.16, 0.44, 0.03), (-0.95, Y1 + 0.21, 0.315), material=LIME)
for x in (-1.52, -0.38):
    box((0.012, 0.44, 0.004), (x, Y1 + 0.21, 0.331), material=LIME_DARK)
for x, y in ((-1.40, Y1 + 0.16), (-1.28, Y1 + 0.30)):
    sack(x, y, 0.33, 0.055)
barrel(-0.45, Y1 + 0.26, 0.33, r=0.065)
amphora(-0.62, Y1 + 0.12, 0.33)
amphora(-0.50, Y1 + 0.34, 0.33)
# Seite und Hof
woodpile(-2.50, 0.1, length=0.40, rows=(3, 2), yaw=90)
sack(-2.52, 0.65, 0.0, 0.06)
sack(-2.50, 0.80, 0.0, 0.055)
barrel(-2.50, -0.30, r=0.07)
barrel(-2.50, -0.12, r=0.07)

count("Speicher")

# ---------------------------------------------------------------- Hof rechts vorn
cart(1.45, -1.55, yaw=-20, load="sacks")
for x, y in ((2.30, -2.05), (2.30, -1.86), (2.12, -2.05)):
    barrel(x, y, r=0.07)
barrel(2.20, -1.60, 0.0, r=0.07, lying=True, yaw=70)
crate(0.98, -2.10, size=0.20, yaw=8)
crate(1.20, -2.14, size=0.18, yaw=-10)
crate(1.06, -2.12, z0=0.20, size=0.16, yaw=20)
for k in range(3):
    amphora(1.55 + k * 0.13, -2.10)
box((0.46, 0.025, 0.025), (1.68, -2.16, 0.18), material=WOOD_DARK)
for x in (1.47, 1.89):
    box((0.03, 0.03, 0.26), (x, -2.10, 0.13), material=WOOD_DARK)
rope_coil(0.95, -1.40, 0.0, 0.07, 2)
sack(2.40, -1.05, 0.0, 0.06)
sack(2.20, -0.95, 0.0, 0.06)

count("Hof")

# ---------------------------------------------------------------- offener Schuppen rechts
SX0, SX1, SY0, SY1 = 1.15, 2.45, -0.80, 0.00
open_shed(SX0, SX1, SY0, SY1, 0.72)
barrel(1.30, -0.22, r=0.07)
barrel(1.48, -0.20, r=0.07)
barrel(1.38, -0.42, 0.0, r=0.07, lying=True, yaw=80)
sack(1.80, -0.26, 0.0, 0.06)
sack(1.96, -0.32, 0.0, 0.055)
rope_coil(2.20, -0.40, 0.0, 0.08, 2)
crate(2.30, -0.15, size=0.17, yaw=6)
oar((2.38, -0.06, 0.0), (2.24, -0.06, 0.70))
oar((1.24, -0.06, 0.0), (1.18, -0.06, 0.70))

# Ware zum Verladen zwischen Schuppen und Kai
crate(1.20, 0.80, size=0.20, yaw=10)
crate(1.42, 0.86, size=0.18, yaw=-8)
crate(1.28, 0.82, z0=0.20, size=0.16, yaw=25)
barrel(2.40, 0.90, r=0.07)
barrel(2.40, 0.72, r=0.07)
sack(0.70, 0.40, 0.0, 0.06)
sack(0.84, 0.50, 0.0, 0.06)
sack(0.72, 0.58, 0.0, 0.055)

count("Schuppen")

# ---------------------------------------------------------------- Kai
quay_platform(-2.65, 2.65, 1.20, 2.65, z=QZ, gaps=((-1.55, -0.80), (0.30, 1.05)))
for (x0, x1) in ((-1.55, -0.80), (0.30, 1.05)):
    plank_pier(x0, x1, 2.20, 2.65, z=QZ)
count("Kaiplatte")
for x in (-2.35, -0.50, 1.20, 2.40):
    bollard(x, 2.44, 0.24, wraps=1)
rope((-0.50, 2.44, 0.30), (-0.78, 2.60, 0.19), sag=0.03, thick=0.011)
rope((1.20, 2.44, 0.30), (1.03, 2.60, 0.19), sag=0.03, thick=0.011)
count("Stege, Poller")
# Dinge auf den Stegen
amphora(-1.35, 2.50, QZ)
amphora(-1.22, 2.55, QZ)
rope_coil(-1.05, 2.45, QZ, 0.06, 2)
bucket(0.50, 2.50, QZ, 0.045)
crate(0.78, 2.46, QZ, 0.16, yaw=15)
fish_basket(0.62, 2.20, QZ, yaw=40)
count("Dinge Stege")
# Tretkran
tip = treadwheel_crane(1.70, 1.70, QZ, jib_back=0.55, jib_front=0.75, hook_z=0.88)
amphora_load(tip[0], tip[1], 0.41, 0.88)
count("Kran")
# Gestelle mit Amphoren, Kisten, Taurollen, Signalfeuer
for ry in (1.78, 2.06):
    for k in range(3):
        amphora(-2.45 + k * 0.13, ry, QZ)
    box((0.46, 0.025, 0.025), (-2.32, ry, QZ + 0.18), material=WOOD_DARK)
    for x in (-2.52, -2.12):
        box((0.03, 0.03, 0.26), (x, ry, QZ + 0.13), material=WOOD_DARK)
crate(-0.55, 1.95, QZ, 0.20, yaw=6)
crate(-0.28, 2.00, QZ, 0.18, yaw=-12)
crate(-0.45, 1.98, QZ + 0.20, 0.16, yaw=24)
sack(0.05, 1.82, QZ, 0.06)
sack(0.20, 1.92, QZ, 0.06)
sack(0.10, 2.05, QZ, 0.055)
barrel(0.60, 1.65, QZ, r=0.07)
barrel(0.78, 1.68, QZ, r=0.07)
crate(2.40, 1.70, QZ, 0.20, yaw=-8)
crate(2.38, 1.92, QZ, 0.18, yaw=12)
rope_coil(2.32, 2.22, QZ, 0.07, 2)
brazier(-2.45, 2.30, QZ)

count("Kai und Kran")
obj = variant("Hafen_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=9.0)

if PREVIEW:
    import bpy
    scene = bpy.context.scene
    cam = bpy.data.objects["Kamera"]
    base = os.path.splitext(PREVIEW)[0]

    def shot(name, loc, target, lens=40):
        cam.location = loc
        cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
        cam.data.lens = lens
        scene.render.filepath = base + name + ".png"
        bpy.ops.render.render(write_still=True)

    shot("_hinten", (1.5, 7.0, 4.2), (0.0, 0.8, 0.5))
    shot("_kran", (-1.0, 4.4, 2.2), (1.3, 2.0, 0.8))
    shot("_hof", (-3.4, -4.2, 2.4), (0.0, -1.0, 0.5))
    shot("_spiel", (0.0, -9.0, 8.5), (0.0, 0.0, 0.3), lens=36)
