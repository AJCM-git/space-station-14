using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// HumanoidSystem. Primarily deals with the appearance and visual data
/// of a humanoid entity. HumanoidVisualizer is what deals with actually
/// organizing the sprites and setting up the sprite component's layers.
///
/// This is a shared system, because while it is server authoritative,
/// you still need a local copy so that players can set up their
/// characters.
/// </summary>
public abstract class SharedHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;

    [ValidatePrototypeId<SpeciesPrototype>]
    public const string DefaultSpecies = "Human";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HumanoidAppearanceComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInit(Entity<HumanoidAppearanceComponent> ent, ref ComponentInit args)
    {
        if (string.IsNullOrEmpty(ent.Comp.Species) || _netManager.IsClient && !IsClientSide(ent))
            return;

        if (string.IsNullOrEmpty(ent.Comp.Initial)
            || !_proto.TryIndex(ent.Comp.Initial, out var startingSet))
        {
            LoadProfile((ent, ent.Comp), HumanoidCharacterProfile.DefaultWithSpecies(ent.Comp.Species));
            return;
        }

        // Do this first, because profiles currently do not support custom base layers
        foreach (var (layer, info) in startingSet.CustomBaseLayers)
        {
            ent.Comp.CustomBaseLayers.Add(layer, info);
        }

        LoadProfile((ent, ent.Comp), startingSet.Profile);
    }

    private void OnExamined(Entity<HumanoidAppearanceComponent> ent, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(ent, EntityManager);
        var species = GetSpeciesRepresentation(ent.Comp.Species).ToLower();
        var age = GetAgeRepresentation(ent.Comp.Species, ent.Comp.Age);

        args.PushText(Loc.GetString("humanoid-appearance-component-examine", ("user", identity), ("age", age), ("species", species)));
    }

    /// <summary>
    /// Toggles a humanoid's sprite layer visibility.
    /// </summary>
    /// <param name="ent">Humanoid mob's UID and the Humanoid component of the entity</param>
    /// <param name="layer">Layer to toggle visibility for</param>
    public void SetLayerVisibility(Entity<HumanoidAppearanceComponent?> ent,
        HumanoidVisualLayers layer,
        bool visible,
        bool permanent = false)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var dirty = false;
        SetLayerVisibility((ent, ent.Comp), layer, visible, permanent, ref dirty);
        if (dirty)
            Dirty(ent);
    }

    /// <summary>
    /// Sets the visibility for multiple layers at once on a humanoid's sprite.
    /// </summary>
    /// <param name="ent">Humanoid mob's UID and the Humanoid component of the entity</param>
    /// <param name="layers">An enumerable of all sprite layers that are going to have their visibility set</param>
    /// <param name="visible">The visibility state of the layers given</param>
    /// <param name="permanent">If this is a permanent change, or temporary. Permanent layers are stored in their own hash set.</param>
    public void SetLayersVisibility(Entity<HumanoidAppearanceComponent?> ent, IEnumerable<HumanoidVisualLayers> layers, bool visible, bool permanent = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var dirty = false;

        foreach (var layer in layers)
        {
            SetLayerVisibility((ent, ent.Comp), layer, visible, permanent, ref dirty);
        }

        if (dirty)
            Dirty(ent);
    }

    protected virtual void SetLayerVisibility(
        Entity<HumanoidAppearanceComponent> ent,
        HumanoidVisualLayers layer,
        bool visible,
        bool permanent,
        ref bool dirty)
    {
        if (visible)
        {
            if (permanent)
                dirty |= ent.Comp.PermanentlyHidden.Remove(layer);

            dirty |= ent.Comp.HiddenLayers.Remove(layer);
        }
        else
        {
            if (permanent)
                dirty |= ent.Comp.PermanentlyHidden.Add(layer);

            dirty |= ent.Comp.HiddenLayers.Add(layer);
        }
    }

    /// <summary>
    /// Set a humanoid mob's species. This will change their base sprites, as well as their current
    /// set of markings to fit against the mob's new species.
    /// </summary>
    /// <param name="ent">The humanoid mob's UID and the Humanoid component of the entity</param>
    /// <param name="species">The species to set the mob to. Will return if the species prototype was invalid.</param>
    /// <param name="sync">Whether to immediately synchronize this to the humanoid mob, or not.</param>
    public void SetSpecies(Entity<HumanoidAppearanceComponent?> ent, string species, bool sync = true)
    {
        if (!Resolve(ent, ref ent.Comp) || !_proto.TryIndex<SpeciesPrototype>(species, out var prototype))
            return;

        ent.Comp.Species = species;
        ent.Comp.MarkingSet.EnsureSpecies(species, ent.Comp.SkinColor, _markingManager);
        var oldMarkings = ent.Comp.MarkingSet.GetForwardEnumerator().ToList();
        ent.Comp.MarkingSet = new(oldMarkings, prototype.MarkingPoints, _markingManager, _proto);

        if (sync)
            Dirty(ent);
    }

    /// <summary>
    /// Sets the skin color of this humanoid mob. Will only affect base layers that are not custom,
    /// custom base layers should use <see cref="SetBaseLayerColor"/> instead.
    /// </summary>
    /// <param name="ent">The humanoid mob's UID and the Humanoid component of the entity</param>
    /// <param name="skinColor">Skin color to set on the humanoid mob.</param>
    /// <param name="sync">Whether to synchronize this to the humanoid mob, or not.</param>
    /// <param name="verify">Whether to verify the skin color can be set on this humanoid or not</param>
    public virtual void SetSkinColor(Entity<HumanoidAppearanceComponent?> ent, Color skinColor, bool sync = true, bool verify = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!_proto.TryIndex(ent.Comp.Species, out var species))
            return;

        if (verify && !SkinColor.VerifySkinColor(species.SkinColoration, skinColor))
            skinColor = SkinColor.ValidSkinTone(species.SkinColoration, skinColor);

        ent.Comp.SkinColor = skinColor;

        if (sync)
            Dirty(ent);
    }

    /// <summary>
    /// Sets the base layer ID of this humanoid mob. A humanoid mob's 'base layer' is
    /// the skin sprite that is applied to the mob's sprite upon appearance refresh.
    /// </summary>
    /// <param name="ent">The humanoid mob's UID and the Humanoid component of the entity</param>
    /// <param name="layer">The layer to target on this humanoid mob.</param>
    /// <param name="id">The ID of the sprite to use. See <see cref="HumanoidSpeciesSpriteLayer"/>.</param>
    /// <param name="sync">Whether to synchronize this to the humanoid mob, or not.</param>
    public void SetBaseLayerId(Entity<HumanoidAppearanceComponent?> ent, HumanoidVisualLayers layer, string? id, bool sync = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.CustomBaseLayers.TryGetValue(layer, out var info))
            ent.Comp.CustomBaseLayers[layer] = info with { Id = id };
        else
            ent.Comp.CustomBaseLayers[layer] = new(id);

        if (sync)
            Dirty(ent);
    }

    /// <summary>
    /// Sets the color of this humanoid mob's base layer. See <see cref="SetBaseLayerId"/> for a
    /// description of how base layers work.
    /// </summary>
    /// <param name="ent">The humanoid mob's UID.</param>
    /// <param name="layer">The layer to target on this humanoid mob.</param>
    /// <param name="color">The color to set this base layer to.</param>
    public void SetBaseLayerColor(Entity<HumanoidAppearanceComponent?> ent, HumanoidVisualLayers layer, Color? color, bool sync = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.CustomBaseLayers.TryGetValue(layer, out var info))
            ent.Comp.CustomBaseLayers[layer] = info with { Color = color };
        else
            ent.Comp.CustomBaseLayers[layer] = new(null, color);

        if (sync)
            Dirty(ent);
    }

    /// <summary>
    /// Set a humanoid mob's sex. This will not change their gender.
    /// </summary>
    /// <param name="ent">The humanoid mob's UID and the Humanoid component of the entity</param>
    /// <param name="sex">The sex to set the mob to.</param>
    /// <param name="sync">Whether to immediately synchronize this to the humanoid mob, or not.</param>
    public void SetSex(Entity<HumanoidAppearanceComponent?> ent, Sex sex, bool sync = true)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Sex == sex)
            return;

        var oldSex = ent.Comp.Sex;
        ent.Comp.Sex = sex;
        ent.Comp.MarkingSet.EnsureSexes(sex, _markingManager);
        RaiseLocalEvent(ent, new SexChangedEvent(oldSex, sex));

        if (sync)
            Dirty(ent);
    }

    /// <summary>
    /// Loads a humanoid character profile directly onto this humanoid mob.
    /// </summary>
    /// <param name="ent">The mob's entity UID and the Humanoid component of the entity</param>
    /// <param name="profile">The character profile to load.</param>
    public virtual void LoadProfile(Entity<HumanoidAppearanceComponent?> ent, HumanoidCharacterProfile? profile)
    {
        if (profile == null)
            return;

        if (!Resolve(ent, ref ent.Comp))
            return;

        SetSpecies(ent, profile.Species, false);
        SetSex(ent, profile.Sex, false);
        ent.Comp.EyeColor = profile.Appearance.EyeColor;

        SetSkinColor(ent, profile.Appearance.SkinColor, false);

        ent.Comp.MarkingSet.Clear();

        // Add markings that doesn't need coloring. We store them until we add all other markings that doesn't need it.
        var markingFColored = new Dictionary<Marking, MarkingPrototype>();
        foreach (var marking in profile.Appearance.Markings)
        {
            if (_markingManager.TryGetMarking(marking, out var prototype))
            {
                if (!prototype.ForcedColoring)
                    AddMarking(ent, marking.MarkingId, marking.MarkingColors, false);
                else
                    markingFColored.Add(marking, prototype);
            }
        }

        // Hair/facial hair - this may eventually be deprecated.
        // We need to ensure hair before applying it or coloring can try depend on markings that can be invalid
        var hairColor = _markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.Hair, out var hairAlpha, _proto)
            ? profile.Appearance.SkinColor.WithAlpha(hairAlpha) : profile.Appearance.HairColor;
        var facialHairColor = _markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.FacialHair, out var facialHairAlpha, _proto)
            ? profile.Appearance.SkinColor.WithAlpha(facialHairAlpha) : profile.Appearance.FacialHairColor;

        if (_markingManager.Markings.TryGetValue(profile.Appearance.HairStyleId, out var hairPrototype) &&
            _markingManager.CanBeApplied(profile.Species, profile.Sex, hairPrototype, _proto))
        {
            AddMarking(ent, profile.Appearance.HairStyleId, hairColor, false);
        }

        if (_markingManager.Markings.TryGetValue(profile.Appearance.FacialHairStyleId, out var facialHairPrototype) &&
            _markingManager.CanBeApplied(profile.Species, profile.Sex, facialHairPrototype, _proto))
        {
            AddMarking(ent, profile.Appearance.FacialHairStyleId, facialHairColor, false);
        }

        ent.Comp.MarkingSet.EnsureSpecies(profile.Species, profile.Appearance.SkinColor, _markingManager, _proto);

        // Finally adding marking with forced colors
        foreach (var (marking, prototype) in markingFColored)
        {
            var markingColors = MarkingColoring.GetMarkingLayerColors(
                prototype,
                profile.Appearance.SkinColor,
                profile.Appearance.EyeColor,
                ent.Comp.MarkingSet
            );
            AddMarking(ent, marking.MarkingId, markingColors, false);
        }

        EnsureDefaultMarkings(ent);

        ent.Comp.Gender = profile.Gender;
        if (TryComp<GrammarComponent>(ent, out var grammar))
            grammar.Gender = profile.Gender;

        ent.Comp.Age = profile.Age;

        Dirty(ent);
    }

    /// <summary>
    /// Adds a marking to this humanoid.
    /// </summary>
    /// <param name="ent">Humanoid mob's UID and the Humanoid component of the entity</param>
    /// <param name="marking">Marking ID to use</param>
    /// <param name="color">Color to apply to all marking layers of this marking</param>
    /// <param name="sync">Whether to immediately sync this marking or not</param>
    /// <param name="forced">If this marking was forced (ignores marking points)</param>
    public void AddMarking(Entity<HumanoidAppearanceComponent?> ent, string marking, Color? color = null, bool sync = true, bool forced = false)
    {
        if (!Resolve(ent, ref ent.Comp)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
            return;

        var markingObject = prototype.AsMarking();
        markingObject.Forced = forced;
        if (color != null)
        {
            for (var i = 0; i < prototype.Sprites.Count; i++)
            {
                markingObject.SetColor(i, color.Value);
            }
        }

        ent.Comp.MarkingSet.AddBack(prototype.MarkingCategory, markingObject);

        if (sync)
            Dirty(ent);
    }

    private void EnsureDefaultMarkings(Entity<HumanoidAppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.MarkingSet.EnsureDefault(ent.Comp.SkinColor, ent.Comp.EyeColor, _markingManager);
    }

    /// <summary>
    /// Adds a marking to this humanoid.
    /// </summary>
    /// <param name="ent">Humanoid mob's UID and Humanoid component of the entity</param>
    /// <param name="marking">Marking ID to use</param>
    /// <param name="colors">Colors to apply against this marking's set of sprites.</param>
    /// <param name="sync">Whether to immediately sync this marking or not</param>
    /// <param name="forced">If this marking was forced (ignores marking points)</param>
    public void AddMarking(Entity<HumanoidAppearanceComponent?> ent, string marking, IReadOnlyList<Color> colors, bool sync = true, bool forced = false)
    {
        if (!Resolve(ent, ref ent.Comp)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
            return;

        var markingObject = new Marking(marking, colors) { Forced = forced };
        ent.Comp.MarkingSet.AddBack(prototype.MarkingCategory, markingObject);

        if (sync)
            Dirty(ent);
    }

    /// <summary>
    /// Takes ID of the species prototype, returns UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(string speciesId)
    {
        if (_proto.TryIndex<SpeciesPrototype>(speciesId, out var species))
            return Loc.GetString(species.Name);

        Log.Error("Tried to get representation of unknown species: {speciesId}");
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    public string GetAgeRepresentation(string species, int age)
    {
        if (!_proto.TryIndex<SpeciesPrototype>(species, out var speciesPrototype))
        {
            Log.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
            return Loc.GetString("identity-age-young");

        return Loc.GetString(age < speciesPrototype.OldAge ? "identity-age-middle-aged" : "identity-age-old");
    }
}
