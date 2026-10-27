using System;
using System.Collections.Generic;
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

        private const int Idle = 0;
        private const int Processing = 1;
        private int _isProcessing;

        private readonly CiscoCodec _parent;

        public event EventHandler<EventArgs> InitialSyncCompleted;

        private readonly CrestronQueue<Action> _systemActions = new CrestronQueue<Action>(100);
        private readonly CrestronQueue<Action> _commandActions = new CrestronQueue<Action>(100);

        private Thread _worker;
        private readonly CEvent _waitHandle = new CEvent();

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

        public bool LoginMessageWasReceived { get; private set; }

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

            _syncWatchdogTimer = new CTimer(o => CheckSyncProgress(), null, SyncWatchdogIntervalMs, SyncWatchdogIntervalMs);

            CrestronEnvironment.ProgramStatusEventHandler += type =>
                {
                    if (type != eProgramStatusEventType.Stopping)
                        return;

                    _syncWatchdogTimer.Stop();
                    _syncWatchdogTimer.Dispose();

                    Interlocked.Exchange(ref _isProcessing, Idle);
                    _waitHandle.Set();
                };
        }

        public void AddCommandToQueue(string query)
        {
            if (string.IsNullOrEmpty(query))
                return;

            _commandActions.Enqueue(() =>
             {
                 _parent.SendTextWithoutQueue(query);
             });

            // if (!_commandActions.TryToEnqueue(() => _parent.SendText(query)))
            // {
            //     this.LogError("Unable to enqueue command:{query}", query);
            //     this.LogError("commandActions queue is full. Consider increasing the queue size if this is a common occurrence. Count = {CommandQueueCount}", _commandActions.Count);
            // }

            Schedule();
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
            _systemActions.Enqueue(() =>
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

            _systemActions.Enqueue(() =>
            {
                if (LoginMessageWasReceived && JsonResponseModeSet)
                    return;

                this.LogDebug("JSON received from the codec: the session is logged in and in JSON mode.");

                JsonResponseModeSet = true;

                if (!LoginMessageWasReceived)
                {
                    // Adopting a session this program did not set up (e.g. after a program restart).
                    LoginMessageWasReceived = true;
                    MarkSessionDetected();

                    // It may still have echo on.
                    _parent.SendTextWithoutQueue("echo off");

                    // Always request the full status here: the JSON that got us here may be a poll
                    // reply (SystemUnit, cameras, SIP, volume), which already counts as the initial
                    // status but leaves call, standby and presentation state unloaded. A session
                    // this program set up already requested it in StartSession.
                    _parent.RequestFullStatus();
                }

                MarkSyncPhase("jsonMode");
                CheckSyncStatus();
            });

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

            _systemActions.Enqueue(() =>
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
            });

            Schedule();
        }

        /// <summary>
        /// Marks the session as logged in and, unless JSON mode is already confirmed, turns echo off and
        /// requests JSON mode. JSON mode is only marked as set once JSON actually arrives.
        /// Runs on the worker thread.
        /// </summary>
        private void StartSession(string reason)
        {
            var isNewSession = !LoginMessageWasReceived;
            LoginMessageWasReceived = true;
            MarkSessionDetected();

            if (JsonResponseModeSet)
                return;

            if (isNewSession)
                this.LogDebug("Codec session detected ({reason}): turning echo off and requesting JSON mode.", reason);

            _parent.SendTextWithoutQueue("echo off");
            _parent.SendTextWithoutQueue("xPreferences outputmode json");

            if (isNewSession && !FullStatusWasReceived)
                _parent.RequestFullStatus();
        }

        /// <summary>The reply to this sync's tagged full xStatus request has been parsed (#142).</summary>
        public void FullStatusReceived()
        {
            _systemActions.Enqueue(() =>
            {
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
            _systemActions.Enqueue(() =>
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
            _systemActions.Enqueue(() =>
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
            _systemActions.Enqueue(() =>
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
            _systemActions.Enqueue(() =>
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
            _commandActions.Clear();

            _systemActions.Enqueue(() =>
            {
                this.LogDebug("Session lost: dropping queued commands and resetting all SyncState flags");
                _commandActions.Clear();
                ResetSync();
            });

            Schedule();
        }

        public void CodecDisconnected()
        {
            _systemActions.Enqueue(() =>
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

            _systemActions.Enqueue(RetryMissingSyncSteps);
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

        private void Schedule()
        {
            if (Interlocked.CompareExchange(
                ref _isProcessing,
                Processing,
                Idle) ==
                Idle)
                _worker = new Thread(RunSyncState, this, Thread.eThreadStartOptions.Running) { Name = Key + ":Codec Sync State" };

            _waitHandle.Set();
        }

        private object RunSyncState(object o)
        {
            while (_isProcessing == Processing)
            {
                // Hold off sending anything to the codec while a JSON response is currently being
                // accumulated - the codec can echo in-flight commands back into the response stream,
                // which corrupts the JSON buffer being parsed.
                if (_parent.IsReceivingJsonMessage)
                {
                    _waitHandle.Wait(20);
                    continue;
                }

                if (_systemActions.TryToDequeue(out Action sys))
                {
                    try
                    {
                        sys();
                    }
                    catch (Exception ex)
                    {
                        this.LogError("Error processing system action: {message}", ex.Message);
                        this.LogVerbose(ex, "Exception");
                    }
                    continue;
                }

                if (_commandActions.TryToDequeue(out Action cmd))
                {
                    try
                    {
                        cmd();
                    }
                    catch (Exception ex)
                    {
                        this.LogError("Error processing user action: {message}", ex.Message);
                        this.LogVerbose(ex, "Exception");
                    }
                    continue;
                }

                _waitHandle.Wait();
            }

            return null;
        }
    }
}