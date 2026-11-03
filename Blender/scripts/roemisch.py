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
from mathutils import Matrix, Vector

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
CLOTH_RED = mat("TuchRot", (0.72, 0.24, 0.18))
CLOTH_OCHRE = mat("TuchOcker", (0.88, 0.70, 0.36))
WATER = mat("Wasser", (0.42, 0.70, 0.85), rough=0.2)

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


def shed_roof(x0, x1, y0, y1, z_high, low_side, overhang=0.08, pitch=16.0, eave_overhang=None):
    """Pultdach über einem Rechteck; low_side '-x', '+x', '-y' oder '+y' ist die Traufseite. z_high = Unterkante der
    Platte an der hohen Seite. overhang gilt an den Seiten, eave_overhang (Standard wie overhang) an der Traufe.
    Gibt die Höhe an der Traufe zurück."""
    sign = 1 if low_side[0] == "+" else -1
    eave = overhang if eave_overhang is None else eave_overhang
    if low_side[1] == "y":
        a0, a1 = x0 - overhang, x1 + overhang
        b_top = y0 if sign > 0 else y1
        run = (y1 - y0) + eave
        return roof_side("x", a0, a1, b_top, sign, run, z_high, pitch)
    a0, a1 = y0 - overhang, y1 + overhang
    b_top = x0 if sign > 0 else x1
    run = (x1 - x0) + eave
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
            m = max(1, int((y1 - y0) / 0.13))
            for j in range(m):
                y = y0 + (j + 0.5) * (y1 - y0) / m
                sphere(0.04, (x, y, 0.065), (1.0, 1.0, 0.8), plant if (i + j) % 3 else LEAF_DARK, segs=5, rings=3)
    else:
        n = max(1, int((y1 - y0) / 0.12))
        for i in range(n):
            y = y0 + (i + 0.5) * (y1 - y0) / n
            m = max(1, int((x1 - x0) / 0.13))
            for j in range(m):
                x = x0 + (j + 0.5) * (x1 - x0) / m
                sphere(0.04, (x, y, 0.065), (1.0, 1.0, 0.8), plant if (i + j) % 3 else LEAF_DARK, segs=5, rings=3)


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


def crate(x, y, z0=0.0, size=0.18, yaw=0.0):
    """Kiste aus Brettern mit dunklen Kanten."""
    box((size, size, size), (x, y, z0 + size / 2), rot=(0, 0, yaw), material=WOOD)
    for z in (0.30, 0.94):
        box((size + 0.01, size + 0.01, size * 0.11), (x, y, z0 + size * z), rot=(0, 0, yaw), material=WOOD_DARK)


def hay(x, y, r=0.20):
    """Heuhaufen: flache Kuppel mit Spitze."""
    sphere(r, (x, y, r * 0.35), (1.0, 1.0, 0.75), STRAW, segs=10, rings=5)
    cone(r * 0.55, 0.0, r * 0.45, (x, y, r * 0.95), STRAW, verts=10)


def placed(origin, angle):
    """Teile einer Gruppe in eigenen Koordinaten setzen (um z gedreht und verschoben)."""
    m = Matrix.Rotation(math.radians(angle), 4, "Z")

    def at(local):
        return tuple(m @ Vector(local) + Vector((origin[0], origin[1], 0.0)))
    return at


