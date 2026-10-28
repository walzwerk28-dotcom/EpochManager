# Epochen-City-Builder (Unity)

## Projekt in einem Satz
3D-City-Builder für den PC, der in der Antike beginnt und bis in die Zukunft reicht. Gebäude behalten ihr Aussehen und ihre Funktion beim Epochenwechsel; neue Epochen und Technologien legen nur Schichten darüber.

## Vorbilder
Spielerische Vorbilder sind **Cities: Skylines** und die **Anno-Reihe**. Sie dienen als Maßstab für Spielgefühl und Bedienung, nicht als Vorlage zum Nachbauen; Namen, Inhalte und Grafik bleiben eigenständig.
- Von Cities: Skylines: Straßenbau und Zonen (Wohnen, Gewerbe), Dienstleistungen mit Reichweite, Overlay-Karten, sichtbarer Verkehr, eine frei bewegliche 3D-Kamera.
- Von Anno: Produktionsketten, Bevölkerungsstufen mit Bedürfnispyramide, Lager mit Straßenanbindung, Regionen mit eigenen Rohstoffen und Handel zwischen ihnen, Forschung und Freischaltungen.
- Eigenes Merkmal: der Epochenwechsel, bei dem alte Gebäude stehen bleiben und weiterarbeiten.
- Bei neuen Mechaniken und bei der Bedienung zuerst prüfen, wie die Vorbilder es lösen, und die Abweichung bewusst begründen.

## Feste Entscheidungen
- Friedlicher Aufbau, keine Gegner. Eine Karte mit wenigen, großen, freischaltbaren Regionen.
- Zuerst Endlos-Modus, später zusätzlicher Modus mit Zielen (Ziele als austauschbare Daten).
- Sechs Epochen: Antike, Mittelalter, Renaissance, Industrialisierung, Moderne, Zukunft. Vertical Slice zuerst mit Antike, Mittelalter, Industrialisierung.
- Bauweise: Zonen für Wohnen und Gewerbe, Einzelplatzierung für Produktion, Dienstleistungen, Wunderbauten, Denkmäler. Jedes Gebäude (selbst gesetzt oder gewachsen) braucht beim Bau ein angrenzendes Straßenfeld. Gebäude werden nie automatisch ersetzt; Zonen füllen sich mit Gebäuden der aktuellen Epoche. Abriss nur durch den Spieler, Denkmalschutz verhindert Abriss.
- Wirtschaft: regionale Lager, Straßenanbindung als Effizienzfaktor (0 bis 100 %), weiche Abfallkurve mit der Entfernung. Verkehr ist sichtbar, aber reine Visualisierung und beeinflusst die Simulation nie.
- Bevölkerung: drei Stufen, Bedürfnispyramide, neue Epochen fügen Bedürfnisse hinzu und entfernen keine. Abstrakt simuliert (Zahlen pro Viertel).
- Geld: eine gemeinsame Stadtkasse, eine interne Währung mit epochenabhängigem Anzeigenamen. Unterhalt alter Gebäude wächst kaum mit den Epochen.
- Technologien: verzweigter Baum mit vier Pfaden (Handel, Kultur, Industrie, Wissenschaft), gemeinsamer Forschungspool, Epochenwechsel über Schlüsseltechnologien und Stadt-Meilensteine.
- Ereignisse: einstellbare Häufigkeit, Warnphase, keine dauerhafte Zerstörung von Gebäuden.
- Wunderbauten sind einmalig pro Spiel.

Das ausführliche Game-Design-Dokument liegt als Doc in Claude (Titel: "Game-Design-Dokument: Epochen-City-Builder").

## Technik
- Unity mit Universal Render Pipeline, neues Input System (Active Input Handling: Input System Package (New)), TextMeshPro, uGUI.
- Namespace: `EpochCity`. Scripts unter `Assets/Scripts/` in den Ordnern `Data`, `Simulation`, `Presentation`.
- **Datengetrieben:** Epochen, Ressourcen, Technologien und Gebäude sind ScriptableObjects. Neue Inhalte und Balancing ohne Codeänderung.
- **Simulation und Optik getrennt:** `Simulation` rechnet mit Zahlen, `Presentation` zeigt sie nur an.

