<!-- markdownlint-disable MD032 -->

# CameraManager Readme

This document is for developers continuing CameraManager work in this repository.

Scope:
- Runtime camera migration across codecs during room-combiner scenario changes
- CameraManager orchestration, recovery logic, and operational diagnostics

Related files:
- src/CameraManager/CameraManager.cs
- src/CameraManager/CameraManagerPropertiesConfig.cs
- src/CameraManager/CameraManagerFactory.cs
- src/Cameras/CiscoCamera.cs
- src/Interfaces/ICiscoCodecCameraFactoryReset.cs

## 1) What CameraManager Does

CameraManager ensures that each managed Cisco camera is attached to the correct codec for the active room-combination scenario.

At runtime it coordinates three systems:
- Room combiner scenario state (source of intent)
- Codec camera assignment/factory-reset controls
- Network switch PoE/VLAN port controls

High-level behavior:
1. Read active scenario from room combiner.
2. For each managed camera, determine target codec and target slot.
3. If camera is online on wrong codec, run migration cascade.
4. Confirm camera attach on target codec.
5. Keep scenario aligned via periodic reconciliation and watchdog recovery.

## 2) Core Components and Contracts

### 2.1 CameraManager (`src/CameraManager/CameraManager.cs`)

Owns:
- Device references:
  - `roomCombiner` (`EssentialsRoomCombiner`)
  - `networkSwitch` (`INetworkSwitchPoeVlanManager`)
  - `managedCodecs` (`ICiscoCodecCameraFactoryReset` map)
  - `managedCameras` (`CiscoCamera` map)
- Migration state:
  - `activeMigrations` keyed by camera key
  - per-camera recovery counters and backoff tracking
- Scheduling:
  - 1-second attach verification timer
  - delayed actions for reset settle and PoE bounce
- Scenario readiness feedback:
  - `ScenarioReconciled`
  - `ScenarioReconciledFeedback`
  - `ScenarioReconciledChanged` event

### 2.2 Camera config model (`src/CameraManager/CameraManagerPropertiesConfig.cs`)

Important properties:
- `networkSwitchKey`: required
- `roomCombinerConfig.roomCombinerKey`: required
- `roomCombinerConfig.combineScenarios`: required
- `factoryResetSettleMs`: optional, default 2000
- `cameraBootWindowMs`: optional, default 180000. Boot window after the manager PoE-cycles or re-VLANs a camera's port; floating recovery does not count the camera as lost or probe it during this window. Set it longer than the slowest camera's boot.
- `disablePoeCycling`: optional, default false
- `useCameraFactoryResetDisconnectFeedback`: optional, default false
- `selectPrimaryCameraOnCallStart`: optional, default false. When a codec goes from no active call to an active call, select its `primary: true` camera for the current scenario as the main video source again, so calls start on it even after someone picked another camera outside a call. It uses the codec's active-call count (`SystemUnit State NumberOfActiveCalls`), so Teams and other platform calls count. A camera the room turned off (main video muted) stays off. A call already up when the program starts, or when the codec finishes syncing again, isn't treated as a call start. If the primary camera isn't online on the codec or is moving when the call starts, it logs `CAMERA_CALL_START_PRIMARY_WAITING` and selects the camera once it's ready, while the call lasts. A selection logs `CAMERA_CALL_START_PRIMARY_SELECT`.

Per-scenario camera assignment supports mixed syntax in `cameraKeys`:
- String form: `"cameraA"`
- Object form: `{ "cameraKey": "cameraA", "cameraId": 8 }`

### 2.2.1 Configuration snippet