def cart(x, y, yaw=0.0, load="sacks"):
    """Handkarren mit zwei Rädern, Deichsel nach -y (vor der Drehung); Ladung 'sacks', 'hay' oder None."""
    at = placed((x, y), yaw)
    box((0.55 * S, 0.85 * S, 0.05 * S), at((0.0, 0.0, 0.36 * S)), rot=(0, 0, yaw), material=WOOD)
    for sx in (-1, 1):
        box((0.04 * S, 0.85 * S, 0.16 * S), at((sx * 0.275 * S, 0.0, 0.46 * S)), rot=(0, 0, yaw), material=WOOD_DARK)
        cyl(0.22 * S, 0.06 * S, at((sx * 0.34 * S, 0.1 * S, 0.22 * S)), rot=(0, 90, yaw), material=WOOD_DARK, verts=12)
        cyl(0.05 * S, 0.08 * S, at((sx * 0.36 * S, 0.1 * S, 0.22 * S)), rot=(0, 90, yaw), material=WOOD, verts=8)
        box((0.04 * S, 0.6 * S, 0.04 * S), at((sx * 0.22 * S, -0.62 * S, 0.32 * S)), rot=(-20, 0, yaw), material=WOOD_DARK)
    box((0.55 * S, 0.04 * S, 0.16 * S), at((0.0, 0.425 * S, 0.46 * S)), rot=(0, 0, yaw), material=WOOD_DARK)
    cyl(0.025 * S, 0.7 * S, at((0.0, 0.1 * S, 0.22 * S)), rot=(0, 90, yaw), material=WOOD_DARK, verts=6)
    if load == "sacks":
        for lx, ly in ((-0.10, 0.15), (0.11, -0.08)):
            p = at((lx * S, ly * S, 0.0))
            sack(p[0], p[1], 0.385 * S, 0.10 * S)
    elif load == "hay":
        p = at((0.0, 0.0, 0.0))
        sphere(0.20 * S, (p[0], p[1], 0.47 * S), (1.3, 1.9, 0.8), STRAW, segs=8, rings=5, rot=(0, 0, yaw))


def cistern(x, y):
    """Zisterne im Hof: runder Steinrand mit dunklem Wasser, Holzgestell mit Welle, Seil und Eimer."""
    cyl(0.15, 0.22, (x, y, 0.11), material=LIME_DARK, verts=12)
    cyl(0.17, 0.03, (x, y, 0.235), material=LIME, verts=12)
    cyl(0.115, 0.01, (x, y, 0.245), material=DARK, verts=12)
    for s in (-1, 1):
        box((0.03, 0.03, 0.50), (x + s * 0.17, y, 0.25 + 0.25), material=WOOD_DARK)
    cyl(0.018, 0.38, (x, y, 0.72), rot=(0, 90, 0), material=WOOD, verts=8)
    box((0.006, 0.006, 0.26), (x, y, 0.58), material=SACK)
    cone(0.035, 0.045, 0.06, (x, y, 0.44), WOOD, verts=8)
    box((0.10, 0.012, 0.012), (x + 0.24, y, 0.72), material=WOOD_DARK)       # Kurbel


def court_wall(points, h=0.40, t=0.09, plaster=PLASTER_WHITE):
    """Hofmauer entlang der Punkte: verputzt auf Bruchsteinsockel, mit Ziegelabdeckung."""
    for (xa, ya), (xb, yb) in zip(points, points[1:]):
        length = math.hypot(xb - xa, yb - ya) + t
        yaw = math.degrees(math.atan2(yb - ya, xb - xa))
        mx, my = (xa + xb) / 2, (ya + yb) / 2
        box((length, t, h), (mx, my, h / 2), rot=(0, 0, yaw), material=plaster)
        box((length + 0.02, t + 0.03, 0.08), (mx, my, 0.04), rot=(0, 0, yaw), material=STONE)
        box((length + 0.02, t + 0.06, 0.035), (mx, my, h + 0.0175), rot=(0, 0, yaw), material=TILE_DARK)
        cyl(0.028, length + 0.02, (mx, my, h + 0.045), rot=(0, 90, yaw), material=TILE, verts=8)


