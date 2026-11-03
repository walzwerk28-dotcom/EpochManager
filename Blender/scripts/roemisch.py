"""
Gemeinsame Bausteine für römische Gebäude (Antike): Materialien, Grundformen, Ziegeldächer, Türen, Fenster mit Läden
und kleine Dinge für Höfe und Gärten. Ein Modell-Script setzt seine Teile mit diesen Funktionen zusammen, schließt jede
Variante mit variant() ab und ruft am Ende export() auf.

Beim Import setzt das Modul die Szene zurück und legt die Materialien an; die Aufrufargumente
(-- <blend> <fbx> [<vorschau.png>]) liest es selbst.

Maße wie bei Markt und Lager: 1 Einheit = 1 m, Boden bei z = 0, Vorderseite (Straße) nach -Y. Ein Stockwerk der Häuser
ist STOREY hoch (etwa 1 m wie bei den übrigen Häusern im Spiel); Türen, Fenster und Dinge, an denen man Menschen misst,
sind dazu passend verkleinert. Farben sind sRGB-Werte, mat() rechnet sie linear um.
"""
import math
import random
import sys

import bmesh
import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
BLEND = args[0] if len(args) > 0 else None
FBX = args[1] if len(args) > 1 else None
PREVIEW = args[2] if len(args) > 2 else None

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
STONE = mat("Bruchstein", (0.58, 0.55, 0.50))
PLASTER_WHITE = mat("PutzWeiss", (0.92, 0.89, 0.82))
PLASTER_OCHRE = mat("PutzOcker", (0.90, 0.76, 0.52))
PLASTER_PINK = mat("PutzRosa", (0.90, 0.72, 0.64))
SOCLE_RED = mat("SockelRot", (0.66, 0.26, 0.18))
DARK = mat("Innenraum", (0.16, 0.13, 0.11))
WOOD = mat("Holz", (0.62, 0.45, 0.28))
WOOD_DARK = mat("HolzDunkel", (0.42, 0.29, 0.18))
PAINT_GREEN = mat("FarbeGruen", (0.34, 0.48, 0.36))
PAINT_BLUE = mat("FarbeBlau", (0.34, 0.45, 0.58))
IRON = mat("Eisen", (0.24, 0.23, 0.22), rough=0.6)
TILE = mat("Ziegel", (0.74, 0.37, 0.22))
TILE_DARK = mat("ZiegelDunkel", (0.62, 0.29, 0.17))
TERRACOTTA = mat("Terrakotta", (0.76, 0.46, 0.30))
SOIL = mat("Erde", (0.45, 0.33, 0.22))
LEAF = mat("Laub", (0.38, 0.55, 0.26))
LEAF_DARK = mat("LaubDunkel", (0.27, 0.42, 0.20))
OLIVE_LEAF = mat("Olivenlaub", (0.52, 0.58, 0.40))
STRAW = mat("Stroh", (0.86, 0.74, 0.46))
FEATHER = mat("Federn", (0.94, 0.92, 0.86))
COMB = mat("Kamm", (0.78, 0.18, 0.14))
SACK = mat("Sack", (0.80, 0.72, 0.56))
CLOTH = mat("Tuch", (0.88, 0.86, 0.80))

STOREY = 1.0       # Stockwerkshöhe der Häuser
PITCH = 24.0       # Dachneigung in Grad
T = 0.04           # Stärke der Dachplatte
S = 0.55           # Maßstab der Dinge, an denen man Menschen misst (wie Markt und Lager)

PARTS = []
VARIANTS = []


# ---------------------------------------------------------------- Grundformen


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


def cyl(radius, depth, loc, rot=(0.0, 0.0, 0.0), material=WOOD, verts=10):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=loc,
                                        rotation=[math.radians(a) for a in rot])
    return keep(bpy.context.active_object, material)


def cone(r_bottom, r_top, depth, loc, material, verts=10, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r_bottom, radius2=r_top, depth=depth, location=loc,
                                    rotation=[math.radians(a) for a in rot])
    return keep(bpy.context.active_object, material)


