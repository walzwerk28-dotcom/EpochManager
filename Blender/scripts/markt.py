"""
Markt (Antike), 2 x 4 Felder (4 m Front zur Straße, 8 m tief): römischer Marktplatz. Vorn zwei Torpfeiler, links ein
Eichtisch (Mensa Ponderaria) mit den Mulden der amtlichen Hohlmaße und ein Gestell mit Amphoren, rechts ein Handkarren
mit Säcken; in der Mitte ein achteckiges Brunnenbecken mit Säule und Schale, links und rechts je drei Holzstände mit
gestreiften Stoffdächern, die Theken zur Gasse in der Mitte (Getreide, Brot, Käse, Töpferware, Äpfel, Oliven,
Stoffballen); hinten eine Verkaufshalle mit Pultdach aus Terrakotta-Ziegeln auf vier Säulen, Theke und Wandbord mit
Krügen, Amphoren an den Enden.

Maßstab: Gebäude und Dinge passen zu den Häusern im Spiel (ein Stockwerk etwa 1 m, Wohnhaus 1,57 m hoch). Stände,
Waren, Karren und Tisch sind daher etwa 55 % so groß wie in Wirklichkeit, Brunnen und Halle etwa 70 %.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/markt.py -- <blend> <fbx> [<vorschau.png>]

1 Einheit = 1 m, Boden bei z = 0, Mitte im Ursprung, alles innerhalb von 3,64 x 7,32 m (Grundfläche 4 x 8 m,
Rand für die Bodenplatte). Vorderseite (Straße) zeigt nach -Y (in Unity -z). Farben sind sRGB-Werte, wie sie im Spiel
erscheinen; mat() rechnet sie linear um. Am Ende wird alles zu einem Objekt mit einem Material je Farbe verschmolzen.
"""
import math
import random
import sys

import bpy
from mathutils import Matrix, Vector

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
BLEND = args[0] if len(args) > 0 else None
FBX = args[1] if len(args) > 1 else None
PREVIEW = args[2] if len(args) > 2 else None

random.seed(11)  # gleiche Datei bei jedem Lauf
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


LIME = mat("Kalkstein", (0.86, 0.82, 0.74))
LIME_DARK = mat("KalksteinDunkel", (0.70, 0.66, 0.58))
SHADOW = mat("SteinSchatten", (0.36, 0.34, 0.31))
WOOD = mat("Holz", (0.62, 0.45, 0.28))
WOOD_DARK = mat("HolzDunkel", (0.42, 0.29, 0.18))
TILE = mat("Ziegel", (0.74, 0.37, 0.22))
TILE_DARK = mat("ZiegelDunkel", (0.62, 0.29, 0.17))
WATER = mat("Wasser", (0.42, 0.70, 0.85), rough=0.2)
RED = mat("Rot", (0.72, 0.22, 0.16))
OCHRE = mat("Ocker", (0.88, 0.70, 0.36))
GREEN = mat("Gruen", (0.36, 0.52, 0.30))
BLUE = mat("Blau", (0.30, 0.42, 0.62))
CREAM = mat("Creme", (0.90, 0.86, 0.74))
OLIVE = mat("Olive", (0.30, 0.33, 0.18))
TERRACOTTA = mat("Terrakotta", (0.76, 0.46, 0.30))
BASKET = mat("Korb", (0.66, 0.52, 0.30))
SACK = mat("Sack", (0.80, 0.72, 0.56))

S = 0.55   # Maßstab der Dinge, an denen man Menschen misst (Stände, Waren, Karren, Tisch)

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


def cyl(radius, depth, loc, rot=(0.0, 0.0, 0.0), material=LIME, verts=10):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc,
                                        rotation=[math.radians(a) for a in rot])
    return keep(bpy.context.active_object, material)


def cone(r_bottom, r_top, depth, loc, material, verts=10):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r_bottom, radius2=r_top, depth=depth, location=loc)
    return keep(bpy.context.active_object, material)


def sphere(radius, loc, scale=(1.0, 1.0, 1.0), material=RED, segs=8, rings=5):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings, radius=radius, location=loc)
    obj = bpy.context.active_object
    obj.scale = scale
    return keep(obj, material)


