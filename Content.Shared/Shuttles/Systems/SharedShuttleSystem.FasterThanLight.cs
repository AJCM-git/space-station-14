using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;

namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedShuttleSystem
{
    /// <summary>
    /// Moves a shuttle from its current position to the target one. Goes through the hyperspace map while the timer is running.
    /// </summary>
    public virtual void FTLTravel(EntityUid shuttleUid, ShuttleComponent? component, EntityCoordinates coordinates, float startupTime, float hyperspaceTime, string? priorityTag = null) {}

    /// <summary>
    /// Moves a shuttle from its current position to docked on the target one. Goes through the hyperspace map while the timer is running.
    /// </summary>
    public virtual void FTLTravel(EntityUid shuttleUid, ShuttleComponent? component, EntityUid target, float startupTime, float hyperspaceTime, bool dock = false, string? priorityTag = null) {}
}
