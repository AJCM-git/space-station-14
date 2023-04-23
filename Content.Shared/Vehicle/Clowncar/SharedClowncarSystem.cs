using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Containers.OnCollide;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Vehicle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Vehicle.Clowncar;

/* TODO emag flashlight color change, enter do after, roll dice and cannon actions when emaged,
    explode if someone has drunk more than 30u of irish car bomb,
    spread space lube as foam on damage with a prob of 33, repair with bananas, squishing???
    popups and chat messages for bumping, crashing, repairing, irish bomb, lubing, emag, squishing, dice roll*/
    //private void OnBucklePreventCollide(EntityUid uid, BuckleComponent component, ref PreventCollideEvent args)
public abstract class SharedClowncarSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] protected readonly SharedActionsSystem ActionsSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem HandsSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem CombatSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClowncarComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ClowncarComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ClowncarCannonComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<ClowncarComponent, EntInsertedIntoContainerMessage>(OnEntInserted);

        SubscribeLocalEvent<ClowncarComponent, ThankRiderActionEvent>(OnThankRider);
        SubscribeLocalEvent<ClowncarComponent, ClowncarFireModeActionEvent>(OnClowncarFireModeAction);
    }

    private void OnComponentStartup(EntityUid uid, ClowncarComponent component, ComponentStartup args)
    {
        component.CannonAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Objects/Fun/bikehorn.rsi/icon.png")),
            DisplayName = Loc.GetString("clowncar-action-name-firemode"),
            Description = Loc.GetString("clowncar-action-desc-firemode"),
            UseDelay = component.CannonSetupDelay,
            Provider = uid,
            Event = new ClowncarFireModeActionEvent(),
        };
    }

    private void OnStartCollide(EntityUid uid, ClowncarComponent component, ref StartCollideEvent args)
    {
        if (TryComp<VehicleComponent>(uid, out var vehicle)
            && vehicle.Rider is { } rider)
        {
            var angle = _random.NextAngle().ToVec() * _random.NextFloat(1.5f, 2.5f);
            _stunSystem.TryParalyze(rider, component.StunTime, true);
            _throwingSystem.TryThrow(rider, angle, 5f, uid, 0);
        }
    }

    private void OnTakeAmmo(EntityUid uid, ClowncarCannonComponent component, TakeAmmoEvent args)
    {
        foreach (var (ammo, _)in args.Ammo)
        {
            if (ammo != null)
                _stunSystem.TryParalyze(ammo.Value, component.StunTime, true);
        }
    }

    private void OnEntInserted(EntityUid uid, ClowncarComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<VehicleComponent>(uid, out var vehicle)
            || vehicle.Rider is not {} rider)
            return;

        component.ThankRiderAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Objects/Fun/bikehorn.rsi/icon.png")),
            DisplayName = Loc.GetString("clowncar-action-name-thankrider"),
            Description = Loc.GetString("clowncar-action-desc-thankrider"),
            UseDelay = TimeSpan.FromSeconds(60),
            Speech = Loc.GetString("clowncar-thankrider", ("rider", Identity.Entity(rider, EntityManager))),
            Event = new ThankRiderActionEvent()
        };

        ActionsSystem.AddAction(args.Entity, component.ThankRiderAction, uid);
    }

    private void OnThankRider(EntityUid uid, ClowncarComponent component, ThankRiderActionEvent args)
    {
        component.ThankCounter++;
    }

    private void OnClowncarFireModeAction(EntityUid uid, ClowncarComponent component, ClowncarFireModeActionEvent args)
    {
        ToggleCannon(uid, component, args.Performer, component.CannonEntity == null);
        args.Handled = true;
    }

    protected void ToggleCannon(EntityUid uid, ClowncarComponent component, EntityUid user, bool activated)
    {
        var ourTransform = Transform(uid);
        var sound = activated ? component.CannonActivateSound : component.CannonDeactivateSound;
        _audioSystem.PlayPredicted(sound, ourTransform.Coordinates, uid, AudioParams.Default.WithVolume(5));

        AppearanceSystem.SetData(uid, ClowncarVisuals.FireModeEnabled, activated);

        switch (activated)
        {
            case true when component.CannonEntity == null:
                if (!ourTransform.Anchored)
                    _transformSystem.AnchorEntity(uid, ourTransform);

                component.CannonEntity = Spawn(component.CannonPrototype, ourTransform.Coordinates);
                if (TryComp<HandsComponent>(user, out var handsComp)
                    && HandsSystem.TryGetEmptyHand(user, out var hand, handsComp))
                {
                    HandsSystem.TryPickup(user, component.CannonEntity.Value, hand, handsComp: handsComp);
                    HandsSystem.SetActiveHand(user, hand, handsComp);
                    Comp<ContainerAmmoProviderComponent>(component.CannonEntity.Value).ProviderUid = uid;
                    CombatSystem.SetInCombatMode(user, true);
                }
                break;

            case false when component.CannonEntity != null:
                Del(component.CannonEntity.Value);
                component.CannonEntity = null;
                if (ourTransform.Anchored)
                    _transformSystem.Unanchor(uid, ourTransform);
                CombatSystem.SetInCombatMode(user, false);
                break;
        }
    }
}

public sealed class ThankRiderActionEvent : InstantActionEvent
{
}

public sealed class ClowncarFireModeActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public enum ClowncarVisuals : byte
{
    FireModeEnabled
}
