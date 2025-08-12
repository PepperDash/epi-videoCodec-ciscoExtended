using System;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;
using PepperDash.Essentials.Devices.Common.Codec;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    public class DoNotDisturbHandler : IHasDoNotDisturbMode
    {
        private readonly IKeyed _parent;
        private readonly IBasicCommunication _coms;
        private readonly GenericQueue _handler;

        private bool _doNotDisturbEnabled;

        public DoNotDisturbHandler(IKeyed parent, IBasicCommunication coms, GenericQueue handler)
        {
            _parent = parent;
            _coms = coms;
            _handler = handler;
            DoNotDisturbModeIsOnFeedback = new BoolFeedback(() => _doNotDisturbEnabled);
            DoNotDisturbModeIsOnFeedback.OutputChange +=
                (sender, args) => parent.LogDebug("Do Not Disturb:{0}", _doNotDisturbEnabled);
        }

        public void ParseStatus(JToken token)
        {
            const string doNotDisturbKey = "DoNotDisturb.Value";

            var doNoDistubToken = (string)token.SelectToken(doNotDisturbKey);
            if (string.IsNullOrEmpty(doNoDistubToken))
                return;

            _doNotDisturbEnabled = doNoDistubToken.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                                                   doNoDistubToken.Equals("On", StringComparison.OrdinalIgnoreCase);

            DoNotDisturbModeIsOnFeedback.FireUpdate();
        }

        public void ActivateDoNotDisturbMode()
        {
            _coms.SendText("xCommand Conference DoNotDisturb Activate\r");
        }

        public void DeactivateDoNotDisturbMode()
        {
            _coms.SendText("xCommand Conference DoNotDisturb Deactivate\r");
        }

        public void ToggleDoNotDisturbMode()
        {
            if (_doNotDisturbEnabled)
            {
                DeactivateDoNotDisturbMode();
            }
            else
            {
                ActivateDoNotDisturbMode();
            }
        }

        public BoolFeedback DoNotDisturbModeIsOnFeedback { get; private set; }
    }
}