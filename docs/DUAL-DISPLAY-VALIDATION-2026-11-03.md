# Dual-display validation — 2026-11-03

Baseline: AIO 9360fda and FlexPanel 2ae5279. Integration branch: feat/dual-display.

- Full solution builds with zero warnings/errors using the pinned SDK and lockfiles.
- Existing AIO regressions and new dual-display tests pass. Tests cover independent profile persistence,
  GPU/provider selection, common FPS data, RAM/VRAM units and CPU thread order, independent output rates,
  disabling one display, a failing transport, a blocked transport, and sensor updates during a blocked USB write.
- All 42 existing AIO golden PNGs remain byte-for-byte identical.
- Real shared-hardware test: 12.086 seconds, 148 observations, zero stale observations, two detected GPUs,
  one stable sensor process, and graceful sensor/PresentMon shutdown with exit code zero.
- Process lifecycle tests pass: crash recovery, stale-stream hiding/restart, malformed data, owned-process shutdown,
  missing executable, and sensor host termination after parent death.
- Two live previews run without USB access; both report zero sent frames and one shared sensor process.
- After explicit UAC approval, both real devices run simultaneously: Thermalright approximately 30 USB frames/s,
  MiraBox approximately 10 USB frames/s. Profiles independently retain MSI vs LHM, rotation 180 vs 0 degrees,
  and their prior designs. Live board/pump/GPU fan RPM and CPU/GPU/memory values are present.
- Process inventory confirms one unified UI, one shared sensor process, one PresentMon child, and no old FlexPanel UI.
- Self-contained package checks 667 files, six matching source archives and 29 required notices; no proprietary vendor binaries.

The original applications, settings and complete Git histories were backed up before integration.
The original source worktrees remain unchanged. The actual handoff stops the originals cleanly, without deletion.
Legacy autostart entries have not been replaced; before permanent login deployment they should be disabled
and a suitable unified startup configured. Current startup remains manual, avoiding an unannounced extra UAC prompt.

USB frame counts confirm successful transfers; the physical LCD refresh rate is not measured. This is a preview
integration for the two attached devices; other models, clean PCs and suspend/resume need separate validation.
