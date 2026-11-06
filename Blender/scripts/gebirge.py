"""
Natur für Gebirge und Wald (Berge Teil B, überarbeitet 2026-11-06): Mittelmeer-Bäume, Macchia und Kalkfelsen.
MountainVisuals verteilt sie im Gebirge nach Höhe, Hangneigung und Mulden, DepositVisuals im Wald der Karte.
Low-Poly mit unregelmäßigen Laubballen statt Scheiben und Etagen; Ursprung am Boden, der Stamm beginnt 0,25 m unter dem
Boden, damit er auch am Hang bis in die Erde reicht. Ein Feld ist 2 m breit.
  Pinie_A-C         Schirmkiefer: schlanker, krummer, kahler Stamm, oben auffächernde Äste, breite flache Schirmkrone
                    aus vielen Ballen (2,6 bis 3,0 m)
  Zypresse_A-B      Mittelmeer-Zypresse: schmale Flamme aus übereinanderliegenden Ballen, ohne Etagen (2,8 bis 3,5 m)
  Steineiche_A-B    knorriger Stamm, runde dichte dunkle Krone (2,3 bis 2,5 m)
  Aleppokiefer_A-B  schiefer Stamm, lichte unregelmäßige Krone in hellem Grün (2,6 bis 3 m)
  Macchia_A-C       Buschgruppen in Oliv, Graugrün und Dunkelgrün, teils mit gelbem Ginster (0,5 bis 0,8 m)
  Fels_A-D          grauer Kalkstein in Schichten mit Abbruchkanten (0,6 bis 1,4 m), unten abgeschnitten
Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/gebirge.py -- <blend> <fbx> [<vorschau.png>]
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

R = random.Random(2026)

# Pinie: bläulich-dunkles Grün, oben heller
PINE_D = mat("PinieDunkel", (0.19, 0.31, 0.20))
PINE = mat("Pinie", (0.26, 0.40, 0.23))
PINE_L = mat("PinieHell", (0.35, 0.49, 0.27))
BARK = mat("PinienRinde", (0.46, 0.31, 0.21))
BARK_D = mat("EichenRinde", (0.33, 0.25, 0.19))
# Steineiche: sehr dunkel, leicht grau
HOLM_D = mat("EicheDunkel", (0.14, 0.25, 0.17))
HOLM = mat("Eiche", (0.20, 0.33, 0.20))
HOLM_L = mat("EicheHell", (0.28, 0.41, 0.24))
# Zypresse: Blaugrün
CYP_D = mat("ZypresseDunkel", (0.10, 0.23, 0.16))
CYP = mat("ZypresseMitte", (0.14, 0.29, 0.20))
CYP_L = mat("ZypresseHell", (0.19, 0.35, 0.24))
# Aleppokiefer: hell, gelblich
ALEP_D = mat("AleppoDunkel", (0.33, 0.45, 0.24))
ALEP = mat("Aleppo", (0.42, 0.54, 0.28))
ALEP_L = mat("AleppoHell", (0.52, 0.62, 0.33))
# Macchia
MAC_OLIVE = mat("MacchiaOliv", (0.42, 0.48, 0.26))
MAC_GREY = mat("MacchiaGrau", (0.50, 0.56, 0.45))
MAC_DARK = mat("MacchiaDunkel", (0.22, 0.34, 0.20))
GINSTER = mat("Ginster", (0.95, 0.80, 0.16))
# Kalkstein in Schichten
LS_A = mat("KalkfelsA", (0.52, 0.52, 0.50), rough=0.95)
LS_B = mat("KalkfelsB", (0.45, 0.45, 0.43), rough=0.95)
LS_C = mat("KalkfelsC", (0.58, 0.57, 0.54), rough=0.95)
LS_D = mat("KalkfelsD", (0.38, 0.38, 0.37), rough=0.95)


# ---------------------------------------------------------------- Bausteine


def mesh_obj(name, verts, faces, material):
    """Objekt aus Eckpunkten und Flächen (Normalen nach außen) als Teil der aktuellen Variante."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    vs = [bm.verts.new(v) for v in verts]
    for f in faces:
        try:
            bm.faces.new([vs[i] for i in f])
        except ValueError:
            pass
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    return keep(obj, material)


