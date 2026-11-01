# Änderungen

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
