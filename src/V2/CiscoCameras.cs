using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PDT.Plugins.Cisco.RoomOs.V2
{
    internal class CiscoCameras : CiscoRoomOsFeature, IHasCodecCameras, IBridgeAdvanced
    {
        private static readonly List<string> PollsList = new List<string>
        {
            "xStatus Cameras"
        };

        private readonly List<CiscoNearEndCamera> cameras = new List<CiscoNearEndCamera>();

        private readonly CiscoRoomOsDevice parent;
        private readonly CiscoFarEndCamera farEndCamera;
        private readonly CCriticalSection listSync = new CCriticalSection();
        private readonly CCriticalSection selectedCameraSync = new CCriticalSection();
        private CameraBase selectedCamera;

        public CiscoCameras(CiscoRoomOsDevice parent) : base(parent.Key + "-cameras")
        {
            this.parent = parent;
            farEndCamera = new CiscoFarEndCamera(parent.Key + "-cameraFar", "Far End", parent);
            ControllingFarEndCameraFeedback = new BoolFeedback(() => selectedCamera is CiscoFarEndCamera);
            SelectedCameraFeedback = new StringFeedback(() => string.Empty);
        }

        public override IEnumerable<string> Polls
        {
            get { return PollsList; }
        }

        public override IEnumerable<string> Subscriptions
        {
            get { return Enumerable.Empty<string>(); }
        }

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            throw new NotImplementedException();
        }

        public void SelectCamera(string key)
        {
            selectedCameraSync.Enter();
            CameraBase nextCamera;
            try
            {
                nextCamera = cameras.Find(camera => camera.Key == key);
                if (selectedCamera == null)
                {
                    Debug.Console(1, this, "Could not find selected camera with key:{0}", key);
                }
                else
                {
                    selectedCamera = nextCamera;
                }
            }
            finally
            {
                selectedCameraSync.Leave();
            }

            if (nextCamera != null)
            {
                SelectedCameraFeedback.SetValueFunc(() => nextCamera.Name);
                SelectedCameraFeedback.FireUpdate();

                EventHandler<CameraSelectedEventArgs> handler = CameraSelected;
                if (handler != null)
                {
                    handler(this, new CameraSelectedEventArgs(nextCamera));
                }
            }
        }

        public List<CameraBase> Cameras
        {
            get
            {
                listSync.Enter();
                try
                {
                    return cameras.Cast<CameraBase>().ToList();
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

        public override bool HandlesResponse(string response)
        {
            return response.IndexOf("*s Cameras", StringComparison.Ordinal) > -1;
        }

        public override void HandleResponse(string response)
        {
            const string pattern = @"\*s Cameras Camera (\d+) (\w+): (.+)";

            foreach (var line in response.Split('|'))
            {
                listSync.Enter();
                try
                {
                    var match = Regex.Match(line, pattern);
                    if (match.Success)
                    {
                        var cameraIndex = int.Parse(match.Groups[1].Value);
                        var property = match.Groups[2].Value;
                        var value = match.Groups[3].Value;

                        var camera = cameras.Find(cam => cam.CameraId == cameraIndex);
                        if (camera == null)
                        {
                            var key = parent.Key + "-camera" + cameraIndex;
                            camera = new CiscoNearEndCamera(key, key, cameraIndex, parent);
                            cameras.Add(camera);
                        }

                        switch (property)
                        {
                            default:
                                Debug.Console(2, this, "Camera:{0} | Property:{1}:{2}", cameraIndex, property, value);
                                break;
                        }
                    }
                }
                finally
                {
                    listSync.Leave();
                }
            }
        }
    }
}