def blob(x, y, z, rx, ry, rz, material, segs=6, rings=3, jit=0.2, yaw=None, lean=(0.0, 0.0)):
    """Unregelmäßiger Laubballen: Ellipsoid mit versetzten Ringen und zufälligen Radien (flach geschattet)."""
    if yaw is None:
        yaw = R.uniform(0, 2 * math.pi)
    verts = [(0.0, 0.0, rz * (1 + R.uniform(-jit, jit)))]
    ring_index = []
    for k in range(1, rings):
        lat = math.pi * k / rings
        idx = []
        for i in range(segs):
            phi = yaw + 2 * math.pi * (i + 0.5 * (k % 2)) / segs
            f = 1.0 + R.uniform(-jit, jit)
            verts.append((rx * math.sin(lat) * math.cos(phi) * f, ry * math.sin(lat) * math.sin(phi) * f,
                          rz * math.cos(lat) * (1 + R.uniform(-jit, jit))))
            idx.append(len(verts) - 1)
        ring_index.append(idx)
    verts.append((0.0, 0.0, -rz * (1 + R.uniform(-jit, jit))))
    bottom = len(verts) - 1
    faces = []
    first, last = ring_index[0], ring_index[-1]
    for i in range(segs):
        faces.append((0, first[(i + 1) % segs], first[i]))
        faces.append((bottom, last[i], last[(i + 1) % segs]))
    for k in range(len(ring_index) - 1):
        a, b = ring_index[k], ring_index[k + 1]
        if (k + 1) % 2 == 1:   # Ring a mit Versatz 0.5, Ring b ohne
            for i in range(segs):
                faces.append((a[i], b[(i + 1) % segs], b[i]))
                faces.append((a[(i - 1) % segs], b[i], a[i]))
        else:                  # Ring a ohne Versatz, Ring b mit
            for i in range(segs):
                faces.append((a[i], a[(i + 1) % segs], b[i]))
                faces.append((b[i], a[(i + 1) % segs], b[(i + 1) % segs]))
    placed = [(x + v[0] + lean[0] * v[2], y + v[1] + lean[1] * v[2], z + v[2]) for v in verts]
    return mesh_obj("Ballen", placed, faces, material)


def tube(path, radii, material, sides=6, cap_top=True, gnarl=0.0):
    """Gebogene Röhre durch die Punkte mit wechselnden Radien (Stamm, Äste); unten und oben geschlossen."""
    pts = [Vector(p) for p in path]
    verts, faces = [], []
    start_phase = R.uniform(0, 2 * math.pi)
    for n, p in enumerate(pts):
        if n == 0:
            d = pts[1] - pts[0]
        elif n == len(pts) - 1:
            d = pts[-1] - pts[-2]
        else:
            d = pts[n + 1] - pts[n - 1]
        d.normalize()
        ref = Vector((1, 0, 0)) if abs(d.x) < 0.9 else Vector((0, 1, 0))
        u = d.cross(ref).normalized()
        v = d.cross(u).normalized()
        for i in range(sides):
            a = start_phase + 2 * math.pi * i / sides
            r = radii[n] * (1 + R.uniform(-gnarl, gnarl))
            q = p + (u * math.cos(a) + v * math.sin(a)) * r
            verts.append(tuple(q))
    for n in range(len(pts) - 1):
        for i in range(sides):
            a, b = n * sides + i, n * sides + (i + 1) % sides
            faces.append((a, b, b + sides, a + sides))
    faces.append(tuple(range(sides)))
    if cap_top:
        faces.append(tuple(range((len(pts) - 1) * sides, len(pts) * sides)))
    return mesh_obj("Rohr", verts, faces, material)


