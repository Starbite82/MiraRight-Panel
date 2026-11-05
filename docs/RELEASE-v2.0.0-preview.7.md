# MiraRight Panel — Hardware Monitor · v2.0.0-preview.7

Windows-x64-Vorschau für **Thermalright FROZEN WARFRAME 360** (getesteter 320×320-Controller `87AD:70DB`, PM32/SUB1) und **MiraBox D92 9.2-inch Universal Screen for PC** (1920×462, `5548:1011`). Eines oder beide Displays können mit eigenen Einstellungen verwendet werden.

## Neu im Thermalright-Dashboard

- **CPU-/GPU-Watt neben Celsius:** Leistungsaufnahme jeweils rechts neben der Temperatur; kompaktere Temperaturzahlen für die native 320×320-Fläche.
- **Größere FPS-Anzeige:** FPS der aktiven Anwendung rechts neben der GPU-Prozentlast, mit 14 statt zunächst 10 Punkt Ausgangsschriftgröße. Längere Zahlen werden bei Bedarf eingepasst.
- Die Zusatzwerte verwenden die bereits vorhandenen, pro Display geglätteten Sensor-/PresentMon-Daten. FPS sind keine USB-Bilder/s. Ohne gültige Messung erscheint `— W` bzw. `— FPS`; gemessene Nullwerte bleiben als Null erkennbar.
- RAM/VRAM, Threadraster, Lüfterbereich und feste Prozentpositionen bleiben erhalten. MiraBox-Designs und Sensorprotokoll sind funktional unverändert.

## Download und Start

**MiraRight-Panel-v2.0.0-preview.7-windows-x64.zip** vollständig entpacken und **Start-MiraRight-Panel.cmd** starten. .NET-Laufzeit, Libre-Sensorbibliothek, PresentMon, Lizenztexte, passende Quellen und Prüfsummen sind enthalten. Die zugehörige `.zip.sha256` enthält die ZIP-Prüfsumme. Die Anwendung ist nicht codesigniert.

Vorhandenes WinUSB für den unterstützten Thermalright-Controller wird vorausgesetzt; MiraBox nutzt Windows HID. Keine Treiber- oder Firmwareinstallation. Andere Warframe-/SE-/Pro-Varianten und abweichende Controller sind nicht pauschal bestätigt. **MiraRight-Panel-Vorschau.cmd** öffnet eine Vorschau ohne USB-Ausgabe und ohne UAC-Anforderung.

## Update und Backup

1. Anwendung im Infobereich über **Beenden** schließen und bisherigen Ordner sichern.
2. Neues Paket separat entpacken und eigene **displays.json** sowie gegebenenfalls eigene Lüfter-/Konfigurationsdateien übernehmen. Verweist `FanConfigPath` auf den alten Ordner, die zugehörige Datei ebenfalls übernehmen und den Pfad anpassen. Bei Ordnerwechsel Desktop-Verknüpfung und Windows-Autostart aktualisieren.
3. Bei eingerichteter Sensoraufgabe **Install-SensorAutostart.ps1 aus dem neuen Paket erneut als Administrator ausführen**, damit auch die geschützte Sensorinstallation zum Paketstand passt. Die bisherige Sensorinstallation bleibt als Backup erhalten. Details: **SENSOR-AUTOSTART.md**.

Preview.6 bleibt als bisheriger Download erhalten. Ohne eingerichtete Sensoraufgabe kann für erweiterte Sensorrechte weiterhin eine UAC-Bestätigung nötig sein. Die optionale Aufgabe speichert keine Kennwörter; die Oberfläche bleibt normal gestartet.

## Prüfung und Grenzen

Build und Regressionstests; Zusatzwerte aus dem gemeinsamen Snapshot; fehlende, ungültige, Null- und veraltete Werte; getrennte Pixelbereiche für Watt/FPS ohne Änderung an Speicher-, Thread-, Lüfter- und GPU-Prozentbereichen. Visuelle Kontrolle drei-/vierstelliger FPS und großer Watt-/Temperaturwerte. Die 42 historischen Referenzbilder bleiben bytegleich; das aktuelle Dashboard ergänzt die neuen Werte bewusst.

Die angepasste Oberfläche wurde lokal mit erhaltenen Einstellungen und vorhandener Sensoraufgabe am Thermalright-LCD gestartet; gemeinsame Sensoren und RPM sind aktuell. Ein kompletter Windows-Neustart, neue Hardware und eine umfassende Prüfung einzelner Spiele/FPS-Erfassungen sind nicht Bestandteil dieser Änderung. Fehlende oder vom Treiber nicht angebotene Werte werden nicht geschätzt.

Inoffizielles Community-Projekt. Nutzung, Datenschutz, Impressum und Lizenzinformationen liegen dem Paket bei.