def lean_to(x0, x1, y0, y1, h_low, low_side, plaster, pitch=14.0, overhang=0.07, eave_overhang=None, socle=None):
    """Flügel bzw. Anbau mit Pultdach (low_side '+x', '-x', '+y' oder '-y' ist die Traufseite): Block bis h_low, darüber
    ein Keil in Wandfarbe unter der Schräge. Gibt die Höhe an der hohen Seite zurück."""
    body(x0, x1, y0, y1, h_low, plaster, socle=socle)
    depth = (x1 - x0) if low_side[1] == "x" else (y1 - y0)
    rise = depth * math.tan(math.radians(pitch))
    if low_side[1] == "x":
        tri = [(x0, h_low), (x1, h_low), (x0, h_low + rise)] if low_side == "+x" else \
              [(x0, h_low), (x1, h_low), (x1, h_low + rise)]
        slab(tri, "xz", y0, y1 - y0, plaster)
    else:
        tri = [(y0, h_low), (y1, h_low), (y0, h_low + rise)] if low_side == "+y" else \
              [(y0, h_low), (y1, h_low), (y1, h_low + rise)]
        slab(tri, "yz", x0, x1 - x0, plaster)
    shed_roof(x0, x1, y0, y1, h_low + rise, low_side, overhang=overhang, pitch=pitch, eave_overhang=eave_overhang)
    return h_low + rise


def open_shed(x0, x1, y0, y1, h, plaster=None):
    """Offener Schuppen: Rückwand (Bretter oder Putz) an y1, Seitenwände halb hoch, vorn zwei Pfosten, Pultdach nach -y."""
    wall = plaster or WOOD
    box((x1 - x0, 0.06, h), ((x0 + x1) / 2, y1 - 0.03, h / 2), material=wall)
    if plaster is None:
        for k in range(1, int((x1 - x0) / 0.12)):
            box((0.008, 0.006, h - 0.02), (x0 + k * 0.12, y1 - 0.064, h / 2), material=WOOD_DARK)
    for x in (x0, x1):
        box((0.05, y1 - y0, h * 0.5), (x, (y0 + y1) / 2, h * 0.25), material=wall)
    rise = (y1 - y0) * math.tan(math.radians(16))
    low = h + 0.04
    for x in (x0 + 0.04, x1 - 0.04):
        box((0.05, 0.05, low), (x, y0 + 0.04, low / 2), material=WOOD_DARK)
    box((x1 - x0, 0.05, 0.05), ((x0 + x1) / 2, y0 + 0.04, low), material=WOOD_DARK)
    for x in (x0, x1):                                                         # Dreiecke unter dem Dach an den Seiten
        slab([(y0 + 0.02, low), (y1, low), (y1, low + rise)], "yz", x - 0.025, 0.05, WOOD_DARK)
    return shed_roof(x0, x1, y0, y1, low + rise, "-y", overhang=0.07, pitch=16)


def balcony(y_face, u0, u1, z, depth=0.20):
    """Holzbalkon an einer Wand nach -y (y_face = Wandfläche): Kragbalken, Bohlenboden, Geländer mit Stäben."""
    w = u1 - u0
    cx = (u0 + u1) / 2
    for k in range(max(2, int(w / 0.5)) + 1):
        x = u0 + 0.05 + k * (w - 0.10) / max(2, int(w / 0.5))
        box((0.04, depth + 0.02, 0.05), (x, y_face - depth / 2, z - 0.04), material=WOOD_DARK)
    box((w, depth, 0.03), (cx, y_face - depth / 2, z), material=WOOD)
    by = y_face - depth + 0.015
    box((w, 0.025, 0.025), (cx, by, z + 0.26), material=WOOD_DARK)
    for x in (u0 + 0.01, u1 - 0.01):
        box((0.025, depth, 0.025), (x, y_face - depth / 2, z + 0.26), material=WOOD_DARK)
    n = max(4, int(w / 0.09))
    for k in range(n + 1):
        box((0.012, 0.012, 0.24), (u0 + 0.01 + k * (w - 0.02) / n, by, z + 0.13), material=WOOD_DARK)
    for x in (u0 + 0.01, u1 - 0.01):
        for k in range(1, 4):
            box((0.012, 0.012, 0.24), (x, y_face - depth + k * depth / 4, z + 0.13), material=WOOD_DARK)


