using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions
{
    public interface IVideoCodecUiExtensionsHandler : IVideoCodecUiExtensionsWebViewDisplayActions, IVideoCodecUiExtensionsClickedEvent
    {
    }

    public interface IVideoCodecUiExtensions
    {
        IVideoCodecUiExtensionsHandler VideoCodecUiExtensionsHandler { get; set; }
    }

}