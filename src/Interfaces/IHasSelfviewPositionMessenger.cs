using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces
{
    /// <summary>
    /// Mobile Control messenger for the self-view (PIP) position, on the codec's /device/{key} path. Essentials has
    /// the IHasSelfviewPosition interface but no messenger for it. The action and state names match
    /// mobile-control-react-app-core's useIHasSelfviewPosition hook: /toggleSelfviewPosition,
    /// /setSelfviewPosition ({ "value": command }), selfviewPipPosition and availablePositions.
    /// </summary>
    public class IHasSelfviewPositionMessenger : MessengerBase
    {
        private readonly IHasSelfviewPosition _selfviewPosition;
        private readonly List<CodecCommandWithLabel> _positions;

        public IHasSelfviewPositionMessenger(string key, string messagePath, IHasSelfviewPosition device,
            IEnumerable<CodecCommandWithLabel> positions)
            : base(key, messagePath, device as Device)
        {
            _selfviewPosition = device;
            _positions = positions?.ToList() ?? new List<CodecCommandWithLabel>();
        }

        protected override void RegisterActions()
        {
            if (_selfviewPosition == null)
            {
                return;
            }

            AddAction("/fullStatus", (id, content) => SendFullStatus(id));

            // Moves the self-view to the next position (and turns self-view on, as the codec command does).
            AddAction("/toggleSelfviewPosition", (id, content) => _selfviewPosition.SelfviewPipPositionToggle());

            // Sets one of availablePositions by its command; an unknown command does nothing.
            AddAction("/setSelfviewPosition", (id, content) =>
            {
                var command = ReadValue(content);
                var position = _positions.FirstOrDefault(p =>
                    string.Equals(p.Command, command, StringComparison.OrdinalIgnoreCase));

                if (position != null)
                {
                    _selfviewPosition.SelfviewPipPositionSet(position);
                }
            });

            _selfviewPosition.SelfviewPipPositionFeedback.OutputChange += (o, a) => SendFullStatus();
        }

        private static string ReadValue(JToken content)
        {
            if (content == null)
            {
                return null;
            }

            return content.Type == JTokenType.Object ? content.Value<string>("value") : content.ToString();
        }

        private void SendFullStatus(string id = null)
        {
            var message = new IHasSelfviewPositionStateMessage
            {
                SelfviewPipPosition = _selfviewPosition.SelfviewPipPositionFeedback.StringValue,
                AvailablePositions = _positions
                    .Select(p => new SelfviewPositionOption { Command = p.Command, Label = p.Label })
                    .ToList(),
            };

            PostStatusMessage(message, id);
        }
    }

    public class IHasSelfviewPositionStateMessage : DeviceStateMessageBase
    {
        /// <summary>The current self-view position's label (e.g. "Lower Right"), or empty before the codec reports one.</summary>
        [JsonProperty("selfviewPipPosition", NullValueHandling = NullValueHandling.Ignore)]
        public string SelfviewPipPosition { get; set; }

        /// <summary>The positions /setSelfviewPosition accepts, in toggle order.</summary>
        [JsonProperty("availablePositions", NullValueHandling = NullValueHandling.Ignore)]
        public List<SelfviewPositionOption> AvailablePositions { get; set; }
    }

    public class SelfviewPositionOption
    {
        /// <summary>The codec's PIPPosition value (e.g. "LowerRight").</summary>
        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }
    }
}
