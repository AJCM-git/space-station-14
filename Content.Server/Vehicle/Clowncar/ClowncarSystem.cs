using Content.Shared.Buckle.Components;
using Content.Shared.Vehicle.Clowncar;

namespace Content.Server.Vehicle.Clowncar;

public sealed class ClowncarSystem : SharedClowncarSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClowncarComponent, BuckleChangeEvent>(OnBuckleChanged);
    }

    private void OnBuckleChanged(EntityUid uid, ClowncarComponent component, ref BuckleChangeEvent args)
    {
        var user = args.BuckledEntity;
        if (args.Buckling)
        {
            component.CannonAction.Toggled = component.CannonEntity != null;
            ActionsSystem.AddAction(user, component.CannonAction, uid);
        }
        else
        {
            ActionsSystem.RemoveAction(user, component.CannonAction);
            if (component.CannonEntity != null)
                ToggleCannon(uid, component, args.BuckledEntity, false);
        }
    }
}
