using Robust.Shared.GameStates;

namespace Content.Shared.Containers.OnCollide;

/// <summary>
/// When this component is added, any entity we collide with we insert in the container,
/// respecting its whitelist and blacklist.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AddToContainerOnCollideSystem))]
public sealed class AddToContainerOnCollideComponent : Component
{
    /// <summary>
    /// ID of the target container
    /// </summary>
    [DataField("container", required: true)]
    [ViewVariables]
    public string Container = default!;

    /// <summary>
    /// The minimum velocity we have to have to be able to insert something in the container.
    /// Represented in meters/tiles per second
    /// </summary>
    [DataField("requiredVelocity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float RequiredVelocity;
}
