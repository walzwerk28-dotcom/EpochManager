"""
Holzfäller (Antike), 2 x 2 Felder: Holzhütte auf Steinsockel mit römischem Ziegeldach (Tegulae und Imbrices aus
Terrakotta, flache Neigung, Stirnziegel an der Traufe), Vordach auf Pfosten über der Tür, Brennholzstapel unter dem
Vordach, Stammstapel, Sägebock mit Säge, Hackklotz mit Axt und Scheiten, Baumstümpfe, Handkarren mit Stämmen.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/holzfaeller.py -- <blend> <fbx> [<vorschau.png>]

Maßstab 1 Einheit = 1 m, Boden bei z = 0, Mitte im Ursprung, alles innerhalb von 3,6 x 3,6 m (Grundfläche 4 m,
Rand für die Bodenplatte). Vorderseite (Tür, Straße) zeigt nach -Y (in Unity -z). Farben sind sRGB-Werte, wie sie im Spiel erscheinen;
mat() rechnet sie linear um. Am Ende wird alles zu einem Objekt mit einem Material je Farbe verschmolzen.
"""
import math
import random
import sys

import bpy

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
BLEND = args[0] if len(args) > 0 else None
FBX = args[1] if len(args) > 1 else None
PREVIEW = args[2] if len(args) > 2 else None

random.seed(7)  # gleiche Datei bei jedem Lauf
bpy.ops.wm.read_factory_settings(use_empty=True)

# ---------------------------------------------------------------- Materialien

MATERIALS = {}


def linear(c):
    """sRGB-Wert (0..1) in lineares Licht: Blender und Unity lesen Materialfarben aus der FBX-Datei linear."""
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def mat(name, rgb, rough=0.85, metal=0.0):
    """Material mit einer Farbe; rgb als sRGB-Wert, wie er im Spiel erscheinen soll."""
    m = bpy.data.materials.new(name)
    if m.node_tree is None:  # ältere Blender-Versionen legen die Knoten erst auf Anfrage an
        m.use_nodes = True
    color = tuple(linear(c) for c in rgb)
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = rough
        bsdf.inputs["Metallic"].default_value = metal
    m.diffuse_color = (*color, 1.0)
    MATERIALS[name] = m
    return m


STONE = mat("Sockelstein", (0.63, 0.60, 0.55))
STONE_DARK = mat("SockelsteinDunkel", (0.50, 0.47, 0.43))
WOOD = mat("Bohlen", (0.62, 0.45, 0.28))
WOOD_DARK = mat("HolzDunkel", (0.42, 0.29, 0.18))
WALL_CORE = mat("Wandkern", (0.50, 0.36, 0.23))
BARK = mat("Rinde", (0.36, 0.26, 0.17))
END_GRAIN = mat("Hirnholz", (0.84, 0.69, 0.46))
TILE = mat("Ziegel", (0.74, 0.37, 0.22))
TILE_DARK = mat("ZiegelDunkel", (0.62, 0.29, 0.17))
METAL = mat("Eisen", (0.42, 0.43, 0.45), rough=0.5, metal=0.6)
INTERIOR = mat("Innenraum", (0.12, 0.09, 0.07))

PARTS = []


def keep(obj, material):
    obj.data.materials.clear()
    obj.data.materials.append(material)
    PARTS.append(obj)
    return obj


def box(size, loc, rot=(0.0, 0.0, 0.0), material=WOOD):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=[math.radians(a) for a in rot])
    obj = bpy.context.active_object
    obj.scale = size
    return keep(obj, material)


def cyl(radius, depth, loc, rot=(0.0, 0.0, 0.0), material=BARK, verts=10):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc,
                                        rotation=[math.radians(a) for a in rot])
    return keep(bpy.context.active_object, material)