```json
{
  "key": "cameraManager1",
  "name": "Camera Manager",
  "type": "cameramanager",
  "properties": {
    "networkSwitchKey": "ciscoSwitch-1",
    "factoryResetSettleMs": 2000,
    "disablePoeCycling": false,
    "useCameraFactoryResetDisconnectFeedback": false,
    "roomCombinerConfig": {
      "roomCombinerKey": "essentialsroomcombiner",
      "combineScenarios": {
        "divided": {
          "codecConfigs": [
            {
              "codecKey": "codecA",
              "cameraKeys": [
                { "cameraKey": "cameraA-front", "cameraId": 7 },
                { "cameraKey": "cameraA-side", "cameraId": 8 }
              ]
            },
            {
              "codecKey": "codecB",
              "cameraKeys": [
                { "cameraKey": "cameraB-front", "cameraId": 7 },
                { "cameraKey": "cameraB-rear", "cameraId": 8 }
              ]
            },
            {
              "codecKey": "codecC",
              "cameraKeys": [
                { "cameraKey": "cameraC-front", "cameraId": 7 }
              ]
            }
          ]
        },
        "abCombined": {
          "codecConfigs": [
            {
              "codecKey": "codecA",
              "cameraKeys": [
                { "cameraKey": "cameraA-front", "cameraId": 7 },
                { "cameraKey": "cameraA-side", "cameraId": 8 },
                { "cameraKey": "cameraB-front", "cameraId": 9 }
              ]
            },
            {
              "codecKey": "codecC",
              "cameraKeys": [
                { "cameraKey": "cameraC-front", "cameraId": 7 }
              ]
            }
          ]
        }
      }
    }
  }
}
```

Notes:
- This example uses explicit `cameraId` values for every camera assignment.
- Include only cameras that can move between codecs in `cameraKeys`.
- Cameras that never move should stay out of CameraManager scenarios.
- `cameraKeys` supports both string and object forms in the same list.

### 2.3 Codec contract (`src/Interfaces/ICiscoCodecCameraFactoryReset.cs`)

CameraManager depends on codecs providing:
- Commands:
  - `CameraFactoryReset(cameraId)`
  - `SetCameraAssignedSerialNumber(cameraId, serial)`
  - `ClearCameraAssignedSerialNumber(cameraId)`
  - `SetInputCameraId(videoConnectorId, cameraId)`
- Feedback/events:
  - `CameraConnected`
  - `CameraDisconnected`
  - `CameraAssignedSerialNumberChanged`
- Network target metadata:
  - `VLanId`

### 2.4 CiscoCamera behavior (`src/Cameras/CiscoCamera.cs`)

Fields used by manager logic:
- Identity and targeting:
  - `SerialNumber`
  - `NetworkSwitchPort`
  - `ParentCodec`
  - `IsOnline`
- Slot semantics:
  - `CameraId` (live/effective)
  - `DefaultCameraId` (config baseline)
  - `MaintainConfiguredCameraId`

Scenario pin helper:
- `SetScenarioCameraId(uint? id)`
  - Pins explicit scenario slot when provided
  - Restores baseline default behavior when null

## 3) Activation and Validation

Activation fails fast if configuration is invalid.

Validation includes:
- Missing room combiner/switch keys
- Missing or wrong device types
- Missing camera serial or switch port
- Scenario integrity checks:
  - camera assigned to more than one codec in same scenario
  - duplicate effective slot on same codec in same scenario
  - explicit `cameraId` equal to 0

Why this matters:
- Prevents runtime slot collisions and undefined migration behavior.

## 4) Runtime State Machine

The migration is feedback-driven and camera-scoped. A migration exists per camera in `activeMigrations`.

Primary phase sequence:
1. Start migration when camera is confirmed online on wrong codec.
2. Issue source codec `CameraFactoryReset`.
3. Start cascade after reset settle strategy:
   - Fixed timer (`factoryResetSettleMs`), or
   - Source disconnect feedback gate (`useCameraFactoryResetDisconnectFeedback=true`) with timeout fallback.
4. Begin cascade:
   - PoE off (unless `disablePoeCycling=true`)
   - clear source assigned serial
5. Wait until prerequisites are confirmed:
   - PoE disabled confirmed
   - assigned serial cleared confirmed (event or fallback)
6. Switch port VLAN to target codec VLAN.
7. Re-enable PoE (unless disabled)
8. Enter attach wait window.
9. Assign target codec slot serial when slot is available.
10. Confirm target attach (`CameraConnected` on target codec), clear migration state.

### 4.1 Event-driven transitions

Network switch events used:
- `PoEDisabled`
- `VlanChanged`
- `PoEEnabled`

Codec events used:
- `CameraConnected`
- `CameraDisconnected`
- `CameraAssignedSerialNumberChanged`

