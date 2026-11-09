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
TERRACOTTA_LIGHT = mat("TerrakottaHell", (0.84, 0.58, 0.42))
SOIL = mat("Erde", (0.45, 0.33, 0.22))
LEAF = mat("Laub", (0.38, 0.55, 0.26))
LEAF_DARK = mat("LaubDunkel", (0.27, 0.42, 0.20))
OLIVE_LEAF = mat("Olivenlaub", (0.52, 0.58, 0.40))
STRAW = mat("Stroh", (0.86, 0.74, 0.46))
GRAIN = mat("Korn", (0.78, 0.60, 0.30))
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
    """Spitzamphore nach Dressel 1 bis 2-4 (in Wirklichkeit 0,55 m): ein weich schattierter Rotationskörper aus kurzem Fußzapfen
    mit Knopf, oben breitem Bauch, gerundeter Schulter, zylindrischem Hals und rundem Lippenwulst (Lippe und Henkel in hellerem
    Ton), dazu zwei kräftige Bandhenkel, die unter der Lippe am Hals ansetzen und im Bogen auf der Schulter enden."""
    segs = 14
    # Profil (Radius, Höhe) in Metern der echten Amphore; die Maße werden danach mit S verkleinert
    profile = ((0.036, 0.000), (0.045, 0.014), (0.040, 0.050), (0.068, 0.140), (0.090, 0.250), (0.099, 0.320),
               (0.088, 0.385), (0.058, 0.425), (0.034, 0.455), (0.031, 0.505), (0.044, 0.522), (0.045, 0.540),
               (0.034, 0.550))
    light_from = 9                                           # ab diesem Ring (Hals oben): Lippe in hellem Ton
    verts, faces, mat_ids = [], [], []
    for r, h in profile:
        for j in range(segs):
            a = 2.0 * math.pi * j / segs
            verts.append((x + r * S * math.cos(a), y + r * S * math.sin(a), z0 + h * S))
    for i in range(len(profile) - 1):
        for j in range(segs):
            j2 = (j + 1) % segs
            faces.append((i * segs + j, i * segs + j2, (i + 1) * segs + j2, (i + 1) * segs + j))
            mat_ids.append(1 if i >= light_from else 0)
    top = (len(profile) - 1) * segs
    faces.append(tuple(range(top, top + segs)))              # Mündung (oben), flach
    mat_ids.append(1)
    mesh = bpy.data.meshes.new("Amphore")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    mesh.materials.append(TERRACOTTA)
    mesh.materials.append(TERRACOTTA_LIGHT)
    for k, p in enumerate(mesh.polygons):
        p.material_index = mat_ids[k]
        p.use_smooth = k < len(mat_ids) - 1                  # Körper weich, Mündungsfläche flach
    obj = bpy.data.objects.new("Amphore", mesh)
    bpy.context.collection.objects.link(obj)
    PARTS.append(obj)
    # Henkel: kräftiges Band entlang einer Kurve (Abstand von der Achse, Höhe), links und rechts; Enden stecken in Hals und Schulter
    path = ((0.027, 0.505), (0.064, 0.497), (0.080, 0.452), (0.080, 0.395))
    width, thick = 0.030, 0.015
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
        mid_of = lambda k: sum((Vector(hv[i]) for i in range(k * 4, k * 4 + 4)), Vector()) / 4
        fixed = []
        for k in range(last):
            mid = (mid_of(k) + mid_of(k + 1)) / 2
            for e in range(4):
                e2 = (e + 1) % 4
                f = [k * 4 + e, k * 4 + e2, (k + 1) * 4 + e2, (k + 1) * 4 + e]
                pts = [Vector(hv[i]) for i in f]
                c = sum(pts, Vector()) / 4
                nrm = (pts[1] - pts[0]).cross(pts[2] - pts[1])
                # nach außen drehen, bezogen auf die Mittellinie des Bandes an dieser Stelle
                ctr = (mid_of(k) + mid_of(k + 1)) / 2
                fixed.append(f if nrm.dot(c - ctr) >= 0 else list(reversed(f)))
        hmesh = bpy.data.meshes.new("AmphorenHenkel")
        hmesh.from_pydata(hv, [], fixed)
        hmesh.update()
        hmesh.materials.append(TERRACOTTA_LIGHT)
        hobj = bpy.data.objects.new("AmphorenHenkel", hmesh)
        bpy.context.collection.objects.link(hobj)
        PARTS.append(hobj)


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


def lying_sack(x, y, z0=0.0, yaw=0.0, r=0.045, length=0.16):
    """Liegender, länglicher Getreidesack (Länge entlang yaw, 0 = +x), an einem Ende zugebunden."""
    a = math.radians(yaw)
    sphere(r, (x, y, z0 + r * 0.8), (length / (2 * r), 1.0, 0.75), SACK, segs=10, rings=6, rot=(0, 0, yaw))
    ex, ey = x + math.cos(a) * length * 0.5, y + math.sin(a) * length * 0.5
    cyl(r * 0.30, r * 0.25, (ex, ey, z0 + r * 0.8), rot=(0, 90, yaw), material=WOOD_DARK, verts=6)
    cone(r * 0.35, r * 0.10, r * 0.45, (ex + math.cos(a) * r * 0.30, ey + math.sin(a) * r * 0.30, z0 + r * 0.8),
         SACK, verts=6, rot=(0, 90, yaw))


def ox_cart(x, y, yaw=0.0, load="sacks"):
    """Ochsenkarren (Plaustrum): zwei Scheibenräder aus Bohlen mit Querleiste und Eisenreif, Kasten mit Lattenwänden,
    lange Deichsel nach -y (vor der Drehung) mit Joch, das vorn auf einem Bock ruht; Ladung 'sacks' oder None."""
    at = placed((x, y), yaw)
    W, L, Z = 0.34, 0.62, 0.20                                                 # Kasten: Breite, Länge, Boden
    box((W, L, 0.025), at((0.0, 0.0, Z + 0.012)), rot=(0, 0, yaw), material=WOOD)
    for sx in (-1, 1):                                                         # Langbäume unter dem Boden
        box((0.03, L + 0.06, 0.03), at((sx * (W / 2 - 0.03), -0.02, Z - 0.012)), rot=(0, 0, yaw), material=WOOD_DARK)
    # Lattenwände: Pfosten und zwei Riegel je Seite, niedrige Bretter vorn und hinten
    for sx in (-1, 1):
        for py in (-L / 2 + 0.02, -0.10, 0.10, L / 2 - 0.02):
            box((0.018, 0.018, 0.13), at((sx * (W / 2 - 0.01), py, Z + 0.09)), rot=(0, 0, yaw), material=WOOD_DARK)
        for pz in (0.07, 0.14):
            box((0.012, L, 0.014), at((sx * (W / 2 - 0.01), 0.0, Z + pz)), rot=(0, 0, yaw), material=WOOD)
    for py in (-L / 2 + 0.01, L / 2 - 0.01):
        box((W - 0.02, 0.014, 0.07), at((0.0, py, Z + 0.05)), rot=(0, 0, yaw), material=WOOD)
    # Räder an einer Achse etwas hinter der Mitte
    ay, R = 0.06, 0.165
    cyl(0.018, W + 0.16, at((0.0, ay, R)), rot=(0, 90, yaw), material=WOOD_DARK, verts=6)
    for sx in (-1, 1):
        wx = sx * (W / 2 + 0.05)
        cyl(R + 0.006, 0.026, at((wx, ay, R)), rot=(0, 90, yaw), material=IRON, verts=16)
        cyl(R, 0.036, at((wx, ay, R)), rot=(0, 90, yaw), material=WOOD, verts=16)
        for dz in (-0.07, 0.07):                                               # Bohlenfugen
            box((0.038, 2 * math.sqrt(R * R - dz * dz) - 0.01, 0.006), at((wx, ay, R + dz)), rot=(0, 0, yaw),
                material=WOOD_DARK)
        box((0.040, 0.035, 2 * R - 0.02), at((wx, ay, R)), rot=(0, 0, yaw), material=WOOD_DARK)   # Querleiste
        cyl(0.035, 0.05, at((wx + sx * 0.01, ay, R)), rot=(0, 90, yaw), material=WOOD_DARK, verts=8)  # Nabe
    # Deichsel mit Joch auf einem Bock
    front = at((0.0, -L / 2, Z - 0.01))
    tip = at((0.0, -L / 2 - 0.52, 0.12))
    rod(front, tip, 0.016, WOOD_DARK, verts=6)
    box((0.40, 0.035, 0.03), at((0.0, -L / 2 - 0.50, 0.135)), rot=(0, 0, yaw), material=WOOD)
    for sx in (-1, 1):
        rod(at((sx * 0.05, -L / 2 - 0.47, 0.0)), at((0.0, -L / 2 - 0.47, 0.11)), 0.009, WOOD_DARK, verts=5)
    if load == "sacks":
        for ly, turn in ((-0.14, 90), (0.13, -90)):                         # zwei Lagen längs, Bänder außen
            for sx in (-1, 1):
                lying_sack(*at((sx * 0.075, ly, 0.0))[:2], z0=Z + 0.025, yaw=yaw + turn + sx * 3, r=0.05, length=0.22)
        lying_sack(*at((0.0, -0.02, 0.0))[:2], z0=Z + 0.10, yaw=yaw + 93, r=0.05, length=0.22)


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


def hoist(x, y_wall, z, reach=0.45, sack_z=0.42):
    """Kranbalken aus der Giebelwand nach -y mit Rolle, Seil und hängendem Sack (Unterkante sack_z)."""
    box((0.06, reach + 0.10, 0.06), (x, y_wall - reach / 2 + 0.05, z), material=WOOD_DARK)
    box((0.04, 0.30, 0.04), (x, y_wall - 0.14, z - 0.13), rot=(45, 0, 0), material=WOOD_DARK)
    cyl(0.035, 0.03, (x, y_wall - reach + 0.03, z - 0.05), rot=(0, 90, 0), material=WOOD, verts=10)
    rope = sack_z + 0.305                                                      # Seil endet am Hals des Sacks
    box((0.008, 0.008, z - rope), (x, y_wall - reach + 0.03, (z + rope) / 2), material=SACK)
    sack(x, y_wall - reach + 0.03, sack_z, 0.06)


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


