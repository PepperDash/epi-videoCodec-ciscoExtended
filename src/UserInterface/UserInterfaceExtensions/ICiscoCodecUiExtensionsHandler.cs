using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions
{
    public interface ICiscoCodecUiExtensionsHandler
    {
        /// <summary>
        /// Called by receive parser to parse event feedback
        /// </summary>
        /// <param name="panel"></param>
        void ParseStatus(Panels.CiscoCodecEvents.Panel panel);
    }
}
