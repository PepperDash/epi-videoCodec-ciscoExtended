using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;


namespace epi_videoCodec_ciscoExtended
{
    /// <summary>
    /// Tracks the initial sycnronization state of the codec when making a connection
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
                    var handler = InitialSyncCompleted;
                    if (handler != null)
                        handler(this, new EventArgs());

                    Schedule();
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
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Unable to enqueue command:{0}", query);
            }

            Schedule();
        }

        public void LoginMessageReceived()
        {
            _systemActions.Enqueue(() =>
            {
                if (!LoginMessageWasReceived)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Login Message Received.");
                    LoginMessageWasReceived = true;
                }

                if (!JsonResponseModeSet)
                {
                    _parent.SendText("xPreferences outputmode json");
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
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Json Response Mode Message Received.");

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
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Initial Codec Status Message Received.");

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
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Initial Codec Configuration DiagnosticsMessage Received.");
                    
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
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Inital Codec Software Information received");

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
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Initial Codec Feedback Registration Successful.");

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
                Debug.Console(1, this, "Codec Sync Complete");

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
                _worker = new Thread(RunSyncState, this, Thread.eThreadStartOptions.Running) { Name = Key +":Codec Sync State" };

            _waitHandle.Set();
        }

        private object RunSyncState(object o)
        {
            while (_isProcessing == Processing)
            {
                Action sys;
                if (_systemActions.TryToDequeue(out sys))
                {
                    try
                    {
                        sys();
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(1, this, "Error processing sys action:{0}", ex);
                    }

                    continue;
                }

                if (!_initialSyncComplete)
                {
                    _waitHandle.Wait();
                    continue;
                }


                Action cmd;
                if (_commandActions.TryToDequeue(out cmd))
                {
                    try
                    {
                        cmd();
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(1, this, "Error processing usr action:{0}", ex);
                    }
                }
                else
                {
                    _waitHandle.Wait();
                }
            }

            return null;
        }
    }
}