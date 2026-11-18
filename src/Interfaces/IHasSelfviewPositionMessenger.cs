using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces
{
    /// <summary>
    /// Mobile Control messenger for the self-view (PIP) position, on the codec's /device/{key} path. Essentials has
    /// the IHasSelfviewPosition interface but no messenger for it.
    /// </summary>
    public class IHasSelfviewPositionMessenger : MessengerBase
    {
        private readonly IHasSelfviewPosition _selfviewPosition;

        public IHasSelfviewPositionMessenger(string key, string messagePath, IHasSelfviewPosition device)
            : base(key, messagePath, device as Device)
        {
            _selfviewPosition = device;
        }

        protected override void RegisterActions()
        {
            if (_selfviewPosition == null)
            {
                return;
            }

            AddAction("/fullStatus", (id, content) => SendFullStatus(id));

            AddAction("/selfviewPipPositionStatus", (id, content) => SendFullStatus(id));

            // Moves the self-view to the next position (and turns self-view on, as the codec command does).
            AddAction("/selfviewPipPositionToggle", (id, content) => _selfviewPosition.SelfviewPipPositionToggle());

            _selfviewPosition.SelfviewPipPositionFeedback.OutputChange += (o, a) => SendFullStatus();
        }

        private void SendFullStatus(string id = null)
        {
            var message = new IHasSelfviewPositionStateMessage
            {
                SelfviewPipPosition = _selfviewPosition.SelfviewPipPositionFeedback.StringValue,
            };

            PostStatusMessage(message, id);
        }
    }

    public class IHasSelfviewPositionStateMessage : DeviceStateMessageBase
    {
        /// <summary>The current self-view position's label (e.g. "Lower Right"), or empty before the codec reports one.</summary>
        [JsonProperty("selfviewPipPosition", NullValueHandling = NullValueHandling.Ignore)]
        public string SelfviewPipPosition { get; set; }
    }
}
