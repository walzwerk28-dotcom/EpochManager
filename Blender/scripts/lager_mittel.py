"""
Mittleres Lager (Antike), 2 x 3 Felder (4 m Front, 6 m tief): zweistöckiger Speicher (Horreum) aus Kalkstein mit
Ziegelbändern wie das kleine Lager, Giebel zur Straße. Unten ein breites Tor mit zwei Bohlenflügeln, oben eine Ladeluke
mit Kranbalken, Rolle und hängendem Sack, darüber ein Rundfenster; Gesims zwischen den Stockwerken, an den Längsseiten je
drei Strebepfeiler, Lüftungsschlitze in beiden Geschossen und kleine Öffnungen im Sockel; davor eine Laderampe mit
Stufen, ein Handkarren unter dem Kran, Fässer, Kisten, Säcke und ein Gestell mit Amphoren.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/lager_mittel.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 3,68 x 5,52 m (Grundfläche 4 x 6 m), Vorderseite nach -Y. Die FBX enthält ein Objekt (LagerMittel_0).
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(101)

X0, X1, Y0, Y1 = -1.30, 1.30, -1.45, 2.45
H = 2.2
body(X0, X1, Y0, Y1, H, LIME)
box((X1 - X0 + 0.06, Y1 - Y0 + 0.06, 0.28), (0.0, (Y0 + Y1) / 2, 0.14), material=LIME_DARK)   # hoher Sockel
gable_roof(X0, X1, Y0, Y1, H, "y", LIME, overhang=0.08, pitch=22)
brick_bands(X0, X1, Y0, Y1, (0.55, 0.95, 1.45, 1.85))
cornice(X0, X1, Y0, Y1, 1.18)
for (x, y) in ((X0, Y0), (X1, Y0), (X0, Y1), (X1, Y1)):                                      # Eckpilaster
    box((0.14, 0.14, H - 0.05), (x, y, (H - 0.05) / 2), material=LIME_DARK)
# Längsseiten: Strebepfeiler, Schlitze in beiden Geschossen, Öffnungen im Sockel
for face, plane in (("-x", X0), ("+x", X1)):
    for u in (-0.45, 0.50, 1.45):
        buttress(face, plane, u, 1.60)
    for u in (-0.95, 0.02, 0.98, 1.95):
        slit(face, plane, u, 0.62)
        slit(face, plane, u, 1.45)
        at, size, _, _ = face_frame(face, plane)
        box(size(0.08, 0.012, 0.07), at(u, 0.035, 0.15), material=DARK)
# Giebelseite zur Straße: Tor, Ladeluke mit Kran, Rundfenster
door("-y", Y0, 0.0, w=0.70, h=0.88, z0=0.28, leaf=WOOD_DARK)
box((0.008, 0.008, 0.84), (0.0, Y0 - 0.026, 0.71), material=WOOD_DARK)                     # Fuge der Torflügel
for a in range(0, 181, 20):                                                                # Ziegelbogen über dem Tor
    t = math.radians(a)
    box((0.07, 0.015, 0.05), (0.42 * math.cos(t), Y0 - 0.008, 1.22 + 0.10 * math.sin(t)), rot=(0, -a * 0.25, 0),
        material=TILE_DARK)
door("-y", Y0, 0.0, w=0.40, h=0.48, z0=1.30, leaf=WOOD)                                    # Ladeluke
hoist(0.0, Y0, 2.02, reach=0.50)
cyl(0.13, 0.01, (0.0, Y0 - 0.005, 2.38), rot=(90, 0, 0), material=TILE_DARK, verts=16)
cyl(0.09, 0.012, (0.0, Y0 - 0.008, 2.38), rot=(90, 0, 0), material=DARK, verts=16)
for u in (-0.85, 0.85):
    slit("-y", Y0, u, 0.62)
    slit("-y", Y0, u, 1.45)
# Rückseite
door("+y", Y1, 0.0, w=0.36, h=0.62, z0=0.28)
for u in (-0.75, 0.75):
    slit("+y", Y1, u, 0.62)
    slit("+y", Y1, u, 1.45)
# Laderampe vor dem Tor
box((1.50, 0.42, 0.27), (0.0, Y0 - 0.21, 0.135), material=LIME_DARK)
box((1.54, 0.46, 0.03), (0.0, Y0 - 0.22, 0.285), material=LIME)
for k, h in enumerate((0.18, 0.09)):
    box((0.45, 0.12, h), (-0.48, Y0 - 0.48 - k * 0.12, h / 2), material=LIME_DARK)
for x, y in ((-0.60, Y0 - 0.18), (-0.48, Y0 - 0.30)):
    sack(x, y, 0.30, 0.06)
barrel(0.58, Y0 - 0.20, 0.30, r=0.07)
# Hof davor: Karren unter dem Kran, Fässer, Kisten, Amphoren
cart(0.12, Y0 - 0.72, yaw=0, load="sacks")
for x, y in ((-1.05, -2.40), (-1.25, -2.20), (-1.05, -2.05)):
    barrel(x, y, r=0.075)
barrel(-1.20, -2.40, 0.0, r=0.075, lying=True, yaw=90)
crate(1.10, -2.30, size=0.20, yaw=6)
crate(1.32, -2.25, size=0.18, yaw=-10)
crate(1.18, -2.28, z0=0.20, size=0.16, yaw=18)
for k in range(4):
    amphora(0.80 + k * 0.13, -1.80)
box((0.60, 0.025, 0.025), (1.0, -1.86, 0.18), material=WOOD_DARK)
for x in (0.72, 1.28):
    box((0.03, 0.03, 0.26), (x, -1.80, 0.13), material=WOOD_DARK)
# Längsseiten: Holz und Säcke an der Wand
woodpile(1.62, 1.95, length=0.36, rows=(3, 2), yaw=90)
sack(-1.58, 2.05, 0.0, 0.06)
sack(-1.62, 2.20, 0.0, 0.055)
variant("LagerMittel_0")

export(spacing=4.2)
