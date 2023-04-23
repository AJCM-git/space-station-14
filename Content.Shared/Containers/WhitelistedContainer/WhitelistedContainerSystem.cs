using Robust.Shared.Containers;

namespace Content.Shared.Containers.WhitelistedContainer;

public sealed class WhitelistedContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhitelistedContainerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<WhitelistedContainerComponent, ContainerIsInsertingAttemptEvent>(OnContainerIsInsertingAttempt);
        SubscribeLocalEvent<WhitelistedContainerComponent, ContainerIsRemovingAttemptEvent>(OnContainerIsRemovingAttempt);
    }

    /// <summary>
    /// Ensure the containers.
    /// </summary>
    private void OnComponentInit(EntityUid uid, WhitelistedContainerComponent component, ComponentInit args)
    {
        foreach (var (id, container) in component.Containers)
        {
            container.BaseContainer = _containers.EnsureContainer<Container>(uid, id);
        }
    }

    /// <summary>
    /// Cancel removal from the container if the removed entity is not part of the whitelist then play a sound
    /// </summary>
    private void OnContainerIsRemovingAttempt(EntityUid uid, WhitelistedContainerComponent component, ContainerIsRemovingAttemptEvent args)
    {
        if (!component.Containers.TryGetValue(args.Container.ID, out var container))
            return;

        // var whitelistSuccess =  container.WhitelistedEntities != null && container.WhitelistedEntities.IsValid(args.EntityUid, EntityManager);
        // var blacklistSuccess =  container.BlacklistedEntities != null && container.BlacklistedEntities.IsValid(args.EntityUid, EntityManager);
        //
        // switch (whitelistSuccess)
        // {
        //     case true:
        //         // Whitelist takes priority
        //         break;
        //     case false when blacklistSuccess:
        //         args.Cancel();
        //         return;
        // }

        // Logger.Debug("a");
        // if (container.WhitelistedEntities == null
        //     || !container.WhitelistedEntities.IsValid(args.EntityUid, EntityManager))
        // {
        //     args.Cancel();
        //     return;
        // }

        if (container.RemoveSounds == null)
            return;

        foreach (var sound in container.RemoveSounds)
        {
            _audioSystem.PlayPredicted(sound, Transform(uid).Coordinates, uid);
        }
    }

    /// <summary>
    /// Cancel insertion in the container if the inserting entity does not pass the whitelist
    /// or the container is full then play a sound
    /// </summary>
    private void OnContainerIsInsertingAttempt(EntityUid uid, WhitelistedContainerComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!component.Containers.TryGetValue(args.Container.ID, out var container))
            return;

        var isContainerFull = container.BaseContainer?.ContainedEntities.Count >= container.Capacity;
        var whitelistSuccess = !isContainerFull && container.WhitelistedEntities != null && container.WhitelistedEntities.IsValid(args.EntityUid, EntityManager);
        var blacklistSuccess = container.BlacklistedEntities != null && container.BlacklistedEntities.IsValid(args.EntityUid, EntityManager);

        switch (whitelistSuccess)
        {
            case true:
                // Whitelist takes priority
                break;
            case false when blacklistSuccess:
                args.Cancel();
                return;
        }

        if (container.InsertSounds == null)
            return;

        foreach (var sound in container.InsertSounds)
        {
            _audioSystem.PlayPredicted(sound, Transform(uid).Coordinates, uid);
        }
    }
}
