using System.Collections.Generic;
using PepperDash.Essentials.Core;
using Feedback = PepperDash.Essentials.Core.Feedback;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
  #region

  public class FeedbackGroup
  {
    private readonly FeedbackCollection<Feedback> _feedbacks;

    public FeedbackGroup(IEnumerable<Feedback> feedbacks)
    {
      _feedbacks = new FeedbackCollection<Feedback>();
      _feedbacks.AddRange(feedbacks);
    }

    public void FireUpdate()
    {
      foreach (var f in _feedbacks)
      {
        var feedback = f;
        feedback.FireUpdate();
      }
    }
  }

  #endregion
}