Timer loop (`AttachVerificationTimer_Elapsed`) handles timeout transitions, safeguards, and periodic watchdog sweep.

## 5) Target Slot Resolution Rules

The code uses two slot-selection paths, and they are not identical.

Migration attach path (`TryAssignSerialToTargetCodec`):
1. If scenario provides explicit `cameraId`, use it.
2. Else use `DefaultCameraId`.

Steady-state connected/self-heal path (`Codec_CameraConnected` -> `GetEffectiveCameraId`):
1. If scenario provides explicit `cameraId`, use it.
2. Else if `MaintainConfiguredCameraId` is true, use `DefaultCameraId`.
3. Else dynamic allocation from pool `[7,8,9]`.

Dynamic allocation protections:
- Uses confirmed in-use slots from codec camera list.
- Tracks in-flight temporary slot reservations per codec to prevent duplicate assignment before feedback catches up.
- Reservation TTL prevents leaked reservations from stale flows.

Slot-blocking behavior:
- If target slot is currently occupied by another serial, manager does not force-clear blindly.
- Migration waits for blocker events (disconnect/connect/slot clear), then retries assignment.

## 6) Recovery and Safety Nets

### 6.1 Attach timeout recovery

If target attach is not confirmed before deadline:
- Remove active migration
- Reseed camera to source codec VLAN
- Ensure PoE on (if PoE cycling enabled)
- Wait for source rediscovery and start fresh migration cycle

This is the primary proven recovery strategy in field behavior.

### 6.2 PoE safeguards

- Max off-duration safeguard can force PoE back on after extended off period (60s / 60000ms).
- Re-enable retries occur when VLAN changed but expected PoE-enabled confirmation is missing.

### 6.3 Source reset feedback timeout

If `useCameraFactoryResetDisconnectFeedback=true`:
- Wait for source camera disconnect as reset confirmation
- If no disconnect before timeout (25s), start cascade anyway

### 6.4 Assigned-serial clear timeout

If source slot clear feedback is missing:
- Timeout forces progression to VLAN switch to avoid deadlock

### 6.5 Floating-camera watchdog

Periodic sweep( ReconcileSweepIntervalMs = 30000) finds cameras that are:
- Not online on target codec
- Not currently migrating

Actions:
- If the camera's target codec is in outage (see 6.6): hold, do nothing.
- If online on wrong codec: start migration immediately.
- If the target codec's own camera-status records report the camera connected (any slot), it is present: no probe. A slot other than the configured one is warned about once (`CAMERA_SLOT_MISMATCH`).
- If the manager itself PoE-cycled or re-VLANed the camera's port within `cameraBootWindowMs` (default 180 s), the camera is rebooting: no episode, no probe until the window closes. Every manager switch command (probe, migration, reseed, port ensure) restarts the window, and so does the switch reporting a command on that port completed (a queued PoE-on can complete well after it was sent).
- If another codec's status reports the camera connected, it is alive: no probe; that codec is asked for its full camera status (at most once a minute per codec) so the normal connect -> migration path picks it up, and the camera is looked at again after `ReconcileBackoffMs`.
- If a codec's status could not be read this sweep and the camera was not found elsewhere, no conclusion is drawn: no episode, no probe, retry next sweep.
- If online nowhere: run bounded floating recovery:
  - first sighting starts a floating episode: one `CAMERA_FLOATING_DETECTED` warning, then a 60 s grace (`FloatingDetectGraceMs`) before the first probe
  - rotate probe VLANs (target codec first, then scenario codecs, then remaining managed codecs), with codecs in outage removed before rotating (A, B, C with B held probes A, C, A, C; a full pass then has 2 probes)
  - PoE bounce per attempt
  - pacing: one full pass over the candidate codecs at 2 min between probes, then doubling per probe up to 10 min (`FloatingProbePolicy`)
  - at most one probe per sweep across all cameras (`MaxFloatingProbesPerSweep`)
  - no probe while the switch session is failing (see 6.7)
  - stop at attempt limit (12 attempts per camera)
  - episode end is logged once: `CAMERA_FLOATING_EPISODE_END` with outcome, probe count and duration

