# AIO Display

Eigenständige Windows-Anwendung für das Thermalright Frozen Warframe Pro LCD mit 320 × 320 Pixeln. Direkter Zugriff über den vorhandenen WinUSB-Treiber; TRCC bleibt geschlossen.

![App-Symbol](App-preview.png)

## Funktionen

- CPU-/GPU-Temperatur und Gesamtlast aus MSI Afterburner
- 32 Threadbalken, die bei höherer Last röter werden
- RAM belegt/gesamt
- Lüfternamen und RPM aus Fan Control, optional ohne stehende Lüfter
- Erhaltene CPU-Detailansicht und vier alternative Balkendarstellungen
- Infobereich, gespeicherte Einstellungen und minimierter Windows-Autostart

## Start

BLUB-Display.exe doppelklicken. Läuft die Anwendung bereits, öffnet sich das Einstellungsfenster der bestehenden Instanz. MSI Afterburner und Fan Control müssen für die jeweiligen Livewerte laufen. TRCC muss geschlossen bleiben.

Autostart und Verhalten beim Schließen werden in den Einstellungen aktiviert. Der Autostart gilt pro Windows-Benutzer und verwendet --minimized. Ein Git-Checkout verändert den Autostart nicht.

## Voraussetzungen

Windows x64 mit .NET Framework, bestätigtes Display 87AD:70DB / PM 32 / SUB 1 und bestehender WinUSB-Treiber. Die Fan-Control-Brücke nutzt die installierten Fan-Control-IPC-Bibliotheken sowie PowerShell 7 mit .NET 10. Derzeit werden die üblichen lokalen Installationspfade verwendet. Die Anwendung ersetzt keine Treiber und ändert keine Lüfterkurven.

## Entwicklung

    .\build.ps1
    .\tests\run.ps1

Der Build verwendet den in Windows enthaltenen .NET-Framework-C#-Compiler. Vor einem Build am gleichen Ziel muss die laufende EXE beendet werden: BLUB-Display.exe --stop.

Tests laufen isoliert unter work/ und verändern weder den Autostart noch die reale Lüfterkonfiguration. tools/BuildIcon.cs und App.svg enthalten die Icon-Quellen; tools/build-icon.ps1 erzeugt das Windows-Icon in neun Größen neu.

## Dateien

- Program.cs: Einstellungen, Infobereich, Einzelinstanz und Übertragung
- PanelRenderer.cs: Dashboard und erhaltene CPU-Ansichten
- UsbDisplay.cs: bestätigtes USB-LCD-Protokoll
- Afterburner.cs: lesender MAHM-Shared-Memory-Zugriff
- FanControlBridge.ps1 und FanData.cs: lesender Fan-Control-Zugriff
- Settings.cs und UiTheme.cs: Einstellungen, Autostart, Menüfarben und Icon
- Archiv-Detailansicht/ und Archiv-Varianten/: erhaltene frühere Entwicklungsstände

Lokale Einstellungen, Logs, Sensordumps und laufende Vorschaubilder werden nicht versioniert. Die gebaute Hauptanwendung und die erhaltenen Archivstände sind enthalten.

Weitere Bedienhinweise: [LIESMICH.txt](LIESMICH.txt).

## Referenzen

USB-Protokoll und Modellzuordnung wurden anhand der [Bulk-LCD-Implementierung](https://github.com/Lexonight1/thermalright-trcc-linux/blob/main/src/trcc/adapters/device/bulk_lcd.py) und der [Protokollprofile](https://github.com/Lexonight1/thermalright-trcc-linux/blob/main/src/trcc/core/protocol.py) geprüft. Das Afterburner-Datenformat stammt aus dem lokal installierten offiziellen SDK (MAHMSharedMemory.h). Es werden keine fremden Bibliotheken mitgeliefert.