## Vorhandene Scripts
- Data: `EpochDefinition` (mit Bedingungen, um diese Epoche zu erreichen: Schlüsseltechnologien, wie viele davon nötig sind, Gebäude-Meilensteine), `ResourceDefinition` (mit `isCurrency`), `TechnologyDefinition`, `BuildingTypes` (Funktionen, Schichten), `BuildingDefinition` (Grundfunktion plus Schichten durch Technologien).
- Simulation: `GridMap`, `BuildingInstance`, `CityController` (Geld, Vorrat, Technologien, Platzieren und Abriss), `ResourceStock`, `GameClock` (Spielmonate, Geschwindigkeit), `EconomySimulation` (Produktion in festen Schritten, Unterhalt pro Monat).
- Simulation: `ResearchSystem` (gemeinsamer Forschungspool: Forschung ist eine Ressource im Lager und fließt in genau ein gewähltes Projekt; Fortschritt bleibt pro Technologie erhalten; erforschbar ist, was Voraussetzungen erfüllt und dessen Epoche erreicht ist). `CityController` meldet Freischaltungen über das Event `TechnologyUnlocked`.
- Simulation: `EpochProgression` (kennt alle Epochen, prüft die Bedingungen der nächsten, `TryAdvance` stellt nur die Epoche um). `CityController.SetEpoch` löst `EpochChanged` aus; `IsEpochReached` sperrt Gebäude und Technologien späterer Epochen. Technologien früherer Epochen bleiben nachholbar.
- Data: `RoadDefinition` (Straßentyp: Kosten und Unterhalt pro Feld, Qualität, Prefab für ein Feld). `ZoneDefinition` (Zonentyp mit Kachel-Prefab); `BuildingDefinition.zone` legt fest, in welcher Zone ein Gebäude wachsen kann (leer = nur einzeln platzierbar).
- Simulation: Zonen liegen als eigene Ebene im `GridMap` (`ZonedCells`, `CanZone`, `RectCells`). `CityController.SetZone` setzt Zonen kostenlos auf freien Feldern oder entfernt sie (null); Straßen und einzeln gesetzte Gebäude verdrängen die Zone unter sich, Zonengebäude behalten sie. Event `ZonesChanged`.
- Simulation: `CityController.CanPlace` prüft freie Fläche, Epoche und angrenzende Straße (Straßenpflicht für alle Gebäude); `TryPlace` kostet Geld, `TryGrow` lässt Zonengebäude kostenlos nur auf Feldern ihrer Zone wachsen; `HousingCapacity` summiert die Wohnplätze. `ZoneGrowth` lässt in festen Abständen (`growthInterval`, Takt der Spielgeschwindigkeit) auf einem zufälligen freien Zonenfeld mit Straße ein Gebäude aus `zoneBuildings` wachsen: aus der neuesten bereits erreichten Epoche, die für diese Zone ein Gebäude hat. Keine Nachfrage.
- Simulation: Straßen liegen als eigene Ebene im `GridMap` (Gebäude und Straßen schließen sich aus; `LPath` liefert die L-förmige Strecke). `CityController` baut und reißt Straßen ab, führt `RoadUpkeepPerMonth` und berechnet in `RefreshConnections` den Anschluss jedes Gebäudes (`BuildingInstance.Connection`, `ConnectionIssue`), nur bei Änderungen, und meldet das über `ConnectionsChanged`. `EconomySimulation` nutzt `Connection` als Effizienz, zeigt den Grund im Status und rechnet den Straßenunterhalt mit ab.
- Simulation: `ConnectionMap` (Dijkstra über Straßenfelder von jedem Lager aus, Kosten pro Feld = 1 / Straßenqualität). Lager sind Gebäude der Kategorie Storage mit Einzugsweite, Abfallkurve (`AnimationCurve`) und Mindestwert in `BuildingDefinition`. Ergebnis: ohne Straße 0 % „keine Straße“, ohne erreichbares Lager 0 % „kein Lager erreichbar“, innerhalb der Einzugsweite Wert aus der Kurve, darüber Mindestwert „zu weit vom Lager“. Ein Lager braucht selbst nur eine Straße.
- Presentation: `CityCameraController`, `BuildingPlacer` (Maus-Platzierung, Abriss von Gebäuden und Straßen, Straßenmodus mit R: ziehen, L-Vorschau grün/rot; Zonenmodus mit Z/G/U: Rechteck ziehen, Vorschau grün = Feld ändert sich; gemeinsamer `CellGhostPool` für Feld-Vorschauen), `CityHud` (inkl. Straßen-, Zonen- und Overlay-Buttons und Infozeile zum Feld unter der Maus), `ConnectionOverlay` (Taste C: färbt Gebäude und Straßen grün/gelb/rot nach Anbindung über MaterialPropertyBlock, Materialien bleiben unverändert), `SimulationHud` und `ResearchHud` (bauen ihre uGUI zur Laufzeit selbst auf, gemeinsame Helfer in `UiFactory`).
- Editor: `TestSetupGenerator` (Menü *EpochCity → Testaufbau erzeugen*) legt unter `Assets/Content/Test/` an: Epoche Antike (Währung Drachmen), Münzen, Weizen, Forschung, Feld, Markt, Bibliothek (5 Münzen → 5 Forschung pro 5 s), Technologien Bewässerung (Feld ×1,5), Schrift und Münzprägung (setzt Schrift voraus, Markt ×1,25), Epoche Mittelalter (Währung Pfennige; 2 von 3 Antike-Technologien und 1 Bibliothek nötig), Mühle (Mittelalter), Straße Trampelpfad (5 pro Feld, Unterhalt 0,1), Lager (Einzugsweite 20, linear 100 % → 25 %, Taste 5), Zonen Wohnen (grün) und Gewerbe (blau), Zonengebäude Wohnhaus (Antike, 6 Wohnplätze), Laden (Antike, 2 Weizen → 4 Münzen) und Fachwerkhaus (Mittelalter, 10 Wohnplätze; nur zum Testen) sowie Platzhalter-Prefabs. `ZoneGrowth` liegt auf „Simulation“. Mehrfaches Ausführen überschreibt nur die Werte.

