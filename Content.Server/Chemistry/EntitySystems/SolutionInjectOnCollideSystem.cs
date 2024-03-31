using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Medical.Blood.Systems;
using Content.Shared.Medical.Blood.Components;
using Content.Shared.Medical.Blood.Systems;
using Content.Shared.Projectiles;
using BloodstreamComponent = Content.Shared.Medical.Blood.Components.BloodstreamComponent;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionInjectOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainersSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SolutionInjectOnCollideComponent, ProjectileHitEvent>(HandleInjection);
    }

    private void HandleInjection(Entity<SolutionInjectOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        var component = ent.Comp;
        var target = args.Target;

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream) ||
            !_solutionContainersSystem.TryGetInjectableSolution(ent.Owner, out var solution, out _))
        {
            return;
        }

        if (component.BlockSlots != 0x0)
        {
            var containerEnumerator = _inventorySystem.GetSlotEnumerator(target, component.BlockSlots);

            // TODO add a helper method for this?
            if (containerEnumerator.MoveNext(out _))
                return;
        }

        var solRemoved = _solutionContainersSystem.SplitSolution(solution.Value, component.TransferAmount);
        var solRemovedVol = solRemoved.Volume;

        var solToInject = solRemoved.SplitSolution(solRemovedVol * component.TransferEfficiency);
        //TODO: Re-implement injection on SolutionInjectOnCollideSystem
        //_bloodstreamSystem.TryAddToChemicals(target, solToInject, bloodstream);
    }
}