def trunk_path(h, lean, wiggle, seed, n=5, z0=-0.25):
    """Stammlinie von z0 bis h mit S-Schwung und Neigung (lean = Versatz der Spitze in x, y)."""
    pts = []
    for i in range(n + 1):
        t = i / n
        pts.append((lean[0] * t * t + wiggle * math.sin(t * 4.2 + seed),
                    lean[1] * t * t + wiggle * math.cos(t * 3.4 + seed * 1.7),
                    z0 + (h - z0) * t))
    return pts


# ---------------------------------------------------------------- Pinie


def pinie(trunk_h, crown_r, lean, seed, name):
    pts = trunk_path(trunk_h, lean, 0.07, seed)
    tube(pts, [0.16, 0.13, 0.11, 0.095, 0.085, 0.075], BARK, gnarl=0.06)
    top = Vector(pts[-1])
    cx, cy, cz = top.x + lean[0] * 0.25, top.y + lean[1] * 0.25, top.z + 0.22
    # vier bis fünf Äste fächern sich vom oberen Stamm zur Krone auf
    nb = 5 if crown_r > 1.2 else 4
    base_a = R.uniform(0, 2 * math.pi)
    for k in range(nb):
        a = base_a + 2 * math.pi * k / nb + R.uniform(-0.3, 0.3)
        s = top - Vector((0, 0, R.uniform(0.05, 0.4)))
        s = Vector((pts[-2][0] + (top.x - pts[-2][0]) * 0.5, pts[-2][1] + (top.y - pts[-2][1]) * 0.5, s.z))
        rr = crown_r * 0.52
        e = Vector((cx + rr * math.cos(a), cy + rr * math.sin(a), cz - 0.08))
        mid = (s + e) / 2 + Vector((0, 0, 0.12))
        tube([tuple(s), tuple(mid), tuple(e)], [0.05, 0.04, 0.03], BARK, sides=5, gnarl=0.05)
    # Schirmkrone: Ring großer Ballen außen, dichterer Kern, kleine Kuppe oben
    n_out = 7
    off = R.uniform(0, 2 * math.pi)
    for i in range(n_out):
        a = off + 2 * math.pi * i / n_out + R.uniform(-0.25, 0.25)
        rr = crown_r * R.uniform(0.58, 0.82)
        rad = crown_r * R.uniform(0.30, 0.38)
        z = cz + R.uniform(-0.04, 0.07) - 0.10 * (rr / crown_r)
        blob(cx + rr * math.cos(a), cy + rr * math.sin(a), z, rad, rad, rad * 0.5,
             R.choice([PINE, PINE_D, PINE, PINE_L]))
    for i in range(4):
        a = off + 2 * math.pi * i / 4 + 0.5
        rr = crown_r * R.uniform(0.18, 0.32)
        rad = crown_r * R.uniform(0.34, 0.40)
        blob(cx + rr * math.cos(a), cy + rr * math.sin(a), cz + 0.14, rad, rad, rad * 0.52, R.choice([PINE, PINE_L]))
    for i in range(2):
        a = off + math.pi * i + 1.0
        rr = crown_r * 0.12
        rad = crown_r * 0.24
        blob(cx + rr * math.cos(a), cy + rr * math.sin(a), cz + 0.30, rad, rad, rad * 0.55, PINE_L, segs=5)
    variant(name)


# ---------------------------------------------------------------- Steineiche


