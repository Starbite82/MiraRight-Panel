# AIO Unified Panel 2.0 Preview

Gemeinsame Windows-Anwendung für **Thermalright 320 × 320** und **MiraBox D92 1920 × 462**.
Beide Displays laufen gleichzeitig mit unabhängigen USB-Verbindungen und Einstellungen.
Diese Entwicklung basiert auf AIO Display; die bisherigen Einzelprogramme bleiben als Backups erhalten.

## Gemeinsam und getrennt

Ein eigener Sensorprozess liest Hardware bis zu 10-mal pro Sekunde und betreibt eine gemeinsame PresentMon-FPS-Erfassung.
Beide Anzeigen wählen daraus unabhängig Sensorquelle, GPU und einzelne Sensoren. Jede Anzeige hat eigene
Messwertübernahme, Displayrate, Glättung, Design und Ausrichtung. Ein fehlendes oder blockiertes Gerät
unterbricht die andere Anzeige nicht. Vorschauen bleiben aufrecht.

Thermalright behält seine sieben AIO-Ansichten und 0/90/180/270°-Drehung.
MiraBox behält klassisches/Panorama-Design, Bereiche/Reihenfolge, Helligkeit und 0/180°-Drehung.
RAM/VRAM zeigen benutzt/gesamt; die Lastfarben und stabilen Prozentzeichen bleiben erhalten.

## Start und Wechsel

ZIP vollständig in einen neuen beschreibbaren Ordner entpacken, `AIO-Unified-Panel.cmd` öffnen.
.NET-Laufzeiten, Sensorbibliothek und das geprüfte PresentMon-Binary sind enthalten.
`AIO-Display-Vorschau.cmd` verwendet reale Sensoren, greift aber auf keines der USB-Displays zu und fordert keine UAC-Rechte an.

Vor dem tatsächlichen Displaybetrieb beide Einzelprogramme im Tray beenden. Sie werden nicht automatisch beendet,
gelöscht oder überschrieben. Ihre aktiven Instanzkennungen verhindern konkurrierende USB-Schreiber.
Beim dauerhaften Wechsel deren alte Autostarts deaktivieren; der gemeinsame Autostart ist eine eigene Option.

`displays.json` speichert zwei getrennte Profile. Beim ersten Start ohne diese Datei können vorhandene
AIO-`settings.ini` und eine ausdrücklich daneben abgelegte FlexPanel-Konfiguration `legacy-flexpanel.json`
übernommen werden. Dabei bleiben die Originaldateien unverändert.

**Sensorrechte:** Nur der separate Sensorprozess benötigt optional eine Windows-Administratorbestätigung.
Die Oberfläche läuft normal. Vorschau fordert diese Rechte nicht an. Der eigene Sensorprozess beendet
beim sauberen Schließen auch seine PresentMon-Sitzung; er installiert keinen Dienst.

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

Die bisherige Projektdokumentation bleibt unter `docs/` als Historie erhalten.
