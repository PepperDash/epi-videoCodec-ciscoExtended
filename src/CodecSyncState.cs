using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Core.Logging;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Tracks the initial synchronization state of the codec when making a connection
    /// </summary>
    public class CodecSyncState : IKeyed
    {
        bool _initialSyncComplete;

        private readonly CiscoCodec _parent;

        public event EventHandler<EventArgs> InitialSyncCompleted;

        // The worker that sends to the codec (#194). Its loop has no Crestron dependencies, so it is
        // unit tested; here it gets the processor's thread, queues and signal.
        private readonly SendWorker _worker;

        public string Key { get; private set; }

        public bool InitialSyncComplete
        {
            get { return _initialSyncComplete; }
            private set
            {
                if (value && !_initialSyncComplete)
                {
                    var handler = InitialSyncCompleted;
                    if (handler != null)
                        handler(this, new EventArgs());
                }
                _initialSyncComplete = value;
            }
        }

        /// <summary>The sync has claimed the codec's session: its login was seen and its setup sent.</summary>
        public bool LoginMessageWasReceived
        {
            get { return _session.Claimed; }
            private set { _session.Claimed = value; }
        }

        private readonly SessionStarter _session;

        public bool JsonResponseModeSet { get; private set; }

        public bool InitialStatusMessageWasReceived { get; private set; }

        /// <summary>
        /// The reply to this sync's own full <c>xStatus</c> request (tagged with a resultId) has been
        /// parsed. <see cref="InitialStatusMessageWasReceived"/> is set by any status message, including
        /// the communication monitor's poll reply, which carries no call data; sync completion waits for
        /// this one, so ActiveCalls and cameras are loaded when Initial Sync Complete is raised (#142).
        /// </summary>
        public bool FullStatusWasReceived { get; private set; }

        public bool InitialConfigurationMessageWasReceived { get; private set; }

        public bool InitialSoftwareVersionMessageWasReceived { get; private set; }

        public bool FeedbackWasRegistered { get; private set; }

        public CodecSyncState(string key, CiscoCodec parent)
        {
            Key = key;
            _parent = parent;
            // The gate's Arriving is set on the receive thread as soon as this session's full status reply
            // is matched, until a reset: a session setup that runs before the worker applies
            // FullStatusReceived must not ask again.
            _session = new SessionStarter(_parent.SendTextWithoutQueue, _parent.RequestFullStatus, () => _parent.IsFullStatusPending,
                () => _parent.FullStatusReply.Arriving);

            _worker = new SendWorker(
                new CrestronActionQueue(QueueCapacity),
                new CrestronActionQueue(QueueCapacity),
                new CEventSignal(),
                loop => new Thread(_ => { loop(); return null; }, null, Thread.eThreadStartOptions.Running) { Name = Key + ":Codec Sync State" },
                () => _parent.IsReceivingJsonMessage,
                new SendQueueHealth(),
                (label, ex) =>
                {
                    this.LogError("Error processing {label}: {message}", label, ex.Message);
                    this.LogVerbose(ex, "Exception");
                });

            _syncWatchdogTimer = new CTimer(o => CheckSyncProgress(), null, SyncWatchdogIntervalMs, SyncWatchdogIntervalMs);

            CrestronEnvironment.ProgramStatusEventHandler += type =>
                {
                    if (type != eProgramStatusEventType.Stopping)
                        return;

                    _syncWatchdogTimer.Stop();
                    _syncWatchdogTimer.Dispose();

                    _worker.Stop();
                };
        }

        public void AddCommandToQueue(string query)
        {
            if (string.IsNullOrEmpty(query))
                return;

            var what = DropEpisodeTracker.Describe(query);
            Enqueue(SendQueueKind.Command, _commandQueueFullDrops, what,
                onResult => _worker.Enqueue(SendQueueKind.Command, () => _parent.SendTextWithoutQueue(query), what, onResult));

            Schedule();
        }

        // Each queue holds QueueCapacity actions. CrestronQueue.Enqueue blocks indefinitely when the
        // queue is full, so with the worker stalled every caller (the receive thread, UI and timer
        // threads) would hang one by one, silently (#194). Full queues drop the action and log instead.
        private const int QueueCapacity = 100;
        private readonly DropEpisodeTracker _systemQueueFullDrops = new DropEpisodeTracker(CiscoCodec.DropReminderInterval);
        private readonly DropEpisodeTracker _commandQueueFullDrops = new DropEpisodeTracker(CiscoCodec.DropReminderInterval);

        /// <summary>
        /// Queues a sync step, once: while it waits, more messages asking for the same step (each JSON
        /// document, status reply or text line while the worker is busy or stalled) are served by the
        /// queued copy, which reads the current state when it runs (#194). Before, a stalled worker let
        /// these fill the system queue within seconds. The step is named by its caller.
        /// </summary>
        /// <param name="action">The step.</param>
        /// <param name="sessionMode">
        /// The session mode a step detects (<see cref="JsonMode"/> or <see cref="TextMode"/>): a copy of
        /// any step waiting from before the mode changed won't serve a request made after it, so steps
        /// keep their order across the change. Steps of the same mode don't separate each other. A
        /// poll echo (LoginMessageReceived) gives no mode: it only proves a session exists, and a session
        /// in JSON mode with echo still on echoes polls too.
        /// </param>
        /// <param name="caller">The step's name (its caller).</param>
        private void EnqueueSystemAction(Action action, string sessionMode = null, [CallerMemberName] string caller = "")
        {
            Enqueue(SendQueueKind.System, _systemQueueFullDrops, caller,
                onResult => _worker.EnqueueSystemOnce(caller, action, sessionMode, onResult));
        }

        private const string JsonMode = "json";
        private const string TextMode = "text";

        /// <summary>What the send path is doing, for codecperf and the send watchdog (#194).</summary>
        internal SendQueueHealth SendHealth => _worker.Health;

        private void ClearCommands() => _worker.ClearCommands();

        /// <summary>
        /// Runs <paramref name="enqueue"/> (a SendWorker enqueue taking a result callback) and logs a
        /// full-queue drop episode around it.
        /// </summary>
        private void Enqueue(SendQueueKind kind, DropEpisodeTracker drops, string what, Action<Action<bool>> enqueue)
        {
            var name = kind == SendQueueKind.Command ? "command" : "system";

            // The drop episode is updated under the worker's lock, in the order the queue saw the
            // results, so a success can't end an episode another caller has just started; logged after.
            int recovered = 0, droppedSinceLog = 0, droppedInEpisode = 0;
            var logDrop = false;
            enqueue(accepted =>
            {
                if (accepted)
                    recovered = drops.OnSucceeded();
                else
                {
                    logDrop = drops.OnDropped(DateTime.UtcNow, out droppedSinceLog);
                    droppedInEpisode = drops.DroppedInEpisode;
                }
            });

            if (recovered > 0)
                this.LogInformation("The codec {queue} queue accepts actions again: {count} were dropped while it was full.", name, recovered);

            if (logDrop)
                this.LogWarning(
                    "The codec {queue} queue is full ({capacity} waiting), so {what} was dropped ({count} dropped since the last warning, {total} in this episode). Nothing is being taken off the queue: the worker that sends to the codec may be stuck.",
                    name, QueueCapacity, what, droppedSinceLog, droppedInEpisode);
        }

        /// <summary>
        /// Queues a session reset, which must never be dropped: with its flags left set, a reconnect in
        /// JSON mode would never re-sync (#194). If the system queue is full, its stale actions are
        /// dropped instead (they belong to the session being reset). Never blocks.
        /// </summary>
        private void EnqueueReset(Action action, [CallerMemberName] string caller = "")
        {
            var discarded = _worker.EnqueueReset(action, caller);
            if (discarded > 0)
                this.LogWarning(
                    "The codec system queue was full when {what} arrived: dropped the {count} sync action(s) waiting in it so the reset is not lost.",
                    caller, discarded);
        }

        // How a session is recognised (#113). Over RS-232 there are no socket events, and the codec's
        // session can outlive a program restart (still logged in, in JSON mode, echo off) or restart on
        // its own (back to text mode, echo on, no feedback registrations). So the sync is driven by what
        // the codec actually sends, never only by echoes:
        //  - a JSON message: the session is logged in and in JSON mode;
        //  - text-mode output (*s, *r, *e, *c, including the "*r Login successful" banner): a session
        //    exists but is not in JSON mode - set it up, or re-sync if JSON mode had been confirmed;
        //  - an echo of the plugin's own poll: a session exists (kept for sessions with echo on).

        /// <summary>
        /// The plugin's poll was echoed: a session exists.
        /// </summary>
        public void LoginMessageReceived()
        {
            EnqueueSystemAction(() =>
            {
                StartSession("poll echoed");
                CheckSyncStatus();
            });

            Schedule();
        }

        /// <summary>
        /// A complete JSON message was parsed: the session is logged in and in JSON mode. This also
        /// covers a session that was already set up before a program restart, where no login banner
        /// or echo will ever arrive.
        /// </summary>
        public void JsonMessageReceived()
        {
            if (LoginMessageWasReceived && JsonResponseModeSet)
                return; // the usual case: nothing to do, and no action queued per message

            EnqueueSystemAction(() =>
            {
                if (LoginMessageWasReceived && JsonResponseModeSet)
                    return;

                this.LogDebug("JSON received from the codec: the session is logged in and in JSON mode.");

                JsonResponseModeSet = true;

                if (!LoginMessageWasReceived)
                {
                    // Adopting a session this program did not set up (e.g. after a program restart).
                    // Echo off (it may still have echo on) and always the full status: the JSON that got
                    // us here may be a poll reply (SystemUnit, cameras, SIP, volume), which already
                    // counts as the initial status but leaves call, standby and presentation state
                    // unloaded. A session this program set up already requested it in StartSession.
                    MarkSessionDetected();
                    if (_session.Adopt(FullStatusWasReceived).SetupDropped)
                        OnSessionSetupDropped("JSON received");
                }

                MarkSyncPhase("jsonMode");
                CheckSyncStatus();
            }, JsonMode);

            Schedule();
        }

        /// <summary>
        /// The codec sent text-mode output (a line starting *s, *r, *e or *c, which includes the
        /// "*r Login successful" banner of a new session).
        /// </summary>
        public void TextOutputReceived()
        {
            if (LoginMessageWasReceived && !JsonResponseModeSet)
                return; // the session is already being set up; the watchdog retries if that stalls

            EnqueueSystemAction(() =>
            {
                if (JsonResponseModeSet)
                {
                    // In JSON mode the codec never sends text output, so the session restarted (or
                    // lost JSON mode) and has lost its echo setting and feedback registrations too.
                    this.LogWarning(
                        "Codec sent text-mode output after JSON mode was confirmed: its session restarted or left JSON mode. Re-syncing.");
                    ResetSync();
                    StartSession("session restarted");
                }
                else if (!LoginMessageWasReceived)
                {
                    StartSession("text output received");
                }

                CheckSyncStatus();
            }, TextMode);

            Schedule();
        }

        /// <summary>
        /// Marks the session as logged in and, unless JSON mode is already confirmed, turns echo off and
        /// requests JSON mode. JSON mode is only marked as set once JSON actually arrives.
        /// Runs on the worker thread.
        /// </summary>
        private void StartSession(string reason)
        {
            if (!LoginMessageWasReceived && !JsonResponseModeSet)
                this.LogDebug("Codec session detected ({reason}): turning echo off and requesting JSON mode.", reason);

            MarkSessionDetected();
            if (_session.Start(JsonResponseModeSet, FullStatusWasReceived).SetupDropped)
                OnSessionSetupDropped(reason);
        }

        /// <summary>
        /// A new session's setup was dropped because the connection reported not connected (an SSH
        /// client can deliver the codec's first output just before it reports connected). The session
        /// stays unclaimed, so the next output the codec sends starts it again, full status included;
        /// the sync is timed from that start.
        /// </summary>
        private void OnSessionSetupDropped(string reason)
        {
            _sessionDetectedAt = 0;
            this.LogInformation(
                "Codec session detected ({reason}) before the connection reported connected: its setup wasn't sent. Starting it again on the codec's next output.",
                reason);
        }

        /// <summary>The reply to this sync's tagged full xStatus request has been parsed (#142).</summary>
        public void FullStatusReceived()
        {
            EnqueueSystemAction(() =>
            {
                // Read when the step runs, not captured: a reply matched before a reset that has run since
                // (which clears Arriving) can't stand in for the new session's own full status, and a
                // waiting copy of this step that serves a later call applies that call's reply.
                if (!_parent.FullStatusReply.Arriving)
                {
                    this.LogDebug("Full Codec Status from a session that has since been reset: ignored.");
                    return;
                }

                if (!FullStatusWasReceived)
                    this.LogDebug("Full Codec Status received.");

                FullStatusWasReceived = true;
                MarkSyncPhase("fullStatus");
                CheckSyncStatus();
            });

            Schedule();
        }

        public void InitialStatusMessageReceived()
        {
            EnqueueSystemAction(() =>
            {
                if (!InitialStatusMessageWasReceived)
                    this.LogDebug("Initial Codec Status Message Received.");

                InitialStatusMessageWasReceived = true;
                MarkSyncPhase("status");
                CheckSyncStatus();
            });

            Schedule();
        }

        public void InitialConfigurationMessageReceived()
        {
            EnqueueSystemAction(() =>
            {
                if (!InitialConfigurationMessageWasReceived)
                    this.LogDebug("Initial Codec Configuration DiagnosticsMessage Received.");

                InitialConfigurationMessageWasReceived = true;
                MarkSyncPhase("configuration");
                CheckSyncStatus();
            });

            Schedule();
        }

        public void InitialSoftwareVersionMessageReceived()
        {
            EnqueueSystemAction(() =>
            {
                if (!InitialSoftwareVersionMessageWasReceived)
                    this.LogDebug("Initial Codec Software Information received");

                InitialSoftwareVersionMessageWasReceived = true;
                MarkSyncPhase("softwareVersion");

                CheckSyncStatus();
            });

            Schedule();
        }

        public void FeedbackRegistered()
        {
            EnqueueSystemAction(() =>
            {
                if (!FeedbackWasRegistered)
                    this.LogDebug("Initial Codec Feedback Registration Successful.");

                FeedbackWasRegistered = true;
                MarkSyncPhase("feedback");
                CheckSyncStatus();
            });

            Schedule();
        }

        /// <summary>
        /// The session is gone and anything sent now would be taken as a login: the RS-232 session
        /// dropped to a login prompt (#115). Drops the queued commands and resets the sync.
        /// </summary>
        public void SessionLost()
        {
            ClearCommands();

            EnqueueReset(() =>
            {
                this.LogDebug("Session lost: dropping queued commands and resetting all SyncState flags");
                ClearCommands();
                ResetSync();
            });

            Schedule();
        }

        public void CodecDisconnected()
        {
            EnqueueReset(() =>
            {
                this.LogDebug("CodecDisconnected: resetting all SyncState flags to false");
                ResetSync();
            });

            Schedule();
        }

        /// <summary>
        /// Clears the sync flags so the next session is synced from the start. Runs on the worker thread.
        /// </summary>
        private void ResetSync()
        {
            // The previous session's full status requests: their replies may never come, and while
            // pending they would hold back the new session's own request. Also clears the arriving mark,
            // under the lock the reply match takes.
            _parent.FullStatusReply.Reset();
            LoginMessageWasReceived = false;
            JsonResponseModeSet = false;
            InitialConfigurationMessageWasReceived = false;
            InitialStatusMessageWasReceived = false;
            FullStatusWasReceived = false;
            FeedbackWasRegistered = false;
            InitialSyncComplete = false;
            _syncIncompleteSinceUtc = null;
            _sessionDetectedAt = 0;

            // A reset sync must not leave the previous sync's periodic timers running while it
            // re-syncs (on RS-232 there is no socket disconnect to stop them).
            _parent.StopRefreshTimers();
        }

        // Sync watchdog (#113): each initial request (JSON mode, xStatus, xConfiguration, xFeedback List)
        // is otherwise sent once, so a lost or unparseable reply left the sync incomplete for good, with
        // nothing logged. While a session is logged in but not synced, re-request what is missing once
        // the sync has made no progress for SyncRetryAfter. Long enough for a full xConfiguration reply
        // (about 96 KB, roughly 9 s at 115200 baud) to arrive and be parsed before anything is resent.
        private const long SyncWatchdogIntervalMs = 15000;
        private static readonly TimeSpan SyncRetryAfter = TimeSpan.FromSeconds(60);
        private CTimer _syncWatchdogTimer;
        private DateTime? _syncIncompleteSinceUtc;

        private void CheckSyncProgress()
        {
            if (InitialSyncComplete)
                return;

            EnqueueSystemAction(RetryMissingSyncSteps);
            Schedule();
        }

        /// <summary>
        /// Runs on the worker thread.
        /// </summary>
        private void RetryMissingSyncSteps()
        {
            if (InitialSyncComplete || !LoginMessageWasReceived)
            {
                _syncIncompleteSinceUtc = null;
                return;
            }

            // Time from the last progress, not from when the watchdog started: a step that completes
            // restarts the wait, so a slow but progressing sync is never retried early.
            var now = DateTime.UtcNow;
            var progress = SyncStepsCompleted();
            if (_syncIncompleteSinceUtc == null || progress != _syncProgressAtLastCheck)
            {
                _syncIncompleteSinceUtc = now;
                _syncProgressAtLastCheck = progress;
                return;
            }

            if (now - _syncIncompleteSinceUtc.Value < SyncRetryAfter)
                return;

            _syncIncompleteSinceUtc = now; // next retry no sooner than another SyncRetryAfter

            this.LogWarning(
                "Sync with the codec still incomplete after {seconds} s (JsonMode={Json}, Status={Status}, FullStatus={FullStatus}, Configuration={Config}, SoftwareVersion={Software}, FeedbackRegistered={Feedback}). Re-requesting what is missing.",
                SyncRetryAfter.TotalSeconds, JsonResponseModeSet, InitialStatusMessageWasReceived, FullStatusWasReceived,
                InitialConfigurationMessageWasReceived, InitialSoftwareVersionMessageWasReceived, FeedbackWasRegistered);

            if (!JsonResponseModeSet)
            {
                _parent.SendTextWithoutQueue("echo off");
                _parent.SendTextWithoutQueue("xPreferences outputmode json");
                _parent.RequestFullStatus();
                return;
            }

            if (!InitialStatusMessageWasReceived || !FullStatusWasReceived)
            {
                _parent.RequestFullStatus();
                if (!InitialStatusMessageWasReceived)
                    return;
            }

            if (!InitialConfigurationMessageWasReceived)
                _parent.SendTextWithoutQueue("xConfiguration");

            // The software version comes from SystemUnit status, which a custom monitor poll may not include.
            if (!InitialSoftwareVersionMessageWasReceived)
                _parent.SendTextWithoutQueue("xStatus SystemUnit");

            if (!FeedbackWasRegistered)
                _parent.SendTextWithoutQueue("xFeedback List");
        }

        private int _syncProgressAtLastCheck;

        private int SyncStepsCompleted()
        {
            var steps = 0;
            if (JsonResponseModeSet) steps++;
            if (InitialStatusMessageWasReceived) steps++;
            if (FullStatusWasReceived) steps++;
            if (InitialConfigurationMessageWasReceived) steps++;
            if (InitialSoftwareVersionMessageWasReceived) steps++;
            if (FeedbackWasRegistered) steps++;
            return steps;
        }

        void CheckSyncStatus()
        {
            this.LogDebug(
                "CheckSyncStatus: LoginMessageWasReceived={Login}, JsonResponseModeSet={Json}, InitialConfigurationMessageWasReceived={Config}, InitialStatusMessageWasReceived={Status}, FullStatusWasReceived={FullStatus}, FeedbackWasRegistered={Feedback}, InitialSoftwareVersionMessageWasReceived={Software}, InitialSyncComplete={Sync}",
                LoginMessageWasReceived, JsonResponseModeSet, InitialConfigurationMessageWasReceived,
                InitialStatusMessageWasReceived, FullStatusWasReceived, FeedbackWasRegistered,
                InitialSoftwareVersionMessageWasReceived, InitialSyncComplete);

            if (LoginMessageWasReceived && JsonResponseModeSet && InitialConfigurationMessageWasReceived &&
                InitialStatusMessageWasReceived && FullStatusWasReceived && FeedbackWasRegistered &&
                InitialSoftwareVersionMessageWasReceived)
            {
                if (InitialSyncComplete)
                {
                    return;
                }

                RecordSyncComplete();
                InitialSyncComplete = true;
                // No SpeakerTrack/PresenterTrack polls here: the sync's full xStatus already carries both,
                // and polling PresenterTrack on a model without it logged an ERROR every sync (#117).
            }
            else
                InitialSyncComplete = false;
        }

        // Sync timing (#129): from the moment a session is detected to Initial Sync Complete, plus when
        // each step completed. Measuring only. All of this runs on the worker thread.
        private long _sessionDetectedAt;
        private readonly Dictionary<string, double> _syncPhaseMs = new Dictionary<string, double>();

        private void MarkSessionDetected()
        {
            if (_sessionDetectedAt != 0)
                return;

            _sessionDetectedAt = CodecTimings.Now;
            _syncPhaseMs.Clear();
        }

        private void MarkSyncPhase(string phase)
        {
            if (_sessionDetectedAt == 0 || _syncPhaseMs.ContainsKey(phase))
                return;

            var ms = CodecTimings.ElapsedMs(_sessionDetectedAt, CodecTimings.Now);
            _syncPhaseMs[phase] = ms;
            _parent.Timings.Record("sync." + phase, ms);
        }

        private double SyncPhaseMs(string phase) =>
            _syncPhaseMs.TryGetValue(phase, out var ms) ? Math.Round(ms) : -1;

        private void RecordSyncComplete()
        {
            if (_sessionDetectedAt == 0)
                return;

            var total = CodecTimings.ElapsedMs(_sessionDetectedAt, CodecTimings.Now);
            _parent.Timings.Record("sync.total", total);
            _sessionDetectedAt = 0;

            this.LogInformation(
                "[perf] Initial sync took {total} ms after the session was detected (JSON mode {json} ms, status {status} ms, configuration {config} ms, software version {software} ms, feedback {feedback} ms, full status {fullStatus} ms; -1 = not measured)",
                Math.Round(total), SyncPhaseMs("jsonMode"), SyncPhaseMs("status"), SyncPhaseMs("configuration"),
                SyncPhaseMs("softwareVersion"), SyncPhaseMs("feedback"), SyncPhaseMs("fullStatus"));
        }

        private void Schedule() => _worker.Schedule();

        /// <summary>
        /// Starts a new worker when the current one has stopped taking actions off the queues (#194).
        /// Called by the send watchdog, from its own timer.
        /// </summary>
        internal void RestartWorker() => _worker.RestartWorker();

        /// <summary>
        /// Queues a probe so the send watchdog can see the worker finish something after a recovery
        /// (#194): an empty queue left by the recovery's drain proves nothing. The probe waits for the
        /// send lock, as a real command would, so a write still blocked under it (a replaced worker's,
        /// over RS-232) keeps the probe from finishing and the stall from ending.
        /// </summary>
        internal void EnqueueWatchdogProbe()
        {
            // Its own slot, not the system queue: a full queue or a session reset can't lose it.
            _worker.RequestProbe(() =>
            {
                _parent.WaitForSendPath();
                SendHealth.OnProbeCompleted();
            }, "SendWatchdogProbe");
            Schedule();
        }

        /// <summary>
        /// Empties the command queue without sending, so commands that waited out a stall aren't sent
        /// late (#194). Returns how many were dropped and describes the first <paramref name="describe"/>.
        /// </summary>
        internal int DrainCommands(int describe, out List<string> described) =>
            _worker.DrainCommands(describe, out described);

        /// <summary>A CrestronQueue for the send worker. TryToEnqueue never blocks (Enqueue would, when full).</summary>
        private sealed class CrestronActionQueue : IActionQueue
        {
            private readonly CrestronQueue<QueuedAction> _queue;

            public CrestronActionQueue(int capacity)
            {
                _queue = new CrestronQueue<QueuedAction>(capacity);
            }

            public bool TryEnqueue(QueuedAction action) => _queue.TryToEnqueue(action);

            public bool TryDequeue(out QueuedAction action) => _queue.TryToDequeue(out action);

            public void Clear() => _queue.Clear();
        }

        /// <summary>A CEvent (auto-reset) as the send worker's wake-up signal.</summary>
        private sealed class CEventSignal : IWorkerSignal
        {
            private readonly CEvent _event = new CEvent();

            public void Set() => _event.Set();

            public void Wait() => _event.Wait();

            public void Wait(int timeoutMs) => _event.Wait(timeoutMs);
        }
    }
}
