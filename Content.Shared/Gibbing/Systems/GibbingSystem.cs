using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Gibbing.Components;
using Content.Shared.Gibbing.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Gibbing.Systems;

public sealed class GibbingSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;

    private const float CleanupCooldown = 3f;
    private TimeSpan _cleanupTime;
    private int _maxGiblets;

    public override void Initialize()
    {
        Subs.CVar(_cfg, CCVars.MaxGiblets, max => _maxGiblets = max, true);
    }

    public override void Update(float frameTime)
    {
        if (_maxGiblets == 0)
            return;

        if (_timing.CurTime < _cleanupTime)
            return;

        _cleanupTime = _timing.CurTime + TimeSpan.FromSeconds(CleanupCooldown);

        // TODO BODY Change with a query
        List<EntityUid> GibletsEntities = new();
        var gibletsCount = GibletsEntities.Count;
        var excess = gibletsCount - _maxGiblets;
        if (excess < 1)
            return;

        for (var i = 0; i < excess; i++)
        {
            var toDelete = GibletsEntities[gibletsCount - excess + i];
            Del(toDelete);
        }
    }

    /// <summary>
    /// Attempt to gib a specified entity. That entity must have a gibbable components. This method is NOT recursive will only
    /// work on the target and any entities it contains (depending on gibContentsOption)
    /// </summary>
    /// <param name="outerEntity">The outermost entity we care about, used to place the dropped items</param>
    /// <param name="gibbable">Target entity/comp we wish to gib</param>
    /// <param name="droppedEntities">a hashset containing all the entities that have been dropped/created</param>
    /// <param name="allowedContainers">A list of containerIds on the target that permit gibing</param>
    /// <param name="excludedContainers">A list of containerIds on the target that DO NOT permit gibing</param>
    /// <param name="logMissingGibbable">Should we log if we are missing a gibbableComp when we call this function</param>
    /// <remarks>Handles entities with the component</remarks>
    /// <returns>True if successful, false if not</returns>
    public bool TryGibEntity(EntityUid outerEntity, Entity<GibbableComponent> gibbable, out HashSet<EntityUid> droppedEntities,
         List<string>? allowedContainers = null, List<string>? excludedContainers = null, bool logMissingGibbable = false)
    {
        return TryGibEntity(outerEntity, (gibbable.Owner, gibbable.Comp), gibbable.Comp.GibType, gibbable.Comp.GibContentsOption, out droppedEntities,
            gibbable.Comp.LaunchOptions, gibbable.Comp.RandomSpreadMod, gibbable.Comp.GibSound != null,
            allowedContainers, excludedContainers, logMissingGibbable);
    }

    /// <inheritdoc cref="TryGibEntity(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.Entity{Content.Shared.Gibbing.Components.GibbableComponent},out System.Collections.Generic.HashSet{Robust.Shared.GameObjects.EntityUid},System.Collections.Generic.List{string}?,System.Collections.Generic.List{string}?,bool)"/>
    /// <param name="gibType">What type of gibing are we performing</param>
    /// <param name="gibContentsOption">What type of gibing do we perform on any container contents?</param>
    /// <param name="launchOptions">Dictates if we should launch the gibs, how much impulse to we apply to them, etc</param>
    /// <param name="randomSpreadMod">How much to multiply the random spread on dropped giblets(if we are dropping them!)</param>
    /// <param name="playAudio">Should we play audio</param>
    /// <remarks>Handles entities without the component</remarks>
    public bool TryGibEntity(EntityUid outerEntity, Entity<GibbableComponent?> gibbable, GibType gibType, GibContentsOption gibContentsOption,
        out HashSet<EntityUid> droppedEntities, GibLaunchOptions launchOptions, float randomSpreadMod = 1.0f, bool playAudio = true,
        List<string>? allowedContainers = null, List<string>? excludedContainers = null, bool logMissingGibbable = false)
    {
        droppedEntities = new();
        return TryGibEntityWithRef(outerEntity, (gibbable.Owner, gibbable.Comp), gibType, gibContentsOption, ref droppedEntities,
            launchOptions, randomSpreadMod, playAudio, allowedContainers, excludedContainers, logMissingGibbable);
    }

    /// <inheritdoc cref="TryGibEntity(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.Entity{Content.Shared.Gibbing.Components.GibbableComponent},Content.Shared.Gibbing.Events.GibType,Content.Shared.Gibbing.Events.GibContentsOption,out System.Collections.Generic.HashSet{Robust.Shared.GameObjects.EntityUid},Content.Shared.Gibbing.Events.GibLaunchOptions,Robust.Shared.Maths.Angle,float,bool,System.Collections.Generic.List{string}?,System.Collections.Generic.List{string}?,bool)"/>
    public bool TryGibEntityWithRef(
        EntityUid outerEntity,
        Entity<GibbableComponent?> gibbable,
        GibType gibType,
        GibContentsOption gibContentsOption,
        ref HashSet<EntityUid> droppedEntities,
        GibLaunchOptions launchOptions,
        float randomSpreadMod = 1.0f,
        bool playAudio = true,
        List<string>? allowedContainers = null,
        List<string>? excludedContainers = null,
        bool logMissingGibbable = false)
    {
        //TODO: Placeholder for testing! Replace with proper bodypart gibbing implementation!
        if (!Resolve(gibbable, ref gibbable.Comp, logMissing: false))
        {
            droppedEntities = new();
            DropEntity(gibbable, Transform(outerEntity), randomSpreadMod, ref droppedEntities, launchOptions);
            if (logMissingGibbable)
            {
                Log.Warning($"{ToPrettyString(gibbable)} does not have a GibbableComponent! " +
                            $"This is not required but may cause issues contained items to not be dropped.");
            }

            return false;
        }

        if (gibType == GibType.Skip && gibContentsOption == GibContentsOption.Skip)
            return true;

        if (launchOptions.Launch)
            randomSpreadMod = 0;

        HashSet<BaseContainer> validContainers = new();
        var gibContentsAttempt =
            new AttemptEntityContentsGibEvent(gibbable, gibContentsOption, allowedContainers, excludedContainers);
        RaiseLocalEvent(gibbable, ref gibContentsAttempt);

        foreach (var container in _containerSystem.GetAllContainers(gibbable))
        {
            var valid = true;
            if (allowedContainers != null)
                valid = allowedContainers.Contains(container.ID);
            if (excludedContainers != null)
                valid = valid && !excludedContainers.Contains(container.ID);
            if (valid)
                validContainers.Add(container);
        }

        var parentXform = Transform(outerEntity);
        switch (gibType)
        {
            case GibType.Skip:
                break;

            case GibType.Drop:
                DropEntity((gibbable.Owner, gibbable.Comp), parentXform, randomSpreadMod, ref droppedEntities, launchOptions);
                break;

            case GibType.Gib:
                GibEntity((gibbable.Owner, gibbable.Comp), parentXform, randomSpreadMod, ref droppedEntities, launchOptions);
                break;
        }

        switch (gibContentsOption)
        {
            case GibContentsOption.Skip:
                break;

            case GibContentsOption.Drop:
                foreach (var container in validContainers)
                {
                    foreach (var ent in container.ContainedEntities)
                    {
                        DropEntity((ent, null), parentXform, randomSpreadMod, ref droppedEntities, launchOptions);
                    }
                }
                break;

            case GibContentsOption.Gib:
                foreach (var container in validContainers)
                {
                    foreach (var ent in container.ContainedEntities)
                    {
                        GibEntity((ent, null), parentXform, randomSpreadMod, ref droppedEntities, launchOptions);
                    }
                }
                break;
        }

        if (playAudio)
            _audioSystem.PlayPredicted(gibbable.Comp.GibSound, parentXform.Coordinates, null);

        if (gibType == GibType.Gib)
            QueueDel(gibbable);

        return true;
    }

    private void DropEntity(Entity<GibbableComponent?> gibbable, TransformComponent parentXform, float randomSpreadMod,
        ref HashSet<EntityUid> droppedEntities, GibLaunchOptions launchOptions)
    {
        var gibCount = 0;
        if (Resolve(gibbable, ref gibbable.Comp, logMissing: false))
            gibCount = gibbable.Comp.GibCount;

        var gibAttemptEvent = new AttemptEntityGibEvent(gibbable, gibCount, GibType.Drop);
        RaiseLocalEvent(gibbable, ref gibAttemptEvent);
        switch (gibAttemptEvent.GibType)
        {
            case GibType.Skip:
                return;

            case GibType.Gib:
                GibEntity(gibbable, parentXform, randomSpreadMod, ref droppedEntities, launchOptions, deleteTarget: false);
                return;
        }

        _transformSystem.AttachToGridOrMap(gibbable);
        _transformSystem.SetCoordinates(gibbable, parentXform.Coordinates);
        _transformSystem.SetWorldRotation(gibbable, _random.NextAngle());
        droppedEntities.Add(gibbable);

        if (launchOptions.Launch)
            FlingDroppedEntity(gibbable, launchOptions);

        var gibbedEvent = new EntityGibbedEvent(gibbable, [gibbable]);
        RaiseLocalEvent(gibbable, ref gibbedEvent);
    }

    private List<EntityUid> GibEntity(Entity<GibbableComponent?> gibbable, TransformComponent parentXform,
        float randomSpreadMod,
        ref HashSet<EntityUid> droppedEntities, GibLaunchOptions launchOptions, bool deleteTarget = true)
    {
        var localGibs = new List<EntityUid>();
        var gibCount = 0;
        var gibProtoCount = 0;
        if (Resolve(gibbable, ref gibbable.Comp, logMissing: false))
        {
            gibCount = gibbable.Comp.GibCount;
            gibProtoCount = gibbable.Comp.GibPrototypes.Count;
        }

        var gibAttemptEvent = new AttemptEntityGibEvent(gibbable, gibCount, GibType.Drop);
        RaiseLocalEvent(gibbable, ref gibAttemptEvent);
        switch (gibAttemptEvent.GibType)
        {
            case GibType.Skip:
                return localGibs;

            case GibType.Drop:
                DropEntity(gibbable, parentXform, randomSpreadMod, ref droppedEntities, launchOptions);
                localGibs.Add(gibbable);
                return localGibs;
        }

        if (gibbable.Comp != null && gibProtoCount > 0)
        {
            if (launchOptions.Launch)
            {
                for (var i = 0; i < gibAttemptEvent.GibletCount; i++)
                {
                    if (!TryCreateRandomGiblet(gibbable.Comp, parentXform.Coordinates, false, out var giblet,
                            randomSpreadMod))
                        continue;
                    FlingDroppedEntity(giblet.Value, launchOptions);
                    droppedEntities.Add(giblet.Value);
                }
            }
            else
            {
                for (var i = 0; i < gibAttemptEvent.GibletCount; i++)
                {
                    if (TryCreateRandomGiblet(gibbable.Comp, parentXform.Coordinates, false, out var giblet,
                            randomSpreadMod))
                        droppedEntities.Add(giblet.Value);
                }
            }
        }

        _transformSystem.AttachToGridOrMap(gibbable, Transform(gibbable));
        if (launchOptions.Launch)
        {
            FlingDroppedEntity(gibbable, launchOptions);
        }

        var gibbedEvent = new EntityGibbedEvent(gibbable, localGibs);
        RaiseLocalEvent(gibbable, ref gibbedEvent);
        if (deleteTarget)
            QueueDel(gibbable);
        return localGibs;
    }


    public bool TryCreateRandomGiblet(Entity<GibbableComponent?> gibbable, [NotNullWhen(true)] out EntityUid? gibletEntity,
        float randomSpreadModifier = 1.0f, bool playSound = true)
    {
        gibletEntity = null;
        return Resolve(gibbable, ref gibbable.Comp) && TryCreateRandomGiblet(gibbable.Comp, Transform(gibbable).Coordinates,
            playSound, out gibletEntity, randomSpreadModifier);
    }

    public bool TryCreateAndFlingRandomGiblet(Entity<GibbableComponent?> gibbable, [NotNullWhen(true)] out EntityUid? gibletEntity,
        GibLaunchOptions launchOptions, bool playSound = true)
    {
        gibletEntity = null;
        if (!Resolve(gibbable, ref gibbable.Comp) ||
            !TryCreateRandomGiblet(gibbable.Comp, Transform(gibbable).Coordinates, playSound, out gibletEntity))
            return false;

        FlingDroppedEntity(gibletEntity.Value, launchOptions);
        return true;
    }

    private void FlingDroppedEntity(EntityUid target, GibLaunchOptions launchOptions)
    {
        var scatterAngle = launchOptions.Direction?.ToAngle() ?? _random.NextAngle();
        var scatterVector = _random.NextAngle(scatterAngle - launchOptions.ScatterCone / 2, scatterAngle + launchOptions.ScatterCone / 2)
            .ToVec() * (launchOptions.Impulse + _random.NextFloat(launchOptions.ImpulseVariance));
        _physicsSystem.ApplyLinearImpulse(target, scatterVector);
    }

    private bool TryCreateRandomGiblet(GibbableComponent gibbable, EntityCoordinates coords,
        bool playSound, [NotNullWhen(true)] out EntityUid? gibletEntity, float? randomSpreadModifier = null)
    {
        gibletEntity = null;
        if (gibbable.GibPrototypes.Count == 0)
            return false;
        gibletEntity = Spawn(gibbable.GibPrototypes[_random.Next(0, gibbable.GibPrototypes.Count)],
            randomSpreadModifier == null
                ? coords
                : coords.Offset(_random.NextVector2(gibbable.GibScatterRange * randomSpreadModifier.Value)));
        if (playSound)
            _audioSystem.PlayPredicted(gibbable.GibSound, coords, null);
        _transformSystem.SetWorldRotation(gibletEntity.Value, _random.NextAngle());
        return true;
    }
}