def amphora(x, y, z0):
    """Spitzamphore nach Dressel 1 (in Wirklichkeit 0,55 m): ein Rotationskörper aus Fuß, schlankem Bauch, Schulter, langem
    Hals und Lippenwulst, dazu zwei gebogene Bandhenkel, die unter der Lippe am Hals ansetzen und auf der Schulter enden."""
    segs = 10
    # Profil (Radius, Höhe) in Metern der echten Amphore; die Maße werden danach mit S verkleinert
    profile = ((0.012, 0.000), (0.026, 0.080), (0.070, 0.190), (0.092, 0.300), (0.074, 0.395),
               (0.032, 0.450), (0.030, 0.505), (0.047, 0.520), (0.047, 0.550))
    verts, faces = [], []
    for r, h in profile:
        for j in range(segs):
            a = 2.0 * math.pi * j / segs
            verts.append((x + r * S * math.cos(a), y + r * S * math.sin(a), z0 + h * S))
    for i in range(len(profile) - 1):
        for j in range(segs):
            j2 = (j + 1) % segs
            faces.append((i * segs + j, i * segs + j2, (i + 1) * segs + j2, (i + 1) * segs + j))
    top = (len(profile) - 1) * segs
    faces.append(tuple(range(top, top + segs)))              # Mündung (oben)
    faces.append(tuple(reversed(range(segs))))               # Fußknopf (unten)
    mesh = bpy.data.meshes.new("Amphore")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    mesh.materials.append(TERRACOTTA)
    obj = bpy.data.objects.new("Amphore", mesh)
    bpy.context.collection.objects.link(obj)
    PARTS.append(obj)
    # Henkel: flaches Band entlang einer Kurve (Abstand von der Achse, Höhe), links und rechts
    path = ((0.025, 0.500), (0.056, 0.508), (0.074, 0.470), (0.076, 0.425), (0.068, 0.388))
    width, thick = 0.020, 0.011
    last = len(path) - 1
    for s in (-1, 1):
        hv = []
        for k, (r, h) in enumerate(path):
            p0, p1 = path[max(k - 1, 0)], path[min(k + 1, last)]
            tx, tz = p1[0] - p0[0], p1[1] - p0[1]
            n = math.hypot(tx, tz)
            nx, nz = -tz / n, tx / n                           # Normale in der Ebene des Henkels
            for dy, dn in ((-width / 2, -thick / 2), (width / 2, -thick / 2), (width / 2, thick / 2), (-width / 2, thick / 2)):
                hv.append((x + s * (r + nx * dn) * S, y + dy * S, z0 + (h + nz * dn) * S))

        def ring_mean(a_, b_):
            pts = [Vector(hv[i]) for i in range(a_ * 4, b_ * 4 + 4)]
            return sum(pts, Vector()) / len(pts)

        faces_h = []                                           # (Fläche, Punkt innerhalb des Bandes)
        for k in range(last):
            mid = ring_mean(k, k + 1)
            for e in range(4):
                e2 = (e + 1) % 4
                faces_h.append(([k * 4 + e, k * 4 + e2, (k + 1) * 4 + e2, (k + 1) * 4 + e], mid))
        faces_h.append(([3, 2, 1, 0], ring_mean(1, 1)))
        faces_h.append(([last * 4 + e for e in range(4)], ring_mean(last - 1, last - 1)))
        fixed = []
        for f, mid in faces_h:                                 # Flächen nach außen drehen
            pts = [Vector(hv[i]) for i in f]
            c = sum(pts, Vector()) / len(pts)
            nrm = (pts[1] - pts[0]).cross(pts[2] - pts[1])
            fixed.append(f if nrm.dot(c - mid) >= 0 else list(reversed(f)))
        hmesh = bpy.data.meshes.new("AmphorenHenkel")
        hmesh.from_pydata(hv, [], fixed)
        hmesh.update()
        hmesh.materials.append(TERRACOTTA)
        hobj = bpy.data.objects.new("AmphorenHenkel", hmesh)
        bpy.context.collection.objects.link(hobj)
        PARTS.append(hobj)


def jug(x, y, z0, r, material=TERRACOTTA):
    """Bauchiger Krug mit Hals (r = Radius des Bauchs)."""
    sphere(r, (x, y, z0 + r * 0.9), (1.0, 1.0, 1.05), material, segs=10, rings=6)
    cyl(r * 0.45, r * 0.7, (x, y, z0 + r * 1.95), material=material, verts=8)
    cyl(r * 0.58, r * 0.18, (x, y, z0 + r * 2.3), material=material, verts=8)


