using System.Collections.Generic;
using PepperDash.Core;

namespace epi_videoCodec_ciscoExtended.V2
{
    public abstract class CiscoRoomOsFeature : IKeyed
    {
        protected CiscoRoomOsFeature(string key)
        {
            Key = key;
        }

        public string Key { get; private set; }
    }

    public interface IHasPolls : IKeyed
    {
        IEnumerable<string> Polls { get; }
    }

    public interface IHasEventSubscriptions : IKeyed
    {
        IEnumerable<string> Subscriptions { get; }
    }

    public interface IHandlesResponses : IKeyed
    {
        bool HandlesResponse(string response);
        void HandleResponse(string response);
    }
}