def steineiche(trunk_h, crown_r, lean, seed, name):
    pts = trunk_path(trunk_h, lean, 0.06, seed, n=3)
    tube(pts, [0.19, 0.16, 0.13, 0.11], BARK_D, gnarl=0.1)
    top = Vector(pts[-1])
    cx, cy, cz = top.x, top.y, top.z + 0.55
    a0 = R.uniform(0, 2 * math.pi)
    for k in range(3):
        a = a0 + 2 * math.pi * k / 3 + R.uniform(-0.3, 0.3)
        e = Vector((cx + crown_r * 0.45 * math.cos(a), cy + crown_r * 0.45 * math.sin(a), cz - 0.1))
        mid = (top + e) / 2 + Vector((0, 0, 0.05))
        tube([tuple(top - Vector((0, 0, 0.08))), tuple(mid), tuple(e)], [0.085, 0.07, 0.05], BARK_D, sides=5, gnarl=0.08)
    blob(cx, cy, cz + 0.25, crown_r * 0.62, crown_r * 0.62, crown_r * 0.52, HOLM)
    for i in range(5):
        a = a0 + 2 * math.pi * i / 5 + R.uniform(-0.3, 0.3)
        rr = crown_r * R.uniform(0.55, 0.72)
        rad = crown_r * R.uniform(0.40, 0.50)
        blob(cx + rr * math.cos(a), cy + rr * math.sin(a), cz + R.uniform(-0.12, 0.12), rad, rad, rad * 0.82,
             R.choice([HOLM, HOLM_D, HOLM]))
    for i in range(3):
        a = a0 + 2 * math.pi * i / 3 + 1.0
        rr = crown_r * 0.3
        rad = crown_r * 0.36
        blob(cx + rr * math.cos(a), cy + rr * math.sin(a), cz + 0.62 + R.uniform(0, 0.1), rad, rad, rad * 0.8,
             R.choice([HOLM_L, HOLM]))
    variant(name)


# ---------------------------------------------------------------- Aleppokiefer


def aleppo(trunk_h, crown_r, lean, seed, name):
    pts = trunk_path(trunk_h, lean, 0.11, seed, n=5)
    tube(pts, [0.15, 0.12, 0.10, 0.085, 0.07, 0.06], BARK, gnarl=0.08)
    top = Vector(pts[-1])
    a0 = R.uniform(0, 2 * math.pi)
    for k in range(5):
        a = a0 + 2 * math.pi * k / 5 + R.uniform(-0.4, 0.4)
        s = Vector(pts[-1 - (k % 3)])
        rr = crown_r * R.uniform(0.45, 0.95)
        e = Vector((s.x + rr * math.cos(a), s.y + rr * math.sin(a), s.z + R.uniform(0.0, 0.5)))
        mid = (s + e) / 2 + Vector((0, 0, R.uniform(0.05, 0.2)))
        tube([tuple(s), tuple(mid), tuple(e)], [0.05, 0.04, 0.028], BARK, sides=5, gnarl=0.05)
        rad = crown_r * R.uniform(0.28, 0.40)
        blob(e.x, e.y, e.z + 0.12, rad, rad, rad * 0.62, R.choice([ALEP, ALEP_L, ALEP_D]))
        if k % 2 == 0:
            rad2 = rad * 0.7
            blob(e.x + rad * 0.6 * math.cos(a + 1.3), e.y + rad * 0.6 * math.sin(a + 1.3), e.z + 0.3, rad2, rad2, rad2 * 0.65,
                 ALEP_L)
    blob(top.x, top.y, top.z + 0.22, crown_r * 0.34, crown_r * 0.34, crown_r * 0.24, ALEP)
    variant(name)


# ---------------------------------------------------------------- Zypresse


def zypresse(widths, spacing, height_scale, name, sway=0.03):
    stub = tube([(0, 0, -0.25), (0, 0, 0.18), (0.0, 0.0, 0.5)], [0.1, 0.085, 0.07], BARK_D, sides=5)
    n = len(widths)
    for i, w in enumerate(widths):
        t = i / max(1, n - 1)
        z = 0.5 + i * spacing
        ox, oy = sway * math.sin(i * 1.7 + 0.3), sway * math.cos(i * 2.1)
        mat_ = CYP_L if i % 3 == 0 and i > 1 else (CYP_D if i % 3 == 1 else CYP)
        rz = max(w * 1.9, spacing * 0.9) * height_scale
        blob(ox, oy, z, w, w, rz, mat_, segs=6, rings=4, jit=0.13)
    variant(name)


# ---------------------------------------------------------------- Macchia