def plank_wall(x0, x1, y0, y1, h, faces=("-y", "+y", "-x", "+x"), step=0.10):
    """Bohlenwand (Scheune, Schuppen): Holzblock auf Bruchsteinsockel mit senkrechten Fugen zwischen den Bohlen auf den
    genannten Seiten, Eckpfosten und einem Rähm oben."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    box((x1 - x0, y1 - y0, h), (cx, cy, h / 2), material=WOOD)
    box((x1 - x0 + 0.04, y1 - y0 + 0.04, 0.12), (cx, cy, 0.06), material=STONE)
    box((x1 - x0 + 0.03, y1 - y0 + 0.03, 0.04), (cx, cy, h - 0.02), material=WOOD_DARK)     # Rähm
    for face in faces:
        plane = {"-y": y0, "+y": y1, "-x": x0, "+x": x1}[face]
        at, size, _, _ = face_frame(face, plane)
        lo, hi = (x0, x1) if face in ("-y", "+y") else (y0, y1)
        n = int((hi - lo) / step)
        for k in range(1, n):
            box(size(0.008, 0.006, h - 0.20), at(lo + k * (hi - lo) / n, 0.003, 0.12 + (h - 0.20) / 2), material=WOOD_DARK)
    for x, y in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
        box((0.06, 0.06, h - 0.10), (x, y, 0.12 + (h - 0.10) / 2), material=WOOD_DARK)


def sheaf(x, y, lean=0.0, yaw=0.0):
    """Garbe (Getreidebündel, etwa 45 cm hoch): unten die Halme leicht gespreizt, in der Mitte das Band, oben fächern
    die Ähren auf und enden in einem runden Schopf; lean = Neigung in Grad nach der Richtung yaw (0 = nach +y)."""
    a, l = math.radians(yaw), math.radians(lean)
    dx, dy = -math.sin(a) * math.sin(l), math.cos(a) * math.sin(l)
    tilt = (lean * math.cos(a) * -1, lean * math.sin(a) * -1, 0.0)
    k = 0.62                                                                   # kleiner als echt, wie die Häuser

    def at(h):
        return (x + dx * h * k, y + dy * h * k, h * math.cos(l) * k)

    cone(0.062 * k, 0.040 * k, 0.24 * k, at(0.12), STRAW, verts=8, rot=tilt)            # Halme
    cyl(0.044 * k, 0.03 * k, at(0.24), material=WOOD_DARK, verts=8, rot=tilt)            # Band
    cone(0.040 * k, 0.078 * k, 0.14 * k, at(0.32), GRAIN, verts=8, rot=tilt)            # Ähren fächern auf
    sphere(0.078 * k, at(0.39), (1.0, 1.0, 0.45), GRAIN, segs=8, rings=4, rot=tilt)       # runder Schopf


def stook(x, y, n=5, yaw=0.0):
    """Hocke: n Garben im Kreis gegeneinander gelehnt, wie sie auf dem Feld oder am Dreschplatz trocknen."""
    for i in range(n):
        t = math.radians(yaw + i * 360.0 / n)
        sheaf(x + 0.035 * math.cos(t), y + 0.035 * math.sin(t), lean=14, yaw=math.degrees(t) + 90)


def plough(x, y, yaw=0.0):
    """Hakenpflug (Ard) im Maßstab der übrigen Geräte: Sohle mit eiserner Schar, Grindel zum Joch hin ansteigend,
    Stütze, Sterz mit Griff und Querjoch; Zugrichtung = yaw (0 = +x)."""
    at = placed((x, y), yaw)

    def P(lx, ly, lz):
        return at((lx * S, ly * S, lz * S))
    bar(P(-0.15, 0, 0.07), P(0.45, 0, 0.07), 0.08 * S, WOOD_DARK)                       # Sohle
    bar(P(0.45, 0, 0.065), P(0.62, 0, 0.045), 0.07 * S, IRON)                           # Schar
    bar(P(-0.05, 0, 0.12), P(1.15, 0, 0.55), 0.075 * S, WOOD)                           # Grindel
    bar(P(0.20, 0, 0.07), P(0.30, 0, 0.25), 0.065 * S, WOOD_DARK)                       # Stütze
    bar(P(-0.12, 0, 0.07), P(-0.45, 0, 0.62), 0.065 * S, WOOD_DARK)                     # Sterz
    bar(P(-0.45, -0.07, 0.62), P(-0.45, 0.07, 0.62), 0.06 * S, WOOD)                    # Griff
    bar(P(1.15, -0.22, 0.55), P(1.15, 0.22, 0.55), 0.07 * S, WOOD)                      # Joch


# ---------------------------------------------------------------- Grünanlagen (Parks)

CYPRESS = mat("Zypresse", (0.14, 0.30, 0.19))
LAUREL = mat("Lorbeer", (0.22, 0.46, 0.26))
HEDGE = mat("Hecke", (0.20, 0.42, 0.22))
HERB_LIGHT = mat("KrautHell", (0.55, 0.72, 0.34))
HERB_GREY = mat("KrautGrau", (0.56, 0.66, 0.52))
LAVENDER = mat("Lavendel", (0.58, 0.44, 0.78))
BLOOM_YELLOW = mat("BlueteGelb", (0.96, 0.82, 0.22))
BLOOM_RED = mat("BlueteRot", (0.86, 0.22, 0.20))
BLOOM_WHITE = mat("BlueteWeiss", (0.96, 0.95, 0.90))
BLOOM_PINK = mat("BlueteRosa", (0.92, 0.52, 0.66))
MARBLE = mat("Marmor", (0.94, 0.93, 0.89), rough=0.5)


def cypress(x, y, h=0.9, r=0.11):
    """Schlanke dunkle Zypresse: kurzer Stamm, vier gestufte Kegel, Spitze."""
    cyl(0.018, 0.10, (x, y, 0.05), material=WOOD_DARK, verts=6)
    n = 4
    for i in range(n):
        z0 = 0.08 + (h - 0.08) * i / n * 0.9
        seg = (h - 0.08) / n * 1.35
        rb = r * (1.0 - 0.18 * i)
        cone(rb, rb * 0.45, seg, (x, y, z0 + seg / 2), CYPRESS, verts=8)
    cone(r * 0.35, 0.0, h * 0.18, (x, y, h - h * 0.09), CYPRESS, verts=8)


def laurel_tree(x, y, size=1.0):
    """Lorbeerbaum: dünner Stamm, runde dichte Krone aus zwei Büscheln."""
    cyl(0.025 * size, 0.40 * size, (x, y, 0.20 * size), rot=(3, 2, 0), material=WOOD_DARK, verts=6)
    sphere(0.22 * size, (x, y, 0.58 * size), (1.0, 1.0, 0.95), LAUREL, segs=9, rings=6)
    sphere(0.14 * size, (x + 0.10 * size, y + 0.04 * size, 0.70 * size), material=LEAF, segs=7, rings=5)


def hedge(x0, x1, y0, y1, h=0.22):
    """Geschnittene Hecke als Quader mit etwas helleren Triebspitzen obenauf."""
    box((x1 - x0, y1 - y0, h), ((x0 + x1) / 2, (y0 + y1) / 2, h / 2), material=HEDGE)
    box((x1 - x0 - 0.03, y1 - y0 - 0.03, 0.03), ((x0 + x1) / 2, (y0 + y1) / 2, h + 0.012), material=LEAF)


def flower_bed(x0, x1, y0, y1, bloom=BLOOM_YELLOW, foliage=LEAF, seed=1, edge=LIME_DARK, step=0.10):
    """Blumenbeet: Steinkante, Erde, grüne Polster und Blütenköpfchen (kräftige Farbe)."""
    rnd = random.Random(seed)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    box((x1 - x0, y1 - y0, 0.03), (cx, cy, 0.015), material=SOIL)
    for x in (x0, x1):
        box((0.035, y1 - y0 + 0.035, 0.07), (x, cy, 0.035), material=edge)
    for y in (y0, y1):
        box((x1 - x0, 0.035, 0.07), (cx, y, 0.035), material=edge)
    nx, ny = max(2, round((x1 - x0) / step)), max(2, round((y1 - y0) / step))
    for i in range(nx):
        for j in range(ny):
            x = x0 + (i + 0.5) * (x1 - x0) / nx + rnd.uniform(-0.015, 0.015)
            y = y0 + (j + 0.5) * (y1 - y0) / ny + rnd.uniform(-0.015, 0.015)
            sphere(step * 0.42, (x, y, 0.06), (1.0, 1.0, 0.65), foliage, segs=5, rings=3)
            for _ in range(2):
                sphere(0.022, (x + rnd.uniform(-0.04, 0.04) * step / 0.1, y + rnd.uniform(-0.04, 0.04) * step / 0.1, 0.09 + rnd.uniform(0, 0.035)), material=bloom, segs=4, rings=2)


def statue(x, y, yaw=0.0, h=0.55):
    """Marmorstatue auf Sockel: Gewandfigur mit erhobenem Arm."""
    box((0.16, 0.16, 0.05), (x, y, 0.025), material=LIME_DARK)
    box((0.12, 0.12, h * 0.38), (x, y, 0.05 + h * 0.19), material=LIME)
    box((0.15, 0.15, 0.03), (x, y, 0.05 + h * 0.38 + 0.015), material=LIME_DARK)
    z0 = 0.05 + h * 0.38 + 0.03
    cone(0.055, 0.032, h * 0.32, (x, y, z0 + h * 0.16), MARBLE, verts=10)
    sphere(0.032, (x, y, z0 + h * 0.32 + 0.025), material=MARBLE, segs=8, rings=5)
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    bar((x - s * 0.03, y + c * 0.03, z0 + h * 0.26), (x - s * 0.06, y + c * 0.06, z0 + h * 0.38), 0.014, MARBLE)


def herm(x, y, yaw=0.0, h=0.40):
    """Herme: Pfeiler mit Kopf und Gesimsplatte."""
    box((0.09, 0.09, h), (x, y, h / 2), material=LIME)
    box((0.12, 0.12, 0.03), (x, y, 0.015), material=LIME_DARK)
    box((0.11, 0.11, 0.025), (x, y, h + 0.012), material=LIME_DARK)
    sphere(0.045, (x, y, h + 0.07), (1.0, 1.0, 1.1), MARBLE, segs=8, rings=5)


ROCK_A = mat("FelsGrauA", (0.50, 0.49, 0.47), rough=0.95)
ROCK_B = mat("FelsGrauB", (0.61, 0.60, 0.57), rough=0.95)
ROCK_C = mat("FelsGrauC", (0.41, 0.40, 0.39), rough=0.95)
WICKER = mat("Weide", (0.70, 0.54, 0.32))
WICKER_DARK = mat("WeideDunkel", (0.50, 0.37, 0.22))


def dry_stone_wall(p0, p1, h=0.15, gaps=(), seed=0):
    """Niedrige Trockenmauer aus rauen Bruchsteinen (Quader, leicht gekippt, drei Grautöne) in zwei Lagen mit wellig
    unregelmäßiger Oberkante. gaps = Liste von (t0, t1) mit Anteilen 0..1 der Strecke, an denen sie unterbrochen ist."""
    rnd = random.Random(seed)
    (xa, ya), (xb, yb) = p0, p1
    length = math.hypot(xb - xa, yb - ya)
    yaw0 = math.degrees(math.atan2(yb - ya, xb - xa))
    ux, uy = (xb - xa) / length, (yb - ya) / length
    mats = (ROCK_A, ROCK_B, ROCK_C)
    for row in range(2):
        z_base = 0.0 if row == 0 else h * 0.52
        s = 0.07 + (0.0 if row == 0 else 0.08)
        while s < length - 0.05:
            w = rnd.uniform(0.12, 0.20)
            t = (s + w / 2) / length
            ok = not any(g0 <= t <= g1 for g0, g1 in gaps)
            if ok:
                hh = (h * 0.55 if row == 0 else h * 0.48) * rnd.uniform(0.85, 1.15) + (0.02 * math.sin(s * 9) if row else 0)
                d = rnd.uniform(0.11, 0.15)
                cx, cy = xa + ux * (s + w / 2), ya + uy * (s + w / 2)
                off = rnd.uniform(-0.012, 0.012)
                box((w, d, hh), (cx - uy * off, cy + ux * off, z_base + hh / 2),
                    rot=(rnd.uniform(-5, 5), rnd.uniform(-5, 5), yaw0 + rnd.uniform(-9, 9)), material=rnd.choice(mats))
            s += w + 0.012


def stone_post(x, y, h=0.34, yaw=0.0):
    """Kleiner rauer Steinpfosten: übereinandergesetzte, leicht verdrehte graue Blöcke mit Deckstein."""
    rnd = random.Random(int(x * 91 + y * 37) + 5)
    z = 0.0
    n = 3
    for i in range(n):
        hh = (h - 0.05) / n
        w = 0.16 - 0.012 * i + rnd.uniform(-0.01, 0.01)
        box((w, w, hh), (x, y, z + hh / 2), rot=(0, 0, yaw + rnd.uniform(-12, 12)), material=(ROCK_A, ROCK_B, ROCK_C)[i % 3])
        z += hh
    box((0.17, 0.15, 0.05), (x, y, z + 0.025), rot=(rnd.uniform(-4, 4), rnd.uniform(-4, 4), yaw + 8), material=ROCK_B)


def wattle_fence(points, h=0.20, spacing=0.30):
    """Flechtzaun: Holzpfosten alle spacing m, dazwischen waagerechte Weidenruten, die abwechselnd vor und hinter den
    Pfosten laufen (dünne Stäbe in zwei Holzfarben)."""
    for (xa, ya), (xb, yb) in zip(points, points[1:]):
        length = math.hypot(xb - xa, yb - ya)
        n = max(1, round(length / spacing))
        ux, uy = (xb - xa) / length, (yb - ya) / length
        yaw = math.degrees(math.atan2(yb - ya, xb - xa))
        for k in range(n):
            t0, t1 = k * length / n, (k + 1) * length / n
            cx, cy = xa + ux * (t0 + t1) / 2, ya + uy * (t0 + t1) / 2
            for r, z in enumerate((0.055, 0.105, 0.155)):
                side = 0.014 if (k + r) % 2 == 0 else -0.014
                box((t1 - t0 + 0.012, 0.016, 0.020), (cx - uy * side, cy + ux * side, z), rot=(0, 0, yaw),
                    material=WICKER if (k + r) % 3 else WICKER_DARK)
    for (xa, ya), (xb, yb) in zip(points, points[1:]):
        length = math.hypot(xb - xa, yb - ya)
        n = max(1, round(length / spacing))
        for k in range(n + 1):
            if k == 0 and (xa, ya) != points[0]:
                continue
            box((0.032, 0.032, h + 0.04), (xa + (xb - xa) * k / n, ya + (yb - ya) * k / n, (h + 0.04) / 2), material=WOOD_DARK)


# ---------------------------------------------------------------- Fischerei (Fischer)

FISH = mat("Fisch", (0.68, 0.72, 0.74), rough=0.35)
FISH_DARK = mat("FischRuecken", (0.38, 0.47, 0.53), rough=0.4)
ROPE = mat("Tau", (0.68, 0.58, 0.40))
CORK = mat("Kork", (0.74, 0.56, 0.36))


def fish(x, y, z0=0.0, yaw=0.0, flat=True):
    """Fisch (etwa 16 cm lang): silbriger Körper mit dunklerem Rücken und Schwanzflosse. flat = liegend (Tisch, Korb);
    sonst hängt er senkrecht mit dem Kopf nach oben, z0 ist dann die Höhe des Schwanzendes."""
    a = math.radians(yaw)
    c, s = math.cos(a), math.sin(a)
    if flat:
        sphere(0.07, (x, y, z0 + 0.015), (1.1, 0.38, 0.20), FISH, segs=6, rings=3, rot=(0, 0, yaw))
        box((0.10, 0.012, 0.004), (x, y, z0 + 0.031), rot=(0, 0, yaw), material=FISH_DARK)         # dunkler Rückenstreifen
        box((0.035, 0.034, 0.005), (x - c * 0.085, y - s * 0.085, z0 + 0.012), rot=(0, 0, yaw), material=FISH_DARK)
    else:
        zc = z0 + 0.09
        sphere(0.07, (x, y, zc), (1.1, 0.2, 0.40), FISH, segs=6, rings=3, rot=(0, 90, yaw))
        box((0.034, 0.005, 0.034), (x, y, z0 + 0.012), rot=(0, 0, yaw), material=FISH_DARK)
        box((0.004, 0.004, 0.05), (x, y, zc + 0.10), material=ROPE)


def net(p0, p1, drop, sag=0.04, mesh=0.075, seed=3):
    """Hängendes Fischernetz an einer Leine von p0 nach p1 (gleiche Höhe): senkrechte Maschenfäden und waagerechte Reihen
    als dünne Stäbe, leicht durchhängend, unten gerundet und ausgefranst; Kork-Schwimmer oben, Bleigewichte unten."""
    rnd = random.Random(seed)
    (x0, y0, z), (x1, y1, _) = p0, p1
    length = math.hypot(x1 - x0, y1 - y0)
    n = max(3, int(length / mesh))
    rows = max(2, int(drop / mesh))
    cols = []
    for k in range(n + 1):
        t = k / n
        zt = z - sag * 4 * t * (1 - t)
        d = drop * (1.0 - 0.45 * (2 * t - 1) ** 4) * rnd.uniform(0.94, 1.0)
        cols.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, zt, d))
    for cx, cy, zt, d in cols:
        bar((cx, cy, zt), (cx, cy, zt - d), 0.006, ROPE)
    for r in range(1, rows + 1):
        f = r / (rows + 0.6)
        for (ax, ay, az, ad), (bx, by, bz, bd) in zip(cols, cols[1:]):
            bar((ax, ay, az - ad * f), (bx, by, bz - bd * f), 0.006, ROPE)
    bar((x0, y0, z), (x1, y1, z), 0.012, ROPE)                                   # Oberleine
    for k in range(0, n + 1, 2):                                                 # Kork-Schwimmer
        cx, cy, zt, _ = cols[k]
        sphere(0.017, (cx, cy, zt - 0.006), (1.0, 1.0, 1.35), CORK, segs=6, rings=4)
    for k in range(1, n, 3):                                                     # Bleigewichte am Saum
        cx, cy, zt, d = cols[k]
        sphere(0.012, (cx, cy, zt - d * (rows / (rows + 0.6)) - 0.004), material=STONE, segs=5, rings=3)


def reuse(x, y, yaw=0.0, z0=0.0, length=0.34, r=0.058):
    """Reuse aus Weide (liegend): Rutenkörper mit dunklen Ringen, trichterförmiger Eingang an einem Ende mit dunkler
    Öffnung, am anderen Ende zugebunden. Achse entlang yaw (0 = +x), Eingang bei +x."""
    a = math.radians(yaw)
    c, s = math.cos(a), math.sin(a)

    def P(l, dz=0.0):
        return (x + c * l, y + s * l, z0 + r + dz)
    cyl(r, length, P(0), rot=(0, 90, yaw), material=WICKER, verts=10)
    for k in (-0.40, -0.14, 0.12, 0.38):
        cyl(r * 1.06, 0.014, P(k * length), rot=(0, 90, yaw), material=WICKER_DARK, verts=10)
    cone(r * 0.35, r, 0.11, P(length / 2 + 0.05), WICKER_DARK, verts=10, rot=(0, 90, yaw))   # Trichter, innen schmal
    cyl(r * 0.30, 0.006, P(length / 2 + 0.105), rot=(0, 90, yaw), material=DARK, verts=8)
    sphere(r * 0.9, P(-length / 2 - 0.01), (0.7, 1.0, 1.0), WICKER, segs=8, rings=4, rot=(0, 0, yaw))
    cone(r * 0.25, r * 0.08, 0.06, P(-length / 2 - 0.08), WICKER_DARK, verts=6, rot=(0, 90, yaw))   # Schnur am Ende


def oar(p0, p1, shaft=0.014):
    """Ruder: Schaft von p0 (Blattende) nach p1 (Griff) und ein flaches Blatt am Ende bei p0 (etwa 1,3 m lang)."""
    a, b = Vector(p0), Vector(p1)
    d = (b - a).normalized()
    bar(a + d * 0.16, b, shaft, WOOD)
    blade = box((0.06, 0.010, 0.24), tuple(a + d * 0.10), material=WOOD)
    blade.rotation_mode = "QUATERNION"
    blade.rotation_quaternion = d.to_track_quat("Z", "Y")
    bar(a + d * 0.22, a + d * 0.24, shaft * 2.0, WOOD_DARK)                        # Wulst am Übergang


def rope_coil(x, y, z0=0.0, r=0.06, layers=3):
    """Aufgerolltes Tau: übereinanderliegende Ringe."""
    for k in range(layers):
        bpy.ops.mesh.primitive_torus_add(major_radius=r * (1.0 - 0.05 * k), minor_radius=0.011, major_segments=14,
                                         minor_segments=5, location=(x, y, z0 + 0.011 + k * 0.021))
        keep(bpy.context.active_object, ROPE)


def stone_anchor(x, y, yaw=0.0):
    """Steinanker: flacher Stein mit Tauring, daneben liegt das Tauende."""
    box((0.13, 0.07, 0.07), (x, y, 0.035), rot=(0, 0, yaw), material=STONE)
    box((0.09, 0.06, 0.05), (x, y, 0.095), rot=(2, 3, yaw + 12), material=ROCK_B)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.028, minor_radius=0.007, major_segments=10, minor_segments=5,
                                     location=(x, y, 0.15), rotation=(math.radians(80), 0, math.radians(yaw)))
    keep(bpy.context.active_object, ROPE)
    a = math.radians(yaw + 70)
    bar((x, y, 0.13), (x + 0.16 * math.cos(a), y + 0.16 * math.sin(a), 0.011), 0.011, ROPE)


def bucket(x, y, z0=0.0, r=0.05, fish_inside=False):
    """Holzeimer mit zwei Reifen und Henkel; wahlweise mit Fischen gefüllt."""
    h = 0.10
    cone(r * 0.82, r, h, (x, y, z0 + h / 2), WOOD, verts=10)
    for k in (0.22, 0.78):
        cyl(r * (0.82 + 0.18 * k) + 0.004, 0.010, (x, y, z0 + h * k), material=IRON, verts=10)
    cyl(r * 0.88, 0.004, (x, y, z0 + h + 0.001), material=WATER if not fish_inside else FISH, verts=10)
    for sgn in (-1, 1):
        bar((x + sgn * r, y, z0 + h - 0.01), (x + sgn * r * 0.9, y, z0 + h + 0.07), 0.006, IRON)
    bar((x - r * 0.9, y, z0 + h + 0.07), (x + r * 0.9, y, z0 + h + 0.07), 0.006, IRON)
    if fish_inside:
        sphere(0.045, (x, y, z0 + h), (1.0, 1.0, 0.45), FISH, segs=7, rings=4)
        fish(x + 0.01, y, z0 + h + 0.01, yaw=25)


def fish_basket(x, y, z0=0.0, r=0.10, yaw=0.0):
    """Rundkorb aus Weide, bis zum Rand mit Fischen gefüllt; Köpfe und Schwänze ragen heraus."""
    h = 0.15
    cone(r * 0.8, r, h, (x, y, z0 + h / 2), WICKER, verts=12)
    for k in (0.2, 0.55, 0.9):
        cyl(r * (0.8 + 0.2 * k) + 0.004, 0.012, (x, y, z0 + h * k), material=WICKER_DARK, verts=12)
    sphere(r * 0.95, (x, y, z0 + h), (1.0, 1.0, 0.5), FISH, segs=8, rings=4)
    for i, ang in enumerate((30, 120, 215, 305)):
        a = math.radians(ang + yaw)
        fish(x + math.cos(a) * r * 0.45, y + math.sin(a) * r * 0.45, z0 + h + 0.005, yaw=ang + yaw + 20 * (i % 2))


def row_boat(cx, cy, length=1.9, beam=0.54, depth=0.20, z_gunwale=0.26, yaw=0.0):
    """Kieloben auf zwei Böcken liegendes Ruderboot (Klinkerplanken in vier Streifen, roter Zierstreifen am Dollbord,
    Kielleiste, Dollbordleisten); die Außenhaut ist als Schale aus Querschnitten aufgebaut, innen eine zweite Schale."""
    N, M = 17, 9
    a = math.radians(yaw)
    ca, sa = math.cos(a), math.sin(a)

    def station(t):
        tt = min(abs(t), 0.992)
        w = beam / 2 * (1.0 - tt ** 2.3) ** 0.55
        d = depth * (1.0 - tt ** 2.0) ** 0.6
        z0 = z_gunwale - 0.10 * tt ** 2.2
        return t * length / 2, w, d, z0

    def vert(t, u, inset):
        lx, w, d, z0 = station(t)
        wy = u * w * (1.0 - inset * 3)
        z = z0 + max(d - inset * 0.8, 0.0) * (1.0 - abs(u) ** 2.2) + (inset * 0.1 if abs(u) == 1 else 0.0)
        return (cx + ca * lx - sa * wy, cy + sa * lx + ca * wy, z)

    bands = [SOCLE_RED, WOOD, WOOD_DARK, WOOD]
    for inset, flip in ((0.0, False), (0.02, True)):
        verts, faces, mats = [], [], []
        for i in range(N):
            t = -1 + 2 * i / (N - 1)
            for j in range(M):
                verts.append(vert(t, -1 + 2 * j / (M - 1), inset))
        for i in range(N - 1):
            for j in range(M - 1):
                v00, v01 = i * M + j, i * M + j + 1
                v10, v11 = (i + 1) * M + j, (i + 1) * M + j + 1
                faces.append((v00, v10, v11, v01) if not flip else (v00, v01, v11, v10))
                mats.append(min(j, M - 2 - j))
        mesh = bpy.data.meshes.new("Boot")
        mesh.from_pydata(verts, [], faces)
        mesh.update()
        obj = bpy.data.objects.new("Boot", mesh)
        bpy.context.collection.objects.link(obj)
        if flip:
            mesh.materials.append(WOOD_DARK)
            for p in mesh.polygons:
                p.material_index = 0
        else:
            for m in bands:
                mesh.materials.append(m)
            for p, mi in zip(mesh.polygons, mats):
                p.material_index = mi
        PARTS.append(obj)
    # Kielleiste oben, Dollbordleisten unten an beiden Kanten
    for i in range(N - 1):
        t0, t1 = -1 + 2 * i / (N - 1), -1 + 2 * (i + 1) / (N - 1)
        bar(vert(t0, 0, 0), vert(t1, 0, 0), 0.03, WOOD_DARK)
        for u in (-1, 1):
            p0, p1 = vert(t0, u, 0), vert(t1, u, 0)
            bar((p0[0], p0[1], p0[2] + 0.005), (p1[0], p1[1], p1[2] + 0.005), 0.022, WOOD_DARK)
    # zwei Böcke unter der Mitte
    for t in (-0.42, 0.42):
        lx, w, d, z0 = station(t)
        px, py = cx + ca * lx, cy + sa * lx
        top = z0 - 0.012
        bar((px - sa * (w + 0.07), py + ca * (w + 0.07), top - 0.02), (px + sa * (w + 0.07), py - ca * (w + 0.07), top - 0.02),
            0.05, WOOD_DARK)
        for sgn in (-1, 1):
            foot = w + 0.15
            bar((px - sa * sgn * (w + 0.05), py + ca * sgn * (w + 0.05), top - 0.03),
                (px - sa * sgn * foot, py + ca * sgn * foot, 0.0), 0.04, WOOD_DARK)
            bar((px - sa * sgn * (w + 0.05) - ca * 0.05, py + ca * sgn * (w + 0.05) - sa * 0.05, top - 0.03),
                (px - sa * sgn * foot - ca * 0.09, py + ca * sgn * foot - sa * 0.09, 0.0), 0.03, WOOD_DARK)
            bar((px - sa * sgn * (w + 0.05) + ca * 0.05, py + ca * sgn * (w + 0.05) + sa * 0.05, top - 0.03),
                (px - sa * sgn * foot + ca * 0.09, py + ca * sgn * foot + sa * 0.09, 0.0), 0.03, WOOD_DARK)


def plank_pier(x0, x1, y0, y1, z=0.15):
    """Kurzer Bohlensteg zum Wasser (nach +y): Querbohlen auf zwei Längsbalken, Pfähle an den Ecken, eine Stufe an der
    Landseite y0. Endet genau bei y1 (äußere Kante der Bohlen und der Pfähle)."""
    n = int((y1 - y0) / 0.092)
    pitch = (y1 - y0) / n
    for k in range(n):
        yc = y0 + (k + 0.5) * pitch
        box((x1 - x0, pitch - 0.012, 0.03), ((x0 + x1) / 2, yc, z - 0.015),
            material=WOOD if k % 3 else WOOD_DARK)
    for x in (x0 + 0.09, x1 - 0.09):
        box((0.06, y1 - y0 - 0.02, 0.05), (x, (y0 + y1) / 2 - 0.0, z - 0.055), material=WOOD_DARK)
    for x in (x0 + 0.04, x1 - 0.04):
        for y in (y0 + 0.05, (y0 + y1) / 2, y1 - 0.045):
            cyl(0.032, z + 0.03 if y > y0 + 0.1 else z - 0.07, (x, y, (z + 0.03) / 2 if y > y0 + 0.1 else (z - 0.07) / 2),
                material=WOOD_DARK, verts=7)
    box((x1 - x0 - 0.1, 0.10, 0.07), ((x0 + x1) / 2, y0 - 0.05, 0.035), material=WOOD)   # Stufe


# ---------------------------------------------------------------- Hafen (Kai, Poller, Tretkran, Waage)

BRONZE = mat("Bronze", (0.72, 0.52, 0.24), rough=0.4)
FLAME = mat("Flamme", (0.98, 0.62, 0.16), rough=0.5)


def rope(p0, p1, sag=0.0, thick=0.012, segs=5, material=ROPE):
    """Tau zwischen zwei Punkten, in der Mitte um sag durchhängend (Stücke aus Vierkantstäben)."""
    a, b = Vector(p0), Vector(p1)
    prev = a
    for i in range(1, segs + 1):
        t = i / segs
        p = a + (b - a) * t
        p.z -= sag * 4 * t * (1 - t)
        bar(prev, p, thick, material)
        prev = p


def quay_platform(x0, x1, y0, y1, z=0.16, gaps=(), curb_z=0.24, curb_depth=0.30, batter=0.10):
    """Kai aus Quadersteinen: Platte (Oberkante z) von y0 bis y1 mit einer Randkante (Oberkante curb_z) an der
    Wasserseite y1; deren Außenseite fällt um batter nach innen ab, nichts ragt über y1. gaps = Lücken (x von, x bis) in
    der Randkante für Stege. Quaderfugen als dünne Leisten."""
    xs = [x0]
    for g0, g1 in gaps:
        xs += [g0, g1]
    xs.append(x1)
    theta = math.degrees(math.atan2(batter, curb_z))

    def yface(zz):
        return y1 - batter * zz / curb_z

    for xa, xb in zip(xs[0::2], xs[1::2]):
        slab([(y0, 0.0), (y1, 0.0), (y1 - batter, curb_z), (y1 - curb_depth, curb_z), (y1 - curb_depth, z), (y0, z)],
             "yz", xa, xb - xa, LIME)
        xm = (xa + xb) / 2
        for zz in (0.08, 0.16):                                                # Lagerfugen der Außenseite
            box((xb - xa, 0.012, 0.012), (xm, yface(zz) - 0.004, zz), rot=(theta, 0, 0), material=LIME_DARK)
        for row, zc in enumerate((0.04, 0.12, 0.20)):                          # Stoßfugen, versetzt
            x = xa + 0.17 + (0.17 if row % 2 else 0.0)
            while x < xb - 0.05:
                box((0.012, 0.012, 0.070), (x, yface(zc) - 0.004, zc), rot=(theta, 0, 0), material=LIME_DARK)
                x += 0.34
        for yy in (y0 + 0.45, y0 + 0.90):                                      # Längsfugen der Platte
            if yy < y1 - curb_depth - 0.05:
                box((xb - xa, 0.012, 0.004), (xm, yy, z + 0.001), material=LIME_DARK)
    for ga, gb in gaps:
        slab([(y0, 0.0), (y1 - curb_depth, 0.0), (y1 - curb_depth, z), (y0, z)], "yz", ga, gb - ga, LIME)


def bollard(x, y, z0, h=0.13, r=0.040, wraps=2):
    """Poller aus Stein mit Kappe und um den Hals gelegtem Tau."""
    cyl(r, h, (x, y, z0 + h / 2), material=LIME_DARK, verts=10)
    cyl(r * 1.45, 0.03, (x, y, z0 + h + 0.015), material=LIME_DARK, verts=10)
    for k in range(wraps):
        bpy.ops.mesh.primitive_torus_add(major_radius=r * 1.08, minor_radius=0.010, major_segments=12,
                                         minor_segments=5, location=(x, y, z0 + h * (0.42 + 0.22 * k)))
        keep(bpy.context.active_object, ROPE)


def amphora_load(x, y, z_bottom, hook_z):
    """Lastnetz mit vier Amphoren: Holzpalette, zwei Seilringe um die Bäuche, vier Seile zum Haken."""
    box((0.25, 0.25, 0.02), (x, y, z_bottom + 0.01), material=WOOD)
    for px, py in ((-0.055, -0.055), (0.055, -0.055), (-0.055, 0.055), (0.055, 0.055)):
        amphora(x + px, y + py, z_bottom + 0.02)
    for z in (0.13, 0.20):
        box((0.19, 0.19, 0.008), (x, y, z_bottom + z), material=ROPE)
    for px, py in ((-0.105, -0.105), (0.105, -0.105), (-0.105, 0.105), (0.105, 0.105)):
        bar((x + px, y + py, z_bottom + 0.02), (x, y, hook_z), 0.007, ROPE)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.03, minor_radius=0.007, major_segments=10, minor_segments=5,
                                     location=(x, y, hook_z + 0.02), rotation=(math.radians(90), 0, 0))
    keep(bpy.context.active_object, IRON)


def treadwheel_crane(cx, cy, z0, jib_back=0.55, jib_front=0.75, hook_z=0.86, tall=1.7):
    """Hölzerner Tretkran, Ausleger nach +y: zwei Masten mit Streben und Querbalken, dazwischen das Laufrad mit
    Trittbrettern, daneben auf der Achse eine Seiltrommel; das Seil läuft über die Mastrolle zur Rolle am Auslegerende,
    daran das Lastseil. Das Rad liegt zwischen den Masten (x von cx - 0.27 bis cx + 0.05). Gibt (x, y, z) der Rolle
    am Auslegerende zurück."""
    r, zc = 0.40, z0 + 0.42

    def Z(h):
        """Höhe über dem Boden; oberhalb des Rads um den Faktor tall gestreckt (hoher Mast, im Spiel lesbar)."""
        return z0 + (h if h <= 0.9 else 0.9 + (h - 0.9) * tall)

    mx = (cx - 0.36, cx + 0.36)
    for x in mx:
        box((0.07, 0.07, Z(1.52) - z0), (x, cy, (z0 + Z(1.52)) / 2), material=WOOD_DARK)
        for dy in (-0.40, 0.40):                                                  # Streben nach vorn und hinten
            bar((x, cy + dy, z0), (x, cy, Z(0.90)), 0.05, WOOD_DARK)
        box((0.12, 0.12, 0.05), (x, cy, z0 + 0.025), material=LIME_DARK)
    bar((mx[0], cy, Z(1.38)), (mx[1], cy, Z(1.38)), 0.06, WOOD)                    # Querbalken, trägt den Ausleger
    bar((mx[0], cy, Z(1.00)), (mx[1], cy, Z(1.00)), 0.05, WOOD)
    bar((mx[0] - 0.04, cy, zc), (mx[1] + 0.04, cy, zc), 0.045, WOOD_DARK)           # Achse
    wx0, wx1 = cx - 0.27, cx + 0.05
    for xs in (wx0, wx1):
        bpy.ops.mesh.primitive_torus_add(major_radius=r, minor_radius=0.018, major_segments=20, minor_segments=5,
                                         location=(xs, cy, zc), rotation=(0, math.radians(90), 0))
        keep(bpy.context.active_object, WOOD_DARK)
        for k in range(8):
            a = math.radians(k * 45 + 11)
            bar((xs, cy, zc), (xs, cy + r * math.cos(a), zc + r * math.sin(a)), 0.03, WOOD)
    for k in range(16):                                                            # Trittbretter
        a = math.radians(k * 22.5)
        box((wx1 - wx0, 0.055, 0.02), ((wx0 + wx1) / 2, cy + r * math.cos(a), zc + r * math.sin(a)),
            rot=(math.degrees(a) - 90, 0, 0), material=WOOD if k % 2 else WOOD_DARK)
    cyl(0.06, 0.12, (cx + 0.20, cy, zc), rot=(0, 90, 0), material=WOOD, verts=10)    # Seiltrommel
    cyl(0.063, 0.06, (cx + 0.20, cy, zc), rot=(0, 90, 0), material=ROPE, verts=10)
    # Ausleger: liegt auf dem Querbalken, hinten Gegengewicht, vorn Rolle
    ytip, ztip = cy + jib_front, Z(1.62)
    yback, zback = cy - jib_back, Z(1.30)

    def jib_z(y):
        return zback + (y - yback) / (ytip - yback) * (ztip - zback)

    bar((cx, yback, zback), (cx, ytip, ztip), 0.06, WOOD)
    bar((cx, cy + 0.10, Z(1.00)), (cx, cy + 0.42, jib_z(cy + 0.42) - 0.03), 0.04, WOOD_DARK)   # Stütze unter dem Ausleger
    box((0.14, 0.10, 0.14), (cx, yback - 0.02, zback - 0.24), material=LIME_DARK)    # Gegengewicht
    bar((cx, yback, zback - 0.01), (cx, yback - 0.02, zback - 0.17), 0.012, ROPE)
    cyl(0.04, 0.05, (cx, ytip, ztip - 0.03), rot=(0, 90, 0), material=WOOD, verts=10)
    for dx in (-0.035, 0.035):
        box((0.012, 0.07, 0.10), (cx + dx, ytip, ztip - 0.03), material=IRON)
    # Seile: Trommel -> Mastrolle -> Auslegerende, Lastseil zum Haken
    cyl(0.035, 0.05, (cx + 0.20, cy, Z(1.50)), rot=(0, 90, 0), material=WOOD, verts=8)
    bar((cx + 0.20, cy - 0.06, zc + 0.06), (cx + 0.20, cy - 0.01, Z(1.47)), 0.014, ROPE)
    bar((cx + 0.20, cy, Z(1.53)), (cx, ytip, ztip + 0.012), 0.014, ROPE)
    bar((cx, ytip + 0.035, ztip - 0.03), (cx, ytip + 0.035, hook_z + 0.04), 0.014, ROPE)
    return cx, ytip + 0.035, ztip


def scale_balance(x, y, z0, arm=0.17):
    """Balkenwaage auf einem Tisch (Oberkante z0): Ständer, Waagebalken aus Eisen, zwei Schalen an Schnüren."""
    cyl(0.04, 0.02, (x, y, z0 + 0.01), material=WOOD_DARK, verts=8)
    cyl(0.011, 0.26, (x, y, z0 + 0.15), material=IRON, verts=6)
    tilt = 0.012
    bar((x - arm, y, z0 + 0.275 - tilt), (x + arm, y, z0 + 0.275 + tilt), 0.012, IRON)
    sphere(0.016, (x, y, z0 + 0.29), material=IRON, segs=6, rings=4)
    for s, dz in ((-1, -tilt), (1, tilt)):
        px, pz = x + s * arm, z0 + 0.275 + dz
        for q in (-0.045, 0.045):
            bar((px, y, pz), (px + q, y, pz - 0.115), 0.004, ROPE)
            bar((px, y, pz), (px, y + q, pz - 0.115), 0.004, ROPE)
        cyl(0.055, 0.008, (px, y, pz - 0.12), material=BRONZE, verts=12)


def brazier(x, y, z0=0.0, h=0.46):
    """Feuerschale auf einem Kalksteinpfeiler (Signalfeuer am Kai)."""
    box((0.16, 0.16, 0.05), (x, y, z0 + 0.025), material=LIME_DARK)
    box((0.11, 0.11, h), (x, y, z0 + 0.05 + h / 2), material=LIME)
    box((0.15, 0.15, 0.03), (x, y, z0 + 0.05 + h + 0.015), material=LIME_DARK)
    cone(0.04, 0.10, 0.07, (x, y, z0 + h + 0.115), BRONZE, verts=10)
    cone(0.075, 0.0, 0.14, (x, y, z0 + h + 0.22), FLAME, verts=8)
    cone(0.04, 0.0, 0.18, (x, y, z0 + h + 0.24), BLOOM_YELLOW, verts=6)


def hoist_out(x, y_wall, z, reach=0.45, sign=1, sack_z=0.42):
    """Kranbalken aus einer Längswand nach +y (sign 1) oder -y (sign -1) mit Strebe, Rolle, Seil und hängendem Sack."""
    box((0.06, reach + 0.10, 0.06), (x, y_wall + sign * (reach / 2 - 0.05), z), material=WOOD_DARK)
    box((0.04, 0.30, 0.04), (x, y_wall + sign * 0.14, z - 0.13), rot=(-sign * 45, 0, 0), material=WOOD_DARK)
    cyl(0.035, 0.03, (x, y_wall + sign * (reach - 0.03), z - 0.05), rot=(0, 90, 0), material=WOOD, verts=10)
    rope_end = sack_z + 0.305
    box((0.008, 0.008, z - rope_end), (x, y_wall + sign * (reach - 0.03), (z + rope_end) / 2), material=SACK)
    sack(x, y_wall + sign * (reach - 0.03), sack_z, 0.06)


# ---------------------------------------------------------------- Schiffe

TAR = mat("Pech", (0.17, 0.14, 0.12), rough=0.9)
HULL_CREAM = mat("RumpfCreme", (0.90, 0.84, 0.66))
SAIL_LINEN = mat("Segelleinen", (0.93, 0.89, 0.78))
SAIL_RED = mat("SegelRot", (0.68, 0.22, 0.17))


def loft(rings, mat_of_col, flip=False, name="Schale"):
    """Fläche aus Punktreihen: rings[i][j] sind die Punkte des i-ten Querschnitts; zwischen benachbarten Querschnitten
    und Punkten entsteht je ein Viereck, dessen Material mat_of_col(j) bestimmt (j = Spalte zwischen Punkt j und j+1).
    flip dreht die Seite, die nach außen zeigt."""
    cols = len(rings[0])
    verts = [p for ring in rings for p in ring]
    faces, mids = [], []
    for i in range(len(rings) - 1):
        for j in range(cols - 1):
            a, b = i * cols + j, i * cols + j + 1
            c, d = (i + 1) * cols + j + 1, (i + 1) * cols + j
            faces.append((a, d, c, b) if flip else (a, b, c, d))
            mids.append(j)
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    slots = {}
    for k, j in enumerate(mids):
        m = mat_of_col(j)
        if m not in slots:
            mesh.materials.append(m)
            slots[m] = len(mesh.materials) - 1
        mesh.polygons[k].material_index = slots[m]
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    PARTS.append(obj)
    return obj


def sail_panel(x0, x1, z0, z1, y, bulge=0.1, stripes=(), nx=14, nz=7, thick=0.008):
    """Rahsegel in der Ebene y: Tuch von x0..x1 und z0..z1, nach -y gewölbt (in der Mitte am meisten), beidseitig;
    stripes = ((u0, u1, Material), ...) senkrechte Streifen (u = 0..1 von links nach rechts)."""
    def point(i, k, dy):
        u, v = i / nx, k / nz
        return (x0 + (x1 - x0) * u, y - bulge * math.sin(math.pi * u) * math.sin(math.pi * (0.15 + 0.85 * v)) + dy,
                z0 + (z1 - z0) * v)

    def color(i):
        u = (i + 0.5) / nx
        for a, b, m in stripes:
            if a <= u <= b:
                return m
        return SAIL_LINEN

    for dy, flip in ((0.0, False), (thick, True)):
        rings = [[point(i, k, dy) for i in range(nx + 1)] for k in range(nz + 1)]
        loft(rings, color, flip=flip, name="Segel")


def goose_neck(x, y, z, r=0.34, sweep=165.0, thick=0.05):
    """Gänsehals-Zier am Heck (Chenisköpfchen): Bogen, der vom Steven nach oben und dann nach innen (-y) schwingt,
    mit Gänsekopf und Schnabel am Ende. (x, y, z) ist der Fuß; der Bogen liegt in der Ebene x."""
    n = 10
    pts = []
    for i in range(n + 1):
        a = math.radians(sweep) * i / n
        pts.append((x, y - r + r * math.cos(a), z + r * math.sin(a)))
    for i in range(n):
        bar(pts[i], pts[i + 1], thick * (1.0 - 0.45 * i / n), WOOD_DARK)
    end, prev = Vector(pts[-1]), Vector(pts[-2])
    d = (end - prev).normalized()
    sphere(thick * 0.95, tuple(end), (1.0, 1.1, 1.0), SOCLE_RED)
    beak = cone(thick * 0.55, thick * 0.12, thick * 1.8, tuple(end + d * thick * 1.4), BRONZE, verts=7)
    beak.rotation_mode = "QUATERNION"
    beak.rotation_quaternion = (-d).to_track_quat("Z", "Y")
    for sx in (-1, 1):
        sphere(thick * 0.18, (end.x + sx * thick * 0.55, end.y, end.z + thick * 0.2), material=DARK, segs=5, rings=3)


def ship_anchor(x, y, z0, yaw=0.0):
    """Anker, wie er an Deck liegt: Schaft, zwei Arme mit Spitzen, Querstock aus Holz mit Bleikern und Ring."""
    a = math.radians(yaw)
    c, s = math.cos(a), math.sin(a)

    def P(u, v, z):
        return (x + c * u - s * v, y + s * u + c * v, z0 + z)

    bar(P(0, -0.20, 0.04), P(0, 0.20, 0.04), 0.035, IRON)
    bar(P(0, 0.20, 0.04), P(0.16, 0.12, 0.04), 0.03, IRON)
    bar(P(0, 0.20, 0.04), P(-0.16, 0.12, 0.04), 0.03, IRON)
    for sgn in (-1, 1):
        cone(0.03, 0.0, 0.06, P(sgn * 0.17, 0.115, 0.04), IRON, verts=5, rot=(0, 90 * sgn, yaw + 25 * sgn))
    box((0.34, 0.045, 0.045), P(0, -0.12, 0.085), rot=(0, 0, yaw), material=WOOD_DARK)
    box((0.12, 0.05, 0.05), P(0, -0.12, 0.09), rot=(0, 0, yaw), material=STONE)
    bpy.ops.mesh.primitive_torus_add(major_radius=0.035, minor_radius=0.008, major_segments=10, minor_segments=5,
                                     location=P(0, -0.21, 0.07), rotation=(math.radians(90), 0, a))
    keep(bpy.context.active_object, IRON)


# ---------------------------------------------------------------- Tempel und Brunnen (Säulen, Altar, Reliefkopf, Wasserstrahl)

TYMPANUM = mat("Giebelfeld", (0.74, 0.46, 0.38))
FOAM = mat("Schaum", (0.88, 0.95, 0.98), rough=0.3)
SMOKE = mat("Rauch", (0.80, 0.79, 0.77), rough=1.0)
PAVE_WORN = mat("PflasterDunkel", (0.40, 0.38, 0.35), rough=0.7)


def fluted_shaft(x, y, z0, z1, r0, r1, flutes=10, material=MARBLE):
    """Kannelierter Säulenschaft: Sternprofil mit abwechselnd vollem und eingezogenem Radius (Kanneluren), unten r0, oben
    r1, in der Mitte leicht gebaucht (Entasis). Flache Schattierung zeigt die Rillen."""
    n = 2 * flutes
    rings = [(0.0, r0), (0.45, r0 + (r1 - r0) * 0.45 + r0 * 0.02), (1.0, r1)]
    verts = []
    for t, rad in rings:
        z = z0 + (z1 - z0) * t
        for i in range(n):
            a = 2 * math.pi * i / n
            rr = rad if i % 2 == 0 else rad * 0.86
            verts.append((x + math.cos(a) * rr, y + math.sin(a) * rr, z))
    faces = []
    for k in range(len(rings) - 1):
        for i in range(n):
            j = (i + 1) % n
            faces.append((k * n + i, k * n + j, (k + 1) * n + j, (k + 1) * n + i))
    faces.append(tuple(range(2 * n, 3 * n)))
    faces.append(tuple(range(n))[::-1])
    mesh = bpy.data.meshes.new("Schaft")
    mesh.from_pydata(verts, [], faces)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Schaft", mesh)
    bpy.context.collection.objects.link(obj)
    return keep(obj, material)


def ring(x, y, z, big_r, small_r, axis="y", material=BRONZE, segs=14, yaw=0.0):
    """Ring (Torus), Achse 'y' = liegt in der xz-Ebene (Kranz an einer Front), 'z' = liegt flach."""
    rot = (math.radians(90), 0, math.radians(yaw)) if axis == "y" else (0, 0, 0)
    bpy.ops.mesh.primitive_torus_add(major_radius=big_r, minor_radius=small_r, major_segments=segs, minor_segments=5,
                                     location=(x, y, z), rotation=rot)
    return keep(bpy.context.active_object, material)


def corinthian_column(x, y, z0, h, r=0.075, shaft=MARBLE, cap=MARBLE):
    """Säule mit attischer Basis (Plinthe, Wülste, Kehle), kanneliertem Schaft und korinthisch angedeutetem Kapitell
    (Kelch, acht Akanthusblätter, vier Eckvoluten, Abakus). Fuß bei z0, Gesamthöhe h."""
    base_h, cap_h = 0.105, 0.17
    box((r * 2.9, r * 2.9, 0.04), (x, y, z0 + 0.02), material=LIME_DARK)
    cyl(r * 1.30, 0.03, (x, y, z0 + 0.055), material=shaft, verts=12)
    cyl(r * 1.05, 0.015, (x, y, z0 + 0.0775), material=LIME_DARK, verts=12)
    cyl(r * 1.18, 0.02, (x, y, z0 + 0.095), material=shaft, verts=12)
    zs = z0 + base_h
    ze = z0 + h - cap_h
    fluted_shaft(x, y, zs, ze, r * 0.98, r * 0.84, flutes=8, material=shaft)
    cyl(r * 0.92, 0.02, (x, y, ze + 0.01), material=LIME_DARK, verts=12)                       # Halsring
    cone(r * 0.88, r * 1.32, 0.10, (x, y, ze + 0.07), cap, verts=12)                            # Kelch
    for k in range(8):
        a = 2 * math.pi * k / 8
        sphere(r * 0.34, (x + math.cos(a) * r * 1.02, y + math.sin(a) * r * 1.02, ze + 0.085), (1.0, 0.45, 1.6),
               LIME, segs=5, rings=3, rot=(0, 0, math.degrees(a) + 90))                           # Akanthusblätter
    for sx in (-1, 1):
        for sy in (-1, 1):
            sphere(r * 0.30, (x + sx * r * 1.30, y + sy * r * 1.30, ze + 0.13), (1.0, 1.0, 0.8), LIME_DARK, segs=6, rings=4)
    box((r * 3.0, r * 3.0, 0.03), (x, y, ze + 0.155), material=cap)                             # Abakus
    box((r * 3.2, r * 3.2, 0.015), (x, y, ze + 0.1625), material=LIME_DARK)


def palmette(x, y, z, yaw=0.0, h=0.16, material=MARBLE):
    """Akroterion: Sockel mit Palmette (fünf Blätter, Mittelkugel); die Fläche zeigt bei yaw 0 nach -y."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    box((h * 0.75, h * 0.32, h * 0.30), (x, y, z + h * 0.15), rot=(0, 0, yaw), material=LIME_DARK)
    for k, a in enumerate((-62, -31, 0, 31, 62)):
        rl = h * (0.60 if k in (0, 4) else 0.78 if k in (1, 3) else 0.92)
        ar = math.radians(a)
        cx, cz = math.sin(ar) * rl * 0.5, math.cos(ar) * rl * 0.5
        box((h * 0.17, h * 0.07, rl), (x + c * cx, y + s * cx, z + h * 0.30 + cz), rot=(0, -a, yaw), material=material)
    sphere(h * 0.12, (x, y, z + h * 0.36), material=material, segs=6, rings=4)


