"""
Lager (Antike), 2 x 2 Felder: römischer Speicher (Horreum). Gemauerter Bau aus Kalkstein mit Ziegelbändern wie der
Markt, Satteldach aus Terrakotta-Ziegeln mit dem Giebel zur Straße, Strebepfeiler an den Längsseiten, hoch liegende
Lüftungsschlitze und ein Sockel mit kleinen Öffnungen unter dem angehobenen Boden. Im Giebel ein breites Bogentor mit
Bohlenflügeln (einer offen, dahinter Säcke) und ein Rundfenster; davor eine Laderampe aus Stein mit Stufen, ein
Ladegalgen mit hängendem Sack über einem Handkarren, Säcke, Kisten und ein Gestell mit Amphoren.

Maßstab wie beim Markt: Gebäude passen zu den Häusern (ein Stockwerk etwa 1 m), Dinge, an denen man Menschen misst
(Karren, Säcke, Kisten, Amphoren, Stufen), sind etwa 55 % so groß wie in Wirklichkeit.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/lager.py -- <blend> <fbx> [<vorschau.png>]

1 Einheit = 1 m, Boden bei z = 0, Mitte im Ursprung, alles innerhalb von 3,64 x 3,64 m (Grundfläche 4 x 4 m, Rand für
die Bodenplatte). Vorderseite (Straße) zeigt nach -Y (in Unity -z). Farben sind sRGB-Werte, wie sie im Spiel
erscheinen; mat() rechnet sie linear um. Am Ende wird alles zu einem Objekt mit einem Material je Farbe verschmolzen.
"""
import math
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
BLEND = args[0] if len(args) > 0 else None
FBX = args[1] if len(args) > 1 else None
PREVIEW = args[2] if len(args) > 2 else None

random.seed(5)  # gleiche Datei bei jedem Lauf
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
DARK = mat("Innenraum", (0.16, 0.13, 0.11))
WOOD = mat("Holz", (0.62, 0.45, 0.28))
WOOD_DARK = mat("HolzDunkel", (0.42, 0.29, 0.18))
IRON = mat("Eisen", (0.24, 0.23, 0.22), rough=0.6)
TILE = mat("Ziegel", (0.74, 0.37, 0.22))
TILE_DARK = mat("ZiegelDunkel", (0.62, 0.29, 0.17))
TERRACOTTA = mat("Terrakotta", (0.76, 0.46, 0.30))
SACK = mat("Sack", (0.80, 0.72, 0.56))
ROPE = mat("Seil", (0.70, 0.60, 0.42))

S = 0.55   # Maßstab der Dinge, an denen man Menschen misst (Karren, Säcke, Kisten, Amphoren)

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


def sphere(radius, loc, scale=(1.0, 1.0, 1.0), material=SACK, segs=8, rings=5):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings, radius=radius, location=loc)
    obj = bpy.context.active_object
    obj.scale = scale
    return keep(obj, material)


def amphora(x, y, z0):
    """Spitzamphore (in Wirklichkeit 0,55 m): Bauch, kurzer Fuß, Hals, Lippe, zwei Henkel."""
    cone(0.015 * S, 0.06 * S, 0.12 * S, (x, y, z0 + 0.06 * S), TERRACOTTA, verts=8)
    sphere(0.095 * S, (x, y, z0 + 0.26 * S), (1.0, 1.0, 1.9), TERRACOTTA, segs=10, rings=6)
    cyl(0.035 * S, 0.12 * S, (x, y, z0 + 0.47 * S), material=TERRACOTTA, verts=8)
    cyl(0.048 * S, 0.025 * S, (x, y, z0 + 0.535 * S), material=TERRACOTTA, verts=8)
    for s in (-1, 1):
        box((0.02 * S, 0.02 * S, 0.13 * S), (x + s * 0.06 * S, y, z0 + 0.45 * S), rot=(0, s * 20, 0), material=TERRACOTTA)


