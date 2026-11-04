# MiraRight Panel — Hardware Monitor · v2.0.0-preview.3

Öffentliche Vorschau für Windows x64: PC-Messwerte auf einem oder beiden unterstützten USB-Displays,
ohne MiraBox Craft oder Thermalright TRCC. Jedes Display hat ein eigenes Profil für Sensoren,
Design, Messwertübernahme, Bildrate, Glättung und Drehung.

## Unterstützte Geräte

- **Thermalright FROZEN WARFRAME 360**, getestete 320-×-320-LCD-Variante mit USB `87AD:70DB`, PM32/SUB1.
- **MiraBox D92 9.2-inch Universal Screen for PC**, 1920 × 462 Pixel, getestetes USB-Gerät `5548:1011`.

Der vollständige Modellname allein bestätigt die Kompatibilität nicht. Bitte die Controllerkennung prüfen;
abweichende Warframe-/SE-/Pro-Varianten sind nicht pauschal unterstützt.

## Download und Start

1. **MiraRight-Panel-v2.0.0-preview.3-windows-x64.zip** vollständig entpacken.
2. Andere Programme mit Zugriff auf dasselbe Display beenden.
3. **Start-MiraRight-Panel.cmd** starten und das angeschlossene Display aktivieren.

Die .NET-Laufzeit, Sensorbibliotheken und PresentMon liegen bei. Der vorhandene passende WinUSB-Treiber
für Thermalright wird vorausgesetzt; MiraBox nutzt Windows HID. Der Download installiert keine Treiber.
**MiraRight-Panel-Vorschau.cmd** zeigt die Oberfläche ohne USB-Ausgabe und ohne UAC-Anforderung.
Zusätzliche Sensorrechte können beim normalen Start eine UAC-Abfrage auslösen.

Die Anwendung ist nicht codesigniert. Die Datei `.zip.sha256` enthält die SHA-256-Prüfsumme des ZIP.
„Source code (zip/tar.gz)“ ist der Quellcode; für die fertige Anwendung das oben benannte Windows-ZIP verwenden.

## Anzeigen und Einstellungen

CPU-/GPU-Auslastung und Temperaturen, CPU-Threads, RAM/VRAM benutzt/gesamt, verfügbare Wattwerte,
Lüfterdrehzahlen und auf dem MiraBox-Layout FPS/Frame Time der aktiven Anwendung.
Fehlende Sensoren erscheinen als `—`. RAM-Temperatur und Leistungswerte sind hardwareabhängig.
Die Software steuert keine Lüfterkurven oder Übertaktung.

Diese Vorschau ergänzt Datenschutzinformationen für den lokalen Betrieb, IONOS-E-Mail-Support
und GitHub-Beiträge. Sie sind in der README, im Download und unter **Info & Impressum → Datenschutz** erreichbar.

## Vorschau und Hinweise

- [Screenshots und kompatible Controller](https://github.com/Starbite82/MiraRight-Panel#vorschau)
- [Anleitung](https://github.com/Starbite82/MiraRight-Panel/blob/main/LIESMICH.txt)
- [Datenschutz](https://github.com/Starbite82/MiraRight-Panel/blob/main/DATENSCHUTZ.txt)
- [Nutzung, Gewährleistung und Haftung](https://github.com/Starbite82/MiraRight-Panel/blob/main/NUTZUNG-UND-HAFTUNG.txt)
- [Impressum](https://github.com/Starbite82/MiraRight-Panel/blob/main/IMPRINT.txt)

Kostenloses, unabhängiges Open-Source-Hobbyprojekt. Eigene App GPL-3.0-only; unabhängige Komponenten
MIT und weitere ausgewiesene Drittanbieterlizenzen. Lizenztexte, Hinweise und passende Quellarchive
liegen im Paket. Gewährleistung und Haftung gelten nach den jeweiligen Lizenzen im gesetzlich
zulässigen Umfang; zwingende Haftung bleibt unberührt. Keine offizielle Verbindung zu den Geräteherstellern.

Vor Updates die Anwendung im Tray vollständig beenden und die Konfiguration sichern.
Die eigene `displays.json` kann in den neuen Programmordner übernommen werden.