def sphere(radius, loc, scale=(1.0, 1.0, 1.0), material=LEAF, segs=8, rings=5, rot=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings, radius=radius, location=loc,
                                         rotation=[math.radians(a) for a in rot])
    obj = bpy.context.active_object
    obj.scale = scale
    return keep(obj, material)


def slab(points, plane, offset, depth, material):
    """Vieleck in der Ebene 'xz' (Punkte x, z; gezogen in y) oder 'yz' (Punkte y, z; gezogen in x) von offset bis
    offset + depth; auch nicht konvexe Umrisse werden sauber in Dreiecke zerlegt (Giebel, Schrägen)."""
    n = len(points)
    if plane == "xz":
        verts = [(a, offset, b) for a, b in points] + [(a, offset + depth, b) for a, b in points]
    else:
        verts = [(offset, a, b) for a, b in points] + [(offset + depth, a, b) for a, b in points]
    faces = [tuple(range(n))[::-1], tuple(range(n, 2 * n))] + [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
    mesh = bpy.data.meshes.new("Teil")
    mesh.from_pydata(verts, [], faces)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4], ngon_method="EAR_CLIP")
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Teil", mesh)
    bpy.context.collection.objects.link(obj)
    return keep(obj, material)


def bar(p0, p1, thick, material):
    """Vierkantstab von p0 nach p1 (Balken, Streben, Latten)."""
    a, b = Vector(p0), Vector(p1)
    d = b - a
    obj = box((thick, thick, d.length), tuple((a + b) / 2), material=material)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = d.to_track_quat("Z", "Y")
    return obj


# ---------------------------------------------------------------- Hauskörper und Dächer


def body(x0, x1, y0, y1, h, plaster, socle=None):
    """Verputzter Hauskörper als Block auf einem Sockel aus Bruchstein, wahlweise mit gemalter Sockelzone."""
    box((x1 - x0, y1 - y0, h), ((x0 + x1) / 2, (y0 + y1) / 2, h / 2), material=plaster)
    box((x1 - x0 + 0.04, y1 - y0 + 0.04, 0.09), ((x0 + x1) / 2, (y0 + y1) / 2, 0.045), material=STONE)
    if socle is not None:
        box((x1 - x0 + 0.012, y1 - y0 + 0.012, 0.18), ((x0 + x1) / 2, (y0 + y1) / 2, 0.18), material=socle)


def roof_side(axis, a0, a1, b_top, sign, run, z_top, pitch=PITCH, antefix=True):
    """Eine Dachfläche aus Tegulae und Imbrices. axis 'x': First parallel zu x, die Fläche fällt in y (sign +1 nach +y);
    axis 'y': First parallel zu y, sie fällt in x. b_top = Lage des Firsts quer zur Achse, run = waagerechte Länge bis
    zur Traufe, z_top = Oberkante der Wand unter dem First (Unterseite der Platte)."""
    p = math.radians(pitch)
    L = run / math.cos(p)

    def P(a, b, z):
        return (a, b, z) if axis == "x" else (b, a, z)

    def Sz(sa, sb, sz):
        return (sa, sb, sz) if axis == "x" else (sb, sa, sz)

    if axis == "x":
        slab_rot, imbrex_rot = (-sign * pitch, 0, 0), (-sign * (90 + pitch), 0, 0)
    else:
        slab_rot, imbrex_rot = (0, sign * pitch, 0), (0, sign * (90 + pitch), 0)
    nb, nz = sign * math.sin(p), math.cos(p)
    mid_b, mid_z = b_top + sign * run / 2, z_top - run / 2 * math.tan(p)
    width = a1 - a0
    box(Sz(width, L, T), P((a0 + a1) / 2, mid_b + nb * T / 2, mid_z + nz * T / 2), rot=slab_rot, material=TILE)
    for k in range(1, int(L / 0.28) + 1):                                     # Stoßfugen parallel zum First
        d = k * 0.28
        if d >= L - 0.05:
            break
        hb = d * math.cos(p)
        box(Sz(width, 0.014, 0.01), P((a0 + a1) / 2, b_top + sign * hb + nb * T, z_top - hb * math.tan(p) + nz * T),
            rot=slab_rot, material=TILE_DARK)
    count = max(1, round(width / 0.18))
    lift = T + 0.01
    eave_b, eave_z = b_top + sign * run, z_top - run * math.tan(p)
    for i in range(count + 1):
        a = a0 + 0.035 + i * (width - 0.07) / count    # äußere Reihen bündig mit der Dachkante
        cyl(0.036, L, P(a, mid_b + nb * lift, mid_z + nz * lift), rot=imbrex_rot,
            material=TILE if random.random() > 0.3 else TILE_DARK, verts=8)
        if antefix:
            box(Sz(0.07, 0.016, 0.075), P(a, eave_b + sign * 0.008, eave_z + 0.04), material=TILE_DARK)
    box(Sz(width, 0.03, 0.06), P((a0 + a1) / 2, eave_b - sign * 0.01, eave_z - 0.02), material=WOOD_DARK)   # Traufbrett
    return eave_z