def sack(loc, r):
    """Gefüllter Sack: bauchig, oben zusammengeschnürt mit Zipfel."""
    x, y, z0 = loc
    sphere(r, (x, y, z0 + r), (1.0, 0.85, 1.15), SACK, segs=8, rings=5)
    cyl(r * 0.28, r * 0.27, (x, y, z0 + r * 2.2), material=WOOD_DARK, verts=6)     # Schnur
    cone(r * 0.32, r * 0.12, r * 0.45, (x, y, z0 + r * 2.4), SACK, verts=6)


def lying_sack(loc, r, angle):
    """Liegender Sack (auf Stapeln), um z gedreht."""
    x, y, z0 = loc
    obj = sphere(r, (x, y, z0 + r * 0.7), (1.45, 0.95, 0.7), SACK, segs=8, rings=5)
    obj.rotation_euler = (0.0, 0.0, math.radians(angle))


def crate(loc, size, angle=0.0):
    """Kiste aus Brettern mit dunklen Kanten oben."""
    x, y, z0 = loc
    box((size, size, size), (x, y, z0 + size / 2), rot=(0, 0, angle), material=WOOD)
    box((size + 0.01, size + 0.01, size * 0.12), (x, y, z0 + size * 0.94), rot=(0, 0, angle), material=WOOD_DARK)
    box((size + 0.01, size + 0.01, size * 0.10), (x, y, z0 + size * 0.30), rot=(0, 0, angle), material=WOOD_DARK)


def slab(points, plane, offset, depth, material):
    """Vieleck in der Ebene 'xz' (Punkte x, z; gezogen in y) oder 'yz' (Punkte y, z; gezogen in x) von offset bis
    offset + depth. Für Giebelwände mit Bogentor und Strebepfeiler mit schräger Oberseite; auch nicht konvexe
    Umrisse werden sauber in Dreiecke zerlegt."""
    n = len(points)
    if plane == "xz":
        verts = [(a, offset, b) for a, b in points] + [(a, offset + depth, b) for a, b in points]
    else:
        verts = [(offset, a, b) for a, b in points] + [(offset + depth, a, b) for a, b in points]
    faces = [tuple(range(n))[::-1], tuple(range(n, 2 * n))] + [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    mesh = bpy.data.meshes.new("Wand")
    mesh.from_pydata(verts, [], faces)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4], ngon_method="EAR_CLIP")
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


# ---------------------------------------------------------------- Maße des Speichers

WX = 1.30                    # Außenseite der Längswände
FRONT, BACK = -0.40, 1.62    # Außenseite von Giebel- und Rückwand
WALL_T = 0.10
FLOOR = 0.30                 # angehobener Boden (Schutz vor Feuchte und Ungeziefer)
PITCH = math.radians(22)
EAVE_Z = 1.60                # Dachmitte über der Außenseite der Längswände
ROOF_X = 1.47                # Traufe
ROOF_Y0, ROOF_Y1 = -0.55, 1.78
T = 0.04
RIDGE_Z = EAVE_Z + WX * math.tan(PITCH)
BANDS = (0.55, 0.95, 1.35)
GATE_HW, SPRING_Z = 0.45, 1.05          # Tor: halbe Breite, Kämpfer
GATE_CZ = SPRING_Z
BUTTRESS_Y = (0.10, 0.61, 1.12)
SLOT_Y = (-0.135, 0.355, 0.865, 1.355)  # Lüftungsschlitze zwischen den Pfeilern


def roof_z(x):
    return RIDGE_Z - abs(x) * math.tan(PITCH)


def wall_top(x):
    return roof_z(x) - 0.04


def arc(cx, cz, r, a0, a1, step):
    """Punkte (x, z) auf einem Kreisbogen von a0 bis a1 Grad (beide eingeschlossen)."""
    count = max(1, round(abs(a1 - a0) / step))
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * k / count)),
             cz + r * math.sin(math.radians(a0 + (a1 - a0) * k / count))) for k in range(count + 1)]


