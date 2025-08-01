﻿using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Devices.Common.Codec.Cisco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces
{
    /// <summary>
    /// Messenger class for handling PresenterTrack functionality communication.
    /// Provides messaging infrastructure for PresenterTrack commands and status updates.
    /// </summary>
    internal class IPresenterTrackMessenger : MessengerBase
    {
        private readonly IPresenterTrack _presenterTrack; 
        
        /// <summary>
        /// Initializes a new instance of the IPresenterTrackMessenger class.
        /// </summary>
        /// <param name="key">The unique key for this messenger.</param>
        /// <param name="messagePath">The message path for communication.</param>
        /// <param name="presenterTrack">The PresenterTrack device to manage.</param>
        public IPresenterTrackMessenger(string key, string messagePath, IPresenterTrack presenterTrack)
            : base(key, messagePath, presenterTrack as Device)
        {
            _presenterTrack = presenterTrack;
        }


        protected override void RegisterActions()
        {
            if(_presenterTrack == null)
            {
                Debug.LogMessage(Serilog.Events.LogEventLevel.Error, $"{Key} does not implement IPresenterTrack", this, _presenterTrack.Key);
                return;
            }

            AddAction("/fullStatus", (id, content) => SendFullStatus());

            AddAction($"/presenterTrackOff", (id, context) => _presenterTrack.PresenterTrackOff());

            AddAction($"/presenterTrackFollow", (id, context) => _presenterTrack.PresenterTrackFollow());

            AddAction($"/presenterTrackBackground", (id, context) => _presenterTrack.PresenterTrackBackground());

            AddAction($"/presenterTrackPersistent", (id, context) => _presenterTrack.PresenterTrackPersistent());

            _presenterTrack.PresenterTrackAvailableFeedback.OutputChange += (o, a) => SendFullStatus();
            _presenterTrack.PresenterTrackStatusOffFeedback.OutputChange += (o, a) => SendFullStatus();
            _presenterTrack.PresenterTrackStatusFollowFeedback.OutputChange += (o, a) => SendFullStatus();
            _presenterTrack.PresenterTrackStatusBackgroundFeedback.OutputChange += (o, a) => SendFullStatus();
            _presenterTrack.PresenterTrackStatusPersistentFeedback.OutputChange += (o, a) => SendFullStatus();
        }

        private void SendFullStatus()
        {
            var message = new IPresenterTrackStateMessage
            {
                PresenterTrackAvailable = _presenterTrack.PresenterTrackAvailableFeedback.BoolValue,
                PresenterTrackStatusOff = _presenterTrack.PresenterTrackStatusOffFeedback.BoolValue,
                PresenterTrackStatusFollow = _presenterTrack.PresenterTrackStatusFollowFeedback.BoolValue,
                PresenterTrackStatusBackground = _presenterTrack.PresenterTrackStatusBackgroundFeedback.BoolValue,
                PresenterTrackStatusPersistent = _presenterTrack.PresenterTrackStatusPersistentFeedback.BoolValue
            };

            PostStatusMessage(message);
        }
    }

    public class IPresenterTrackStateMessage : DeviceStateMessageBase
    {
        public bool? PresenterTrackAvailable { get; set; }

        public bool? PresenterTrackStatusOff { get; set; }

        public bool? PresenterTrackStatusFollow { get; set; }

        public bool? PresenterTrackStatusBackground { get; set; }

        public bool? PresenterTrackStatusPersistent { get; set; }
    }
}