State is reset on scenario change.

### 6.6 Codec/network outage hold

Every timer tick each managed codec's own camera-status records are sampled (`CiscoCodec.GetCameraStatusSnapshot`, fed to `CodecCameraOutageTracker`); the `Cameras` control collection is not used, since it drops codec-built cameras for good on disconnect and its config-built wrappers carry no serial. An "anchor" for a codec is a connected camera whose codec-reported serial is known and that is either not managed at all (e.g. a Quad), or a managed camera assigned to that codec, and only that codec, in every scenario. A moving camera is never an anchor, and a fixed managed camera seen on another codec (during a probe or wrong-codec recovery) is not an anchor there. A camera with no serial yet is not an anchor. A codec is in outage when:
- its control connection is offline, or
- it reports no online camera after it has been seen with an online anchor.

A codec whose cameras are all moving cameras is never put in outage by camera count, since it can legitimately end up empty.

While a codec is in outage, floating recovery holds every camera that belongs on it and never probes through its VLAN. When any camera comes back (an anchor alone is enough) the outage ends, and those cameras get a fresh grace period (`ReconcileBackoffMs`) before recovery may touch their ports. An "all cameras lost" outage holds recovery for at most 30 min (`CodecOutageMaxHoldMs`); after that recovery resumes (switch back-off and probe pacing still apply). A codec-offline outage holds for as long as the codec is offline. Markers: `CAMERA_CODEC_OUTAGE_START`, `CAMERA_CODEC_OUTAGE_END`, `CAMERA_CODEC_OUTAGE_HOLD_EXPIRED`.

### 6.7 Switch session back-off

`SwitchCommandHealth` follows the switch's port events. A command session that starts while the previous one never completed (timed out or aborted), or that has not completed 60 s after it started, counts as a failure; any completed command clears the failures. While failing, floating probes are held except one trial probe per back-off window (1 min, doubling per failure, capped at 10 min). A switch driver that raises no in-progress events is never seen as failing. Markers: `CAMERA_SWITCH_SESSION_FAILING`, `CAMERA_SWITCH_SESSION_RECOVERED`.

## 7) Scenario Reconciliation Semantics

`ScenarioReconciled` is true only when:
- Active scenario exists and has valid config
- No active migrations
- Every scenario camera is confirmed online on its target codec

This status is exposed as feedback and event, and should be used by external monitoring/UI as the source of truth for migration completion.

## 8) Important Behavioral Guards

These are critical to preserve during refactors:
- Migrations start only from confirmed-online wrong-codec connect path.
- Disconnect alone does not start new migrations (prevents churn and unnecessary power cycles).
- Port-state ensure is target-online gated: this reconciliation step only re-applies VLAN/PoE for cameras already online on the target codec(Switch state can drift after manual changes, switch reboot, or other automation touching the same port.). Cameras not yet on target are moved by the migration cascade (factory reset -> clear source assignment -> VLAN switch -> attach wait), not by port-state ensure.
- Source live slot is resolved by serial before reset/clear; do not trust stale slot assumptions.
- Target slot assignment waits when blocked; do not overwrite occupied slots forcefully.

## 9) Logging Guide for Troubleshooting

Use `CAMERA_SWITCHOVER_*` markers to reconstruct a single camera timeline.