def gable_roof(x0, x1, y0, y1, h, axis, plaster, overhang=0.09, pitch=PITCH):
    """Satteldach über dem Rechteck der Wände mit Giebeldreiecken in der Wandfarbe; axis 'x' = First parallel zu x
    (Traufen vorn und hinten), 'y' = First parallel zu y (Giebel zur Straße). Gibt die Firsthöhe zurück."""
    p = math.radians(pitch)
    if axis == "x":
        a0, a1, b0, b1 = x0 - overhang, x1 + overhang, y0, y1
    else:
        a0, a1, b0, b1 = y0 - overhang, y1 + overhang, x0, x1
    half = (b1 - b0) / 2
    bc = (b0 + b1) / 2
    ridge = h + half * math.tan(p)
    for sign in (-1, 1):
        roof_side(axis, a0, a1, bc, sign, half + overhang, ridge, pitch)
    # Giebel: Dreieck in Wandstärke über die ganze Länge (füllt den Dachraum)
    tri = [(b0, h), (b1, h), (bc, ridge - 0.02)]
    if axis == "x":
        slab(tri, "yz", x0, x1 - x0, plaster)
    else:
        slab(tri, "xz", y0, y1 - y0, plaster)
    # Firstziegel und Ortgangbretter an beiden Giebeln
    length = a1 - a0
    rot = (0, 90, 0) if axis == "x" else (90, 0, 0)
    pos = ((a0 + a1) / 2, bc, ridge + T + 0.025) if axis == "x" else (bc, (a0 + a1) / 2, ridge + T + 0.025)
    cyl(0.05, length + 0.02, pos, rot=rot, material=TILE_DARK, verts=10)
    for a in (a0, a1):
        for sign in (-1, 1):
            run = half + overhang
            mid_b = bc + sign * run / 2
            mid_z = ridge - run / 2 * math.tan(p) - 0.02
            L = run / math.cos(p)
            if axis == "x":
                box((0.03, L, 0.06), (a, mid_b, mid_z), rot=(-sign * pitch, 0, 0), material=WOOD_DARK)
            else:
                box((L, 0.03, 0.06), (mid_b, a, mid_z), rot=(0, sign * pitch, 0), material=WOOD_DARK)
    return ridge


def shed_roof(x0, x1, y0, y1, z_high, low_side, overhang=0.08, pitch=16.0):
    """Pultdach über einem Rechteck; low_side '-x', '+x', '-y' oder '+y' ist die Traufseite. z_high = Unterkante der
    Platte an der hohen Seite. Gibt die Höhe an der Traufe zurück."""
    sign = 1 if low_side[0] == "+" else -1
    if low_side[1] == "y":
        a0, a1 = x0 - overhang, x1 + overhang
        b_top = y0 if sign > 0 else y1
        run = (y1 - y0) + overhang
        return roof_side("x", a0, a1, b_top, sign, run, z_high, pitch)
    a0, a1 = y0 - overhang, y1 + overhang
    b_top = x0 if sign > 0 else x1
    run = (x1 - x0) + overhang
    return roof_side("y", a0, a1, b_top, sign, run, z_high, pitch)