# ---------------------------------------------------------------- Wände

# Giebelwand zur Straße: zwei Hälften um das Bogentor, oben unter der Dachschräge
for sx in (-1, 1):
    pts = [(-WX, 0.0), (0.0, 0.0), (0.0, FLOOR), (-GATE_HW, FLOOR), (-GATE_HW, SPRING_Z)]
    pts += arc(0.0, GATE_CZ, GATE_HW, 180, 90, 15)[1:]
    pts += [(0.0, wall_top(0.0)), (-WX, wall_top(WX))]
    if sx > 0:
        pts = [(-x, z) for x, z in reversed(pts)]
    slab(pts, "xz", FRONT, WALL_T, LIME)
# Rückwand als volles Giebeldreieck
slab([(-WX, 0.0), (WX, 0.0), (WX, wall_top(WX)), (0.0, wall_top(0.0)), (-WX, wall_top(WX))], "xz", BACK - WALL_T, WALL_T, LIME)
# Längswände bis unter die Traufe
for sx in (-1, 1):
    h = wall_top(WX)
    box((WALL_T, BACK - FRONT, h), (sx * (WX - WALL_T / 2), (FRONT + BACK) / 2, h / 2), material=LIME)

# Sockel unter dem angehobenen Boden, etwas vorstehend, mit kleinen Lüftungsöffnungen an den Längsseiten
box((2 * WX + 0.04, 0.03, FLOOR), (0.0, FRONT - 0.015, FLOOR / 2), material=LIME_DARK)
box((2 * WX + 0.04, 0.03, FLOOR), (0.0, BACK + 0.015, FLOOR / 2), material=LIME_DARK)
for sx in (-1, 1):
    box((0.03, BACK - FRONT + 0.06, FLOOR), (sx * (WX + 0.015), (FRONT + BACK) / 2, FLOOR / 2), material=LIME_DARK)
    for y in SLOT_Y:
        box((0.012, 0.08, 0.07), (sx * (WX + 0.031), y, 0.15), material=DARK)

# Ziegelbänder wie Opus mixtum
for z in BANDS:
    for x0, x1 in ((-WX, -GATE_HW - 0.08), (GATE_HW + 0.08, WX)):
        box((x1 - x0, 0.012, 0.05), ((x0 + x1) / 2, FRONT - 0.006, z), material=TILE_DARK)
    box((2 * WX, 0.012, 0.05), (0.0, BACK + 0.006, z), material=TILE_DARK)
    for sx in (-1, 1):
        box((0.012, BACK - FRONT, 0.05), (sx * (WX + 0.006), (FRONT + BACK) / 2, z), material=TILE_DARK)

# Gesims unter der Traufe, Eckpilaster
for sx in (-1, 1):
    box((0.06, BACK - FRONT + 0.06, 0.05), (sx * (WX + 0.02), (FRONT + BACK) / 2, wall_top(WX) - 0.04), material=LIME_DARK)
    for y in (FRONT, BACK):
        inward = 1 if y == FRONT else -1
        box((0.12, 0.04, 1.50), (sx * (WX - 0.06), y - inward * 0.02, 0.75), material=LIME_DARK)

# Strebepfeiler an den Längsseiten mit schräger Oberseite, dazwischen hoch liegende Lüftungsschlitze
for sx in (-1, 1):
    for y in BUTTRESS_Y:
        pts = [(WX - 0.01, 0.0), (WX + 0.16, 0.0), (WX + 0.16, 0.80), (WX - 0.01, 1.12)]
        if sx < 0:
            pts = [(-x, z) for x, z in reversed(pts)]
        slab([(x, z) for x, z in pts], "xz", y - 0.08, 0.16, LIME_DARK)
    for y in SLOT_Y:
        box((0.012, 0.05, 0.30), (sx * (WX + 0.006), y, 1.10), material=DARK)
        box((0.02, 0.11, 0.04), (sx * (WX + 0.01), y, 1.27), material=TILE_DARK)      # Ziegelsturz
        box((0.03, 0.10, 0.025), (sx * (WX + 0.015), y, 0.94), material=LIME)         # Bank

