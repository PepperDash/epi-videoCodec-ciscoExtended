using System.Collections.Generic;
using PepperDash.Core;

namespace PDT.Plugins.Cisco.RoomOs.V2
{
    public abstract class CiscoRoomOsFeature : IKeyed
    {
        protected CiscoRoomOsFeature(string key)
        {
            Key = key;
        }

        public abstract IEnumerable<string> Polls { get; }
        public abstract IEnumerable<string> Subscriptions { get; }
        public string Key { get; private set; }
        public abstract bool HandlesResponse(string response);
        public abstract void HandleResponse(string response);
    }
}