def relief_head(x, y, z, yaw=0.0, r=0.085, spout=True):
    """Reliefkopf (Maske) an einer Wand: Scheibe, Stirn, Augen, Nase, offener Mund mit Wasserspeier (Bronzerohr), Haar
    und Bart aus Locken. Blickt bei yaw 0 nach -y, dreht sich mit yaw um die Wandmitte."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))

    def W(lx, ly, lz):
        return (x + lx * c - ly * s, y + lx * s + ly * c, z + lz)

    cyl(r, 0.035, W(0, 0, 0), rot=(90, 0, yaw), material=MARBLE, verts=14)
    sphere(r * 0.80, W(0, -0.012, 0.0), (1.0, 0.55, 1.05), MARBLE, segs=9, rings=6, rot=(0, 0, yaw))
    for sx in (-1, 1):
        sphere(r * 0.17, W(sx * r * 0.34, -r * 0.40, r * 0.22), (1.0, 0.6, 0.7), DARK, segs=5, rings=3, rot=(0, 0, yaw))
        box((r * 0.42, 0.012, 0.012), W(sx * r * 0.34, -r * 0.40, r * 0.46), rot=(0, 0, yaw), material=LIME_DARK)
    cone(r * 0.16, r * 0.07, r * 0.46, W(0, -r * 0.52, -r * 0.02), MARBLE, verts=5, rot=(90, 0, yaw))
    sphere(r * 0.24, W(0, -r * 0.42, -r * 0.46), (1.0, 0.5, 0.75), DARK, segs=6, rings=4, rot=(0, 0, yaw))   # offener Mund
    if spout:
        cyl(r * 0.13, r * 0.9, W(0, -r * 0.7, -r * 0.44), rot=(90, 0, yaw), material=BRONZE, verts=6)
    for k in range(9):                                                                    # Haar
        a = math.pi * (0.05 + 0.9 * k / 8)
        sphere(r * 0.17, W(math.cos(a) * r * 0.90, -r * 0.35, math.sin(a) * r * 0.92), (1.0, 0.8, 1.0), LIME_DARK, segs=5,
               rings=3, rot=(0, 0, yaw))
    for k in range(5):                                                                    # Bart
        a = math.pi * (1.1 + 0.8 * k / 4)
        sphere(r * 0.15, W(math.cos(a) * r * 0.78, -r * 0.35, math.sin(a) * r * 0.84 - r * 0.15), (1.0, 0.8, 1.0), LIME_DARK,
               segs=5, rings=3, rot=(0, 0, yaw))


def water_jet(p0, p1, thick=0.022, segs=8, material=WATER):
    """Wasserstrahl als gebogene Kette dünner Stäbe von p0 (Mund, anfangs waagerecht) zu p1 (Aufprall, senkrecht);
    Spritzer aus Schaumkugeln am Ende."""
    pts = []
    for k in range(segs + 1):
        t = k / segs
        pts.append((p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t, p0[2] + (p1[2] - p0[2]) * t * t))
    for k in range(segs):
        bar(pts[k], pts[k + 1], thick * (1.0 - 0.25 * k / segs), material)
    for k in range(5):
        a = 2 * math.pi * k / 5
        sphere(0.014, (p1[0] + math.cos(a) * 0.035, p1[1] + math.sin(a) * 0.035, p1[2] + 0.012 + 0.01 * (k % 2)),
               material=FOAM, segs=5, rings=3)
    sphere(0.030, (p1[0], p1[1], p1[2] + 0.005), (1.0, 1.0, 0.45), FOAM, segs=7, rings=4)
    return pts


def altar(x, y, yaw=0.0, with_flame=True):
    """Opferaltar aus Kalkstein: Sockel, Körper mit Kranz-Relief und Inschriftfeld, Gesims, zwei Polster (Pulvini) und
    Bronzeschale mit Opferflamme. Vorderseite nach -y (bei yaw 0)."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))

    def W(lx, ly):
        return (x + lx * c - ly * s, y + lx * s + ly * c)

    box((0.32, 0.24, 0.04), (*W(0, 0), 0.02), rot=(0, 0, yaw), material=LIME_DARK)
    box((0.27, 0.19, 0.03), (*W(0, 0), 0.055), rot=(0, 0, yaw), material=LIME)
    box((0.22, 0.15, 0.20), (*W(0, 0), 0.17), rot=(0, 0, yaw), material=LIME)
    box((0.12, 0.012, 0.07), (*W(0, -0.078), 0.12), rot=(0, 0, yaw), material=LIME_DARK)             # Inschriftfeld
    ring(*W(0, -0.080), 0.215, 0.036, 0.007, "y", LEAF_DARK, 10, yaw)                                 # Kranz
    box((0.30, 0.22, 0.035), (*W(0, 0), 0.2875), rot=(0, 0, yaw), material=LIME_DARK)
    for sx in (-1, 1):
        cyl(0.022, 0.20, (*W(sx * 0.10, 0), 0.325), rot=(90, 0, yaw), material=LIME, verts=8)
    cone(0.07, 0.10, 0.045, (*W(0, 0), 0.35), BRONZE, verts=10)
    if with_flame:
        cone(0.07, 0.0, 0.13, (*W(0, 0), 0.43), FLAME, verts=8)
        cone(0.035, 0.0, 0.17, (*W(0, 0), 0.45), BLOOM_YELLOW, verts=6)


def incense_burner(x, y):
    """Weihrauchbecken: Bronzeschale auf Dreifuß mit glühenden Kohlen und Rauchfahne."""
    for k in range(3):
        a = 2 * math.pi * k / 3 + 0.5
        bar((x + math.cos(a) * 0.075, y + math.sin(a) * 0.075, 0.0), (x + math.cos(a) * 0.03, y + math.sin(a) * 0.03, 0.22), 0.016, BRONZE)
    cyl(0.085, 0.014, (x, y, 0.20), material=BRONZE, verts=10)
    cone(0.05, 0.095, 0.07, (x, y, 0.245), BRONZE, verts=10)
    cyl(0.07, 0.01, (x, y, 0.282), material=FLAME, verts=10)
    for dx, dz, rr in ((0.0, 0.34, 0.035), (0.03, 0.42, 0.030), (-0.02, 0.50, 0.026), (0.025, 0.57, 0.020)):
        sphere(rr, (x + dx, y, dz), (1.0, 1.0, 0.9), SMOKE, segs=6, rings=4)


def stele(x, y, yaw=0.0, h=0.22):
    """Votivstele: kleine Platte mit Giebelchen auf Sockel und einem Kranz."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    box((0.14, 0.08, 0.03), (x, y, 0.015), rot=(0, 0, yaw), material=LIME_DARK)
    box((0.10, 0.035, h), (x, y, 0.03 + h / 2), rot=(0, 0, yaw), material=LIME)
    for sx in (-1, 1):
        box((0.062, 0.035, 0.014), (x + c * sx * 0.026, y + s * sx * 0.026, 0.03 + h + 0.003), rot=(0, -sx * 28, yaw), material=LIME)
    ring(x + s * 0.019, y - c * 0.019, 0.03 + h * 0.62, 0.026, 0.005, "y", LIME_DARK, 8, yaw)


def temple_podium(x0, x1, y0, y1, h):
    """Podium mit Profil: Sockelplatte, Wulst, Wandfläche mit Feldern, Gesims und Deckplatte (Oberkante bei h)."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    w, d = x1 - x0, y1 - y0
    box((w + 0.20, d + 0.20, 0.07), (cx, cy, 0.035), material=LIME_DARK)
    box((w + 0.12, d + 0.12, 0.05), (cx, cy, 0.095), material=LIME)
    box((w + 0.04, d + 0.04, h - 0.20), (cx, cy, 0.12 + (h - 0.20) / 2), material=LIME)
    box((w + 0.10, d + 0.10, 0.04), (cx, cy, h - 0.07), material=LIME_DARK)
    box((w + 0.16, d + 0.16, 0.05), (cx, cy, h - 0.025), material=LIME)


def grand_stairs(x, y_top, width, height, steps, run, cheek=0.14, material=LIME):
    """Freitreppe zwischen zwei Wangen, steigt nach +y bis y_top (Vorderkante des Podiums). steps = Zahl der Stufen
    unterhalb des Podiums; die Wangen laufen schräg zur Podiumhöhe und enden vorn in einem Block mit Kugel."""
    rise = height / (steps + 1)
    for k in range(1, steps + 1):
        d = run * (steps + 1 - k)
        box((width, d, rise * k), (x, y_top - d / 2, rise * k / 2), material=material)
        box((width + 0.02, 0.025, 0.02), (x, y_top - d - 0.005, rise * k - 0.01), material=LIME_DARK)       # Stufenkante
    front = run * steps
    for s in (-1, 1):
        xa = x + s * width / 2
        slab([(y_top - front, 0.0), (y_top, 0.0), (y_top, height + 0.11), (y_top - front, rise * steps + 0.10)], "yz",
             xa if s > 0 else xa - cheek, cheek, LIME_DARK)
        box((cheek + 0.05, 0.10, rise * steps + 0.14), (xa + s * cheek / 2, y_top - front - 0.04, (rise * steps + 0.14) / 2), material=LIME_DARK)
        sphere(0.055, (xa + s * cheek / 2, y_top - front - 0.04, rise * steps + 0.14 + 0.05), material=LIME, segs=8, rings=5)


# ---------------------------------------------------------------- Dienstgebäude (Feuerwache, Schule, Bibliothek, Akademie)

PAPYRUS = mat("Papyrus", (0.90, 0.83, 0.62))
WAX = mat("Wachs", (0.34, 0.20, 0.14), rough=0.5)
WOOL_BLUE = mat("WolleBlau", (0.32, 0.40, 0.58))
WOOL_GREY = mat("WolleGrau", (0.60, 0.58, 0.54))
SAIL_AWNING = mat("Sonnensegel", (0.92, 0.88, 0.76))


def arc_pts(cx, cz, r, a0, a1, step=15):
    """Punkte (x, z) auf einem Kreisbogen von a0 bis a1 Grad (beide eingeschlossen), z. B. für Bogenöffnungen."""
    count = max(1, round(abs(a1 - a0) / step))
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * k / count)),
             cz + r * math.sin(math.radians(a0 + (a1 - a0) * k / count))) for k in range(count + 1)]


def wheel_yz(x, y, z, r, thick=0.035, spokes=3):
    """Speichenrad mit Eisenreif, Nabe und Speichen; die Achse läuft in x-Richtung (Rad steht in der yz-Ebene)."""
    bpy.ops.mesh.primitive_torus_add(major_radius=r - 0.008, minor_radius=0.011, major_segments=16, minor_segments=5,
                                     location=(x, y, z), rotation=(0, math.radians(90), 0))
    keep(bpy.context.active_object, IRON)
    cyl(r - 0.004, thick, (x, y, z), rot=(0, 90, 0), material=WOOD_DARK, verts=16)
    cyl(r * 0.62, thick + 0.006, (x, y, z), rot=(0, 90, 0), material=WOOD, verts=14)       # Speichenfeld heller
    for k in range(spokes):
        box((thick + 0.008, 2 * (r - 0.012), 0.016), (x, y, z), rot=(k * 180.0 / spokes, 0, 0), material=WOOD_DARK)
    cyl(0.026, thick + 0.03, (x, y, z), rot=(0, 90, 0), material=WOOD, verts=8)               # Nabe


def scroll(x, y, z, yaw=0.0, length=0.12, r=0.012, tag=True):
    """Schriftrolle (Papyrus) mit hölzernen Stabknöpfen; liegt waagerecht, Achse bei yaw 0 in x-Richtung. Mit tag ein
    heller Anhänger (Titelschild) am Ende."""
    cyl(r, length, (x, y, z), rot=(0, 90, yaw), material=PAPYRUS, verts=8)
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    for k in (-1, 1):
        cyl(r * 0.55, 0.016, (x + c * k * (length / 2 + 0.007), y + s * k * (length / 2 + 0.007), z), rot=(0, 90, yaw),
            material=WOOD_DARK, verts=6)
    if tag:
        box((0.006, 0.016, 0.018), (x + c * (length / 2 + 0.012), y + s * (length / 2 + 0.012), z - r - 0.006),
            rot=(0, 0, yaw), material=CLOTH_RED)


def scroll_rack(x, y, yaw=0.0, w=0.46, h=0.62, d=0.16, rows=4, cols=3, floor_z=0.0):
    """Rollenschrank (Armarium) mit Fächern wie Taubenschläge, in jedem Fach liegen zwei Schriftrollen mit Ende zur
    Vorderseite (bei yaw 0 nach -y). Mittelpunkt der Rückwand-Unterkante bei (x, y)."""
    at = placed((x, y), yaw)
    z0 = floor_z
    t = 0.022
    box((w + 0.04, d + 0.03, 0.03), at((0, 0, z0 + 0.015)), rot=(0, 0, yaw), material=LIME_DARK)             # Sockel
    box((w, 0.02, h), at((0, d / 2 - 0.01, z0 + 0.03 + h / 2)), rot=(0, 0, yaw), material=WOOD_DARK)          # Rückwand
    for sx in (-1, 1):
        box((t, d, h), at((sx * (w / 2 - t / 2), 0, z0 + 0.03 + h / 2)), rot=(0, 0, yaw), material=WOOD)
    box((w + 0.03, d + 0.02, 0.025), at((0, 0, z0 + 0.03 + h + 0.012)), rot=(0, 0, yaw), material=WOOD)     # Deckplatte
    cw, ch = (w - 2 * t) / cols, (h - 0.02) / rows
    for i in range(1, cols):
        box((t * 0.7, d, h), at((-w / 2 + t + i * cw, 0, z0 + 0.03 + h / 2)), rot=(0, 0, yaw), material=WOOD)
    for j in range(rows):
        zz = z0 + 0.03 + j * ch
        box((w, d, 0.016), at((0, 0, zz + 0.008)), rot=(0, 0, yaw), material=WOOD)
    for j in range(rows):
        for i in range(cols):
            cx = -w / 2 + t + (i + 0.5) * cw
            zb = z0 + 0.03 + j * ch + 0.016
            rr = min(0.017, ch * 0.2)
            for k, off in enumerate((-0.28, 0.28)):
                px, py, _ = at((cx + off * cw, -d / 2 + 0.035, 0))
                cyl(rr, d - 0.07, (px, py, zb + rr), rot=(90, 0, yaw),
                    material=PAPYRUS if (i + j + k) % 3 else CLOTH, verts=7)
                ex, ey, _ = at((cx + off * cw, -d / 2 + 0.03, 0))
                cyl(rr * 0.65, 0.01, (ex, ey, zb + rr), rot=(90, 0, yaw), material=WOOD_DARK, verts=6)


def school_bench(x, y, yaw=0.0, length=0.62, tablets=2, seed=0):
    """Schulbank: lange Sitzbohle auf zwei Brettfüßen, davor (bei yaw 0 nach +y) ein schräges Pult mit Wachstafeln
    (Holzrahmen, dunkles Wachs) und Griffel. Die Kinder sitzen mit dem Rücken zur Straße (-y)."""
    rnd = random.Random(seed)
    at = placed((x, y), yaw)
    for k in (-1, 1):
        px, py, _ = at((k * length * 0.42, 0.04, 0))
        box((0.03, 0.17, 0.20), (px, py, 0.10), rot=(0, 0, yaw), material=WOOD_DARK)
    px, py, _ = at((0, 0, 0))
    box((length, 0.11, 0.025), (px, py, 0.21), rot=(0, 0, yaw), material=WOOD)
    dx, dy, _ = at((0, 0.17, 0))
    box((length, 0.15, 0.02), (dx, dy, 0.285), rot=(-12, 0, yaw), material=WOOD)                           # Pult, zu den Kindern geneigt
    for k in (-1, 1):
        lx, ly, _ = at((k * length * 0.44, 0.17, 0))
        box((0.025, 0.04, 0.27), (lx, ly, 0.135), rot=(0, 0, yaw), material=WOOD_DARK)
    for i in range(tablets):
        u = (-0.18 + 0.36 * i / max(1, tablets - 1)) * length / 0.62 if tablets > 1 else 0.0
        tx, ty, _ = at((u, 0.17, 0))
        tz = 0.299
        box((0.085, 0.060, 0.010), (tx, ty, tz), rot=(-12, 0, yaw + rnd.uniform(-6, 6)), material=WOOD_DARK)
        box((0.068, 0.045, 0.004), (tx, ty, tz + 0.006), rot=(-12, 0, yaw + rnd.uniform(-6, 6)), material=WAX)
    sx, sy, _ = at((length * 0.30, 0.20, 0))
    box((0.07, 0.006, 0.006), (sx, sy, 0.298), rot=(-12, 0, yaw + 20), material=IRON)                       # Griffel


def cathedra(x, y, yaw=0.0, z0=0.0):
    """Lehrerstuhl (Cathedra): Korbsessel aus Weide mit gewölbter Rückenlehne, Sitzkissen und Fußschemel; steht bei z0."""
    at = placed((x, y), yaw)
    for sx in (-1, 1):
        for sy in (-1, 1):
            px, py, _ = at((sx * 0.085, sy * 0.075, 0))
            cyl(0.014, 0.20, (px, py, z0 + 0.10), material=WICKER_DARK, verts=6)
    px, py, _ = at((0, 0, 0))
    box((0.21, 0.19, 0.035), (px, py, z0 + 0.205), rot=(0, 0, yaw), material=WICKER)
    bx, by, _ = at((0, 0.08, 0))
    sphere(0.115, (bx, by, z0 + 0.34), (1.0, 0.22, 0.95), WICKER, segs=12, rings=6, rot=(0, 0, yaw))       # Lehne, rund
    for sx in (-1, 1):
        ax, ay, _ = at((sx * 0.105, 0.0, 0))
        box((0.018, 0.17, 0.016), (ax, ay, z0 + 0.285), rot=(0, 0, yaw), material=WICKER_DARK)            # Armlehnen
        cyl(0.011, 0.085, (ax, ay, z0 + 0.24), material=WICKER_DARK, verts=6)
    kx, ky, _ = at((0, -0.01, 0))
    box((0.17, 0.15, 0.025), (kx, ky, z0 + 0.235), rot=(0, 0, yaw), material=CLOTH_RED)                    # Kissen
    fx, fy, _ = at((0, -0.20, 0))
    box((0.15, 0.08, 0.05), (fx, fy, z0 + 0.025), rot=(0, 0, yaw), material=WOOD_DARK)                     # Fußschemel


def ladder(p_bottom, p_top, across, width=0.24, rung_gap=0.115, rail=0.016):
    """Leiter von p_bottom nach p_top; across ist die waagerechte Richtung quer zur Leiter (Einheitsvektor)."""
    a, b = Vector(p_bottom), Vector(p_top)
    u = Vector(across).normalized() * (width / 2)
    bar(tuple(a + u), tuple(b + u), rail, WOOD)
    bar(tuple(a - u), tuple(b - u), rail, WOOD)
    length = (b - a).length
    n = max(2, int(length / rung_gap))
    for k in range(1, n + 1):
        t = k / (n + 1)
        c = a + (b - a) * t
        bar(tuple(c - u), tuple(c + u), rail * 0.8, WOOD_DARK)


def fire_hook(p_bottom, p_top, across):
    """Feuerhaken: langer Holzstab mit eisernem Haken (zum Einreißen brennender Dächer und Balken) an der Spitze, die
    zur Wand lehnt; across = waagerechte Richtung, in die der Haken zeigt."""
    a, b = Vector(p_bottom), Vector(p_top)
    rod(tuple(a), tuple(b), 0.011, WOOD, verts=6)
    d = (b - a).normalized()
    h = Vector(across).normalized()
    c1 = b + d * 0.02
    c2 = c1 + h * 0.045
    bar(tuple(b - d * 0.03), tuple(c1), 0.014, IRON)
    bar(tuple(c1), tuple(c2), 0.012, IRON)
    bar(tuple(c2), tuple(c2 - d * 0.045), 0.012, IRON)
    cyl(0.017, 0.03, tuple(b - d * 0.04), material=IRON, verts=6)


