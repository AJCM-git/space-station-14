using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Humanoid.Systems;

public sealed partial class HumanoidAppearanceSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    private void OnVerbsRequest(Entity<HumanoidAppearanceComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        if (!_adminManager.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun))
            return;

        args.Verbs.Add(new Verb
        {
            Text = "Modify markings",
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Customization/reptilian_parts.rsi"), "tail_smooth"),
            Act = () =>
            {
                _uiSystem.OpenUi(ent.Owner, HumanoidMarkingModifierKey.Key, actor.PlayerSession);
                _uiSystem.SetUiState(
                    ent.Owner,
                    HumanoidMarkingModifierKey.Key,
                    new HumanoidMarkingModifierState(ent.Comp.MarkingSet, ent.Comp.Species,
                        ent.Comp.Sex,
                        ent.Comp.SkinColor,
                        ent.Comp.CustomBaseLayers
                    ));
            }
        });
    }

    private void OnBaseLayersSet(Entity<HumanoidAppearanceComponent> ent, ref HumanoidMarkingModifierBaseLayersSetMessage message)
    {
        if (!_adminManager.HasAdminFlag(message.Actor, AdminFlags.Fun))
            return;

        if (message.Info == null)
            ent.Comp.CustomBaseLayers.Remove(message.Layer);
        else
            ent.Comp.CustomBaseLayers[message.Layer] = message.Info.Value;

        Dirty(ent);

        if (message.ResendState)
        {
            _uiSystem.SetUiState(
                ent.Owner,
                HumanoidMarkingModifierKey.Key,
                new HumanoidMarkingModifierState(ent.Comp.MarkingSet, ent.Comp.Species,
                        ent.Comp.Sex,
                        ent.Comp.SkinColor,
                        ent.Comp.CustomBaseLayers
                    ));
        }
    }

    private void OnMarkingsSet(Entity<HumanoidAppearanceComponent> ent, ref HumanoidMarkingModifierMarkingSetMessage message)
    {
        if (!_adminManager.HasAdminFlag(message.Actor, AdminFlags.Fun))
            return;

        ent.Comp.MarkingSet = message.MarkingSet;
        Dirty(ent);

        if (message.ResendState)
        {
            _uiSystem.SetUiState(
                ent.Owner,
                HumanoidMarkingModifierKey.Key,
                new HumanoidMarkingModifierState(ent.Comp.MarkingSet, ent.Comp.Species,
                        ent.Comp.Sex,
                        ent.Comp.SkinColor,
                        ent.Comp.CustomBaseLayers
                    ));
        }

    }
}
