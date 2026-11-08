"""
Feuerwache (Antike), 2 x 2 Felder (4 x 4 m): Wache der Vigiles, der römischen Feuerwehr. Zweistöckiges verputztes Wachhaus
(rote Sockelzone, Gesims, Ziegeldach mit dem Giebel zur Straße und einer Tafel im Giebel) mit breitem Bogentor, durch das
man den Löschkarren sieht (Wasserfass, Druckpumpe mit Schwengel, Lederschlauch). Auf dem Dach ein kleiner Ausguck mit
Ziegelhelm, Signalbecken und roter Fahne. Rechts neben dem Haus ein gemauertes Wasserbecken mit einer Reihe Eimer an der
Wand darüber, daneben Leitern und Feuerhaken an der Wand; vor der Front eine Bank mit gerollten Decken (centones).
Hinter dem Haus ein kleiner Hof mit Wasserfässern, Brennholz, Kisten und einer Zypresse.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/feuerwache.py -- <blend> <fbx> [<vorschau.png>]

Vorderseite nach -Y, alles innerhalb von 3,61 x 3,61 m (Grundfläche 4 x 4 m). Die FBX enthält ein Objekt (Feuerwache_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(41)

X0, X1 = -1.70, 0.95        # Wachhaus
Y0, Y1 = -1.66, 0.70
H = 1.75                    # Wandhöhe unter dem Dach
WT = 0.12                   # Stärke der Vorderwand
GC, GHW, SPRING = -0.85, 0.50, 0.55   # Tor: Mitte, halbe Breite, Kämpferhöhe
CAV_Y = -0.30               # Rückwand der Wagenhalle
XM = (X0 + X1) / 2

# ---------------------------------------------------------------- Mauerwerk
# Vorderwand mit Bogentor aus einem Stück (Umriss mit Einschnitt von unten, per Ohrenschnitt zerlegt)
front = [(X0, 0.0), (GC - GHW, 0.0), (GC - GHW, SPRING)] + arc_pts(GC, SPRING, GHW, 180, 0, 15)[1:-1]
front += [(GC + GHW, SPRING), (GC + GHW, 0.0), (X1, 0.0), (X1, H), (X0, H)]
slab(front, "xz", Y0, WT, PLASTER_WHITE)
# Rest des Hauses: links und rechts der Wagenhalle, hinter ihr, und über ihr das Obergeschoss
box((GC - GHW - X0, Y1 - (Y0 + WT), H), ((X0 + GC - GHW) / 2, (Y0 + WT + Y1) / 2, H / 2), material=PLASTER_WHITE)
box((X1 - (GC + GHW), Y1 - (Y0 + WT), H), ((GC + GHW + X1) / 2, (Y0 + WT + Y1) / 2, H / 2), material=PLASTER_WHITE)
box((2 * GHW, Y1 - CAV_Y, H), (GC, (CAV_Y + Y1) / 2, H / 2), material=PLASTER_WHITE)
HALL_TOP = 1.12
box((2 * GHW, CAV_Y - (Y0 + WT), H - HALL_TOP), (GC, (Y0 + WT + CAV_Y) / 2, (H + HALL_TOP) / 2), material=PLASTER_WHITE)
# Boden der Wagenhalle
box((2 * GHW, CAV_Y - (Y0 + WT) + 0.1, 0.03), (GC, (Y0 + WT + CAV_Y) / 2 - 0.05, 0.015), material=PAVE_WORN)

# Bruchsteinsockel und rote Sockelzone, nur wo Wand steht (nicht im Tor)
for xa, xb in ((X0, GC - GHW), (GC + GHW, X1)):
    cx = (xa + xb) / 2
    box((xb - xa + (0.04 if xa == X0 else 0.0), 0.04, 0.09), (cx - (0.02 if xa == X0 else 0), Y0 - 0.02, 0.045), material=STONE)
    box((xb - xa, 0.012, 0.26), (cx, Y0 - 0.006, 0.20), material=SOCLE_RED)
for x in (X0, X1):
    out = -1 if x == X0 else 1
    box((0.04, Y1 - Y0 + 0.04, 0.09), (x + out * 0.02, (Y0 + Y1) / 2, 0.045), material=STONE)
    box((0.012, Y1 - Y0, 0.26), (x + out * 0.006, (Y0 + Y1) / 2, 0.20), material=SOCLE_RED)
box((X1 - X0 + 0.04, 0.04, 0.09), (XM, Y1 + 0.02, 0.045), material=STONE)
box((X1 - X0, 0.012, 0.26), (XM, Y1 + 0.006, 0.20), material=SOCLE_RED)

# Gesims zwischen den Geschossen und Eckpilaster
cornice(X0, X1, Y0, Y1, 1.19)
for x, ox in ((X0, -1), (X1, 1)):
    for y, oy in ((Y0, -1), (Y1, 1)):
        box((0.09, 0.09, H - 0.30), (x + ox * 0.012, y + oy * 0.012, 0.09 + (H - 0.30) / 2 + 0.0), material=LIME_DARK)

# ---------------------------------------------------------------- Tor
r_out = GHW + 0.045
for a in range(0, 181, 15):                                         # Bogensteine aus Ziegel, Schlussstein oben
    t = math.radians(a)
    big = a == 90
    box((0.075 if not big else 0.10, 0.02, 0.06 if not big else 0.095),
        (GC + r_out * math.cos(t), Y0 - 0.01, SPRING + r_out * math.sin(t)), rot=(0, -a, 0),
        material=LIME if big else TILE_DARK)
for s in (-1, 1):
    box((0.07, 0.025, SPRING - 0.03), (GC + s * r_out, Y0 - 0.0125, (SPRING + 0.03) / 2), material=LIME_DARK)   # Gewände
    box((0.12, 0.04, 0.05), (GC + s * r_out, Y0 - 0.02, SPRING), material=LIME_DARK)                           # Kämpfer
box((2 * GHW + 0.34, 0.12, 0.04), (GC, Y0 - 0.06, 0.02), material=LIME)                                       # Schwelle
box((1.30, 0.10, 0.045), (GC, -1.71, 0.0225), material=LIME_DARK)                                # Auftritt
# Eisenhaken seitlich des Tores (Torflügel hängen offen innen, hier nur die Haken)
for s in (-1, 1):
    box((0.025, 0.03, 0.025), (GC + s * (GHW + 0.015), Y0 + WT + 0.02, 0.32), material=IRON)

# Löschkarren in der Halle
fire_cart(GC, -1.00)
bucket(GC - 0.36, -0.44, 0.0, 0.05)
bucket(GC + 0.30, -0.42, 0.0, 0.05)
rope_coil(GC + 0.34, -0.60, 0.0, 0.07, 3)

# ---------------------------------------------------------------- Fenster und Tür
door("-y", Y0, 0.62, w=0.30, h=0.55)
window("-y", Y0, 0.12, 0.30, w=0.20, h=0.24, shutters=None)
window("-y", Y0, X0 + 0.20, 0.30, w=0.20, h=0.24, shutters=None)
for u in (-1.45, -0.70, 0.05, 0.70):
    window("-y", Y0, u, 1.36, w=0.20, h=0.28, shutters=PAINT_GREEN)
for u in (-1.0, -0.2, 0.55):
    window("-x", X0, u, 1.36, w=0.20, h=0.28, shutters=PAINT_GREEN)
    window("+x", X1, u, 1.36, w=0.20, h=0.28, shutters=PAINT_GREEN)
for u in (-1.3, 0.35):
    window("+y", Y1, u, 1.36, w=0.20, h=0.28, shutters=PAINT_GREEN)
window("+y", Y1, 0.35, 0.40, w=0.20, h=0.24, shutters=None)
window("-x", X0, -0.3, 0.45, w=0.20, h=0.24, shutters=None)
door("+y", Y1, -0.40, w=0.30, h=0.55)

# ---------------------------------------------------------------- Dach, Tafel im Giebel
ridge = gable_roof(X0, X1, Y0, Y1, H, "y", PLASTER_WHITE, overhang=0.09, pitch=24.0)
box((0.50, 0.02, 0.17), (XM, Y0 - 0.012, 2.00), material=WOOD_DARK)
box((0.45, 0.01, 0.125), (XM, Y0 - 0.02, 2.00), material=SOCLE_RED)
for k in range(-3, 4):                                                # Schriftzug VIGILES als helle Klötzchen
    box((0.034, 0.008, 0.07), (XM + k * 0.052, Y0 - 0.028, 2.00), material=FEATHER)
for s in (-1, 1):                                                     # kleine Bronzescheiben beidseits
    cyl(0.035, 0.012, (XM + s * 0.40, Y0 - 0.012, 2.00), rot=(90, 0, 0), material=BRONZE, verts=10)

# ---------------------------------------------------------------- Ausguck auf dem Dach
TX, TY, TS = XM, 0.12, 0.24             # Mitte und halbe Seite
ZB, ZC = 2.05, 2.72                     # Turmschaft von ZB (unten im Dach versenkt) bis ZC (Gesims)
box((2 * TS, 2 * TS, ZC - ZB), (TX, TY, (ZB + ZC) / 2), material=PLASTER_WHITE)
box((2 * TS + 0.04, 2 * TS + 0.04, 0.09), (TX, TY, 2.36), material=STONE)
for sx, sy, face, plane, u in ((0, -1, "-y", TY - TS, TX), (0, 1, "+y", TY + TS, TX),
                               (-1, 0, "-x", TX - TS, TY), (1, 0, "+x", TX + TS, TY)):
    window(face, plane, u, 2.46, w=0.13, h=0.18, shutters=None)
cornice(TX - TS, TX + TS, TY - TS, TY + TS, ZC)
ZP = ZC + 0.02
PH = 0.34                               # Höhe der Pfosten
for sx in (-1, 1):
    for sy in (-1, 1):
        box((0.075, 0.075, PH), (TX + sx * (TS - 0.02), TY + sy * (TS - 0.02), ZP + PH / 2), material=LIME)
for s in (-1, 1):                                                           # niedrige Brüstung auf drei Seiten
    box((2 * TS - 0.06, 0.04, 0.12), (TX, TY + s * (TS - 0.02), ZP + 0.06), material=PLASTER_WHITE)
    if s > 0:
        box((0.04, 2 * TS - 0.06, 0.12), (TX + (TS - 0.02), TY, ZP + 0.06), material=PLASTER_WHITE)
        box((0.04, 2 * TS - 0.06, 0.12), (TX - (TS - 0.02), TY, ZP + 0.06), material=PLASTER_WHITE)
ZR = ZP + PH + 0.02
box((2 * TS + 0.02, 2 * TS + 0.02, 0.04), (TX, TY, ZR), material=LIME_DARK)   # Balken
brazier(TX, TY, ZP, h=0.22)                                                          # Signalbecken
HH = 0.26                                                                             # Helmhöhe
cone(TS * 1.9, 0.0, HH, (TX, TY, ZR + 0.02 + HH / 2), TILE, verts=4, rot=(0, 0, 45))
for k in range(4):                                                                    # Gratziegel
    a = math.radians(45 + 90 * k)
    rod((TX + TS * 1.9 * math.cos(a), TY + TS * 1.9 * math.sin(a), ZR + 0.03),
        (TX, TY, ZR + 0.02 + HH), 0.012, TILE_DARK, verts=5)
ZT = ZR + 0.02 + HH
cyl(0.007, 0.24, (TX, TY, ZT + 0.12), material=WOOD_DARK, verts=5)                   # Fahnenstange
sphere(0.02, (TX, TY, ZT + 0.015), material=BRONZE, segs=6, rings=4)
box((0.20, 0.008, 0.10), (TX + 0.10, TY, ZT + 0.19), material=SOCLE_RED)             # Fahne
box((0.04, 0.009, 0.10), (TX + 0.22, TY, ZT + 0.19), material=CLOTH_OCHRE)

# ---------------------------------------------------------------- Becken, Eimer, Leitern, Haken an der rechten Wand
BX0, BX1, BY0, BY1, BH = 1.07, 1.70, -1.66, -0.80, 0.30
for xa, xb, ya, yb in ((BX0, BX1, BY0, BY0 + 0.10), (BX0, BX1, BY1 - 0.10, BY1), (BX0, BX0 + 0.10, BY0, BY1),
                       (BX1 - 0.10, BX1, BY0, BY1)):
    box((xb - xa, yb - ya, BH), ((xa + xb) / 2, (ya + yb) / 2, BH / 2), material=LIME)
    box((xb - xa + 0.03, yb - ya + 0.03, 0.04), ((xa + xb) / 2, (ya + yb) / 2, BH + 0.02), material=LIME_DARK)
box((BX1 - BX0 - 0.2, BY1 - BY0 - 0.2, 0.26), ((BX0 + BX1) / 2, (BY0 + BY1) / 2, 0.13), material=LIME_DARK)
box((BX1 - BX0 - 0.2, BY1 - BY0 - 0.2, 0.01), ((BX0 + BX1) / 2, (BY0 + BY1) / 2, 0.265), material=WATER)
rod((X1, -1.25, 0.52), (1.16, -1.25, 0.50), 0.016, BRONZE, verts=8)                  # Wasserhahn aus der Wand
water_jet((1.17, -1.25, 0.49), (1.19, -1.25, 0.28), thick=0.014)
for y in (-1.58, -1.40, -1.22, -1.04):                                                 # Eimerreihe an Zapfen
    rod((X1, y, 0.76), (1.03, y, 0.76), 0.012, WOOD_DARK, verts=5)
    bucket(1.025 + 0.0, y, 0.64, 0.045)
for y, xb, zt in ((-0.40, 1.40, 1.68), (0.00, 1.32, 1.58)):
    ladder((xb, y, 0.0), (X1 + 0.045, y, zt), across=(0, 1, 0), width=0.26)
fire_hook((1.18, 0.34, 0.0), (X1 + 0.03, 0.34, 1.76), across=(0, 1, 0))
fire_hook((1.12, 0.52, 0.0), (X1 + 0.03, 0.52, 1.70), across=(0, 1, 0))

# Bank mit gerollten Decken vor der Front
bench(0.10, -1.69, yaw=0, length=0.62)
for k, (m, dx) in enumerate(((CLOTH_RED, -0.18), (WOOL_BLUE, -0.06), (WOOL_GREY, 0.07), (CLOTH_OCHRE, 0.19))):
    cyl(0.045, 0.12, (0.10 + dx, -1.69, 0.195 + 0.045), rot=(0, 90, 90), material=m, verts=8)
    cyl(0.047, 0.012, (0.10 + dx, -1.69, 0.24), rot=(0, 90, 90), material=WOOD_DARK, verts=8)

# ---------------------------------------------------------------- Hof hinter dem Haus
YB = 1.68
for xa, xb in ((-1.72, -0.60), (-0.20, 1.72)):                                       # Hofmauer hinten mit Lücke
    cx = (xa + xb) / 2
    box((xb - xa, 0.12, 0.34), (cx, YB, 0.17), material=PLASTER_OCHRE)
    box((xb - xa + 0.02, 0.14, 0.05), (cx, YB, 0.025), material=STONE)
    box((xb - xa + 0.04, 0.17, 0.04), (cx, YB, 0.36), material=TILE_DARK)
for s in (-1, 1):
    box((0.13, 0.13, 0.5), (-0.40 + s * 0.26, YB, 0.25), material=LIME)
    sphere(0.06, (-0.40 + s * 0.26, YB, 0.54), material=LIME_DARK, segs=7, rings=4)
for sx in (-1, 1):
    ya, yb = Y1 + 0.05, YB
    x = sx * 1.66
    if sx > 0:
        x = 1.66
    box((0.12, yb - ya, 0.30), (x, (ya + yb) / 2, 0.15), material=PLASTER_OCHRE)
    box((0.17, yb - ya + 0.04, 0.04), (x, (ya + yb) / 2, 0.32), material=TILE_DARK)
for x, y in ((-1.50, 1.30), (-1.20, 1.28), (-1.36, 1.05)):                           # Wasserfässer
    barrel(x, y, 0.0, 0.085)
    cyl(0.080, 0.006, (x, y, 0.2152 + 0.0), material=WATER, verts=10)
woodpile(0.55, 1.58, length=0.9, rows=(5, 4, 3))
crate(1.40, 1.30, 0.0, 0.17)
crate(1.40, 1.30, 0.17, 0.15, yaw=25)
amphora(-0.90, 1.55)
amphora(-0.74, 1.52)
sack(0.40, 1.20, 0.0, 0.08)
sack(0.55, 1.14, 0.0, 0.075)
cypress(1.50, 1.00, 1.05, 0.12)
bucket(-0.1, 1.2, 0.0, 0.05)
bucket(0.05, 1.28, 0.0, 0.05)

obj = variant("Feuerwache_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}")

export(spacing=6.0)
