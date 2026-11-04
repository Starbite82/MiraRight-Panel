# Änderungen

## 2.0.0-preview.3 — Öffentliche Vorschau

- Datenschutzhinweise für lokalen Betrieb, IONOS-E-Mail-Support und GitHub-Beiträge.
- Datenschutz im Informationsfenster und als Textdatei im Programmpaket erreichbar.
- Direkter Downloadlink zur aktuellen MiraRight-Vorschau.

## 2.0.0-preview.2 — MiraRight Panel

- Gemeinsame Anwendung als „MiraRight Panel — Hardware Monitor“ benannt.
- Vollständige Modellnamen und geprüfte USB-Controller in der Anleitung.
- Vorschauen der Oberfläche, des Thermalright-Dashboards und beider MiraBox-Designs.
- Deutsche Nutzungs-, Gewährleistungs- und Haftungshinweise im Programm und Download.
- Hinweise für Fehlermeldungen, Beiträge und vertrauliche Sicherheitsmeldungen.

## 2.0.0-preview.1 — Zusammenführung

- Thermalright und MiraBox gleichzeitig in einer AIO-Anwendung.
- Ein Sensorprozess und eine FPS-Erfassung; GPU/Quelle/Sensoren je Display.
- Getrennte Messwertübernahme, USB-Raten, Glättung, Designs, Ausrichtung und Status.
- Unabhängige Ausgabeschleifen und Tests für fehlerhafte/blockierte USB-Geräte.
- Aufrechte Vorschauen ohne USB-Zugriff und ohne UAC-Anforderung.
- Alte Einzelprogramme, Einstellungen und Historien als Backups erhalten.
- FlexPanel.Core als eigenständiges MIT-Modul in die GPL-AIO-Oberfläche eingebunden.

## Noch nicht veröffentlicht

- Gespeicherte Display-Drehung um 0°, 90°, 180° oder 270° im Uhrzeigersinn.
- Die Drehung betrifft nur die LCD-Ausgabe; Vorschau und Vorschau-Bild bleiben aufrecht.
- Regressionstests prüfen Drehrichtung, unveränderte Vorschau und gespeicherte Einstellungen.

## 1.1.3 — Direkte Sensoren mit Adminrechten und eigene Lüfternamen

- Optionaler Admin-Sensorprozess; Oberfläche bleibt ohne erhöhte Rechte.
- Abgesicherter lokaler Datenkanal, keine FanControl-DLL im erhöhten Prozess.
- Direkte CPU-Temperatur und Mainboard-RPM, soweit vom Board unterstützt.
- Eigene Lüfternamen, inklusive Übernahme vorhandener FanControl-Namen.
- GPU-RPM bleiben zusammen mit den direkten Mainboard-RPM sichtbar.
- UAC-Abbruch löst keine automatischen Wiederholungen der Abfrage aus.

## 1.1.2 — Lüftersensoren und Hardware-Einstellungen

- Direkte Mainboard-/Controller-RPM über Libre als automatische Ausweichquelle.
- FanControl wird weiterhin verbunden, wenn es erst nach der Anzeige bereit ist.
- Lüfterquelle wird korrekt angezeigt; reine GPU-RPM gelten nicht mehr als aktive FanControl-Verbindung.
- Ordnerauswahl unter „Erweitert“; automatische Erkennung als Standard.

## 1.1.1 — Lizenzbereinigung

- Automatische Programmsuche mit sichtbaren Pfaden im Hardware-Menü; keine separate Libre-Installation nötig.
- GPL-3.0-only für die Windows-App; MIT für unabhängigen SensorHost und Contracts.
- Vollständige Lizenztexte, Geltungsbereiche und SPDX-Kennzeichnungen.
- Explizite .NET-Laufzeit- und Drittanbieterhinweise.
- Passende Projekt-/MPL-/LGPL-Quellen und Buildskripte im Paket.
- Zusätzliche PawnIO-/CrystalDiskInfo-Hinweise, Herkunfts- und Integrationsdokumentation.
- Info-/Impressumsdialog vorbereitet; persönliche Anbieterangaben bleiben offen.


## 1.1.0 — 2026-10-30

- Eigener C#-Sensorprozess ersetzt die PowerShell-Brücken.
- Eigenständiges Windows-ZIP mit .NET-Laufzeiten und LibreHardwareMonitor 0.9.6.
- Saubere Solution mit getrennten Projekten für App, Datenvertrag, Sensorprozess und Tests.
- Versionierte Prozesskommunikation statt Sensordateien; Überwachung und automatische Wiederherstellung.
- Fan Control unabhängig vom schnellen CPU-/GPU-Abfragetakt; Afterburner bleibt optional.
- Bestehende Anzeige, Detailansicht, Farben, Prozentpositionen und Glättung erhalten.
- Getrennte Vorschau, minimierter Autostart und kompatible Einstellungen.
- Vollständige deutsche Anleitung im ZIP sowie Drittanbieterhinweise und Prüfsummen.

Geprüft: 42 identische Referenzbilder, Regressionen, Prozessausfälle, reale Sensoren und anschließender Betrieb am LCD mit 1.971 USB-Frames in 65,64 Sekunden ohne Fehler oder veralteten Status.

## 1.0.0

Erster stabiler Stand mit dynamischer Hardware-Erkennung und PowerShell-Sensorbrücken.
