using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Server.Humanoid.Systems;

public sealed partial class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
{
    [Dependency] private readonly MarkingManager _markingManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, HumanoidMarkingModifierMarkingSetMessage>(OnMarkingsSet);
        SubscribeLocalEvent<HumanoidAppearanceComponent, HumanoidMarkingModifierBaseLayersSetMessage>(OnBaseLayersSet);
        SubscribeLocalEvent<HumanoidAppearanceComponent, GetVerbsEvent<Verb>>(OnVerbsRequest);
    }

    /// <summary>
    /// Clones a humanoid's appearance to a target mob, provided they both have humanoid components.
    /// This was done enough times that it only made sense to do it here
    /// </summary>
    /// <param name="source">Source entity to fetch the original appearance from.</param>
    /// <param name="target">Target entity to apply the source entity's appearance to.</param>
    public void CloneAppearance(Entity<HumanoidAppearanceComponent?> source, Entity<HumanoidAppearanceComponent?> target)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;

        target.Comp.Species = source.Comp.Species;
        target.Comp.SkinColor = source.Comp.SkinColor;
        target.Comp.EyeColor = source.Comp.EyeColor;
        target.Comp.Age = source.Comp.Age;
        SetSex((target, target.Comp), source.Comp.Sex, false);
        target.Comp.CustomBaseLayers = new(source.Comp.CustomBaseLayers);
        target.Comp.MarkingSet = new(source.Comp.MarkingSet);

        target.Comp.Gender = source.Comp.Gender;
        if (TryComp<GrammarComponent>(target, out var grammar))
            grammar.Gender = source.Comp.Gender;

        Dirty(target);
    }

    /// <summary>
    /// Removes a marking from a humanoid by ID.
    /// </summary>
    /// <param name="ent">Humanoid mob's UID and component</param>
    /// <param name="marking">The marking to try and remove.</param>
    /// <param name="sync">Whether to immediately sync this to the humanoid</param>
    public void RemoveMarking(Entity<HumanoidAppearanceComponent?> ent, string marking, bool sync = true)
    {
        if (!Resolve(ent, ref ent.Comp)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        ent.Comp.MarkingSet.Remove(prototype.MarkingCategory, marking);

        if (sync)
            Dirty(ent);
    }

    /// <summary>
    /// Removes a marking from a humanoid by category and index.
    /// </summary>
    /// <param name="ent">Humanoid mob's UID and component</param>
    /// <param name="category">Category of the marking</param>
    /// <param name="index">Index of the marking</param>
    public void RemoveMarking(Entity<HumanoidAppearanceComponent?> ent, MarkingCategories category, int index)
    {
        if (index < 0
            || !Resolve(ent, ref ent.Comp)
            || !ent.Comp.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        ent.Comp.MarkingSet.Remove(category, index);
        Dirty(ent);
    }

    /// <summary>
    /// Sets the marking ID of the humanoid in a category at an index in the category's list.
    /// </summary>
    /// <param name="ent">Humanoid mob's UID and component</param>
    /// <param name="category">Category of the marking</param>
    /// <param name="index">Index of the marking</param>
    /// <param name="markingId">The marking ID to use</param>
    public void SetMarkingId(Entity<HumanoidAppearanceComponent?> ent, MarkingCategories category, int index, string markingId)
    {
        if (index < 0
            || !_markingManager.MarkingsByCategory(category).TryGetValue(markingId, out var markingPrototype)
            || !Resolve(ent, ref ent.Comp)
            || !ent.Comp.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        var marking = markingPrototype.AsMarking();
        for (var i = 0; i < marking.MarkingColors.Count && i < markings[index].MarkingColors.Count; i++)
        {
            marking.SetColor(i, markings[index].MarkingColors[i]);
        }

        ent.Comp.MarkingSet.Replace(category, index, marking);
        Dirty(ent);
    }

    /// <summary>
    /// Sets the marking colors of the humanoid in a category at an index in the category's list.
    /// </summary>
    /// <param name="ent">Humanoid mob's UID and component</param>
    /// <param name="category">Category of the marking</param>
    /// <param name="index">Index of the marking</param>
    /// <param name="colors">The marking colors to use</param>
    public void SetMarkingColor(Entity<HumanoidAppearanceComponent?> ent, MarkingCategories category, int index, List<Color> colors)
    {
        if (index < 0
            || !Resolve(ent, ref ent.Comp)
            || !ent.Comp.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        for (var i = 0; i < markings[index].MarkingColors.Count && i < colors.Count; i++)
        {
            markings[index].SetColor(i, colors[i]);
        }

        Dirty(ent);
    }
}
