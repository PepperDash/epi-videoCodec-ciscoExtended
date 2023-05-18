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

        private readonly CiscoCodec _parent;
        private readonly CCriticalSection _lock = new CCriticalSection();

        public event EventHandler<EventArgs> InitialSyncCompleted;

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

                    _parent.PollSpeakerTrack();
                    _parent.PollPresenterTrack();
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
        }

        public void LoginMessageReceived()
        {
            _lock.Enter();
            try
            {
                if (!LoginMessageWasReceived)
                {
                    LoginMessageWasReceived = true;
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Login Message Received.");
                }

                CheckSyncStatus();

                if (!JsonResponseModeSet)
                {
                    _parent.SendText("xPreferences outputmode json");
                }
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Ex:{0}", ex);
                throw;
            }
            finally
            {
                _lock.Leave();
            }

            //Schedule();
        }

        public void JsonResponseModeMessageReceived()
        {
            _lock.Enter();
            try
            {
                if (!JsonResponseModeSet)
                {
                    JsonResponseModeSet = true;
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Json Response Mode Message Received.");
                }

                CheckSyncStatus();

                if (!InitialStatusMessageWasReceived)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Sending Status query");
                    _parent.SendText("xStatus");
                }
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Ex:{0}", ex);
                throw;
            }
            finally
            {
                _lock.Leave();
            }
        }

        public void InitialStatusMessageReceived()
        {
            _lock.Enter();
            try
            {
                if (!InitialStatusMessageWasReceived)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Initial Codec Status Message Received.");
                    InitialStatusMessageWasReceived = true;
                }

                CheckSyncStatus();

                if (!InitialConfigurationMessageWasReceived)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Sending Configuration query");
                    _parent.SendText("xConfiguration");
                }
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Ex:{0}", ex);
                throw;
            }
            finally
            {
                _lock.Leave();
            }
        }

        public void InitialConfigurationMessageReceived()
        {
            _lock.Enter();
            try
            {
                if (!InitialConfigurationMessageWasReceived)
                {
                    Debug.Console(1,
                                  this,
                                  Debug.ErrorLogLevel.Notice,
                                  "Initial Codec Configuration DiagnosticsMessage Received.");
                    InitialConfigurationMessageWasReceived = true;
                }

                CheckSyncStatus();


                if (FeedbackWasRegistered) return;
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Sending feedback registrations");
                _parent.SendFeedbackRegistrations();
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Ex:{0}", ex);
                throw;
            }
            finally
            {
                _lock.Leave();
            }
        }

        public void InitialSoftwareVersionMessageReceived()
        {
            _lock.Enter();
            try
            {
                if (!InitialSoftwareVersionMessageWasReceived)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Inital Codec Software Information received");
                    InitialSoftwareVersionMessageWasReceived = true;
                }
                  
                CheckSyncStatus();
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Ex:{0}", ex);
                throw;
            }
            finally
            {
                _lock.Leave();
            }
        }

        public void FeedbackRegistered()
        {
            _lock.Enter();
            try
            {
                if (!FeedbackWasRegistered)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Initial Codec Feedback Registration Successful.");
                    FeedbackWasRegistered = true;
                }

                CheckSyncStatus();
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Ex:{0}", ex);
                throw;
            }
            finally
            {
                _lock.Leave();
            }
        }

        public void CodecDisconnected()
        {
            _lock.Enter();
            try
            {
                LoginMessageWasReceived = false;
                JsonResponseModeSet = false;
                InitialConfigurationMessageWasReceived = false;
                InitialStatusMessageWasReceived = false;
                FeedbackWasRegistered = false;
                InitialSoftwareVersionMessageWasReceived = false;
                CheckSyncStatus();
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Ex:{0}", ex);
                throw;
            }
            finally
            {
                _lock.Leave();
            }
        }

        void CheckSyncStatus()
        {
            if (LoginMessageWasReceived && JsonResponseModeSet && InitialConfigurationMessageWasReceived &&
                InitialStatusMessageWasReceived && FeedbackWasRegistered && InitialSoftwareVersionMessageWasReceived)
            {
                Debug.Console(1, this, "Codec Sync Complete");
                InitialSyncComplete = true;
            }
            else
                InitialSyncComplete = false;
        }
    }
}