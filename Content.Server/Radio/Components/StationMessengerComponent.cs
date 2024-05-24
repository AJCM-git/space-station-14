using Content.Server.CartridgeLoader.Cartridges;
using Content.Shared.MassMedia.Components;

namespace Content.Server.Radio.Components;

[RegisterComponent]
[Access(typeof(MessengerSystem))]
public sealed partial class StationMessengerComponent : Component
{
    /// <summary>
    /// The list of the users that this cartridge has available
    /// </summary>
    [DataField]
    public List<MessengerProfileData> Profiles = [];

    /// <summary>
    /// The list of messages that this cartridge has stored
    /// </summary>
    [DataField]
    public List<MessengerMessageData> Messages = [];

}
