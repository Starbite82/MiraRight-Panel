# AIO Display

Eigenständige Windows-Anwendung für das Thermalright Frozen Warframe Pro LCD mit 320 × 320 Pixeln. Direkte WinUSB-Übertragung, gespeicherte Einstellungen, Infobereich und minimierter Autostart.

![App-Symbol](App-preview.png)

## Anzeige und Hardware

- CPU/GPU-Temperatur und Last, RAM/VRAM belegt und gesamt.
- Automatische CPU-Threadzahl, anpassende Balkenraster; mehr als 32 Threads wechseln in Seiten alle fünf Sekunden. Detailansicht und Varianten bleiben erhalten.
- Intel-/AMD-CPU-Sensornamen sowie von LibreHardwareMonitor erkannte GPUs werden berücksichtigt. Automatische Auswahl nach gemeldeter VRAM-Kapazität oder ausdrückliche GPU-Auswahl.
- Lüfternamen aus der aktiven Fan-Control-Konfiguration; alle erkannten RPM-Sensoren, Filter für stehende Lüfter, Seiten bei mehr als acht Einträgen.
- Hardware & Pfade: portable Fan-Control-/LHM-Installationen und PowerShell ausführbar auswählen. Leere Pfade nutzen automatische Suche.
- Separate Display- und Sensorraten: bis 60 USB-Übertragungen/s und 10 LHM-Messungen/s. Die physische LCD-Bildrate wurde nicht gemessen. Fan Control liefert ungefähr 1 Hz.
- Feste Prozentpositionen, helle Balkenspuren und zeitbasierte Balkenmittelung. RAM/VRAM gelb ab 80, orange ab 85 und rot ab 90 Prozent.

## Start und Voraussetzungen

Release-ZIP vollständig entpacken und BLUB-Display.exe starten. TRCC muss geschlossen sein. Ein erneuter Start öffnet die Einstellungen der vorhandenen Instanz. Autostart lässt sich im Fenster aktivieren.

Windows x64, .NET Framework 4.x, bestätigtes LCD 87AD:70DB / PM 32 / SUB 1 und vorhandener WinUSB-Treiber. PowerShell 7 und eine vorhandene LibreHardwareMonitorLib.dll mit ihren Abhängigkeiten sind erforderlich. Für die getesteten Fan-Control-v284-/LHM-0.9.6-Bibliotheken wird PowerShell 7.6 mit .NET 10 verwendet. Fremde Laufzeiten und DLLs werden nicht mitgeliefert.

Die automatische Suche berücksichtigt laufende Fan-Control-/LHM-Prozesse, Standardordner und die lokalen Unterordner FanControl, LibreHardwareMonitor und runtime/pwsh.exe. Andere Installationen werden unter Hardware & Pfade gewählt. Fan Control muss für seine RPM- und CPU-Temperatur-Ersatzwerte laufen. Afterburner bleibt eine alternative Sensorquelle; die ausdrückliche GPU-Auswahl gilt im LHM-Modus.

Die CPU-Temperatur bevorzugt gültige Package-/Tctl-Werte, ersatzweise gültige direkte Temperatursensoren oder Fan Control. Fehlende Messwerte erscheinen als --. Ohne Fan-Control-Sensorliste kann ein direkt gelieferter GPU-RPM-Wert angezeigt werden.

Physisch getestet: Ryzen 9 9950X3D, Radeon RX 9070 XT plus integrierte AMD-GPU und das oben genannte LCD. Weitere CPU-/GPU-Konfigurationen werden von der Erkennung berücksichtigt, sind hier jedoch nicht physisch geprüft. Andere LCD-Protokolle sind nicht implementiert.

Weitere Bedien- und Updatehinweise: [LIESMICH.txt](LIESMICH.txt).

## Entwicklung und Prüfung

    .\build.ps1
    .\tests\run.ps1

Vor dem Überschreiben der EXE: BLUB-Display.exe --stop. Der Build verwendet den Windows-.NET-Framework-C#-Compiler. Die Tests laufen unter work/, prüfen unter anderem 1–128 Threads, alle Ansichten, 25 Lüfter mit Seitenschaltung, feste Prozentzeichen, Glättung und konkurrierende Snapshot-Leser/Schreiber. Sie ändern weder Autostart noch Lüfterkonfiguration.

Sensorübergabe: vollständige Snapshots über File.Replace, Leser mit ReadWrite/Delete-Sharing, begrenzte Wiederholungen bei Sperren und ein Watchdog anhand frischer erfolgreicher Lesungen. Die USB-Übertragung läuft in einem separaten Thread. Siehe [Stabilitätsprüfung](docs/STABILITY-2026-10-30.md).

Die alten Darstellungen bleiben in Archiv-Detailansicht/ und Archiv-Varianten/. Einstellungen, Sensordateien, Logs und Vorschauen werden nicht versioniert.

## Quellen

USB-Protokoll: [Bulk-LCD-Implementierung](https://github.com/Lexonight1/thermalright-trcc-linux/blob/main/src/trcc/adapters/device/bulk_lcd.py), [Protokollprofile](https://github.com/Lexonight1/thermalright-trcc-linux/blob/main/src/trcc/core/protocol.py). Afterburner: lokal installiertes offizielles MAHMSharedMemory.h-SDK.

Die Einheitenkonventionen werden anhand des SensorType-Enums unterschieden: [LHM 0.9.4](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/blob/v0.9.4/LibreHardwareMonitorLib/Hardware/ISensor.cs) und [aktuelle Schnittstelle](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/blob/master/LibreHardwareMonitorLib/Hardware/ISensor.cs).
