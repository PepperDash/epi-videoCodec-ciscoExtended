using System;
using Crestron.SimplSharp;
using PepperDash.Core;


namespace epi_videoCodec_ciscoExtended
{
    /// <summary>
    /// Tracks the initial sycnronization state of the codec when making a connection
    /// </summary>
    public class CodecSyncState : IKeyed
    {
        bool _initialSyncComplete;
        private readonly CiscoCodec _parent;

        public event EventHandler<EventArgs> InitialSyncCompleted;
        private readonly CrestronQueue<string> _commandQueue;

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
            _commandQueue = new CrestronQueue<string>(50);
            CodecDisconnected();
        }

        private void ProcessQueuedCommands()
        {
            while (InitialSyncComplete)
            {
                var query = _commandQueue.Dequeue();

                _parent.SendText(query);
            }
        }

        public void AddCommandToQueue(string query)
        {
            _commandQueue.Enqueue(query);
        }

        public void LoginMessageReceived()
        {
            if (!LoginMessageWasReceived)
                Debug.Console(1, this, "Login Message Received.");
            LoginMessageWasReceived = true;
            CheckSyncStatus();
        }

        public void JsonResponseModeMessageReceived()
        {
            if (!JsonResponseModeSet)
                Debug.Console(1, this, "Json Response Mode Message Received.");

            JsonResponseModeSet = true;
            CheckSyncStatus();
        }

        public void InitialStatusMessageReceived()
        {
            if (!InitialConfigurationMessageWasReceived)
                Debug.Console(1, this, "Initial Codec Status Message Received.");

            InitialStatusMessageWasReceived = true;
            CheckSyncStatus();
        }

        public void InitialConfigurationMessageReceived()
        {
            if(!InitialConfigurationMessageWasReceived)
                Debug.Console(1, this, "Initial Codec Configuration DiagnosticsMessage Received.");
            InitialConfigurationMessageWasReceived = true;
            CheckSyncStatus();
        }

        public void InitialSoftwareVersionMessageReceived()
        {
            if(!InitialSoftwareVersionMessageWasReceived)
                Debug.Console(1, this, "Inital Codec Software Information received");

            InitialSoftwareVersionMessageWasReceived = true;
            CheckSyncStatus();
        }

        public void FeedbackRegistered()
        {
            if (!FeedbackWasRegistered)
                Debug.Console(1, this, "Initial Codec Feedback Registration Successful.");

            FeedbackWasRegistered = true;
            CheckSyncStatus();
        }

        public void CodecDisconnected()
        {
            _commandQueue.Clear();
            LoginMessageWasReceived = false;
            JsonResponseModeSet = false;
            InitialConfigurationMessageWasReceived = false;
            InitialStatusMessageWasReceived = false;
            FeedbackWasRegistered = false;
            InitialSyncComplete = false;
        }


        void CheckSyncStatus()
        {
            if (LoginMessageWasReceived && JsonResponseModeSet && InitialConfigurationMessageWasReceived &&
                InitialStatusMessageWasReceived && FeedbackWasRegistered && InitialSoftwareVersionMessageWasReceived)
            {
                Debug.Console(1, this, "Initial Codec Sync Complete!");
                Debug.Console(1, this, "{0} Command queued. Processing now...", _commandQueue.Count);

                // Invoke a thread for the queue
                CrestronInvoke.BeginInvoke(o => ProcessQueuedCommands());

                InitialSyncComplete = true;
            }
            else
                InitialSyncComplete = false;
        }
    }

}