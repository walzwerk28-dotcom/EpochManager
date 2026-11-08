"""
Brunnen (Antike), 1 x 1 Feld (2 x 2 m): öffentlicher Laufbrunnen wie in Pompeji an einer Straßenecke.
Rechteckiger Trog aus vier Kalksteinplatten (Eisenklammern an den Fugen) mit Wasserfläche; hinten der aufrechte
Brunnenstein mit Giebelchen und einem Reliefkopf (Maske), aus dessen Mund durch ein Bronzerohr ein gebogener Wasserstrahl
in den Trog fällt (Spritzer am Auftreffpunkt). Davor ein abgetretener Stein, Krug auf dem Trogrand, Amphoren, Eimer und
Krug am Boden; das Pflaster um den Trog ist abgenutzt, dunkler und nass.

Aufruf (ohne Fenster):
  blender --background --python Blender/scripts/brunnen.py -- <blend> <fbx> [<vorschau.png>]

Alles innerhalb von 1,70 x 1,70 m (Grundfläche 2 x 2 m), Vorderseite (Straße, Zugang zum Trog) nach -Y.
Die FBX enthält ein Objekt (Brunnen_0). Bei einer Vorschau entstehen: <name>.png, _nah, _hinten, _spiel.
"""
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from roemisch import *  # noqa: E402,F401,F403

random.seed(5)

TROUGH_H = 0.36
WATER_Z = 0.315
PIL_Y = 0.46                      # Mitte des Brunnensteins

# ---------------------------------------------------------------- abgenutztes Pflaster um den Trog
WET = mat("PflasterNass", (0.30, 0.30, 0.31), rough=0.35)
rnd = random.Random(12)
y = -0.80
while y < 0.80:
    row_h = rnd.uniform(0.17, 0.22)
    row_h = 0.80 - y if 0.80 - y < row_h + 0.08 else row_h
    x = -0.84
    while x < 0.84:
        w = rnd.uniform(0.16, 0.24)
        w = 0.84 - x if 0.84 - x < w + 0.08 else w
        cx, cy = x + w / 2, y + row_h / 2
        inside_trough = abs(cx) < 0.70 and -0.30 < cy < 0.30
        inside_pillar = abs(cx) < 0.26 and 0.28 < cy < 0.66
        if not inside_trough and not inside_pillar:
            near = abs(cx) < 0.72 and cy < 0.0            # vor dem Trog: Stufe, am stärksten abgetreten und nass
            m = WET if (near and cy < -0.28 and abs(cx) < 0.5) else (PAVE_WORN if rnd.random() < 0.7 else ROCK_C)
            box((w - 0.015, row_h - 0.015, 0.014), (cx, cy, 0.021), material=m)
        x += w
    y += row_h

# ---------------------------------------------------------------- Trog aus vier Platten
box((1.22, 0.54, 0.06), (0, 0, 0.03), material=LIME_DARK)                                # Boden
box((1.24, 0.095, TROUGH_H), (0, -0.2175, TROUGH_H / 2), material=LIME)                  # Vorderplatte
box((1.24, 0.095, TROUGH_H), (0, 0.2175, TROUGH_H / 2), material=LIME)                   # Hinterplatte
for s in (-1, 1):
    box((0.095, 0.34, TROUGH_H), (s * 0.5725, 0, TROUGH_H / 2), material=LIME_DARK)      # Stirnplatten
    box((0.115, 0.58, 0.035), (s * 0.5725, 0, TROUGH_H + 0.0175), material=LIME)         # Deckplatten der Enden
box((1.24, 0.115, 0.035), (0, -0.2175, TROUGH_H + 0.0175), material=LIME)               # Deckleisten der Längsseiten
box((1.24, 0.115, 0.035), (0, 0.2175, TROUGH_H + 0.0175), material=LIME)
box((1.30, 0.62, 0.03), (0, 0, 0.015), material=LIME_DARK)                               # Fußplatte
box((1.0, 0.34, 0.012), (0, 0, WATER_Z), material=WATER)
for s in (-1, 1):                                                                         # Eisenklammern an den Fugen
    for ys in (-0.17, 0.17):
        box((0.13, 0.025, 0.014), (s * 0.55, ys, TROUGH_H + 0.042), material=IRON)
        box((0.045, 0.045, 0.016), (s * 0.45, ys, TROUGH_H + 0.043), material=IRON)
        box((0.045, 0.045, 0.016), (s * 0.65, ys, TROUGH_H + 0.043), material=IRON)