# Rückwand: zwei Pilaster und zwei Lüftungsschlitze wie an den Längsseiten
for x in (-0.65, 0.65):
    box((0.12, 0.04, 1.40), (x * 1.55, BACK + 0.02, 0.70), material=LIME_DARK)
    box((0.05, 0.012, 0.30), (x, BACK + 0.006, 1.10), material=DARK)
    box((0.11, 0.02, 0.04), (x, BACK + 0.01, 1.27), material=TILE_DARK)
    box((0.10, 0.03, 0.025), (x, BACK + 0.015, 0.94), material=LIME)

# Rundfenster in beiden Giebeln, mit Ziegelring
for y, out in ((FRONT, -1), (BACK, 1)):
    cyl(0.135, 0.01, (0.0, y + out * 0.005, 1.82), rot=(90, 0, 0), material=TILE_DARK, verts=16)
    cyl(0.095, 0.012, (0.0, y + out * 0.008, 1.82), rot=(90, 0, 0), material=DARK, verts=16)
    for a in (0, 90):                                               # Gitterkreuz
        box((0.19, 0.01, 0.015), (0.0, y + out * 0.012, 1.82), rot=(0, a, 0), material=WOOD_DARK)

# ---------------------------------------------------------------- Tor

# Torbogen aus Ziegeln mit Schlussstein, Gewände und Kämpfersteine
for a in range(0, 181, 15):
    t = math.radians(a)
    r = GATE_HW + 0.04
    big = a == 90
    box((0.08 if not big else 0.10, 0.015, 0.06 if not big else 0.09), (r * math.cos(t), FRONT - 0.0075, GATE_CZ + r * math.sin(t)),
        rot=(0, -a, 0), material=LIME if big else TILE_DARK)
for sx in (-1, 1):
    box((0.08, 0.02, SPRING_Z - FLOOR), (sx * (GATE_HW + 0.04), FRONT - 0.01, (SPRING_Z + FLOOR) / 2), material=LIME_DARK)
    box((0.13, 0.05, 0.05), (sx * (GATE_HW + 0.04), FRONT - 0.02, SPRING_Z), material=LIME_DARK)

# Dunkler Innenraum hinter dem Tor: Vorraum mit Boden, Seiten, Decke, dahinter die dunkle Halle
IN_Y = 0.30
box((2 * GATE_HW + 0.30, IN_Y - FRONT, 0.02), (0.0, (FRONT + IN_Y) / 2, FLOOR - 0.01), material=LIME_DARK)
for sx in (-1, 1):
    box((0.02, IN_Y - FRONT - WALL_T, 1.30), (sx * (GATE_HW + 0.16), (FRONT + WALL_T + IN_Y) / 2, FLOOR + 0.65), material=DARK)
box((2 * GATE_HW + 0.34, IN_Y - FRONT - WALL_T, 0.02), (0.0, (FRONT + WALL_T + IN_Y) / 2, FLOOR + 1.30), material=DARK)
box((2 * GATE_HW + 0.34, 0.02, 1.30), (0.0, IN_Y, FLOOR + 0.65), material=DARK)
# Säcke im Vorraum
for x, y, r in ((-0.30, 0.12, 0.065), (-0.16, 0.18, 0.06), (-0.27, 0.22, 0.055)):
    sack((x, y, FLOOR), r)
lying_sack((0.05, 0.18, FLOOR), 0.06, 80)