# ---------------------------------------------------------------- Türen und Fenster

FACES = {"-y": (0, -1), "+y": (0, 1), "-x": (-1, 0), "+x": (1, 0)}


def face_frame(face, plane):
    """Richtung entlang der Wand (u) und nach außen (n) für eine Wand bei x = plane bzw. y = plane."""
    nx, ny = FACES[face]
    ux, uy = (1, 0) if ny != 0 else (0, 1)

    def at(u, out, z):
        if ny != 0:
            return (u, plane + ny * out, z)
        return (plane + nx * out, u, z)

    def size(su, sn, sz):
        return (su, sn, sz) if ny != 0 else (sn, su, sz)
    return at, size, (ux, uy), (nx, ny)


def door(face, plane, u, w=0.32, h=0.62, z0=0.09, frame=LIME_DARK, leaf=WOOD, open_angle=0):
    """Bohlentür mit Gewände, Sturz und Stufe; open_angle > 0 öffnet sie nach innen (dahinter dunkel)."""
    at, size, (ux, uy), (nx, ny) = face_frame(face, plane)
    box(size(w, 0.012, h), at(u, 0.004, z0 + h / 2), material=DARK)
    for s in (-1, 1):
        box(size(0.045, 0.03, h + 0.02), at(u + s * (w / 2 + 0.02), 0.012, z0 + h / 2), material=frame)
    box(size(w + 0.12, 0.035, 0.05), at(u, 0.015, z0 + h + 0.02), material=frame)
    if z0 < 0.2:                                                              # Stufe nur am Boden
        box(size(w + 0.08, 0.09, 0.04), at(u, 0.045, 0.02), material=STONE)
    if open_angle == 0:
        box(size(w - 0.01, 0.02, h - 0.01), at(u, 0.012, z0 + h / 2), material=leaf)
        for k in (-1, 0, 1):
            box(size(0.006, 0.006, h - 0.03), at(u + k * w / 4, 0.024, z0 + h / 2), material=WOOD_DARK)
        for z in (0.16, h - 0.14):
            box(size(w - 0.04, 0.006, 0.022), at(u, 0.025, z0 + z), material=IRON)
    else:
        # Flügel am Gewände angeschlagen, ins Haus gedreht (zeigt die dunkle Öffnung)
        a = math.radians(open_angle)
        hinge = Vector(at(u - w / 2, -0.01, 0.0)[:2])
        d = Vector((ux, uy)) * math.cos(a) - Vector((nx, ny)) * math.sin(a)
        c = hinge + d * (w / 2)
        yaw = math.degrees(math.atan2(d.y, d.x))
        box((w - 0.01, 0.02, h - 0.01), (c.x, c.y, z0 + h / 2), rot=(0, 0, yaw), material=leaf)


def window(face, plane, u, z, w=0.20, h=0.26, shutters=WOOD, open_shutters=True, frame=LIME):
    """Fensteröffnung mit Bank und Sturz; Läden offen (20° von der Wand) oder geschlossen, None = ohne Läden."""
    at, size, (ux, uy), (nx, ny) = face_frame(face, plane)
    box(size(w, 0.012, h), at(u, 0.004, z + h / 2), material=DARK)
    box(size(w + 0.06, 0.045, 0.025), at(u, 0.02, z - 0.012), material=frame)
    box(size(w + 0.05, 0.02, 0.035), at(u, 0.008, z + h + 0.018), material=TILE_DARK)
    if shutters is None:
        for k in (-1, 1):
            box(size(0.012, 0.006, h), at(u + k * w / 6, 0.012, z + h / 2), material=WOOD_DARK)   # Gitterstäbe
        return
    half = w / 2
    if not open_shutters:
        box(size(w, 0.018, h), at(u, 0.012, z + h / 2), material=shutters)
        box(size(0.006, 0.006, h), at(u, 0.023, z + h / 2), material=WOOD_DARK)
        return
    a = math.radians(20)
    for s in (-1, 1):
        hinge = Vector(at(u + s * half, 0.012, 0.0)[:2])
        d = Vector((ux, uy)) * (s * math.cos(a)) + Vector((nx, ny)) * math.sin(a)
        c = hinge + d * (half / 2)
        yaw = math.degrees(math.atan2(d.y, d.x))
        box((half, 0.016, h), (c.x, c.y, z + h / 2), rot=(0, 0, yaw), material=shutters)
        for zz in (z + h * 0.25, z + h * 0.75):                               # Querleisten
            c2 = c + Vector((nx, ny)) * 0.01
            box((half * 0.9, 0.006, 0.018), (c2.x, c2.y, zz), rot=(0, 0, yaw), material=WOOD_DARK)


