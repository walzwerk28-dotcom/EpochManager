"""
Lehmgrube (Antike), 2 x 2 Felder (4 x 4 m), ein Objekt ohne Varianten (Lehmgrube_0).

Ein flacher Lehmrücken (Geländenetz mit weichen Rändern) trägt alles Wichtige. Darin ist hinten links die Abbaugrube
eingetieft: unregelmäßiger Rand mit Randwulst, an der Nordwest-Seite drei Abbaustufen mit Spatenspuren, am Grund nasser
dunkler Lehm mit einer trüben Pfütze. Rund um den Rand liegen weiche Aushubhaufen mit Gras. Von der Ostseite führt eine
schräge Bohlenrampe (Längsbohlen mit Querleisten) auf den Grund, auf ihr steht eine Schubkarre mit Lehm. Eine Holzrinne
leitet den Lehm in zwei Schlämmbecken (trüb ockerbraun) mit Schieber dazwischen. Hinten rechts ein kleiner Unterstand
mit Pultdach (Lehmkörbe, Spaten, Hacke). Vorn auf dem Boden der Trockenplatz: Lehmziegel hochkant als Hocken aufgestellt,
ein Tisch mit Ziegelformrahmen und ein Stapel fertiger Ziegel im Verband.

Hinweis: Die Bodenplatte im Spiel liegt bei z = 0,015 m und deckt alles darunter ab. Die Grube ist deshalb als Einschnitt
in einen Rücken gebaut (Grund bei 0,04 m, Rand bei 0,20 m plus Randwulst).

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/lehmgrube.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,64 x 3,64 m (Grundfläche 4 x 4 m), Vorderseite nach -Y.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(29)
rnd = random.Random(8)

# ---------------------------------------------------------------- Materialien (nur hier)
LEHM_NASS = mat("LehmNass", (0.31, 0.19, 0.12), rough=0.9)
SCHLAMM_BRAUN = mat("SchlammBraun", (0.58, 0.42, 0.24), rough=0.9)
SCHLAMM_HELL = mat("SchlammHell", (0.70, 0.56, 0.36), rough=0.85)
PFUETZE = mat("Pfuetze", (0.22, 0.15, 0.09), rough=0.72)
LEHM_BODEN = mat("LehmBoden", (0.54, 0.38, 0.26))
LEHM_HANG = mat("LehmHang", (0.43, 0.28, 0.19))
LEHM_STUFE = mat("LehmStufe", (0.62, 0.45, 0.32))
HAUFEN_A = mat("AushubA", (0.55, 0.41, 0.29))
HAUFEN_B = mat("AushubB", (0.60, 0.47, 0.34))
GRAS_RAND = mat("GrasRand", (0.40, 0.45, 0.24))

BASE = 0.18           # Höhe des Lehmrückens
FLOOR = 0.04          # Grubengrund


# ---------------------------------------------------------------- Hilfen
def sm(a, b, t):
    u = max(0.0, min(1.0, (t - a) / (b - a)))
    return u * u * (3 - 2 * u)


def _h(ix, iy, seed):
    n = (ix * 374761393 + iy * 668265263 + seed * 1442695041) & 0xFFFFFFFF
    n = ((n ^ (n >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((n ^ (n >> 16)) & 0xFFFF) / 65535.0


def vnoise(x, y, seed=0):
    ix, iy = math.floor(x), math.floor(y)
    fx, fy = x - ix, y - iy
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = _h(ix, iy, seed), _h(ix + 1, iy, seed)
    c, d = _h(ix, iy + 1, seed), _h(ix + 1, iy + 1, seed)
    return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy


def smooth_obj(obj):
    for p in obj.data.polygons:
        p.use_smooth = True
    return obj


def lift(fn, dz, *a, **k):
    """Ruft einen Baustein auf und hebt alle dabei entstandenen Teile um dz an (Bausteine bauen bei z = 0)."""
    n = len(PARTS)
    r = fn(*a, **k)
    for o in PARTS[n:]:
        o.location.z += dz
    return r


def tilt(fn, pivot, angle_y, *a, **k):
    """Baut mit fn und kippt alle neuen Teile um die y-Achse durch pivot (Winkel in Grad)."""
    n = len(PARTS)
    fn(*a, **k)
    bpy.context.view_layer.update()
    p = Vector(pivot)
    m = Matrix.Translation(p) @ Matrix.Rotation(math.radians(angle_y), 4, "Y") @ Matrix.Translation(-p)
    for o in PARTS[n:]:
        o.matrix_world = m @ o.matrix_world


# ---------------------------------------------------------------- Gelände mit Grube
PCX, PCY, PA, PB = -0.45, 0.50, 1.00, 0.80          # Mitte und Halbachsen der Grube


def pit_q(x, y):
    dx, dy = x - PCX, y - PCY
    th = math.atan2(dy, dx)
    rr = 1 + 0.09 * math.sin(2 * th + 0.8) + 0.06 * math.sin(3 * th + 2.1) + 0.04 * math.sin(5 * th)
    return math.hypot(dx / PA, dy / PB) / rr, th


def terrace_weight(th):
    d = abs(((th - 2.36 + math.pi) % math.tau) - math.pi)
    return 1 - sm(1.0, 1.8, d)


def terrain(x, y):
    yf = -0.80 + 0.08 * math.sin(2.3 * x + 0.7) + 0.05 * math.sin(5 * x)
    m = sm(yf - 0.34, yf + 0.34, y)
    m *= sm(-1.82 + 0.10 * (0.5 + 0.5 * math.sin(4 * y + 2)), -1.52 + 0.08 * math.sin(3 * y + 1) + 0.04 * math.sin(7 * y), x)
    m *= 1 - sm(1.58 + 0.06 * math.sin(2.7 * y) + 0.03 * math.sin(6 * y), 1.82 - 0.10 * (0.5 + 0.5 * math.sin(3.3 * y + 1)), x)
    m *= 1 - sm(1.50 + 0.07 * math.sin(2 * x + 2) + 0.03 * math.sin(5 * x), 1.82 - 0.10 * (0.5 + 0.5 * math.sin(2.6 * x)), y)
    plat = (BASE + 0.012 * (vnoise(x * 3, y * 3, 1) - 0.5)) * m
    q, th = pit_q(x, y)
    z = plat
    if q < 1.0:
        s = sm(0.50, 1.0, q)
        sf = s * 3
        if sf >= 3:
            st = 1.0
        else:
            k = math.floor(sf)
            st = (k + sm(0.0, 0.35, sf - k)) / 3
        s2 = s + (st - s) * terrace_weight(th)
        fl = FLOOR + 0.008 * (vnoise(x * 5, y * 5, 7) - 0.5)
        z = min(plat, fl + (plat - fl) * s2)
    if q < 1.5:
        z += 0.040 * math.exp(-((q - 1.06) / 0.15) ** 2) * (plat / BASE)       # Randwulst
    return max(0.0, z)


def slope_at(x, y, e=0.03):
    return math.hypot((terrain(x + e, y) - terrain(x - e, y)) / (2 * e), (terrain(x, y + e) - terrain(x, y - e)) / (2 * e))


GX0, GX1, GY0, GY1, GSTEP = -1.82, 1.82, -1.25, 1.82, 0.115
GNX, GNY = int(round((GX1 - GX0) / GSTEP)), int(round((GY1 - GY0) / GSTEP))
GSX, GSY = (GX1 - GX0) / GNX, (GY1 - GY0) / GNY
GVERTS = [(GX0 + i * GSX, GY0 + j * GSY) for j in range(GNY + 1) for i in range(GNX + 1)]
GZ = [terrain(x, y) for x, y in GVERTS]
GTRIS = []
for _j in range(GNY):
    for _i in range(GNX):
        _a, _b, _c, _d = _j * (GNX + 1) + _i, _j * (GNX + 1) + _i + 1, (_j + 1) * (GNX + 1) + _i + 1, (_j + 1) * (GNX + 1) + _i
        for _t in ((_a, _b, _c), (_a, _c, _d)):
            if max(GZ[k] for k in _t) >= 0.005:
                GTRIS.append(_t)


def mesh_object(name, verts, faces, material, weld=False):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(material)
    mesh.update()
    if weld:
        bm = bmesh.new()
        bm.from_mesh(mesh)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.to_mesh(mesh)
        bm.free()
    for p in mesh.polygons:
        p.use_smooth = True
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    PARTS.append(obj)
    return obj


def overlay(name, field, material, lift_z):
    """Fleck auf dem Gelände: Dreiecke des Geländenetzes an der Linie field = 0 geschnitten (lineare Interpolation), so
    entsteht eine glatte Kontur ohne Sägezahn. field(x, y, z) > 0 = innerhalb."""
    fv = [field(x, y, z) for (x, y), z in zip(GVERTS, GZ)]
    verts, faces = [], []
    for t in GTRIS:
        poly = []
        for n in range(3):
            a, b = t[n], t[(n + 1) % 3]
            pa = (GVERTS[a][0], GVERTS[a][1], GZ[a], fv[a])
            pb = (GVERTS[b][0], GVERTS[b][1], GZ[b], fv[b])
            if pa[3] >= 0:
                poly.append(pa)
            if (pa[3] >= 0) != (pb[3] >= 0):
                u = pa[3] / (pa[3] - pb[3])
                poly.append(tuple(pa[k] + (pb[k] - pa[k]) * u for k in range(3)) + (0.0,))
        if len(poly) < 3:
            continue
        base = len(verts)
        for p in poly:
            verts.append((round(p[0], 6), round(p[1], 6), p[2] + lift_z))
        for k in range(1, len(poly) - 1):
            faces.append((base, base + k, base + k + 1))
    if faces:
        mesh_object(name, verts, faces, material, weld=True)


def build_terrain():
    faces = [t for t in GTRIS]
    verts = [(x, y, z) for (x, y), z in zip(GVERTS, GZ)]
    mesh_object("Gelaende", verts, faces, LEHM_BODEN)
    # untere Grubenhänge dunkler, Trittflächen der Stufen heller, Grund nass
    overlay("Hang", lambda x, y, z: min(0.12 - z, 0.99 - pit_q(x, y)[0]), LEHM_HANG, 0.006)
    overlay("Stufen", lambda x, y, z: min((0.14 - slope_at(x, y)) * 0.4, terrace_weight(pit_q(x, y)[1]) - 0.3, pit_q(x, y)[0] - 0.6,
                                         0.97 - pit_q(x, y)[0], (z - (FLOOR + 0.03)) * 6, ((BASE - 0.02) - z) * 6),
            LEHM_STUFE, 0.010)
    overlay("Grund", lambda x, y, z: min(0.07 - z, 0.99 - pit_q(x, y)[0]), LEHM_NASS, 0.014)


build_terrain()


def surf_normal(x, y):
    e = 0.03
    dzx = (terrain(x + e, y) - terrain(x - e, y)) / (2 * e)
    dzy = (terrain(x, y + e) - terrain(x, y - e)) / (2 * e)
    return Vector((-dzx, -dzy, 1.0)).normalized()


# ---------------------------------------------------------------- Spatenspuren an den Abbaustufen
for qk, cnt in ((0.585, 9), (0.715, 10), (0.845, 11)):
    for i in range(cnt):
        th = math.radians(60 + (i + rnd.uniform(-0.25, 0.25)) * 180 / cnt)
        if terrace_weight(th) < 0.35:
            continue
        rr = 1 + 0.09 * math.sin(2 * th + 0.8) + 0.06 * math.sin(3 * th + 2.1) + 0.04 * math.sin(5 * th)
        x = PCX + math.cos(th) * PA * qk * rr
        y = PCY + math.sin(th) * PB * qk * rr
        n = surf_normal(x, y)
        tang = Vector((-math.sin(th), math.cos(th), 0.0))
        tang = (tang - n * tang.dot(n)).normalized()
        oriented_box((0.075, 0.016, 0.013), (x, y, terrain(x, y) + 0.003), tang, n, LEHM_NASS)

# Pfütze am Grund (trüb)
sphere(1.0, (PCX - 0.10, PCY - 0.04, 0.058), (0.36, 0.20, 0.004), PFUETZE, segs=20, rings=3)
sphere(1.0, (PCX + 0.30, PCY - 0.22, 0.056), (0.15, 0.09, 0.004), PFUETZE, segs=14, rings=3)


# ---------------------------------------------------------------- Aushubhaufen am Rand (weich, unregelmäßig)
def blob(x, y, z, r, h, seed, material):
    """Weicher, flacher Lehmklumpen: Kugel mit Rauschen verformt, unten glatt abgeschnitten (keine Unterkante), glatt schattiert."""
    rr = random.Random(seed)
    obj = sphere(r, (x, y, z), (1.0, rr.uniform(0.8, 1.2), h / r), material, segs=9, rings=5, rot=(0, 0, rr.uniform(0, 90)))
    for v in obj.data.vertices:
        v.co.x *= 1 + rr.uniform(-0.16, 0.16)
        v.co.y *= 1 + rr.uniform(-0.16, 0.16)
        v.co.z = max(v.co.z * (1 + rr.uniform(-0.2, 0.2)), 0.0)
    return smooth_obj(obj)


def spoil(x, y, r, h, seed):
    rr = random.Random(seed)
    z0 = terrain(x, y)
    blob(x, y, z0 - 0.012, r, h, seed, HAUFEN_A)
    for k in range(2):
        a = rr.uniform(0, math.tau)
        d = rr.uniform(0.5, 0.8) * r
        r2 = r * rr.uniform(0.45, 0.65)
        px, py = x + math.cos(a) * d, y + math.sin(a) * d
        blob(px, py, terrain(px, py) - 0.012, r2, h * rr.uniform(0.6, 0.9), seed * 3 + k, (HAUFEN_B, HAUFEN_A)[k])
    for k in range(2):                                              # Brocken obenauf
        a = rr.uniform(0, math.tau)
        d = rr.uniform(0.0, 0.5) * r
        sphere(0.025, (x + math.cos(a) * d, y + math.sin(a) * d, z0 + h * 0.75), (1.2, 0.9, 0.6), HAUFEN_B if k else CLAY_DARK,
               segs=6, rings=4)


SPOILS = [(-1.30, 1.36, 0.30, 0.075), (-0.55, 1.44, 0.26, 0.065), (-1.64, 0.80, 0.24, 0.07),
          (-1.40, -0.40, 0.28, 0.07), (-0.70, -0.56, 0.24, 0.06), (0.05, -0.50, 0.22, 0.06), (0.62, -0.32, 0.20, 0.06)]
for i, (x, y, r, h) in enumerate(SPOILS):
    spoil(x, y, r, h, 11 + i)

# Gras auf Haufen, Rand und Hang
for i, (gx, gy) in enumerate([(-1.38, 1.30), (-1.62, 0.98), (-0.68, 1.42), (-1.00, 1.30), (-1.72, 0.20), (-1.50, -0.30),
                              (0.20, -0.50), (-0.30, 1.50),
                              (0.50, 1.40), (-1.45, 1.40), (-1.78, 0.60)]):
    lift(grass_tuft, terrain(gx, gy) - 0.005, gx, gy, 0.11 + 0.03 * (i % 3), seed=i * 5 + 1)

# ---------------------------------------------------------------- Bohlenrampe an der Ostseite in die Grube
YR = 0.12
xt = next(x / 100 for x in range(130, 0, -1) if pit_q(x / 100, YR)[0] <= 0.95)
xb = next(x / 100 for x in range(130, -90, -1) if pit_q(x / 100, YR)[0] <= 0.52)
zt, zb = terrain(xt, YR), terrain(xb, YR)
dev = 0.0
for k in range(41):
    t = k / 40
    xx = xt + (xb - xt) * t
    dev = max(dev, terrain(xx, YR) - (zt + (zb - zt) * t))
lift_r = dev + 0.022
P0 = Vector((xt, YR, zt + lift_r))
P1 = Vector((xb, YR, zb + lift_r))
dvec = P1 - P0
rlen = dvec.length
dvec.normalize()
nrm = Vector((dvec.z, 0.0, -dvec.x))
mid = (P0 + P1) / 2
for off, mt in ((-0.105, WOOD), (0.0, WOOD_DARK), (0.105, WOOD)):                  # drei Längsbohlen
    oriented_box((rlen + 0.06, 0.098, 0.028), tuple(mid + Vector((0, off, 0)) + nrm * 0.012), dvec, nrm, mt)
for k in range(1, int(rlen / 0.085)):                                              # Querleisten obenauf
    c = P0 + dvec * (k * 0.085 + 0.02) + nrm * 0.032
    oriented_box((0.03, 0.34, 0.016), tuple(c), dvec, nrm, WOOD_DARK)
for sy in (-0.19, 0.19):                                                           # Seitenbretter
    oriented_box((rlen, 0.02, 0.05), tuple(mid + Vector((0, sy, 0)) + nrm * 0.03), dvec, nrm, WOOD_DARK)

# Schubkarre (eigener Bau): Holzkasten mit schrägen Brettern, zwei Holme unter dem Kasten, Scheibenrad vorn, zwei Beine hinten.
# Lokal: Fahrtrichtung +x, Boden z = 0; danach verschoben und gedreht.
LEHM_LADUNG = mat("LehmLadung", (0.60, 0.43, 0.29), rough=0.95)


def lehm_barrow(px, py, pz, yaw, tilt=0.0):
    n0 = len(PARTS)
    for sy in (-1, 1):                                                    # Holme: Griff hinten, Achse vorn
        rod((-0.36, sy * 0.055, 0.19), (0.30, sy * 0.055, 0.115), 0.012, WOOD_DARK, verts=6)
        rod((-0.21, sy * 0.09, 0.0), (-0.21, sy * 0.075, 0.165), 0.011, WOOD_DARK, verts=6)      # kurzes Bein
        cyl(0.017, 0.03, (-0.36, sy * 0.055, 0.19), rot=(0, 90, 0), material=WOOD, verts=6)       # Griffende
    box((0.04, 0.17, 0.02), (-0.05, 0.0, 0.155), material=WOOD)               # Querbrett unter dem Kasten
    box((0.04, 0.17, 0.02), (0.13, 0.0, 0.135), material=WOOD)
    box((0.34, 0.20, 0.02), (0.01, 0.0, 0.17), material=WOOD)                 # Kastenboden
    for sy in (-1, 1):                                                        # Seitenbretter, nach oben ausgestellt
        box((0.34, 0.012, 0.11), (0.01, sy * 0.114, 0.233), rot=(-sy * 15, 0, 0), material=WOOD_DARK)
    box((0.012, 0.22, 0.11), (0.179, 0.0, 0.233), rot=(0, 15, 0), material=WOOD_DARK)
    box((0.012, 0.22, 0.11), (-0.169, 0.0, 0.233), rot=(0, -15, 0), material=WOOD_DARK)
    cyl(0.103, 0.030, (0.30, 0.0, 0.10), rot=(90, 0, 0), material=IRON, verts=16)
    cyl(0.097, 0.042, (0.30, 0.0, 0.10), rot=(90, 0, 0), material=WOOD, verts=16)
    cyl(0.020, 0.15, (0.30, 0.0, 0.10), rot=(90, 0, 0), material=IRON, verts=8)        # Achse
    # Ladung: ein Körper, der das Kasteninnere ausfüllt (Pyramidenstumpf passend zu den ausgestellten Brettern, 4 mm Abstand),
    # oben leicht gewölbt und unregelmäßig; an den Brettern 1 cm unter dem Rand, in der Mitte bis 2,4 cm darüber.
    z_floor, z_edge = 0.181, 0.276
    lr = random.Random(5)

    def wall(z):
        s = (z - 0.18) * 0.264
        return -0.145 - s, 0.155 + s, 0.090 + s            # x hinten, x vorn, halbe Breite

    nx_, ny_ = 8, 4
    hts = {}
    for j in range(ny_ + 1):
        for i in range(nx_ + 1):
            u, v = i / nx_ * 2 - 1, j / ny_ * 2 - 1
            edge = abs(u) == 1 or abs(v) == 1
            hts[(i, j)] = z_edge if edge else z_edge + 0.034 * (1 - u * u) * (1 - v * v) ** 0.7 + lr.uniform(-0.005, 0.005)

    def surface(u, v, z):
        xb, xf, hw = wall(z)
        return ((xb + xf) / 2 + u * (xf - xb) / 2, v * hw, z)

    verts = []
    for j in range(ny_ + 1):
        for i in range(nx_ + 1):
            verts.append(surface(i / nx_ * 2 - 1, j / ny_ * 2 - 1, hts[(i, j)]))
    loop = [(i, 0) for i in range(nx_ + 1)] + [(nx_, j) for j in range(1, ny_ + 1)] + \
           [(i, ny_) for i in range(nx_ - 1, -1, -1)] + [(0, j) for j in range(ny_ - 1, 0, -1)]
    base0 = len(verts)
    for (i, j) in loop:
        verts.append(surface(i / nx_ * 2 - 1, j / ny_ * 2 - 1, z_floor))
    faces = [(j * (nx_ + 1) + i, j * (nx_ + 1) + i + 1, (j + 1) * (nx_ + 1) + i + 1, (j + 1) * (nx_ + 1) + i)
             for j in range(ny_) for i in range(nx_)]
    n = len(loop)
    for k in range(n):
        a, b = loop[k], loop[(k + 1) % n]
        faces.append((a[1] * (nx_ + 1) + a[0], b[1] * (nx_ + 1) + b[0], base0 + (k + 1) % n, base0 + k))
    faces.append(tuple(base0 + k for k in range(n)))
    smooth_obj(solid_mesh(verts, faces, [0] * len(faces), [LEHM_LADUNG], "Ladung"))
    for k, (bu, bv) in enumerate(((-0.45, 0.25), (0.15, -0.35), (0.55, 0.3), (-0.1, 0.1))):      # Brocken, zu etwa 30 % eingesenkt
        i, j = (bu + 1) / 2 * nx_, (bv + 1) / 2 * ny_
        i0, j0 = min(int(i), nx_ - 1), min(int(j), ny_ - 1)
        fi, fj = i - i0, j - j0
        h = (hts[(i0, j0)] * (1 - fi) * (1 - fj) + hts[(i0 + 1, j0)] * fi * (1 - fj) +
             hts[(i0, j0 + 1)] * (1 - fi) * fj + hts[(i0 + 1, j0 + 1)] * fi * fj)
        x, y, _ = surface(bu, bv, h)
        sphere(0.026, (x, y, h + 0.007), (1.2, 1.0, 0.65), HAUFEN_B if k % 2 else LEHM_NASS, segs=6, rings=4)
    bpy.context.view_layer.update()
    m = Matrix.Translation((px, py, pz)) @ Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(tilt), 4, "Y")
    for o in PARTS[n0:]:
        o.matrix_world = m @ o.matrix_world


# Auf der Rampe: Beine und Rad liegen auf der Rampenfläche, gleiche Neigung wie die Rampe, Rad bergab
slope = math.degrees(math.atan2(P0.z - P1.z, P0.x - P1.x))
print("Rampe: Laenge", round(rlen, 3), "Neigung", round(slope, 1))
_s = max(0.24, min(rlen - 0.34, rlen / 2 - 0.045))
_piv = P0 + dvec * _s + nrm * 0.040
lehm_barrow(_piv.x, _piv.y, _piv.z, 180, tilt=slope)

# ---------------------------------------------------------------- Werkzeug und Körbe in der Grube
spade((-0.10, 0.42, 0.045), (-0.04, 0.38, 0.64), across=(0.2, 1.0, 0.0))
hoe((-1.00, 0.20, 0.09), (-1.22, 0.36, 0.64), across=(0.0, 1.0, 0.0))
olive_basket(-0.30, 0.86, 0.045, r=0.085, fill=False)
for k in range(5):
    sphere(0.04, (-0.30 + rnd.uniform(-0.04, 0.04), 0.86 + rnd.uniform(-0.04, 0.04), 0.135 + 0.012 * (k % 2)),
           (1.0, 1.0, 0.8), CLAY if k % 2 else CLAY_DARK, segs=6, rings=4)
for k in range(4):                                                # abgestochene Brocken am Grund
    sphere(0.05, (-0.95 + 0.11 * k, 0.70 - 0.05 * (k % 2), 0.065), (1.2, 1.0, 0.7), CLAY_BRICK if k % 2 else CLAY, segs=7, rings=4)

# ---------------------------------------------------------------- Rinne und zwei Schlämmbecken
B1 = (1.02, 1.58, 0.55, 0.93)
B2 = (1.02, 1.58, -0.05, 0.33)
lift(clay_basin, BASE, *B1, fill=SCHLAMM_BRAUN, h=0.15)
lift(clay_basin, BASE, *B2, fill=SCHLAMM_HELL, h=0.13)
for k in range(5):                                                # Klumpen im ersten Becken
    sphere(0.03, (1.14 + 0.09 * k, 0.74 + 0.09 * ((k % 3) - 1), BASE + 0.12), (1.0, 1.0, 0.3), CLAY_BRICK, segs=6, rings=3)
GY = 0.74                                                          # Rinne vom Grubenrand zum ersten Becken
gz0, gz1 = BASE + 0.16, BASE + 0.14
x_g0, x_g1 = 0.50, 1.06
cz = (gz0 + gz1) / 2
gang = math.degrees(math.atan2(gz0 - gz1, x_g1 - x_g0))
gmx = (x_g0 + x_g1) / 2
box((x_g1 - x_g0, 0.11, 0.025), (gmx, GY, cz), rot=(0, gang, 0), material=WOOD)
for sy in (-1, 1):
    box((x_g1 - x_g0, 0.02, 0.06), (gmx, GY + sy * 0.055, cz + 0.03), rot=(0, gang, 0), material=WOOD)
box((x_g1 - x_g0 - 0.06, 0.07, 0.008), (gmx, GY, cz + 0.02), rot=(0, gang, 0), material=SCHLAMM_BRAUN)
for x in (0.62, 0.92):
    for sy in (-1, 1):
        box((0.04, 0.04, gz0 - BASE - 0.02), (x, GY + sy * 0.055, BASE + (gz0 - BASE - 0.02) / 2 - 0.005), material=WOOD_DARK)
# Schieber zwischen den Becken
box((0.10, 0.20, 0.025), (1.30, 0.44, BASE + 0.07), material=WOOD)
for sx in (-1, 1):
    box((0.02, 0.20, 0.07), (1.30 + sx * 0.05, 0.44, BASE + 0.10), material=WOOD)
box((0.12, 0.012, 0.13), (1.30, 0.44, BASE + 0.13), material=WOOD_DARK)
cyl(0.010, 0.07, (1.30, 0.44, BASE + 0.215), rot=(0, 90, 0), material=WOOD, verts=6)
bar((1.46, 0.56, BASE + 0.40), (1.20, 0.70, BASE + 0.16), 0.02, WOOD_DARK)                      # Rührstange

# ---------------------------------------------------------------- Unterstand hinten rechts (niedrig, Traufe etwa 0,7 m)
SX0, SX1, SY0, SY1 = 0.90, 1.62, 1.04, 1.60
for x in (SX0 + 0.04, SX1 - 0.04):
    for y, hh in ((SY1 - 0.04, 0.64), (SY0 + 0.04, 0.46)):
        box((0.065, 0.065, hh + 0.02), (x, y, BASE + hh / 2 - 0.01), material=WOOD_DARK)
shed_roof(SX0, SX1, SY0, SY1, BASE + 0.66, "-y", overhang=0.05, pitch=14, eave_overhang=0.05)
bar((SX0 + 0.04, SY1 - 0.04, BASE + 0.56), (SX0 + 0.04, SY0 + 0.04, BASE + 0.42), 0.032, WOOD)
bar((SX1 - 0.04, SY1 - 0.04, BASE + 0.56), (SX1 - 0.04, SY0 + 0.04, BASE + 0.42), 0.032, WOOD)
box((SX1 - SX0 - 0.10, 0.035, 0.34), ((SX0 + SX1) / 2, SY1 - 0.02, BASE + 0.18), material=WOOD)       # Bretterwand hinten
lift(clay_mound, BASE, 1.12, 1.46, r=0.17, h=0.11, seed=3)
for bx, by in ((1.36, 1.22), (1.50, 1.34), (1.22, 1.30)):
    lift(olive_basket, BASE, bx, by, 0.0, r=0.075, fill=False)
    for k in range(4):
        sphere(0.034, (bx + rnd.uniform(-0.03, 0.03), by + rnd.uniform(-0.03, 0.03), BASE + 0.11 + 0.01 * (k % 2)), (1.0, 1.0, 0.8),
               CLAY if k % 2 else CLAY_BRICK, segs=6, rings=4)
spade((1.52, 1.52, BASE + 0.02), (1.47, 1.56, BASE + 0.62), across=(0.2, 1.0, 0.0))
hoe((0.99, 1.52, BASE + 0.02), (1.04, 1.56, BASE + 0.60), across=(0.0, 1.0, 0.0))

# ---------------------------------------------------------------- Trockenplatz vorn: Lehmziegel hochkant zum Trocknen
BL, BW, BH = 0.19, 0.055, 0.095          # Ziegel auf der langen Schmalkante: Länge, Dicke, Höhe


def stand(x, y, mt, lean=0.0, yaw=0.0):
    """Ziegel hochkant, um die Längsachse (x) um lean Grad geneigt; Fuß steht auf z = 0,015."""
    c, s = math.cos(math.radians(abs(lean))), math.sin(math.radians(abs(lean)))
    box((BL, BW, BH), (x, y, 0.015 + BH / 2 * c + BW / 2 * s - 0.003), rot=(lean, 0, yaw), material=mt)


def tent(x, y, mat_a, mat_b, phi=22):
    for sgn, mt in ((1, mat_a), (-1, mat_b)):
        stand(x, y + sgn * 0.05, mt, lean=sgn * phi)


for i in range(7):                                                   # Reihe 1: dachförmige Paare mit Lücken
    tent(-1.60 + i * 0.27, -1.72, CLAY_UNFIRED if i % 2 else CLAY_BRICK, CLAY_BRICK if i % 2 else CLAY_UNFIRED)
for i in range(7):                                                   # Reihe 2: lockere Reihe, alle leicht geneigt
    stand(-1.55 + i * 0.27, -1.50, CLAY_UNFIRED if i % 3 else CLAY_BRICK, lean=-12, yaw=rnd.uniform(-4, 4))
for i in range(3):                                                   # frisch geformt, noch flach
    box((BL, 0.095, 0.045), (-1.52 + i * 0.30, -1.28, 0.015 + 0.0225), rot=(0, 0, 3 * (i - 1)), material=CLAY)

# Tisch mit Ziegelformrahmen
TX, TY, TH = 0.60, -1.52, 0.27
box((0.60, 0.34, 0.025), (TX, TY, 0.015 + TH), material=WOOD)
for sx in (-1, 1):
    for sy in (-1, 1):
        box((0.035, 0.035, TH), (TX + sx * 0.26, TY + sy * 0.13, 0.015 + TH / 2), material=WOOD_DARK)
bar((TX - 0.26, TY, 0.015 + 0.10), (TX + 0.26, TY, 0.015 + 0.10), 0.014, WOOD_DARK)
FZ = 0.015 + TH + 0.0125
FX, FY = TX - 0.10, TY
box((0.34, 0.15, 0.006), (FX, FY, FZ + 0.003), material=LEHM_NASS)                       # Lehmspur auf dem Tisch
for sy in (-1, 1):
    box((0.34, 0.016, 0.045), (FX, FY + sy * 0.067, FZ + 0.0225), material=WOOD)
for sx in (-1, 1):
    box((0.016, 0.15, 0.045), (FX + sx * 0.162, FY, FZ + 0.0225), material=WOOD)
for dx in (-0.055, 0.055):
    box((0.012, 0.14, 0.045), (FX + dx, FY, FZ + 0.0225), material=WOOD_DARK)
box((0.100, 0.115, 0.030), (FX - 0.108, FY, FZ + 0.021), material=CLAY)                  # ein Fach gefüllt
for i in range(2):                                                                       # fertig geformte Ziegel
    box((0.17, 0.085, 0.045), (TX + 0.18, TY + (i - 0.5) * 0.10, FZ + 0.0225), rot=(0, 0, 5 * i), material=CLAY_BRICK)
smooth_obj(sphere(0.07, (TX + 0.20, TY + 0.10, FZ + 0.03), (1.0, 0.9, 0.65), CLAY, segs=10, rings=5))
barrel(TX - 0.43, TY + 0.04, z0=0.015, r=0.07)

# Stapel fertiger Ziegel im Verband
SXc, SYc = 1.30, -1.45
for layer in range(7):
    z = 0.015 + layer * 0.047
    for a in range(2):
        for b in range(4):
            if layer % 2 == 0:
                x = SXc + (a - 0.5) * (0.17 + 0.004)
                y = SYc + (b - 1.5) * (0.085 + 0.004)
                size = (0.17, 0.085, 0.045)
            else:
                x = SXc + (b - 1.5) * (0.085 + 0.004)
                y = SYc + (a - 0.5) * (0.17 + 0.004)
                size = (0.085, 0.17, 0.045)
            box(size, (x, y, z + 0.0235), rot=(0, 0, rnd.uniform(-1.2, 1.2)), material=CLAY_BRICK if (a + b + layer) % 3 else CLAY_UNFIRED)

# Gras am Rand des Platzes
for i, (gx, gy) in enumerate([(-1.78, -1.25), (-1.74, -1.74), (-0.35, -1.76), (1.65, -1.70), (1.74, -1.20), (0.15, -1.15), (-1.3, -1.10)]):
    grass_tuft(gx, gy, 0.09 + 0.02 * (i % 2), seed=60 + i)

obj = variant("Lehmgrube_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=7.0)
