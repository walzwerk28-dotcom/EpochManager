"""
Tempel (Antike), 2 x 3 Felder (4 x 6 m): kleiner römischer Podiumstempel, Giebel zur Straße (-Y).
Podium aus Kalkstein mit Sockelprofil und Gesims, vorn eine Freitreppe zwischen zwei Wangen; darauf die Vorhalle
(Pronaos) mit vier kannelierten Säulen in der Front und je einer weiteren an den Seiten (Basis, Schaft, korinthisch
angedeutetes Kapitell), Gebälk rundum (Architrav, Fries mit Rosetten, Gesims mit Zahnschnitt). Die Cella hat helle
verputzte Wände mit Ecken- und Wandpilastern, Fenstern, roter Zierleiste, einer zweiflügeligen Bronzetür (ein Flügel
offen, dunkler Innenraum) und hinten einer blinden Scheintür. Dreiecksgiebel vorn und hinten mit Relieffeld (Kranz,
zwei Figuren, zwei Liegende) und Akroterien (Palmetten); Satteldach mit Tegulae, Imbrices und Stirnziegeln.
Davor der Altar mit Opferflamme auf einem Pflasterfeld; ringsum ein heiliger Bezirk mit niedriger Mauer (vorn offen
zwischen zwei Torpfeilern), Zypressen, Lorbeer, Statue, Weihrauchbecken, Votivstelen, Bänken und Amphoren.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/tempel.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,55 x 5,40 m (Grundfläche 4 x 6 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (Tempel_0).
Bei einer Vorschau entstehen mehrere Bilder: <name>.png (schräg von vorn), _hinten, _nah, _spiel.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(41)

POD = 0.45                      # Höhe des Podiums
CH = 1.30                       # Säulenhöhe
Z_ARCH = POD + CH               # Unterkante des Architravs (1,75)
H = Z_ARCH + 0.31               # Oberkante des Gesimses (2,06)
YF = -1.15                      # vordere Säulenreihe
YS = -0.675                     # Säulen an den Seiten der Vorhalle
XC = 0.95                       # äußere Säulen
CELLA_X = 0.90
CELLA_Y0, CELLA_Y1 = -0.20, 1.85
_last = [0]


def count(label):
    """Gibt die Dreiecke seit dem letzten Aufruf aus (zum Abwägen beim Bauen)."""
    import roemisch
    n = sum(len(q.vertices) - 2 for o in roemisch.PARTS for q in o.data.polygons)
    print(f"  {label}: {n - _last[0]} Dreiecke")
    _last[0] = n


# ---------------------------------------------------------------- Podium und Freitreppe
temple_podium(-1.20, 1.20, -1.50, 2.00, POD)
grand_stairs(0.0, -1.58, 1.0, POD, 3, 0.14)
count("Podium, Treppe")

# ---------------------------------------------------------------- Säulen
for x in (-XC, -0.32, 0.32, XC):
    corinthian_column(x, YF, POD, CH)
for x in (-XC, XC):
    corinthian_column(x, YS, POD, CH)
count("Säulen")

# ---------------------------------------------------------------- Cella mit Tür
WALL_H = Z_ARCH - POD
box((2 * CELLA_X, CELLA_Y1, WALL_H), (0, CELLA_Y1 / 2, POD + WALL_H / 2), material=PLASTER_WHITE)
for sx in (-1, 1):                                              # Vorderwand beiderseits der Tür
    xa, xb = (0.22, CELLA_X) if sx > 0 else (-CELLA_X, -0.22)
    box((xb - xa, 0.20, WALL_H), ((xa + xb) / 2, CELLA_Y0 + 0.10, POD + WALL_H / 2), material=PLASTER_WHITE)
box((0.44, 0.20, Z_ARCH - 1.36), (0, CELLA_Y0 + 0.10, (1.36 + Z_ARCH) / 2), material=PLASTER_WHITE)
box((0.44, 0.20, 1.36 - POD), (0, CELLA_Y0 + 0.10, (POD + 1.36) / 2), material=DARK)            # dunkler Innenraum
box((2 * CELLA_X + 0.06, CELLA_Y1 - CELLA_Y0 + 0.06, 0.10), (0, (CELLA_Y0 + CELLA_Y1) / 2, POD + 0.05), material=LIME_DARK)
box((2 * CELLA_X + 0.03, CELLA_Y1 - CELLA_Y0 + 0.03, 0.05), (0, (CELLA_Y0 + CELLA_Y1) / 2, 1.62), material=SOCLE_RED)

# Eckpilaster und Wandpilaster
for sx in (-1, 1):
    for py in (CELLA_Y0 + 0.06, 0.62, 1.24, CELLA_Y1 - 0.06):
        corner = py in (CELLA_Y0 + 0.06, CELLA_Y1 - 0.06)
        px = sx * (CELLA_X + (0.0 if corner else 0.015))
        w = 0.13 if corner else 0.07
        box((w, 0.12 if corner else 0.11, WALL_H - 0.10), (px, py, POD + 0.10 + (WALL_H - 0.10) / 2), material=LIME)
        box((w + 0.03, 0.15 if corner else 0.14, 0.05), (px, py, POD + 0.125), material=LIME_DARK)
        box((w + 0.03, 0.15 if corner else 0.14, 0.05), (px, py, Z_ARCH - 0.025), material=LIME_DARK)
# Fenster in den Seitenwänden
for sx in (-1, 1):
    for wy in (0.31, 0.93, 1.55):
        xo = sx * (CELLA_X + 0.005)
        box((0.03, 0.15, 0.30), (xo, wy, 1.12), material=DARK)
        for jy in (-1, 1):
            box((0.05, 0.03, 0.34), (sx * (CELLA_X + 0.012), wy + jy * 0.09, 1.12), material=LIME)
        box((0.06, 0.24, 0.04), (sx * (CELLA_X + 0.015), wy, 1.30), material=LIME)
        box((0.07, 0.25, 0.03), (sx * (CELLA_X + 0.02), wy, 0.965), material=LIME_DARK)
count("Cella")

# Tür: Gewände, Sturz, Verdachung, Schwelle, zwei Bronzeflügel (rechts offen)
for sx in (-1, 1):
    box((0.05, 0.07, 0.93), (sx * 0.245, CELLA_Y0 - 0.025, POD + 0.465), material=LIME)
box((0.56, 0.07, 0.07), (0, CELLA_Y0 - 0.025, 1.395), material=LIME)
box((0.66, 0.10, 0.035), (0, CELLA_Y0 - 0.04, 1.4575 + 0.02), material=LIME_DARK)
for sx in (-1, 1):
    box((0.07, 0.07, 0.10), (sx * 0.26, CELLA_Y0 - 0.06, 1.37), material=LIME_DARK)
box((0.60, 0.12, 0.04), (0, CELLA_Y0 - 0.06, POD + 0.02), material=LIME)
DOOR_H = 1.36 - POD - 0.04
HINGE = (0.22, CELLA_Y0 + 0.03)
box((0.22, 0.03, DOOR_H), (-0.11, CELLA_Y0 + 0.03, POD + 0.02 + DOOR_H / 2), material=BRONZE)
for k in range(3):
    for j in range(2):
        sphere(0.015, (-0.055 - j * 0.11, CELLA_Y0 + 0.012, POD + 0.2 + k * 0.28), material=LIME_DARK, segs=5, rings=3)
ang = math.radians(-112.0)                                        # offener Flügel schwingt nach außen (-y)
dx, dy = math.cos(ang), math.sin(ang)
box((0.21, 0.03, DOOR_H), (HINGE[0] + dx * 0.105, HINGE[1] + dy * 0.105, POD + 0.02 + DOOR_H / 2),
    rot=(0, 0, math.degrees(ang)), material=BRONZE)
for k in range(3):
    for j in range(2):
        d = 0.05 + j * 0.10
        sphere(0.015, (HINGE[0] + dx * d + 0.0, HINGE[1] + dy * d - 0.02, POD + 0.2 + k * 0.28), material=LIME_DARK, segs=5, rings=3)
count("Tür")

# Scheintür und Nische an der Rückwand
yb = CELLA_Y1
box((0.40, 0.03, 0.62), (0, yb + 0.012, POD + 0.31 + 0.03), material=WOOD_DARK)
for sx in (-1, 1):
    box((0.05, 0.05, 0.68), (sx * 0.23, yb + 0.02, POD + 0.34 + 0.03), material=LIME)
box((0.56, 0.05, 0.05), (0, yb + 0.02, POD + 0.70 + 0.03), material=LIME)
box((0.64, 0.08, 0.03), (0, yb + 0.03, POD + 0.74 + 0.03), material=LIME_DARK)
for k in range(4):
    box((0.012, 0.012, 0.58), (-0.15 + k * 0.10, yb + 0.03, POD + 0.31 + 0.03), material=LIME_DARK)
for sx in (-1, 1):                                                 # Rückwand: Felder
    box((0.30, 0.02, 0.62), (sx * 0.58, yb + 0.005, POD + 0.62), material=LIME_DARK)
    ring(sx * 0.58, yb + 0.02, 1.38, 0.07, 0.012, "y", LEAF_DARK, 10)
count("Rückwand")

# ---------------------------------------------------------------- Gebälk rundum


def ring_rect(xo, y0, y1, wd, z, hh, material):
    """Rechteckiger Rahmen (Architrav, Fries, Gesims) um den Tempel: außen bei x = ±xo, von y0 bis y1, Breite wd."""
    box((2 * xo, wd, hh), (0, y0 + wd / 2, z + hh / 2), material=material)
    box((2 * xo, wd, hh), (0, y1 - wd / 2, z + hh / 2), material=material)
    for sx in (-1, 1):
        box((wd, y1 - y0 - 2 * wd, hh), (sx * (xo - wd / 2), (y0 + y1) / 2, z + hh / 2), material=material)


ring_rect(1.04, -1.25, 1.95, 0.20, Z_ARCH, 0.12, MARBLE)                     # Architrav
box((2.08, 0.012, 0.012), (0, -1.255, Z_ARCH + 0.04), material=LIME_DARK)    # Faszien
box((2.08, 0.012, 0.012), (0, -1.255, Z_ARCH + 0.08), material=LIME_DARK)
ring_rect(1.01, -1.22, 1.92, 0.14, Z_ARCH + 0.12, 0.12, LIME)                # Fries
ring_rect(1.10, -1.31, 2.01, 0.26, Z_ARCH + 0.24, 0.07, MARBLE)              # Gesims
for k in range(5):                                                          # Rosetten im Fries
    x = -0.80 + k * 0.40
    cyl(0.032, 0.02, (x, -1.235, Z_ARCH + 0.18), rot=(90, 0, 0), material=LIME_DARK, verts=8)
    sphere(0.016, (x, -1.247, Z_ARCH + 0.18), material=BRONZE, segs=5, rings=3)
for sx in (-1, 1):
    for k in range(3):
        y = -0.80 + k * 0.45
        cyl(0.032, 0.02, (sx * 1.025, y, Z_ARCH + 0.18), rot=(0, 90, 0), material=LIME_DARK, verts=8)
n_dent = int(2.0 / 0.09)                                                      # Zahnschnitt unter dem Gesims
for k in range(n_dent):
    x = -1.0 + (k + 0.5) * 2.0 / n_dent
    box((0.04, 0.04, 0.035), (x, -1.27, Z_ARCH + 0.2225), material=LIME_DARK)
for sx in (-1, 1):
    for k in range(int(3.0 / 0.10)):
        y = -1.20 + k * 0.10
        box((0.04, 0.05, 0.035), (sx * 1.06, y, Z_ARCH + 0.2225), material=LIME_DARK)
count("Gebälk")

# ---------------------------------------------------------------- Dach, Giebelfelder und Akroterien
ridge = gable_roof(-1.06, 1.06, -1.25, 1.95, H, "y", TYMPANUM, overhang=0.09, pitch=20.0)


def figure(x, z, h, y, face=-1):
    """Stehende Reliefgestalt im Giebelfeld: Gewand, Kopf, erhobener Arm."""
    cone(0.045, 0.026, h * 0.75, (x, y, z + h * 0.375), MARBLE, verts=8)
    sphere(0.026, (x, y, z + h * 0.82), material=MARBLE, segs=6, rings=4)
    bar((x + 0.025, y, z + h * 0.55), (x + 0.065, y, z + h * 0.86), 0.014, MARBLE)


def lying(x, z, y, mirror=1):
    """Liegende Reliefgestalt in der Giebelecke."""
    box((0.15, 0.04, 0.045), (x, y, z + 0.03), rot=(0, mirror * -8, 0), material=MARBLE)
    sphere(0.028, (x - mirror * 0.085, y, z + 0.07), material=MARBLE, segs=6, rings=4)
    box((0.05, 0.035, 0.03), (x + mirror * 0.085, y, z + 0.02), material=MARBLE)


for face, y, yaw in ((-1, -1.262, 0.0), (1, 1.962, 180.0)):
    zb = H + 0.02
    ring(0.0, y, H + 0.17, 0.085, 0.013, "y", MARBLE, 14)                    # Kranz
    cyl(0.07, 0.012, (0.0, y + (0.0 if face < 0 else 0.0), H + 0.17), rot=(90, 0, 0), material=LIME_DARK, verts=12)
    sphere(0.03, (0.0, y - face * 0.01, H + 0.17), (1.0, 0.6, 1.0), MARBLE, segs=6, rings=4)
    for sx in (-1, 1):
        figure(sx * 0.40, zb, 0.18, y, face)
        lying(sx * 0.76, zb, y, mirror=sx)
        for k in range(3):                                                   # Kranzbänder
            sphere(0.012, (sx * (0.11 + k * 0.045), y, H + 0.17 - k * 0.012), material=LEAF_DARK, segs=4, rings=2)
# Akroterien: Palmetten auf dem First und an den unteren Ecken
palmette(0.0, -1.30, ridge - 0.02, 0.0, h=0.20)
palmette(0.0, 2.00, ridge - 0.02, 180.0, h=0.20)
for sx in (-1, 1):
    palmette(sx * 1.06, -1.31, H, 0.0, h=0.14)
    palmette(sx * 1.06, 2.01, H, 180.0, h=0.14)
count("Dach, Giebel")

# ---------------------------------------------------------------- Heiliger Bezirk


def wall_seg(x0, x1, y0, y1, h=0.26):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    sx, sy = x1 - x0, y1 - y0
    box((sx, sy, h), (cx, cy, h / 2), material=PLASTER_WHITE)
    box((sx + 0.02, sy + 0.02, 0.06), (cx, cy, 0.03), material=STONE)
    box((sx + 0.03, sy + 0.03, 0.035), (cx, cy, h + 0.0175), material=LIME)


wall_seg(-1.74, -1.62, -2.66, 2.66)
wall_seg(1.62, 1.74, -2.66, 2.66)
wall_seg(-1.74, 1.74, 2.54, 2.66)
wall_seg(-1.74, -0.46, -2.66, -2.54)
wall_seg(0.46, 1.74, -2.66, -2.54)
for s in (-1, 1):                                                           # Torpfeiler
    px = s * 0.54
    box((0.19, 0.19, 0.07), (px, -2.60, 0.035), material=LIME_DARK)
    box((0.14, 0.14, 0.60), (px, -2.60, 0.37), material=LIME)
    box((0.20, 0.20, 0.05), (px, -2.60, 0.695), material=LIME_DARK)
    cone(0.095, 0.0, 0.12, (px, -2.60, 0.78), LIME, verts=4, rot=(0, 0, 45))
pot_plant(-0.34, -2.46, 0.05, leaves=LAUREL)
pot_plant(0.34, -2.46, 0.05, leaves=LAUREL)

# Pflasterfeld vor der Treppe mit Altar
box((1.0, 0.54, 0.02), (0, -2.27, 0.022), material=LIME)
for k in range(1, 6):
    box((1.0, 0.008, 0.004), (0, -2.54 + k * 0.09, 0.034), material=LIME_DARK)
for k in range(1, 6):
    box((0.008, 0.54, 0.004), (-0.5 + k * 0.1667, -2.27, 0.034), material=LIME_DARK)
altar(0.0, -2.30, 0.0)
count("Bezirk, Altar")

# Bäume, Bänke, Statue, Weihrauchbecken, Votivgaben
cypress(-1.52, -1.25, 1.50, 0.11)
cypress(1.52, -1.25, 1.50, 0.11)
cypress(-1.52, 1.72, 1.30, 0.10)
cypress(1.52, 1.72, 1.30, 0.10)
laurel_tree(-0.90, 2.34, 1.05)
laurel_tree(0.90, 2.34, 1.05)
bench(-1.48, 0.45, yaw=90, length=0.46)
bench(1.48, 0.45, yaw=90, length=0.46)
bush(-1.50, 1.10, 0.12, LAUREL)
bush(1.50, 1.10, 0.12, LAUREL)
bush(-1.45, -0.55, 0.10, LEAF)
bush(1.45, -0.55, 0.10, LEAF)
statue(-1.15, -2.12, h=0.62)
incense_burner(0.80, -2.28)
stele(1.22, -2.18, yaw=-10)
stele(1.45, -1.95, yaw=-25, h=0.18)
amphora(-1.45, -2.30)
amphora(-1.32, -2.42)
amphora(1.42, -2.42)
pot_plant(-1.55, -1.85, 0.05, leaves=LAUREL)
count("Ausstattung")

obj = variant("Tempel_0")
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

    shot("_hinten", (-4.5, 6.5, 3.4), (0.0, 0.0, 1.0))
    shot("_nah", (1.8, -4.6, 1.5), (0.0, -1.2, 1.1), lens=45)
    shot("_spiel", (0.0, -9.0, 8.5), (0.0, 0.0, 0.3), lens=36)