def path(points, w=0.16):
    """Weg aus unregelmäßigen Trittplatten zwischen den Punkten."""
    rnd = random.Random(int(points[0][0] * 77 + points[0][1] * 31))
    for (xa, ya), (xb, yb) in zip(points, points[1:]):
        length = math.hypot(xb - xa, yb - ya)
        n = max(1, int(length / (w * 1.25)))
        for k in range(n + 1):
            t = k / n
            box((w * rnd.uniform(0.85, 1.1), w * rnd.uniform(0.7, 0.95), 0.02),
                (xa + (xb - xa) * t + rnd.uniform(-0.02, 0.02), ya + (yb - ya) * t + rnd.uniform(-0.02, 0.02), 0.01),
                rot=(0, 0, rnd.uniform(-12, 12)), material=LIME_DARK)


# ---------------------------------------------------------------- Größere Häuser: Säulen, Läden, Atrium, Treppen


def column(x, y, h, r=0.042):
    """Säule aus Kalkstein mit Basis und Kapitell, Fuß bei z = 0."""
    box((r * 3.2, r * 3.2, 0.05), (x, y, 0.025), material=LIME_DARK)
    cyl(r, h - 0.11, (x, y, 0.05 + (h - 0.11) / 2), material=LIME, verts=10)
    box((r * 3.4, r * 3.4, 0.06), (x, y, h - 0.03), material=LIME_DARK)


def colonnade(p0, p1, count, h, beam=True):
    """Säulenreihe von p0 nach p1 (beide Enden eingeschlossen) mit Gebälk darüber."""
    (xa, ya), (xb, yb) = p0, p1
    for k in range(count):
        t = k / (count - 1) if count > 1 else 0.5
        column(xa + (xb - xa) * t, ya + (yb - ya) * t, h)
    if beam:
        length = math.hypot(xb - xa, yb - ya) + 0.16
        yaw = math.degrees(math.atan2(yb - ya, xb - xa))
        box((length, 0.12, 0.10), ((xa + xb) / 2, (ya + yb) / 2, h + 0.05), rot=(0, 0, yaw), material=LIME)


def cornice(x0, x1, y0, y1, z, material=LIME_DARK):
    """Gesimsband rund um einen Block."""
    box((x1 - x0 + 0.05, y1 - y0 + 0.05, 0.05), ((x0 + x1) / 2, (y0 + y1) / 2, z), material=material)