def fire_cart(x, y):
    """Löschkarren der Vigiles: zweirädriger Karren mit liegendem Wasserfass, dahinter eine Druckpumpe (Siphon: zwei
    Bronzezylinder und ein Schwengelbalken auf einem Bock), Standrohr mit Spritzdüse, aufgerollter Lederschlauch, vorn
    die Deichsel zum Ziehen. Zeigt nach -y, Breite 0,62 m, Länge etwa 1,0 m."""
    zb = 0.25
    box((0.42, 0.86, 0.035), (x, y, zb), material=WOOD)
    for sx in (-1, 1):
        box((0.035, 0.96, 0.045), (x + sx * 0.18, y + 0.0, zb - 0.036), material=WOOD_DARK)                  # Langbäume
        wheel_yz(x + sx * 0.285, y + 0.10, 0.155, 0.155)
    cyl(0.016, 0.64, (x, y + 0.10, 0.155), rot=(0, 90, 0), material=WOOD_DARK, verts=6)                      # Achse
    for sx in (-1, 1):                                                                                      # Deichsel
        rod((x + sx * 0.17, y - 0.30, zb - 0.04), (x + sx * 0.05, y - 0.62, 0.11), 0.016, WOOD_DARK, verts=6)
    box((0.14, 0.03, 0.03), (x, y - 0.58, 0.115), material=WOOD)
    # Wasserfass liegend, in zwei Holzlagern, mit Reifen und Einfüllstutzen
    fy = y - 0.17
    cyl(0.128, 0.46, (x, fy, zb + 0.018 + 0.128), rot=(90, 0, 0), material=WOOD, verts=14)
    for k in (-0.17, -0.06, 0.06, 0.17):
        cyl(0.134, 0.016, (x, fy + k, zb + 0.146), rot=(90, 0, 0), material=IRON, verts=14)
    for sy in (-0.16, 0.16):
        box((0.20, 0.03, 0.045), (x, fy + sy, zb + 0.033), material=WOOD_DARK)
    cyl(0.026, 0.05, (x, fy - 0.05, zb + 0.30), material=WOOD_DARK, verts=8)
    cyl(0.030, 0.012, (x, fy - 0.05, zb + 0.33), material=IRON, verts=8)
    # Pumpe: Doppelzylinder aus Bronze auf dem Hinterteil, Schwengel quer, Standrohr mit Düse nach hinten
    py = y + 0.27
    box((0.24, 0.12, 0.03), (x, py, zb + 0.032), material=WOOD_DARK)
    for sx in (-1, 1):
        cyl(0.034, 0.20, (x + sx * 0.060, py, zb + 0.145), material=BRONZE, verts=10)
        cyl(0.041, 0.018, (x + sx * 0.060, py, zb + 0.255), material=BRONZE, verts=10)
        cyl(0.011, 0.07, (x + sx * 0.060, py, zb + 0.30), material=IRON, verts=6)
    box((0.045, 0.045, 0.07), (x, py, zb + 0.30), material=WOOD_DARK)                                      # Bock
    bar((x - 0.24, py, zb + 0.352), (x + 0.24, py, zb + 0.352), 0.026, WOOD)                               # Schwengel
    for sx in (-1, 1):
        bar((x + sx * 0.24, py, zb + 0.352), (x + sx * 0.24, py, zb + 0.46), 0.017, WOOD_DARK)
        bar((x + sx * 0.28, py, zb + 0.46), (x + sx * 0.20, py, zb + 0.46), 0.021, WOOD_DARK)
    rod((x, py, zb + 0.20), (x, py + 0.22, zb + 0.16), 0.016, BRONZE, verts=8)                             # Standrohr
    cone(0.020, 0.008, 0.06, (x, py + 0.255, zb + 0.15), BRONZE, verts=8, rot=(-90, 0, 0))
    # Lederschlauch als Spirale auf dem Karren
    for k in range(3):
        bpy.ops.mesh.primitive_torus_add(major_radius=0.07 - k * 0.006, minor_radius=0.012, major_segments=14,
                                         minor_segments=5, location=(x + 0.10, y - 0.38, zb + 0.030 + k * 0.022))
        keep(bpy.context.active_object, WOOD_DARK)


def sun_sail(corners, sag=0.05, bands=8, colors=(None, None), nu=16, nv=6, thick=0.006):
    """Gespanntes Sonnensegel (Velum) über vier Eckpunkte (3D, der Reihe nach p0 p1 p2 p3): die Kante p0 -> p1 und die
    Kante p3 -> p2 liegen gegenüber; zwischen den Ecken hängt das Tuch um sag durch. Das Tuch ist in bands Streifen
    längs der Richtung p0 -> p3 geteilt, die abwechselnd colors[0] und colors[1] haben (None = Segeltuch)."""
    p = [Vector(c) for c in corners]

    def point(i, k, dz):
        u, v = i / nu, k / nv
        a = p[0] + (p[1] - p[0]) * u
        b = p[3] + (p[2] - p[3]) * u
        q = a + (b - a) * v
        return (q.x, q.y, q.z - sag * math.sin(math.pi * u) * math.sin(math.pi * v) + dz)

    def color(i):
        return colors[int((i + 0.5) / nu * bands) % 2] or SAIL_AWNING

    for dz, flip in ((0.0, False), (thick, True)):
        rings = [[point(i, k, dz) for i in range(nu + 1)] for k in range(nv + 1)]
        loft(rings, color, flip=flip, name="Segel")


def exedra(cx, cy, r, a0, a1, yaw=0.0, seat_z=0.17, step=12, back=True):
    """Halbrunde Marmorbank (Exedra): Sitzfläche auf einem Sockel entlang des Kreisbogens von a0 bis a1 Grad um
    (cx, cy) im Abstand r, wahlweise mit niedriger Lehne außen. yaw dreht den Bogen um den Mittelpunkt."""
    n = max(2, round(abs(a1 - a0) / step))
    for k in range(n):
        am = a0 + (a1 - a0) * (k + 0.5) / n + yaw
        chord = 2 * r * math.sin(math.radians(abs(a1 - a0) / n / 2)) + 0.012
        ca, sa = math.cos(math.radians(am)), math.sin(math.radians(am))
        rot = am + 90
        box((chord, 0.075, seat_z - 0.04), (cx + (r - 0.02) * ca, cy + (r - 0.02) * sa, (seat_z - 0.04) / 2),
            rot=(0, 0, rot), material=LIME)
        box((chord, 0.14, 0.035), (cx + r * ca, cy + r * sa, seat_z - 0.0175), rot=(0, 0, rot), material=MARBLE)
        if back:
            bx, by = cx + (r + 0.075) * ca, cy + (r + 0.075) * sa
            box((chord, 0.03, 0.14), (bx, by, seat_z + 0.07), rot=(0, 0, rot), material=MARBLE)
            box((chord, 0.045, 0.02), (bx, by, seat_z + 0.15), rot=(0, 0, rot), material=LIME_DARK)