## Teststand (2026-10-28, im Play-Modus geprüft)
- Kompiliert ohne Fehler und Warnungen. `SampleScene` ist verdrahtet: Gebäudeleiste mit Feld, Markt, Bibliothek und Mühle, Startepoche Antike, `ResearchSystem` und `EpochProgression` (auf „Simulation“), `ResearchHud` (auf „ui“, zeigt auch Epochenbedingungen und den Wechsel-Button), sichtbarer Boden. Keine Starttechnologien.
- Geprüft: Platzieren mit Kosten und Überlappung, Produktion und Verkauf, Monatsabrechnung, Technologie-Multiplikator, Denkmalschutz verhindert Abriss, HUD-Anzeige.
- Forschung geprüft: Bibliothek liefert 1 Punkt pro Spielsekunde, Bewässerung und Schrift werden erforscht und wirken, Münzprägung ist bis zur Schrift gesperrt, ohne Projekt sammelt sich Forschung im Lager. Nicht geprüft: Fortschritt beim Projektwechsel (im Testaufbau gab es keine Gelegenheit dazu).
- Epochenwechsel geprüft: Mühle in der Antike nicht baubar (ausgegraut), Wechsel erst bei 2/2 Technologien und 1/1 Bibliothek möglich, danach Mittelalter mit Pfennigen, Mühle baubar, alle Antike-Gebäude laufen unverändert weiter, Technologie-Boni bleiben, Münzprägung bleibt nachholbar, „Letzte Epoche erreicht“ wird angezeigt. Bedienung per Klick nicht geprüft.
- Straßen geprüft: L-Strecke korrekt, Strecke durch Gebäude abgelehnt, Kosten nur für neue Felder, Gebäude nicht auf Straßen baubar, ohne Straße 0 % und „keine Straße“, mit Straße 100 % und Produktion, Abriss einzelner Felder trennt den Anschluss, Unterhalt erscheint in der Monatsabrechnung.
- Lager-Anbindung geprüft: ohne Lager „kein Lager erreichbar“; 2 / 10 / 34 Straßenfelder Entfernung ergeben 92,5 / 62,5 / 25 % („zu weit vom Lager“); getrenntes Straßennetz und Gebäude ohne Straße korrekt 0 %; Produktionsrate entspricht den Prozentwerten; Overlay zeigt den Farbverlauf per Screenshot.
- Zonen anlegen geprüft: Rechteck belegt nur freie Felder (Straße, Gebäude, gleiche Zone ausgespart), Umzonen Wohnen → Gewerbe, Aufheben, Bibliothek und Straße verdrängen die Zone, Zonen kosten nichts, Kacheln = Zonenfelder, Darstellung per Screenshot.
- Straßenpflicht geprüft: Feld und Lager ohne Straße abgelehnt (kein Geld abgezogen), nur über Eck ebenfalls abgelehnt, an der Straße gebaut.
- Zonenwachstum geprüft: 10 von 10 Feldern an der Straße bebaut (6 Wohnhäuser, 4 Läden, jeweils richtige Zone), zweite Reihe und Zone ohne Straße bleiben leer, kostenlos, Wohnplätze stimmen; Abriss → gleiches Gebäude wächst nach; im Mittelalter wachsen Fachwerkhäuser, alte Wohnhäuser bleiben, Gewerbe nimmt mangels Mittelalter-Gebäude weiter den Laden; zeitgesteuertes Wachstum füllt neue Zonen; Läden laufen mit Lager-Anbindung oder warten auf Weizen.
- Nicht geprüft: Bedienung mit Maus und Tastatur (Bauen, Straßen und Zonen ziehen samt Vorschau, Abriss-Modus, Infozeile beim Überfahren, Overlay per Taste C, Kamera). Straßen mit höherer Qualität (es gibt nur den Trampelpfad). Im Test wurde per Code platziert.
- Automatisierte Tests gibt es noch nicht.
- Hinweis zum Testen per MCP: Ohne Fokus berechnet Unity im Play-Modus keine Frames. Für die laufende Sitzung `Application.runInBackground = true` setzen (die Projekteinstellung bleibt unverändert).

