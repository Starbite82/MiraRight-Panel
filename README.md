# AIO Unified Panel 2.0 Preview

[Impressum / Anbieter und Kontakt](IMPRINT.txt)

## Wofür ist das Programm?

**AIO Unified Panel zeigt PC-Messwerte auf kleinen USB-Hardwaredisplays an.**
Dazu gehören CPU-/GPU-Auslastung und Temperaturen, RAM-/VRAM-Belegung, Lüfterdrehzahlen
und auf dem MiraBox-Layout FPS/Frame Time der aktuell aktiven Anwendung. Verfügbare
Leistungs- und Temperatursensoren lassen sich gezielt auswählen.

Unterstützt werden das getestete **Thermalright-LCD mit 320 × 320 Pixeln** und das
**MiraBox D92 mit 1920 × 462 Pixeln**. Du kannst eines der Geräte oder beide gleichzeitig
verwenden. Jedes Display bekommt eigene Einstellungen. Das Programm ist kein allgemeiner
Treiber für beliebige USB-Bildschirme und steuert weder Lüfter noch Übertaktung.

Die Anzeige läuft ohne MiraBox Craft oder Thermalright TRCC. LibreHardwareMonitor ist
enthalten; MSI Afterburner ist eine optionale alternative Sensorquelle. Die lokale
Anwendung benötigt weder Anmeldung noch Cloudkonto und lädt keine Messwerte hoch.

## Download

Lade das **Windows-x64-Programmpaket** unter [Releases](https://github.com/Starbite82/AIO-Display/releases)
herunter und entpacke das ZIP vollständig. .NET-Laufzeit, Sensorbibliothek und PresentMon sind enthalten.
Die GitHub-Schaltfläche „Code → Download ZIP“ liefert den Quellcode für Entwickler.

## Schnellstart für Nutzer

1. Das passende Windows-x64-Programmpaket vollständig in einen beschreibbaren Ordner entpacken.
2. Andere Programme beenden, die auf dasselbe LCD zugreifen, insbesondere MiraBox Craft oder Thermalright TRCC.
3. **`AIO-Unified-Panel.cmd`** öffnen. Die .NET-Laufzeit und Sensorbibliotheken sind enthalten.
4. Im Reiter **Thermalright** bzw. **MiraBox D92** die angeschlossenen Displays aktivieren;
   den Reiter eines nicht vorhandenen Geräts deaktivieren.
5. Design, Drehung, Sensorquelle und gewünschte Raten pro Gerät einstellen. Änderungen werden gespeichert.

Für einen Blick auf die Oberfläche ohne USB-Ausgabe **`AIO-Display-Vorschau.cmd`** verwenden.
Diese Vorschau fordert keine Administratorrechte an. Fehlende Sensorwerte erscheinen als `—`.

Die ausführliche [deutsche Anleitung](LIESMICH.txt) erklärt Einrichtung, Sensorrechte,
Autostart, Updates, Backups und Fehlerhilfe.

## Gemeinsam und getrennt

Ein eigener Sensorprozess liest Hardware bis zu 10-mal pro Sekunde und betreibt eine gemeinsame PresentMon-FPS-Erfassung.
Beide Anzeigen wählen daraus unabhängig Sensorquelle, GPU und einzelne Sensoren. Jede Anzeige hat eigene
Messwertübernahme, Displayrate, Glättung, Design und Ausrichtung. Ein fehlendes oder blockiertes Gerät
unterbricht die andere Anzeige nicht. Vorschauen bleiben aufrecht.

Für Thermalright stehen sieben Ansichten und eine Drehung um 0/90/180/270° zur Wahl.
Für MiraBox sind klassisches/Panorama-Design, Bereiche/Reihenfolge, Helligkeit und 0/180°-Drehung einstellbar.
RAM/VRAM zeigen benutzt/gesamt; die Lastfarben und stabilen Prozentzeichen bleiben erhalten.

## Unterstützte Displays

| Display | Auflösung | Unterstützter Controller |
| --- | --- | --- |
| Thermalright AIO-LCD | 320 × 320 Pixel | USB `87AD:70DB`, Handshake PM32/SUB1 |
| MiraBox D92 / 9,2-Zoll Universal Screen | 1920 × 462 Pixel | USB `5548:1011` |

**Thermalright Frozen Warframe 360:** Innerhalb der Frozen-Warframe-Reihe gibt es unterschiedliche
LCD-Controller und Auflösungen. Prüfe deshalb die USB-Kennung im Windows-Geräte-Manager.
Ein gleicher Kühlername allein bestätigt die Kompatibilität nicht. Der hier getestete Thermalright-
Controller ist oben angegeben; andere Warframe-/SE-/Pro-Varianten sind nicht pauschal unterstützt.

Das D92 wird direkt über Windows HID angesprochen. Für das Thermalright-LCD wird der bereits
vorhandene passende WinUSB-Treiber benötigt. Die Anwendung installiert keine Treiber.
Eines der unterstützten Displays genügt; zwei gleichzeitig sind optional.

## Messwerte und Rechte

Fehlende Werte erscheinen als `—`; nicht jede Hardware liefert alle Temperaturen oder Wattwerte.
Nur der separate Sensorprozess benötigt optional eine Windows-Administratorbestätigung.
Die Oberfläche läuft normal. Der Vorschaumodus fordert keine Administratorrechte an.
Die ausführliche Anleitung erklärt die Einstellung und die möglichen UAC-Abfragen beim Start.

## Updates und Einstellungen

Einstellungen werden in `displays.json` gespeichert. Für ein Update die Anwendung vollständig
im Tray beenden, den bisherigen Ordner sichern und die Konfiguration in den neuen Ordner übernehmen.
Einrichtung, Autostart und die optionale Übernahme älterer Konfigurationen stehen in der Anleitung.

## Bauen und testen

Windows x64, Git und SDK aus `global.json`:

```powershell
./tests/run.ps1
./tests/run.ps1 -Integration -LiveSeconds 30
./tools/package.ps1 -OutputDirectory ./artifacts
```

Tests prüfen bestehende AIO-Ansichten, Rotation, Einstellungen, IPC und Prozessausfälle sowie getrennte
Profile, gemeinsame GPU-/FPS-Daten, unabhängige Takte und Fehler-/Blockade-Isolation der beiden USB-Ausgaben.
42 Referenzbilder der bisherigen AIO-Ansichten bleiben bytegleich.

Eigener App-/UI-Code GPL-3.0-only; SensorHost, Contracts und FlexPanel.Core MIT.
Die ursprünglichen MIT-Hinweise des FlexPanel-Moduls bleiben erhalten. Vollständiger Umfang: `LICENSE-MAP.txt`.
Der Build enthält Drittanbieterhinweise, passende offene Quellen und Prüfsummen.

Bestätigte Geräte: Thermalright `87AD:70DB`, Handshake PM32/SUB1, und MiraBox D92 `5548:1011`.
Weitere Geräte/Firmware sind nicht pauschal bestätigt. USB-Bilder/s sind Übertragungsraten,
keine zugesicherte physische LCD-Bildwiederholrate. Die App ist nicht codesigniert.

Details für Entwickler und bisherige Hardwareprüfungen stehen unter `docs/`.