def philosopher(x, y, yaw=0.0):
    """Marmorstatue eines Philosophen auf Sockel (Gewand mit Mantelfalten, Kopf mit Bart, Schriftrolle in der Hand);
    etwa 0,80 m hoch, blickt bei yaw 0 nach -y."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))

    def at(dx, dy):
        return (x + dx * c - dy * s, y + dx * s + dy * c)
    box((0.26, 0.26, 0.05), (x, y, 0.025), material=LIME_DARK)
    box((0.20, 0.20, 0.26), (x, y, 0.05 + 0.13), rot=(0, 0, yaw), material=LIME)
    box((0.25, 0.25, 0.04), (x, y, 0.05 + 0.26 + 0.02), rot=(0, 0, yaw), material=LIME_DARK)
    z0 = 0.37
    cone(0.085, 0.055, 0.30, (x, y, z0 + 0.15), MARBLE, verts=10)                        # Tunika und Mantel
    px, py = at(0.0, -0.045)
    sphere(0.045, (x, y, z0 + 0.31), (1.35, 0.8, 0.9), MARBLE, segs=8, rings=5, rot=(0, 0, yaw))   # Schultern
    sphere(0.036, (x, y, z0 + 0.375), material=MARBLE, segs=8, rings=5)                   # Kopf
    bx, by = at(0.0, -0.025)
    sphere(0.024, (bx, by, z0 + 0.345), (1.0, 0.7, 1.2), MARBLE, segs=6, rings=4)         # Bart
    ax, ay = at(-0.07, -0.04)
    bar((x - 0.0 + (ax - x), y + (ay - y), z0 + 0.27), (ax, ay, z0 + 0.15), 0.022, MARBLE)  # Arm mit Mantelfalte
    scx, scy = at(-0.075, -0.07)
    scroll(scx, scy, z0 + 0.14, yaw=yaw + 90, length=0.10, r=0.014, tag=False)
    ex, ey = at(0.08, -0.03)
    bar((ex, ey, z0 + 0.26), (ex, ey, z0 + 0.12), 0.022, MARBLE)                           # herabhängender Mantelzipfel


def sundial(x, y, h=0.5):
    """Sonnenuhr auf einer Säule: Fuß, Schaft, Platte mit Stundenlinien (dünne dunkle Leisten) und bronzenem Zeiger."""
    box((0.17, 0.17, 0.05), (x, y, 0.025), material=LIME_DARK)
    cyl(0.044, h - 0.12, (x, y, 0.05 + (h - 0.12) / 2), material=LIME, verts=10)
    cyl(0.07, 0.03, (x, y, h - 0.065), material=LIME_DARK, verts=10)
    cyl(0.125, 0.025, (x, y, h - 0.04), material=MARBLE, verts=16)
    cyl(0.113, 0.006, (x, y, h - 0.025), material=LIME, verts=16)
    for k in range(7):
        a = math.radians(-90 + 30 * k)
        bar((x, y, h - 0.020), (x + 0.108 * math.cos(a), y + 0.108 * math.sin(a), h - 0.020), 0.005, LIME_DARK)
    bar((x, y - 0.07, h - 0.02), (x, y + 0.01, h + 0.05), 0.011, BRONZE)                                  # Zeiger (Gnomon)


# ---------------------------------------------------------------- Holz, Zimmerei, Steinbruch (Produktionsbetriebe)

BARK = mat("Rinde", (0.36, 0.26, 0.17))
BARK_LIGHT = mat("RindeHell", (0.50, 0.38, 0.25))
END_GRAIN = mat("Hirnholz", (0.84, 0.69, 0.46))
END_GRAIN_DARK = mat("HirnholzDunkel", (0.70, 0.54, 0.34))
WOOD_FRESH = mat("HolzFrisch", (0.80, 0.64, 0.42))
SAPLING_PINE = mat("JungPinie", (0.27, 0.42, 0.24))
SAPLING_PINE_LIGHT = mat("JungPinieHell", (0.36, 0.50, 0.28))
SAPLING_CYPRESS = mat("JungZypresse", (0.14, 0.29, 0.20))
SAPLING_CYPRESS_LIGHT = mat("JungZypresseHell", (0.19, 0.35, 0.24))
SAPLING_OAK = mat("JungEiche", (0.22, 0.36, 0.21))
SAPLING_OAK_LIGHT = mat("JungEicheHell", (0.30, 0.44, 0.25))
QUARRY_LIGHT = mat("BruchHell", (0.73, 0.69, 0.60), rough=0.95)
QUARRY_MID = mat("BruchMitte", (0.65, 0.61, 0.53), rough=0.95)
QUARRY_DARK = mat("BruchDunkel", (0.53, 0.49, 0.42), rough=0.95)
QUARRY_BROWN = mat("BruchBraun", (0.69, 0.60, 0.46), rough=0.95)
ASHLAR = mat("Quader", (0.80, 0.78, 0.72), rough=0.9)


def solid_mesh(verts, faces, mat_index, materials, name="Teil"):
    """Geschlossener Körper aus Eckpunkten und Flächen (Normalen werden nach außen gerichtet); mat_index gibt je Fläche
    den Platz in materials an."""
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    for m in materials:
        mesh.materials.append(m)
    mesh.update()
    for p, i in zip(mesh.polygons, mat_index):
        p.material_index = i
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    PARTS.append(obj)
    return obj


def flat_face(verts, material):
    """Einzelne ebene Fläche; die Normale folgt der Reihenfolge der Punkte (gegen den Uhrzeigersinn von vorn gesehen)."""
    mesh = bpy.data.meshes.new("Flaeche")
    mesh.from_pydata(verts, [], [tuple(range(len(verts)))])
    mesh.update()
    obj = bpy.data.objects.new("Flaeche", mesh)
    bpy.context.collection.objects.link(obj)
    return keep(obj, material)


def log_piece(p0, p1, r, sides=7, bark=None, grain=None, r1=None):
    """Stamm oder Scheit von p0 nach p1 als Prisma mit Rinde an den Seiten und hellem Hirnholz an beiden Enden."""
    bark = bark or BARK
    grain = grain or END_GRAIN
    a, b = Vector(p0), Vector(p1)
    d = (b - a).normalized()
    ref = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
    u = d.cross(ref).normalized()
    v = d.cross(u).normalized()
    r1 = r if r1 is None else r1
    phase = random.random() * math.tau
    verts = []
    for c, rr in ((a, r), (b, r1)):
        for i in range(sides):
            t = phase + math.tau * i / sides
            verts.append(tuple(c + (u * math.cos(t) + v * math.sin(t)) * rr))
    faces = [(i, (i + 1) % sides, sides + (i + 1) % sides, sides + i) for i in range(sides)]
    faces += [tuple(range(sides)), tuple(range(sides, 2 * sides))]
    idx = [0] * sides + [1, 1]
    return solid_mesh(verts, faces, idx, [bark, grain], "Stamm")


def oriented_box(size, center, x_axis, z_axis, material):
    """Quader, dessen Seiten x und z in die angegebenen Richtungen zeigen (frei im Raum)."""
    ax = Vector(x_axis).normalized()
    az = Vector(z_axis).normalized()
    ay = az.cross(ax).normalized()
    ax = ay.cross(az).normalized()
    obj = box(size, tuple(center), material=material)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Matrix(((ax.x, ay.x, az.x), (ax.y, ay.y, az.y), (ax.z, ay.z, az.z))).to_quaternion()
    return obj


def chunky(x0, x1, y0, y1, z0, z1, material, jitter=0.03, seed=0, top_jitter=None, yaw=0.0):
    """Roher Gesteinsblock: Quader, dessen Eckpunkte leicht verschoben sind (oben stärker), flach schattiert; yaw dreht
    ihn um seine Mitte."""
    rnd = random.Random(seed)
    tj = jitter * 1.6 if top_jitter is None else top_jitter
    corners = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
               (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
    verts = []
    for k, (x, y, z) in enumerate(corners):
        j = tj if k >= 4 else jitter * 0.4
        verts.append((x + rnd.uniform(-j, j), y + rnd.uniform(-j, j), z + (rnd.uniform(-j, j) if k >= 4 else 0.0)))
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    if yaw:
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        verts = [(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c, z) for x, y, z in verts]
    return solid_mesh(verts, faces, [0] * 6, [material], "Block")


def firewood_stack(cx, cy, width, depth, rows, r=0.034, yaw=0.0, seed=0, profile=None):
    """Brennholzklafter: Die Scheite liegen parallel zur Tiefe; vorn (lokal -y, vor der Drehung um yaw) sieht man die
    hellen Stirnseiten im Sechseckverband (jede zweite Reihe versetzt, einzelne Scheite stehen vor), die oberste Reihe
    sind ganze Scheite mit Rinde. profile = Reihen je Spalte (ein angebrochener Stapel), sonst überall rows."""
    rnd = random.Random(seed)
    at = placed((cx, cy), yaw)
    pu, pz = 2 * r * 1.03, r * 1.74
    n = max(1, int(width / pu))
    heights = list(profile) if profile else [rows] * n
    n = len(heights)
    x_start = -n * pu / 2
    front = -depth / 2
    k = 0
    while k < n:                                   # Kern aus Rinde hinter den Stirnseiten
        e = k
        while e + 1 < n and heights[e + 1] == heights[k]:
            e += 1
        top = max(0.05, heights[k] * pz - r * 0.1)
        w = (e - k + 1) * pu
        p = at((x_start + k * pu + w / 2, front + depth / 2 + 0.004, top / 2))
        box((w, depth - 0.008, top), (p[0], p[1], top / 2), rot=(0, 0, yaw), material=BARK)
        k = e + 1
    for col in range(n):
        for row in range(heights[col]):
            u = x_start + (col + 0.5) * pu + (pu / 2 if row % 2 else 0.0)
            if row % 2 and col == n - 1:
                continue
            z = r + row * pz
            rr = r * rnd.uniform(0.86, 1.0)
            grain = END_GRAIN if rnd.random() > 0.38 else END_GRAIN_DARK
            if row == heights[col] - 1:            # oberste Reihe: ganze Scheite
                ln = depth + rnd.uniform(-0.02, 0.02)
                a, b = at((u, front - 0.005, z)), at((u, front + ln, z))
                log_piece((a[0], a[1], z), (b[0], b[1], z), rr, sides=6, bark=BARK_LIGHT if rnd.random() > 0.3 else BARK, grain=grain)
                continue
            if rnd.random() < 0.08:                # einzelne Scheite stehen vor
                a, b = at((u, front - 0.03, z)), at((u, front + 0.02, z))
                log_piece((a[0], a[1], z), (b[0], b[1], z), rr, sides=6, grain=grain)
                continue
            push = rnd.uniform(-0.012, 0.0) if rnd.random() < 0.35 else 0.0
            phase = rnd.random() * math.tau
            pts = []
            for i in range(6):
                t = phase + math.tau * i / 6
                q = at((u + math.cos(t) * rr * 0.92, front + push, 0.0))
                pts.append((q[0], q[1], z + math.sin(t) * rr * 0.92))
            flat_face(pts, grain)


def log_pile(cx, cy, length, r=0.08, rows=(4, 3, 2), yaw=0.0, seed=0, posts=True, bark=None):
    """Stammstapel: Stämme liegen entlang der lokalen y-Achse nebeneinander, darüber die nächste Reihe in den Lücken;
    vier Haltepfosten an den Enden. Gibt die Breite des Stapels zurück."""
    rnd = random.Random(seed)
    at = placed((cx, cy), yaw)
    pitch = 2 * r + 0.008
    width = rows[0] * pitch
    for row, n in enumerate(rows):
        for k in range(n):
            u = (k - (n - 1) / 2) * pitch
            z = r + row * r * 1.78
            ln = length * rnd.uniform(0.9, 1.04)
            off = rnd.uniform(-0.04, 0.04)
            a, b = at((u, off - ln / 2, 0.0)), at((u, off + ln / 2, 0.0))
            log_piece((a[0], a[1], z), (b[0], b[1], z), r * rnd.uniform(0.92, 1.06), sides=8, bark=bark)
    if posts:
        h = len(rows) * r * 1.8 + r * 0.6
        for su in (-1, 1):
            for sv in (-1, 1):
                p = at((su * (width / 2 + 0.035), sv * length * 0.38, 0.0))
                box((0.05, 0.05, h), (p[0], p[1], h / 2), rot=(0, 0, yaw), material=WOOD_DARK)
    return width


def chopping_block(x, y, yaw=0.0, r=0.14, h=0.27):
    """Hackklotz mit Rinde, rissiger heller Schnittfläche und eingeschlagener Axt (Stiel 30° über der Waagerechten,
    Schneide 5 cm im Holz); die Axt zeigt nach der lokalen +x-Richtung."""
    at = placed((x, y), yaw)
    cyl(r, h, (x, y, h / 2), material=BARK, verts=12)
    cyl(r * 0.94, 0.008, (x, y, h + 0.003), material=END_GRAIN, verts=12)
    for k in range(3):                                                        # Risse in der Schnittfläche
        a = k * 2.1 + 0.4
        box((r * 1.1, 0.008, 0.006), (x + math.cos(a) * 0.015, y + math.sin(a) * 0.015, h + 0.008),
            rot=(0, 0, math.degrees(a)), material=END_GRAIN_DARK)
    ang = 60
    a_ = math.radians(ang)
    ux, uz = math.sin(a_), math.cos(a_)                # vom Auge zum Griffende (lokal x, z)
    vx, vz = math.cos(a_), -math.sin(a_)               # vom Auge zur Schneide
    eye = (-0.04, h + 0.09)

    def p(lu, lz):
        w = at((lu, 0.0, 0.0))
        return (w[0], w[1], lz)
    cyl(0.014, 0.38, p(eye[0] + ux * 0.17, eye[1] + uz * 0.17), rot=(0, ang, yaw), material=WOOD, verts=6)
    box((0.11, 0.022, 0.045), p(eye[0] + vx * 0.03, eye[1] + vz * 0.03), rot=(0, ang, yaw), material=IRON)
    box((0.03, 0.010, 0.085), p(eye[0] + vx * 0.075, eye[1] + vz * 0.075), rot=(0, ang, yaw), material=IRON)


def split_logs(cx, cy, n, rmin=0.2, rmax=0.5, seed=0, z0=0.0):
    """Verstreute gespaltene Scheite (teils schräg an andere gelehnt) um einen Punkt."""
    rnd = random.Random(seed)
    for k in range(n):
        a = rnd.uniform(0, math.tau)
        rr = rnd.uniform(rmin, rmax)
        x, y = cx + math.cos(a) * rr, cy + math.sin(a) * rr
        yaw = rnd.uniform(0, 180)
        ln = rnd.uniform(0.17, 0.25)
        w = rnd.uniform(0.045, 0.065)
        if k % 4 == 3:       # ein Scheit lehnt schräg am nächsten
            box((ln, w, w * 0.9), (x, y, z0 + w * 0.9 + 0.012), rot=(0, rnd.uniform(14, 24), yaw), material=END_GRAIN_DARK)
            box((ln, w, w * 0.9), (x, y, z0 + w * 0.45), rot=(0, 0, yaw + 20), material=WOOD)
        else:
            box((ln, w, w * 0.9), (x, y, z0 + w * 0.45), rot=(0, 0, yaw), material=END_GRAIN if k % 2 else WOOD_FRESH)


def wood_chips(cx, cy, n, rmax, seed=0, z0=0.0):
    """Späne: kleine flache Schnipsel, dichter in der Mitte."""
    rnd = random.Random(seed)
    for _ in range(n):
        a = rnd.uniform(0, math.tau)
        rr = rmax * rnd.random() ** 1.4
        box((rnd.uniform(0.035, 0.06), rnd.uniform(0.014, 0.024), 0.006),
            (cx + math.cos(a) * rr, cy + math.sin(a) * rr, z0 + 0.004 + rnd.random() * 0.006),
            rot=(rnd.uniform(-12, 12), rnd.uniform(-12, 12), rnd.uniform(0, 180)),
            material=END_GRAIN if rnd.random() > 0.3 else WOOD_FRESH)


def sawhorse(x, y, yaw=0.0, log_len=0.9, with_saw=True):
    """Sägebock aus zwei Kreuzgestellen mit aufliegendem Stamm; die Bügelsäge steckt im Schnitt."""
    at = placed((x, y), yaw)
    for v in (-log_len * 0.32, log_len * 0.32):
        for a in (-26, 26):
            p = at((0.0, v, 0.0))
            box((0.045, 0.045, 0.52), (p[0], p[1], 0.20), rot=(0, a, yaw), material=WOOD_DARK)
    p = at((0.0, 0.0, 0.0))
    box((0.04, log_len * 0.78, 0.04), (p[0], p[1], 0.17), rot=(0, 0, yaw), material=WOOD_DARK)
    a0, a1 = at((0.0, -log_len / 2, 0.0)), at((0.0, log_len / 2, 0.0))
    log_piece((a0[0], a0[1], 0.40), (a1[0], a1[1], 0.40), 0.075, sides=8)
    if with_saw:
        c = at((0.0, 0.10, 0.0))
        box((0.008, 0.20, 0.035), (c[0], c[1], 0.50), rot=(0, 0, yaw), material=IRON)               # Blatt im Schnitt
        for sv in (-1, 1):
            q = at((0.0, 0.10 + sv * 0.10, 0.0))
            box((0.022, 0.022, 0.20), (q[0], q[1], 0.54), rot=(0, 0, yaw), material=WOOD_DARK)       # Bügel
        q = at((0.0, 0.10, 0.0))
        box((0.022, 0.24, 0.022), (q[0], q[1], 0.64), rot=(0, 0, yaw), material=WOOD_DARK)


def brushwood(x, y, z0=0.0, yaw=0.0, length=0.55, r=0.065, seed=0):
    """Reisigbündel: viele dünne Zweige, an den Enden leicht gespreizt, mit Bast zusammengebunden."""
    rnd = random.Random(seed)
    at = placed((x, y), yaw)
    for k in range(14):
        a = rnd.uniform(0, math.tau)
        rr = r * math.sqrt(rnd.random()) * 0.8
        px, pz = math.cos(a) * rr, math.sin(a) * rr
        spread = 1.0 + rnd.uniform(0.0, 0.5)
        ln = length * rnd.uniform(0.85, 1.05)
        p0 = at((px * spread, -ln / 2, 0.0))
        p1 = at((px * spread, ln / 2, 0.0))
        zz = z0 + r + pz * spread
        rod((p0[0], p0[1], zz), (p1[0], p1[1], zz), rnd.uniform(0.008, 0.013),
            rnd.choice((WOOD_DARK, BARK, WOOD, BARK_LIGHT)), verts=4)
    for v in (-0.12 * length, 0.12 * length):
        c = at((0.0, v, 0.0))
        cyl(r * 1.0, 0.03, (c[0], c[1], z0 + r), rot=(90, 0, yaw), material=SACK, verts=8)


def wheelbarrow(x, y, yaw=0.0, load="soil"):
    """Schubkarre: Mulde auf zwei Holmen, ein Rad vorn (lokal -y), zwei Beine hinten; Ladung 'soil' (Erdhaufen),
    'logs' (Scheite) oder None."""
    at = placed((x, y), yaw)

    def P(u, v, z):
        w = at((u, v, 0.0))
        return (w[0], w[1], z)
    for su in (-1, 1):
        rod(P(su * 0.11, 0.38, 0.30), P(su * 0.09, -0.20, 0.17), 0.013, WOOD_DARK, verts=5)               # Holm
        rod(P(su * 0.09, 0.14, 0.18), P(su * 0.12, 0.20, 0.0), 0.014, WOOD_DARK, verts=5)                 # Bein
        box((0.025, 0.12, 0.025), P(su * 0.11, 0.36, 0.30), rot=(0, 0, yaw), material=WOOD)               # Griff
    box((0.30, 0.34, 0.03), P(0.0, 0.0, 0.20), rot=(0, 0, yaw), material=WOOD)
    box((0.03, 0.34, 0.12), P(-0.155, 0.0, 0.25), rot=(0, 0, yaw), material=WOOD_DARK)
    box((0.03, 0.34, 0.12), P(0.155, 0.0, 0.25), rot=(0, 0, yaw), material=WOOD_DARK)
    box((0.30, 0.03, 0.12), P(0.0, 0.17, 0.25), rot=(0, 0, yaw), material=WOOD_DARK)
    box((0.26, 0.03, 0.10), P(0.0, -0.18, 0.24), rot=(-18, 0, yaw), material=WOOD_DARK)
    c = P(0.0, -0.30, 0.10)
    cyl(0.10, 0.04, c, rot=(0, 90, yaw), material=WOOD_DARK, verts=14)
    cyl(0.025, 0.12, c, rot=(0, 90, yaw), material=IRON, verts=6)
    if load == "soil":
        c = P(0.0, -0.02, 0.30)
        sphere(0.16, c, (1.0, 1.1, 0.5), SOIL, segs=8, rings=4)
    elif load == "logs":
        for k in range(3):
            a, b = P(-0.07 + k * 0.07, -0.13, 0.285), P(-0.07 + k * 0.07, 0.14, 0.285)
            log_piece(a, b, 0.034, sides=6)


def spade(foot, top, across=(1.0, 0.0, 0.0)):
    """Spaten: Holzstiel mit T-Griff, unten ein Eisenblatt (across = Breite des Blatts)."""
    a, b = Vector(foot), Vector(top)
    d = (b - a).normalized()
    ac = Vector(across).normalized()
    rod(tuple(a + d * 0.12), tuple(b), 0.011, WOOD, verts=5)
    oriented_box((0.10, 0.012, 0.15), a + d * 0.075, ac, d, IRON)
    oriented_box((0.05, 0.03, 0.035), a + d * 0.15, ac, d, WOOD_DARK)
    rod(tuple(b - ac * 0.07), tuple(b + ac * 0.07), 0.012, WOOD, verts=5)


def hoe(foot, top, across=(1.0, 0.0, 0.0)):
    """Hacke: Holzstiel, unten ein winklig angesetztes Eisenblatt."""
    a, b = Vector(foot), Vector(top)
    d = (b - a).normalized()
    ac = Vector(across).normalized()
    rod(tuple(a + d * 0.05), tuple(b), 0.011, WOOD, verts=5)
    n = d.cross(ac).normalized()
    oriented_box((0.11, 0.04, 0.014), a + d * 0.04 + n * 0.015, ac, n, IRON)
    oriented_box((0.03, 0.03, 0.05), a + d * 0.075, ac, d, IRON)


# -- junge Bäume (Förster): klein und aus wenigen Teilen, im Stil der Waldbäume aus gebirge.py


def lump(x, y, z, rx, ry, rz, material, rnd, segs=6, rings=4):
    """Laubballen: flaches Ellipsoid, zufällig gedreht."""
    return sphere(1.0, (x, y, z), (rx, ry, rz), material, segs=segs, rings=rings, rot=(0, 0, rnd.uniform(0, 180)))


def young_tree(x, y, z0, h, kind="oak", seed=0):
    """Junger Baum der Höhe h (0,2 bis 1,3 m): 'pine' (Schirmkrone auf schlankem Stamm), 'cypress' (schmale Flamme)
    oder 'oak' (runde dunkle Krone). Ab etwa 0,8 m ist die Art gut zu erkennen, darunter sind es Setzlinge."""
    rnd = random.Random(seed)
    tr = max(0.007, h * 0.024)
    if kind == "cypress":
        rod((x, y, z0 - 0.02), (x + h * 0.01, y, z0 + h * 0.3), tr, BARK, verts=5)
        n = 3 if h < 0.6 else 5
        for i in range(n):
            t = i / max(1, n - 1)
            w = h * (0.145 - 0.07 * t)
            zc = z0 + h * (0.20 + 0.72 * t) * 0.97
            m = SAPLING_CYPRESS_LIGHT if (i % 2 and i > 1) else SAPLING_CYPRESS
            lump(x + rnd.uniform(-0.01, 0.01) * h, y + rnd.uniform(-0.01, 0.01) * h, zc, w, w, h * (0.21 - 0.06 * t), m, rnd)
    elif kind == "pine":
        lean = h * 0.04
        rod((x, y, z0 - 0.02), (x + lean, y, z0 + h * 0.72), tr, BARK_LIGHT, verts=5)
        cr = h * (0.30 if h > 0.5 else 0.26)
        top = z0 + h * (0.78 if h > 0.5 else 0.70)
        for k in range(5 if h > 0.5 else 3):
            a = k * 2.4 + rnd.uniform(0, 1)
            rr = 0 if k == 0 else cr * 0.55
            lump(x + lean + math.cos(a) * rr, y + math.sin(a) * rr,
                 top + (0.03 * h if k == 0 else rnd.uniform(-0.04, 0.02) * h),
                 cr * (0.62 if k else 0.8), cr * (0.62 if k else 0.8), h * 0.09,
                 SAPLING_PINE_LIGHT if k % 2 else SAPLING_PINE, rnd)
    else:
        rod((x, y, z0 - 0.02), (x - h * 0.02, y, z0 + h * 0.45), tr * 1.1, BARK, verts=5)
        cr = h * 0.27
        top = z0 + h * 0.66
        lump(x - h * 0.02, y, top, cr, cr, cr * 0.85, SAPLING_OAK, rnd, segs=7, rings=4)
        for k in range(3):
            a = k * 2.1 + rnd.uniform(0, 1)
            lump(x - h * 0.02 + math.cos(a) * cr * 0.55, y + math.sin(a) * cr * 0.55, top + rnd.uniform(-0.1, 0.18) * cr,
                 cr * 0.55, cr * 0.55, cr * 0.5, SAPLING_OAK_LIGHT if k else SAPLING_OAK, rnd)


def seedling(x, y, seed=0):
    """Frisch gesetzter Keimling: kleiner Erdhügel mit Stützstab und zwei Blättern."""
    rnd = random.Random(seed)
    sphere(0.045, (x, y, 0.012), (1.0, 1.0, 0.45), SOIL, segs=7, rings=3)
    rod((x, y, 0.0), (x + 0.005, y, 0.19), 0.004, WOOD_DARK, verts=4)
    for s in (-1, 1):
        lump(x + s * 0.022, y, 0.14 + rnd.uniform(0, 0.02), 0.025, 0.012, 0.007, SAPLING_OAK_LIGHT, rnd, segs=5, rings=3)


def basket_with_sapling(x, y, size=0.22, kind="oak"):
    """Weidenkorb mit Erde und einem Setzling darin."""
    cone(0.085, 0.105, 0.11, (x, y, 0.055), WICKER, verts=9)
    cyl(0.108, 0.012, (x, y, 0.114), material=WICKER_DARK, verts=9)
    cyl(0.088, 0.01, (x, y, 0.112), material=SOIL, verts=9)
    young_tree(x, y, 0.11, size, kind, seed=int(abs(x) * 100 + abs(y) * 10))


# -- Zimmerei


def workbench(x, y, yaw=0.0):
    """Zimmermannsbank: dicke Platte auf vier gespreizten Beinen mit Bord, Schraubstock am Ende; darauf Hobel, Beitel,
    Klüpfel, ein eingespanntes Brett und Hobelspäne."""
    at = placed((x, y), yaw)

    def P(u, v, z):
        w = at((u, v, 0.0))
        return (w[0], w[1], z)
    H, L, Wd = 0.40, 0.84, 0.26
    box((Wd, L, 0.045), P(0.0, 0.0, H), rot=(0, 0, yaw), material=WOOD)
    box((Wd + 0.01, L + 0.01, 0.012), P(0.0, 0.0, H + 0.0275), rot=(0, 0, yaw), material=WOOD_FRESH)
    for sv in (-1, 1):
        for su in (-1, 1):
            rod(P(su * 0.105, sv * 0.36, 0.0), P(su * 0.09, sv * 0.34, H - 0.02), 0.022, WOOD_DARK, verts=5)
    box((Wd - 0.04, L - 0.12, 0.025), P(0.0, 0.0, 0.14), rot=(0, 0, yaw), material=WOOD_DARK)
    box((0.07, 0.05, 0.10), P(0.0, -L / 2 + 0.03, H + 0.03), rot=(0, 0, yaw), material=WOOD_DARK)           # Schraubstock
    rod(P(-0.11, -L / 2 - 0.02, H + 0.02), P(0.0, -L / 2 + 0.03, H + 0.02), 0.01, IRON, verts=5)
    box((0.04, 0.03, 0.03), P(-0.12, -L / 2 - 0.02, H + 0.02), rot=(0, 0, yaw), material=WOOD)
    box((0.12, 0.30, 0.014), P(0.0, -L / 2 + 0.17, H + 0.06), rot=(0, 0, yaw), material=WOOD_FRESH)        # Brett
    box((0.045, 0.14, 0.03), P(0.05, 0.12, H + 0.045), rot=(0, 0, yaw + 8), material=WOOD_DARK)            # Hobel
    box((0.035, 0.03, 0.045), P(0.05, 0.16, H + 0.07), rot=(0, 0, yaw + 8), material=WOOD)
    box((0.04, 0.01, 0.005), P(0.05, 0.12, H + 0.062), rot=(0, 0, yaw + 8), material=IRON)
    for k in range(3):                                                                                    # Beitel
        rod(P(-0.07 + k * 0.016, 0.32, H + 0.034), P(-0.07 + k * 0.016, 0.20, H + 0.034), 0.006,
            IRON if k else WOOD_DARK, verts=4)
    cyl(0.032, 0.07, P(-0.07, 0.04, H + 0.045), rot=(0, 90, yaw), material=WOOD, verts=8)                  # Klüpfel
    rod(P(-0.07, 0.04, H + 0.04), P(-0.07, -0.08, H + 0.035), 0.009, WOOD_DARK, verts=5)
    for k in range(4):                                                                                    # Hobelspäne
        sp = P(0.02 + 0.02 * k, 0.02 - 0.05 * k, H + 0.06)
        bpy.ops.mesh.primitive_torus_add(major_radius=0.016, minor_radius=0.003, major_segments=7, minor_segments=3,
                                         location=sp, rotation=(math.radians(70 + 20 * k), 0, math.radians(k * 50)))
        keep(bpy.context.active_object, END_GRAIN)


def sawpit(cx, cy, yaw=0.0, length=1.3):
    """Sägegrube: dunkle Grube zwischen zwei Bohlenrändern, darüber auf Querbalken ein Stamm, vorn noch rund, hinten
    schon in senkrechte Bretter zersägt; darin steckt die Rahmensäge (Blatt senkrecht im Schnitt, Rahmen mit zwei
    Armen und einem Seitenholm neben dem Stamm)."""
    at = placed((cx, cy), yaw)

    def P(u, v, z):
        w = at((u, v, 0.0))
        return (w[0], w[1], z)
    pw = 0.52
    box((pw, length, 0.012), P(0.0, 0.0, 0.006), rot=(0, 0, yaw), material=DARK)
    for su in (-1, 1):
        box((0.10, length + 0.1, 0.055), P(su * (pw / 2 + 0.05), 0.0, 0.0275), rot=(0, 0, yaw), material=WOOD_DARK)
    for sv in (-1, 1):
        box((pw + 0.24, 0.085, 0.07), P(0.0, sv * length * 0.36, 0.085), rot=(0, 0, yaw), material=WOOD)
    r = 0.115
    zc = 0.12 + r
    half = length * 0.5 + 0.08
    log_piece(P(0.0, -half, zc), P(0.0, -0.02, zc), r, sides=9, bark=BARK_LIGHT)
    for i in range(-3, 4):                    # Rückseite: senkrechte Bretter, nach außen leicht gespreizt
        u = i * 0.034
        hgt = 2 * math.sqrt(max(0.0, r * r - (u * 0.97) ** 2)) * 0.97
        if hgt < 0.03:
            continue
        board_len = half - 0.02
        box((0.030, board_len, hgt), P(u + i * 0.006, 0.02 + board_len / 2, zc), rot=(0, 0, yaw),
            material=BARK if abs(i) == 3 else WOOD_FRESH)
    box((0.02, 0.05, 0.10), P(0.0, half, zc), rot=(0, 0, yaw), material=WOOD_DARK)                       # Keil im Schnitt
    box((0.007, 0.075, 0.62), P(0.0, 0.0, zc + 0.10), rot=(0, 0, yaw), material=IRON)                    # Blatt
    for z in (zc + 0.36, 0.03):
        rod(P(0.0, 0.0, z), P(0.27, 0.0, z), 0.016, WOOD_DARK, verts=5)
    rod(P(0.27, 0.0, 0.03), P(0.27, 0.0, zc + 0.36), 0.016, WOOD_DARK, verts=5)
    box((0.04, 0.05, 0.035), P(0.0, 0.0, zc + 0.37), rot=(0, 0, yaw), material=WOOD)
    box((0.04, 0.05, 0.035), P(0.0, 0.0, 0.03), rot=(0, 0, yaw), material=WOOD)
    wood_chips(cx, cy, 24, 0.55, seed=5)


def plank_stack(cx, cy, w, length, layers, yaw=0.0, seed=0, board=0.030):
    """Bretterstapel auf zwei Unterlegern, zwischen den Lagen Stapelhölzer (Zwischenhölzer); die Lagen ragen unter-
    schiedlich weit heraus, die oberste ist in Bretter geteilt. Gibt die Höhe zurück."""
    rnd = random.Random(seed)
    at = placed((cx, cy), yaw)

    def P(u, v, z):
        wv = at((u, v, 0.0))
        return (wv[0], wv[1], z)
    gap = 0.022
    for su in (-1, 1):
        box((0.07, length * 0.9, 0.06), P(su * (w / 2 - 0.04), 0.0, 0.03), rot=(0, 0, yaw), material=WOOD_DARK)
    z = 0.06
    for k in range(layers):
        ln = length * rnd.uniform(0.94, 1.0)
        off = rnd.uniform(-0.02, 0.02)
        box((w, ln, board), P(rnd.uniform(-0.012, 0.012), off, z + board / 2), rot=(0, 0, yaw),
            material=(WOOD_FRESH, WOOD, WOOD_FRESH)[k % 3])
        for v in (-0.38, 0.0, 0.38):
            box((w * 0.98, 0.05, gap), P(0.0, v * length, z + board + gap / 2), rot=(0, 0, yaw), material=WOOD_DARK)
        z += board + gap
    for k in (1, 2):
        box((0.006, length * 0.96, 0.004), P(-w / 2 + k * w / 3, 0.0, z - gap + 0.001), rot=(0, 0, yaw), material=WOOD_DARK)
    return z


def beam_stack(cx, cy, length, beam=0.075, rows=(3, 2, 1), yaw=0.0, seed=0):
    """Balkenstapel: Kanthölzer mit hellem Hirnholz an den Enden, reihenweise übereinander, mit Stapelhölzern."""
    rnd = random.Random(seed)
    at = placed((cx, cy), yaw)

    def P(u, v, z):
        wv = at((u, v, 0.0))
        return (wv[0], wv[1], z)
    pitch = beam + 0.012
    for row, n in enumerate(rows):
        z = beam / 2 + 0.05 + row * (beam + 0.025)
        for k in range(n):
            u = (k - (n - 1) / 2) * pitch
            ln = length * rnd.uniform(0.92, 1.02)
            off = rnd.uniform(-0.03, 0.03)
            box((beam, ln, beam), P(u, off, z), rot=(0, 0, yaw), material=WOOD if (k + row) % 2 else WOOD_FRESH)
            for sv in (-1, 1):
                box((beam * 0.9, 0.004, beam * 0.9), P(u, off + sv * (ln / 2 + 0.001), z), rot=(0, 0, yaw), material=END_GRAIN)
        if row < len(rows) - 1:
            for v in (-length * 0.32, length * 0.32):
                box((rows[row] * pitch, 0.04, 0.025), P(0.0, v, z + beam / 2 + 0.012), rot=(0, 0, yaw), material=WOOD_DARK)
    for v in (-length * 0.32, length * 0.32):
        box((rows[0] * pitch + 0.04, 0.06, 0.05), P(0.0, v, 0.025), rot=(0, 0, yaw), material=WOOD_DARK)


def truss(x, y, width, height, yaw=0.0, thick=0.04, with_rafters=(True, True), collar=0.62, z_base=0.0):
    """Ein Sparrengebinde (Dreiecksbinder) in der Ebene quer zur lokalen y-Achse: zwei Sparren, Spannbalken unten und
    Zangenholz auf Höhe collar * height. Fuß bei z_base."""
    at = placed((x, y), yaw)

    def P(u, z):
        w = at((u, 0.0, 0.0))
        return (w[0], w[1], z_base + z)
    for s, on in zip((-1, 1), with_rafters):
        if on:
            bar(P(s * width / 2, 0.0), P(0.0, height), thick, WOOD)
    box((width + 0.02, thick, thick), P(0.0, thick / 2), rot=(0, 0, yaw), material=WOOD_DARK)
    if all(with_rafters):
        z = height * collar
        hw = width / 2 * (1 - collar)
        bar(P(-hw, z), P(hw, z), thick * 0.8, WOOD_FRESH)
        for s in (-1, 1):                                                             # Holznägel
            c = P(s * hw, z)
            cyl(0.008, thick * 1.3, c, rot=(90, 0, yaw), material=WOOD_DARK, verts=5)


def tool_wall(x0, x1, y_wall, z0, z1):
    """Werkzeugwand an der Rückwand (Fläche bei y_wall, Vorderseite nach -y): Bohlen und daran hängend Beile,
    Handsäge, Bohrer, Dechsel, Hammer und eine Schnur mit Lot."""
    cxm = (x0 + x1) / 2
    box((x1 - x0, 0.04, z1 - z0), (cxm, y_wall - 0.02, (z0 + z1) / 2), material=WOOD)
    n = int((x1 - x0) / 0.10)
    for k in range(1, n):
        box((0.006, 0.006, z1 - z0 - 0.02), (x0 + k * (x1 - x0) / n, y_wall - 0.043, (z0 + z1) / 2), material=WOOD_DARK)
    for z in (z0 + 0.05, z1 - 0.05):
        box((x1 - x0 + 0.04, 0.04, 0.045), (cxm, y_wall - 0.055, z), material=WOOD_DARK)
    yf = y_wall - 0.062
    slots = [x0 + 0.12 + k * (x1 - x0 - 0.24) / 5 for k in range(6)]
    zt = z1 - 0.14
    x = slots[0]                                                                           # Beil
    rod((x, yf, zt - 0.22), (x, yf, zt), 0.010, WOOD, verts=5)
    box((0.085, 0.016, 0.06), (x + 0.03, yf, zt), material=IRON)
    x = slots[1]                                                                           # Handsäge
    slab([(x - 0.05, zt - 0.28), (x + 0.07, zt - 0.28), (x + 0.07, zt - 0.14), (x - 0.05, zt - 0.02)], "xz", yf - 0.003, 0.004, IRON)
    box((0.04, 0.02, 0.06), (x - 0.07, yf, zt - 0.01), material=WOOD_DARK)
    x = slots[2]                                                                           # Bohrer
    rod((x, yf, zt - 0.30), (x, yf, zt - 0.02), 0.007, IRON, verts=4)
    rod((x - 0.07, yf, zt - 0.02), (x + 0.07, yf, zt - 0.02), 0.011, WOOD_DARK, verts=5)
    x = slots[3]                                                                           # Dechsel
    rod((x, yf, zt - 0.26), (x + 0.02, yf, zt), 0.010, WOOD, verts=5)
    box((0.02, 0.07, 0.06), (x + 0.02, yf, zt - 0.01), material=IRON)
    x = slots[4]                                                                           # Hammer
    rod((x, yf, zt - 0.20), (x, yf, zt - 0.02), 0.010, WOOD, verts=5)
    box((0.075, 0.03, 0.035), (x, yf, zt - 0.005), material=IRON)
    x = slots[5]                                                                           # zweites Beil
    rod((x, yf, zt - 0.34), (x, yf, zt - 0.14), 0.010, WOOD, verts=5)
    box((0.085, 0.016, 0.06), (x - 0.03, yf, zt - 0.13), material=IRON)
    sx = x0 + 0.06                                                                         # Schnur mit Lot
    box((0.006, 0.006, 0.20), (sx, yf, z1 - 0.12), material=SACK)
    cone(0.014, 0.0, 0.04, (sx, yf, z1 - 0.24), IRON, verts=6, rot=(180, 0, 0))
    for sxp in slots:                                                                      # Haken
        box((0.012, 0.016, 0.012), (sxp, yf + 0.004, zt + 0.002), material=WOOD_DARK)


def roof_frame(x, y, n, spacing, width, height, yaw=0.0, standing=3, wall=0.0):
    """Halb fertiger Dachstuhl: zwei Mauerlatten auf Steinen, n Sparrengebinde im Abstand spacing in Richtung der lokalen
    y-Achse; die ersten standing sind komplett und oben durch eine Firstpfette verbunden, das nächste hat nur einen
    Sparren, auf der linken Dachseite liegen schon Latten, ein weiterer Sparren liegt unfertig daneben. wall > 0: die
    Mauerlatten liegen auf zwei niedrigen Bruchsteinmauern dieser Höhe."""
    at = placed((x, y), yaw)

    def P(u, v, z):
        w = at((u, v, 0.0))
        return (w[0], w[1], z)
    length = spacing * (n - 1) + 0.2
    mid = spacing * (n - 1) / 2
    for su in (-1, 1):
        if wall > 0:
            for k in range(int(length / 0.16)):                      # Mauer aus Bruchsteinlagen
                v = -0.1 + 0.08 + k * 0.16
                for z0, hh in ((0.0, wall * 0.5), (wall * 0.5, wall * 0.5)):
                    box((0.16, 0.15, hh - 0.005), P(su * (width / 2 + 0.01), v + (0.08 if z0 else 0.0), z0 + hh / 2),
                        rot=(0, 0, yaw), material=(STONE, ROCK_B, ROCK_A)[k % 3])
            box((0.07, length, 0.07), P(su * (width / 2 + 0.01), mid, wall + 0.035), rot=(0, 0, yaw), material=WOOD_DARK)
        else:
            for v in (0.0, mid, spacing * (n - 1)):
                box((0.12, 0.10, 0.08), P(su * (width / 2 + 0.01), v, 0.04), rot=(0, 0, yaw), material=STONE)
            box((0.07, length, 0.07), P(su * (width / 2 + 0.01), mid, 0.115), rot=(0, 0, yaw), material=WOOD_DARK)
    z_base = wall + 0.07 if wall > 0 else 0.15
    for k in range(n):
        c = at((0.0, k * spacing, 0.0))
        if k < standing:
            truss(c[0], c[1], width, height, yaw, with_rafters=(True, True), z_base=z_base)
        elif k == standing:
            truss(c[0], c[1], width, height, yaw, with_rafters=(True, False), z_base=z_base)
    a = P(0.0, 0.0, z_base + height)
    b = P(0.0, spacing * (standing - 1), z_base + height)
    bar((a[0], a[1], a[2] + 0.03), (b[0], b[1], b[2] + 0.03), 0.05, WOOD_DARK)
    for j in range(1, 5):                                      # Latten auf der linken Dachseite der fertigen Gebinde
        t = j / 5.2
        p0 = P(-width / 2 * (1 - t), -0.05, z_base + height * t + 0.04)
        p1 = P(-width / 2 * (1 - t), spacing * (standing - 1) + 0.05, z_base + height * t + 0.04)
        bar(p0, p1, 0.025, WOOD_FRESH)
    q0, q1 = P(width / 2 + 0.28, spacing * 0.2, 0.13), P(width / 2 + 0.28, spacing * 1.6, 0.13)
    log_piece(q0, q1, 0.03, sides=5, bark=WOOD_FRESH)


# -- Steinbruch


def ashlar_stack(cx, cy, nx, ny, nz, yaw=0.0, seed=0, size=(0.34, 0.20, 0.15)):
    """Stapel behauener Quader (nx in die Breite, ny in die Tiefe, nz Lagen), mit leichtem Versatz und kleinen Fugen,
    unten auf zwei Balken, zwischen den Lagen Stapelhölzer."""
    rnd = random.Random(seed)
    at = placed((cx, cy), yaw)

    def P(u, v, z):
        w = at((u, v, 0.0))
        return (w[0], w[1], z)
    sx, sy, sz = size
    for su in range(2):
        box((0.06, ny * (sy + 0.015), 0.05), P((su - 0.5) * nx * (sx + 0.015) * 0.7, 0.0, 0.025), rot=(0, 0, yaw), material=WOOD_DARK)
    z = 0.05
    for lz in range(nz):
        for ix in range(nx):
            for iy in range(ny):
                u = (ix - (nx - 1) / 2) * (sx + 0.014) + rnd.uniform(-0.012, 0.012)
                v = (iy - (ny - 1) / 2) * (sy + 0.014) + rnd.uniform(-0.012, 0.012)
                h = sz * rnd.uniform(0.96, 1.03)
                box((sx * rnd.uniform(0.97, 1.0), sy, h), P(u, v, z + h / 2), rot=(0, 0, yaw + rnd.uniform(-2.5, 2.5)),
                    material=ASHLAR if (ix + iy + lz) % 3 else QUARRY_LIGHT)
        z += sz + 0.014


def quarry_block(x0, x1, y0, y1, z0, z1, faces=("-y",), seed=0):
    """Stufe einer Abbauwand aus geschichtetem Kalkstein. Sie besteht aus mehreren Abschnitten entlang x, die unterschiedlich
    weit vorn stehen; jeder hat einen rohen Kern, eine unebene Deckplatte und an den genannten Seiten übereinanderliegende
    Gesteinsbänke von 14 bis 30 cm (jede in ein bis zwei Brocken geteilt, verschieden weit vorstehend, in drei Grautönen,
    mit schiefen Kanten), dazu eine Reihe kleiner Keilspuren je Abschnitt und eine dunkle Spalte an den Grenzen."""
    rnd = random.Random(seed)
    segs = []
    x = x0
    while x < x1 - 0.05:
        w = rnd.uniform(0.5, 0.9)
        if x1 - (x + w) < 0.35:
            w = x1 - x
        segs.append((x, x + w))
        x += w
    mats = (QUARRY_LIGHT, QUARRY_MID, QUARRY_BROWN, QUARRY_LIGHT, QUARRY_MID, QUARRY_DARK)
    for i, (a, b) in enumerate(segs):
        sy0 = y0 + (rnd.uniform(0.0, 0.11) if i % 2 else 0.0)
        zt = z1 + rnd.uniform(-0.02, 0.02)
        chunky(a + 0.012, b - 0.012, sy0 + 0.012, y1, z0, zt - 0.03, QUARRY_DARK, jitter=0.012, seed=seed * 7 + i, top_jitter=0.0)
        chunky(a, b, sy0, y1, zt - 0.05, zt, QUARRY_MID, jitter=0.03, seed=seed * 7 + i + 50, top_jitter=0.02)
        for face in faces:
            if (face == "+x" and i != len(segs) - 1) or (face == "-x" and i != 0):
                continue
            lo, hi = (sy0, y1) if face in ("+x", "-x") else (a, b)
            z = z0
            while z < zt - 0.06:
                hh = min(rnd.uniform(0.14, 0.30), zt - 0.03 - z)
                if hh < 0.06:
                    break
                cuts = [lo, hi]
                if hi - lo > 0.5 and rnd.random() < 0.6:
                    cuts.insert(1, rnd.uniform(lo + 0.2, hi - 0.2))
                for ta, tb in zip(cuts, cuts[1:]):
                    if rnd.random() < 0.07:
                        continue
                    d = rnd.uniform(0.01, 0.075)
                    m = rnd.choice(mats)
                    if face == "-y":
                        chunky(ta + 0.01, tb - 0.01, sy0 - d, sy0 + 0.03, z, z + hh - 0.012, m, jitter=0.02,
                               seed=seed * 1000 + i * 100 + int(z * 100) + int(ta * 10), top_jitter=0.025)
                    elif face == "+y":
                        chunky(ta + 0.01, tb - 0.01, y1 - 0.03, y1 + d, z, z + hh - 0.012, m, jitter=0.02,
                               seed=seed * 1000 + i * 100 + int(z * 100) + int(ta * 10), top_jitter=0.025)
                    elif face == "-x":
                        chunky(a - d, a + 0.03, ta + 0.01, tb - 0.01, z, z + hh - 0.012, m, jitter=0.02,
                               seed=seed * 1000 + i * 100 + int(z * 100) + int(ta * 10), top_jitter=0.025)
                    else:
                        chunky(b - 0.03, b + d, ta + 0.01, tb - 0.01, z, z + hh - 0.012, m, jitter=0.02,
                               seed=seed * 1000 + i * 100 + int(z * 100) + int(ta * 10), top_jitter=0.025)
                z += hh
            if face == "-y":
                ku = a + (b - a) * rnd.uniform(0.3, 0.7)
                zz = z0 + 0.08
                while zz < zt - 0.08:
                    box((0.020, 0.012, 0.030), (ku, sy0 - 0.082, zz), material=QUARRY_DARK)
                    zz += 0.10
                if i:
                    box((0.025, 0.03, zt - z0 - 0.05), (a, sy0 - 0.02, (z0 + zt) / 2), material=DARK)


def wedge_row(p0, p1, z, count, seed=0):
    """Reihe von Keillöchern mit eisernen Spaltkeilen (so löst man Quader aus dem Fels) entlang der Strecke p0 -> p1
    auf der Oberseite einer Stufe in der Höhe z."""
    rnd = random.Random(seed)
    (xa, ya), (xb, yb) = p0, p1
    yaw = math.degrees(math.atan2(yb - ya, xb - xa))
    for k in range(count):
        t = (k + 0.5) / count
        x, y = xa + (xb - xa) * t, ya + (yb - ya) * t
        box((0.055, 0.03, 0.016), (x, y, z + 0.004), rot=(0, 0, yaw), material=DARK)
        if rnd.random() > 0.15:
            box((0.04, 0.016, 0.04), (x, y, z + 0.02), rot=(rnd.uniform(-8, 8), 0, yaw + rnd.uniform(-4, 4)), material=IRON)


def cut_groove(p0, p1, z, w=0.035):
    """Dunkle Schrotrinne (Spalt, mit dem ein Block vom Fels getrennt wird) auf der Oberseite einer Stufe."""
    (xa, ya), (xb, yb) = p0, p1
    length = math.hypot(xb - xa, yb - ya)
    yaw = math.degrees(math.atan2(yb - ya, xb - xa))
    box((length, w, 0.012), ((xa + xb) / 2, (ya + yb) / 2, z + 0.002), rot=(0, 0, yaw), material=DARK)


def rubble(cx, cy, r, n, seed=0, peak=0.22, size=1.0):
    """Schutthaufen aus kantigen Bruchstücken: in der Mitte höher und gröber, am Rand verstreute Splitter."""
    rnd = random.Random(seed)
    for k in range(n):
        a = rnd.uniform(0, math.tau)
        rr = r * math.sqrt(rnd.random())
        s = rnd.uniform(0.035, 0.11) * (1.3 - rr / r * 0.6) * size
        z = max(0.0, 1.0 - rr / r) * peak * rnd.uniform(0.4, 1.0)
        x, y = cx + math.cos(a) * rr, cy + math.sin(a) * rr
        chunky(x - s, x + s * 1.1, y - s * 0.9, y + s, z, z + s * rnd.uniform(0.7, 1.1),
               rnd.choice((QUARRY_LIGHT, QUARRY_MID, QUARRY_DARK, QUARRY_BROWN, QUARRY_LIGHT)), jitter=s * 0.4,
               seed=seed * 131 + k, yaw=rnd.uniform(0, 90))


def tripod_crane(cx, cy, height=1.35, spread=0.52, yaw=0.0, block_z=0.30, seed=0):
    """Dreibeinkran mit Flaschenzug: drei Rundhölzer, oben mit einem Tau verzurrt, daran die Rollenblöcke, darunter hängt an
    zwei Schlingen ein Quader knapp über dem Boden; hinter dem Kran eine Handwinde (Haspel) auf zwei Pfosten, zu der das Zugseil läuft."""
    rnd = random.Random(seed)
    apex = (cx, cy, height)
    for k in range(3):
        a = math.radians(yaw + 90 + 120 * k)
        base = (cx + spread * math.cos(a), cy + spread * math.sin(a), 0.0)
        top = (cx + 0.035 * math.cos(a), cy + 0.035 * math.sin(a), height - 0.02)
        log_piece(base, top, 0.034, sides=6, bark=BARK_LIGHT, r1=0.026)
        box((0.07, 0.07, 0.03), (base[0], base[1], 0.015), rot=(0, 0, math.degrees(a)), material=STONE)    # Fußstein
    for dz in (-0.06, 0.0, 0.06):                                     # Verzurrung
        cyl(0.062, 0.03, (cx, cy, height - 0.02 + dz * 0.6), material=ROPE, verts=8)
    z_up = height - 0.26
    z_low = block_z + 0.50
    rod((cx, cy, height - 0.03), (cx, cy, z_up + 0.06), 0.006, SACK, verts=4)
    box((0.07, 0.04, 0.12), (cx, cy, z_up), material=WOOD_DARK)
    cyl(0.032, 0.03, (cx, cy, z_up), rot=(0, 90, 0), material=IRON, verts=8)
    box((0.07, 0.04, 0.10), (cx, cy, z_low), material=WOOD_DARK)
    cyl(0.028, 0.03, (cx, cy, z_low), rot=(0, 90, 0), material=IRON, verts=8)
    for dx in (-0.018, 0.018):                                        # zwei Seilstränge zwischen den Rollen
        rod((cx + dx, cy, z_up - 0.05), (cx + dx, cy, z_low + 0.05), 0.005, SACK, verts=4)
    # Quader an zwei Schlingen
    bw, bd, bh = 0.34, 0.20, 0.20
    yawb = 12
    c = (cx, cy, block_z + bh / 2)
    box((bw, bd, bh), c, rot=(0, 0, yawb), material=ASHLAR)
    box((bw + 0.004, bd + 0.004, 0.012), (c[0], c[1], block_z + bh - 0.03), rot=(0, 0, yawb), material=QUARRY_LIGHT)
    top = (cx, cy, block_z + bh + 0.012)
    for s in (-1, 1):
        ex = cx + s * 0.12 * math.cos(math.radians(yawb))
        ey = cy + s * 0.12 * math.sin(math.radians(yawb))
        rod((cx, cy, z_low - 0.05), (ex, ey, block_z + bh + 0.01), 0.006, SACK, verts=4)
        cyl(0.012, 0.03, (ex, ey, block_z + bh - 0.02), material=IRON, verts=6)
    # Zugseil zur Handwinde hinter dem Kran
    wy = cy + spread + 0.42
    rod((cx, cy, height - 0.05), (cx, wy, 0.40), 0.005, SACK, verts=4)
    for s in (-1, 1):
        box((0.05, 0.05, 0.45), (cx + s * 0.20, wy, 0.225), material=WOOD_DARK)
        bar((cx + s * 0.20, wy, 0.2), (cx + s * 0.20, wy + 0.18, 0.0), 0.035, WOOD_DARK)
    cyl(0.05, 0.36, (cx, wy, 0.36), rot=(0, 90, 0), material=WOOD, verts=10)
    rod((cx + 0.20, wy, 0.36), (cx + 0.31, wy, 0.36 + 0.10), 0.011, WOOD_DARK, verts=5)               # Kurbel
    box((0.04, 0.03, 0.03), (cx + 0.32, wy, 0.46), material=WOOD)


def stonecutter_block(x, y, yaw=0.0):
    """Arbeitsblock des Steinmetzen: roher Block mit halb geglätteter Oberseite, darauf Schlägel, drei Meißel, Spaltkeile
    und Steinsplitter."""
    at = placed((x, y), yaw)

    def P(u, v, z):
        w = at((u, v, 0.0))
        return (w[0], w[1], z)
    chunky(x - 0.28, x + 0.28, y - 0.20, y + 0.20, 0.0, 0.28, QUARRY_LIGHT, jitter=0.02, seed=3, top_jitter=0.01, yaw=yaw)
    box((0.30, 0.34, 0.012), P(-0.10, 0.0, 0.287), rot=(0, 0, yaw), material=ASHLAR)                 # geglättete Fläche
    cyl(0.033, 0.11, P(0.14, -0.08, 0.31), rot=(0, 90, yaw + 20), material=WOOD, verts=8)             # Schlägel
    rod(P(0.14, -0.08, 0.31), P(0.14 - 0.14 * math.cos(math.radians(20)), -0.08 - 0.14 * math.sin(math.radians(20)), 0.30), 0.009, WOOD_DARK, verts=5)
    for k in range(3):                                                                               # Meißel
        a = yaw + 70 + k * 14
        rod(P(-0.12 + k * 0.07, -0.10, 0.296), P(-0.12 + k * 0.07 + 0.03, 0.08, 0.296), 0.007, IRON, verts=4)
        cyl(0.011, 0.012, P(-0.12 + k * 0.07 + 0.03, 0.085, 0.296), rot=(90, 0, yaw), material=IRON, verts=6)
    for k in range(3):                                                                               # Keile
        box((0.04, 0.02, 0.03), P(0.18, 0.06 + k * 0.045, 0.30), rot=(0, 0, yaw + k * 17), material=IRON)
    for k in range(7):
        s = random.Random(k).uniform(0.015, 0.03)
        box((s, s * 0.8, s * 0.5), P(-0.20 + k * 0.07, -0.02 + (k % 3) * 0.05, 0.296), rot=(0, 0, yaw + k * 30), material=QUARRY_MID)


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
        import os as _os
        only = _os.environ.get("PREVIEW_ONLY")                    # optional: nur diese Variante (Nummer) zeigen
        for i, obj in enumerate(VARIANTS):
            obj.location.x = (i - (len(VARIANTS) - 1) / 2) * spacing
            if only is not None:
                obj.location.x = 0.0
                obj.hide_render = (str(i) != only)
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
        import os                                                # optional: PREVIEW_YAW=180 PREVIEW_ZOOM=0.6 PREVIEW_AIM=0.5
        yaw = math.radians(float(os.environ.get("PREVIEW_YAW", "0")))
        zoom = float(os.environ.get("PREVIEW_ZOOM", "1"))
        cam.location = ((cam.location.x * math.cos(yaw) - cam.location.y * math.sin(yaw)) * zoom,
                        (cam.location.x * math.sin(yaw) + cam.location.y * math.cos(yaw)) * zoom,
                        cam.location.z * zoom * float(os.environ.get("PREVIEW_ELEV", "1")))
        cam.rotation_euler = (Vector((0.0, 0.0, float(os.environ.get("PREVIEW_AIMZ", "0.5")))) - cam.location).to_track_quat("-Z", "Y").to_euler()
        cam_data.lens = 40
        scene.camera = cam
        scene.render.filepath = PREVIEW
        bpy.ops.render.render(write_still=True)


# ================================================================ Betriebe Paket 2: Olivenhain, Ölpresse, Lehmgrube, Töpferei

OLIVE_BARK = mat("OlivenRinde", (0.42, 0.37, 0.31))
OLIVE_LEAF_LIGHT = mat("OlivenlaubHell", (0.68, 0.74, 0.60))
OLIVE_LEAF_DARK = mat("OlivenlaubDunkel", (0.40, 0.47, 0.32))
OLIVE_FRUIT = mat("OlivenDunkel", (0.24, 0.17, 0.26))
OLIVE_GREEN = mat("OlivenGruen", (0.46, 0.52, 0.22))
NET = mat("Erntenetz", (0.58, 0.52, 0.38))
OIL = mat("OelGold", (0.78, 0.64, 0.18), rough=0.25)
PASTE = mat("Olivenpaste", (0.30, 0.25, 0.17))
CLAY = mat("Lehm", (0.66, 0.40, 0.26))
CLAY_DARK = mat("LehmDunkel", (0.52, 0.31, 0.20))
CLAY_BRICK = mat("Lehmziegel", (0.74, 0.55, 0.38))
CLAY_UNFIRED = mat("TonHell", (0.80, 0.68, 0.54))
SLURRY = mat("Schlamm", (0.62, 0.45, 0.33), rough=0.35)
KILN_BRICK = mat("OfenZiegel", (0.64, 0.36, 0.25))
KILN_PLASTER = mat("OfenLehm", (0.76, 0.58, 0.42))
SOOT = mat("Russ", (0.13, 0.11, 0.10))
DONKEY = mat("Esel", (0.54, 0.49, 0.43))
DONKEY_LIGHT = mat("EselHell", (0.80, 0.75, 0.68))


def lathe(profile, x, y, z0=0.0, material=TERRACOTTA, segs=12, scale=1.0, name="Gefaess"):
    """Rotationskörper aus einem Profil (Radius, Höhe) von unten nach oben; ein Profil, das bei Radius 0 beginnt und
    endet, ergibt einen geschlossenen Körper (Krüge, Becken, Schalen)."""
    rings = len(profile)
    verts, faces = [], []
    for r, z in profile:
        for i in range(segs):
            t = math.tau * i / segs
            verts.append((x + math.cos(t) * r * scale, y + math.sin(t) * r * scale, z0 + z * scale))
    for k in range(rings - 1):
        for i in range(segs):
            j = (i + 1) % segs
            a, b, c, d = k * segs + i, k * segs + j, (k + 1) * segs + j, (k + 1) * segs + i
            if profile[k][0] == 0 and profile[k + 1][0] == 0:
                continue
            if profile[k][0] == 0:
                faces.append((a, c, d))
            elif profile[k + 1][0] == 0:
                faces.append((a, b, c))
            else:
                faces.append((a, b, c, d))
    return solid_mesh(verts, faces, [0] * len(faces), [material], name)


def bowl(x, y, z0, r, material=TERRACOTTA):
    """Flache Schale mit Standring und Rand."""
    lathe([(0, 0), (r * 0.40, 0), (r * 0.42, r * 0.06), (r * 0.85, r * 0.30), (r, r * 0.42), (r * 0.93, r * 0.42),
           (r * 0.80, r * 0.28), (r * 0.40, r * 0.10), (0, r * 0.10)], x, y, z0, material, segs=10, name="Schale")


def pot(x, y, z0, r, material=TERRACOTTA):
    """Bauchiger Topf mit weiter Öffnung."""
    lathe([(0, 0), (r * 0.55, 0), (r * 0.95, r * 0.55), (r, r * 0.95), (r * 0.80, r * 1.35), (r * 0.78, r * 1.50),
           (r * 0.90, r * 1.55), (r * 0.74, r * 1.55), (r * 0.62, r * 1.30), (0, r * 1.30)], x, y, z0, material,
          segs=10, name="Topf")


def dolium(x, y, h=0.40, r=0.17, lid=True):
    """Dolium: großes bauchiges Vorratsgefäß aus Ton mit Wulstrand, dazu ein Holzdeckel."""
    lathe([(0, 0), (r * 0.45, 0), (r * 0.85, h * 0.12), (r, h * 0.45), (r * 0.82, h * 0.82), (r * 0.62, h * 0.93),
           (r * 0.70, h * 0.97), (r * 0.62, h * 1.0), (0, h * 1.0)], x, y, 0.0, TERRACOTTA, segs=14, name="Dolium")
    if lid:
        cyl(r * 0.60, 0.02, (x, y, h + 0.01), material=WOOD, verts=12)
        cyl(0.025, 0.03, (x, y, h + 0.035), material=WOOD_DARK, verts=6)


def grass_tuft(x, y, h=0.10, seed=0):
    """Grasbüschel aus fünf schlanken Kegeln."""
    rnd = random.Random(seed)
    for k in range(5):
        a = rnd.uniform(0, math.tau)
        d = rnd.uniform(0.0, 0.025)
        cone(0.010, 0.0, h * rnd.uniform(0.7, 1.2), (x + math.cos(a) * d, y + math.sin(a) * d, h * 0.5),
             LEAF if k % 2 else LEAF_DARK, verts=4, rot=(rnd.uniform(-15, 15), rnd.uniform(-15, 15), 0))


def olives_scatter(cx, cy, w, d, n, z=0.012, seed=0, yaw=0.0):
    """Verstreute Oliven (dunkelviolett und grün) in einer Fläche w x d."""
    rnd = random.Random(seed)
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    for k in range(n):
        u, v = rnd.uniform(-w / 2, w / 2), rnd.uniform(-d / 2, d / 2)
        sphere(0.014, (cx + u * c - v * s, cy + u * s + v * c, z + 0.010), (1.0, 0.8, 1.15),
               OLIVE_FRUIT if rnd.random() > 0.3 else OLIVE_GREEN, segs=5, rings=3, rot=(0, 0, rnd.uniform(0, 180)))


def gnarled_olive(x, y, size=1.0, seed=0, lean=0.0):
    """Alter, knorriger Olivenbaum (Höhe etwa 1,3 m bei size 1): kräftiger, gedrehter Stamm, der sich in Gabelhöhe in
    zwei, drei Äste spaltet, silbrig graugrünes Laub in mehreren flachen Ballen (hell, mittel und dunkel gemischt)."""
    rnd = random.Random(seed)
    s = size
    cone(0.14 * s, 0.095 * s, 0.07 * s, (x, y, 0.035 * s), OLIVE_BARK, verts=8)                  # Wurzelanlauf
    for k in range(3):                                                                            # Wurzeln
        a = k * 2.1 + rnd.uniform(0, 1)
        rod((x + math.cos(a) * 0.05 * s, y + math.sin(a) * 0.05 * s, 0.05 * s),
            (x + math.cos(a) * 0.16 * s, y + math.sin(a) * 0.16 * s, 0.012), 0.022 * s, OLIVE_BARK, verts=5)
    pts = [(0.0, 0.0, 0.0), (0.035, -0.03, 0.10), (-0.03, 0.035, 0.20), (0.02, 0.0, 0.30)]         # gedrehter Stamm
    pts = [(x + (px + lean * pz) * s, y + py * s, pz * s) for px, py, pz in pts]
    radii = [0.105 * s, 0.088 * s, 0.078 * s, 0.07 * s]
    for i in range(3):
        log_piece(pts[i], pts[i + 1], radii[i], sides=7, bark=OLIVE_BARK, grain=OLIVE_BARK, r1=radii[i + 1])
    fork = Vector(pts[-1])
    tops = []
    for ang, reach, up in ((rnd.uniform(150, 200), 0.19, 0.48), (rnd.uniform(-30, 20), 0.20, 0.52), (rnd.uniform(80, 110), 0.09, 0.42)):
        a = math.radians(ang)
        mid = fork + Vector((math.cos(a) * reach * 0.35 * s, math.sin(a) * reach * 0.35 * s, 0.22 * s))
        mid.x += rnd.uniform(-0.03, 0.03) * s
        top = fork + Vector((math.cos(a) * reach * s, math.sin(a) * reach * s, up * s))
        log_piece(tuple(fork), tuple(mid), 0.050 * s, sides=6, bark=OLIVE_BARK, grain=OLIVE_BARK, r1=0.040 * s)
        log_piece(tuple(mid), tuple(top), 0.040 * s, sides=6, bark=OLIVE_BARK, grain=OLIVE_BARK, r1=0.022 * s)
        tops.append(top)
    mats = (OLIVE_LEAF, OLIVE_LEAF_LIGHT, OLIVE_LEAF_DARK)
    for i, top in enumerate(tops):
        for k in range(3):
            dx, dy = rnd.uniform(-0.10, 0.10) * s, rnd.uniform(-0.10, 0.10) * s
            dz = rnd.uniform(-0.10, 0.10) * s
            r = rnd.uniform(0.17, 0.24) * s
            lump(top.x + dx, top.y + dy, top.z + dz, r, r * rnd.uniform(0.9, 1.1), r * 0.52, mats[(i + k) % 3], rnd,
                 segs=7, rings=4)
    lump(fork.x, fork.y, fork.z + 0.40 * s, 0.20 * s, 0.20 * s, 0.12 * s, OLIVE_LEAF_DARK, rnd, segs=7, rings=4)


def harvest_net(cx, cy, w, d, yaw=0.0, olives=46, seed=0):
    """Ausgebreitetes Erntenetz (Tuch auf dem Boden, vier Heringe an den Ecken) mit heruntergeschlagenen Oliven."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    box((w, d, 0.008), (cx, cy, 0.006), rot=(0, 0, yaw), material=NET)
    for k in range(1, 5):                                                  # Maschenlinien
        u = -w / 2 + w * k / 5
        box((0.005, d, 0.004), (cx + u * c, cy + u * s, 0.011), rot=(0, 0, yaw), material=WOOD_DARK)
    for sx in (-1, 1):
        for sy in (-1, 1):
            u, v = sx * w / 2, sy * d / 2
            cyl(0.012, 0.07, (cx + u * c - v * s, cy + u * s + v * c, 0.03), rot=(sy * 12, -sx * 12, yaw), material=WOOD_DARK, verts=5)
    olives_scatter(cx, cy, w * 0.9, d * 0.9, olives, z=0.008, seed=seed, yaw=yaw)