for k in range(3):                                                                        # abgetretene Mulden am Rand
    box((0.16, 0.05, 0.006), (-0.30 + k * 0.22, -0.2175, TROUGH_H + 0.037), material=LIME_DARK)
# abgetretener Stein vor dem Trog
box((0.62, 0.20, 0.07), (0.0, -0.40, 0.045), material=LIME_DARK)
box((0.55, 0.16, 0.012), (0.0, -0.40, 0.086), material=PAVE_WORN)

# ---------------------------------------------------------------- Brunnenstein mit Reliefkopf
box((0.38, 0.32, 0.08), (0, PIL_Y, 0.04), material=LIME_DARK)
box((0.34, 0.28, 0.04), (0, PIL_Y, 0.10), material=LIME)
box((0.26, 0.20, 0.64), (0, PIL_Y, 0.12 + 0.32), material=LIME)
box((0.22, 0.012, 0.34), (0, PIL_Y - 0.106, 0.54), material=LIME_DARK)                   # Feld um den Kopf
for s in (-1, 1):
    box((0.025, 0.02, 0.38), (s * 0.115, PIL_Y - 0.106, 0.54), material=LIME)
box((0.26, 0.02, 0.025), (0, PIL_Y - 0.106, 0.735), material=LIME)
box((0.32, 0.26, 0.04), (0, PIL_Y, 0.78), material=LIME_DARK)                            # Gesims
slab([(-0.15, 0.80), (0.15, 0.80), (0.0, 0.89)], "xz", PIL_Y - 0.11, 0.22, LIME)           # Giebelchen
box((0.04, 0.02, 0.04), (0, PIL_Y - 0.11, 0.875), material=LIME_DARK)
relief_head(0, PIL_Y - 0.10, 0.52, yaw=0.0, r=0.088)
# Spülrinne unter dem Mund
box((0.07, 0.07, 0.012), (0, PIL_Y - 0.19, 0.4595), material=BRONZE)

# ---------------------------------------------------------------- Wasserstrahl: aus dem Rohr in den Trog
water_jet((0.0, PIL_Y - 0.205, 0.464), (0.0, 0.03, WATER_Z + 0.004), thick=0.024, segs=9)
for k in range(4):                                                                        # Ringe auf der Wasserfläche
    ring(0.0, 0.03, WATER_Z + 0.008, 0.055 + k * 0.045, 0.004, "z", FOAM, 16)

# ---------------------------------------------------------------- Dinge am Brunnen
jug(-0.40, -0.2175, TROUGH_H + 0.035, 0.040)
jug(-0.82, -0.36, 0.0, 0.050)
amphora(0.80, 0.02)
amphora(0.72, 0.20)
bucket(0.66, -0.50, 0.015, 0.055)
pot_plant(-0.75, 0.50, 0.06, leaves=LAUREL)
stone_post(0.75, 0.62, h=0.30)
stone_post(-0.75, -0.65, h=0.30)

obj = variant("Brunnen_0")
xs = [v.co.x for v in obj.data.vertices]
ys = [v.co.y for v in obj.data.vertices]
zs = [v.co.z for v in obj.data.vertices]
print(f"Grenzen x {min(xs):.2f}..{max(xs):.2f}, y {min(ys):.2f}..{max(ys):.2f}, z {min(zs):.2f}..{max(zs):.2f}")

export(spacing=2.4)

if PREVIEW:
    import bpy
    scene = bpy.context.scene
    cam = bpy.data.objects["Kamera"]
    base = os.path.splitext(PREVIEW)[0]

    def shot(name, loc, target, lens=40):
        cam.location = loc
        cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
        cam.data.lens = lens
        scene.render.filepath = base + name + ".png"
        bpy.ops.render.render(write_still=True)

    shot("_nah", (0.9, -1.7, 0.9), (0.0, 0.15, 0.40), lens=45)
    shot("_hinten", (-1.6, 2.0, 1.1), (0.0, 0.0, 0.35), lens=40)
    shot("_spiel", (0.0, -4.6, 4.4), (0.0, 0.0, 0.2), lens=36)