def macchia(spec, name, ginster=0):
    for (x, y, rx, rz, m) in spec:
        blob(x, y, rz * 0.45, rx, rx * R.uniform(0.85, 1.1), rz, m, segs=6, rings=3, jit=0.22)
    for _ in range(ginster):
        a = R.uniform(0, 2 * math.pi)
        rr = R.uniform(0.0, 0.3)
        blob(math.cos(a) * rr, math.sin(a) * rr, R.uniform(0.28, 0.5), 0.10, 0.10, 0.08, GINSTER, segs=5, rings=2, jit=0.2)
    variant(name)


# ---------------------------------------------------------------- Felsen


def chunk(x, y, z0, w, d, h, material, yaw=0.0, taper=0.85, jit=0.12, tilt=(0.0, 0.0), shift=(0.0, 0.0)):
    """Kantiger Gesteinsblock (verzerrter Quader, oben schmaler) als Schicht oder loser Brocken."""
    base = [(-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, d / 2), (-w / 2, d / 2)]
    verts = []
    for level, zz in ((0, 0.0), (1, h)):
        for (px, py) in base:
            s = 1.0 if level == 0 else taper
            jx = R.uniform(-jit, jit) * w
            jy = R.uniform(-jit, jit) * d
            jz = R.uniform(-jit, jit) * h * 0.6 if level == 1 else 0.0
            verts.append((px * s + jx + shift[0] * level, py * s + jy + shift[1] * level, zz + jz))
    c, s_ = math.cos(yaw), math.sin(yaw)
    out = []
    for (px, py, pz) in verts:
        px += tilt[0] * pz
        py += tilt[1] * pz
        out.append((x + px * c - py * s_, y + px * s_ + py * c, z0 + pz))
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return mesh_obj("Block", out, faces, material)


def layered(x, y, w, d, heights, mats, yaw, shrink=0.88, drift=0.05, tilt=(0.0, 0.0), z0=-0.2, taper=0.9):
    """Gestapelte Schichten, jede etwas kleiner und seitlich versetzt: sichtbare Gesteinsbänke und Absätze."""
    z = z0
    cx, cy = x, y
    for i, h in enumerate(heights):
        chunk(cx, cy, z, w, d, h + (0.2 if i == 0 else 0.0), mats[i % len(mats)], yaw + R.uniform(-0.18, 0.18), taper=taper,
              tilt=tilt)
        z += h + (0.2 if i == 0 else 0.0) - 0.01
        w *= shrink * R.uniform(0.94, 1.04)
        d *= shrink * R.uniform(0.94, 1.04)
        cx += R.uniform(-drift, drift)
        cy += R.uniform(-drift, drift)
    return z


# ---------------------------------------------------------------- Varianten
pinie(2.1, 1.25, (0.18, 0.06), 1.0, "Pinie_A")
pinie(1.75, 1.10, (-0.30, 0.14), 2.3, "Pinie_B")
pinie(2.35, 1.30, (0.08, -0.22), 3.7, "Pinie_C")

zypresse([0.19, 0.25, 0.28, 0.27, 0.23, 0.17, 0.10], 0.43, 1.0, "Zypresse_A")
zypresse([0.21, 0.27, 0.29, 0.25, 0.19, 0.10], 0.40, 0.95, "Zypresse_B", sway=0.04)

steineiche(0.85, 1.15, (0.10, 0.04), 1.1, "Steineiche_A")
steineiche(1.0, 1.05, (-0.12, 0.08), 2.9, "Steineiche_B")

aleppo(2.0, 1.15, (0.45, 0.10), 1.4, "Aleppokiefer_A")
aleppo(2.2, 1.0, (-0.35, -0.28), 3.1, "Aleppokiefer_B")

macchia([(0.0, 0.0, 0.36, 0.36, MAC_OLIVE), (0.34, 0.12, 0.26, 0.28, MAC_GREY), (-0.28, 0.2, 0.24, 0.26, MAC_DARK),
         (0.05, -0.30, 0.22, 0.24, MAC_OLIVE), (-0.1, 0.05, 0.28, 0.36, MAC_DARK)], "Macchia_A", ginster=4)