# Bogenfeld: Holz mit dunklen Strahlen
lunette = arc(0.0, GATE_CZ, GATE_HW, 0, 180, 15)
slab(lunette, "xz", FRONT + 0.04, 0.02, WOOD)
for a in (45, 90, 135):
    t = math.radians(a)
    box((GATE_HW - 0.04, 0.008, 0.02), (math.cos(t) * GATE_HW / 2, FRONT + 0.035, GATE_CZ + math.sin(t) * GATE_HW / 2),
        rot=(0, -a, 0), material=WOOD_DARK)
box((2 * GATE_HW, 0.03, 0.04), (0.0, FRONT + 0.04, SPRING_Z), material=WOOD_DARK)      # Kämpferbalken


def door_leaf(hinge, angle):
    """Torflügel aus Bohlen mit zwei Eisenbändern; angle = Richtung vom Scharnier zur freien Kante (Grad um z)."""
    w, h = GATE_HW - 0.01, SPRING_Z - FLOOR - 0.03
    d = (math.cos(math.radians(angle)), math.sin(math.radians(angle)))
    n = (-d[1], d[0])
    cx, cy = hinge[0] + d[0] * w / 2, hinge[1] + d[1] * w / 2
    box((w, 0.035, h), (cx, cy, FLOOR + h / 2), rot=(0, 0, angle), material=WOOD)
    for side in (-1, 1):
        off = side * 0.019
        for k in (1, 2, 3):
            t = k / 4 - 0.5
            box((0.008, 0.004, h - 0.02), (cx + d[0] * t * w + n[0] * off, cy + d[1] * t * w + n[1] * off, FLOOR + h / 2),
                rot=(0, 0, angle), material=WOOD_DARK)
        for z in (0.22, 0.58):
            box((w - 0.02, 0.006, 0.03), (cx + n[0] * off * 1.1, cy + n[1] * off * 1.1, FLOOR + z), rot=(0, 0, angle), material=IRON)


door_leaf((-GATE_HW, FRONT + 0.06), 0)          # links geschlossen
door_leaf((GATE_HW, FRONT + 0.06), 110)         # rechts nach innen offen
box((0.12, 0.02, 0.02), (-0.04, FRONT + 0.035, FLOOR + 0.40), material=IRON)      # Riegel

# ---------------------------------------------------------------- Satteldach mit Ziegeln

slope = ROOF_X / math.cos(PITCH)
length = ROOF_Y1 - ROOF_Y0
mid_y = (ROOF_Y0 + ROOF_Y1) / 2
deg = math.degrees(PITCH)
for sx in (-1, 1):
    cx = sx * ROOF_X / 2
    box((slope, length, T), (cx, mid_y, roof_z(cx)), rot=(0, sx * deg, 0), material=TILE)
    normal = (sx * math.sin(PITCH), 0.0, math.cos(PITCH))
    # Stoßfugen der Tegulae parallel zum First
    for k in range(1, int(slope / 0.30) + 1):
        d = k * 0.30
        if d >= slope - 0.05:
            break
        px = sx * d * math.cos(PITCH)
        box((0.016, length, 0.01), (px + normal[0] * T / 2, mid_y, roof_z(px) + normal[2] * T / 2),
            rot=(0, sx * deg, 0), material=TILE_DARK)
    # Imbrices als Halbrohre den Hang hinab, Stirnziegel an der Traufe
    count = int(length / 0.19)
    spacing = length / count
    lift = T / 2 + 0.012
    for i in range(count + 1):
        y = ROOF_Y0 + i * spacing
        cyl(0.042, slope, (cx + normal[0] * lift, y, roof_z(cx) + normal[2] * lift), rot=(0, sx * (90 + deg), 0),
            material=TILE if random.random() > 0.3 else TILE_DARK, verts=8)
        box((0.02, 0.085, 0.09), (sx * (ROOF_X + 0.01), y, roof_z(ROOF_X) + 0.035), rot=(0, sx * -6, 0), material=TILE_DARK)
    box((0.035, length, 0.08), (sx * (ROOF_X + 0.015), mid_y, roof_z(ROOF_X) - 0.04), material=WOOD_DARK)   # Traufbrett
    for y in (ROOF_Y0, ROOF_Y1):                                                                       # Ortgangbretter
        box((slope, 0.035, 0.08), (cx, y + (0.015 if y > 0 else -0.015), roof_z(cx) - 0.04), rot=(0, sx * deg, 0), material=WOOD_DARK)
