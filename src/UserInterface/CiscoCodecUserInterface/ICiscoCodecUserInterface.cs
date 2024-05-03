using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions;
using PepperDash.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface
{
    public interface ICiscoCodecUserInterface : IKeyed
    {
        CiscoCodec UisCiscoCodec { get; }
        CiscoCodecUserInterfaceConfig ConfigProps { get; }
        IVideoCodecUiExtensionsHandler VideoCodecUiExtensionsHandler { get; }
        ICiscoCodecUiExtensions UiExtensions { get; }
    }
}