# ---------------------------------------------------------------- Dinge für Höfe und Gärten


def amphora(x, y, z0=0.0):
    """Spitzamphore (in Wirklichkeit 0,55 m): Bauch, kurzer Fuß, Hals, Lippe, zwei Henkel."""
    cone(0.015 * S, 0.06 * S, 0.12 * S, (x, y, z0 + 0.06 * S), TERRACOTTA, verts=8)
    sphere(0.095 * S, (x, y, z0 + 0.26 * S), (1.0, 1.0, 1.9), TERRACOTTA, segs=10, rings=6)
    cyl(0.035 * S, 0.12 * S, (x, y, z0 + 0.47 * S), material=TERRACOTTA, verts=8)
    cyl(0.048 * S, 0.025 * S, (x, y, z0 + 0.535 * S), material=TERRACOTTA, verts=8)
    for s in (-1, 1):
        box((0.02 * S, 0.02 * S, 0.13 * S), (x + s * 0.06 * S, y, z0 + 0.45 * S), rot=(0, s * 20, 0), material=TERRACOTTA)


def jug(x, y, z0, r, material=TERRACOTTA):
    """Bauchiger Krug mit Hals (r = Radius des Bauchs)."""
    sphere(r, (x, y, z0 + r * 0.9), (1.0, 1.0, 1.05), material, segs=10, rings=6)
    cyl(r * 0.45, r * 0.7, (x, y, z0 + r * 1.95), material=material, verts=8)
    cyl(r * 0.58, r * 0.18, (x, y, z0 + r * 2.3), material=material, verts=8)


def pot_plant(x, y, r=0.06, leaves=LEAF):
    """Terrakotta-Topf mit Busch (Kräuter, Lorbeer)."""
    cone(r * 0.7, r, r * 1.3, (x, y, r * 0.65), TERRACOTTA, verts=10)
    sphere(r * 1.15, (x, y, r * 1.3 + r * 0.8), (1.0, 1.0, 0.9), leaves, segs=8, rings=5)