def shop_front(face, plane, u, w=0.56, h=0.62, goods=None, awning=None):
    """Laden (Taberna) zur Straße: breite dunkle Öffnung mit Holzsturz, davor eine gemauerte Theke mit Waren,
    darüber auf Wunsch eine gestreifte Markise (awning = (Farbe A, Farbe B))."""
    at, size, (ux, uy), (nx, ny) = face_frame(face, plane)
    z0 = 0.09
    box(size(w, 0.012, h), at(u, 0.004, z0 + h / 2), material=DARK)
    box(size(w + 0.10, 0.04, 0.07), at(u, 0.015, z0 + h + 0.03), material=WOOD_DARK)          # Sturz
    for s in (-1, 1):
        box(size(0.05, 0.03, h), at(u + s * (w / 2 + 0.02), 0.012, z0 + h / 2), material=LIME_DARK)
    box(size(w * 0.8, 0.13, 0.30), at(u - w * 0.08, 0.075, 0.15), material=LIME_DARK)          # Theke
    box(size(w * 0.84, 0.15, 0.03), at(u - w * 0.08, 0.075, 0.315), material=LIME)
    top = 0.33
    for k, kind in enumerate(goods or ("jug", "jug", "basket")):
        p = at(u - w * 0.36 + k * w * 0.2, 0.075, top)
        if kind == "jug":
            jug(p[0], p[1], top, 0.028)
        elif kind == "amphora":
            amphora(p[0], p[1], top)
        elif kind == "bread":
            sphere(0.03, (p[0], p[1], top + 0.012), (1.0, 0.8, 0.5), WOOD, segs=8, rings=4)
        else:
            cyl(0.045, 0.04, (p[0], p[1], top + 0.02), material=STRAW, verts=8)
            sphere(0.04, (p[0], p[1], top + 0.04), (1.0, 1.0, 0.4), LEAF if k % 2 else CLOTH_OCHRE, segs=6, rings=4)
    if awning:
        a, b = awning
        depth, drop = 0.34, 0.10
        stripes = max(3, int(w / 0.12))
        sw = (w + 0.10) / stripes
        tilt = math.degrees(math.atan2(drop, depth))
        for k in range(stripes):
            uu = u - (w + 0.10) / 2 + (k + 0.5) * sw
            c = at(uu, depth / 2 + 0.02, z0 + h + 0.12 - drop / 2)
            if ny != 0:
                box((sw, depth, 0.012), c, rot=(-ny * tilt, 0, 0), material=a if k % 2 == 0 else b)
            else:
                box((depth, sw, 0.012), c, rot=(0, nx * tilt, 0), material=a if k % 2 == 0 else b)
            edge = at(uu, depth + 0.02, z0 + h + 0.12 - drop - 0.03)
            box(size(sw, 0.008, 0.06), edge, material=a if k % 2 == 0 else b)


def stairs(x, y, yaw, height, width=0.26, step=0.16):
    """Holztreppe mit Wange, steigt in Richtung yaw (Grad um z, 0 = +x) bis height."""
    n = max(2, int(height / step))
    run = n * 0.11
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    for k in range(n):
        d = (k + 0.5) * 0.11
        box((0.11, width, 0.03), (x + c * d, y + s * d, (k + 1) * height / n - 0.015), rot=(0, 0, yaw), material=WOOD)
    pitch = math.degrees(math.atan2(height, run))
    L = math.hypot(height, run)
    for side in (-1, 1):
        ox, oy = -s * side * width / 2, c * side * width / 2
        box((L, 0.025, 0.06), (x + c * run / 2 + ox, y + s * run / 2 + oy, height / 2), rot=(0, -pitch, yaw), material=WOOD_DARK)


def prism(top, thickness, material):
    """Platte aus einem ebenen Vieleck (3D-Punkte der Oberseite), nach unten um thickness verdickt."""
    n = len(top)
    verts = [tuple(p) for p in top] + [(p[0], p[1], p[2] - thickness) for p in top]
    faces = [tuple(range(n)), tuple(range(n, 2 * n))[::-1]] + [(i, n + i, n + (i + 1) % n, (i + 1) % n) for i in range(n)]
    mesh = bpy.data.meshes.new("Platte")
    mesh.from_pydata(verts, [], faces)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Platte", mesh)
    bpy.context.collection.objects.link(obj)
    return keep(obj, material)


def rod(p0, p1, r, material, verts=8):
    """Rundstab (Zylinder) von p0 nach p1, z. B. Imbrex oder Gratziegel auf einer beliebigen Schräge."""
    a, b = Vector(p0), Vector(p1)
    d = b - a
    obj = cyl(r, d.length, tuple((a + b) / 2), material=material, verts=verts)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = d.to_track_quat("Z", "Y")
    return obj


