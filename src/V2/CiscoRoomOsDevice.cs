using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using epi_videoCodec_ciscoExtended;
using epi_videoCodec_ciscoExtended.V2;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PDT.Plugins.Cisco.RoomOs.V2
{
    public class CiscoRoomOsPluginFactory : EssentialsPluginDeviceFactory<CiscoRoomOsDevice>
    {
        public CiscoRoomOsPluginFactory()
        {
            TypeNames = new List<string> {"ciscoRoomOsV2"};
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            return new CiscoRoomOsDevice(dc);
        }
    }

    public class CiscoRoomOsDevice : EssentialsDevice, IHasCodecCameras, IOnline, ICommunicationMonitor
    {
        public const string Delimiter = "\r\n";

        internal readonly CiscoCameras CodecCameras;
        internal readonly CiscoStandby CodecStandby;

        private readonly IBasicCommunication communications;
        private readonly IList<CiscoRoomOsFeature> features = new List<CiscoRoomOsFeature>();

        private readonly string username;
        private readonly string password;

        private bool isLoggedIn;
        private StringBuilder buffer = new StringBuilder();

        public CiscoRoomOsDevice(DeviceConfig config) : base(config.Key, config.Name)
        {
            var props = config.Properties.ToObject<CiscoCodecConfig>();
            username = props.Username ?? string.Empty;
            password = props.Password ?? string.Empty;

            communications = CommFactory.CreateCommForDevice(config);
            var gather = new CommunicationGather(communications, Delimiter);

            gather.LineReceived += GatherOnLineReceived;

            if (props.CommunicationMonitorProperties != null)
            {
                CommunicationMonitor = new GenericCommunicationMonitor(
                    this,
                    communications,
                    props.CommunicationMonitorProperties);
            }
            else
            {
                const string pollString = "xStatus SystemUnit\r";

                CommunicationMonitor = new GenericCommunicationMonitor(
                    this,
                    communications,
                    30000,
                    120000,
                    300000,
                    pollString);
            }

            CodecCameras = new CiscoCameras(this);
            CodecCameras.CameraSelected += CameraSelected;

            CodecStandby = new CiscoStandby(this);

            features.Add(CodecCameras);
            features.Add(CodecStandby);
        }

        private void GatherOnLineReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            var data = args.Text.Trim();

            if (string.IsNullOrEmpty(data) 
                || data == "Command not recognized."
                || data == "OK"
                || data == "ERROR")
            {
                return;
            }

            if (
                (data.StartsWith("*s SystemUnit") || data.Contains("*r Login successful"))
                && !isLoggedIn
                && (communications as ISocketStatus) == null) // RS232 Login Sucessful
            {
                isLoggedIn = true;

                var handler = Rs232LoggedIn;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }

            if (data.Contains("login:"))
            {
                // handles login for non-delimited
            }
            else if (data.Contains("{") || data.Contains("}"))
            {
                SendText("xPreferences outputmode terminal");
            }
            else if (data.Contains("Password:"))
            {
                // handles login for non-delimited
            }
            else if (data.StartsWith("tshell:"))
            {
                // generic tshell notification
            }
            else if (data.StartsWith("xStatus"))
            {
                // echo needs to be disabled
                SendText("echo off");
            }
            else if (data.Contains("Login incorrect"))
            {
                Debug.Console(0, this, "Incorrect Login");
            }
            else if (data == "** end")
            {
                var dataToProcess = buffer.ToString();
                buffer = new StringBuilder();
                ProcessResponse(dataToProcess);
            }
            else
            {
                buffer.Append(data);
                buffer.Append("|");
            }
        }

        private void ProcessResponse(string response)
        {
            const string pattern = @"resultId:\s*\""(?<id>\d+)\""";

            var match = Regex.Match(response, pattern);
            if (match.Success)
            {
                var result = match.Groups["id"].Value;
                var resultId = Int32.Parse(result);

                Debug.Console(0, this, "Found a result id:{0}", resultId);

                Action<string> callback;
                if (requestCallbacks.TryGetValue(resultId, out callback))
                {
                    try
                    {
                        callback(response);
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(0, this, Debug.ErrorLogLevel.Notice,
                            "Caught an exception handling a response:{0}", ex);
                    }
                    finally
                    {
                        requestCallbacks.Remove(resultId);
                    }
                }
                else
                {
                    Debug.Console(0, this, Debug.ErrorLogLevel.Notice,
                        "Couldn't find a matching callback for resultId:{0}", resultId);
                }
            }
            else
            {
                foreach (var handler in features.Where(feature => feature.HandlesResponse(response)))
                {
                    try
                    {
                        handler.HandleResponse(response);
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(0, handler, Debug.ErrorLogLevel.Notice, "Caught and exception handling a response:{0}", ex);
                    }
                }
            }
        }

        public void SelectCamera(string key)
        {
            CodecCameras.SelectCamera(key);
        }

        public List<CameraBase> Cameras
        {
            get { return CodecCameras.Cameras; }
        }

        public CameraBase SelectedCamera
        {
            get { return CodecCameras.SelectedCamera; }
        }

        public StringFeedback SelectedCameraFeedback
        {
            get { return CodecCameras.SelectedCameraFeedback; }
        }

        public event EventHandler<CameraSelectedEventArgs> CameraSelected;

        public CameraBase FarEndCamera
        {
            get { return CodecCameras.FarEndCamera; }
        }

        public BoolFeedback ControllingFarEndCameraFeedback
        {
            get { return CodecCameras.ControllingFarEndCameraFeedback; }
        }

        private event EventHandler Rs232LoggedIn;

        public override void Initialize()
        {
            var pollTimer = new CTimer(
                _ =>
                {
                    PollFeatures();
                    PollForFeedback();
                },
                Timeout.Infinite);

            if (communications is ISocketStatus)
            {
                (communications as ISocketStatus)
                    .ConnectionChange += (sender, args) =>
                    {
                        if (args.Client.ClientStatus ==
                            SocketStatus.SOCKET_STATUS_CONNECTED)
                        {
                            pollTimer.Reset(250, 60000);
                        }
                        else
                        {
                            pollTimer.Stop();
                        }
                    };

                communications.Connect();
                isLoggedIn = true;
                CommunicationMonitor.Start();
            }
            else
            {
                communications.TextReceived += CommunicationsOnTextReceived;
                Rs232LoggedIn += (sender, args) => pollTimer.Reset(250, 60000);
                CommunicationMonitor.Start();
            }

            CrestronEnvironment
                .ProgramStatusEventHandler += type =>
                {
                    if (type == eProgramStatusEventType.Stopping)
                    {
                        pollTimer.Stop();
                    }
                };
        }

        private void CommunicationsOnTextReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            var data = args.Text.Trim();

            if (data.Contains("login:"))
            {
                isLoggedIn = false;
                if (string.IsNullOrEmpty(username))
                {
                    Debug.Console(0, Debug.ErrorLogLevel.Notice, "Prompted for a username but none is configured");
                }
                else
                {
                    SendText(username);
                }
            }
            else if (data.Contains("Password:"))
            {
                isLoggedIn = false;
                if (string.IsNullOrEmpty(username))
                {
                    Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Prompted for a password but none is configured");
                }
                else
                {
                    SendText(password);
                }
            }
        }

        public void SendText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            const string delimiter = "\r";
            var textToSend = text.Trim() + delimiter;
            communications.SendText(textToSend);
        }

        public void PollFeatures()
        {
            var polls = from feature in features
                from poll in feature.Polls
                select poll;

            foreach (var item in polls)
            {
                SendText(item);
            }
        }

        public void PollForFeedback()
        {
            Action<string> parseFeedbackRequest = response =>
            {
                var feedbackSubscriptions = from feature in features
                    from sub in feature.Subscriptions
                    select sub;

                foreach (var subscription in feedbackSubscriptions
                    .Where(sub =>
                        response.IndexOf(sub, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    Debug.Console(1, this, "Registering for feedback:{0}", subscription);
                    SendText("xFeedback register " + subscription);
                }
            };

            SendRequest("xFeedback list", parseFeedbackRequest);
        }

        private static int lastRequestId;

        private readonly Dictionary<int, Action<string>> requestCallbacks = new Dictionary<int, Action<string>>();

        private void SendRequest(string request)
        {
            SendRequest(request, (s => Debug.Console(1, this, "Unhandled response:{0}", s)));
        }

        private void SendRequest(string request, Action<string> onResponse)
        {
            var requestId = Interlocked.Increment(ref lastRequestId);
            var textToSend = request.Trim() + ("|resultid=" + requestId);
            var callback = onResponse ?? (s => Debug.Console(0, this, "Unhandled response:{0}", s));

            requestCallbacks.Add(requestId, callback);

            CTimer timer = null;
            timer = new CTimer(_ =>
            {
                if (requestCallbacks.ContainsKey(requestId))
                {
                    Debug.Console(1, this, "Removing request due to timeout:{0}", requestId);
                    requestCallbacks.Remove(requestId);
                }

                if (timer != null)
                    timer.Dispose();

            }, 120000);

            SendText(textToSend);
        }

        public BoolFeedback IsOnline
        {
            get { return CommunicationMonitor.IsOnlineFeedback; }
        }

        public StatusMonitorBase CommunicationMonitor { get; private set; }
    }
}