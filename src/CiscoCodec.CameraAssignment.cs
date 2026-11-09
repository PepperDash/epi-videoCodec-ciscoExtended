
using System;
using System.Linq;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{

    public partial class CiscoCodec : ICiscoCodecCameraFactoryReset
    {
        public event EventHandler<CameraEventArgs> CameraDisconnected;
        public event EventHandler<CameraEventArgs> CameraConnected;
        public event EventHandler<CameraEventArgs> CameraAssignedSerialNumberChanged;

        public void CameraFactoryReset(uint cameraId)
        {
            this.LogDebug("Issuing factory reset for camera ID {cameraId} on codec {codecKey}", cameraId, Key);
            EnqueueCommand($"xCommand Camera FactoryReset CameraId: {cameraId} Confirm: Yes");
        }

        /// <summary>
        /// Copies the codec's live camera-status records (every camera the codec reports, managed
        /// or not) as (connected, serial) pairs. Unlike <see cref="Cameras"/>, which only holds
        /// control wrappers and drops a codec-built camera for good once it disconnects, this
        /// follows the codec's own status. Returns null when the list changed while copying; the
        /// caller should take the next sample.
        /// </summary>
        public System.Collections.Generic.List<CodecCameraStatusSample> GetCameraStatusSnapshot()
        {
            try
            {
                var list = CodecStatus?.Status?.Cameras?.CameraList;
                if (list == null)
                {
                    return new System.Collections.Generic.List<CodecCameraStatusSample>();
                }

                return list.ToList()
                    .Where(c => c != null)
                    .Select(c => new CodecCameraStatusSample(
                        string.Equals(c.Connected?.Value, "True", StringComparison.OrdinalIgnoreCase),
                        c.SerialNumber?.Value))
                    .ToList();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                // List<T>.ToList can throw this when another thread resizes the list mid-copy.
                return null;
            }
        }

        public string GetCameraSerialNumber(uint cameraId)
        {
            var camera = Cameras.Where(c => c is CiscoCamera cam && cam.CameraId == cameraId).FirstOrDefault() as CiscoCamera;

            if (camera != null)
            {
                return camera.SerialNumber;
            }
            else
            {
                this.LogWarning("Camera with ID {cameraId} not found on codec {codecKey}", cameraId, Key);
                return null;
            }
        }

		public void SetCameraAssignedSerialNumber(uint cameraId, string serialNumber)
		{
			if (string.IsNullOrEmpty(serialNumber))
			{
				this.LogDebug("Clearing the serial number of camera {id}", cameraId);
				ClearCameraAssignedSerialNumber(cameraId);
				return;
			}

			this.LogDebug("Setting the serial number of camera {id} to {serialNumber}", cameraId, serialNumber);
			EnqueueCommand($"xConfiguration Cameras Camera[{cameraId}] AssignedSerialNumber: \"{EscapeConfigurationStringValue(serialNumber)}\"");
		}

		private static string EscapeConfigurationStringValue(string value)
		{
			return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
		}

		public void SetInputCameraId(uint videoConnectorId, uint inputCameraId)
		{
			this.LogDebug("Setting the camera id of video connector {id} to {inputCameraId}", videoConnectorId, inputCameraId);
			EnqueueCommand($"xConfiguration Video Input Connector[{videoConnectorId}] CameraControl CameraId: {inputCameraId}");
		}

        public void ClearCameraAssignedSerialNumber(uint cameraId)
        {
            this.LogDebug("Clearing the assigned serial number for camera ID {cameraId}", cameraId);
            EnqueueCommand($"xConfiguration Cameras Camera[{cameraId}] AssignedSerialNumber: \"\"");
        }

        public void SetCameraFlip(uint cameraId, bool flip)
        {
            var flipValue = flip ? "On" : "Off";
            this.LogDebug("Setting camera {cameraId} Flip to {flip}", cameraId, flipValue);
            EnqueueCommand($"xConfiguration Cameras Camera[{cameraId}] Flip: {flipValue}");
        }

        public void SetPresenterTrackConnector(uint connector)
        {
            this.LogDebug("Setting PresenterTrack connector to {connector}", connector);
            EnqueueCommand($"xConfiguration Cameras PresenterTrack Connector: {connector}");
        }

        public void SetPresenterTrackEnabled(bool enabled)
        {
            var enabledValue = enabled ? "True" : "False";
            this.LogDebug("Setting PresenterTrack Enabled to {enabled}", enabledValue);
            EnqueueCommand($"xConfiguration Cameras PresenterTrack Enabled: {enabledValue}");
        }
    }

}