def atrium_roof(x0, x1, y0, y1, ox0, ox1, oy0, oy1, z_out, z_in, spout=TERRACOTTA):
    """Atriumdach (Compluvium): vier Dachflächen fallen von den Außenwänden (z_out) nach innen zu einer rechteckigen
    Öffnung (z_in) ab und treffen sich an den Ecken in Graten. Ziegel als Halbrohre den Hang hinab, Gratziegel,
    Stirnziegel am Rand der Öffnung und in der Mitte jeder Seite ein Wasserspeier über dem Becken."""
    outer = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    inner = [(ox0, oy0), (ox1, oy0), (ox1, oy1), (ox0, oy1)]
    for i in range(4):
        j = (i + 1) % 4
        a0, a1, b0, b1 = outer[i], outer[j], inner[i], inner[j]
        prism([(a0[0], a0[1], z_out), (a1[0], a1[1], z_out), (b1[0], b1[1], z_in), (b0[0], b0[1], z_in)], T, TILE)
        # Halbrohre: Linien von der Außenkante senkrecht nach innen, an den Graten gekürzt
        ax, ay = a1[0] - a0[0], a1[1] - a0[1]
        length = math.hypot(ax, ay)
        ux, uy = ax / length, ay / length
        nx, ny = -uy, ux                                           # nach innen (Umlauf gegen den Uhrzeiger)
        depth = abs((b0[0] - a0[0]) * nx + (b0[1] - a0[1]) * ny)
        s0 = (b0[0] - a0[0]) * ux + (b0[1] - a0[1]) * uy           # Lage der inneren Ecken entlang der Kante
        s1 = (b1[0] - a0[0]) * ux + (b1[1] - a0[1]) * uy
        count = max(2, int(length / 0.18))
        for k in range(1, count):
            s = k * length / count
            run = depth * min(1.0, s / s0 if s0 > 0 else 1.0, (length - s) / (length - s1) if length > s1 else 1.0)
            if run < 0.08:
                continue
            px, py = a0[0] + ux * s, a0[1] + uy * s
            z_end = z_out + (z_in - z_out) * run / depth
            rod((px, py, z_out + 0.02), (px + nx * run, py + ny * run, z_end + 0.02), 0.032,
                TILE if random.random() > 0.3 else TILE_DARK, verts=6)
        rod((a0[0], a0[1], z_out + 0.03), (b0[0], b0[1], z_in + 0.03), 0.045, TILE_DARK, verts=8)   # Grat
        # Rand der Öffnung: Stirnziegel und Wasserspeier
        il = math.hypot(b1[0] - b0[0], b1[1] - b0[1])
        for k in range(max(2, int(il / 0.16)) + 1):
            t = k / max(2, int(il / 0.16))
            px, py = b0[0] + (b1[0] - b0[0]) * t, b0[1] + (b1[1] - b0[1]) * t
            box((0.06, 0.06, 0.07), (px + nx * 0.01, py + ny * 0.01, z_in + 0.04), rot=(0, 0, math.degrees(math.atan2(uy, ux))),
                material=TILE_DARK)
        mx, my = (b0[0] + b1[0]) / 2, (b0[1] + b1[1]) / 2
        rod((mx - nx * 0.02, my - ny * 0.02, z_in - 0.01), (mx + nx * 0.16, my + ny * 0.16, z_in - 0.05), 0.025, spout, verts=6)


def brick_bands(x0, x1, y0, y1, zs, faces=("-y", "+y", "-x", "+x")):
    """Ziegelbänder im Kalkstein (Opus mixtum) auf den genannten Seiten eines Blocks."""
    for z in zs:
        if "-y" in faces:
            box((x1 - x0, 0.012, 0.05), ((x0 + x1) / 2, y0 - 0.006, z), material=TILE_DARK)
        if "+y" in faces:
            box((x1 - x0, 0.012, 0.05), ((x0 + x1) / 2, y1 + 0.006, z), material=TILE_DARK)
        if "-x" in faces:
            box((0.012, y1 - y0, 0.05), (x0 - 0.006, (y0 + y1) / 2, z), material=TILE_DARK)
        if "+x" in faces:
            box((0.012, y1 - y0, 0.05), (x1 + 0.006, (y0 + y1) / 2, z), material=TILE_DARK)