def bench(x, y, yaw=0.0, length=0.40):
    """Steinbank: Platte auf zwei Blöcken."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    for k in (-1, 1):
        box((0.05, 0.10, 0.16), (x + c * k * length * 0.38, y + s * k * length * 0.38, 0.08), rot=(0, 0, yaw), material=LIME_DARK)
    box((length, 0.13, 0.035), (x, y, 0.175), rot=(0, 0, yaw), material=LIME)


def sack(x, y, z0, r):
    """Gefüllter Sack: bauchig, oben zusammengeschnürt mit Zipfel."""
    sphere(r, (x, y, z0 + r), (1.0, 0.85, 1.15), SACK, segs=8, rings=5)
    cyl(r * 0.28, r * 0.27, (x, y, z0 + r * 2.2), material=WOOD_DARK, verts=6)
    cone(r * 0.32, r * 0.12, r * 0.45, (x, y, z0 + r * 2.4), SACK, verts=6)


def veg_bed(x0, x1, y0, y1, rows_along="y", plant=LEAF):
    """Gemüsebeet: Erdbeet mit Brettkante und Reihen kleiner Pflanzen."""
    box((x1 - x0, y1 - y0, 0.05), ((x0 + x1) / 2, (y0 + y1) / 2, 0.025), material=SOIL)
    for x in (x0, x1):
        box((0.02, y1 - y0, 0.06), (x, (y0 + y1) / 2, 0.03), material=WOOD_DARK)
    for y in (y0, y1):
        box((x1 - x0, 0.02, 0.06), ((x0 + x1) / 2, y, 0.03), material=WOOD_DARK)
    if rows_along == "y":
        n = max(1, int((x1 - x0) / 0.12))
        for i in range(n):
            x = x0 + (i + 0.5) * (x1 - x0) / n
            m = max(1, int((y1 - y0) / 0.09))
            for j in range(m):
                y = y0 + (j + 0.5) * (y1 - y0) / m
                sphere(0.035, (x, y, 0.065), (1.0, 1.0, 0.8), plant if (i + j) % 3 else LEAF_DARK, segs=6, rings=4)
    else:
        n = max(1, int((y1 - y0) / 0.12))
        for i in range(n):
            y = y0 + (i + 0.5) * (y1 - y0) / n
            m = max(1, int((x1 - x0) / 0.09))
            for j in range(m):
                x = x0 + (j + 0.5) * (x1 - x0) / m
                sphere(0.035, (x, y, 0.065), (1.0, 1.0, 0.8), plant if (i + j) % 3 else LEAF_DARK, segs=6, rings=4)


def fence(points, h=0.30, material=WOOD_DARK):
    """Lattenzaun: Pfosten an den Punkten, zwei Riegel und Latten dazwischen."""
    for (xa, ya), (xb, yb) in zip(points, points[1:]):
        length = math.hypot(xb - xa, yb - ya)
        yaw = math.degrees(math.atan2(yb - ya, xb - xa))
        mx, my = (xa + xb) / 2, (ya + yb) / 2
        for z in (h * 0.35, h * 0.80):
            box((length, 0.018, 0.022), (mx, my, z), rot=(0, 0, yaw), material=material)
        n = max(1, int(length / 0.08))
        for k in range(1, n):
            t = k / n
            box((0.025, 0.012, h * 0.95), (xa + (xb - xa) * t, ya + (yb - ya) * t, h * 0.475), rot=(0, 0, yaw), material=WOOD)
    for x, y in points:
        box((0.035, 0.035, h + 0.03), (x, y, (h + 0.03) / 2), material=material)


def pergola(x0, x1, y0, y1, h=0.95, vine_post=0):
    """Weinlaube: vier Pfosten, Pfetten, Querlatten und Laub obenauf; am Pfosten vine_post rankt ein Rebstock."""
    posts = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    for x, y in posts:
        box((0.045, 0.045, h), (x, y, h / 2), material=WOOD_DARK)
    for y in (y0, y1):
        box((x1 - x0 + 0.10, 0.04, 0.04), ((x0 + x1) / 2, y, h + 0.02), material=WOOD_DARK)
    n = max(2, int((x1 - x0) / 0.16))
    for k in range(n + 1):
        x = x0 + k * (x1 - x0) / n
        box((0.025, y1 - y0 + 0.12, 0.025), (x, (y0 + y1) / 2, h + 0.055), material=WOOD)
    rnd = random.Random(int((x0 + y0) * 100))
    # dichtes Blätterdach aus flachen, überlappenden Büscheln, an den Rändern etwas herabhängend
    nx, ny = max(2, round((x1 - x0) / 0.13)), max(2, round((y1 - y0) / 0.13))
    for i in range(nx + 1):
        for j in range(ny + 1):
            edge = i in (0, nx) or j in (0, ny)
            if edge and rnd.random() < 0.35:
                continue
            x = x0 + i * (x1 - x0) / nx + rnd.uniform(-0.03, 0.03)
            y = y0 + j * (y1 - y0) / ny + rnd.uniform(-0.03, 0.03)
            z = h + 0.09 + rnd.uniform(-0.02, 0.03) - (0.04 if edge else 0.0)
            sphere(0.12, (x, y, z), (1.0, 1.0, 0.38), LEAF if rnd.random() > 0.4 else LEAF_DARK, segs=6, rings=4,
                   rot=(0, 0, rnd.uniform(0, 60)))
    vx, vy = posts[vine_post]
    for k in range(3):                                                         # Rebstock windet sich am Pfosten hoch
        z = 0.15 + k * h / 3
        sphere(0.05, (vx + 0.03 * math.cos(k * 2.1), vy + 0.03 * math.sin(k * 2.1), z + 0.12), (0.8, 0.8, 1.0), LEAF_DARK, segs=6, rings=4)
    cyl(0.02, h, (vx + 0.03, vy + 0.03, h / 2), material=WOOD_DARK, verts=6)
    for k in range(4):                                                         # Trauben
        gx = x0 + (k + 0.7) * (x1 - x0) / 4.5
        sphere(0.025, (gx, (y0 + y1) / 2 + rnd.uniform(-0.1, 0.1), h - 0.02), (0.8, 0.8, 1.3), WOOD_DARK, segs=6, rings=4)


def olive_tree(x, y, size=1.0):
    """Olivenbaum: knorriger Stamm aus zwei Stücken, drei silbrig-grüne Kronenteile."""
    cyl(0.045 * size, 0.40 * size, (x, y, 0.20 * size), rot=(6, -4, 0), material=WOOD_DARK, verts=7)
    cyl(0.035 * size, 0.30 * size, (x + 0.04 * size, y, 0.50 * size), rot=(0, 18, 0), material=WOOD_DARK, verts=7)
    for dx, dy, dz, r in ((0.0, 0.0, 0.78, 0.26), (0.16, 0.05, 0.68, 0.19), (-0.13, -0.06, 0.66, 0.18)):
        sphere(r * size, (x + dx * size, y + dy * size, dz * size), (1.0, 1.0, 0.75), OLIVE_LEAF, segs=8, rings=5)


def fig_tree(x, y, size=1.0):
    """Feigenbaum: kurzer Stamm, breite dunkle Krone."""
    cyl(0.04 * size, 0.42 * size, (x, y, 0.21 * size), material=WOOD_DARK, verts=7)
    sphere(0.30 * size, (x, y, 0.62 * size), (1.0, 1.0, 0.7), LEAF_DARK, segs=9, rings=5)
    sphere(0.20 * size, (x + 0.12 * size, y - 0.06 * size, 0.72 * size), (1.0, 1.0, 0.7), LEAF, segs=8, rings=5)


def oven(x, y, r=0.16):
    """Backofen aus Lehm: Kuppel auf einem Sockel, dunkle Öffnung nach -y."""
    box((r * 2.4, r * 2.4, 0.12), (x, y, 0.06), material=STONE)
    sphere(r, (x, y, 0.12), (1.0, 1.0, 0.85), LIME_DARK, segs=10, rings=5)
    box((r * 0.7, 0.03, r * 0.45), (x, y - r * 0.93, 0.12 + r * 0.22), material=DARK)


def woodpile(x, y, length=0.36, rows=(4, 3, 2), yaw=0.0):
    """Brennholzstapel: Scheite längs gestapelt (gedreht um yaw), Stirnseiten hell."""
    r = 0.028
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    for level, count in enumerate(rows):
        for k in range(count):
            off = (k - (count - 1) / 2) * 2 * r
            px, py = x - s * off, y + c * off
            z = r + level * r * 1.75
            cyl(r, length, (px, py, z), rot=(0, 90, yaw), material=WOOD, verts=6)


def chicken(x, y, yaw=0.0):
    """Huhn: Körper, Kopf, Kamm (etwa 13 cm hoch)."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    sphere(0.045, (x, y, 0.06), (1.3, 0.9, 0.9), FEATHER, segs=6, rings=4, rot=(0, 0, yaw))
    hx, hy = x + c * 0.05, y + s * 0.05
    sphere(0.022, (hx, hy, 0.11), material=FEATHER, segs=6, rings=4)
    box((0.006, 0.02, 0.02), (hx, hy, 0.135), rot=(0, 0, yaw + 90), material=COMB)


