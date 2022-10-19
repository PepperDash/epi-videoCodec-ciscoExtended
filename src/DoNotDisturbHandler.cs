﻿using System;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;

namespace epi_videoCodec_ciscoExtended
{
    public class DoNotDisturbHandler : IHasDoNotDisturbMode
    {
        private readonly IKeyed _parent;
        private readonly IBasicCommunication _coms;
        private readonly MessageProcessor _handler;

        private bool _doNotDisturbEnabled;

        public DoNotDisturbHandler(IKeyed parent, IBasicCommunication coms, MessageProcessor handler)
        {
            _parent = parent;
            _coms = coms;
            _handler = handler;
            DoNotDisturbModeIsOnFeedback = new BoolFeedback(() => _doNotDisturbEnabled);
            DoNotDisturbModeIsOnFeedback.OutputChange +=
                (sender, args) => Debug.Console(1, parent, "Do Not Disturb:{0}", _doNotDisturbEnabled);
        }

        public void ParseStatus(JToken token)
        {
            const string doNotDisturbKey = "DoNotDisturb.Value";

            var doNoDistubToken = (string)token.SelectToken(doNotDisturbKey);
            if (String.IsNullOrEmpty(doNoDistubToken))
                return;

            _handler.PostMessage(() =>
            {
                _doNotDisturbEnabled = doNoDistubToken.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                                       doNoDistubToken.Equals("On", StringComparison.OrdinalIgnoreCase);

                DoNotDisturbModeIsOnFeedback.FireUpdate();
            });
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