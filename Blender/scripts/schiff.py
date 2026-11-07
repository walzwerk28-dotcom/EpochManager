"""
Römisches Frachtschiff (Corbita) für den Hafen: bauchiger Rumpf aus Planken (Wulst rot, Streifen creme, Planken in
zwei Brauntönen, unter der Wasserlinie Pech), Deck mit Ladeluke, Amphoren, Fässern und Säcken, kleine Kajüte mit
Ziegeldach am Heck, Gänsehals-Zier am Heck, zwei Steuerruder, Mast mit Rah und halb gerefftem Rahsegel (Leinen mit
roten Streifen), Vorsegel (Artemon) am schräg vorgeneigten Mast, Wanten und Stage, Anker an Deck.

Maße: etwa 3,6 m lang, 1,1 m breit (Ruder 1,3 m), Mast 2,4 m über der Wasserlinie. Ursprung in der Mitte, die
Wasserlinie liegt bei z = 0 (der Rumpf ragt bis -0,15 m darunter), Bug nach -Y, Heck nach +Y. Ein Objekt (Schiff).

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/schiff.py -- <blend> <fbx> [<vorschau.png>]
Bei einer Vorschau entstehen mehrere Bilder: <name>.png (schräg von der Seite), _oben, _bug, _heck.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

L2 = 1.75            # halbe Rumpflänge
WMAX = 0.55          # halbe Breite
P_ROUND = 1.8        # Rundung des Querschnitts
BULWARK = 0.30       # Anteil der Rumpftiefe, der als Bordwand über das Deck ragt
S_DECK = (1.0 - BULWARK) ** (1.0 / P_ROUND)
COLS = 21            # Punkte je Querschnitt
STATIONS = 25
DECK_WOOD = mat("DeckHolz", (0.66, 0.50, 0.32))
DECK_WOOD2 = mat("DeckHolzDunkel", (0.55, 0.40, 0.25))


def half_width(t):
    tt = min(abs(t), 0.985)
    return WMAX * (1.0 - tt ** (2.0 if t < 0 else 2.8)) ** 0.6


def z_gun(t):
    return 0.34 + (0.08 * t * t if t < 0 else 0.15 * t ** 3 + 0.02 * t * t)


def z_keel(t):
    return -0.15 * (1.0 - min(abs(t), 1.0) ** 3)


def depth(t):
    return z_gun(t) - z_keel(t)


def deck_z(y):
    t = max(-1.0, min(1.0, y / L2))
    return z_gun(t) - BULWARK * depth(t)


def hull_pt(t, s):
    """Punkt der Außenhaut: t = -1 Bug .. 1 Heck, s = -1 Backbord-Reling .. 0 Kiel .. 1 Steuerbord-Reling."""
    return (s * half_width(t), t * L2, z_gun(t) - depth(t) * (1.0 - abs(s) ** P_ROUND))


def inner_pt(t, s):
    x, y, z = hull_pt(t, s)
    inset = min(0.035, abs(x) * 0.8)
    return (x - math.copysign(inset, x) if x != 0 else 0.0, y, z)


ts = [-1.0 + 2.0 * i / (STATIONS - 1) for i in range(STATIONS)]
ss = [-1.0 + 2.0 * j / (COLS - 1) for j in range(COLS)]

# ------------------------------------------------------------------ Rumpf
BANDS = [SOCLE_RED, HULL_CREAM, HULL_CREAM, WOOD, WOOD_DARK, TAR, TAR, TAR, TAR, TAR]
loft([[hull_pt(t, s) for s in ss] for t in ts], lambda j: BANDS[min(j, COLS - 2 - j)], flip=True, name="Rumpf")

# Bordwand: Kappe und Innenseite je Seite, dazu das Deck
for side in (-1, 1):
    rings = []
    for t in ts:
        ring = [hull_pt(t, side), inner_pt(t, side)]
        for k in (0.5, 1.0):
            ring.append(inner_pt(t, side * (1.0 - k * (1.0 - S_DECK))))
        rings.append(ring)
    loft(rings, lambda j: WOOD, flip=(side > 0), name="Bordwand")
deck_cols = 10
loft([[(inner_pt(t, -S_DECK)[0] + (inner_pt(t, S_DECK)[0] - inner_pt(t, -S_DECK)[0]) * j / deck_cols, t * L2,
        z_gun(t) - BULWARK * depth(t)) for j in range(deck_cols + 1)] for t in ts],
     lambda j: DECK_WOOD if j % 2 == 0 else DECK_WOOD2, name="Deck")

# Steven und Achtersteven, Kielleiste
bar((0.0, -1.745, -0.04), (0.0, -1.80, 0.47), 0.05, WOOD_DARK)
sphere(0.04, (0.0, -1.80, 0.49), material=WOOD_DARK, segs=7, rings=4)
bar((0.0, 1.745, -0.02), (0.0, 1.80, 0.52), 0.05, WOOD_DARK)
for a, b in zip(ts, ts[1:]):
    bar((0.0, a * L2, z_keel(a) - 0.01), (0.0, b * L2, z_keel(b) - 0.01), 0.04, WOOD_DARK)

# Gänsehals-Zier am Heck
goose_neck(0.0, 1.80, 0.52, r=0.45, sweep=140.0, thick=0.05)

# Augen am Bug
t_eye = -0.70
for side in (-1, 1):
    z_eye = 0.20
    sv = (1.0 - (z_gun(t_eye) - z_eye) / depth(t_eye)) ** (1.0 / P_ROUND)
    x = side * half_width(t_eye) * sv
    cyl(0.055, 0.012, (x + side * 0.004, t_eye * L2, z_eye), rot=(0, 90, 0), material=HULL_CREAM, verts=12)
    cyl(0.025, 0.014, (x + side * 0.008, t_eye * L2, z_eye), rot=(0, 90, 0), material=DARK, verts=10)

# ------------------------------------------------------------------ Deck
MAST_Y = -0.30
# Mastspur und Querbalken
box((0.46, 0.07, 0.05), (0.0, MAST_Y, deck_z(MAST_Y) + 0.025), material=WOOD_DARK)
box((0.12, 0.12, 0.06), (0.0, MAST_Y, deck_z(MAST_Y) + 0.03), material=WOOD_DARK)

# Ladeluke: Süll, dunkle Öffnung, halb mit Planken abgedeckt
hy, hz = 0.33, deck_z(0.33)
for dx in (-0.17, 0.17):
    box((0.035, 0.42, 0.06), (dx, hy, hz + 0.03), material=WOOD_DARK)
for dy in (-0.21, 0.21):
    box((0.37, 0.035, 0.06), (0.0, hy + dy, hz + 0.03), material=WOOD_DARK)
box((0.30, 0.38, 0.01), (0.0, hy, hz + 0.045), material=DARK)
for i in range(4):
    box((0.32, 0.075, 0.018), (0.0, hy - 0.16 + i * 0.085, hz + 0.065), material=DECK_WOOD)
for dx in (-0.1, 0.1):
    box((0.03, 0.40, 0.012), (dx, hy - 0.0, hz + 0.082), material=WOOD_DARK)

# Ladung: Amphoren vorn, Fässer und Säcke hinter der Luke
for x, y in ((-0.22, -0.98), (0.0, -0.98), (0.22, -0.98), (-0.11, -0.78), (0.11, -0.78), (-0.22, -0.60), (0.22, -0.60)):
    amphora(x, y, deck_z(y))
barrel(-0.25, 0.68, deck_z(0.68), 0.065)
barrel(0.26, 0.66, deck_z(0.66), 0.065)
barrel(0.0, 0.72, deck_z(0.72), 0.06, lying=True, yaw=90)
sack(-0.10, 0.60, deck_z(0.60), 0.065)
sack(0.12, 0.74, deck_z(0.74), 0.06)
rope_coil(0.24, 0.10, deck_z(0.10), 0.07, 3)
rope_coil(-0.22, -0.14, deck_z(-0.14), 0.06, 2)
ship_anchor(-0.24, -1.30, deck_z(-1.30), yaw=8)
barrel(0.22, -1.32, deck_z(-1.32), 0.055)

# ------------------------------------------------------------------ Kajüte am Heck
cy0, cy1, cx = 0.84, 1.17, 0.21
cz0 = deck_z(cy1) - 0.02
CH = 0.32
box((2 * cx, cy1 - cy0, CH), (0.0, (cy0 + cy1) / 2, cz0 + CH / 2), material=WOOD)
for k in range(5):                                                           # senkrechte Leisten
    xk = -cx + (k + 0.5) * 2 * cx / 5
    box((0.02, 0.01, CH), (xk, cy0 - 0.004, cz0 + CH / 2), material=WOOD_DARK)
box((0.11, 0.012, 0.22), (0.0, cy0 - 0.004, cz0 + 0.14), material=DARK)    # Tür nach vorn
box((0.13, 0.016, 0.02), (0.0, cy0 - 0.006, cz0 + 0.255), material=WOOD_DARK)
for sx in (-1, 1):
    box((0.012, 0.09, 0.09), (sx * (cx + 0.002), (cy0 + cy1) / 2, cz0 + 0.20), material=DARK)
box((2 * cx + 0.04, 0.03, 0.03), (0.0, cy0, cz0 + 0.01), material=WOOD_DARK)
pitch = 28.0
half = cx + 0.05
rw = half / math.cos(math.radians(pitch))
rz = cz0 + CH + math.tan(math.radians(pitch)) * half / 2
ry = (cy0 + cy1) / 2
for sx in (-1, 1):
    box((rw, cy1 - cy0 + 0.08, 0.025), (sx * half / 2, ry, rz), rot=(0, sx * pitch, 0), material=TILE)
bar((0.0, cy0 - 0.04, cz0 + CH + math.tan(math.radians(pitch)) * half),
    (0.0, cy1 + 0.04, cz0 + CH + math.tan(math.radians(pitch)) * half), 0.03, TILE_DARK)

# ------------------------------------------------------------------ Steuerruder
for side in (-1, 1):
    oar((side * 0.40, 1.66, -0.30), (side * 0.56, 1.02, 0.78), shaft=0.026)
    # Halterung: Pfosten an der Reling und Zurrung
    box((0.04, 0.05, 0.14), (side * 0.43, 1.27, z_gun(1.27 / L2) + 0.02), material=WOOD_DARK)
    bar((side * 0.43, 1.27, z_gun(1.27 / L2) + 0.08), (side * 0.50, 1.27, z_gun(1.27 / L2) + 0.12), 0.025, WOOD_DARK)
    bar((side * 0.56, 1.02, 0.78), (side * 0.56, 1.02, 0.92), 0.03, WOOD_DARK)

# ------------------------------------------------------------------ Mast, Rah, Segel
cyl(0.034, 2.25, (0.0, MAST_Y, deck_z(MAST_Y) + 1.125), material=WOOD, verts=10)
cyl(0.042, 0.06, (0.0, MAST_Y, deck_z(MAST_Y) + 0.30), material=IRON, verts=10)
sphere(0.045, (0.0, MAST_Y, 2.40), material=WOOD_DARK, segs=8, rings=5)
slab([(MAST_Y, 2.37), (MAST_Y + 0.34, 2.335), (MAST_Y, 2.30)], "yz", -0.005, 0.01, SAIL_RED)    # Wimpel

YARD_Y, YARD_Z = MAST_Y - 0.06, 2.00
bar((-0.82, YARD_Y, YARD_Z + 0.07), (0.0, YARD_Y, YARD_Z), 0.034, WOOD)
bar((0.0, YARD_Y, YARD_Z), (0.82, YARD_Y, YARD_Z + 0.07), 0.034, WOOD)
box((0.09, 0.07, 0.07), (0.0, MAST_Y - 0.035, YARD_Z), material=IRON)
sail_panel(-0.76, 0.76, 1.20, 1.97, YARD_Y - 0.02, bulge=0.13,
           stripes=((0.30, 0.34, SAIL_RED), (0.66, 0.70, SAIL_RED), (0.0, 0.025, SAIL_RED), (0.975, 1.0, SAIL_RED)))
# gerefftes Tuch: aufgerollt am Unterliek, mit Zeisingen
cyl(0.048, 1.5, (0.0, YARD_Y - 0.03, 1.17), rot=(0, 90, 0), material=SAIL_LINEN, verts=10)
for xk in (-0.62, -0.31, 0.0, 0.31, 0.62):
    cyl(0.052, 0.018, (xk, YARD_Y - 0.03, 1.17), rot=(0, 90, 0), material=ROPE, verts=10)
for xk in (-0.5, -0.17, 0.17, 0.5):                                         # Gordings zur Rah
    rope((xk, YARD_Y - 0.045, 1.17), (xk, YARD_Y - 0.01, YARD_Z + 0.01), sag=0.0, thick=0.008, segs=3)

# Wanten und Stage
for side in (-1, 1):
    for dy in (-0.14, 0.0, 0.14):
        yy = MAST_Y + dy
        x_rail = side * (half_width(yy / L2) - 0.01)
        bar((side * 0.03, MAST_Y, 1.75), (x_rail, yy, z_gun(yy / L2)), 0.011, ROPE)
rope((0.0, MAST_Y - 0.03, 2.33), (0.0, -1.76, 0.50), sag=0.03, thick=0.012, segs=6)
rope((0.0, MAST_Y + 0.03, 2.33), (0.0, 1.20, 0.80), sag=0.02, thick=0.012, segs=6)

# Vorsegel (Artemon) am schräg nach vorn geneigten Mast
bar((0.0, -1.22, 0.30), (0.0, -1.78, 1.10), 0.028, WOOD)
bar((-0.27, -1.745, 1.0), (0.27, -1.745, 1.0), 0.022, WOOD)
sail_panel(-0.25, 0.25, 0.62, 0.99, -1.77, bulge=0.05, stripes=((0.44, 0.56, SAIL_RED),), nx=8, nz=4)
rope((0.0, -1.78, 1.12), (0.0, -1.80, 0.50), sag=0.0, thick=0.010, segs=2)

variant("Schiff")
export(spacing=6.0)

if PREVIEW:
    scene = bpy.context.scene
    cam = bpy.data.objects["Kamera"]
    base = os.path.splitext(PREVIEW)[0]
    ship = bpy.data.objects["Schiff"]
    ship.location.x = 0.0
    xs = [v.co.x for v in ship.data.vertices]
    ys = [v.co.y for v in ship.data.vertices]
    zs = [v.co.z for v in ship.data.vertices]
    print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")
    scene.render.resolution_x, scene.render.resolution_y = 1400, 900
    # Wasserfläche zur Orientierung
    bpy.ops.mesh.primitive_plane_add(size=14, location=(0, 0, -0.005))
    plane = bpy.context.active_object
    wm = mat("VorschauWasser", (0.18, 0.5, 0.55))
    plane.data.materials.append(wm)

    def shot(name, loc, target, lens=40):
        cam.location = loc
        cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
        cam.data.lens = lens
        scene.render.filepath = base + name + ".png"
        bpy.ops.render.render(write_still=True)

    shot("", (4.4, -3.2, 2.6), (0.0, 0.0, 0.9), lens=45)
    shot("_oben", (0.0, -3.0, 5.2), (0.0, 0.0, 0.4), lens=40)
    shot("_bug", (-2.2, -4.0, 1.4), (0.0, -0.8, 0.8), lens=40)
    shot("_heck", (2.6, 3.8, 1.8), (0.0, 0.8, 0.5), lens=40)
    shot("_spiel", (0.0, -5.0, 6.0), (0.0, 0.0, 0.3), lens=45)