def olive_basket(x, y, z0=0.0, r=0.085, fill=True, seed=0):
    """Erntekorb aus Weide, bis zum Rand voll dunkler Oliven."""
    rnd = random.Random(seed)
    h = r * 1.15
    cone(r * 0.8, r, h, (x, y, z0 + h / 2), WICKER, verts=10)
    for k in (0.3, 0.7):
        cyl(r * (0.8 + 0.2 * k) + 0.003, 0.012, (x, y, z0 + h * k), material=WICKER_DARK, verts=10)
    cyl(r * 1.02, 0.014, (x, y, z0 + h), material=WICKER_DARK, verts=10)
    if fill:
        sphere(r * 0.95, (x, y, z0 + h), (1.0, 1.0, 0.45), OLIVE_FRUIT, segs=8, rings=4)
        for k in range(7):
            a = rnd.uniform(0, math.tau)
            d = rnd.uniform(0, r * 0.6)
            sphere(0.015, (x + math.cos(a) * d, y + math.sin(a) * d, z0 + h + r * 0.40),
                   material=OLIVE_FRUIT if k % 3 else OLIVE_GREEN, segs=5, rings=3)


def donkey(x, y, yaw=0.0, baskets=True):
    """Esel (Maßstab 0,55, Kopfhöhe etwa 0,65 m) in Seitenansicht entlang der lokalen +x-Richtung, mit Packsattel und zwei
    Tragkörben voller Oliven."""
    at = placed((x, y), yaw)

    def P(u, v, z):
        w = at((u, v, 0.0))
        return (w[0], w[1], z)
    sphere(1.0, P(0.0, 0.0, 0.36), (0.30, 0.125, 0.145), DONKEY, rot=(0, 0, yaw))                    # Rumpf
    sphere(1.0, P(0.02, 0.0, 0.30), (0.24, 0.10, 0.10), DONKEY_LIGHT, rot=(0, 0, yaw))               # heller Bauch
    for su in (-1, 1):                                                                              # Beine
        for sv in (-1, 1):
            rod(P(su * 0.20, sv * 0.065, 0.28), P(su * 0.205, sv * 0.065, 0.015), 0.024, DONKEY, verts=6)
            cyl(0.026, 0.03, P(su * 0.205, sv * 0.065, 0.015), material=IRON, verts=6)
    rod(P(0.24, 0.0, 0.42), P(0.36, 0.0, 0.58), 0.062, DONKEY, verts=7)                              # Hals
    sphere(1.0, P(0.43, 0.0, 0.61), (0.105, 0.052, 0.058), DONKEY, rot=(0, -22, yaw))               # Kopf
    sphere(1.0, P(0.495, 0.0, 0.585), (0.04, 0.042, 0.04), DONKEY_LIGHT, rot=(0, -22, yaw))         # Maul
    for sv in (-1, 1):
        cone(0.022, 0.006, 0.12, P(0.37, sv * 0.035, 0.70), DONKEY, verts=5, rot=(sv * -10, 6, yaw))   # Ohren
    box((0.12, 0.012, 0.04), P(0.31, 0.0, 0.56), rot=(0, -50, yaw), material=IRON)                  # Mähne (dunkel)
    rod(P(-0.29, 0.0, 0.42), P(-0.32, 0.0, 0.22), 0.012, DONKEY, verts=5)                            # Schwanz
    sphere(0.028, P(-0.32, 0.0, 0.20), (1.0, 1.0, 1.6), IRON, segs=6, rings=4)
    box((0.30, 0.22, 0.035), P(-0.01, 0.0, 0.50), rot=(0, 0, yaw), material=WOOD_DARK)               # Packsattel
    for su in (-0.10, 0.12):                                                                        # Gurt
        box((0.025, 0.25, 0.20), P(su, 0.0, 0.36), rot=(0, 0, yaw), material=WOOD)
    if baskets:
        for sv in (-1, 1):
            c = P(0.0, sv * 0.17, 0.0)
            olive_basket(c[0], c[1], 0.27, r=0.095, seed=sv + 5)
            rod(P(0.0, sv * 0.075, 0.50), P(0.0, sv * 0.17, 0.40), 0.008, ROPE, verts=4)


def lever_press(u_pier, u_free, v, z=0.62, u_press=-0.55, axis="x", fill=None):
    """Balkenpresse (Prelum): Der lange Hebelbalken steckt am Ende u_pier in einem Mauerpfeiler, liegt bei u_press auf
    dem Stapel aus Flechtkörben (Fiscinae) auf einem Steinbett mit Rinne und trägt am freien Ende u_free zwischen zwei
    Pfosten einen hängenden Gegengewichtsstein. axis 'x': Der Balken liegt parallel zur x-Achse bei y = v; axis 'y':
    parallel zur y-Achse bei x = v (u ist dann die y-Koordinate). Gibt die Höhe der Balkenoberkante zurück."""
    sg = 1 if u_free > u_pier else -1
    fill = fill or PASTE                           # Füllung der Körbe und der Rinne (bei Wein Maische)

    def G(a, b):                                   # (entlang, quer) -> (x, y)
        return (a, b) if axis == "x" else (b, a)

    def Z(a, b, c):                                # Maße (entlang, quer, Höhe) -> Maße in x, y, z
        return (a, b, c) if axis == "x" else (b, a, c)

    def At(a, b, h):
        return (*G(a, b), h)
    box(Z(0.30, 0.34, 1.10), At(u_pier + sg * 0.07, v, 0.55), material=LIME)                         # Mauerpfeiler
    lat = ("-y", "+y") if axis == "x" else ("-x", "+x")
    end = (("+x" if sg > 0 else "-x") if axis == "x" else ("+y" if sg > 0 else "-y"),)
    px0, py0 = G(u_pier + sg * 0.07 - 0.15, v - 0.17)
    px1, py1 = G(u_pier + sg * 0.07 + 0.15, v + 0.17)
    brick_bands(min(px0, px1), max(px0, px1), min(py0, py1), max(py0, py1), (0.30, 0.60, 0.90), faces=lat + end)
    box(Z(0.14, 0.18, 0.15), At(u_pier + sg * 0.24, v, z), material=DARK)                            # Balkenschlitz
    bar(At(u_pier + sg * 0.10, v, z), At(u_free + sg * 0.15, v, z), 0.12, WOOD_DARK)                 # Hebelbalken
    for k in range(4):                                                                              # Eisenbänder
        box(Z(0.012, 0.13, 0.13), At(u_pier + sg * (0.55 + k * 0.55), v, z), material=IRON)
    box(Z(0.70, 0.62, 0.07), At(u_press, v, 0.035), material=LIME_DARK)                               # Pressbett
    box(Z(0.62, 0.04, 0.012), At(u_press, v - 0.20, 0.076), material=fill)                           # Rinne
    for k in range(4):                                                                              # Fiscinae
        disc = 0.22 - 0.01 * (k % 2)
        cyl(disc, 0.065, At(u_press, v, 0.105 + k * 0.075), material=WICKER if k % 2 else WICKER_DARK, verts=12)
        sphere(disc * 0.97, At(u_press, v, 0.138 + k * 0.075), (1.0, 1.0, 0.18), fill, segs=10, rings=3)
    box((0.30, 0.30, 0.06), At(u_press, v, 0.435), material=WOOD)                                    # Pressplatte
    box((0.22, 0.22, 0.08), At(u_press, v, 0.505), material=WOOD_DARK)                               # Pressklotz
    for s_ in (-1, 1):                                                                              # Gegengewicht
        box((0.07, 0.07, 1.12), At(u_free + sg * 0.02, v + s_ * 0.16, 0.56), material=WOOD_DARK)
    box(Z(0.12, 0.44, 0.08), At(u_free + sg * 0.02, v, 1.14), material=WOOD_DARK)
    stone_z = 0.30
    box(Z(0.26, 0.24, 0.30), At(u_free - sg * 0.12, v, stone_z), rot=(0, 0, 4), material=LIME_DARK)
    rod(At(u_free - sg * 0.12, v, stone_z + 0.15), At(u_free - sg * 0.12, v, z - 0.04), 0.012, ROPE, verts=5)
    rod(At(u_free - sg * 0.18, v, stone_z + 0.15), At(u_free - sg * 0.12, v, z - 0.04), 0.008, ROPE, verts=4)
    return z + 0.06


def settling_basin(x, y, r=0.17, fill=OIL):
    """Rundes Absetzbecken aus Stein, bis kurz unter den Rand mit goldenem Öl gefüllt."""
    lathe([(0, 0), (r, 0), (r + 0.025, 0.03), (r + 0.025, 0.14), (r - 0.025, 0.14), (r - 0.025, 0.05), (0, 0.05)],
          x, y, 0.0, LIME_DARK, segs=14, name="Becken")
    cyl(r - 0.024, 0.008, (x, y, 0.118), material=fill, verts=14)


