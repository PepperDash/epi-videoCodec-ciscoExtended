# CameraManager Changes Summary

Branch: feature/add-camera-manager
Range: c42f954..HEAD

## Executive Summary

- In-scope changes focus on switchover reliability, log clarity, and simplification.
- Camera migration is now strongly feedback-gated with explicit timeout/safeguard paths.
- Startup reconciliation and attach recovery behavior were hardened for field stability.
- Warm-session readiness plumbing was removed to reduce control-flow complexity.
- Net result is more deterministic behavior under noisy real-world camera/switch feedback.

## Scope
This document includes only changes from commit c42f954 (exclusive) to the latest commit.

## Commits In Scope

- 4d05976 - fix:stabilize camera switchover recovery and startup reconciliation
- 6b21429 - fix: Reduce high-frequency debug noise in camera switchover logs
- 7837063 - fix: remove unused network switch warmup logic and related variables

## Files Changed

- src/CameraManager/CameraManager.cs
- src/CiscoCodec.CameraAssignement.cs
- src/CiscoCodec.cs
- src/Interfaces/ICiscoCodecCameraFactoryReset.cs

## What Changed

### 1) Switchover stabilization and recovery hardening
Primary file: src/CameraManager/CameraManager.cs

- Added active migration tracking per camera via activeMigrations and CameraMigrationState.
- Added attachVerificationTimer (1 second interval) to evaluate timeout and safeguard conditions.
- Added switchover control constants:
	- AttachWaitTimeoutMs = 45000
	- MaxPoeOffDurationMs = 60000
	- MaxAttachRecoveryAttempts = 1
- Added startup reconciliation in CustomActivate by calling TryExecuteScenarioCameraResets with roomCombiner.CurrentScenario.Key.

Detailed flow changes:

- NetworkSwitch_PortStateChanged
	- On PoEDisabled: marks migration.PoeDisabledConfirmed and starts PoE-off safeguard deadline.
	- On VlanChanged: marks migration.VlanChangedConfirmed and triggers PoE re-enable once.
	- On PoEEnabled: starts attach wait window and logs CAMERA_SWITCHOVER_ATTACH_WAITING marker.
- AttachVerificationTimer_Elapsed
	- Enforces PoE-off safeguard by forcing PoE on when off-window exceeds MaxPoeOffDurationMs.
	- Handles attach timeout recovery with two paths:
		- retry path: reassert target VLAN + PoE and increment AttachRecoveryAttempts
		- failure path: reseed source VLAN + PoE when retry budget is exhausted
- Codec_CameraDisconnected
	- Creates/updates CameraMigrationState.
	- Detects and ignores duplicate disconnects for same source/target/port migration.
	- Applies assignment-cleared fallback when disconnect serial is empty.
	- Issues PoE off and clear-assigned-serial actions, then waits for feedback gates.
- Codec_CameraAssignedSerialNumberChanged
	- Handles blank assigned serial as confirmation gate and calls TryIssueVlanSwitch.
- TryIssueVlanSwitch
	- Requires both gates before VLAN move:
		- PoeDisabledConfirmed
		- AssignmentClearedConfirmed
	- Calls TryConfirmAssignmentClearedFromSourceCodecState fallback before blocking.
- Codec_CameraConnected
	- Confirms attach completion for target codec, removes migration state, and runs final PoE-on guard.

Why:

- Improve reliability when field feedback arrives late/out-of-order.
- Reduce stuck states and make post-restart behavior deterministic.

### 2) Logging noise reduction in hot paths
Files:

- src/CameraManager/CameraManager.cs
- src/CiscoCodec.cs

- Reduced repetitive high-frequency debug chatter while preserving key switchover lifecycle markers.

Code-level notes:

- Changed noisy per-event debug output in switch state updates to verbose where appropriate.
- Kept structured switchover markers for key transitions, including:
	- CAMERA_SWITCHOVER_WAITING
	- CAMERA_SWITCHOVER_READY
	- CAMERA_SWITCHOVER_ATTACH_WAITING
	- CAMERA_SWITCHOVER_ATTACH_TIMEOUT
	- CAMERA_SWITCHOVER_ATTACH_FAILED
	- CAMERA_SWITCHOVER_ATTACH_AUTOMAGIC_RECOVERY_TRIGGERED
	- CAMERA_SWITCHOVER_POE_SAFEGUARD_TRIGGERED
	- CAMERA_SWITCHOVER_POE_GUARD_ENSURE_ON
- In CiscoCodec camera status parsing, changed connected-camera inventory logging from debug to verbose for lower log pressure.

Why:

- Keeps diagnostics useful under production load and improves troubleshooting signal-to-noise.

### 3) Removed unused warm-session orchestration
Primary file: src/CameraManager/CameraManager.cs

- Removed warm-session readiness prechecks and related warmup retry/plumbing paths that were not needed in final flow.

Removed elements in CameraManager.cs:

- Removed switchWarmupRetryTimer and pending scenario warmup state.
- Removed reflection-based readiness path:
	- IsNetworkSwitchReadyForFastCommands
	- SubscribeToNetworkSwitchWarmSessionReady
	- NetworkSwitch_WarmSessionReady
	- RequestNetworkSwitchWarmSession
	- ScheduleScenarioRetry
	- SwitchWarmupRetryTimer_Elapsed
