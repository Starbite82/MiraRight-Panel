# MiraRight Panel — Hardware Monitor · v2.0.0-preview.6

Windows-x64-Vorschau für **Thermalright FROZEN WARFRAME 360** (getesteter 320-×-320-Controller `87AD:70DB`, PM32/SUB1) und **MiraBox D92 9.2-inch Universal Screen for PC** (1920 × 462, `5548:1011`). Beide Geräte können gleichzeitig mit eigenen Einstellungen laufen, ohne MiraBox Craft oder Thermalright TRCC.

## Neu

- **GPU-Details auf der MiraBox:** GPU-Takt und Speichertemperatur als Zahlen, darunter drei Balken für **3D, Video Decode und Video Encode**. Zahlen und Balken verwenden ihre jeweiligen Glättungseinstellungen.
- Die Details werden der ausgewählten Grafikkarte zugeordnet. Fehlende Sensoren bleiben `—`; echte 0-%-Werte werden angezeigt. Mehrere Sensoren derselben Engine-Art werden über den höchsten Wert zusammengefasst. Ein allgemeiner Video-Engine-Wert wird nicht als getrennte Decode-/Encode-Messung ausgegeben.
- **Watt neben Celsius:** CPU- und GPU-Leistungsaufnahme stehen neben den Temperaturen. Der frei gewordene Platz wird für ein größeres CPU-Thread-Raster genutzt; im MiraBox-Panorama wächst dessen Bereich von 68 auf 140 Pixel Höhe.
- **RAM ohne leere Wattangabe:** Die Leistungszeile wird ausgeblendet, wenn kein gültiger Wert vorliegt. CPU-Speichercontroller-Leistung wird nicht automatisch als DIMM-Leistung verwendet.
- Anleitung und Vorschaubilder mit festen Beispielwerten aktualisiert.

Debug-Log, fensterloser Sensorprozess, Sensor-Wiederverbindung über die eingerichtete Aufgabe und getrennte Displayprofile bleiben enthalten. Welche GPU-Details verfügbar sind, hängt von Hardware, Treiber und Sensorquelle ab. Fehlt eine getrennte Video-Engine-Messung, wird dafür keine Auslastung geschätzt.

## Download und Start

**MiraRight-Panel-v2.0.0-preview.6-windows-x64.zip** vollständig entpacken und **Start-MiraRight-Panel.cmd** starten. .NET-Laufzeit, Sensorbibliotheken, PresentMon, Lizenzen und zugehörige Quellen liegen bei. Die `.zip.sha256` enthält die Prüfsumme. Die Anwendung ist nicht codesigniert.

Der passende WinUSB-Treiber für den unterstützten Thermalright-Controller wird vorausgesetzt; MiraBox verwendet Windows HID. Dieses Paket installiert keine Treiber. Andere Warframe-/SE-/Pro-Varianten und abweichende Controller sind nicht pauschal unterstützt. **MiraRight-Panel-Vorschau.cmd** öffnet eine Vorschau ohne USB-Ausgabe und ohne UAC-Anforderung.

## Update und Backup

1. Das Panel über **Beenden** im Infobereich schließen und den bisherigen Ordner mit Einstellungen sichern.
2. Neues Paket separat entpacken und die eigene `displays.json` übernehmen. Bei einem Ordnerwechsel Desktop-Verknüpfung und Windows-Autostart auf den neuen Pfad umstellen.
3. Bei eingerichteter Sensoraufgabe **Install-SensorAutostart.ps1 aus dem neuen Paket erneut als Administrator ausführen**, damit der geschützte Sensorprozess ebenfalls aktualisiert wird. Die vorherige Sensorinstallation bleibt als Backup erhalten. Details stehen in **SENSOR-AUTOSTART.md**.

**v2.0.0-preview.5 bleibt unverändert als bisheriger Download erhalten.** Ohne eingerichtete Sensoraufgabe kann der Start mit erweiterten Sensorrechten weiterhin eine UAC-Bestätigung benötigen. Die optionale Aufgabe verwendet das eigene Windows-Administratorkonto und speichert keine Kennwörter.

## Validierung

Build und Regressionstests; Zuordnung zu einer von mehreren GPUs; getrennte Video-Engines und Mehrfach-Engine-Zusammenfassung; fehlende und Null-Werte; Glättung der neuen Zahlen und Balken; visuelle Prüfung beider MiraBox-Designs. Der aktuelle GPU-Stand wurde außerdem lokal mit erhaltenen Einstellungen und Sensor-Autostart installiert; beide Displays sind verbunden. Ein vollständiger Windows-Neustart war nicht Teil dieser Prüfung.

Unabhängiges Community-Projekt. Nutzung, Datenschutz, Impressum und Lizenztexte liegen dem Paket bei.
