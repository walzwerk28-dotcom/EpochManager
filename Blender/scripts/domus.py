"""
Domus der Bauern (Antike), 3 x 3 Felder (6 x 6 m), zwei Varianten:
  Variante 1: Atriumhaus: zur Straße zwei Läden (Tabernae) mit Theke und Markise beiderseits des Eingangs; Flügel mit
              Pultdächern, die nach innen zum offenen Atrium mit Wasserbecken (Impluvium) abfallen; hinten ein
              zweistöckiger Hauptbau (Tablinum) mit Satteldach; dahinter ein ummauerter Säulengarten mit Brunnenschale,
              Büschen, Beeten und Bank.
  Variante 0: zweistöckiges Haus in L-Form (ocker, rote Sockelzone) mit Balkon, einem Laden und Seitenflügel; dahinter
              ein ummauerter Garten mit Weinlaube, Beeten, Oliven- und Feigenbaum, Zisterne, Bank und Weg.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/domus.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 5,52 x 5,52 m (Grundfläche 6 x 6 m), Vorderseite nach -Y. Die FBX enthält je Variante ein Objekt.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(81)

# ---------------------------------------------------------------- Variante 1: Atriumhaus (Name wie bisher: 0 Garten, 1 Atrium)

X0, X1 = -2.55, 2.55
YF, YH, YT, YG = -2.35, -1.55, 0.45, 1.15     # Straße, Atriumhalle, Tablinum, Garten
OX0, OX1, OY0, OY1 = -0.90, 0.90, -1.05, -0.05  # Öffnung im Atriumdach
Z_OUT, Z_IN = 1.35, 1.05                        # Dach an den Außenwänden und an der Öffnung
plaster = PLASTER_WHITE
WALL = 0.10

# Vorderhaus mit zwei Läden und niedrigem Obergeschoss
body(X0, X1, YF, YH, 1.75, plaster, socle=SOCLE_RED)
gable_roof(X0, X1, YF, YH, 1.75, "x", plaster, overhang=0.07)
cornice(X0, X1, YF, YH, 1.02)
shop_front("-y", YF, -1.60, w=0.62, goods=("jug", "amphora", "jug"), awning=(CLOTH_RED, CLOTH))
shop_front("-y", YF, 1.60, w=0.62, goods=("bread", "basket", "bread"), awning=(CLOTH_OCHRE, CLOTH))
for u in (-1.60, 1.60):                                       # Ladenbretter neben der Öffnung
    for s in (-1, 1):
        box((0.10, 0.02, 0.62), (u + s * 0.46, YF - 0.05, 0.40), rot=(0, s * 4, 0), material=WOOD)
# Eingang (Fauces): Tür mit Pilastern und kleinem Dreiecksgiebel
door("-y", YF, 0.0, w=0.40, h=0.78)
for s in (-1, 1):
    box((0.08, 0.05, 0.92), (s * 0.30, YF - 0.025, 0.46), material=LIME)
    box((0.11, 0.06, 0.05), (s * 0.30, YF - 0.03, 0.94), material=LIME_DARK)
box((0.74, 0.07, 0.06), (0.0, YF - 0.035, 0.99), material=LIME)                         # Gebälk
slab([(-0.40, 1.02), (0.40, 1.02), (0.0, 1.22)], "xz", YF - 0.06, 0.05, LIME)           # Giebel
for u in (-2.05, -1.15, 1.15, 2.05):                                                    # Fenster im Obergeschoss
    window("-y", YF, u, 1.22, w=0.16, h=0.18, shutters=WOOD, open_shutters=(abs(u) > 2))
window("-y", YF, -0.62, 1.24, w=0.12, h=0.14, shutters=None)
window("-y", YF, 0.62, 1.24, w=0.12, h=0.14, shutters=None)

# Atriumhalle: Außenwände, bemalte Innenwände, Boden, Atriumdach mit Öffnung, vier Säulen, Becken
for x in (X0 + WALL / 2, X1 - WALL / 2):
    box((WALL, YT - YH, Z_OUT), (x, (YH + YT) / 2, Z_OUT / 2), material=plaster)
box((X1 - X0 + 0.04, 0.03, 0.09), (0.0, YT, 0.045), material=STONE)
for x in (X0, X1):
    box((0.03, YT - YH, 0.09), (x, (YH + YT) / 2, 0.045), material=STONE)
    box((0.012, YT - YH, 0.18), (x + (-0.006 if x < 0 else 0.006), (YH + YT) / 2, 0.18), material=SOCLE_RED)
    box((0.04, YT - YH + 0.04, 0.05), (x + (-0.02 if x < 0 else 0.02), (YH + YT) / 2, Z_OUT - 0.04), material=LIME_DARK)
box((X1 - X0 - 2 * WALL, YT - YH, 0.03), (0.0, (YH + YT) / 2, 0.025), material=LIME_DARK)        # Boden
# Innenwände pompejanisch rot mit gelbem Fries, dunkle Türen zu den Kammern
for (face, plane, lo, hi) in (("+y", YH, X0 + WALL, X1 - WALL), ("-y", YT, X0 + WALL, X1 - WALL),
                              ("+x", X0 + WALL, YH, YT), ("-x", X1 - WALL, YH, YT)):
    at, size, _, _ = face_frame(face, plane)
    mid, span = (lo + hi) / 2, hi - lo
    box(size(span, 0.008, Z_OUT - 0.10), at(mid, 0.004, 0.05 + (Z_OUT - 0.10) / 2), material=SOCLE_RED)
    box(size(span, 0.010, 0.07), at(mid, 0.006, Z_OUT - 0.18), material=CLOTH_OCHRE)
    box(size(span, 0.010, 0.14), at(mid, 0.006, 0.12), material=DARK)
for u in (-1.70, -0.95, 0.95, 1.70):
    box((0.30, 0.012, 0.66), (u, YH + 0.012, 0.38), material=DARK)                    # Läden und Kammern zur Halle
box((0.40, 0.012, 0.76), (0.0, YH + 0.012, 0.43), material=DARK)                       # Fauces
for face, plane in (("+x", X0 + WALL), ("-x", X1 - WALL)):
    for u in (-1.05, -0.05):
        at, size, _, _ = face_frame(face, plane)
        box(size(0.30, 0.012, 0.66), at(u, 0.012, 0.38), material=DARK)
box((1.20, 0.012, 0.92), (0.0, YT - 0.012, 0.50), material=DARK)                       # Tablinum, breit offen
box((1.30, 0.04, 0.07), (0.0, YT - 0.02, 0.98), material=WOOD_DARK)
atrium_roof(X0, X1, YH, YT, OX0, OX1, OY0, OY1, Z_OUT, Z_IN)
for x, y in ((OX0, OY0), (OX1, OY0), (OX1, OY1), (OX0, OY1)):
    column(x, y, Z_IN - 0.04, r=0.04)
impluvium((OX0 + OX1) / 2, (OY0 + OY1) / 2, OX1 - OX0 - 0.10, OY1 - OY0 - 0.10)
box((0.36, 0.16, 0.04), (0.0, YT - 0.34, 0.30), material=LIME)                         # Marmortisch
for x in (-0.13, 0.13):
    box((0.05, 0.11, 0.28), (x, YT - 0.34, 0.14), material=LIME_DARK)
for x in (-1.40, 1.40):
    pot_plant(x, (OY0 + OY1) / 2, 0.06, OLIVE_LEAF)
# kleine Fenster außen an den Seiten der Halle
for face, plane in (("-x", X0), ("+x", X1)):
    for u in (-1.05, -0.05):
        window(face, plane, u, 0.78, w=0.12, h=0.16, shutters=None)

# Tablinum: ein hohes Stockwerk mit Satteldach
body(X0, X1, YT, YG, 1.55, plaster, socle=SOCLE_RED)
gable_roof(X0, X1, YT, YG, 1.55, "x", plaster, overhang=0.07)
for face, plane in (("-x", X0), ("+x", X1)):
    window(face, plane, (YT + YG) / 2, 0.55, shutters=WOOD)
door("+y", YG, 0.0, w=0.44, h=0.80)
for u in (-1.60, 1.60):
    window("+y", YG, u, 0.50, shutters=WOOD, open_shutters=False)

# Peristyl: Garten mit Säulengang an drei Seiten, Brunnen, Statue, Hecken, Bank
GY1 = 2.62
court_wall([(X0, YG), (X0, GY1), (X1, GY1), (X1, YG)], h=0.50, plaster=plaster)
PX0, PX1, PY1 = X0 + 0.45, X1 - 0.45, GY1 - 0.42                  # Säulenreihen
# offener Säulengang an drei Seiten (Säulen mit Gebälk, darüber Holzlatten mit Rankpflanzen)
colonnade((PX0, YG + 0.25), (PX0, PY1), 3, 0.80)
colonnade((PX1, YG + 0.25), (PX1, PY1), 3, 0.80)
colonnade((PX0, PY1), (PX1, PY1), 6, 0.80)
for k in range(9):
    y = YG + 0.25 + k * (PY1 - YG - 0.25) / 8
    box((PX0 - X0 - 0.02, 0.03, 0.03), ((X0 + PX0) / 2, y, 0.88), material=WOOD)
    box((X1 - PX1 - 0.02, 0.03, 0.03), ((X1 + PX1) / 2, y, 0.88), material=WOOD)
for k in range(13):
    x = PX0 + k * (PX1 - PX0) / 12
    box((0.03, GY1 - PY1 - 0.02, 0.03), (x, (PY1 + GY1) / 2, 0.88), material=WOOD)
for x, y in ((PX0 - 0.20, YG + 0.6), (PX1 + 0.20, YG + 0.9), (-0.6, PY1 + 0.20), (1.3, PY1 + 0.20)):
    sphere(0.14, (x, y, 0.95), (1.0, 1.0, 0.4), LEAF if x < 0 else LEAF_DARK, segs=6, rings=4)
fountain_bowl(0.0, (YG + PY1) / 2 + 0.05, 0.15)
box((0.14, 0.14, 0.22), (-1.25, PY1 - 0.20, 0.11), material=LIME_DARK)                # Statue auf Sockel
cyl(0.045, 0.30, (-1.25, PY1 - 0.20, 0.37), material=LIME, verts=8)
sphere(0.045, (-1.25, PY1 - 0.20, 0.56), material=LIME, segs=8, rings=5)
for (bx0, bx1) in ((-1.55, -0.55), (0.55, 1.55)):                                      # Beete mit Hecken
    veg_bed(bx0, bx1, YG + 0.20, YG + 0.62, rows_along="x", plant=LEAF)
    box((bx1 - bx0, 0.08, 0.12), ((bx0 + bx1) / 2, YG + 0.14, 0.06), material=LEAF_DARK)
bench(1.20, PY1 - 0.18, length=0.40)
amphora(-2.35, -2.50)
variant("Domus_1")

# ---------------------------------------------------------------- Variante 0: L-Haus mit großem Garten

X0, X1 = -2.55, 2.55
YF, YB = -2.35, -1.15
plaster = PLASTER_OCHRE
body(X0, X1, YF, YB, 2 * STOREY, plaster, socle=SOCLE_RED)
gable_roof(X0, X1, YF, YB, 2 * STOREY, "x", plaster, overhang=0.08)
cornice(X0, X1, YF, YB, STOREY)
# Seitenflügel rechts, zweistöckig, First quer
SX0 = 1.35
body(SX0, X1, YB, 0.55, 2 * STOREY, plaster, socle=SOCLE_RED)
gable_roof(SX0, X1, YB - 0.4, 0.55, 2 * STOREY, "y", plaster, overhang=0.08)
cornice(SX0, X1, YB, 0.55, STOREY)
# Straßenseite
door("-y", YF, -0.70)
shop_front("-y", YF, 1.60, w=0.66, goods=("jug", "jug", "amphora", "basket"), awning=(PAINT_BLUE, CLOTH))
for u in (-2.0, 0.30):
    window("-y", YF, u, 0.42, shutters=PAINT_GREEN)
balcony(YF, -2.20, -0.30, STOREY + 0.06, depth=0.20)
door("-y", YF, -1.25, w=0.30, h=0.55, z0=STOREY + 0.06)
for u in (0.25, 1.05, 1.85):
    window("-y", YF, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=(u != 1.05))
window("-x", X0, -1.95, 0.42, shutters=PAINT_GREEN)
window("-x", X0, -1.95, STOREY + 0.38, shutters=PAINT_GREEN)
for u in (-1.95, -0.85, 0.05):
    window("+x", X1, u, 0.42, shutters=PAINT_GREEN, open_shutters=(u != -0.85))
    window("+x", X1, u, STOREY + 0.38, shutters=PAINT_GREEN)
# Gartenseite
door("+y", YB, -0.70)
for u in (-2.0, 0.40):
    window("+y", YB, u, 0.42, shutters=PAINT_GREEN)
    window("+y", YB, u, STOREY + 0.38, shutters=PAINT_GREEN, open_shutters=False)
door("-x", SX0, -0.40)
window("-x", SX0, 0.20, STOREY + 0.38, shutters=PAINT_GREEN)
window("+y", 0.55, 1.95, STOREY + 0.38, shutters=PAINT_GREEN)
# Garten mit Mauer
court_wall([(X0 - 0.05, YB), (X0 - 0.05, 2.66), (X1 + 0.05, 2.66), (X1 + 0.05, 0.55)], plaster=plaster)
path([(-0.70, -1.25), (-0.30, -0.10), (0.20, 0.90)])
pergola(-2.30, -1.05, -1.00, 0.05, h=0.86, vine_post=1)
bench(-1.67, -0.75, length=0.46)
cistern(0.55, -0.55)
veg_bed(-2.30, -0.80, 0.95, 2.45, rows_along="y")
olive_tree(0.55, 1.75, 1.05)
fig_tree(1.85, 1.40, 0.95)
bench(1.70, 2.35, length=0.42)
for x, y in ((1.20, 0.75), (1.32, 0.80)):
    amphora(x, y)
woodpile(-0.05, 2.40, length=0.38, rows=(4, 3, 2), yaw=0)
variant("Domus_0")

export(spacing=6.6)
