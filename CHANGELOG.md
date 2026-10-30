# Änderungen

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
