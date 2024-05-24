namespace Content.Server.Radio.Components;

/// <summary>
/// Entities with <see cref="MessengerServerComponent"/> are needed to transmit messages using PDAs.
/// They also need to be powered by <see cref="ApcPowerReceiverComponent"/>
/// in order for them to work on the same map as server.
/// </summary>
[RegisterComponent]
public sealed partial class MessengerServerComponent : Component
{
    [DataField]
    public EntityUid? MainServer;
}
