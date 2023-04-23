using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Vehicle.Clowncar;

/// <summary>
/// This is particularly for vehicles that use
/// buckle. Stuff like clown cars may need a different
/// component at some point.
/// All vehicles should have Physics, Strap, and SharedPlayerInputMover components.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedClowncarSystem))]
public sealed class ClowncarComponent : Component
{
    [DataField("knockdownTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.FromSeconds(5f);

    [DataField("thankRiderAction")]
    [ViewVariables]
    public InstantAction ThankRiderAction = new();

    [DataField("thankCounter")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int ThankCounter;

    #region Cannon
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? CannonEntity = default!;

    [DataField("cannonPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public string CannonPrototype = "ClowncarCannon";

    [DataField("cannonAction")]
    [ViewVariables]
    public InstantAction CannonAction = new();

    [DataField("cannonSetupDelay")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CannonSetupDelay = TimeSpan.FromSeconds(2);

    [DataField("cannonRange")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float CannonRange = 30;

    #endregion

    #region Sound
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("cannonActivateSound")]
    public SoundSpecifier CannonActivateSound = new SoundPathSpecifier("/Audio/Effects/Vehicle/Clowncar/clowncar_activate_cannon.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("cannonDeactivateSound")]
    public SoundSpecifier CannonDeactivateSound = new SoundPathSpecifier("/Audio/Effects/Vehicle/Clowncar/clowncar_deactivate_cannon.ogg");

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("fartSound")]
    public SoundSpecifier FartSound = new SoundPathSpecifier("/Audio/Effects/Vehicle/Clowncar/clowncar_fart.ogg");

    #endregion
}