def trapetum(x, y, yaw=0.0, r=0.50):
    """Trapetum: runder Steintrog mit mittlerer Säule, an der ein Holzbalken umläuft; auf dem Balken zwei halbkugelige
    Mahlsteine rollen durch die Olivenpaste (Griffe an den Balkenenden)."""
    at = placed((x, y), yaw)

    def P(u, v, z):
        w = at((u, v, 0.0))
        return (w[0], w[1], z)
    lathe([(0, 0), (r, 0), (r + 0.03, 0.03), (r + 0.03, 0.25), (r - 0.05, 0.25), (r - 0.07, 0.12), (0, 0.12)],
          x, y, 0.0, LIME, segs=18, name="Trapetum")
    cyl(r - 0.07, 0.012, (x, y, 0.118), material=PASTE, verts=18)
    olives_scatter(x, y, r * 1.2, r * 1.2, 14, z=0.12, seed=3)
    cone(0.10, 0.06, 0.12, (x, y, 0.18), LIME_DARK, verts=10)                                         # Säulenfuß
    cyl(0.055, 0.50, (x, y, 0.40), material=LIME, verts=10)                                           # Säule
    box((0.13, 0.13, 0.05), (x, y, 0.675), rot=(0, 0, yaw), material=LIME_DARK)
    box((1.18, 0.065, 0.07), (x, y, 0.32), rot=(0, 0, yaw), material=WOOD_DARK)                      # Balken
    box((0.09, 0.09, 0.09), (x, y, 0.32), rot=(0, 0, yaw), material=IRON)
    for s_ in (-1, 1):
        c = P(s_ * 0.30, 0.0, 0.0)
        sphere(0.17, (c[0], c[1], 0.285), (0.42, 1.0, 1.0), LIME_DARK, segs=12, rings=7, rot=(0, 0, yaw))  # Mahlstein
        cyl(0.075, 0.03, (c[0], c[1], 0.285), rot=(0, 90, yaw), material=IRON, verts=8)
        g = P(s_ * 0.58, 0.0, 0.0)
        cyl(0.018, 0.30, (g[0], g[1], 0.46), material=WOOD, verts=6)                                    # Griff
        sphere(0.026, (g[0], g[1], 0.62), material=WOOD, segs=6, rings=4)


def clay_mound(x, y, r=0.30, h=0.22, seed=0):
    """Lehmhaufen: breiter, dunkler Haufen mit helleren Brocken."""
    rnd = random.Random(seed)
    cone(r, r * 0.55, h * 0.55, (x, y, h * 0.275), CLAY, verts=10)
    sphere(r * 0.55, (x, y, h * 0.55), (1.0, 1.0, h * 0.45 / (r * 0.55)), CLAY, segs=9, rings=4, rot=(0, 0, rnd.uniform(0, 90)))
    for k in range(5):
        a = rnd.uniform(0, math.tau)
        d = rnd.uniform(0.2, 0.8) * r
        rr = rnd.uniform(0.18, 0.30) * r
        sphere(rr, (x + math.cos(a) * d, y + math.sin(a) * d, h * (1 - d / r) * 0.6 + rr * 0.72),
               (1.0, rnd.uniform(0.8, 1.1), 0.7), CLAY_DARK if k % 2 else CLAY_BRICK, segs=6, rings=4)


def mud_brick_row(x0, y0, count, yaw=0.0, gap=0.025, rows=1, row_gap=0.10):
    """Reihen geformter Lehmziegel, die zum Trocknen flach auf dem Boden liegen (von x0,y0 aus entlang yaw)."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    for j in range(rows):
        for i in range(count):
            u, v = i * (0.135 + gap), j * (0.08 + row_gap)
            box((0.135, 0.08, 0.04), (x0 + u * c - v * s, y0 + u * s + v * c, 0.02),
                rot=(0, 0, yaw + (1.5 if (i + j) % 2 else -1.5)), material=CLAY_BRICK if (i + j) % 3 else CLAY)


def brick_hurdle(x, y, yaw=0.0, layers=5):
    """Trockenstapel: Lehmziegel kreuzweise geschichtet, damit die Luft hindurchstreicht."""
    c, s = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    for k in range(layers):
        rot = yaw + (90 if k % 2 else 0)
        for i in range(2):
            off = (i - 0.5) * 0.115
            ox, oy = (-s * off, c * off) if k % 2 == 0 else (c * off, s * off)
            box((0.135, 0.08, 0.04), (x + ox, y + oy, 0.02 + k * 0.042), rot=(0, 0, rot),
                material=CLAY_BRICK if k % 2 else CLAY)


def clay_basin(x0, x1, y0, y1, fill=SLURRY, h=0.12):
    """Schlämmbecken: Bohlenwände mit Pfosten an den Ecken, außen ein Lehmwall, innen glänzend nasse Lehmbrühe."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    box((x1 - x0, y1 - y0, 0.012), (cx, cy, h * 0.74), material=fill)
    for y, sy in ((y0, -1), (y1, 1)):
        box((x1 - x0 + 0.10, 0.07, h), (cx, y, h / 2), material=WOOD)
        box((x1 - x0 + 0.20, 0.10, 0.05), (cx, y + sy * 0.04, 0.025), material=CLAY_DARK)
    for x, sx in ((x0, -1), (x1, 1)):
        box((0.07, y1 - y0, h), (x, cy, h / 2), material=WOOD)
        box((0.10, y1 - y0 + 0.20, 0.05), (x + sx * 0.04, cy, 0.025), material=CLAY_DARK)
    for x in (x0, x1):
        for y in (y0, y1):
            box((0.075, 0.075, h + 0.07), (x, y, (h + 0.07) / 2), material=WOOD_DARK)


def potters_wheel(x, y, yaw=0.0, vessel=True):
    """Töpferscheibe mit Fußantrieb: Gestell mit Tischplatte, unten die schwere Schwungscheibe, durch die Platte die
    Achse zur oberen Wurfscheibe, darauf ein Gefäß in Arbeit; dazu ein Hocker und ein Wassereimer."""
    at = placed((x, y), yaw)

    def P(u, v, z):
        w = at((u, v, 0.0))
        return (w[0], w[1], z)
    box((0.36, 0.30, 0.025), P(0.0, 0.0, 0.27), rot=(0, 0, yaw), material=WOOD)
    for su in (-1, 1):
        for sv in (-1, 1):
            rod(P(su * 0.15, sv * 0.12, 0.0), P(su * 0.14, sv * 0.11, 0.26), 0.02, WOOD_DARK, verts=5)
    for sv in (-1, 1):
        box((0.30, 0.025, 0.025), P(0.0, sv * 0.12, 0.10), rot=(0, 0, yaw), material=WOOD_DARK)
    c0 = P(0.0, 0.0, 0.0)
    cyl(0.17, 0.035, (c0[0], c0[1], 0.115), material=WOOD_DARK, verts=16)                       # Schwungscheibe
    cyl(0.018, 0.22, (c0[0], c0[1], 0.22), material=IRON, verts=6)                               # Achse
    cyl(0.075, 0.022, (c0[0], c0[1], 0.305), material=WOOD, verts=12)                            # Wurfscheibe
    if vessel:
        lathe([(0, 0), (0.05, 0), (0.07, 0.04), (0.075, 0.08), (0.06, 0.13), (0.05, 0.145), (0.06, 0.155), (0.048, 0.155),
               (0.04, 0.14), (0, 0.04)], c0[0], c0[1], 0.316, CLAY_UNFIRED, segs=10, name="Werkstueck")
    sx, sy, _ = P(0.0, -0.36, 0.0)                                                                  # Hocker
    cyl(0.085, 0.03, (sx, sy, 0.21), material=WOOD, verts=10)
    for k in range(3):
        a = k * 2.1
        rod((sx + math.cos(a) * 0.05, sy + math.sin(a) * 0.05, 0.20),
            (sx + math.cos(a) * 0.09, sy + math.sin(a) * 0.09, 0.0), 0.014, WOOD_DARK, verts=5)
    wx, wy, _ = P(0.30, 0.06, 0.0)
    cone(0.06, 0.075, 0.09, (wx, wy, 0.045), WOOD, verts=10)
    cyl(0.062, 0.006, (wx, wy, 0.085), material=WATER, verts=10)


def kiln(x, y, r=0.52):
    """Brennofen aus Ziegeln: Sockel mit Ziegelschichten, darüber eine lehmverputzte Kuppel mit Eisenreifen und einer
    Rauchöffnung mit kurzem Schlot; vorn (-y) die Feuerstelle mit Ziegelgewände, Rußspuren und Aschehaufen. Gibt die
    Höhe der Schlotmündung zurück."""
    base_h = 0.36
    cyl(r, base_h, (x, y, base_h / 2), material=KILN_BRICK, verts=20)
    for k in range(1, 6):                                                                            # Ziegelschichten
        cyl(r + 0.006, 0.012, (x, y, k * base_h / 6), material=TILE_DARK, verts=20)
    cyl(r + 0.04, 0.04, (x, y, 0.02), material=STONE, verts=20)
    dome = [(0.0, base_h)] + [(r * math.cos(math.radians(a)), base_h + r * 0.92 * math.sin(math.radians(a))) for a in range(0, 90, 15)]
    dome.append((0.0, base_h + r * 0.92))
    lathe(dome, x, y, 0.0, KILN_PLASTER, segs=18, name="Kuppel")                                      # Kuppel
    for z_ in (base_h + 0.10, base_h + 0.26):                                                         # Reifen
        rr = math.sqrt(max(0.0, 1 - ((z_ - base_h) / (r * 0.92)) ** 2)) * r
        cyl(rr + 0.008, 0.022, (x, y, z_), material=IRON, verts=20)
    top = base_h + r * 0.92
    cyl(0.085, 0.16, (x, y, top + 0.05), material=KILN_BRICK, verts=10)                               # Schlot
    cyl(0.095, 0.03, (x, y, top + 0.14), material=TILE_DARK, verts=10)
    cyl(0.055, 0.012, (x, y, top + 0.157), material=SOOT, verts=10)
    fy = y - r + 0.01                                                                               # Feuerloch
    box((0.34, 0.10, 0.34), (x, fy - 0.02, 0.17), material=KILN_BRICK)
    box((0.20, 0.06, 0.20), (x, fy - 0.075, 0.12), material=SOOT)
    cyl(0.10, 0.06, (x, fy - 0.075, 0.22), rot=(90, 0, 0), material=SOOT, verts=10)
    box((0.30, 0.06, 0.03), (x, fy - 0.08, 0.015), material=LIME_DARK)                                # Schwelle
    for sx in (-1, 1):
        box((0.045, 0.07, 0.24), (x + sx * 0.14, fy - 0.075, 0.12), material=TILE_DARK)
    for k in range(5):                                                                              # Bogenziegel
        a = math.radians(-70 + k * 35)
        box((0.045, 0.07, 0.035), (x + math.sin(a) * 0.145, fy - 0.075, 0.24 + math.cos(a) * 0.08),
            rot=(0, -math.degrees(a), 0), material=TILE_DARK)
    sphere(0.16, (x, fy - 0.06, 0.34), (1.1, 0.5, 0.9), SOOT, segs=8, rings=4)                         # Ruß über dem Loch
    sphere(0.10, (x + 0.06, fy - 0.16, 0.03), (1.4, 1.0, 0.5), SOOT, segs=7, rings=4)                 # Asche
    sphere(0.06, (x - 0.09, fy - 0.17, 0.02), (1.2, 1.0, 0.5), PAVE_WORN, segs=6, rings=3)
    return top + 0.16


def smoke_plume(x, y, z0, n=3):
    """Dünner Rauchfaden aus wenigen wachsenden, grauen Kugeln (nur Geometrie)."""
    for k in range(n):
        r = 0.045 + 0.022 * k
        sphere(r, (x + 0.03 * k, y + 0.02 * k, z0 + 0.10 + k * 0.15), (1.0, 1.0, 0.85), SMOKE, segs=8, rings=5)


def vessel_shelf(x, y, yaw=0.0, w=1.2, tiers=3, fired=True, seed=0, d=0.16):
    """Regal mit Gefäßen auf mehreren Brettern: gebrannte (Terrakotta) oder noch ungebrannte (heller Ton)."""
    rnd = random.Random(seed)
    at = placed((x, y), yaw)

    def P(u, v, z):
        w_ = at((u, v, 0.0))
        return (w_[0], w_[1], z)
    mat_v = TERRACOTTA if fired else CLAY_UNFIRED
    hh = 0.20 * tiers + 0.08
    for su in (-1, 1):
        for sv in (-1, 1):
            c = P(su * (w / 2 - 0.015), sv * (d / 2 - 0.015), 0.0)
            box((0.035, 0.035, hh), (c[0], c[1], hh / 2), rot=(0, 0, yaw), material=WOOD_DARK)
    for t in range(tiers):
        z = 0.18 + t * 0.20
        box((w, d, 0.025), P(0.0, 0.0, z), rot=(0, 0, yaw), material=WOOD)
        n = int(w / 0.20)
        for i in range(n):
            u = -w / 2 + (i + 0.5) * w / n
            c = P(u, rnd.uniform(-0.02, 0.02), 0.0)
            kind = (i + t) % 3
            if kind == 0:
                jug(c[0], c[1], z + 0.012, 0.038, mat_v)
            elif kind == 1:
                pot(c[0], c[1], z + 0.012, 0.050, mat_v)
            else:
                bowl(c[0], c[1], z + 0.012, 0.062, mat_v)


# ================================================================ Betriebe Paket 3: Weinberg und Kelterei

GRAPE_LEAF_A = mat("RebLaubA", (0.33, 0.49, 0.19))
GRAPE_LEAF_B = mat("RebLaubB", (0.22, 0.38, 0.17))
GRAPE_LEAF_C = mat("RebLaubC", (0.50, 0.57, 0.24))
GRAPE_BLUE = mat("TraubeBlau", (0.20, 0.19, 0.44), rough=0.4)
GRAPE_VIOLET = mat("TraubeViolett", (0.42, 0.17, 0.50), rough=0.4)
GRAPE_RED = mat("TraubeRot", (0.60, 0.13, 0.22), rough=0.4)
GRAPE_MASS = mat("Maische", (0.32, 0.09, 0.20), rough=0.45)
MUST = mat("Most", (0.38, 0.05, 0.09), rough=0.22)
VINE_BARK = mat("RebRinde", (0.34, 0.25, 0.18))
GRAPES = (GRAPE_BLUE, GRAPE_VIOLET, GRAPE_RED)


def grape_cluster(x, y, z_top, g=0.026, seed=0, colors=GRAPES[:2]):
    """Hängendes Traubenbüschel: kleine Kugeln in drei Lagen (3, 2, 1), unten spitz zulaufend."""
    rnd = random.Random(seed)
    m = colors[rnd.randrange(len(colors))]
    cyl(0.003, 0.03, (x, y, z_top - 0.005), material=VINE_BARK, verts=4)
    z = z_top - 0.03
    a0 = rnd.uniform(0, math.tau)
    for li, (n, rr) in enumerate(((3, 0.021), (1, 0.0))):
        for i in range(n):
            a = a0 + math.tau * i / n + li
            sphere(g, (x + math.cos(a) * rr, y + math.sin(a) * rr, z - li * g * 1.5), (1, 1, 1.1), m, segs=4, rings=3)


def grape_pile(cx, cy, z, w, d, n, seed=0, colors=GRAPES, g=0.021):
    """Haufen loser Trauben (Kugeln in drei Farben) in einer Ellipse w x d, in der Mitte höher."""
    rnd = random.Random(seed)
    for k in range(n):
        a = rnd.uniform(0, math.tau)
        rr = math.sqrt(rnd.random())
        u, v = math.cos(a) * rr * w / 2, math.sin(a) * rr * d / 2
        sphere(g * rnd.uniform(0.85, 1.15), (cx + u, cy + v, z + 0.045 * (1 - rr * rr) + g * 0.6), (1, 1, 0.9),
               colors[rnd.randrange(len(colors))], segs=4, rings=3)


def grapevine(x, y, size=1.0, seed=0, clusters=2):
    """Rebstock am Spalier (Zeile längs y): knorriger, gedrehter Stamm bis zum mittleren Draht (z = 0,5), Draht hochgeführt,
    dichtes Laubband in drei Grüntönen (satt und kräftig) und hängende Trauben an beiden Seiten."""
    rnd = random.Random(seed)
    s = size
    pts = [(x, y, 0.0), (x + 0.020, y - 0.014, 0.22), (x + 0.004, y, 0.50)]
    for i in range(2):
        log_piece(pts[i], pts[i + 1], 0.030 - 0.004 * i, sides=5, bark=VINE_BARK, grain=VINE_BARK, r1=0.026 - 0.004 * i)
    mats = (GRAPE_LEAF_A, GRAPE_LEAF_B, GRAPE_LEAF_C)
    k0 = rnd.randrange(3)
    sphere(1.0, (x, y - 0.10, 0.50), (0.17 * s, 0.27 * s, 0.23 * s), mats[k0 % 3], segs=6, rings=4)
    sphere(1.0, (x, y + 0.14, 0.52), (0.17 * s, 0.27 * s, 0.23 * s), mats[(k0 + 1) % 3], segs=6, rings=4)
    sphere(1.0, (x + rnd.uniform(-0.02, 0.02), y, 0.74), (0.12 * s, 0.22 * s, 0.10 * s), mats[(k0 + 2) % 3], segs=6, rings=4)
    for c in range(clusters):
        side = -1 if c % 2 == 0 else 1
        grape_cluster(x + side * 0.19, y + rnd.uniform(-0.16, 0.16), 0.40, seed=seed * 7 + c)


def trellis(x, y0, y1, h=0.80, spacing=0.54):
    """Spalier einer Rebzeile längs y bei x: Holzpfähle mit Spitze und drei Querdrähten auf 0,3 / 0,5 / 0,7 m."""
    n = max(1, round((y1 - y0) / spacing))
    for k in range(n + 1):
        y = y0 + k * (y1 - y0) / n
        box((0.04, 0.04, h), (x, y, h / 2 - 0.03), material=WOOD_DARK)
        cone(0.026, 0.0, 0.05, (x, y, h - 0.005), WOOD_DARK, verts=4)
    for z in (0.30, 0.50, 0.70):
        box((0.014, y1 - y0, 0.014), (x, (y0 + y1) / 2, z), material=WOOD if z == 0.50 else IRON)


def grape_basket(x, y, z0=0.0, r=0.085, seed=0):
    """Lesekorb aus Weide, gehäuft voll mit blauen und violetten Trauben."""
    h = r * 1.15
    cone(r * 0.8, r, h, (x, y, z0 + h / 2), WICKER, verts=10)
    for k in (0.3, 0.7):
        cyl(r * (0.8 + 0.2 * k) + 0.003, 0.012, (x, y, z0 + h * k), material=WICKER_DARK, verts=10)
    cyl(r * 1.02, 0.014, (x, y, z0 + h), material=WICKER_DARK, verts=10)
    sphere(r * 0.95, (x, y, z0 + h), (1.0, 1.0, 0.45), GRAPE_MASS, segs=8, rings=4)
    grape_pile(x, y, z0 + h + 0.005, r * 1.5, r * 1.5, 9, seed=seed, g=0.021)


def vat(x, y, z0=0.0, r=0.10, h=0.17, seed=0):
    """Bütte: schwach kegeliger Holzbottich mit drei Eisenreifen, bis über den Rand voll Trauben."""
    cone(r * 0.88, r, h, (x, y, z0 + h / 2), WOOD, verts=12)
    for k in (0.15, 0.50, 0.88):
        cyl(r * (0.88 + 0.12 * k) + 0.004, 0.012, (x, y, z0 + h * k), material=IRON, verts=12)
    cyl(r * 1.02, 0.014, (x, y, z0 + h), material=WOOD_DARK, verts=12)
    sphere(r * 0.95, (x, y, z0 + h), (1.0, 1.0, 0.5), GRAPE_MASS, segs=8, rings=4)
    grape_pile(x, y, z0 + h + 0.01, r * 1.7, r * 1.7, 16, seed=seed)


def sickle(x, y, z, flat=False, yaw=0.0):
    """Rebmesser (Sichel): kurzer Holzgriff, gekrümmte Eisenklinge; an der Wand in der xz-Ebene (flat False) oder
    flach auf dem Boden bzw. einem Gegenstand in der xy-Ebene (flat True, um yaw gedreht)."""
    c, s_ = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))

    def P(u, v):
        return (x + (u * c - v * s_), y + (u * s_ + v * c), z) if flat else (x + u, y, z + v)
    rod(P(-0.06, 0.0), P(0.0, 0.0), 0.011, WOOD_DARK, verts=6)
    prev = P(0.0, 0.0)
    for k in range(1, 5):
        a = math.radians(-20 + k * 38)
        cur = P(0.05 * math.sin(a), 0.05 * (1 - math.cos(a)))
        rod(prev, cur, 0.0055, IRON, verts=4)
        prev = cur


def _basin_walls(x0, x1, y0, y1, h, wall, cap_h):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    box((x1 - x0, wall, h), (cx, y0 + wall / 2, h / 2), material=LIME)
    box((x1 - x0, wall, h), (cx, y1 - wall / 2, h / 2), material=LIME)
    box((wall, y1 - y0 - 2 * wall, h), (x0 + wall / 2, cy, h / 2), material=LIME)
    box((wall, y1 - y0 - 2 * wall, h), (x1 - wall / 2, cy, h / 2), material=LIME)
    box((x1 - x0 + 0.03, wall + 0.03, cap_h), (cx, y0 + wall / 2, h + cap_h / 2), material=LIME_DARK)
    box((x1 - x0 + 0.03, wall + 0.03, cap_h), (cx, y1 - wall / 2, h + cap_h / 2), material=LIME_DARK)
    box((wall + 0.03, y1 - y0 - 2 * wall, cap_h), (x0 + wall / 2, cy, h + cap_h / 2), material=LIME_DARK)
    box((wall + 0.03, y1 - y0 - 2 * wall, cap_h), (x1 - wall / 2, cy, h + cap_h / 2), material=LIME_DARK)


def tread_basin(x0, x1, y0, y1, h=0.30, wall=0.08, spout_x=None, seed=0):
    """Calcatorium: gemauertes Tretbecken (Kalkstein mit Ziegelbändern), gehäuft voll roter und violetter Trauben, an
    der Vorderwand ein Ausguss aus Stein. Gibt den Austritt des Mostes (x, y, z) zurück."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    box((x1 - x0, y1 - y0, 0.05), (cx, cy, 0.025), material=LIME_DARK)
    _basin_walls(x0, x1, y0, y1, h, wall, 0.03)
    brick_bands(x0, x1, y0, y1, (0.10, 0.20))
    iw, idp = x1 - x0 - 2 * wall, y1 - y0 - 2 * wall
    zf = h - 0.075
    sphere(1.0, (cx, cy, zf), (iw / 2 * 0.98, idp / 2 * 0.98, 0.085), GRAPE_MASS, segs=12, rings=4)
    grape_pile(cx, cy, zf + 0.03, iw * 0.9, idp * 0.9, 62, seed=seed, colors=(GRAPE_RED, GRAPE_VIOLET, GRAPE_BLUE, GRAPE_RED))
    sx = cx if spout_x is None else spout_x
    box((0.07, 0.15, 0.06), (sx, y0 - 0.045, h - 0.14), material=LIME)                         # Ausguss
    box((0.04, 0.05, 0.012), (sx, y0 - 0.095, h - 0.108), material=MUST)
    return sx, y0 - 0.12, h - 0.11


def must_basin(x0, x1, y0, y1, h=0.14, wall=0.06):
    """Niedriges, gemauertes Mostbecken, bis fast zum Rand voll dunkelrotem Most."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    box((x1 - x0, y1 - y0, 0.04), (cx, cy, 0.02), material=LIME_DARK)
    _basin_walls(x0, x1, y0, y1, h, wall, 0.025)
    brick_bands(x0, x1, y0, y1, (0.07,))
    box((x1 - x0 - 2 * wall + 0.01, y1 - y0 - 2 * wall + 0.01, 0.02), (cx, cy, h - 0.035), material=MUST)


def buried_dolium(x, y, r=0.17, open_lid=False, yaw=0.0):
    """Eingegrabenes Dolium: nur Hals, Wulstrand und Deckel ragen aus dem Boden, ein Steinkranz liegt darum.
    open_lid: Deckel schräg angelehnt, dunkler Most in der Öffnung."""
    lathe([(r * 0.78, 0.0), (r * 1.32, 0.0), (r * 1.32, 0.022), (r * 1.05, 0.045), (r * 0.82, 0.05)], x, y, 0.0, LIME_DARK,
          segs=14, name="Kranz")
    lathe([(r * 0.80, 0.04), (r * 0.76, 0.085), (r * 0.60, 0.115), (r * 0.54, 0.145), (r * 0.64, 0.165), (r * 0.58, 0.178),
           (r * 0.46, 0.178)], x, y, 0.0, TERRACOTTA, segs=14, name="DoliumHals")
    if open_lid:
        cyl(r * 0.47, 0.004, (x, y, 0.172), material=MUST, verts=12)
        cyl(r * 0.60, 0.02, (x + r * 0.55, y, 0.20), rot=(0, 62, yaw), material=WOOD, verts=12)
    else:
        cyl(r * 0.62, 0.022, (x, y, 0.19), material=WOOD, verts=12)
        cyl(0.024, 0.03, (x, y, 0.22), material=WOOD_DARK, verts=6)


def vine_row(x, y0, y1, seed=0, vine_gap=0.55, mid_post_gap=0.55):
    """Rebzeile am Spalier längs y bei x (von y0 bis y1): kräftige Endpfähle mit Querholz, schmalere Pfähle dazwischen,
    drei Drähte zwischen den Endpfählen; kurze, knorrige Stämme (0,3 m), die sich oben in zwei Arme (Kordon) entlang des
    untersten Drahts teilen. Darüber ein zusammenhängendes Laubband (0,31..0,78 m, etwa 0,17 m breit) aus fünf Lagen
    kleiner, flacher, stark überlappender Büschel (jede Lage überlappt die darunter um etwa ein Drittel, die unterste
    sitzt auf dem Kordon); dünne grüne und braune Triebe laufen senkrecht vom Kordon durch das Laub und liegen an den
    Drähten an. Unter dem Band hängen an Stielen Trauben."""
    rnd = random.Random(seed)
    length = y1 - y0
    for y in (y0, y1):                                                            # Endpfähle mit Querholz
        box((0.05, 0.05, 0.80), (x, y, 0.37), material=WOOD_DARK)
        box((0.20, 0.035, 0.035), (x, y, 0.70), material=WOOD)
    n = max(1, round(length / mid_post_gap))
    for k in range(1, n):                                                         # Zwischenpfähle, Köpfe im Laub
        box((0.04, 0.04, 0.66), (x, y0 + k * length / n, 0.30), material=WOOD_DARK)
    for z, m in ((0.30, IRON), (0.52, IRON), (0.68, WOOD)):                       # Drähte
        box((0.012, length, 0.012), (x, (y0 + y1) / 2, z), material=m)
    mats = (GRAPE_LEAF_A, GRAPE_LEAF_B, GRAPE_LEAF_C)
    nv = max(2, int(length / vine_gap))
    for k in range(nv):                                                           # Rebstöcke mit Kordon
        y = y0 + (k + 0.5) * length / nv
        p0, p2 = (x, y, 0.0), (x, y, 0.30)
        log_piece(p0, p2, 0.028, sides=5, bark=VINE_BARK, grain=VINE_BARK, r1=0.022)
        for sg in (-1, 1):
            rod((x, y, 0.29), (x, y + sg * vine_gap * 0.5, 0.31), 0.011, VINE_BARK, verts=4)
    step = 0.13
    ny = int(length / step)
    for layer, z in enumerate((0.37, 0.455, 0.54, 0.625, 0.71)):                 # Laubband: kleine, überlappende Büschel
        for i in range(ny + 1):
            y = y0 + 0.02 + (i + (0.5 if layer % 2 else 0.0)) * (length - 0.04) / ny
            if y > y1 - 0.01:
                continue
            sphere(1.0, (x + rnd.uniform(-0.02, 0.02), y, z + rnd.uniform(-0.012, 0.012)),
                   (rnd.uniform(0.055, 0.065), rnd.uniform(0.09, 0.105), 0.06),
                   mats[(i + layer + rnd.randrange(2)) % 3], segs=4, rings=2, rot=(0, 0, rnd.uniform(0, 80)))
    for i in range(ny):                                                           # Triebe oben, im Laub verankert
        if rnd.random() < 0.4:
            sphere(1.0, (x + rnd.uniform(-0.02, 0.02), y0 + 0.1 + i * (length - 0.2) / ny, 0.77),
                   (0.05, 0.07, 0.045), mats[rnd.randrange(3)], segs=4, rings=2)
    nt = max(3, int(length / 0.26))
    for i in range(nt):                                                           # Triebe vom Kordon durchs Laub
        y = y0 + 0.12 + i * (length - 0.24) / (nt - 1)
        dx = rnd.uniform(-0.012, 0.012)
        rod((x + dx, y, 0.31), (x + dx * 0.4, y + rnd.uniform(-0.02, 0.02), 0.77), 0.0075,
            GRAPE_LEAF_B if i % 2 else VINE_BARK, verts=4)
    for side in (-1, 1):                                                          # Trauben an Stielen unter dem Band
        for i in range(3):
            grape_cluster(x + side * rnd.uniform(0.03, 0.05), y0 + 0.25 + (i + rnd.uniform(-0.2, 0.2)) * (length - 0.5) / 2,
                          0.345, g=0.03, seed=seed * 31 + i * 2 + (side > 0))