def coop(x, y, w=0.36, d=0.28, h=0.32):
    """Hühnerstall: Bretterkiste auf Stelzen mit Pultdach aus Brettern und Hühnerleiter."""
    for dx in (-1, 1):
        for dy in (-1, 1):
            box((0.03, 0.03, 0.10), (x + dx * (w / 2 - 0.02), y + dy * (d / 2 - 0.02), 0.05), material=WOOD_DARK)
    box((w, d, h - 0.10), (x, y, 0.10 + (h - 0.10) / 2), material=WOOD)
    for k in range(1, 4):
        box((0.006, d + 0.004, h - 0.12), (x - w / 2 + k * w / 4, y, 0.10 + (h - 0.10) / 2), material=WOOD_DARK)
    box((w + 0.08, d + 0.10, 0.025), (x, y, h + 0.02), rot=(-10, 0, 0), material=WOOD_DARK)
    box((0.07, 0.012, 0.08), (x, y - d / 2 - 0.006, 0.17), material=DARK)
    box((0.06, 0.16, 0.012), (x, y - d / 2 - 0.07, 0.07), rot=(-40, 0, 0), material=WOOD)


def washing_line(x0, y0, x1, y1, h=0.75, cloths=((CLOTH, 0.12), (PAINT_BLUE, 0.10), (CLOTH, 0.08))):
    """Wäscheleine zwischen zwei Pfosten mit hängenden Tüchern."""
    for x, y in ((x0, y0), (x1, y1)):
        box((0.025, 0.025, h), (x, y, h / 2), material=WOOD_DARK)
    length = math.hypot(x1 - x0, y1 - y0)
    yaw = math.degrees(math.atan2(y1 - y0, x1 - x0))
    box((length, 0.005, 0.005), ((x0 + x1) / 2, (y0 + y1) / 2, h - 0.03), rot=(0, 0, yaw), material=SACK)
    for k, (m, w) in enumerate(cloths):
        t = (k + 1) / (len(cloths) + 1)
        box((w, 0.006, 0.14), (x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, h - 0.10), rot=(0, 0, yaw), material=m)


