# AIO Display

Eigenständige Windows-Anwendung für das Thermalright Frozen Warframe Pro LCD mit 320 × 320 Pixeln. Direkter Zugriff über den vorhandenen WinUSB-Treiber; TRCC bleibt geschlossen.

![App-Symbol](App-preview.png)

## Funktionen

- CPU-/GPU-Last, GPU-Temperatur, RAM und VRAM direkt aus LibreHardwareMonitor; Sensorquelle im Menü umschaltbar
- CPU-Temperatur bei fehlendem direktem Messwert aus Fan Control
- 32 Threadbalken, die bei höherer Last röter werden
- RAM und VRAM belegt/gesamt
- Displayaktualisierung von 1 bis 10 Bildern/s; die Sensorprogramme bestimmen die Messrate
- Lüfternamen und RPM aus Fan Control, optional ohne stehende Lüfter
- Erhaltene CPU-Detailansicht und vier alternative Balkendarstellungen
- Infobereich, gespeicherte Einstellungen und minimierter Windows-Autostart

## Start

BLUB-Display.exe doppelklicken. Läuft die Anwendung bereits, öffnet sich das Einstellungsfenster der bestehenden Instanz. Im LHM-Modus ist Afterburner optional. Fan Control liefert Lüfternamen, Drehzahlen und bei fehlendem direktem Messwert die CPU-Temperatur. TRCC muss geschlossen bleiben.

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
- SensorHub.cs und LibreHardwareBridge.ps1: direkte LHM-Abfrage mit eigener Messrate von 1 bis 10 Hz
- Afterburner.cs: alternative MAHM-Sensorquelle und gemeinsames Werteformat
- FanControlBridge.ps1 und FanData.cs: lesender Fan-Control-Zugriff
- Settings.cs und UiTheme.cs: Einstellungen, Autostart, Menüfarben und Icon
- Archiv-Detailansicht/ und Archiv-Varianten/: erhaltene frühere Entwicklungsstände

Lokale Einstellungen, Logs, Sensordumps und laufende Vorschaubilder werden nicht versioniert. Die gebaute Hauptanwendung und die erhaltenen Archivstände sind enthalten.

Weitere Bedienhinweise: [LIESMICH.txt](LIESMICH.txt).

## Referenzen

USB-Protokoll und Modellzuordnung wurden anhand der [Bulk-LCD-Implementierung](https://github.com/Lexonight1/thermalright-trcc-linux/blob/main/src/trcc/adapters/device/bulk_lcd.py) und der [Protokollprofile](https://github.com/Lexonight1/thermalright-trcc-linux/blob/main/src/trcc/core/protocol.py) geprüft. Das Afterburner-Datenformat stammt aus dem lokal installierten offiziellen SDK (MAHMSharedMemory.h). Es werden keine fremden Bibliotheken mitgeliefert.

## Geprüfter Betriebsstand

Die installierte LHM-Bibliothek liefert Data-Sensoren in Bytes; die Brücke normiert diese explizit in MiB. Die diskrete RX 9070 XT wird anhand der VRAM-Kapazität gegenüber der integrierten GPU ausgewählt. CPU-Gesamtlast und alle 32 logischen Threads werden separat zugeordnet.

Die direkte Messrate wurde auf diesem PC mit 10,01 Hz gemessen. Die CPU-Temperatur kommt ohne zusätzliche Rechte hier direkt als 0 °C zurück und wird deshalb durch den gültigen Fan-Control-Wert ersetzt. Diese Quelle wird im Fenster angezeigt. Fan-Control-Werte bleiben bei etwa 1 Hz.

Ein begrenzter USB-Test erreichte rund 64 vollständige Frame-Übertragungen pro Sekunde (typisch 16,7 ms). Das misst nur den Abschluss der Host-Übertragung, nicht die physische LCD-Bildrate. 100 sichtbare Bilder/s werden nicht zugesichert.

Die Bildübertragung läuft in einem eigenen, vom Sensorlesen getrennten Stream.
Ziele: bis 60 vollständige USB-Frames/s; unter Last gemessen rund 60,4 Transfers/s
bei parallel 10,03 LHM-Messungen/s. Dies bestätigt nicht die physische
Panel-Refreshfrequenz. Fehlende Bildinhalte werden nicht interpoliert.
CPU-/GPU-Prozentwerte nutzen drei feste Ziffernzellen und ein feststehendes
Prozentzeichen. Balkenspuren sind heller; RAM/VRAM wechseln ab 80/85/90 Prozent
auf Gelb/Orange/Rot.

## Balkenglättung

Die Einstellung Balkenglättung mittelt nur die gezeichneten Thread-, RAM- und
VRAM-Balken über 0,25/0,5/1/2 Sekunden; Aus zeigt unveränderte Balken.
Standard ist 0,5 Sekunden. Prozentzahlen und Speicherzahlen bleiben Rohwerte.
Identische Sensorframes werden nicht mehrfach gewichtet. Bei fehlenden Daten,
Quellenwechsel und geändertem Mittelungsfenster wird der Verlauf verworfen.

## Zuverlässige Sensorübergabe

Die Sensorbrücken veröffentlichen vollständige Momentaufnahmen atomar über
Windows File.Replace. Leser erlauben ReadWrite/Delete-Sharing, damit das
Ersetzen einer Datei während des Lesens keinen Sensorprozess beendet.
Kurzzeitige Fremdsperren werden wiederholt und notfalls für einen Abtastschritt
übersprungen; der nächste Schritt läuft weiter. Ein LHM-Watchdog startet einen
Prozess mit ausbleibenden Daten erneut. Hilfsprozessfehler und Neustarts stehen
in panel.log, Schreibkonflikte gedrosselt in snapshot-errors.log.

Der Watchdog verwendet erfolgreiche, frische Sensorlesungen als Lebenszeichen,
keine Dateizeitstempelabfrage während eines Austauschs. Kurze Name-/Freigabe-
konflikte beim Öffnen werden ebenfalls abgefangen und erneut versucht.
Regressionstest: tests/SnapshotFilesCheck.cs.
