# Camera Migration — Work Handoff

> Working notes for the codec↔codec camera migration on room-combine scenario changes.
> File: `src/CameraManager/CameraManager.cs`. Camera class: `src/Cameras/CiscoCamera.cs`.
> Branch: `feat/progress-autorecovery`. Build: `dotnet build` (net472, 0 errors as of last session).

## The architecture (the rule everything follows)

- **Codec = status authority.** `CiscoCodec.cs` parses `xStatus Cameras Camera`, reads `Connected.Value` + `SerialNumber.Value`, and calls `CiscoCamera.SetOnlineStatus(true/false)`. `CiscoCamera.IsOnline` is the single source of truth for "is this camera really there?".
- **CLI / network switch = executor.** PoE on/off + VLAN changes run the physical migration.
- **Golden rule:** NEVER start a migration blindly. Only start it after we can **confirm the camera is genuinely ONLINE on the wrong/source codec**. That confirmation is why migrations can "blindly" run the cascade afterward — we already proved it *can* work.

## The migration cascade (the proven machinery)

User's exact spec: *confirm ONLINE → factory reset → DON'T wait for disconnect → ~500ms → PoE off → VLAN change → PoE on.*

Implemented sequence (all in `CameraManager.cs`):

1. `TryStartMigration(camera, sourceCodec, sourceCameraId, targetCodecKey, scenarioKey)` — debounce check → create `CameraMigrationState` → register in `activeMigrations` (synchronous) → log `CAMERA_SWITCHOVER_FACTORY_RESET_ISSUED` → `sourceCodec.CameraFactoryReset(sourceCameraId)` → `ScheduleDelayed(MigrationPoeOffDelayMs=500, …)`.
2. `BeginMigrationPoeOffAndClearSerial(migration, sourceCodec)` — verify migration still active → log `CAMERA_SWITCHOVER_MIGRATION_STARTED actions='PoeOff+ClearAssignedSerial'` → `SetPortPoeState(port, false)` → `ClearCameraAssignedSerialNumber(sourceCameraId)`.
3. `NetworkSwitch_PortStateChanged` — `PoEDisabled` → `TryIssueVlanSwitch` (target VLAN) → `VlanChanged` → `SetPortPoeState(true)` + `TryAssignSerialToTargetCodec(…, "attachWaitStart")` → `PoEEnabled` → set `AttachWaitStarted`.
4. `AttachVerificationTimer_Elapsed` — attach-wait (120s) with auto-recovery (`MaxAttachRecoveryAttempts=2`): on failure reseed source, on timeout reassert target + `TryAssignSerialToTargetCodec(…, "attachTimeoutReassert")`.
5. `TryAssignSerialToTargetCodec` — pins `targetCodec.SetCameraAssignedSerialNumber(DefaultCameraId, SerialNumber)` so the target codec claims the camera. Logs `CAMERA_SWITCHOVER_TARGET_SERIAL_ASSIGN`.

Constants (lines ~33-36): `AttachWaitTimeoutMs=120000`, `MaxPoeOffDurationMs=60000`, `MaxAttachRecoveryAttempts=2`, `MigrationPoeOffDelayMs=500`.

## The two — and only two — migration triggers

Both now route through the **single shared `TryStartMigration` helper**:

1. **Scenario-reset** — `TryExecuteScenarioCameraResets(scenarioKey)` (~line 689). For each camera not on its target codec: skip if already on target; then **source-online gate** (`camera.ParentCodec.Cameras.OfType<CiscoCamera>().Any(IsOnline && serial match)`); if not online → skip/defer; if online → `TryStartMigration(...)`.
2. **Connect-on-wrong-codec** — `Codec_CameraConnected` (~line 920). Camera connects on the wrong codec → debounce (`activeMigrations.ContainsKey`) → online gate (`!camera.IsOnline` → defer) → `TryStartMigration(...)`.

`Codec_CameraDisconnected` (~line 800) **NEVER starts a migration** anymore — it only advances an already-active one (duplicate-disconnect path / PoE retry) and has phantom-disconnect + target-codec-ignore guards.

`TryEnsureScenarioCameraPortStates` (~line 555) sets VLAN+PoE only for cameras **without** an active migration (checks `hasActiveMigration`). Because `TryExecuteScenarioCameraResets` registers the migration synchronously and runs FIRST, PORT_ENSURE correctly skips migrating cameras → no VLAN race.

## Config (CLT-2WD-Norman&Wylie)

- codecA VLAN 45, codecB VLAN 46.
- cameraA-front: serial `AVR29501789`, port `gi1/0/3`, cameraId 9, `defaultParentCodecKey` codecA.
- codecB's own cameras: `AVR29501767` (id 7), `AVR29501675` (id 8).
- cameraManager1 manages cameraA-front: divided → codecA, abCombined → codecB.

## History of bugs fixed this session

1. codecB never claimed camera → added `TryAssignSerialToTargetCodec`. Resolved.
2. Switchover when already on right codec at startup → `ParentCodec` defaults to configured codecA but camera physically on codecB.
3. Online-on-TARGET guard failed (codecB hadn't reported cameras yet, empty list) → flipped to **source-online gate**.
4. Disconnect handler started false migrations (powered off healthy camera) → removed migration-creation from disconnect handler.
5. Removing that broke BOTH directions (disconnect handler had been the only path running the full cascade) → **extracted `TryStartMigration` and wired both triggers through it.** ← last change, build green.

## TOMORROW — verify both directions live

Deploy, then watch `c:\Users\equinoy\Documents\Logs.txt` for this sequence (each direction):

```
CAMERA_SWITCHOVER_FACTORY_RESET_ISSUED
  → (500ms) CAMERA_SWITCHOVER_MIGRATION_STARTED actions='PoeOff+ClearAssignedSerial'
  → CAMERA_SWITCHOVER_READY
  → VLAN change
  → CAMERA_SWITCHOVER_POE_ON_AFTER_VLAN
  → CAMERA_SWITCHOVER_ATTACH_WAITING
  → CAMERA_SWITCHOVER_TARGET_SERIAL_ASSIGN
  → CAMERA_SWITCHOVER_ATTACH_CONFIRMED
```

Checklist:
- [ ] **divided → codecA** completes (this was the broken one — camera dropped from codecB but never landed on codecA).
- [ ] **abCombined → codecB** still completes (was working via the now-removed disconnect path — MUST re-verify under new architecture).
- [ ] No false switchover when the camera is already on the correct codec.
- [ ] No premature VLAN flip from PORT_ENSURE racing the factory reset (confirm PORT_ENSURE skips the migrating camera).
- [ ] No bounce loop.

## Gotchas / lessons

- Codec camera-list + device collection have **sync lag right after SSH connect** — don't trust them in the first ~1s.
- `camera.ParentCodec` is unreliable at startup (defaults to configured parent).
- The cascade machinery (gated PoE power-cycle + attach-wait + target-serial-assign) is what actually moves a camera — a bare factory reset + immediate VLAN flip is NOT enough.
- Keep changes minimal; do not add docs unless asked.