# Firstziegel
cyl(0.06, length + 0.04, (0.0, mid_y, RIDGE_Z + T / 2 + 0.02), rot=(90, 0, 0), material=TILE_DARK, verts=10)
for y in (ROOF_Y0 - 0.02, ROOF_Y1 + 0.02):
    cyl(0.07, 0.02, (0.0, y, RIDGE_Z + T / 2 + 0.03), rot=(90, 0, 0), material=TILE, verts=10)        # Stirnscheiben

# ---------------------------------------------------------------- Laderampe, Galgen, Waren

# Rampe aus Stein vor dem Tor, zwei Stufen zur Straße
RAMP_Y = FRONT - 0.44
box((1.70, 0.44, FLOOR - 0.02), (0.0, (FRONT + RAMP_Y) / 2, (FLOOR - 0.02) / 2), material=LIME_DARK)
box((1.74, 0.48, 0.03), (0.0, (FRONT + RAMP_Y) / 2 - 0.02, FLOOR - 0.015), material=LIME)
for k, h in enumerate((0.20, 0.10)):
    box((0.70, 0.13, h), (0.0, RAMP_Y - 0.065 - k * 0.13, h / 2), material=LIME_DARK if k else LIME)

# Säcke auf der Rampe links vom Tor
for x, y in ((-0.72, -0.56), (-0.58, -0.56), (-0.65, -0.70), (-0.78, -0.72)):
    lying_sack((x, y, FLOOR), 0.06, random.uniform(-15, 15) + 90)
lying_sack((-0.68, -0.62, FLOOR + 0.08), 0.06, 75)
sack((-0.47, -0.74, FLOOR), 0.06)
for x in (-0.62, -0.53):                    # zwei Amphoren am Tor
    amphora(x, -0.48, FLOOR)

# Ladegalgen rechts auf der Rampe: Pfosten, Ausleger zur Straße, Strebe, Seil mit Rolle und hängendem Sack
GX, GY = 0.72, -0.62
box((0.12, 0.12, 0.05), (GX, GY, FLOOR + 0.025), material=WOOD_DARK)
box((0.06, 0.06, 1.12), (GX, GY, FLOOR + 0.56), material=WOOD_DARK)
JIB_Y0, JIB_Y1, JIB_Z = GY + 0.04, -1.28, FLOOR + 1.10
box((0.05, JIB_Y0 - JIB_Y1, 0.05), (GX, (JIB_Y0 + JIB_Y1) / 2, JIB_Z), material=WOOD_DARK)
box((0.04, 0.45, 0.04), (GX, GY - 0.16, JIB_Z - 0.16), rot=(-45, 0, 0), material=WOOD_DARK)
HANG_Y = -1.20
cyl(0.03, 0.03, (GX, HANG_Y, JIB_Z - 0.05), rot=(0, 90, 0), material=WOOD, verts=10)      # Rolle
SACK_Z = 0.70
box((0.008, 0.008, JIB_Z - 0.05 - (SACK_Z + 0.18)), (GX, HANG_Y, (JIB_Z - 0.05 + SACK_Z + 0.18) / 2), material=ROPE)
box((0.006, abs(HANG_Y - GY) - 0.03, 0.006), (GX + 0.03, (HANG_Y + GY) / 2, JIB_Z - 0.035), material=ROPE)
box((0.006, 0.006, 0.55), (GX + 0.035, GY - 0.03, JIB_Z - 0.31), material=ROPE)            # Zugseil am Pfosten
sack((GX, HANG_Y, SACK_Z), 0.065)

