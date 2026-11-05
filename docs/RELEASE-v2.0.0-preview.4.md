# MiraRight Panel — Hardware Monitor · v2.0.0-preview.4

Windows-x64-Vorschau für **Thermalright FROZEN WARFRAME 360** (getesteter 320-×-320-Controller `87AD:70DB`, PM32/SUB1) und **MiraBox D92 9.2-inch Universal Screen for PC** (1920 × 462, `5548:1011`). Beide Displays können gleichzeitig mit eigenen Einstellungen laufen, ohne MiraBox Craft oder Thermalright TRCC.

## Neu

- Optionaler Sensorstart ohne wiederholte UAC-Abfrage: Eine einmalig als Administrator eingerichtete Windows-Aufgabe startet ausschließlich den separaten Sensorprozess. Die Oberfläche läuft normal, UAC bleibt aktiv und es werden keine Kennwörter gespeichert. Die Sensordateien liegen geschützt unter `Program Files`.
- Mehr Scroll-Abstand am Ende beider Display-Reiter: Auch die letzte Auswahl „RAM · Leistung · Sensor“ bleibt vollständig erreichbar.
- Separat schaltbare Threadnummern auf der MiraBox, sowohl im klassischen Dashboard als auch im Thermalright-Panorama-Design.
- Eindeutige Anzeige der erweiterten Sensorrechte samt Aktivieren-/Deaktivieren-Schalter. Fehlende Mainboard-Lüfter können auf ausgeschaltete Sensorrechte zurückgehen.

## Download und Verwendung

**MiraRight-Panel-v2.0.0-preview.4-windows-x64.zip** vollständig entpacken und **Start-MiraRight-Panel.cmd** starten. Die .NET-Laufzeit, Sensorbibliotheken, PresentMon, Lizenzhinweise und zugehörigen Quellen sind enthalten. Die `.zip.sha256` enthält die Prüfsumme. Die Anwendung ist nicht codesigniert.

Der passende WinUSB-Treiber für den unterstützten Thermalright-Controller wird vorausgesetzt; MiraBox nutzt Windows HID. Der Download installiert keine Treiber. Abweichende Controller oder Warframe-/SE-/Pro-Varianten sind nicht pauschal unterstützt.

Für den optionalen Sensorstart ohne UAC das Panel beenden und **Install-SensorAutostart.ps1** einmal als Administrator mit dem eigenen Windows-Administratorkonto ausführen. Anschließend das Panel neu starten. Anleitung: **SENSOR-AUTOSTART.md** und **LIESMICH.txt**. Ohne diese Einrichtung bleibt die bisherige UAC-Abfrage für aktivierte Sensorrechte bestehen. Die Vorschau fordert keine Rechte an.

## Update und Backups

Die bisherige Version vollständig beenden und ihren Ordner als Backup behalten. Das neue Paket separat entpacken und die eigene `displays.json` übernehmen. Beim Ordnerwechsel Desktop-Verknüpfung und Autostart auf den neuen Ordner umstellen. Eine eingerichtete Sensoraufgabe mit dem Skript aus dem neuen Paket aktualisieren; die alte geschützte Sensorinstallation wird dabei gesichert.

Die bisherige Veröffentlichung **v2.0.0-preview.3** bleibt unverändert abrufbar. Verfügbarkeit einzelner Temperatur-, Leistungs- und FPS-Werte hängt von Hardware, Treibern und Sensorrechten ab. FPS beziehen sich auf die aktive Anwendung, nicht auf die USB-Bildrate.

Validiert: Build und Regressionstests, Layout bei normaler und minimaler Fenstergröße, getrennte Threadoptionen, Sensoraufgabe aus einem nicht erhöhten Prozess, wiederholter minimierter Start, sauberes Sensorende sowie beide verbundenen Displays. Ein kompletter Windows-Neustart wurde für diese Prüfung nicht durchgeführt.

Unabhängiges Community-Projekt, kein offizielles Produkt der genannten Hersteller. Nutzung, Datenschutz, Impressum und Lizenztexte liegen dem Paket bei.