# ---------------------------------------------------------------- Varianten und Export


def variant(name):
    """Schließt die bisher gebauten Teile zu einem Objekt (eine Variante) mit einem Material je Farbe zusammen."""
    global PARTS
    bpy.ops.object.select_all(action="DESELECT")
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.material_slot_remove_unused()
    dims = obj.dimensions
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(f"{name}: {dims.x:.2f} x {dims.y:.2f} x {dims.z:.2f} m, {tris} Dreiecke, {len(obj.material_slots)} Materialien")
    VARIANTS.append(obj)
    PARTS = []
    return obj


def export(spacing):
    """Speichert die .blend, exportiert alle Varianten als einzelne Objekte in eine FBX (jede in ihrem eigenen
    Ursprung) und rendert auf Wunsch ein Vorschaubild, in dem die Varianten nebeneinander stehen (spacing in x)."""
    if BLEND:
        bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in VARIANTS:
        obj.select_set(True)
    if FBX:
        bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={"MESH"},
                                 apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                                 axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                                 mesh_smooth_type="FACE", add_leaf_bones=False, use_mesh_modifiers=True)
    if PREVIEW:
        for i, obj in enumerate(VARIANTS):
            obj.location.x = (i - (len(VARIANTS) - 1) / 2) * spacing
        scene = bpy.context.scene
        scene.render.engine = "BLENDER_WORKBENCH"
        scene.display.shading.color_type = "MATERIAL"
        scene.display.shading.light = "STUDIO"
        scene.display.shading.show_shadows = True
        scene.render.resolution_x, scene.render.resolution_y = 1400, 800
        cam_data = bpy.data.cameras.new("Kamera")
        cam = bpy.data.objects.new("Kamera", cam_data)
        scene.collection.objects.link(cam)
        width = spacing * len(VARIANTS)
        cam.location = (width * 0.35, -width * 0.95, width * 0.55)
        cam.rotation_euler = (Vector((0.0, 0.0, 0.5)) - cam.location).to_track_quat("-Z", "Y").to_euler()
        cam_data.lens = 40
        scene.camera = cam
        scene.render.filepath = PREVIEW
        bpy.ops.render.render(write_still=True)
