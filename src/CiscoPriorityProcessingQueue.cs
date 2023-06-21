using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace epi_videoCodec_ciscoExtended
{
    public class CiscoPriorityProcessingQueueEventArgs : EventArgs
    {
        public string Payload { get; set; }
    }

    public class CiscoPriorityProcessingQueue : IKeyed
    {
        private readonly CiscoCodec _parent;
        private readonly CrestronQueue<string> _tx = new CrestronQueue<string>(200);
        private readonly CrestronQueue<string> _rx = new CrestronQueue<string>(5000);
        private readonly CEvent _waitHandle = new CEvent();
        private readonly CodecSyncState _syncState;

        private bool _jsonFeedbackMessageIsIncoming;
        private bool _feedbackListMessageIncoming;
        private StringBuilder _feedbackListMessage;
        private StringBuilder _jsonMessage;
        private bool _shouldProcess = true;

        private const string Delimiter = "\r\n";

        public event EventHandler<CiscoPriorityProcessingQueueEventArgs> ResponseReceived;
        public event EventHandler<CiscoPriorityProcessingQueueEventArgs> FeedbackResponseReceived;

        protected virtual void OnFeedbackResponseReceived(CiscoPriorityProcessingQueueEventArgs e)
        {
            EventHandler<CiscoPriorityProcessingQueueEventArgs> handler = FeedbackResponseReceived;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnResponseReceived(CiscoPriorityProcessingQueueEventArgs e)
        {
            EventHandler<CiscoPriorityProcessingQueueEventArgs> handler = ResponseReceived;
            if (handler != null) handler(this, e);
        }

        public CiscoPriorityProcessingQueue(CiscoCodec parent, CommunicationGather gather, CodecSyncState syncState)
        {
            CrestronEnvironment.ProgramStatusEventHandler += type =>
                                                             {
                                                                 if (type != eProgramStatusEventType.Stopping) 
                                                                     return;

                                                                 _shouldProcess = false;
                                                                 _waitHandle.Set();
                                                             };
            _parent = parent;
            _syncState = syncState;
            gather.LineReceived += (sender, args) =>
                                    {
                                        _rx.Enqueue(args.Text);
                                        _waitHandle.Set();
                                    };

            _syncState.InitialSyncCompleted += (sender, args) => _waitHandle.Set();
            new Thread(Run, this)
            {
                Priority = Global.ProcessorSeries == eCrestronSeries.Series3 
                    ? Thread.eThreadPriority.LowestPriority
                    : Thread.eThreadPriority.MediumPriority
            };
        }

        public void Enqueue(string payload)
        {
            if (string.IsNullOrEmpty(payload))
                return;

            _tx.Enqueue(payload);
            _waitHandle.Set();
        }

        private object Run(object _)
        {
            while (_shouldProcess)
            {
                string message;
                if (_rx.TryToDequeue(out message))
                {
                    ProcessIncoming(message);
                    continue;
                }

                if (_jsonFeedbackMessageIsIncoming || _feedbackListMessageIncoming)
                {
                    _waitHandle.Wait();
                    continue;
                }

                if (_syncState.InitialSyncComplete && _tx.TryToDequeue(out message))
                {
                    SendText(message);
                    continue;                    
                }

                _waitHandle.Wait();
            }

            return default(int);
        }

        private void ProcessIncoming(string response)
        {
            try
            {
                if (response.ToLower().Contains("xcommand"))
                {
                    Debug.Console(1, this, "Received command echo response.  Ignoring");
                    return;
                }

                if (!response.StartsWith("/") && _feedbackListMessage != null && _feedbackListMessageIncoming)
                {
                    _feedbackListMessageIncoming = false;

                    var feedbackListString = _feedbackListMessage.ToString();
                    _feedbackListMessage = null;

                    //ProcessFeedbackList(feedbackListString);
                    OnFeedbackResponseReceived(new CiscoPriorityProcessingQueueEventArgs { Payload = feedbackListString });
                }

                if (response.StartsWith("/"))
                {
                    _feedbackListMessageIncoming = true;
                    if (_feedbackListMessage == null) _feedbackListMessage = new StringBuilder();
                }

                if (_feedbackListMessageIncoming && _feedbackListMessage != null)
                {
                    _feedbackListMessage.Append(response);
                    return;
                }

                if (!_syncState.InitialSyncComplete)
                {
                    var data = response.Trim().ToLower();
                    if (data.Contains("*r login successful") || data.Contains("xstatus systemunit"))
                    {
                        _syncState.LoginMessageReceived();
                        
                        //if (_loginMessageReceivedTimer != null)
                            //_loginMessageReceivedTimer.Stop();

                        //SendText("echo off");
                    }
                    else if (data.Contains("xpreferences outputmode json"))
                    {
                        if (_syncState.JsonResponseModeSet)
                            return;

                        _syncState.JsonResponseModeMessageReceived();
                    }
                    else if (data.Contains("xfeedback register /event/calldisconnect"))
                    {
                        _syncState.FeedbackRegistered();
                    }
                }

                if (response == "{" + Delimiter) // Check for the beginning of a new JSON message
                {
                    _jsonFeedbackMessageIsIncoming = true;
                    _jsonMessage = new StringBuilder();
                }
                else if (response == "}" + Delimiter) // Check for the end of a JSON message
                {
                    _jsonFeedbackMessageIsIncoming = false;

                    _jsonMessage.Append(response);

                    // Enqueue the complete message to be deserialized
                    //DeserializeResponse(_jsonMessage.ToString());
                    OnResponseReceived(new CiscoPriorityProcessingQueueEventArgs { Payload = _jsonMessage.ToString() });
                    return;
                }

                if (!_jsonFeedbackMessageIsIncoming) return;
                _jsonMessage.Append(response);
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Swallowing an exception processing a response:{0}", ex);
            }
        }

        private void SendText(string text)
        {
            try
            {
                _parent.SendText(text);
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Error:{0}", ex);
            }
        }

        public string Key
        {
            get { return _parent.Key; }
        }
    }
}