## Bekannte Lücken
- Lager sind nur Knotenpunkte für die Anbindung, ohne eigene Kapazität oder Bestand; es gibt nur einen Straßentyp.
- Es gibt ein gemeinsames Lager statt regionaler Lager.
- Epochenwechsel bringt noch keine Boni und hat nur Gebäude-Meilensteine (Bevölkerung fehlt).
- Zonenwachstum hat keine Nachfrage (feste Rate) und keine älteren Gebäude in neuen Vierteln. Zonengebäude sind 1 × 1 und brauchen Straßenfront, deshalb bleiben zweite Reihen leer.
- Die untere Bauleiste hat 11 Buttons; bei schmalem Fenster wird die Schrift klein (später Kategorien).
- Keine Bevölkerung, keine Steuern, keine Ereignisse, keine Regionen, kein sichtbarer Verkehr, kein Speichersystem.
- Geld kann ins Minus gehen, Folgen fehlen.
- `GetActiveFunctions` baut pro Simulationsschritt eine neue Liste (später zwischenspeichern).
- Ordner weichen von der Struktur ab: `CityHud`, `SimulationHud` (Presentation) sowie `EconomySimulation`, `GameClock`, `ResourceStock` (Simulation) liegen noch unter `Data/`. Beim Verschieben die `.meta`-Dateien mitnehmen.

## Nächste Schritte (Vorschlag)
**Priorität (entschieden):** Erst die Antike zu einem vollständigen, spielbaren Kern ausbauen, bevor weitere Epochen Inhalte bekommen. Das Mittelalter bleibt vorerst nur Testepoche für den Wechsel; neue Mechaniken werden zuerst mit Antike-Inhalten gebaut und geprüft.

1. ~~Projekt kompilieren und mit einem Testaufbau prüfen~~ (erledigt, siehe Teststand).
2. ~~Forschungssystem und Epochenwechsel mit Meilensteinen~~ (erledigt; Bevölkerungs-Meilensteine und Epochen-Boni folgen mit Schritt 3).
3. Bevölkerung, Bedürfnisse, Zufriedenheit und Steuern.
4. ~~Anbindungswert über Straßen und Lagerhäuser, Overlay-Karte~~ (erledigt). Entschieden: kein automatisches Startlager, regionale Bestände und Lagerkapazität später.
5. ~~Zonen für Wohnen und Gewerbe~~ (erledigt). Entschieden: keine Nachfrage bis zur Bevölkerung, nur Straße nötig (kein Lager), Zonengebäude 1 × 1, kostenlos. (Reihenfolge entschieden: Zonen vor Bevölkerung.)
6. Speichersystem früh einplanen.

## Arbeitsweise
- Antworten und Kommentare auf Deutsch, Code und Bezeichner auf Englisch oder wie im bestehenden Code.
- Kleine, getestete Schritte; Entscheidungen, die das Konzept ändern, vorher mit dem Entwickler abstimmen.
