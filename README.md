# AIO Display 1.1.0

Windows application for the confirmed Thermalright 320 × 320 LCD (87AD:70DB / PM32 SUB1). The existing dashboard, detail view, colors, percentages, thread bars, fan labels and smoothing are retained.

![Icon](assets/App-preview.png)

## Projects

- **AioDisplay.App**: WinForms settings/tray, existing renderer, averaging and WinUSB output.
- **AioDisplay.Contracts**: versioned JSON messages and shared sensor types; no hardware or UI dependencies.
- **AioDisplay.SensorHost**: separate C# process with pinned LibreHardwareMonitor 0.9.6, optional read-only Fan Control IPC and Afterburner shared memory.
- **AioDisplay.Tests / FakeSensorHost**: renderer/settings regression tests, protocol validation, crashes, stale streams and parent process exit.

No PowerShell is started by the application. Full snapshots travel through redirected stdout, configuration through stdin, diagnostics through stderr. File snapshots are no longer used for sensor transport. Fan Control polls independently of the fast LHM loop.

The app restarts a silent or invalid producer and hides stale readings. A valid response with unavailable sensors keeps the process alive, avoiding restart loops. Child ownership is checked by process instance; the host also monitors parent PID and start time.

## Anleitung

Die vollständige deutsche [Anleitung](LIESMICH.txt) liegt auch direkt im Download-ZIP. Sie beschreibt ersten Start, Sensoren, Einstellungen, Autostart, Updates, Backup und Fehlerhilfe.

## Build and test

Install the SDK specified in global.json.

    dotnet build AioDisplay.sln -c Release -p:RestoreLockedMode=true
    ./tests/run.ps1
    ./tests/run.ps1 -Integration -LiveSeconds 240
    ./tools/package.ps1

The PowerShell scripts above are development conveniences; the application and sensor process run directly as EXEs. Self-contained packages include their runtimes and the LHM library, so users do not need PowerShell, .NET or an installed LHM application. The app and sensor runtimes are published separately to avoid shared dependency collisions. Package restore uses committed lock files.

## Running and preview

Use **AIO-Display-Vorschau.cmd** in the native package. This opens a live preview without USB access. Preview has separate single-instance/show/stop signals and disables autostart changes.

The native version has been deployed with minimized autostart. The previous application remains available as a separate local backup. The native default launch shares the production single-instance name and therefore opens an existing production instance instead of competing for the LCD.

The package must be extracted into a writable directory. settings.ini remains compatible with the existing layout and rate settings; obsolete PowerShell/LHM-library paths are ignored. Copy settings.ini manually only after exiting the target application.

## Sensors and compatibility

CPU/GPU/RAM values come from bundled LHM. Fan Control is optional, must be running for its RPM and CPU-temperature fallback, and is accessed through the installed FanControl.IPC.dll (not redistributed). Its portable folder may be selected under Hardware & Pfade. Tested IPC: Fan Control v284. Afterburner remains optional and must provide MAHM shared memory.

The existing LCD model/protocol restriction and WinUSB driver requirement remain. No drivers or fan curves are installed or changed. More hardware support still requires physical testing.

A clean Windows install, other vendors, suspend/resume and physical USB disconnects need separate device testing. Updates are installed into a separate directory with the existing settings; keep the previous directory as a backup.

## Validation and history

42 deterministic images were generated from the stable v1.0.0 renderer and compared byte-for-byte with the native build; all matched. References are recorded in tests/golden-reference.sha256.json. Existing hardware-count, fixed-percent, smoothing and rotation regressions remain.

See [migration](docs/NATIVE-MIGRATION.md), [previous stability test](docs/STABILITY-2026-10-30.md) and [previous hardware adaptation](docs/HARDWARE-2026-10-30.md). Historical source/binaries in Archiv-* are retained as archives and are neither built nor packaged.

Bundled third-party metadata and license notices are generated from restored packages. The LHM source link and MPL-2.0 text are included; .NET runtime notices remain in their publish folders.

Final migration test evidence: [NATIVE-VALIDATION-2026-10-30.md](docs/NATIVE-VALIDATION-2026-10-30.md).
