# MiraRight Panel — Hardware Monitor · v2.0.0-preview.5

Windows-x64-Vorschau für **Thermalright FROZEN WARFRAME 360** (getesteter 320-×-320-Controller `87AD:70DB`, PM32/SUB1) und **MiraBox D92 9.2-inch Universal Screen for PC** (1920 × 462, `5548:1011`). Ein oder beide Displays können unabhängig eingestellt werden, ohne MiraBox Craft oder Thermalright TRCC.

## Neu und verbessert

- **Alle Messwert-Zahlen glättbar:** Prozent, Temperaturen, Watt, Speicherbelegung, Lüfterwerte, FPS und Frame Time. Pro Display Aus bis zwei Sekunden; Standard eine Sekunde. Balkenglättung bleibt separat. Fehlende Werte werden nicht durch alte Werte ersetzt, und beim Anwendungswechsel wird die FPS-Mittelung zurückgesetzt.
- **CPU-/GPU-Leistung optisch angeglichen:** einheitliche Beschriftung, Schriftgröße und Position in beiden MiraBox-Designs.
- **Sensorprozess ohne Konsolenfenster**, auch beim Start über die Windows-Aufgabe.
- **Automatische Wiederverbindung** über eine bereits eingerichtete Sensoraufgabe nach Prozessende oder ausbleibenden gültigen Daten. Diese automatischen Versuche lösen keine neue UAC-Abfrage aus. Die Ursache eines ursprünglichen Hardware-/Treiberstillstands wird dadurch nicht pauschal behoben.
- **Lokales Debug-Log** mit Start-, Verbindungs- und Fehlerereignissen, Datenalter und Anzahl verfügbarer Lüfterwerte. Über „Gemeinsame Sensoren & Start → Debug-Log öffnen“ erreichbar. Begrenzte Dateigröße mit bis zu drei Sicherungen; keine automatische Übermittlung.
- Anleitung, Beispielkonfiguration und Vorschaubilder aktualisiert.

## Download und Start

**MiraRight-Panel-v2.0.0-preview.5-windows-x64.zip** vollständig entpacken und **Start-MiraRight-Panel.cmd** starten. .NET-Laufzeit, Sensorbibliotheken, PresentMon, Lizenzen und zugehörige Quellen sind enthalten. Die `.zip.sha256` enthält die Prüfsumme. Die Anwendung ist nicht codesigniert.

Thermalright benötigt den passenden vorhandenen WinUSB-Treiber; MiraBox nutzt Windows HID. Dieses Paket installiert keine Treiber. Abweichende Controller oder Warframe-/SE-/Pro-Varianten sind nicht pauschal unterstützt. **MiraRight-Panel-Vorschau.cmd** zeigt eine Vorschau ohne USB-Ausgabe und ohne Administratorabfrage.

## Update und Backup

1. Das bisherige Panel über **Beenden** im Infobereich schließen und seinen vollständigen Ordner sichern.
2. Neues Paket separat entpacken und die eigene `displays.json` übernehmen. Sensorzuordnung und Displayprofile bleiben erhalten; eine bisherige Einstellung für Prozentglättung wird als Glättung aller Messwert-Zahlen übernommen.
3. Bei einem Ordnerwechsel Verknüpfung und Windows-Autostart auf den neuen Pfad umstellen.
4. Bei eingerichteter Sensoraufgabe **Install-SensorAutostart.ps1 aus dem neuen Paket erneut als Administrator ausführen**, damit auch der geschützte Sensorprozess aktualisiert wird. Das Skript bewahrt die alte Sensorinstallation als Backup auf. Details: **SENSOR-AUTOSTART.md**.

Ohne eingerichtete Sensoraufgabe bleibt beim Start mit aktivierten Sensorrechten die normale UAC-Abfrage bestehen. Für die optionale Aufgabe wird das eigene Windows-Administratorkonto benötigt; es werden keine Kennwörter gespeichert.

**v2.0.0-preview.4 bleibt unverändert als bisheriger Download erhalten.** Messwertverfügbarkeit hängt weiterhin von Hardware, Treibern und Sensorrechten ab. Bei manueller Sensorauswahl auf das passende Gerät achten: „GPU Package“ der ausgewählten Grafikkarte ist nicht die Leistung eines CPU-Kerns.

## Validierung

Build und Regressionstests; Glättung, fehlende Werte, Sensor-/Anwendungswechsel und unabhängige Balken; Speicherkonsistenz; rotierende und gleichzeitig geschriebene Debug-Protokolle; Einstellungsfenster in normaler und minimaler Größe; Verbindungstrennung und Wiederherstellung über die Sensoraufgabe ohne UAC. Aktualisierte Vorschauen nutzen feste Beispieldaten. Ein vollständiger Windows-Neustart war nicht Bestandteil dieser Prüfung.

Unabhängiges Community-Projekt. Nutzungs-, Datenschutz-, Impressums- und Lizenzinformationen liegen dem Download bei.