- Removed System.Reflection dependency introduced only for warm-session reflection hooks.

Why:

- Simplifies control flow and removes unnecessary orchestration complexity.
- Aligns with decision to rely on normal switch session execution timing.

### 4) Supporting interface/codec adjustments in-range
Files:

- src/Interfaces/ICiscoCodecCameraFactoryReset.cs
- src/CiscoCodec.cs
- src/CiscoCodec.CameraAssignement.cs

- Included supporting updates required by the stabilization flow and event-driven camera migration handling in this range.

Code-level supporting updates:

- src/Interfaces/ICiscoCodecCameraFactoryReset.cs
	- Added CameraAssignedSerialNumberChanged event to interface contract.
- src/CiscoCodec.CameraAssignement.cs
	- Added public CameraAssignedSerialNumberChanged event implementation.
- src/CiscoCodec.cs
	- Added ParseCameraAssignedSerialFeedback and invoked it from ParseConfiguration to emit CameraAssignedSerialNumberChanged when AssignedSerialNumber feedback arrives.
	- Hardened camera status list update to avoid duplicate CameraList entries by re-checking current list and populating existing entries when needed.

Why:

- Keep CameraManager orchestration and codec feedback behavior aligned during migration and recovery steps.

## Before vs After Behavior

Before:

- Camera switchover flow relied on fewer explicit migration guards and had less structured timeout/safeguard handling.
- Startup did not proactively reconcile the active room scenario through CameraManager reset flow.
- Duplicate or sparse camera feedback (disconnect/assigned-serial sequences) had fewer defensive paths.
- Warm-session readiness orchestration existed in CameraManager, adding extra control-path complexity.
- High-frequency debug logs in hot paths could overwhelm useful diagnostic signal.

After:

- Migration is feedback-gated and stateful per camera via CameraMigrationState, with explicit prerequisites before VLAN switch.
- Attach and PoE safeguards are timer-driven and bounded:
	- 45s attach wait
	- 60s max PoE-off safeguard
	- 1 automatic attach recovery attempt before reseed/fail path
- Startup reconciliation runs on activation and applies current-scenario reset logic immediately.
- Duplicate disconnect and blank-serial fallback handling are explicit and integrated into migration progression.
- Warm-session precheck plumbing was removed, and CameraManager now relies on normal switch session execution.
- Noise was reduced by pushing repetitive logs to verbose while keeping switchover markers.

Operational impact:

- Better resilience to out-of-order/duplicated field feedback.
- Lower chance of cameras getting stuck in off/unattached states.
- Faster troubleshooting due to clearer marker logs and less noise.
- Simpler control flow with fewer non-essential readiness branches.

## Port-State Reconciliation Fix (Stranded-Camera Bug)

Primary file: src/CameraManager/CameraManager.cs (TryEnsureScenarioCameraPortStates)

Problem:

- On a room combination scenario change, the port-state reconciliation pass moved each
  camera's switch port to the target codec's VLAN unconditionally.
- When the migration/factory-reset was skipped by the source-online gate (camera not yet
  confirmed online on its source codec at scenario change), the VLAN was still flipped to
  the target codec.
- This stranded the camera on the target VLAN before any migration ran. The source codec
  could then never see the camera report "online on the wrong codec", so the deferred
  Codec_CameraConnected -> TryStartMigration path never fired.
- With disablePoeCycling set, the reconcile watchdog logged action='none'
  reason='poeCyclingDisabled' and could not bounce the port, leaving the camera floating
  unpaired until the room was reverted to divided.

Fix:

- TryEnsureScenarioCameraPortStates now gates the VLAN/PoE re-assert on
  IsCameraOnlineOnCodec(targetCodec, camera):
	- Camera confirmed online on target codec -> re-assert VLAN/PoE (idempotent alignment
	  for already-migrated cameras).
	- Camera not yet on target codec -> leave the port on its current VLAN and log
	  CAMERA_PORT_ENSURE ... action='skipped' reason='notOnlineOnTargetCodec' so the camera
	  stays visible to its source codec and the normal confirmed-online migration cascade
	  can move it.

Camera moves now gated on known state of all involved cameras:

- Cameras are only sent to switch codecs after the actual (codec-reported) state of all
  cameras involved in the scenario is known, rather than acting on assumed or stale state.
- This prevents moving a camera on a guess (for example at startup before the codecs have
  reported real camera status), which was a contributing factor to the stranded-camera and
  restart-recovery failures above.

Additional startup scenario covered by this fix:

- Restarting the program with the partition physically open (rooms combined) while the
  room combiner config is set to auto but was last left in manual/divided previously did
  not recover: the port-ensure pass would strand cameras on the target VLAN before startup
  reconciliation could migrate them. With the target-online gate in place, cameras now stay
  on their source codec until the proper migration moves them, so the system recovers on
  restart regardless of the prior manual/divided combiner state.

Applied to branches:

- feat/cameramanager-configurable-poe-cycling
- feat/cameamanager-useFactoryResetDisconnectFeedback
