"""
Deko für das Gebirge (Berge Teil B). MountainVisuals verteilt sie in Unity zufällig, aber fest an der Feldposition,
und setzt sie auf die Höhe des Reliefs. Low-Poly, ein Feld ist 2 m breit, Ursprung jeweils am Boden.
  Pinie_A    Schirmkiefer, hoher krummer Stamm, flache Schirmkrone aus drei Lagen (etwa 3,6 m)
  Pinie_B    kleinere Schirmkiefer, Krone seitlich verschoben, Stamm geneigt (etwa 2,8 m)
  Zypresse_A schlank, spitz (etwa 4 m)
  Macchia_A  Buschgruppe aus drei Kuppeln in Olivgrün (etwa 0,8 m)
  Macchia_B  flacher Busch mit trockenen Ocker-Büscheln (etwa 0,6 m)
  Fels_A     kantiger Block (etwa 1,6 m)
  Fels_B     flacher Brocken mit kleinem Begleiter
  Fels_C     größerer Brocken mit Begleitern
Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/gebirge.py -- <blend> <fbx> [<vorschau.png>]
"""
import math
import os
import random
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(2026)
PINE = mat("Pinie", (0.26, 0.40, 0.22))
PINE_LIGHT = mat("PinieHell", (0.36, 0.50, 0.26))
BARK = mat("PinienRinde", (0.42, 0.29, 0.20))
MACCHIA = mat("Macchia", (0.42, 0.48, 0.26))
MACCHIA_DRY = mat("MacchiaTrocken", (0.62, 0.55, 0.30))
CLIFF_A = mat("KalkfelsA", (0.72, 0.68, 0.60), rough=0.95)
CLIFF_B = mat("KalkfelsB", (0.62, 0.58, 0.52), rough=0.95)
CLIFF_C = mat("KalkfelsC", (0.80, 0.74, 0.64), rough=0.95)


def crown_layer(x, y, z, r, h, material, squash=1.0):
    """Eine flache Kuppel der Schirmkrone (abgeflachte Kugel)."""
    sphere(r, (x, y, z), (1.0, squash, h / r), material, segs=7, rings=4)


def trunk(points, r0, r1, material=BARK):
    """Stamm als Folge kegelförmiger Stücke durch die Punkte (leicht gekrümmt)."""
    for i in range(len(points) - 1):
        p0, p1 = Vector(points[i]), Vector(points[i + 1])
        ra = r0 + (r1 - r0) * i / (len(points) - 1)
        rb = r0 + (r1 - r0) * (i + 1) / (len(points) - 1)
        mid = (p0 + p1) / 2
        d = p1 - p0
        rot = d.to_track_quat("Z", "Y").to_euler()
        cone(ra, rb, d.length, tuple(mid), material, verts=6, rot=tuple(math.degrees(a) for a in rot))


# ---------------------------------------------------------------- Pinien
trunk([(0, 0, 0), (0.05, 0.03, 1.1), (-0.04, 0.02, 2.2), (0.06, -0.02, 2.9)], 0.17, 0.09)
crown_layer(0.1, 0.0, 3.05, 1.35, 0.42, PINE, 0.9)
crown_layer(-0.55, 0.35, 2.85, 0.9, 0.32, PINE_LIGHT)
crown_layer(0.6, -0.4, 3.15, 0.8, 0.30, PINE)
variant("Pinie_A")

trunk([(0, 0, 0), (0.18, 0.05, 0.9), (0.38, 0.08, 1.8), (0.55, 0.05, 2.3)], 0.15, 0.08)
crown_layer(0.65, 0.05, 2.45, 1.1, 0.36, PINE, 0.95)
crown_layer(0.2, -0.45, 2.3, 0.7, 0.28, PINE_LIGHT)
crown_layer(1.05, 0.4, 2.55, 0.6, 0.24, PINE)
variant("Pinie_B")


# ---------------------------------------------------------------- Zypresse
cyl(0.07, 0.3, (0, 0, 0.15), material=BARK, verts=5)
for i in range(5):
    z0 = 0.25 + i * 0.62
    rb = 0.52 * (1.0 - 0.17 * i)
    cone(rb, rb * 0.5, 0.78, (0, 0, z0 + 0.39), CYPRESS, verts=7)
cone(0.14, 0.0, 0.5, (0, 0, 3.5), CYPRESS, verts=7)
variant("Zypresse_A")


# ---------------------------------------------------------------- Macchia
def dome(x, y, r, h, material):
    sphere(r, (x, y, h * 0.35), (1.0, 1.0, h / r), material, segs=7, rings=4)


dome(0.0, 0.0, 0.42, 0.62, MACCHIA)
dome(0.34, 0.14, 0.30, 0.46, MACCHIA_DRY)
dome(-0.28, 0.22, 0.26, 0.42, LEAF_DARK)
variant("Macchia_A")

dome(0.0, 0.0, 0.55, 0.40, MACCHIA)
dome(0.4, -0.2, 0.28, 0.30, MACCHIA_DRY)
dome(-0.35, -0.25, 0.24, 0.26, MACCHIA_DRY)
dome(0.1, 0.35, 0.22, 0.24, OLIVE_LEAF)
variant("Macchia_B")


# ---------------------------------------------------------------- Felsen
def rock(x, y, sx, sy, sz, material, jitter=0.22, yaw=0.0, sink=0.3, subdiv=1):
    """Kantiger Brocken: Ikosphäre mit versetzten Eckpunkten, flach geschattet, unten abgeschnitten."""
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv, radius=1.0, location=(x, y, sz * (1.0 - sink)))
    obj = bpy.context.active_object
    for v in obj.data.vertices:
        f = 1.0 + random.uniform(-jitter, jitter)
        v.co = Vector((v.co.x * f, v.co.y * f, v.co.z * (1.0 + random.uniform(-jitter, jitter))))
        if v.co.z < -0.45:
            v.co.z = -0.45  # Boden: flach, damit der Fels aufsitzt
    obj.scale = (sx, sy, sz)
    obj.rotation_euler = (math.radians(random.uniform(-6, 6)), math.radians(random.uniform(-6, 6)), math.radians(yaw))
    return keep(obj, material)


rock(0.0, 0.0, 0.85, 0.7, 0.75, CLIFF_A, yaw=25)
rock(0.75, 0.3, 0.45, 0.4, 0.4, CLIFF_B, yaw=-20)
rock(-0.55, -0.45, 0.35, 0.3, 0.3, CLIFF_C, yaw=60)
variant("Fels_A")

rock(0.0, 0.0, 1.1, 0.8, 0.5, CLIFF_B, jitter=0.18, yaw=-10)
rock(0.9, -0.4, 0.35, 0.3, 0.26, CLIFF_A, yaw=40)
variant("Fels_B")

rock(0.0, 0.0, 0.62, 0.55, 0.7, CLIFF_C, jitter=0.2, yaw=15, sink=0.2)
rock(0.5, 0.2, 0.4, 0.36, 0.38, CLIFF_A, yaw=-30)
rock(-0.4, 0.3, 0.3, 0.26, 0.4, CLIFF_B, yaw=70)
variant("Fels_C")

export(spacing=3.2)
