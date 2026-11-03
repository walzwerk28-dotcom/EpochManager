"""
Markt (Antike), 2 x 2 Felder: kleiner römischer Marktplatz, zur Straße offen. In der Mitte ein achteckiges Brunnenbecken
aus Kalkstein mit Säule und Schale, aus der Wasser fällt; hinten eine Verkaufshalle mit Pultdach aus Terrakotta-Ziegeln
auf drei Säulen, darunter eine gemauerte Theke mit Krügen, Körben und Brot, an der Rückwand Amphoren; links und rechts je
ein Holzstand mit gestreiftem Stoffdach (rot-ocker mit Getreide, Brot und Käse; grün-ocker mit Äpfeln und Oliven);
vorn links ein Eichtisch (Mensa Ponderaria) mit den Mulden der amtlichen Hohlmaße und ein Gestell mit Amphoren, vorn
rechts ein Handkarren mit Säcken. Vorn in der Mitte bleibt der Zugang von der Straße frei.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/markt.py -- <blend> <fbx> [<vorschau.png>]

Maßstab 1 Einheit = 1 m, Boden bei z = 0, Mitte im Ursprung, alles innerhalb von 3,64 x 3,64 m (Grundfläche 4 m,
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
OLIVE = mat("Olive", (0.30, 0.33, 0.18))
TERRACOTTA = mat("Terrakotta", (0.76, 0.46, 0.30))
BASKET = mat("Korb", (0.66, 0.52, 0.30))
SACK = mat("Sack", (0.80, 0.72, 0.56))

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
    """Spitzamphore (0,55 m): Bauch, kurzer Fuß, Hals, Lippe, zwei Henkel."""
    cone(0.015, 0.06, 0.12, (x, y, z0 + 0.06), TERRACOTTA, verts=8)
    sphere(0.095, (x, y, z0 + 0.26), (1.0, 1.0, 1.9), TERRACOTTA, segs=10, rings=6)
    cyl(0.035, 0.12, (x, y, z0 + 0.47), material=TERRACOTTA, verts=8)
    cyl(0.048, 0.025, (x, y, z0 + 0.535), material=TERRACOTTA, verts=8)
    for s in (-1, 1):
        box((0.02, 0.02, 0.13), (x + s * 0.06, y, z0 + 0.45), rot=(0, s * 20, 0), material=TERRACOTTA)


def jug(x, y, z0, r=0.07, material=TERRACOTTA):
    """Bauchiger Krug mit Hals."""
    sphere(r, (x, y, z0 + r * 0.9), (1.0, 1.0, 1.05), material, segs=10, rings=6)
    cyl(r * 0.45, r * 0.7, (x, y, z0 + r * 1.95), material=material, verts=8)
    cyl(r * 0.58, r * 0.18, (x, y, z0 + r * 2.3), material=material, verts=8)


def basket(x, y, z0, r, fill):
    """Flacher Korb mit gewölbtem Inhalt."""
    cyl(r, 0.12, (x, y, z0 + 0.06), material=BASKET, verts=10)
    sphere(r * 0.92, (x, y, z0 + 0.12), (1.0, 1.0, 0.35), fill, segs=10, rings=5)


def sack(loc, r=0.11):
    """Gefüllter Sack: bauchig, oben zusammengeschnürt mit Zipfel."""
    x, y, z0 = loc
    sphere(r, (x, y, z0 + r), (1.0, 0.85, 1.15), SACK, segs=8, rings=5)
    cyl(r * 0.28, 0.03, (x, y, z0 + r * 2.2), material=WOOD_DARK, verts=6)     # Schnur
    cone(r * 0.32, r * 0.12, r * 0.45, (x, y, z0 + r * 2.4), SACK, verts=6)


def placed(origin, angle):
    """Teile einer Gruppe in eigenen Koordinaten setzen (um z gedreht und verschoben)."""
    m = Matrix.Rotation(math.radians(angle), 4, "Z")

    def at(local):
        return tuple(m @ Vector(local) + Vector((origin[0], origin[1], 0.0)))
    return at


# ---------------------------------------------------------------- Brunnen in der Mitte

FX, FY = 0.0, 0.0
APOTHEM, RIM_T, RIM_H = 0.50, 0.10, 0.42
cyl(0.66, 0.06, (FX, FY, 0.03), material=LIME_DARK, verts=8)                   # Stufe
cyl(APOTHEM / math.cos(math.radians(22.5)), 0.30, (FX, FY, 0.15), material=SHADOW, verts=8)   # Beckenboden
side = 2 * APOTHEM * math.tan(math.radians(22.5)) + 0.02
for k in range(8):
    a = math.radians(22.5 + k * 45)
    x, y = FX + math.cos(a) * APOTHEM, FY + math.sin(a) * APOTHEM
    box((RIM_T, side, RIM_H), (x, y, RIM_H / 2), rot=(0, 0, math.degrees(a)), material=LIME_DARK)
    box((RIM_T + 0.06, side + 0.04, 0.04), (x, y, RIM_H + 0.02), rot=(0, 0, math.degrees(a)), material=LIME)
inner = APOTHEM - RIM_T / 2
cyl(inner / math.cos(math.radians(22.5)) + 0.01, 0.01, (FX, FY, 0.35), material=WATER, verts=8)
# Säule mit Schale, aus der an vier Seiten Wasser ins Becken fällt
cyl(0.11, 0.10, (FX, FY, 0.40), material=LIME_DARK, verts=10)
cyl(0.065, 0.62, (FX, FY, 0.76), material=LIME, verts=10)
cone(0.07, 0.21, 0.09, (FX, FY, 1.115), LIME, verts=12)
cyl(0.18, 0.01, (FX, FY, 1.158), material=WATER, verts=12)
for k in range(4):
    a = math.radians(45 + k * 90)
    x, y = FX + math.cos(a) * 0.215, FY + math.sin(a) * 0.215
    box((0.010, 0.018, 0.80), (x, y, 0.75), rot=(0, 0, math.degrees(a)), material=WATER)

# ---------------------------------------------------------------- Verkaufshalle hinten mit Pultdach

PITCH = math.radians(20)
T = 0.05
EAVE_Y, BACK_Y, EAVE_Z = 0.85, 1.82, 1.95      # Dachmitte an der vorderen Traufe
ROOF_X0, ROOF_X1 = -1.76, 1.76
COL_Y = 0.98
WALL_Y = 1.74


def roof_z(y):
    return EAVE_Z + (y - EAVE_Y) * math.tan(PITCH)


# Rückwand aus Kalkstein bis unter das Dach, mit dunkler Sockelleiste
box((3.50, 0.12, roof_z(WALL_Y) - 0.03), (0.0, WALL_Y, (roof_z(WALL_Y) - 0.03) / 2), material=LIME)
box((3.52, 0.14, 0.12), (0.0, WALL_Y, 0.06), material=LIME_DARK)
# Bemalung wie in Pompeji: rote Sockelzone mit ockerfarbenem Abschlussband
box((3.46, 0.01, 0.80), (0.0, WALL_Y - 0.065, 0.52), material=RED)
box((3.46, 0.012, 0.05), (0.0, WALL_Y - 0.066, 0.945), material=OCHRE)
# Drei Säulen mit Basis und Kapitell, darüber der Architrav
for x in (-1.45, 0.0, 1.45):
    box((0.20, 0.20, 0.08), (x, COL_Y, 0.04), material=LIME_DARK)
    cyl(0.075, 1.60, (x, COL_Y, 0.88), material=LIME, verts=12)
    box((0.22, 0.22, 0.11), (x, COL_Y, 1.735), material=LIME_DARK)
box((3.30, 0.20, 0.18), (0.0, COL_Y, 1.88), material=LIME)
# Dachbalken von der Wand zum Architrav, an den Enden sichtbar
for x in (-1.45, -0.72, 0.0, 0.72, 1.45):
    box((0.08, WALL_Y - COL_Y + 0.12, 0.08), (x, (WALL_Y + COL_Y) / 2, (roof_z(COL_Y) + roof_z(WALL_Y)) / 2 - 0.08),
        rot=(math.degrees(PITCH), 0, 0), material=WOOD_DARK)

# Ziegeldach: Tegulae als Platte mit Stoßfugen, Imbrices als Halbrohre, Stirnziegel an der vorderen Traufe
run = BACK_Y - EAVE_Y
length = run / math.cos(PITCH)
roof_w = ROOF_X1 - ROOF_X0
mid_y = (EAVE_Y + BACK_Y) / 2
box((roof_w, length, T), (0.0, mid_y, roof_z(mid_y)), rot=(math.degrees(PITCH), 0, 0), material=TILE)
normal = (0.0, -math.sin(PITCH), math.cos(PITCH))
for k in range(1, int(length / 0.42) + 1):
    d = k * 0.42 - 0.02
    if d >= length:
        break
    py = EAVE_Y + d * math.cos(PITCH)
    box((roof_w, 0.02, 0.012), (0.0, py + normal[1] * T / 2, roof_z(py) + normal[2] * T / 2),
        rot=(math.degrees(PITCH), 0, 0), material=TILE_DARK)
count = int(roof_w / 0.24)
spacing = roof_w / count
for i in range(count + 1):
    x = ROOF_X0 + i * spacing
    lift = T / 2 + 0.015
    cyl(0.055, length, (x, mid_y + normal[1] * lift, roof_z(mid_y) + normal[2] * lift), rot=(-(90 - math.degrees(PITCH)), 0, 0),
        material=TILE if random.random() > 0.3 else TILE_DARK, verts=8)
    box((0.11, 0.025, 0.12), (x, EAVE_Y - 0.01, roof_z(EAVE_Y) + 0.04), rot=(6, 0, 0), material=TILE_DARK)
box((roof_w, 0.04, 0.10), (0.0, EAVE_Y - 0.02, roof_z(EAVE_Y) - 0.05), material=WOOD_DARK)          # Traufbrett
for x in (ROOF_X0, ROOF_X1):                                                                       # Ortgang
    box((0.04, length, 0.10), (x, mid_y, roof_z(mid_y) - 0.05), rot=(math.degrees(PITCH), 0, 0), material=WOOD_DARK)

# Gemauerte Theke unter der Halle
CTR_Y, CTR_TOP = 1.35, 0.88
box((2.60, 0.34, 0.85), (0.0, CTR_Y, 0.425), material=LIME_DARK)
box((2.70, 0.44, 0.06), (0.0, CTR_Y, 0.85), material=LIME)
for x in (-1.15, -1.0):
    jug(x, CTR_Y, CTR_TOP, 0.06)
basket(-0.62, CTR_Y, CTR_TOP, 0.14, OCHRE)
for k in range(3):                                                          # Brotlaibe
    sphere(0.07, (-0.20 + k * 0.13, CTR_Y + (0.05 if k % 2 else -0.05), CTR_TOP + 0.03), (1.0, 0.8, 0.5), WOOD, segs=10, rings=5)
basket(0.40, CTR_Y, CTR_TOP, 0.13, RED)
for k in range(2):                                                          # Ölkrüge
    jug(0.80 + k * 0.17, CTR_Y + 0.04 * (1 - 2 * k), CTR_TOP, 0.065, TERRACOTTA)
basket(1.15, CTR_Y, CTR_TOP, 0.12, OLIVE)
# Amphoren an der Rückwand neben der Theke (dahinter wären sie verdeckt)
for x, y in ((-1.58, 1.55), (-1.58, 1.33), (1.58, 1.55)):
    amphora(x, y, 0.0)

# ---------------------------------------------------------------- Stände links und rechts


def stall(cx, cy, cloth_a, cloth_b, goods):
    """Holzstand mit gestreiftem Stoffdach, das zur Platzmitte hin abfällt; Theke auf der Innenseite."""
    inner = 1 if cx < 0 else -1
    half_x, half_y = 0.42, 0.60
    x_out, x_in = cx - inner * half_x, cx + inner * half_x
    z_out, drop, reach = 1.80, 0.28, 2 * half_x + 0.12
    beta = math.atan2(drop, reach)

    def awning_z(x):
        return z_out - abs(x - x_out) * math.tan(beta)

    for x in (x_out, x_in):
        for y in (cy - half_y, cy + half_y):
            h = awning_z(x) - 0.02
            box((0.07, 0.07, h), (x, y, h / 2), material=WOOD_DARK)
        box((0.06, 2 * half_y + 0.08, 0.06), (x, cy, awning_z(x) - 0.05), material=WOOD_DARK)   # Längsriegel
    # Stoffbahnen in der Neigung, abwechselnd in zwei Farben, vorn ein Behang
    stripes = 6
    w = (2 * half_y + 0.16) / stripes
    edge_x = x_out + inner * reach
    for k in range(stripes):
        y = cy - half_y - 0.08 + (k + 0.5) * w
        cloth = cloth_a if k % 2 == 0 else cloth_b
        box((reach / math.cos(beta), w, 0.02), ((x_out + edge_x) / 2, y, (z_out + awning_z(edge_x)) / 2),
            rot=(0, inner * math.degrees(beta), 0), material=cloth)
        box((0.015, w, 0.13), (edge_x, y, awning_z(edge_x) - 0.065), material=cloth)
    # Theke zur Platzmitte mit Tuch davor
    tx = cx + inner * 0.25
    box((0.34, 2 * half_y - 0.05, 0.78), (tx, cy, 0.39), material=WOOD)
    box((0.40, 2 * half_y + 0.02, 0.04), (tx, cy, 0.80), material=WOOD_DARK)
    box((0.015, 2 * half_y - 0.15, 0.32), (tx + inner * 0.175, cy, 0.60), material=cloth_a)
    # Hinten Kiste und Säcke
    bx = cx - inner * 0.18
    box((0.30, 0.30, 0.30), (bx, cy + 0.32, 0.15), rot=(0, 0, 8), material=WOOD)
    for k in range(2):
        sack((bx, cy - 0.12 - k * 0.24, 0.0))
    goods(tx, cy, 0.82)


def grain_bread_cheese(x, cy, z):
    basket(x, cy - 0.40, z, 0.13, OCHRE)
    basket(x, cy - 0.10, z, 0.11, OCHRE)
    for k in range(3):
        sphere(0.065, (x + (0.05 if k % 2 else -0.05), cy + 0.14 + k * 0.08, z + 0.03), (0.8, 1.0, 0.5), WOOD, segs=10, rings=5)
    for k in range(2):
        cyl(0.08, 0.06, (x, cy + 0.44, z + 0.03 + k * 0.062), material=OCHRE, verts=12)


def fruit(x, cy, z):
    for by in (cy - 0.38, cy - 0.05):
        cyl(0.13, 0.06, (x, by, z + 0.03), material=TERRACOTTA, verts=12)
        for k in range(7):
            a = k * 2 * math.pi / 6
            r = 0.0 if k == 6 else 0.07
            sphere(0.036, (x + math.cos(a) * r, by + math.sin(a) * r, z + 0.075 + (0.03 if k == 6 else 0.0)), material=RED, segs=8, rings=4)
    cyl(0.11, 0.05, (x, cy + 0.26, z + 0.025), material=TERRACOTTA, verts=12)
    sphere(0.10, (x, cy + 0.26, z + 0.05), (1.0, 1.0, 0.4), OLIVE, segs=10, rings=5)
    basket(x, cy + 0.48, z, 0.10, GREEN)


stall(-1.30, -0.15, RED, OCHRE, grain_bread_cheese)
stall(1.30, -0.15, GREEN, OCHRE, fruit)

# ---------------------------------------------------------------- Vorn: Eichtisch, Amphorengestell, Handkarren

# Mensa Ponderaria: Kalksteinplatte auf zwei Blöcken, Mulden der amtlichen Hohlmaße
MX, MY = -1.20, -1.10
for s in (-1, 1):
    box((0.12, 0.32, 0.74), (MX + s * 0.33, MY, 0.37), material=LIME_DARK)
box((0.92, 0.42, 0.08), (MX, MY, 0.78), material=LIME)
for k, r in enumerate((0.11, 0.085, 0.065, 0.045)):
    cyl(r, 0.012, (MX - 0.30 + sum((0.11, 0.085, 0.065, 0.045)[:k]) * 2 + r + k * 0.03, MY, 0.817), material=SHADOW, verts=12)

# Gestell mit vier Amphoren an der Vorderkante links
RY = -1.62
for x in (-1.68, -0.78):
    box((0.05, 0.05, 0.50), (x, RY, 0.25), material=WOOD_DARK)
for z in (0.12, 0.34):
    box((0.95, 0.04, 0.04), (-1.23, RY - 0.11, z), material=WOOD_DARK)
for x in (-1.55, -1.33, -1.11, -0.89):
    amphora(x, RY, 0.0)

# Handkarren rechts vorn, Deichsel zur Platzmitte, mit drei Säcken
at = placed((1.15, -1.30), -90)
CART_A = -90
box((0.55, 0.85, 0.05), at((0.0, 0.0, 0.36)), rot=(0, 0, CART_A), material=WOOD)
for sx in (-1, 1):
    box((0.04, 0.85, 0.16), at((sx * 0.275, 0.0, 0.46)), rot=(0, 0, CART_A), material=WOOD_DARK)
    cyl(0.22, 0.06, at((sx * 0.34, 0.1, 0.22)), rot=(0, 90, CART_A), material=WOOD_DARK, verts=12)
    cyl(0.05, 0.08, at((sx * 0.36, 0.1, 0.22)), rot=(0, 90, CART_A), material=WOOD, verts=8)
    box((0.04, 0.6, 0.04), at((sx * 0.22, -0.62, 0.32)), rot=(-20, 0, CART_A), material=WOOD_DARK)
box((0.55, 0.04, 0.16), at((0.0, 0.425, 0.46)), rot=(0, 0, CART_A), material=WOOD_DARK)
cyl(0.025, 0.7, at((0.0, 0.1, 0.22)), rot=(0, 90, CART_A), material=WOOD_DARK, verts=6)
for lx, ly in ((-0.13, -0.22), (0.13, -0.05), (-0.05, 0.20)):
    sack(at((lx, ly, 0.385)), 0.10)

# Ecksteine an den Grenzen (wie bei den anderen Modellen: gleiche Ausdehnung, nicht zu sehen)
for x in (-1.8, 1.8):
    for y in (-1.8, 1.8):
        box((0.04, 0.04, 0.02), (x, y, 0.01), material=LIME_DARK)

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
    cam.location = (4.6, -6.4, 4.4)
    direction = Vector((0.0, 0.0, 0.7)) - cam.location
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    cam_data.lens = 42
    scene.camera = cam
    scene.render.filepath = PREVIEW
    bpy.ops.render.render(write_still=True)