# Handkarren unter dem Galgen, Deichsel zur Straße, mit zwei Säcken
CART_A = 0
at = placed((GX, -1.30), CART_A)
box((0.55 * S, 0.85 * S, 0.05 * S), at((0.0, 0.0, 0.36 * S)), rot=(0, 0, CART_A), material=WOOD)
for sx in (-1, 1):
    box((0.04 * S, 0.85 * S, 0.16 * S), at((sx * 0.275 * S, 0.0, 0.46 * S)), rot=(0, 0, CART_A), material=WOOD_DARK)
    cyl(0.22 * S, 0.06 * S, at((sx * 0.34 * S, 0.1 * S, 0.22 * S)), rot=(0, 90, CART_A), material=WOOD_DARK, verts=12)
    cyl(0.05 * S, 0.08 * S, at((sx * 0.36 * S, 0.1 * S, 0.22 * S)), rot=(0, 90, CART_A), material=WOOD, verts=8)
    box((0.04 * S, 0.6 * S, 0.04 * S), at((sx * 0.22 * S, -0.62 * S, 0.32 * S)), rot=(-20, 0, CART_A), material=WOOD_DARK)
box((0.55 * S, 0.04 * S, 0.16 * S), at((0.0, 0.425 * S, 0.46 * S)), rot=(0, 0, CART_A), material=WOOD_DARK)
cyl(0.025 * S, 0.7 * S, at((0.0, 0.1 * S, 0.22 * S)), rot=(0, 90, CART_A), material=WOOD_DARK, verts=6)
for lx, ly in ((-0.10, 0.15), (0.11, -0.08)):
    sack(at((lx * S, ly * S, 0.385 * S)), 0.10 * S)

# Links vorn: Kisten und ein Gestell mit Amphoren
crate((-1.30, -0.80, 0.0), 0.20, 4)
crate((-1.06, -0.82, 0.0), 0.20, -6)
crate((-1.20, -0.80, 0.20), 0.18, 12)
crate((-1.40, -1.08, 0.0), 0.16, 20)
RY = -1.55
for x in (-1.52, -0.92):
    box((0.03, 0.03, 0.28), (x, RY, 0.14), material=WOOD_DARK)
for z in (0.07, 0.19):
    box((0.62, 0.025, 0.025), (-1.22, RY - 0.065, z), material=WOOD_DARK)
for x in (-1.43, -1.29, -1.15, -1.01):
    amphora(x, RY, 0.0)
for x, y in ((-0.55, -1.40), (-0.42, -1.48)):
    sack((x, y, 0.0), 0.06)

# Ecksteine an den Grenzen (wie bei den anderen Modellen: gleiche Ausdehnung, nicht zu sehen)
for x in (-1.82, 1.82):
    for y in (-1.82, 1.82):
        box((0.02, 0.02, 0.02), (x - math.copysign(0.01, x), y - math.copysign(0.01, y), 0.01), material=LIME_DARK)

# ---------------------------------------------------------------- Zusammenfügen und Export

bpy.ops.object.select_all(action="DESELECT")
for obj in PARTS:
    obj.select_set(True)
bpy.context.view_layer.objects.active = PARTS[0]
bpy.ops.object.join()
model = bpy.context.active_object
model.name = "Lager"
model.data.name = "Lager"
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Gleiche Materialien zusammenlegen (join übernimmt die Slots aller Teile)
bpy.ops.object.material_slot_remove_unused()

dims = model.dimensions
tris = sum(len(p.vertices) - 2 for p in model.data.polygons)
print(f"Lager: {dims.x:.2f} x {dims.y:.2f} x {dims.z:.2f} m, {tris} Dreiecke, {len(model.material_slots)} Materialien")

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
    cam.location = (5.0, -6.5, 4.6)
    direction = Vector((0.0, 0.0, 0.7)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = 40
    scene.camera = cam
    scene.render.filepath = PREVIEW
    bpy.ops.render.render(write_still=True)
