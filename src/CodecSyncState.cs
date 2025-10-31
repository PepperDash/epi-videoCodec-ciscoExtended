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

        private const int Idle = 0;
        private const int Processing = 1;
        private int _isProcessing;

        private readonly CiscoCodec _parent;

        public event EventHandler<EventArgs> InitialSyncCompleted;

        private readonly CrestronQueue<Action> _systemActions = new CrestronQueue<Action>(50);
        private readonly CrestronQueue<Action> _commandActions = new CrestronQueue<Action>(50);

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
                    InitialSyncCompleted?.Invoke(this, new EventArgs());
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

            if (!_commandActions.TryToEnqueue(() => _parent.SendText(query)))
            {
                this.LogDebug("Unable to enqueue command:{query}", query);
            }

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
                    _parent.SendText("xPreferences outputmode json");

                    JsonResponseModeMessageReceived();

                    if (!InitialStatusMessageWasReceived)
                    {
                        _parent.SendText("xStatus Cameras");
                        _parent.SendText("xStatus SIP");
                        _parent.SendText("xStatus Call");
                        _parent.SendText("xStatus");
                    }
                }

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
                LoginMessageWasReceived = false;
                JsonResponseModeSet = false;
                InitialConfigurationMessageWasReceived = false;
                InitialStatusMessageWasReceived = false;
                FeedbackWasRegistered = false;
                InitialSyncComplete = false;
            });

            Schedule();
        }

        void CheckSyncStatus()
        {
            if (LoginMessageWasReceived && JsonResponseModeSet && InitialConfigurationMessageWasReceived &&
                InitialStatusMessageWasReceived && FeedbackWasRegistered && InitialSoftwareVersionMessageWasReceived)
            {
                this.LogInformation("Codec Sync Complete");

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