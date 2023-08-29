using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Containers;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(TransportWithContainedSystem))]
public sealed class TransportWithContainedComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField("targetCoordinates")]
    public readonly EntityCoordinates? TargetCoordiantes;

    /// <summary>
    /// How much time we want to be in FTL, if null or 0 the travel will be instant
    /// </summary>
    [DataField("ftlDuration")]
    public readonly TimeSpan? FTLDuration;

    /// <summary>
    /// After an entity enters the container, how much time will it take to start going to the destination
    /// </summary>
    [DataField("transportAfter")] public readonly TimeSpan? TransportAfter = TimeSpan.FromSeconds(5f);

    /// <summary>
    ///
    /// </summary>
    [DataField("transportAt")]
    public TimeSpan? TransportAt;
}
