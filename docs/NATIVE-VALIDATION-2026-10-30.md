# Native migration validation — 2026-10-30

Code commit: 6f94b88. Stable base: 578631e / v1.0.0.

- Build: .NET SDK 10.0.403, Windows x64, no compiler warnings/errors. Fixed NuGet versions and runtime 10.0.13.
- Regression suite: passed. Rotation/RGB565, fixed percentage glyphs, RAM/VRAM, all views, 1–128 threads, 25-fan paging, smoothing and compatible settings.
- Stable vs native rendering: all 42 PNG files match byte-for-byte, including the final namespaced project build. Hashes are committed in tests/golden-reference.sha256.json.
- Process lifecycle: passed on the final 20-second-startup-grace policy. Tested forced crash, frozen producer, invalid messages, hiding stale values, automatic recovery, missing EXE, stopping the owned process, and host termination after parent death.
- Complete physical sensor integration run: 240.03 s, 9.14 Hz, maximum observed interval 419.9 ms, 0 stale observations, no unplanned restart. The stable app, a separate native preview and packaging/build work ran in parallel. An earlier 240-s sensor run reached 9.99 Hz.
- Source selection: both detected AMD GPUs selected explicitly, automatic fallback for missing GPU, operation with absent Fan Control and switching to Afterburner passed. Unsupported Afterburner sentinel readings are filtered before serialization, ANSI strings use the Windows code page.
- Actual machine: Ryzen 9 9950X3D (32 logical threads), Radeon RX 9070 XT plus integrated AMD GPU, eleven Fan Control sensor rows, CPU-temperature fallback from Fan Control v284.
- Self-contained ZIP extracted and preview run. App and host loaded coreclr.dll from their own package directories. No PowerShell runtime children or sensor transport files. Preview reported zero USB frames. Cold startup allowed 20 seconds; once producing, silence timeout is five seconds and display freshness cutoff two seconds.
- Native preview stop uses its own signal. Stable PID 33832 and stable EXE SHA256 15b7734f92ff4d996fd5346ee87e93d627bb72a01d660dc04aee3c4610543add remained unchanged; main worktree stays clean.
- GitHub Windows build, regression tests, self-contained packaging and artifact upload passed: https://github.com/Starbite82/AIO-Display/actions/runs/41041997118 .

The stable application has not been replaced or merged into. No new native USB smoke test was performed on the physical LCD while it was owned by the stable app. Display transport was moved without protocol changes; deterministic output is unchanged. Fresh Windows installations, additional physical vendors and suspend/resume/disconnect scenarios remain separate compatibility work.