def basket(x, y, z0, r, fill):
    """Flacher Korb mit gewölbtem Inhalt."""
    cyl(r, r * 0.9, (x, y, z0 + r * 0.45), material=BASKET, verts=10)
    sphere(r * 0.92, (x, y, z0 + r * 0.9), (1.0, 1.0, 0.35), fill, segs=10, rings=5)


def sack(loc, r):
    """Gefüllter Sack: bauchig, oben zusammengeschnürt mit Zipfel."""
    x, y, z0 = loc
    sphere(r, (x, y, z0 + r), (1.0, 0.85, 1.15), SACK, segs=8, rings=5)
    cyl(r * 0.28, r * 0.27, (x, y, z0 + r * 2.2), material=WOOD_DARK, verts=6)     # Schnur
    cone(r * 0.32, r * 0.12, r * 0.45, (x, y, z0 + r * 2.4), SACK, verts=6)


def slab_yz(points, x0, depth, material):
    """Vieleck (y, z) in der YZ-Ebene, in x von x0 bis x0 + depth gezogen (Wandstücke unter der Dachschräge, Zwickel)."""
    import bmesh
    n = len(points)
    mesh = bpy.data.meshes.new("Wand")
    verts = [(x0, y, z) for y, z in points] + [(x0 + depth, y, z) for y, z in points]
    faces = [tuple(range(n))[::-1], tuple(range(n, 2 * n))] + [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    mesh.from_pydata(verts, [], faces)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Wand", mesh)
    bpy.context.collection.objects.link(obj)
    return keep(obj, material)


def placed(origin, angle):
    """Teile einer Gruppe in eigenen Koordinaten setzen (um z gedreht und verschoben)."""
    m = Matrix.Rotation(math.radians(angle), 4, "Z")

    def at(local):
        return tuple(m @ Vector(local) + Vector((origin[0], origin[1], 0.0)))
    return at


# ---------------------------------------------------------------- Torpfeiler an der Straße

FRONT_Y = -3.45
for x in (-0.62, 0.62):
    box((0.26, 0.26, 0.06), (x, FRONT_Y, 0.03), material=LIME_DARK)
    box((0.18, 0.18, 1.00), (x, FRONT_Y, 0.56), material=LIME)
    box((0.24, 0.24, 0.06), (x, FRONT_Y, 1.09), material=LIME_DARK)
    cone(0.07, 0.0, 0.10, (x, FRONT_Y, 1.17), LIME, verts=4)

# ---------------------------------------------------------------- Brunnen in der Mitte

FX, FY = 0.0, -0.60
APOTHEM, RIM_T, RIM_H = 0.36, 0.07, 0.28
cyl(0.47, 0.04, (FX, FY, 0.02), material=LIME_DARK, verts=8)                   # Stufe
cyl(APOTHEM / math.cos(math.radians(22.5)), 0.20, (FX, FY, 0.10), material=SHADOW, verts=8)   # Beckenboden
side = 2 * APOTHEM * math.tan(math.radians(22.5)) + 0.015
for k in range(8):
    a = math.radians(22.5 + k * 45)
    x, y = FX + math.cos(a) * APOTHEM, FY + math.sin(a) * APOTHEM
    box((RIM_T, side, RIM_H), (x, y, RIM_H / 2), rot=(0, 0, math.degrees(a)), material=LIME_DARK)
    box((RIM_T + 0.04, side + 0.03, 0.03), (x, y, RIM_H + 0.015), rot=(0, 0, math.degrees(a)), material=LIME)
inner = APOTHEM - RIM_T / 2
cyl(inner / math.cos(math.radians(22.5)) + 0.01, 0.01, (FX, FY, 0.23), material=WATER, verts=8)
# Säule mit Schale, aus der an vier Seiten Wasser ins Becken fällt
cyl(0.08, 0.07, (FX, FY, 0.265), material=LIME_DARK, verts=10)
cyl(0.045, 0.44, (FX, FY, 0.52), material=LIME, verts=10)
cone(0.05, 0.15, 0.065, (FX, FY, 0.77), LIME, verts=12)
cyl(0.13, 0.01, (FX, FY, 0.80), material=WATER, verts=12)
for k in range(4):
    a = math.radians(45 + k * 90)
    x, y = FX + math.cos(a) * 0.155, FY + math.sin(a) * 0.155
    box((0.008, 0.014, 0.56), (x, y, 0.51), rot=(0, 0, math.degrees(a)), material=WATER)

# ---------------------------------------------------------------- Verkaufshalle hinten mit Pultdach

PITCH = math.radians(20)
T = 0.04
EAVE_Y, BACK_Y, EAVE_Z = 1.62, 3.64, 1.34      # Dachmitte an der vorderen Traufe
ROOF_X0, ROOF_X1 = -1.77, 1.77
COL_Y = 1.74
WALL_Y = 3.50


def roof_z(y):
    return EAVE_Z + (y - EAVE_Y) * math.tan(PITCH)


# Rückwand aus Kalkstein bis unter das Dach, mit dunkler Sockelleiste und roter Sockelzone wie in Pompeji
wall_h = roof_z(WALL_Y) - 0.03
box((3.50, 0.10, wall_h), (0.0, WALL_Y, wall_h / 2), material=LIME)
box((3.52, 0.12, 0.08), (0.0, WALL_Y, 0.04), material=LIME_DARK)
box((3.46, 0.01, 0.50), (0.0, WALL_Y - 0.055, 0.33), material=RED)
box((3.46, 0.012, 0.035), (0.0, WALL_Y - 0.056, 0.60), material=OCHRE)

# Außenseite der Rückwand wie Opus mixtum: Ziegelbänder im Kalkstein, Pilaster hinter den Säulen, Gesims unter der
# Traufe, in jedem Feld ein Gitterfenster, in der Mitte eine Hintertür für Lieferungen
BANDS = (0.35, 0.70, 1.05, 1.40, 1.75)
BACK_FACE = WALL_Y + 0.05
for z in BANDS:
    box((3.50, 0.012, 0.05), (0.0, BACK_FACE + 0.001, z), material=TILE_DARK)
for x in (-1.50, -0.50, 0.50, 1.50):
    box((0.20, 0.07, 0.10), (x, BACK_FACE + 0.025, 0.05), material=LIME_DARK)
    box((0.16, 0.05, 1.78), (x, BACK_FACE + 0.025, 0.99), material=LIME)
    box((0.20, 0.07, 0.06), (x, BACK_FACE + 0.025, 1.89), material=LIME_DARK)
box((3.56, 0.07, 0.06), (0.0, BACK_FACE + 0.035, 1.94), material=LIME_DARK)
for x in (-1.0, 0.0, 1.0):
    box((0.20, 0.012, 0.42), (x, BACK_FACE + 0.008, 1.40), material=SHADOW)          # Öffnung
    for dx in (-0.05, 0.05):
        box((0.02, 0.01, 0.42), (x + dx, BACK_FACE + 0.016, 1.40), material=WOOD_DARK)  # Gitter
    box((0.20, 0.01, 0.02), (x, BACK_FACE + 0.016, 1.40), material=WOOD_DARK)
    box((0.28, 0.05, 0.035), (x, BACK_FACE + 0.025, 1.172), material=LIME)            # Fensterbank
    box((0.28, 0.02, 0.05), (x, BACK_FACE + 0.01, 1.635), material=TILE_DARK)        # Ziegelsturz
box((0.42, 0.02, 0.80), (0.0, BACK_FACE + 0.015, 0.40), material=WOOD)               # Tür aus Bohlen
for dx in (-0.105, 0.0, 0.105):
    box((0.01, 0.005, 0.78), (dx, BACK_FACE + 0.027, 0.40), material=WOOD_DARK)
for z in (0.20, 0.60):
    box((0.40, 0.006, 0.035), (0.0, BACK_FACE + 0.028, z), material=WOOD_DARK)
for s in (-1, 1):
    box((0.05, 0.04, 0.85), (s * 0.235, BACK_FACE + 0.02, 0.425), material=LIME_DARK)  # Gewände
box((0.58, 0.05, 0.09), (0.0, BACK_FACE + 0.025, 0.875), material=LIME_DARK)          # Sturz
box((0.55, 0.10, 0.04), (0.0, BACK_FACE + 0.06, 0.02), material=LIME_DARK)            # Stufe

# Seitenwände unter der Dachschräge, mit Rundbogenöffnung, Ziegelbändern und Eckpilastern
SIDE_Y0, SIDE_Y1 = 2.05, BACK_FACE
OPEN_Y0, OPEN_Y1, SPRING_Z = 2.55, 3.15, 0.70
ARCH_R = (OPEN_Y1 - OPEN_Y0) / 2
ARCH_CY = (OPEN_Y0 + OPEN_Y1) / 2
APEX_Z = SPRING_Z + ARCH_R


def wall_top(y):
    return roof_z(y) - 0.035


for sx in (-1, 1):
    x0 = 1.65 if sx > 0 else -1.75
    outer = sx * 1.75
    slab_yz([(SIDE_Y0, 0.0), (OPEN_Y0, 0.0), (OPEN_Y0, wall_top(OPEN_Y0)), (SIDE_Y0, wall_top(SIDE_Y0))], x0, 0.10, LIME)
    slab_yz([(OPEN_Y1, 0.0), (SIDE_Y1, 0.0), (SIDE_Y1, wall_top(SIDE_Y1)), (OPEN_Y1, wall_top(OPEN_Y1))], x0, 0.10, LIME)
    slab_yz([(OPEN_Y0, APEX_Z), (OPEN_Y1, APEX_Z), (OPEN_Y1, wall_top(OPEN_Y1)), (OPEN_Y0, wall_top(OPEN_Y0))], x0, 0.10, LIME)
    arc_l = [(ARCH_CY + ARCH_R * math.cos(math.radians(a)), SPRING_Z + ARCH_R * math.sin(math.radians(a))) for a in range(180, 89, -15)]
    arc_r = [(ARCH_CY + ARCH_R * math.cos(math.radians(a)), SPRING_Z + ARCH_R * math.sin(math.radians(a))) for a in range(90, -1, -15)]
    slab_yz(arc_l + [(OPEN_Y0, APEX_Z)], x0, 0.10, LIME)                             # Zwickel links und rechts
    slab_yz(arc_r + [(OPEN_Y1, APEX_Z)], x0, 0.10, LIME)
    for a in range(180, -1, -20):                                                   # Bogensteine aus Ziegel
        t = math.radians(a)
        box((0.015, 0.06, 0.08), (outer + sx * 0.0075, ARCH_CY + (ARCH_R + 0.04) * math.cos(t), SPRING_Z + (ARCH_R + 0.04) * math.sin(t)),
            rot=(a - 90, 0, 0), material=TILE_DARK)
    for z in BANDS:
        y_from = max(SIDE_Y0, EAVE_Y + (z + 0.06 + 0.035 - EAVE_Z) / math.tan(PITCH))
        segments = [(y_from, OPEN_Y0), (OPEN_Y1, SIDE_Y1)] if z < APEX_Z + 0.08 else [(y_from, SIDE_Y1)]
        for a, b in segments:
            a = max(a, y_from)
            if b - a > 0.05:
                box((0.012, b - a, 0.05), (outer, (a + b) / 2, z), material=TILE_DARK)
    for y in (SIDE_Y0 + 0.07, SIDE_Y1 - 0.07):                                       # Eckpilaster
        h = wall_top(y) - 0.06
        box((0.05, 0.14, h), (outer + sx * 0.02, y, h / 2), material=LIME_DARK)
# Vier Säulen mit Basis und Kapitell, darüber der Architrav
for x in (-1.50, -0.50, 0.50, 1.50):
    box((0.15, 0.15, 0.05), (x, COL_Y, 0.025), material=LIME_DARK)
    cyl(0.052, 1.08, (x, COL_Y, 0.59), material=LIME, verts=12)
    box((0.16, 0.16, 0.07), (x, COL_Y, 1.165), material=LIME_DARK)
box((3.30, 0.14, 0.13), (0.0, COL_Y, 1.265), material=LIME)
# Dachbalken von der Wand zum Architrav
for x in (-1.50, -0.50, 0.50, 1.50):
    box((0.06, WALL_Y - COL_Y + 0.10, 0.06), (x, (WALL_Y + COL_Y) / 2, (roof_z(COL_Y) + roof_z(WALL_Y)) / 2 - 0.06),
        rot=(math.degrees(PITCH), 0, 0), material=WOOD_DARK)

# Ziegeldach: Tegulae als Platte mit Stoßfugen, Imbrices als Halbrohre, Stirnziegel an der vorderen Traufe
run = BACK_Y - EAVE_Y
length = run / math.cos(PITCH)
roof_w = ROOF_X1 - ROOF_X0
mid_y = (EAVE_Y + BACK_Y) / 2
box((roof_w, length, T), (0.0, mid_y, roof_z(mid_y)), rot=(math.degrees(PITCH), 0, 0), material=TILE)
normal = (0.0, -math.sin(PITCH), math.cos(PITCH))
for k in range(1, int(length / 0.34) + 1):
    d = k * 0.34 - 0.02
    if d >= length:
        break
    py = EAVE_Y + d * math.cos(PITCH)
    box((roof_w, 0.016, 0.01), (0.0, py + normal[1] * T / 2, roof_z(py) + normal[2] * T / 2),
        rot=(math.degrees(PITCH), 0, 0), material=TILE_DARK)
count = int(roof_w / 0.20)
spacing = roof_w / count
for i in range(count + 1):
    x = ROOF_X0 + i * spacing
    lift = T / 2 + 0.012
    cyl(0.045, length, (x, mid_y + normal[1] * lift, roof_z(mid_y) + normal[2] * lift), rot=(-(90 - math.degrees(PITCH)), 0, 0),
        material=TILE if random.random() > 0.3 else TILE_DARK, verts=8)
    box((0.09, 0.02, 0.10), (x, EAVE_Y - 0.01, roof_z(EAVE_Y) + 0.035), rot=(6, 0, 0), material=TILE_DARK)
box((roof_w, 0.035, 0.08), (0.0, EAVE_Y - 0.02, roof_z(EAVE_Y) - 0.04), material=WOOD_DARK)        # Traufbrett
for x in (ROOF_X0, ROOF_X1):                                                                       # Ortgang
    box((0.035, length, 0.08), (x, mid_y, roof_z(mid_y) - 0.04), rot=(math.degrees(PITCH), 0, 0), material=WOOD_DARK)
    for k in range(int(length / 0.25)):                                                           # Stirnziegel am Ortgang
        py = EAVE_Y + (0.12 + k * 0.25) * math.cos(PITCH)
        box((0.025, 0.06, 0.07), (x + math.copysign(0.02, x), py, roof_z(py) + 0.025), rot=(math.degrees(PITCH), 0, 0), material=TILE_DARK)

# Gemauerte Theke unter der Halle, dahinter ein Wandbord mit Krügen
CTR_Y, CTR_TOP = 2.45, 0.50
box((2.80, 0.24, 0.47), (0.0, CTR_Y, 0.235), material=LIME_DARK)
box((2.90, 0.30, 0.035), (0.0, CTR_Y, 0.4825), material=LIME)
for x in (-1.15, -1.05):
    jug(x, CTR_Y, CTR_TOP, 0.035)
basket(-0.65, CTR_Y, CTR_TOP, 0.08, OCHRE)
for k in range(3):                                                          # Brotlaibe
    sphere(0.04, (-0.25 + k * 0.08, CTR_Y + (0.03 if k % 2 else -0.03), CTR_TOP + 0.017), (1.0, 0.8, 0.5), WOOD, segs=10, rings=5)
basket(0.25, CTR_Y, CTR_TOP, 0.075, RED)
for k in range(2):                                                          # Ölkrüge
    jug(0.65 + k * 0.10, CTR_Y + 0.025 * (1 - 2 * k), CTR_TOP, 0.038)
basket(1.05, CTR_Y, CTR_TOP, 0.07, OLIVE)
box((2.60, 0.14, 0.03), (0.0, WALL_Y - 0.12, 0.80), material=WOOD_DARK)     # Wandbord
for x in (-1.15, 0.0, 1.15):                                                # Konsolen unter dem Bord
    box((0.03, 0.10, 0.08), (x, WALL_Y - 0.10, 0.745), material=WOOD_DARK)
for k in range(9):
    jug(-1.15 + k * 0.29, WALL_Y - 0.12, 0.815, 0.032 + 0.006 * (k % 3), TERRACOTTA)
# Amphoren an den Enden der Theke
for x, y in ((-1.58, 2.30), (-1.58, 2.48), (-1.45, 2.39), (1.58, 2.30), (1.58, 2.48)):
    amphora(x, y, 0.0)

# ---------------------------------------------------------------- Stände links und rechts


def stall(cx, cy, cloth_a, cloth_b, goods):
    """Holzstand mit gestreiftem Stoffdach, das zur Gasse hin abfällt; Theke auf der Innenseite."""
    inner = 1 if cx < 0 else -1
    half_x, half_y = 0.27, 0.40
    x_out, x_in = cx - inner * half_x, cx + inner * half_x
    z_out, drop, reach = 1.02, 0.17, 2 * half_x + 0.08
    beta = math.atan2(drop, reach)

    def awning_z(x):
        return z_out - abs(x - x_out) * math.tan(beta)

    for x in (x_out, x_in):
        for y in (cy - half_y, cy + half_y):
            h = awning_z(x) - 0.012
            box((0.04, 0.04, h), (x, y, h / 2), material=WOOD_DARK)
        box((0.035, 2 * half_y + 0.05, 0.035), (x, cy, awning_z(x) - 0.03), material=WOOD_DARK)   # Längsriegel
    # Stoffbahnen in der Neigung, abwechselnd in zwei Farben, vorn ein Behang
    stripes = 5
    w = (2 * half_y + 0.10) / stripes
    edge_x = x_out + inner * reach
    for k in range(stripes):
        y = cy - half_y - 0.05 + (k + 0.5) * w
        cloth = cloth_a if k % 2 == 0 else cloth_b
        box((reach / math.cos(beta), w, 0.012), ((x_out + edge_x) / 2, y, (z_out + awning_z(edge_x)) / 2),
            rot=(0, inner * math.degrees(beta), 0), material=cloth)
        box((0.01, w, 0.075), (edge_x, y, awning_z(edge_x) - 0.0375), material=cloth)
    # Theke zur Gasse mit Tuch davor
    tx = cx + inner * 0.14
    box((0.19, 2 * half_y - 0.03, 0.43), (tx, cy, 0.215), material=WOOD)
    box((0.23, 2 * half_y + 0.01, 0.025), (tx, cy, 0.4425), material=WOOD_DARK)
    box((0.01, 2 * half_y - 0.10, 0.18), (tx + inner * 0.10, cy, 0.33), material=cloth_a)
    # Hinten Kiste und Säcke
    bx = cx - inner * 0.11
    box((0.17, 0.17, 0.17), (bx, cy + 0.20, 0.085), rot=(0, 0, 8), material=WOOD)
    for k in range(2):
        sack((bx, cy - 0.08 - k * 0.14, 0.0), 0.06)
    goods(tx, cy, 0.455)


def grain_bread(x, cy, z):
    basket(x, cy - 0.24, z, 0.075, OCHRE)
    basket(x, cy - 0.06, z, 0.062, OCHRE)
    for k in range(3):
        sphere(0.037, (x + (0.03 if k % 2 else -0.03), cy + 0.10 + k * 0.065, z + 0.017), (0.8, 1.0, 0.5), WOOD, segs=10, rings=5)


def cheese(x, cy, z):
    for k, y in enumerate((cy - 0.24, cy - 0.06)):
        for h in range(2 if k == 0 else 1):
            cyl(0.048, 0.035, (x, y, z + 0.0175 + h * 0.037), material=OCHRE, verts=12)
    for k in range(2):
        sphere(0.037, (x, cy + 0.12 + k * 0.10, z + 0.017), (0.8, 1.0, 0.5), WOOD, segs=10, rings=5)


def pottery(x, cy, z):
    for k in range(5):
        jug(x + (0.03 if k % 2 else -0.03), cy - 0.28 + k * 0.14, z, 0.032 + 0.008 * (k % 2), TERRACOTTA)


def apples(x, cy, z):
    for by in (cy - 0.22, cy + 0.0):
        cyl(0.075, 0.035, (x, by, z + 0.0175), material=TERRACOTTA, verts=12)
        for k in range(7):
            a = k * 2 * math.pi / 6
            r = 0.0 if k == 6 else 0.04
            sphere(0.021, (x + math.cos(a) * r, by + math.sin(a) * r, z + 0.043 + (0.017 if k == 6 else 0.0)), material=RED, segs=8, rings=4)
    basket(x, cy + 0.24, z, 0.06, RED)


def olives(x, cy, z):
    for by in (cy - 0.20, cy + 0.02):
        cyl(0.063, 0.03, (x, by, z + 0.015), material=TERRACOTTA, verts=12)
        sphere(0.057, (x, by, z + 0.03), (1.0, 1.0, 0.4), OLIVE, segs=10, rings=5)
    basket(x, cy + 0.24, z, 0.058, GREEN)


def cloth_bolts(x, cy, z):
    for k, c in enumerate((RED, BLUE, OCHRE, GREEN, CREAM)):
        cyl(0.026, 0.17, (x, cy - 0.26 + k * 0.13, z + 0.026), rot=(0, 90, 0), material=c, verts=8)


STALL_Y = (-2.02, -0.62, 0.78)
for cy, (a, b), goods in zip(STALL_Y, ((RED, OCHRE), (BLUE, CREAM), (GREEN, OCHRE)), (grain_bread, cheese, pottery)):
    stall(-1.50, cy, a, b, goods)
for cy, (a, b), goods in zip(STALL_Y, ((GREEN, OCHRE), (RED, OCHRE), (BLUE, CREAM)), (apples, olives, cloth_bolts)):
    stall(1.50, cy, a, b, goods)

# ---------------------------------------------------------------- Vorn: Eichtisch, Amphorengestell, Handkarren

# Mensa Ponderaria: Kalksteinplatte auf zwei Blöcken, Mulden der amtlichen Hohlmaße
MX, MY = -1.25, -2.95
for s in (-1, 1):
    box((0.07, 0.18, 0.41), (MX + s * 0.18, MY, 0.205), material=LIME_DARK)
box((0.51, 0.23, 0.045), (MX, MY, 0.4325), material=LIME)
radii = (0.06, 0.047, 0.036, 0.025)
for k, r in enumerate(radii):
    cyl(r, 0.008, (MX - 0.192 + sum(radii[:k]) * 2 + r + k * 0.016, MY, 0.4535), material=SHADOW, verts=12)

# Gestell mit vier Amphoren an der Vorderkante links
RY = -3.45
for x in (-1.70, -1.10):
    box((0.03, 0.03, 0.28), (x, RY, 0.14), material=WOOD_DARK)
for z in (0.07, 0.19):
    box((0.62, 0.025, 0.025), (-1.40, RY - 0.065, z), material=WOOD_DARK)
for x in (-1.61, -1.47, -1.33, -1.19):
    amphora(x, RY, 0.0)

# Handkarren rechts vorn, Deichsel zur Mitte, mit drei Säcken
CART_A = -90
at = placed((1.25, -2.95), CART_A)
box((0.55 * S, 0.85 * S, 0.05 * S), at((0.0, 0.0, 0.36 * S)), rot=(0, 0, CART_A), material=WOOD)
for sx in (-1, 1):
    box((0.04 * S, 0.85 * S, 0.16 * S), at((sx * 0.275 * S, 0.0, 0.46 * S)), rot=(0, 0, CART_A), material=WOOD_DARK)
    cyl(0.22 * S, 0.06 * S, at((sx * 0.34 * S, 0.1 * S, 0.22 * S)), rot=(0, 90, CART_A), material=WOOD_DARK, verts=12)
    cyl(0.05 * S, 0.08 * S, at((sx * 0.36 * S, 0.1 * S, 0.22 * S)), rot=(0, 90, CART_A), material=WOOD, verts=8)
    box((0.04 * S, 0.6 * S, 0.04 * S), at((sx * 0.22 * S, -0.62 * S, 0.32 * S)), rot=(-20, 0, CART_A), material=WOOD_DARK)
box((0.55 * S, 0.04 * S, 0.16 * S), at((0.0, 0.425 * S, 0.46 * S)), rot=(0, 0, CART_A), material=WOOD_DARK)
cyl(0.025 * S, 0.7 * S, at((0.0, 0.1 * S, 0.22 * S)), rot=(0, 90, CART_A), material=WOOD_DARK, verts=6)
for lx, ly in ((-0.13, -0.22), (0.13, -0.05), (-0.05, 0.20)):
    sack(at((lx * S, ly * S, 0.385 * S)), 0.10 * S)

# Ecksteine an den Grenzen (wie bei den anderen Modellen: gleiche Ausdehnung, nicht zu sehen)
for x in (-1.82, 1.82):
    for y in (-3.66, 3.66):
        box((0.02, 0.02, 0.02), (x - math.copysign(0.01, x), y - math.copysign(0.01, y), 0.01), material=LIME_DARK)

# ---------------------------------------------------------------- Zusammenfügen und Export

bpy.ops.object.select_all(action="DESELECT")
for obj in PARTS:
    obj.select_set(True)
bpy.context.view_layer.objects.active = PARTS[0]
bpy.ops.object.join()
model = bpy.context.active_object
model.name = "Markt"
model.data.name = "Markt"
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Gleiche Materialien zusammenlegen (join übernimmt die Slots aller Teile)
bpy.ops.object.material_slot_remove_unused()

dims = model.dimensions
tris = sum(len(p.vertices) - 2 for p in model.data.polygons)
print(f"Markt: {dims.x:.2f} x {dims.y:.2f} x {dims.z:.2f} m, {tris} Dreiecke, {len(model.material_slots)} Materialien")

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
    cam.location = (6.5, -9.5, 6.5)
    direction = Vector((0.0, 0.0, 0.4)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = 40
    scene.camera = cam
    scene.render.filepath = PREVIEW
    bpy.ops.render.render(write_still=True)
