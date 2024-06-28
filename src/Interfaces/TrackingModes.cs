using PepperDash.Essentials.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.Interfaces
{
    internal enum eCiscoCameraMode
    {
        SpeakerTrack,
        PresenterTrack,
        Manual
    }
    
    /// <summary>
    /// Describes the available tracking modes for a Cisco codec
    /// </summary>
    internal interface ISpeakerTrack
    {
        bool SpeakerTrackAvailability { get; }

        BoolFeedback SpeakerTrackAvailableFeedback { get; }

        bool SpeakerTrackStatus { get; }

        void SpeakerTrackOff();
        void SpeakerTrackOn();
    }

    internal interface IPresenterTrack
    {
        bool PresenterTrackAvailability { get; }

        BoolFeedback PresenterTrackAvailableFeedback { get; }

        BoolFeedback PresenterTrackStatusOffFeedback { get; }
        BoolFeedback PresenterTrackStatusFollowFeedback { get; }
        BoolFeedback PresenterTrackStatusBackgroundFeedback { get; }
        BoolFeedback PresenterTrackStatusPersistentFeedback { get; }

        bool PresenterTrackStatus { get; }

        void PresenterTrackOff();
        void PresenterTrackFollow();
        void PresenterTrackBackground();
        void PresenterTrackPersistent();
    }
}
