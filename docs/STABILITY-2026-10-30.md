# Stabiler Stand – 30. Oktober 2026

Der Nutzer hat die reparierte Anzeige am AIO-Display als flüssig bestätigt.

## Geprüftes Profil

- 30 USB-Bildübertragungen pro Sekunde
- Direkte LibreHardwareMonitor-Messungen im 100-ms-Takt
- CPU-Temperatur und Lüfter-RPM aus Fan Control
- Balkenglättung über 500 ms
- Dashboard mit CPU/RAM links, GPU/VRAM rechts, 32 Threadbalken unten links und laufenden Lüftern unten rechts

Dieses Profil steht als settings.example.ini bereit. Die persönliche settings.ini und Live-Daten bleiben aus Git ausgeschlossen.

## Reparatur

Die Sensorbrücken publizieren vollständige Snapshots über File.Replace. Leser erlauben ReadWrite/Delete-Sharing und wiederholen kurze Öffnungskonflikte. Kurzzeitige Schreibsperren beenden den Hilfsprozess nicht mehr. Der Watchdog bewertet frisch gelesene Messungen statt möglicherweise vorübergehend fehlender Dateizeitstempel.

## Validierung

Die Tests für RGB565 und Drehung, Sensorzuordnung und ungültige Werte, Einstellungen und minimierten Start, direkte Sensorquelle und Temperaturergänzung, Balkenmittelung sowie parallele Snapshot-Zugriffe sind bestanden.

Im anschließenden vierminütigen Lauf wurden 2.394 Aktualisierungen beobachtet: 9,97 Hz, kein Sensorneustart, keine veralteten Werte, kein Lesefehler. Die längste beobachtete Aktualisierungspause betrug 373 ms.

Die USB-Übertragungsrate ist keine Messung der physikalischen LCD-Bildwiederholfrequenz.