Key markers to watch:
- Start: `CAMERA_SWITCHOVER_FACTORY_RESET_ISSUED` - Source codec factory reset command was sent for this camera.
- Cascade start: `CAMERA_SWITCHOVER_MIGRATION_STARTED` - Migration workflow has started (PoE/VLAN/assignment steps now in progress).
- Waiting gates: `CAMERA_SWITCHOVER_WAITING` - Migration is paused waiting for prerequisites (typically PoE-disabled and/or source assignment-cleared confirmation).
- VLAN readiness: `CAMERA_SWITCHOVER_READY` - Required preconditions are satisfied; manager is issuing or about to issue the VLAN switch to target codec VLAN.
- Attach wait start: `CAMERA_SWITCHOVER_ATTACH_WAITING` - VLAN/PoE sequence is complete and manager is now waiting for target codec attach confirmation.
- Target slot blocked: `CAMERA_SWITCHOVER_TARGET_SLOT_BUSY` - Desired target slot is occupied by another serial; manager will wait and retry assignment when slot clears.
- Attach success: `CAMERA_SWITCHOVER_ATTACH_CONFIRMED` - Camera was confirmed online on target codec; migration is considered complete.
- Attach timeout/recovery: `CAMERA_SWITCHOVER_ATTACH_FAILED`, `CAMERA_SWITCHOVER_ATTACH_AUTOMAGIC_RECOVERY_TRIGGERED` - Target attach timed out; manager initiated automatic recovery (reseed/retry path).
- Watchdog intervention: `CAMERA_SWITCHOVER_RECONCILE_WRONG_CODEC`, `CAMERA_SWITCHOVER_RECONCILE_FLOATING` - Periodic reconcile detected camera off-target or floating and is intervening (start migration or probe/bounce recovery).
- Floating episode: `CAMERA_FLOATING_DETECTED`, `CAMERA_FLOATING_EPISODE_END` - Once per camera per episode.
- Outage/back-off: `CAMERA_CODEC_OUTAGE_START`/`_END`, `CAMERA_SWITCH_SESSION_FAILING`/`_RECOVERED` - Floating recovery is on hold (codec down, or switch not answering).

Practical workflow:
1. Filter logs by camera key.
2. Confirm expected marker order.
3. Check whether flow stalls on a gate (PoE, assignment clear, slot busy, attach confirmation).
4. Correlate with codec diagnostics (camera auth/pairing/serial faults).

## 10) Configuration Checklist for New Deployments

For each managed camera:
- `serialNumber` is set and correct
- `networkSwitchPort` is set and correct
- `defaultParentCodecKey` is valid
- `defaultCameraId` is sane

For each codec in migration scenarios:
- codec is resolvable and implements `ICiscoCodecCameraFactoryReset`
- `vlanId` is configured and reachable by switch

For switch:
- device implements `INetworkSwitchPoeVlanManager`
- emits expected port state feedback events

For scenarios:
- no camera duplicated across codecs inside same scenario
- no effective slot collisions inside same codec/scenario
- only movable cameras are included

## 11) Manual Regression Test Matrix

Minimum matrix before merging behavior changes:
1. Divided -> combined migration with PoE cycling enabled.
2. Divided -> combined migration with `disablePoeCycling=true`.
3. `useCameraFactoryResetDisconnectFeedback=true` and missing disconnect fallback path.
4. Stale target slot occupied by blocker camera.
5. Attach timeout path and source reseed recovery.
6. Startup with scenario unresolved at activation (deferred startup reconciliation).
7. Scenario change while migration already active.
8. Camera appears online nowhere and watchdog floating recovery path engages.

Expected completion criterion for each case:
- `ScenarioReconciled=true` for active scenario and no active migration left.

## 12) Handover Notes for Continuation Work

If the next developer is extending this feature set, start here:
1. Read this guide and README CameraManager section end-to-end.
2. Trace one real camera migration from logs using marker sequence.
3. Verify current branch behavior against the regression matrix above.
4. Implement only one behavioral change at a time, then re-run matrix.

Recommended coding strategy:
- Preserve existing gate conditions and marker logging.
- Add new behavior behind explicit config flags where possible.
- Prefer advancing state from confirmed feedback, not assumptions.

## 13) Quick Reference: Main Methods

In `CameraManager.cs`, these are the main orchestration methods to understand first:
- `CustomActivateInternal`
- `RunScenarioReconciliation`
- `TryExecuteScenarioCameraResets`
- `TryEnsureScenarioCameraPortStates`
- `TryStartMigration`
- `BeginMigrationPoeOffAndClearSerial`
- `TryIssueVlanSwitch`
- `AttachVerificationTimer_Elapsed`
- `TryReconcileFloatingCameras`
- `Codec_CameraConnected`
- `Codec_CameraDisconnected`
- `Codec_CameraAssignedSerialNumberChanged`
- `GetEffectiveCameraId`

---

If you are taking over active work, keep this file updated with:
- Any new config flags
- New migration markers
- Changed timeout/retry defaults
- New recovery branches

<!-- markdownlint-enable MD032 -->
