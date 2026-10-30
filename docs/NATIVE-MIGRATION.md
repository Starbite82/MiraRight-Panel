# Native sensor process migration

Branch: refactor/native-sensor-process. Base: 578631e / v1.0.0.

The stable worktree and running EXE remain untouched during development.

Projects: AioDisplay.App (existing WinForms UI/rendering/USB), AioDisplay.Contracts (versioned sensor messages), AioDisplay.SensorHost (LHM, optional read-only Fan Control IPC, Afterburner). Tests exercise rendering, protocol, process lifecycle and real sensors without opening the LCD.

The host receives configuration once via stdin and emits complete versioned JSON snapshots via stdout. No PowerShell, sensor snapshot files or independent file-writer/watchdog races. UI owns/restarts its child, checks valid protocol frames as its heartbeat, and discards stale/invalid readings. Unavailable sensors do not cause restart loops. Fan Control polling is isolated from the fast LHM sampler.

The native app uses separate preview mode signals so it cannot stop or signal the stable instance. Test builds do not register autostart. Hardware deployment happens only after validation.
