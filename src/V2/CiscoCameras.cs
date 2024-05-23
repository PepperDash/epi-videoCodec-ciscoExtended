using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace epi_videoCodec_ciscoExtended.V2
{
    /* *s Cameras SpeakerTrack State: Off
    *s Cameras SpeakerTrack Status: Active */
    internal class CiscoCameras : CiscoRoomOsFeature, IHasCodecCameras, IHasPolls, IHasEventSubscriptions, IHandlesResponses, IHasCameraAutoMode
    {
        [Flags]
        public enum TrackingCapabilities
        {
            None = 0,
            PresenterTrack = 1,
            SpeakerTrack = 2
        }

        private static readonly List<string> PollsList = new List<string>
        {
            "xStatus Cameras"
        };

        private readonly List<CiscoNearEndCamera> cameras = new List<CiscoNearEndCamera>();

        // Auto mode is an abstraction for Speaker Track
        private bool speakerTrackIsOn;
        private bool speakerTrackIsAvailable;

        private TrackingCapabilities trackingCapabilities = TrackingCapabilities.None;

        private readonly CiscoRoomOsDevice parent;
        private readonly CiscoFarEndCamera farEndCamera;
        private readonly CCriticalSection listSync = new CCriticalSection();
        private readonly CCriticalSection selectedCameraSync = new CCriticalSection();

        private CameraBase selectedCamera;

        public CiscoCameras(CiscoRoomOsDevice parent) : base(parent.Key + "-cameras")
        {
            this.parent = parent;
            farEndCamera = new CiscoFarEndCamera(parent.Key + "-cameraFar", "Far End", parent);
            ControllingFarEndCameraFeedback = new BoolFeedback("ControllingFarEndCamera", () => selectedCamera is CiscoFarEndCamera);
            SelectedCameraFeedback = new StringFeedback("SelectedCamera", () => selectedCamera == null ? string.Empty : selectedCamera.Name);
            CameraAutoModeIsOnFeedback = new BoolFeedback("SpeakerTrackEnabled", () => speakerTrackIsOn);
            SpeakerTrackIsAvailable = new BoolFeedback("SpeakerTrackAvailable", () => speakerTrackIsAvailable);

            ControllingFarEndCameraFeedback.RegisterForDebug(parent);
            SelectedCameraFeedback.RegisterForDebug(parent);
            CameraAutoModeIsOnFeedback.RegisterForDebug(parent);
            SpeakerTrackIsAvailable.RegisterForDebug(parent);

            Subscriptions = new List<string>
            {
                "Event/CameraPresetListUpdated"
            };
        }

        public IEnumerable<string> Polls
        {
            get { return PollsList; }
        }

        public IEnumerable<string> Subscriptions { get; private set; }

        public void SelectCamera(string key)
        {
            selectedCameraSync.Enter();
            CameraBase nextCamera;

            try
            {
                nextCamera = Cameras.Find(camera => camera.Key == key);
                if (nextCamera == null)
                {
                    Debug.Console(1, this, "Could not find selected camera with key:{0}", key);
                    return;
                }
                else
                {
                    selectedCamera = nextCamera;
                    var cameraToSwitch = selectedCamera as CiscoNearEndCamera;
                    if (cameraToSwitch != null)
                    {
                        parent.SendText("xCommand Video Input SetMainVideoSource ConnectorId: " + cameraToSwitch.Connector);
                    }
                }
            }
            finally
            {
                selectedCameraSync.Leave();
            }

            SelectedCameraFeedback.FireUpdate();

            var handler = CameraSelected;
            if (handler != null)
            {
                handler(this, new CameraSelectedEventArgs(nextCamera));
            }
        }

        public List<CameraBase> Cameras
        {
            get
            {
                listSync.Enter();
                try
                {
                    return cameras.OfType<CameraBase>().ToList();
                }
                finally
                {
                    listSync.Leave();
                }
            }
        }

        public CameraBase SelectedCamera
        {
            get
            {
                selectedCameraSync.Enter();
                try
                {
                    return selectedCamera;
                }
                finally
                {
                    selectedCameraSync.Leave();
                }
            }
        }

        public StringFeedback SelectedCameraFeedback { get; private set; }

        public event EventHandler<CameraSelectedEventArgs> CameraSelected;

        public CameraBase FarEndCamera
        {
            get { return farEndCamera; }
        }

        public BoolFeedback ControllingFarEndCameraFeedback { get; private set; }

        public bool HandlesResponse(string response)
        {
            return response.IndexOf("*s Cameras", StringComparison.Ordinal) > -1;
        }

        public void HandleResponse(string response)
        {
            listSync.Enter();
            try
            {
                var cameraIds = new List<int>();

                foreach (var line in response.Split('|'))
                {
                    const string pattern = @"\*s Cameras Camera (\d+) (.*?): (.+)";
                    var match = Regex.Match(line, pattern);
                    if (match.Success)
                    {
                        var cameraIndex = int.Parse(match.Groups[1].Value);
                        var property = match.Groups[2].Value;
                        var value = match.Groups[3].Value;

                        if (!cameraIds.Contains(cameraIndex))
                            cameraIds.Add(cameraIndex);

                        var camera = cameras.Find(cam => cam.CameraId == cameraIndex);
                        if (camera == null)
                        {
                            var key = parent.Key + "-camera" + cameraIndex;
                            camera = new CiscoNearEndCamera(key, key, cameraIndex, parent);
                            cameras.Add(camera);
                        }

                        switch (property)
                        {
                            case "DetectedConnector":
                                camera.Connector = Convert.ToInt32(value);
                                // Debug.Console(1, this, "Camera:{0} | DetectedConnector {1}", cameraIndex, value);
                                break;
                            case "Capabilities Options":
                                camera.SetCapabilites(value);
                                // Debug.Console(1, this, "Camera:{0} | Capabilities {1}", cameraIndex, value);
                                break;
                            default:
                                // Debug.Console(1, this, "Camera:{0} | Property:{1} = {2}", cameraIndex, property, value);
                                break;
                        }
                    }
                    else if (line.Equals("*s Cameras SpeakerTrack Availability:"))
                    {
                        var parts = line.Split(':');
                        speakerTrackIsAvailable = parts[1].Trim().Equals("Available");
                        SpeakerTrackIsAvailable.FireUpdate();

                        if (trackingCapabilities == TrackingCapabilities.None)
                        {
                            trackingCapabilities = TrackingCapabilities.SpeakerTrack;
                        }
                        else
                        {
                            trackingCapabilities = trackingCapabilities | TrackingCapabilities.SpeakerTrack;
                        }
                    }
                    else if (line.Contains("*s Cameras SpeakerTrack Status:"))
                    {
                        var parts = line.Split(':');
                        speakerTrackIsOn = parts[1].Trim().Equals("Active");
                        CameraAutoModeIsOnFeedback.FireUpdate();
                    }
                    else if (line.Contains("*s Cameras SpeakerTrack ActiveConnector:"))
                    {
                        var parts = line.Split(':');
                        var connector = Convert.ToInt32(parts[1]);
                        foreach (var camera in cameras)
                        {
                            camera.IsSpeakerTrack = camera.Connector == connector;
                        }
                    }
                    else if (line.Equals("*s Cameras PresenterTrack Availability:"))
                    {
                        /*
                        var parts = line.Split(':');
                        speakerTrackIsAvailable = parts[1].Trim().Equals("Available");
                        SpeakerTrackIsAvailable.FireUpdate();*/

                        if (trackingCapabilities == TrackingCapabilities.None)
                        {
                            trackingCapabilities = TrackingCapabilities.SpeakerTrack;
                        }
                        else
                        {
                            trackingCapabilities = trackingCapabilities | TrackingCapabilities.SpeakerTrack;
                        }
                    }
                    else if (line.Contains("*s Cameras PresenterTrack Status:"))
                    {
                        /*
                        var parts = line.Split(':');
                        speakerTrackIsOn = parts[1].Trim().Equals("Active");
                        CameraAutoModeIsOnFeedback.FireUpdate();*/
                    }
                    else if (line.Contains("*s Cameras PresenterTrack ActiveConnector:"))
                    {
                        var parts = line.Split(':');
                        var connector = Convert.ToInt32(parts[1]);
                        foreach (var camera in cameras)
                        {
                            camera.IsPresenterTrack = camera.Connector == connector;
                        }
                    }
                }

                var camerasToRemove = 
                    from camera in cameras 
                    where !cameraIds.Contains(camera.CameraId) 
                    select camera;

                foreach (var camera in camerasToRemove)
                {
                    cameras.Remove(camera);
                }
            }
            finally
            {
                listSync.Leave();
            }

            if (cameras.Count == 1)
            {
                SelectCamera(cameras[0].Key);
            }

            if (selectedCamera == null)
            {
                selectedCamera = cameras.FirstOrDefault();
            }

            SelectedCameraFeedback.FireUpdate();
        }

        public void CameraAutoModeOn()
        {
            parent.SendText("xCommand Cameras SpeakerTrack Activate");
        }

        public void CameraAutoModeOff()
        {
            parent.SendText("xCommand Cameras SpeakerTrack Deactivate");
        }

        public void CameraAutoModeToggle()
        {
            if (!CameraAutoModeIsOnFeedback.BoolValue)
            {
                CameraAutoModeOn();
            }
            else
            {
                CameraAutoModeOff();
            }
        }

        public BoolFeedback CameraAutoModeIsOnFeedback { get; private set; }

        public BoolFeedback SpeakerTrackIsAvailable { get; private set; }
    }
}