def buttress(face, plane, u, h, depth=0.16, width=0.16, material=LIME_DARK):
    """Strebepfeiler an einer Wand: steht um depth vor, oben schräg zur Wand hin."""
    nx, ny = FACES[face]
    top_out, top_in = h * 0.72, h
    if ny != 0:
        pts = [(0.0, 0.0), (depth, 0.0), (depth, top_out), (0.0, top_in)]
        pts = [(plane + ny * a, z) for a, z in pts]
        slab(pts, "yz", u - width / 2, width, material)
    else:
        pts = [(0.0, 0.0), (depth, 0.0), (depth, top_out), (0.0, top_in)]
        pts = [(plane + nx * a, z) for a, z in pts]
        slab(pts, "xz", u - width / 2, width, material)


def slit(face, plane, u, z, h=0.30):
    """Schmaler Lüftungsschlitz mit Bank und Ziegelsturz (Speicherbauten)."""
    at, size, _, _ = face_frame(face, plane)
    box(size(0.05, 0.012, h), at(u, 0.004, z + h / 2), material=DARK)
    box(size(0.11, 0.02, 0.04), at(u, 0.008, z + h + 0.02), material=TILE_DARK)
    box(size(0.10, 0.03, 0.025), at(u, 0.012, z - 0.012), material=LIME)


def barrel(x, y, z0=0.0, r=0.07, lying=False, yaw=0.0):
    """Fass aus Dauben mit dunklen Reifen; stehend oder liegend (um yaw gedreht)."""
    h = r * 2.6
    if lying:
        cyl(r, h, (x, y, z0 + r), rot=(0, 90, yaw), material=WOOD, verts=10)
        c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        for k in (-0.32, 0.32):
            cyl(r * 1.04, 0.012, (x + c * k * h, y + s * k * h, z0 + r), rot=(0, 90, yaw), material=WOOD_DARK, verts=10)
    else:
        cyl(r, h, (x, y, z0 + h / 2), material=WOOD, verts=10)
        for k in (0.18, 0.82):
            cyl(r * 1.04, 0.012, (x, y, z0 + h * k), material=WOOD_DARK, verts=10)
        cyl(r * 0.92, 0.005, (x, y, z0 + h + 0.002), material=WOOD_DARK, verts=10)


def hoist(x, y_wall, z, reach=0.45):
    """Kranbalken aus der Giebelwand nach -y mit Rolle, Seil und hängendem Sack."""
    box((0.06, reach + 0.10, 0.06), (x, y_wall - reach / 2 + 0.05, z), material=WOOD_DARK)
    box((0.04, 0.30, 0.04), (x, y_wall - 0.14, z - 0.13), rot=(45, 0, 0), material=WOOD_DARK)
    cyl(0.035, 0.03, (x, y_wall - reach + 0.03, z - 0.05), rot=(0, 90, 0), material=WOOD, verts=10)
    box((0.008, 0.008, z - 0.75), (x, y_wall - reach + 0.03, (z + 0.70) / 2), material=SACK)
    sack(x, y_wall - reach + 0.03, 0.42, 0.06)


def impluvium(x, y, w, d):
    """Wasserbecken im Atrium: flacher Steinrand mit Wasser."""
    box((w, d, 0.06), (x, y, 0.03), material=LIME_DARK)
    box((w - 0.08, d - 0.08, 0.01), (x, y, 0.062), material=WATER)


def fountain_bowl(x, y, r=0.16):
    """Brunnenschale auf Fuß mit Wasser (Gartenbrunnen)."""
    cyl(r * 0.35, 0.22, (x, y, 0.11), material=LIME, verts=10)
    cone(r * 0.4, r, 0.06, (x, y, 0.25), LIME, verts=12)
    cyl(r * 0.9, 0.01, (x, y, 0.278), material=WATER, verts=12)


def bush(x, y, r=0.12, leaves=LEAF):
    """Runder Busch."""
    sphere(r, (x, y, r * 0.8), (1.0, 1.0, 0.85), leaves, segs=8, rings=5)


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
