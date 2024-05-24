using Content.Shared.MassMedia.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.MassMedia.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMessengerSystem)), AutoGenerateComponentPause]
public sealed partial class MessengerClientComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public MessengerProfileData UserProfile = new(0, string.Empty, new SpriteSpecifier.Texture(new("/Textures/Interface/Misc/job_icons.rsi/Unknown.png")), string.Empty);

    [DataField]
    [AutoNetworkedField]
    public List<MessengerProfileData> CachedProfiles = new();

    [DataField]
    [AutoNetworkedField]
    public List<MessengerMessageData?> CachedMessages = new();

    [DataField]
    [AutoNetworkedField]
    public bool Error;

    /// <summary>
    /// The time at which the cooldown for refreshing the contact list
    /// and message list will end.
    /// </summary>
    [DataField]
    [AutoPausedField]
    [AutoNetworkedField]
    public TimeSpan RefreshCooldownEnd = TimeSpan.Zero;

    /// <summary>
    /// The cooldown between refreshing contacts and messages.
    /// </summary>
    [DataField]
    public TimeSpan RefreshCooldownLength = TimeSpan.FromSeconds(SharedMessengerSystem.MessengerUpdateRate);
}
