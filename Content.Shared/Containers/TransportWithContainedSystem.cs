using Content.Shared.Shuttles.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Containers;

public sealed class TransportWithContainedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedShuttleSystem _shuttleSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TransportWithContainedComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<TransportWithContainedComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var transportQuery = EntityQueryEnumerator<TransportWithContainedComponent>();
        while (transportQuery.MoveNext(out var ent, out var transport))
        {
            if (transport.FTLDuration == null || transport.FTLDuration <= TimeSpan.Zero)
                continue;

            if (_timing.CurTime == transport.TransportAt)
            {
                _shuttleSystem.FTLTravel(ent, null, Transform(ent).Coordinates, 2f, 5f);
            }
        }
    }

    private void OnEntityInserted(EntityUid uid,  TransportWithContainedComponent component, EntInsertedIntoContainerMessage args)
    {
        component.TransportAt = _timing.CurTime + component.TransportAfter;
    }

    private void OnEntityRemoved(EntityUid uid,  TransportWithContainedComponent component, EntRemovedFromContainerMessage args)
    {
        /*
        _shuttleSystem.TryStopFtl(uid);
    */
    }
}