macchia([(0.0, 0.0, 0.42, 0.22, MAC_GREY), (0.36, -0.18, 0.24, 0.2, MAC_OLIVE), (-0.34, -0.22, 0.22, 0.2, MAC_OLIVE),
         (0.08, 0.34, 0.2, 0.18, MAC_DARK), (-0.3, 0.2, 0.18, 0.22, MAC_GREY)], "Macchia_B", ginster=0)
macchia([(0.0, 0.0, 0.3, 0.34, MAC_DARK), (0.3, 0.1, 0.24, 0.3, MAC_OLIVE), (-0.22, -0.2, 0.2, 0.24, MAC_GREY),
         (0.12, -0.28, 0.18, 0.2, MAC_OLIVE), (-0.3, 0.22, 0.17, 0.2, MAC_OLIVE), (0.28, -0.12, 0.22, 0.34, MAC_DARK)],
        "Macchia_C", ginster=6)

# Fels A: kräftiger Felskopf aus fünf Bänken mit losen Brocken
top = layered(0.0, 0.0, 1.15, 0.95, [0.26, 0.22, 0.28, 0.2, 0.16], [LS_A, LS_B, LS_C, LS_A, LS_D], 0.4, shrink=0.86)
chunk(0.85, 0.3, -0.15, 0.4, 0.34, 0.34, LS_B, yaw=0.8, taper=0.7)
chunk(-0.7, -0.45, -0.15, 0.3, 0.26, 0.26, LS_C, yaw=-0.5, taper=0.7)
chunk(0.2, 0.7, -0.15, 0.22, 0.2, 0.18, LS_A, yaw=0.2, taper=0.7)
variant("Fels_A")

# Fels B: flache, lange Felsbank mit Stufe
layered(0.0, 0.0, 1.9, 0.95, [0.2, 0.18, 0.14], [LS_B, LS_C, LS_A], 0.15, shrink=0.78, drift=0.1)
layered(0.35, -0.05, 0.8, 0.6, [0.12, 0.12], [LS_A, LS_D], -0.3, shrink=0.8, z0=0.15)
chunk(-1.1, 0.5, -0.15, 0.35, 0.3, 0.2, LS_C, yaw=0.6, taper=0.7)
chunk(0.95, -0.7, -0.15, 0.28, 0.22, 0.16, LS_D, yaw=0.1, taper=0.7)
variant("Fels_B")

# Fels C: hohe, leicht schiefe Felsnadel aus schmalen Bänken
layered(0.0, 0.0, 0.62, 0.55, [0.3, 0.26, 0.3, 0.26, 0.24, 0.2], [LS_C, LS_A, LS_B, LS_A, LS_C, LS_D], 0.2, shrink=0.9,
        drift=0.035, tilt=(0.07, -0.04), taper=0.93)
chunk(0.55, 0.2, -0.15, 0.36, 0.3, 0.3, LS_B, yaw=0.9, taper=0.7)
chunk(-0.45, 0.35, -0.15, 0.28, 0.26, 0.22, LS_A, yaw=-0.4, taper=0.7)
chunk(0.1, -0.5, -0.15, 0.24, 0.2, 0.16, LS_C, yaw=0.3, taper=0.7)
variant("Fels_C")

# Fels D: Geröll aus vier Brocken verschiedener Größe
chunk(0.0, 0.0, -0.2, 0.7, 0.6, 0.6, LS_A, yaw=0.3, taper=0.72)
chunk(0.65, 0.2, -0.2, 0.46, 0.4, 0.4, LS_B, yaw=-0.5, taper=0.7)
chunk(-0.5, 0.45, -0.2, 0.36, 0.32, 0.34, LS_C, yaw=0.9, taper=0.7)
chunk(0.2, -0.55, -0.2, 0.3, 0.26, 0.24, LS_D, yaw=0.1, taper=0.7)
chunk(-0.55, -0.35, -0.2, 0.22, 0.2, 0.18, LS_B, yaw=0.5, taper=0.7)
variant("Fels_D")

export(spacing=4.0)