FIELD_STONE_A = mat("FeldsteinA", (0.64, 0.60, 0.52), rough=0.95)
FIELD_STONE_B = mat("FeldsteinB", (0.68, 0.64, 0.56), rough=0.95)
FIELD_STONE_C = mat("FeldsteinC", (0.60, 0.56, 0.49), rough=0.95)
FIELD_CAP = mat("FeldsteinDeck", (0.70, 0.66, 0.58), rough=0.95)


def field_wall(points, h=0.26, t=0.16, seed=0, ext_start=False, ext_end=False):
    """Niedrige, durchgehende Trockenmauer entlang der Punkte: drei Lagen kleiner, unregelmäßiger Bruchsteine (leicht
    verformte Quader, Läuferverband, eng geschichtet bis zum Boden) in drei eng beieinanderliegenden Kalkstein-Tönen,
    oben flache Decksteine. An inneren Knickpunkten laufen die Stücke bis zur Außenkante, damit die Ecken geschlossen
    sind; ext_start / ext_end tun das auch an den Enden."""
    rnd = random.Random(seed)
    mats = (FIELD_STONE_A, FIELD_STONE_B, FIELD_STONE_C)
    layers = 3
    lh = (h - 0.035) / layers
    n_seg = len(points) - 1
    for si in range(n_seg):
        (xa, ya), (xb, yb) = points[si], points[si + 1]
        length = math.hypot(xb - xa, yb - ya)
        ux, uy = (xb - xa) / length, (yb - ya) / length
        yaw = math.degrees(math.atan2(uy, ux))
        s0 = -t / 2 if (si > 0 or ext_start) else 0.0
        s1 = length + t / 2 if (si < n_seg - 1 or ext_end) else length

        def put(w, s, hh, z, d, material, tilt=2.0):
            c = s + w / 2
            off = rnd.uniform(-0.01, 0.01)
            box((w, d, hh), (xa + ux * c - uy * off, ya + uy * c + ux * off, z + hh / 2),
                rot=(rnd.uniform(-tilt, tilt), rnd.uniform(-tilt, tilt), yaw + rnd.uniform(-3, 3)), material=material)
        for j in range(layers):
            s = s0
            while s < s1 - 0.01:
                w = rnd.uniform(0.17, 0.26)
                if j % 2 and s == s0:
                    w = rnd.uniform(0.08, 0.12)
                if s1 - (s + w) < 0.08:
                    w = s1 - s
                put(w - 0.004, s, lh * rnd.uniform(0.96, 1.06), j * lh, t * rnd.uniform(0.88, 1.0), mats[rnd.randrange(3)])
                s += w
        s = s0
        while s < s1 - 0.01:                                                       # flache Decksteine
            w = rnd.uniform(0.25, 0.35)
            if s1 - (s + w) < 0.10:
                w = s1 - s
            put(w - 0.006, s, 0.035, h - 0.035, t * 1.12, FIELD_CAP, tilt=1.0)
            s += w


def field_pillar(x, y, h=0.42, w=0.20, yaw=0.0, seed=0):
    """Gemauerter Torpfosten: fünf Lagen verformter Quader mit leicht verdrehten Fugen und flachem Deckstein."""
    rnd = random.Random(seed)
    mats = (FIELD_STONE_A, FIELD_STONE_B, FIELD_STONE_C)
    layers = 5
    lh = (h - 0.04) / layers
    for j in range(layers):
        ww = w * rnd.uniform(0.97, 1.0)
        box((ww, ww * rnd.uniform(0.97, 1.0), lh * 1.02), (x + rnd.uniform(-0.004, 0.004), y + rnd.uniform(-0.004, 0.004), j * lh + lh / 2),
            rot=(rnd.uniform(-1, 1), rnd.uniform(-1, 1), yaw + rnd.uniform(-1.5, 1.5)),
            material=mats[(j + rnd.randrange(2)) % 3])
    box((w * 1.28, w * 1.28, 0.04), (x, y, h - 0.02), rot=(0, 0, yaw + 2), material=FIELD_CAP)


# ================================================================ Gewerbe Paket 1: Laden, Taberna, Werkstatt, Kontor

BREAD = mat("Brot", (0.72, 0.46, 0.22))
BREAD_DARK = mat("BrotDunkel", (0.52, 0.32, 0.15))
APPLE = mat("Apfel", (0.76, 0.20, 0.15))
APPLE_GREEN = mat("ApfelGruen", (0.62, 0.70, 0.26))
CHEESE = mat("Kaese", (0.93, 0.80, 0.42))
WINE_DARK = mat("WeinDunkel", (0.34, 0.06, 0.10), rough=0.3)
INK = mat("Tinte", (0.10, 0.09, 0.09), rough=0.5)


def basket_goods(x, y, z0=0.0, r=0.085, kind="grain", seed=0):
    """Weidenkorb mit Ware: 'grain' (Getreide), 'apples', 'olives' oder 'bread' (gehäuft)."""
    rnd = random.Random(seed)
    h = r * 1.0
    cone(r * 0.78, r, h, (x, y, z0 + h / 2), WICKER, verts=8)
    for k in (0.55,):
        cyl(r * (0.78 + 0.22 * k) + 0.003, 0.011, (x, y, z0 + h * k), material=WICKER_DARK, verts=8)
    cyl(r * 1.02, 0.013, (x, y, z0 + h), material=WICKER_DARK, verts=8)
    base = {"grain": GRAIN, "apples": APPLE_GREEN, "olives": OLIVE_FRUIT, "bread": BREAD}[kind]
    sphere(r * 0.94, (x, y, z0 + h), (1.0, 1.0, 0.42), base, segs=8, rings=3)
    if kind == "grain":
        return
    n = 7 if kind != "bread" else 4
    for k in range(n):
        a = k * 2.4 + rnd.uniform(-0.3, 0.3)
        d = r * 0.62 * math.sqrt((k + 0.5) / n)
        zt = z0 + h + r * 0.94 * 0.42 * math.sqrt(max(0.0, 1 - (d / (r * 0.94)) ** 2))
        px, py = x + math.cos(a) * d, y + math.sin(a) * d
        if kind == "apples":
            sphere(0.024, (px, py, zt), material=APPLE if k % 3 else APPLE_GREEN, segs=5, rings=3)
        elif kind == "olives":
            sphere(0.014, (px, py, zt), (1.0, 0.8, 1.15), OLIVE_FRUIT if k % 3 else OLIVE_GREEN, segs=5, rings=3)
        else:
            sphere(0.034, (px, py, zt + 0.004), (1.0, 0.8, 0.55), BREAD, segs=7, rings=4, rot=(0, 0, rnd.uniform(0, 180)))


def loaf(x, y, z0=0.0, yaw=0.0, r=0.036):
    """Rundes Brot mit eingeschnittenem Kreuz (Panis quadratus)."""
    sphere(r, (x, y, z0 + r * 0.38), (1.0, 1.0, 0.55), BREAD, segs=8, rings=4)
    for a in (0, 90):
        box((r * 1.7, 0.005, 0.004), (x, y, z0 + r * 0.38 + r * 0.55 * 0.92), rot=(0, 0, yaw + a), material=BREAD_DARK)


def cheese_wheel(x, y, z0=0.0, r=0.04, wedge=False):
    cyl(r, 0.032, (x, y, z0 + 0.016), material=CHEESE, verts=10)
    if wedge:
        box((r * 0.9, r * 0.8, 0.034), (x + r * 0.5, y, z0 + 0.017), rot=(0, 0, 25), material=DARK)


def stool(x, y, h=0.17, r=0.06):
    """Dreibeiniger Holzhocker."""
    cyl(r, 0.024, (x, y, h - 0.012), material=WOOD, verts=10)
    for k in range(3):
        a = math.radians(k * 120 + 30)
        rod((x + math.cos(a) * r * 1.0, y + math.sin(a) * r * 1.0, 0.0),
            (x + math.cos(a) * r * 0.55, y + math.sin(a) * r * 0.55, h - 0.02), 0.011, WOOD_DARK, verts=5)


def trestle_table(x, y, yaw=0.0, w=0.40, d=0.20, h=0.30, cloth=None):
    """Tisch auf zwei Böcken, auf Wunsch mit Tuch (Farbe) und herabhängender Kante vorn."""
    at = placed((x, y), yaw)
    for su in (-1, 1):
        for sv in (-1, 1):
            p0 = at((su * (w / 2 - 0.03), sv * (d / 2 - 0.025), 0.0))
            p1 = at((su * (w / 2 - 0.045), sv * (d / 2 - 0.045), 0.0))
            rod((p0[0], p0[1], 0.0), (p1[0], p1[1], h - 0.02), 0.010, WOOD_DARK, verts=5)
    box((w, d, 0.025), (x, y, h - 0.012), rot=(0, 0, yaw), material=WOOD)
    if cloth is not None:
        box((w + 0.01, d + 0.01, 0.006), (x, y, h + 0.003), rot=(0, 0, yaw), material=cloth)
        p = at((0.0, -d / 2 - 0.004, 0.0))
        box((w + 0.01, 0.006, 0.05), (p[0], p[1], h - 0.022), rot=(0, 0, yaw), material=cloth)
    return h


def sign_board(face, plane, u, z, w=0.20, h=0.12, arm=0.17, board=WOOD, mark=None, mark_mat=TERRACOTTA):
    """Aushängeschild: Eisenausleger rechtwinklig aus der Wand, am Ende eine Querstange, daran hängt ein Brett parallel
    zur Wand (zur Straße lesbar) mit einem aufgemalten Zeichen (mark = 'disc', 'bar' oder None)."""
    at, size, (ux, uy), (nx, ny) = face_frame(face, plane)
    box(size(0.014, arm, 0.014), at(u, arm / 2, z + h + 0.03), material=IRON)
    box(size(0.020, 0.020, 0.040), at(u, 0.012, z + h + 0.02), material=IRON)               # Wandplatte
    box(size(w + 0.03, 0.012, 0.012), at(u, arm, z + h + 0.03), material=IRON)               # Querstange
    for s in (-1, 1):
        box(size(0.004, 0.004, 0.032), at(u + s * w * 0.42, arm, z + h + 0.014), material=IRON)
    box(size(w, 0.016, h), at(u, arm, z + h / 2), material=board)
    box(size(w + 0.012, 0.012, 0.012), at(u, arm, z + h + 0.002), material=WOOD_DARK)
    box(size(w + 0.012, 0.012, 0.012), at(u, arm, z - 0.002), material=WOOD_DARK)
    if mark == "disc":
        c = at(u, arm + 0.009, z + h / 2)
        cyl(h * 0.30, 0.004, c, rot=(90, 0, 0) if ny != 0 else (0, 90, 0), material=mark_mat, verts=10)
    elif mark == "bar":
        box(size(w * 0.6, 0.004, h * 0.22), at(u, arm + 0.009, z + h / 2), material=mark_mat)


def striped_canopy(x0, x1, y0, y1, z_back, z_front, a, b, stripe=0.10, thickness=0.010):
    """Freistehendes Sonnendach aus gestreiftem Tuch: fällt von z_back bei y1 nach z_front bei y0 (Vorderkante -y), Streifen
    laufen in y-Richtung; vorn hängt ein Volant."""
    width = x1 - x0
    n = max(3, round(width / stripe))
    sw = width / n
    L = math.hypot(y1 - y0, z_back - z_front)
    tilt = math.degrees(math.atan2(z_back - z_front, y1 - y0))
    for k in range(n):
        m = a if k % 2 == 0 else b
        cx = x0 + (k + 0.5) * sw
        box((sw, L, thickness), (cx, (y0 + y1) / 2, (z_back + z_front) / 2), rot=(tilt, 0, 0), material=m)
        box((sw, 0.008, 0.045), (cx, y0 - 0.002, z_front - 0.020), material=m)


def counter(x0, x1, y0, y1, h=0.30, body_mat=LIME_DARK, top_mat=LIME):
    """Gemauerte Theke (Quader mit überstehender Platte)."""
    box((x1 - x0, y1 - y0, h - 0.03), ((x0 + x1) / 2, (y0 + y1) / 2, (h - 0.03) / 2), material=body_mat)
    box((x1 - x0 + 0.03, y1 - y0 + 0.03, 0.03), ((x0 + x1) / 2, (y0 + y1) / 2, h - 0.015), material=top_mat)
    return h


def thermopolium(x0, x1, y0, y1, h=0.34, pots=3, open_mask=(1, 0, 1)):
    """Thermopolium: gemauerte Theke, in die Platte sind Töpfe (Dolia) eingelassen, einige offen mit Speise, andere mit
    Holzdeckel; die Front hat eine rot gemalte Zone."""
    counter(x0, x1, y0, y1, h)
    box((x1 - x0 + 0.002, 0.006, 0.12), ((x0 + x1) / 2, y0 - 0.002, h * 0.50), material=SOCLE_RED)
    w = x1 - x0
    for k in range(pots):
        px = x0 + (k + 0.5) * w / pots
        py = (y0 + y1) / 2
        cyl(0.060, 0.030, (px, py, h + 0.015), material=TERRACOTTA, verts=12)
        if open_mask[k % len(open_mask)]:
            cyl(0.046, 0.004, (px, py, h + 0.031), material=PASTE if k % 2 == 0 else GRAIN, verts=12)
        else:
            cyl(0.050, 0.014, (px, py, h + 0.036), material=WOOD, verts=12)
            cyl(0.014, 0.014, (px, py, h + 0.050), material=WOOD_DARK, verts=6)
    return h


def cloth_bale(x, y, z0=0.0, yaw=0.0, colors=(CLOTH_RED, CLOTH), w=0.16, d=0.09, h=0.07):
    """Ballen gefalteten Stoffs (Streifen übereinander)."""
    layers = 4
    for k in range(layers):
        box((w, d, h / layers * 0.92), (x, y, z0 + (k + 0.5) * h / layers), rot=(0, 0, yaw + (k - 1.5) * 2), material=colors[k % 2])


def light_pergola(x0, x1, y0, y1, h=0.62, vine_post=0, step=0.20):
    """Weinlaube wie pergola(), aber mit gröberen Blattbüscheln (wenig Dreiecke für Zonengebäude)."""
    posts = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    for x, y in posts:
        box((0.045, 0.045, h), (x, y, h / 2), material=WOOD_DARK)
    for y in (y0, y1):
        box((x1 - x0 + 0.10, 0.04, 0.04), ((x0 + x1) / 2, y, h + 0.02), material=WOOD_DARK)
    n = max(2, int((x1 - x0) / 0.24))
    for k in range(n + 1):
        box((0.025, y1 - y0 + 0.12, 0.025), (x0 + k * (x1 - x0) / n, (y0 + y1) / 2, h + 0.055), material=WOOD)
    rnd = random.Random(int((x0 + y0) * 100) + 5)
    nx, ny = max(2, round((x1 - x0) / step)), max(2, round((y1 - y0) / step))
    for i in range(nx + 1):
        for j in range(ny + 1):
            edge = i in (0, nx) or j in (0, ny)
            if edge and rnd.random() < 0.3:
                continue
            x = x0 + i * (x1 - x0) / nx + rnd.uniform(-0.03, 0.03)
            y = y0 + j * (y1 - y0) / ny + rnd.uniform(-0.03, 0.03)
            z = h + 0.09 + rnd.uniform(-0.02, 0.03) - (0.04 if edge else 0.0)
            sphere(0.15, (x, y, z), (1.0, 1.0, 0.34), LEAF if rnd.random() > 0.4 else LEAF_DARK, segs=6, rings=3,
                   rot=(0, 0, rnd.uniform(0, 60)))
    vx, vy = posts[vine_post]
    cyl(0.02, h, (vx + 0.03, vy + 0.03, h / 2), material=WOOD_DARK, verts=6)
    for k in range(3):
        sphere(0.05, (vx + 0.03 * math.cos(k * 2.1), vy + 0.03 * math.sin(k * 2.1), 0.15 + k * h / 3 + 0.12),
               (0.8, 0.8, 1.0), LEAF_DARK, segs=5, rings=3)


# ================================================================ Gewerbe Paket 2: Werkstatthof, Speicherhof, Handelshaus

CLOTH_GREEN = mat("TuchGruen", (0.36, 0.52, 0.34))
CLOTH_BLUE = mat("TuchBlau", (0.30, 0.42, 0.62))


def flush_wall(x0, x1, y0, y1, h, plaster=PLASTER_WHITE, cap=True, plinth=True):
    """Mauer als Rechteck mit genau vorgegebenen Außenflächen (x0, x1, y0, y1): Putz, Bruchsteinsockel wie bei body(),
    Ziegelabdeckung. Liegt die Außenfläche in der Ebene einer Hauswand und endet 1 bis 2 cm im Hauskörper, schließt
    sie ohne Vor- oder Rücksprung an."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    box((x1 - x0, y1 - y0, h), (cx, cy, h / 2), material=plaster)
    if plinth:
        box((x1 - x0 + 0.04, y1 - y0 + 0.04, 0.09), (cx, cy, 0.045), material=STONE)
    if cap:
        along_x = (x1 - x0) >= (y1 - y0)
        L = (x1 - x0) if along_x else (y1 - y0)
        t = (y1 - y0) if along_x else (x1 - x0)
        if along_x:
            box((L, t + 0.03, 0.035), (cx, cy, h + 0.0175), material=TILE_DARK)
            cyl(0.026, L, (cx, cy, h + 0.043), rot=(0, 90, 0), material=TILE, verts=8)
        else:
            box((t + 0.03, L, 0.035), (cx, cy, h + 0.0175), material=TILE_DARK)
            cyl(0.026, L, (cx, cy, h + 0.043), rot=(90, 0, 0), material=TILE, verts=8)


def arch_opening(face, plane, u, w, h, z0=0.0, voussoir=True):
    """Dunkle Rundbogenöffnung (Breite w, gerade Höhe h bis zum Kämpfer, darüber ein Halbkreis) in einer Wand nach
    -y oder +y, mit Gewänden aus Kalkstein und Bogensteinen aus Ziegel mit hellem Schlussstein."""
    nx, ny = FACES[face]
    hw = w / 2
    zc = z0 + h
    box((w, 0.012, h), (u, plane + ny * 0.004, z0 + h / 2), material=DARK)
    cyl(hw, 0.012, (u, plane + ny * 0.004, zc), rot=(90, 0, 0), material=DARK, verts=16)
    if voussoir:
        r = hw + 0.04
        for a in range(0, 181, 15):
            t = math.radians(a)
            big = a == 90
            box((0.10 if big else 0.08, 0.016, 0.09 if big else 0.06),
                (u + r * math.cos(t), plane + ny * 0.008, zc + r * math.sin(t)), rot=(0, -a, 0),
                material=LIME if big else TILE_DARK)
    for s in (-1, 1):
        box((0.08, 0.02, h), (u + s * (hw + 0.04), plane + ny * 0.01, z0 + h / 2), material=LIME_DARK)
        box((0.13, 0.04, 0.05), (u + s * (hw + 0.04), plane + ny * 0.02, zc), material=LIME_DARK)


def fountain_basin(x, y, r=0.52, h=0.22):
    """Brunnen auf Marktplätzen und in Höfen: achteckiges Steinbecken mit Wasser, Mittelsäule, Schale und vier
    Wasserstrahlen, die in das Becken fallen."""
    cyl(r, h, (x, y, h / 2), material=LIME, verts=8)
    cyl(r + 0.03, 0.04, (x, y, h + 0.02), material=LIME_DARK, verts=8)
    cyl(r - 0.07, 0.012, (x, y, h + 0.012), material=WATER, verts=8)
    cyl(0.07, 0.45, (x, y, h + 0.22), material=LIME, verts=8)
    cone(0.20, 0.07, 0.07, (x, y, h + 0.42), LIME_DARK, verts=10)
    cyl(0.185, 0.04, (x, y, h + 0.49), material=LIME, verts=10)
    cyl(0.155, 0.012, (x, y, h + 0.514), material=WATER, verts=10)
    sphere(0.045, (x, y, h + 0.55), (1.0, 1.0, 1.3), WATER, segs=8, rings=5)
    for k in range(4):
        a = math.radians(45 + 90 * k)
        water_jet((x + math.cos(a) * 0.17, y + math.sin(a) * 0.17, h + 0.50),
                  (x + math.cos(a) * 0.40, y + math.sin(a) * 0.40, h + 0.02), thick=0.016, segs=6)


def sack_stack(x, y, yaw=0.0, per=(3, 2, 1), r=0.05, length=0.22, z0=0.0):
    """Stapel liegender Säcke (Lagen von unten nach oben mit je per[k] Säcken nebeneinander), die Säcke liegen quer
    zur Stapelachse; jede Lage ruht auf der darunter."""
    at = placed((x, y), yaw)
    for k, n in enumerate(per):
        for i in range(n):
            u = (i - (n - 1) / 2) * 0.092
            p = at((u, 0.0, 0.0))
            lying_sack(p[0], p[1], z0=z0 + k * 0.074, yaw=yaw + 90 + (3 if (i + k) % 2 else -3), r=r, length=length)


def amphora_row(p0, p1, n):
    """Amphoren stehend in einer Reihe von p0 nach p1, hinten durch eine Holzlatte in 0,17 m Höhe gehalten."""
    for k in range(n):
        t = k / (n - 1) if n > 1 else 0.5
        amphora(p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t)
    yaw = math.degrees(math.atan2(p1[1] - p0[1], p1[0] - p0[0]))
    L = math.hypot(p1[0] - p0[0], p1[1] - p0[1]) + 0.12
    off = Vector((-math.sin(math.radians(yaw)), math.cos(math.radians(yaw)))) * 0.05
    box((L, 0.014, 0.02), ((p0[0] + p1[0]) / 2 + off.x, (p0[1] + p1[1]) / 2 + off.y, 0.17), rot=(0, 0, yaw),
        material=WOOD_DARK)
    for end in (p0, p1):
        box((0.02, 0.02, 0.17), (end[0] + off.x, end[1] + off.y, 0.085), material=WOOD_DARK)


def market_stall(x, y, w, kind, canopy, depth=0.46, yaw=0.0):
    """Verkaufsstand (lokal: u quer, v Tiefe, v = 0 vorn): hinten ein niedriger Tisch mit Vorrat unter einem schmalen,
    gestreiften Tuchdach auf vier Pfosten, davor ein offener Auslagetisch mit Ware, der nicht überdacht ist.
    kind: 'grain', 'oil', 'pots' oder 'wine'."""
    at = placed((x, y), yaw)
    hw = w / 2
    zb, zf = 0.62, 0.50

    def P(u, v):
        q = at((u, v, 0.0))
        return q[0], q[1]
    for su in (-1, 1):
        for v, hh in ((0.03, zf), (depth - 0.03, zb)):
            c = P(su * (hw - 0.02), v)
            box((0.028, 0.028, hh), (c[0], c[1], hh / 2), rot=(0, 0, yaw), material=WOOD_DARK)
    a, b = canopy
    n = max(4, round(w / 0.12))
    sw = w / n
    tilt = math.degrees(math.atan2(zb - zf, depth))
    L = math.hypot(depth, zb - zf)
    for k in range(n):
        m = a if k % 2 == 0 else b
        u = -hw + (k + 0.5) * sw
        cx, cy = P(u, depth / 2)
        box((sw, L, 0.010), (cx, cy, (zb + zf) / 2 + 0.005), rot=(tilt, 0, yaw), material=m)
        fx, fy = P(u, -0.002)
        box((sw, 0.008, 0.045), (fx, fy, zf - 0.015), rot=(0, 0, yaw), material=m)
    # Vorratstisch unter dem Dach
    cx, cy = P(0.0, depth - 0.12)
    box((w - 0.08, 0.16, 0.26), (cx, cy, 0.13), rot=(0, 0, yaw), material=LIME_DARK)
    box((w - 0.04, 0.18, 0.025), (cx, cy, 0.2725), rot=(0, 0, yaw), material=LIME)
    # offener Auslagetisch davor
    tv = -0.20
    tx, ty = P(0.0, tv)
    th = 0.26
    trestle_table(tx, ty, yaw, w=w - 0.10, d=0.30, h=th, cloth=CLOTH_OCHRE if kind == "grain" else CLOTH)
    top = 0.285
    if kind == "grain":
        for i in range(3):
            p = P(-w * 0.28 + i * w * 0.28, tv)
            basket_goods(p[0], p[1], th + 0.006, 0.062, "grain", seed=i + 3)
        for uu in (-0.30, 0.30):
            p = P(uu * w, depth - 0.12)
            sack(p[0], p[1], top, 0.055)
    elif kind == "oil":
        for i in range(3):
            p = P(-w * 0.28 + i * w * 0.28, tv)
            jug(p[0], p[1], th + 0.006, 0.032, TERRACOTTA_LIGHT)
        for uu in (-0.30, 0.0, 0.30):
            p = P(uu * w, depth - 0.12)
            amphora(p[0], p[1], top)
    elif kind == "pots":
        for i in range(5):
            p = P(-w * 0.34 + i * w * 0.17, tv + (0.03 if i % 2 else -0.03))
            if i % 2:
                bowl(p[0], p[1], th + 0.006, 0.050, TERRACOTTA_LIGHT)
            else:
                pot(p[0], p[1], th + 0.006, 0.038)
        for uu in (-0.28, 0.28):
            p = P(uu * w, depth - 0.12)
            pot(p[0], p[1], top, 0.05)
    else:
        for i in range(3):
            p = P(-w * 0.28 + i * w * 0.28, tv)
            jug(p[0], p[1], th + 0.006, 0.030, TERRACOTTA)
        for uu in (-0.26, 0.26):
            p = P(uu * w, depth - 0.12)
            barrel(p[0], p[1], top, r=0.05)


def check_bounds(hx, hy, label=""):
    """Meldet Teile der aktuellen Variante, die über +-hx bzw. +-hy hinausragen (vor variant() aufrufen)."""
    for o in PARTS:
        pts = [o.matrix_world @ Vector(c) for c in o.bound_box]
        xs, ys = [p.x for p in pts], [p.y for p in pts]
        if max(xs) > hx or min(xs) < -hx or max(ys) > hy or min(ys) < -hy:
            print(f"GRENZE {label}: {o.name} {o.data.name} x {min(xs):.2f}..{max(xs):.2f} y {min(ys):.2f}..{max(ys):.2f} "
                  f"mat {o.data.materials[0].name if o.data.materials else '-'}")
