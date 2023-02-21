using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using PepperDash.Core;

namespace epi_videoCodec_ciscoExtended.State
{
    public static class Registration
    {
        public static IDictionary<string, object> RegistrationStrings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "xFeedback register /Status/Audio", new object() },
                            { "xFeedback register /Status/Call", new object() },
                            { "xFeedback register /Status/Conference/Presentation", new object() },
                            { "xFeedback register /Status/Conference/Call/AuthenticationRequest", new object() },
                            { "xFeedback register /Status/Conference/DoNotDisturb", new object() },
                            { "xFeedback register /Status/Cameras/SpeakerTrack", new object() },
                            { "xFeedback register /Status/Cameras/SpeakerTrack/Status", new object() },
                            { "xFeedback register /Status/Cameras/SpeakerTrack/Availability", new object() },
                            { "xFeedback register /Status/Cameras/PresenterTrack", new object() },
                            { "xFeedback register /Status/Cameras/PresenterTrack/Status", new object() },
                            { "xFeedback register /Status/Cameras/PresenterTrack/Availability", new object() },
                            { "xFeedback register /Status/RoomAnalytics", new object() },
                            { "xFeedback register /Status/RoomPreset", new object() },
                            { "xFeedback register /Status/Standby", new object() },
                            { "xFeedback register /Status/Video/Selfview", new object() },
                            { "xFeedback register /Status/MediaChannels/Call", new object() },
                            { "xFeedback register /Status/Video/Layout/CurrentLayouts", new object() },
                            { "xFeedback register /Status/Video/Layout/LayoutFamily", new object() },
                            { "xFeedback register /Status/Video/Input/MainVideoMute", new object() },
                            { "xFeedback register /Bookings", new object() },
                            { "xFeedback register /Event/Bookings", new object() },
                            { "xFeedback register /Event/CameraPresetListUpdated", new object() },
                            { "xFeedback register /Event/Conference/Call/AuthenticationResponse", new object() },
                            { "xFeedback register /Event/UserInterface/Presentation/ExternalSource/Selected/SourceIdentifier", new object() },
                            { "xFeedback register /Event/UserInterface/Extensions/Event", new object() },
                            { "xFeedback register /Event/UserInterface/Extensions/PageOpened", new object() },
                            { "xFeedback register /Event/UserInterface/Extensions/PageClosed", new object() },
                            { "xFeedback register /Event/UserInterface/Extensions/Widget/LayoutUpdated", new object() },
                            { "xFeedback register /Event/CallDisconnect", new object() },
                            { "xFeedback register /Configuration", new object() },
                        };

        public static void DispatchRegistrations(IBasicCommunication communication)
        {
            using (var waitHandle = new CEvent(true, true))
            {
                foreach (var registrationString in RegistrationStrings)
                {
                    communication.SendText(registrationString.Key + "\r");
                    waitHandle.Wait(20);
                }
            }
        }

        public static bool ContainsRegistrationString(string value)
        {
            return RegistrationStrings.ContainsKey(value);
        }
    }
}