def prism(points, depth, x0, material):
    """Dreieck (y, z) in der YZ-Ebene, in x von x0 bis x0 + depth gezogen (Giebelfelder)."""
    mesh = bpy.data.meshes.new("Giebel")
    verts = [(x0, y, z) for y, z in points] + [(x0 + depth, y, z) for y, z in points]
    faces = [(0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Giebel", mesh)
    bpy.context.collection.objects.link(obj)
    return keep(obj, material)


def log(length, radius, loc, rot, caps=True, verts=10):
    """Stamm mit Rinde, an beiden Enden helles Hirnholz (Achse nach der Drehung rot)."""
    cyl(radius, length, loc, rot, BARK, verts)
    if not caps:
        return
    # Endflächen: dünne Scheiben knapp vor den Enden, entlang derselben Achse versetzt
    rx, ry, rz = (math.radians(a) for a in rot)
    from mathutils import Euler, Vector
    axis = Euler((rx, ry, rz)).to_matrix() @ Vector((0.0, 0.0, 1.0))
    for sign in (-1.0, 1.0):
        center = Vector(loc) + axis * sign * (length * 0.5 + 0.004)
        cyl(radius * 0.92, 0.01, tuple(center), rot, END_GRAIN, verts)


# ---------------------------------------------------------------- Hütte

HX0, HX1 = -1.50, 0.50      # Hütte in x
HY0, HY1 = -0.05, 1.42      # Hütte in y (Vorderwand bei HY0)
PLINTH = 0.30               # Steinsockel
WALL_TOP = 1.80
HCX, HCY = (HX0 + HX1) / 2, (HY0 + HY1) / 2
W, D = HX1 - HX0, HY1 - HY0

# Sockel aus Bruchsteinen: Grundkörper und eine Reihe unregelmäßiger Steine davor
box((W + 0.08, D + 0.08, PLINTH), (HCX, HCY, PLINTH / 2), material=STONE_DARK)
for side in range(4):
    length = W if side < 2 else D
    x = HX0
    pos = 0.0
    while pos < length - 0.05:
        s = random.uniform(0.18, 0.32)
        s = min(s, length - pos)
        h = random.uniform(0.11, 0.15)
        for row, z in enumerate((0.075, 0.225)):
            off = (s * 0.5 if row else 0.0)
            p = pos + off
            if p + s * 0.5 > length:
                continue
            c = p + s * 0.5
            if side == 0:
                loc = (HX0 + c, HY0 - 0.045, z)
            elif side == 1:
                loc = (HX0 + c, HY1 + 0.045, z)
            elif side == 2:
                loc = (HX0 - 0.045, HY0 + c, z)
            else:
                loc = (HX1 + 0.045, HY0 + c, z)
            size = (s - 0.02, 0.04, h) if side < 2 else (0.04, s - 0.02, h)
            box(size, loc, material=STONE)
        pos += s

# Wandkern (dunkel, damit Fugen zwischen den Bohlen nicht durchsichtig sind)
box((W - 0.06, D - 0.06, WALL_TOP - PLINTH), (HCX, HCY, (PLINTH + WALL_TOP) / 2), material=WALL_CORE)

DOOR_X0, DOOR_X1, DOOR_TOP = -1.05, -0.45, 1.50
WIN_Y0, WIN_Y1, WIN_Z0, WIN_Z1 = 0.70, 1.10, 1.05, 1.40

# Senkrechte Bohlen ringsum, mit leicht wechselnder Breite und Tiefe
plank_h = WALL_TOP - PLINTH
for side in range(4):
    length = W if side < 2 else D
    pos = 0.0
    while pos < length - 0.02:
        w = min(random.uniform(0.15, 0.19), length - pos)
        c = pos + w * 0.5
        depth = random.uniform(0.035, 0.05)
        mat_ = WOOD if random.random() > 0.25 else WOOD_DARK
        if side == 0:
            x = HX0 + c
            if DOOR_X0 - 0.02 < x < DOOR_X1 + 0.02:   # Türöffnung: nur über der Tür
                box((w - 0.012, depth, WALL_TOP - DOOR_TOP), (x, HY0 - depth / 2, (DOOR_TOP + WALL_TOP) / 2), material=mat_)
            else:
                box((w - 0.012, depth, plank_h), (x, HY0 - depth / 2, PLINTH + plank_h / 2), material=mat_)
        elif side == 1:
            box((w - 0.012, depth, plank_h), (HX0 + c, HY1 + depth / 2, PLINTH + plank_h / 2), material=mat_)
        elif side == 2:
            box((depth, w - 0.012, plank_h), (HX0 - depth / 2, HY0 + c, PLINTH + plank_h / 2), material=mat_)
        else:
            y = HY0 + c
            if WIN_Y0 - 0.02 < y < WIN_Y1 + 0.02:     # Fensteröffnung: unter und über dem Fenster
                box((depth, w - 0.012, WIN_Z0 - PLINTH), (HX1 + depth / 2, y, (PLINTH + WIN_Z0) / 2), material=mat_)
                box((depth, w - 0.012, WALL_TOP - WIN_Z1), (HX1 + depth / 2, y, (WIN_Z1 + WALL_TOP) / 2), material=mat_)
            else:
                box((depth, w - 0.012, plank_h), (HX1 + depth / 2, y, PLINTH + plank_h / 2), material=mat_)
        pos += w

# Eckpfosten, Schwelle und Rähm (Balken oben ringsum)
for x in (HX0, HX1):
    for y in (HY0, HY1):
        box((0.13, 0.13, WALL_TOP - PLINTH + 0.06), (x, y, (PLINTH + WALL_TOP) / 2 + 0.03), material=WOOD_DARK)
box((W + 0.16, 0.12, 0.12), (HCX, HY0 - 0.02, WALL_TOP + 0.04), material=WOOD_DARK)
box((W + 0.16, 0.12, 0.12), (HCX, HY1 + 0.02, WALL_TOP + 0.04), material=WOOD_DARK)
box((0.12, D + 0.16, 0.12), (HX0 - 0.02, HCY, WALL_TOP + 0.04), material=WOOD_DARK)
box((0.12, D + 0.16, 0.12), (HX1 + 0.02, HCY, WALL_TOP + 0.04), material=WOOD_DARK)

# Tür: dunkle Öffnung, Sturz, halb geöffnetes Türblatt aus Bohlen mit Eisenbändern
box((DOOR_X1 - DOOR_X0, 0.02, DOOR_TOP - PLINTH), ((DOOR_X0 + DOOR_X1) / 2, HY0 - 0.005, (PLINTH + DOOR_TOP) / 2), material=INTERIOR)
box((DOOR_X1 - DOOR_X0 + 0.2, 0.1, 0.1), ((DOOR_X0 + DOOR_X1) / 2, HY0 - 0.06, DOOR_TOP + 0.05), material=WOOD_DARK)
door_w = DOOR_X1 - DOOR_X0 - 0.04
hinge = (DOOR_X0 + 0.02, HY0 - 0.04)
angle = math.radians(65)
dx, dy = math.cos(angle) * door_w / 2, -math.sin(angle) * door_w / 2
box((door_w, 0.05, DOOR_TOP - PLINTH - 0.04), (hinge[0] + dx, hinge[1] + dy, (PLINTH + DOOR_TOP) / 2), rot=(0, 0, -65), material=WOOD)
for z in (0.55, 1.25):
    box((door_w * 0.9, 0.06, 0.04), (hinge[0] + dx * 0.95, hinge[1] + dy * 0.95 - 0.005, z), rot=(0, 0, -65), material=METAL)
# Schwelle aus Stein vor der Tür
box((DOOR_X1 - DOOR_X0 + 0.25, 0.3, 0.06), ((DOOR_X0 + DOOR_X1) / 2, HY0 - 0.18, PLINTH - 0.03), material=STONE)

# Fenster in der rechten Wand: Öffnung, Rahmen, zwei offene Läden
wy, wz = (WIN_Y0 + WIN_Y1) / 2, (WIN_Z0 + WIN_Z1) / 2
box((0.02, WIN_Y1 - WIN_Y0, WIN_Z1 - WIN_Z0), (HX1 + 0.005, wy, wz), material=INTERIOR)
box((0.07, WIN_Y1 - WIN_Y0 + 0.1, 0.06), (HX1 + 0.035, wy, WIN_Z0 - 0.03), material=WOOD_DARK)
box((0.07, WIN_Y1 - WIN_Y0 + 0.1, 0.06), (HX1 + 0.035, wy, WIN_Z1 + 0.03), material=WOOD_DARK)
# Läden: Scharnier an der Fensterkante auf der Wandfläche, fast flach an die Wand geklappt (20° Abstand zur Wand)
shutter_w = (WIN_Y1 - WIN_Y0) / 2
open_a = math.radians(20)
for sign in (-1, 1):
    hinge_y = WIN_Y1 if sign > 0 else WIN_Y0
    hx, hy = HX1 + 0.07, hinge_y
    out_x, out_y = math.sin(open_a), sign * math.cos(open_a)     # Richtung vom Scharnier zur äußeren Kante
    cx, cy = hx + out_x * shutter_w / 2, hy + out_y * shutter_w / 2
    box((0.03, shutter_w, WIN_Z1 - WIN_Z0), (cx, cy, wz), rot=(0, 0, -sign * 20), material=WOOD)
    nx, ny = math.cos(open_a), -sign * math.sin(open_a)          # Außenseite des Ladens
    for z in (WIN_Z0 + 0.07, WIN_Z1 - 0.07):    # Querleisten
        box((0.015, shutter_w * 0.9, 0.04), (cx + nx * 0.02, cy + ny * 0.02, z), rot=(0, 0, -sign * 20), material=WOOD_DARK)

# ---------------------------------------------------------------- Römisches Ziegeldach

PITCH = math.radians(22)
OVER_SIDE = 0.18            # Überstand an den Giebeln
OVER_BACK = 0.22            # Überstand hinten
PORCH = 0.65                # Vordach vorn über der Tür
T = 0.05                    # Dicke der Tegulae
RIDGE_Z = WALL_TOP + 0.10 + (D / 2) * math.tan(PITCH)
roof_x0, roof_x1 = HX0 - OVER_SIDE, HX1 + OVER_SIDE
roof_w = roof_x1 - roof_x0
roof_cx = (roof_x0 + roof_x1) / 2

# Giebelfelder aus Bohlen über dem Rähm
gable = [(HY0, WALL_TOP + 0.1), (HY1, WALL_TOP + 0.1), (HCY, RIDGE_Z - 0.02)]
prism(gable, 0.05, HX0 - 0.05, WOOD_DARK)
prism(gable, 0.05, HX1, WOOD_DARK)


def slope(front):
    """Eine Dachfläche: Tegulae als Platte mit Stoßfugen, darauf Imbrices als Halbrohre, Stirnziegel an der Traufe."""
    run = D / 2 + (PORCH if front else OVER_BACK)
    length = run / math.cos(PITCH)
    s = -1 if front else 1
    rot_x = math.degrees(PITCH) * (1 if front else -1)
    cy = HCY + s * (run / 2)
    cz = RIDGE_Z - (run / 2) * math.tan(PITCH)
    box((roof_w, length, T), (roof_cx, cy, cz), rot=(rot_x, 0, 0), material=TILE)
    # Querfugen der Tegulae: flache dunkle Leisten alle 0,42 m
    normal = (0.0, s * math.sin(PITCH), math.cos(PITCH))
    n = int(length / 0.42)
    for k in range(1, n + 1):
        d = k * 0.42 - 0.02
        if d >= length:
            break
        py = HCY + s * d * math.cos(PITCH)
        pz = RIDGE_Z - d * math.sin(PITCH)
        box((roof_w, 0.02, 0.012), (roof_cx, py + normal[1] * T / 2, pz + normal[2] * T / 2), rot=(rot_x, 0, 0), material=TILE_DARK)
    # Imbrices: halb in die Tegulae gesenkte Zylinder entlang der Neigung
    count = int(roof_w / 0.24)
    spacing = roof_w / count
    for i in range(count + 1):
        x = roof_x0 + i * spacing
        rx = 90 + math.degrees(PITCH) if front else -(90 + math.degrees(PITCH))
        mid = (roof_cx * 0 + x, cy + normal[1] * (T / 2 + 0.015), cz + normal[2] * (T / 2 + 0.015))
        cyl(0.055, length, mid, rot=(rx, 0, 0), material=TILE if random.random() > 0.3 else TILE_DARK, verts=8)
        # Stirnziegel (Antefix) am unteren Ende
        ey = HCY + s * (run + 0.01)
        ez = RIDGE_Z - run * math.tan(PITCH)
        box((0.11, 0.025, 0.12), (x, ey, ez + 0.04), rot=(rot_x * 0.3, 0, 0), material=TILE_DARK)
    # Traufbrett
    box((roof_w, 0.04, 0.10), (roof_cx, HCY + s * (run + 0.02), RIDGE_Z - run * math.tan(PITCH) - 0.04), material=WOOD_DARK)


slope(front=True)
slope(front=False)
# Firstziegel entlang des Firsts
cyl(0.085, roof_w + 0.04, (roof_cx, HCY, RIDGE_Z + 0.04), rot=(0, 90, 0), material=TILE_DARK, verts=10)
# Ortgang: Bretter an den Giebelseiten unter den Ziegeln
for x in (roof_x0, roof_x1):
    for s in (-1, 1):
        run = D / 2 + (PORCH if s < 0 else OVER_BACK)
        length = run / math.cos(PITCH)
        box((0.04, length, 0.12), (x, HCY + s * run / 2, RIDGE_Z - (run / 2) * math.tan(PITCH) - 0.05),
            rot=(math.degrees(PITCH) * (-s), 0, 0), material=WOOD_DARK)

# Vordach: zwei Pfosten auf Steinen und ein Querbalken unter der Traufe
porch_y = HY0 - PORCH + 0.12
porch_top = RIDGE_Z - (HCY - porch_y) * math.tan(PITCH) - T
for x in (HX0 + 0.1, HX1 - 0.1):
    box((0.18, 0.18, 0.08), (x, porch_y, 0.04), material=STONE)
    box((0.11, 0.11, porch_top - 0.08), (x, porch_y, 0.08 + (porch_top - 0.08) / 2), material=WOOD_DARK)
box((W + 0.1, 0.12, 0.12), (HCX, porch_y, porch_top - 0.06), material=WOOD_DARK)

# ---------------------------------------------------------------- Hof

# Brennholzstapel unter dem Vordach rechts der Tür (Scheite mit der Stirnseite nach vorn)
for row in range(5):
    for col in range(5):
        x = -0.30 + col * 0.13 + (0.06 if row % 2 else 0.0)
        if x > HX1 - 0.12:
            continue
        spin = random.uniform(0, 60)   # Drehung um die eigene Längsachse (nach X 90 ist das die Welt-Y-Achse)
        cyl(0.06, 0.38, (x, HY0 - 0.24, 0.07 + row * 0.115), rot=(90, spin, 0), material=BARK, verts=6)
        cyl(0.055, 0.01, (x, HY0 - 0.435, 0.07 + row * 0.115), rot=(90, spin, 0), material=END_GRAIN, verts=6)

# Stammstapel rechts: 4-3-2, Achse entlang y, mit Haltepfosten
LOG_R, LOG_L = 0.13, 1.35
pile_y = 0.95
for row, n in enumerate((4, 3, 2)):
    for k in range(n):
        x = 0.82 + k * (2 * LOG_R + 0.01) + row * (LOG_R + 0.005)
        z = LOG_R + row * (LOG_R * 1.75)
        log(LOG_L + random.uniform(-0.08, 0.08), LOG_R * random.uniform(0.92, 1.05), (x, pile_y + random.uniform(-0.05, 0.05), z), (90, 0, 0))
for x in (0.68, 1.76):
    for y in (pile_y - 0.45, pile_y + 0.45):
        box((0.07, 0.07, 0.55), (x, y, 0.275), material=WOOD_DARK)

# Sägebock vorn rechts: zwei X-Gestelle, Stamm darauf, Säge im Schnitt
SAW_X, SAW_Y = 1.15, -0.85
for y in (SAW_Y - 0.32, SAW_Y + 0.32):
    for a in (-28, 28):
        box((0.06, 0.06, 0.75), (SAW_X, y, 0.33), rot=(0, a, 0), material=WOOD_DARK)
box((0.06, 0.75, 0.06), (SAW_X, SAW_Y, 0.25), material=WOOD_DARK)
log(1.15, 0.11, (SAW_X, SAW_Y, 0.66), (90, 0, 0))
box((0.012, 0.03, 0.42), (SAW_X, SAW_Y + 0.1, 0.78), rot=(0, 0, 0), material=METAL)      # Sägeblatt
box((0.04, 0.32, 0.04), (SAW_X, SAW_Y + 0.1, 1.0), material=WOOD_DARK)                  # Bügel oben
cyl(0.02, 0.18, (SAW_X, SAW_Y + 0.28, 0.98), rot=(0, 90, 0), material=WOOD, verts=6)   # Griff

# Hackklotz mit Axt und gespaltenen Scheiten
BLOCK = (-0.15, -1.15)
cyl(0.24, 0.42, (BLOCK[0], BLOCK[1], 0.21), material=BARK, verts=12)
cyl(0.22, 0.01, (BLOCK[0], BLOCK[1], 0.425), material=END_GRAIN, verts=12)
# Axt eingeschlagen: Stiel 30° über der Waagerechten, Kopf quer dazu, Schneide nach unten 6 cm im Holz
AXE_A = 60                                        # Drehung um Y: Stielachse u, Schneidenrichtung v
a_ = math.radians(AXE_A)
u = (math.sin(a_), math.cos(a_))                  # (x, z) vom Auge zum Griffende
v = (math.cos(a_), -math.sin(a_))                 # (x, z) vom Auge zur Schneide
eye = (BLOCK[0] - 0.05, 0.48)
ay = BLOCK[1] - 0.05
cyl(0.022, 0.64, (eye[0] + u[0] * 0.28, ay, eye[1] + u[1] * 0.28), rot=(0, AXE_A, 0), material=WOOD, verts=6)   # Stiel
box((0.17, 0.03, 0.08), (eye[0] + v[0] * 0.055, ay, eye[1] + v[1] * 0.055), rot=(0, AXE_A, 0), material=METAL)  # Kopf mit Nacken
box((0.05, 0.014, 0.12), (eye[0] + v[0] * 0.12, ay, eye[1] + v[1] * 0.12), rot=(0, AXE_A, 0), material=METAL)   # breite Schneide
for k in range(6):
    a = random.uniform(0, 2 * math.pi)
    r = random.uniform(0.38, 0.55)
    x, y = BLOCK[0] + math.cos(a) * r, BLOCK[1] + math.sin(a) * r
    box((0.28, 0.08, 0.07), (x, y, 0.035), rot=(0, 0, random.uniform(0, 180)), material=END_GRAIN if k % 2 else WOOD)
# Späne
for k in range(10):
    a = random.uniform(0, 2 * math.pi)
    r = random.uniform(0.3, 0.6)
    box((0.06, 0.03, 0.01), (BLOCK[0] + math.cos(a) * r, BLOCK[1] + math.sin(a) * r, 0.005),
        rot=(0, 0, random.uniform(0, 180)), material=END_GRAIN)

# Baumstümpfe
for x, y, r, h in ((1.5, -1.42, 0.16, 0.18), (0.5, -1.5, 0.15, 0.2)):
    cyl(r, h, (x, y, h / 2), material=BARK, verts=10)
    cyl(r * 0.9, 0.01, (x, y, h + 0.005), material=END_GRAIN, verts=10)
    for k in range(3):   # Wurzelansätze
        a = random.uniform(0, 2 * math.pi)
        box((0.18, 0.07, 0.06), (x + math.cos(a) * r, y + math.sin(a) * r, 0.03), rot=(0, 0, math.degrees(a)), material=BARK)

# Handkarren vorn links mit zwei kurzen Stämmen
CART_X, CART_Y, CART_A = -1.2, -1.2, 70
from mathutils import Matrix, Vector


def cart_part(size, local, material, rot=(0.0, 0.0, 0.0), kind="box", radius=0.0, depth=0.0):
    m = Matrix.Rotation(math.radians(CART_A), 4, "Z")
    p = m @ Vector(local) + Vector((CART_X, CART_Y, 0.0))
    r = (rot[0], rot[1], rot[2] + CART_A)
    if kind == "box":
        return box(size, tuple(p), r, material)
    return cyl(radius, depth, tuple(p), r, material, verts=12)


cart_part((0.55, 0.85, 0.05), (0.0, 0.0, 0.36), WOOD)
for sx in (-1, 1):
    cart_part((0.04, 0.85, 0.16), (sx * 0.275, 0.0, 0.46), WOOD_DARK)
cart_part((0.55, 0.04, 0.16), (0.0, 0.425, 0.46), WOOD_DARK)
for sx in (-1, 1):
    cart_part(None, (sx * 0.34, 0.1, 0.24), WOOD_DARK, rot=(0, 90, 0), kind="cyl", radius=0.24, depth=0.06)
    cart_part(None, (sx * 0.36, 0.1, 0.24), METAL, rot=(0, 90, 0), kind="cyl", radius=0.06, depth=0.08)
    cart_part((0.04, 0.6, 0.04), (sx * 0.22, -0.62, 0.42), WOOD_DARK, rot=(-12, 0, 0))
cart_part(None, (0.0, 0.1, 0.24), WOOD_DARK, rot=(0, 90, 0), kind="cyl", radius=0.025, depth=0.7)
for k in range(2):
    m = Matrix.Rotation(math.radians(CART_A), 4, "Z")
    p = m @ Vector((-0.12 + k * 0.24, 0.0, 0.48)) + Vector((CART_X, CART_Y, 0.0))
    log(0.8, 0.1, tuple(p), (90, 0, CART_A))

# Ecksteine an den Grenzen (wie bei den anderen Modellen: gleiche Ausdehnung, nicht zu sehen)
for x in (-1.8, 1.8):
    for y in (-1.8, 1.8):
        box((0.04, 0.04, 0.02), (x, y, 0.01), material=STONE_DARK)

# ---------------------------------------------------------------- Zusammenfügen und Export

bpy.ops.object.select_all(action="DESELECT")
for obj in PARTS:
    obj.select_set(True)
bpy.context.view_layer.objects.active = PARTS[0]
bpy.ops.object.join()
model = bpy.context.active_object
model.name = "Holzfaeller"
model.data.name = "Holzfaeller"
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Gleiche Materialien zusammenlegen (join übernimmt die Slots aller Teile)
bpy.ops.object.material_slot_remove_unused()

dims = model.dimensions
tris = sum(len(p.vertices) - 2 for p in model.data.polygons)
print(f"Holzfaeller: {dims.x:.2f} x {dims.y:.2f} x {dims.z:.2f} m, {tris} Dreiecke, {len(model.material_slots)} Materialien")

if BLEND:
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
if FBX:
    bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                             mesh_smooth_type="FACE", add_leaf_bones=False, use_mesh_modifiers=True)

if PREVIEW:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.light = "STUDIO"
    scene.render.resolution_x, scene.render.resolution_y = 1200, 900
    cam_data = bpy.data.cameras.new("Kamera")
    cam = bpy.data.objects.new("Kamera", cam_data)
    scene.collection.objects.link(cam)
    cam.location = (4.6, -6.4, 4.4)
    direction = Vector((0.0, 0.0, 0.7)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = 42
    scene.camera = cam
    scene.render.filepath = PREVIEW
    bpy.ops.render.render(write_still=True)
