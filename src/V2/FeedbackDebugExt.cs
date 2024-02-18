using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace epi_videoCodec_ciscoExtended.V2
{
    public static class FeedbackDebugExt
    {
        public static void RegisterForDebug(this Feedback feedback, IKeyed parent)
        {
            feedback.OutputChange += (sender, args) =>
            {
                if (sender is BoolFeedback)
                    Debug.Console(1, parent, "Received {0} Update : '{1}'", feedback.Key, args.BoolValue);

                if (sender is IntFeedback)
                    Debug.Console(1, parent, "Received {0} Update : '{1}'", feedback.Key, args.IntValue);

                if (sender is StringFeedback)
                    Debug.Console(1, parent, "Received {0} Update : '{1}'", feedback.Key, args.StringValue);
            };
        }
    }
}