using System;
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

        // Ensures the initial 'xStatus' dump is requested exactly once per connection, independent
        // of whether the codec echoes the 'xPreferences outputmode json' command back.
        private bool _initialStatusRequested;

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

        public bool InitialConfigurationMessageWasReceived { get; private set; }

        public bool InitialSoftwareVersionMessageWasReceived { get; private set; }

        public bool FeedbackWasRegistered { get; private set; }

        public CodecSyncState(string key, CiscoCodec parent)
        {
            Key = key;
            _parent = parent;

            CrestronEnvironment.ProgramStatusEventHandler += type =>
                {
                    if (type != eProgramStatusEventType.Stopping)
                        return;

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

        public void LoginMessageReceived()
        {
            _systemActions.Enqueue(() =>
            {
                if (!LoginMessageWasReceived)
                {
                    this.LogDebug("Login Message Received.");
                    LoginMessageWasReceived = true;
                }

                if (!JsonResponseModeSet)
                {
                    _parent.SendTextWithoutQueue("xPreferences outputmode json");
                }

                // Kick off the initial status dump here rather than waiting on the codec to echo
                // the 'xPreferences outputmode json' command - with SSH 'echo off' that echo may
                // never arrive, which would otherwise stall initial sync.
                RequestInitialStatusOnce();

                CheckSyncStatus();
            });

            Schedule();
        }

        public void JsonResponseModeMessageReceived()
        {
            _systemActions.Enqueue(() =>
            {
                if (!JsonResponseModeSet)
                    this.LogDebug("Json Response Mode Message Received.");

                JsonResponseModeSet = true;
                RequestInitialStatusOnce();
                CheckSyncStatus();
            });

            Schedule();
        }

        /// <summary>
        /// Marks JSON output mode as active based on a successfully-parsed JSON message, for codecs
        /// (e.g. the EQ) that never echo/acknowledge the 'xPreferences outputmode json' command.
        /// </summary>
        public void JsonResponseModeConfirmedByValidJson()
        {
            _systemActions.Enqueue(() =>
            {
                if (!JsonResponseModeSet)
                    this.LogDebug("JSON output mode confirmed by a valid JSON response (outputmode echo not received).");

                JsonResponseModeSet = true;
                RequestInitialStatusOnce();
                CheckSyncStatus();
            });

            Schedule();
        }

        private void RequestInitialStatusOnce()
        {
            if (InitialStatusMessageWasReceived || _initialStatusRequested)
                return;

            _initialStatusRequested = true;
            _parent.SendTextWithoutQueue("xStatus");
        }

        public void InitialStatusMessageReceived()
        {
            _systemActions.Enqueue(() =>
            {
                if (!InitialStatusMessageWasReceived)
                    this.LogDebug("Initial Codec Status Message Received.");

                InitialStatusMessageWasReceived = true;
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
                CheckSyncStatus();
            });

            Schedule();
        }

        public void CodecDisconnected()
        {
            _systemActions.Enqueue(() =>
            {
                this.LogDebug("CodecDisconnected: resetting all SyncState flags to false");
                LoginMessageWasReceived = false;
                JsonResponseModeSet = false;
                _initialStatusRequested = false;
                InitialConfigurationMessageWasReceived = false;
                InitialStatusMessageWasReceived = false;
                FeedbackWasRegistered = false;
                InitialSyncComplete = false;
            });

            Schedule();
        }

        void CheckSyncStatus()
        {
            this.LogDebug(
                "CheckSyncStatus: LoginMessageWasReceived={Login}, JsonResponseModeSet={Json}, InitialConfigurationMessageWasReceived={Config}, InitialStatusMessageWasReceived={Status}, FeedbackWasRegistered={Feedback}, InitialSoftwareVersionMessageWasReceived={Software}, InitialSyncComplete={Sync}",
                LoginMessageWasReceived, JsonResponseModeSet, InitialConfigurationMessageWasReceived,
                InitialStatusMessageWasReceived, FeedbackWasRegistered, InitialSoftwareVersionMessageWasReceived,
                InitialSyncComplete);

            if (LoginMessageWasReceived && JsonResponseModeSet && InitialConfigurationMessageWasReceived &&
                InitialStatusMessageWasReceived && FeedbackWasRegistered && InitialSoftwareVersionMessageWasReceived)
            {
                if (InitialSyncComplete)
                {
                    return;
                }

                InitialSyncComplete = true;
                _parent.PollSpeakerTrack();
                _parent.PollPresenterTrack();
            }
            else
                InitialSyncComplete = false;
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