using Robust.Shared.GameStates;

namespace Content.Shared.Containers.OnCollide;

/// <summary>
/// When this component is added we remove everything from the container
/// when the entity collides
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(RemoveFromContainerOnCollideSystem))]
public sealed class RemoveFromContainerOnCollideComponent : Component
{
    /// <summary>
    /// ID of the target container
    /// </summary>
    [DataField("container", required: true)]
    [ViewVariables]
    public string Container = default!;

    /// <summary>
    /// Min velocity we need to remove everything in the container.
    /// Represented in meters/tiles per second
    /// </summary>
    [DataField("requiredVelocity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float RequiredVelocity;

    /// <summary>
    /// Whether or not try to remove everything inside the container
    /// Only remove one thing if false
    /// </summary>
    [DataField("removeEverything")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool RemoveEverything = true;
}
