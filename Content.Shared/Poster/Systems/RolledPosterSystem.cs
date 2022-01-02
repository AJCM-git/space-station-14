using System.Collections.Generic;
using Content.Shared.Poster.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using JetBrains.Annotations;

namespace Content.Shared.Poster.Systems;

[UsedImplicitly]
public class RolledPosterSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protManager = default!;

    private List<EntityPrototype> candidatesList = new List<EntityPrototype>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RolledPosterComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<RolledPosterComponent, AfterInteractEvent>(OnAfterInteract);
        _protManager.PrototypesReloaded += OnPrototypesReloaded;

        foreach (var prot in _protManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (prot.TryGetComponent<TagComponent>("Tag", out var tag) && tag.HasAnyTag("PosterContraband", "PosterLegit"))
                {
                    candidatesList.Add(prot);
                }
        }
    }

    private void OnComponentStartup (EntityUid uid, RolledPosterComponent component, ComponentStartup args)
    {
        var prot = IoCManager.Resolve<IRobustRandom>().Pick(candidatesList);

        if (prot.TryGetComponent<TagComponent>("Tag", out var tag) &&
            component.Key != null && tag.HasTag(component.Key) && prot.Abstract != true)
        {
            component.Spawn = prot;

            var selected = component.Spawn;
            var uidMetadata = Comp<MetaDataComponent>(uid);
            uidMetadata.EntityName = $"{selected.Name} rolled poster";
            uidMetadata.EntityDescription = selected.Description;
            return;
        }
    }

    private void OnAfterInteract(EntityUid uid, RolledPosterComponent component, AfterInteractEvent args)
    {
        if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(args.User) ||
            !args.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true) ||
            !_mapManager.TryGetGrid(args.ClickLocation.GetGridId(_entMan), out var grid))
            return;

        var coordsSnapPos = grid.GridTileToLocal(grid.TileIndicesFor(args.ClickLocation));
        Spawn(component.Spawn.ID, coordsSnapPos);
        Del(uid);
    }


    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.ByType.TryGetValue(typeof(EntityPrototype), out var set))
            return;

        candidatesList.RemoveAll(prot => set.Modified.ContainsKey(prot.ID));

        foreach (var prot in set.Modified.Values)
        {
            candidatesList.Add((EntityPrototype) prot);
        }
    }

//     private void UpdateVisualizer()
//     {
//         _appearance?.SetData(ExpendableLightVisuals.State, CurrentState);

//         switch (CurrentState)
//         {
//             case ExpendableLightState.Lit:
//                 _appearance?.SetData(ExpendableLightVisuals.Behavior, TurnOnBehaviourID);
//                 break;

//             case ExpendableLightState.Fading:
//                 _appearance?.SetData(ExpendableLightVisuals.Behavior, FadeOutBehaviourID);
//                 break;

//             case ExpendableLightState.Dead:
//                 _appearance?.SetData(ExpendableLightVisuals.Behavior, string.Empty);
//                 break;
//         }
//     }
}
