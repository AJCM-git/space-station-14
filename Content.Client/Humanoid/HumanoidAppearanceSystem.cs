using System.Linq;
using Content.Shared.Body.Part;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Humanoid;

public sealed class HumanoidAppearanceSystem : SharedHumanoidAppearanceSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, BodyPartRemovedEvent>(OnBodyPartRemoved);
        SubscribeLocalEvent<HumanoidAppearanceComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnBodyPartRemoved(Entity<HumanoidAppearanceComponent> ent, ref BodyPartRemovedEvent args)
    {
        /* TODO BODY this is very hacky because if you put a bodypart of a different color in a body and then remove it
        the body part will be the color of the new owner's */
        if (args.Part.Comp.ToHumanoidLayers() is not {} basePartLayer)
            return;

        var bodyEnt  = new Entity<HumanoidAppearanceComponent, SpriteComponent>(ent.Owner, ent.Comp, Comp<SpriteComponent>(ent));
        var partEnt  = new Entity<BodyPartComponent, SpriteComponent>(args.Part, args.Part.Comp, Comp<SpriteComponent>(args.Part));

        var partSublayers = HumanoidVisualLayersExtension.Sublayers(basePartLayer).ToArray();
        // TODO BODY The color and state fields get set in the eyes and markings and they are visible but for some reason they only slightly change the head sprite
        SyncBodyPartSprites(bodyEnt, partEnt, basePartLayer, partSublayers);
        ApplyPartMarkingSet(bodyEnt, partEnt, partSublayers);

        SetLayersVisibility((ent, ent.Comp), partSublayers, visible: false, permanent: true);
    }

    private void SyncBodyPartSprites(Entity<HumanoidAppearanceComponent, SpriteComponent> bodyEnt, Entity<BodyPartComponent, SpriteComponent> partEnt,
        HumanoidVisualLayers basePartLayer, HumanoidVisualLayers[] partSublayers)
    {
        var bodySprite = bodyEnt.Comp2;
        var partSprite = partEnt.Comp2;

        // Set the eye layer and color in the head body part
        // TODO BODY also handle tails
        foreach (var partSublayer in partSublayers)
        {
            if (partSublayer != HumanoidVisualLayers.Eyes)
                continue;

            var idx = partSprite.LayerMapReserveBlank(partSublayer);
            partSprite.LayerSetState(idx, bodySprite[partSublayer].RsiState, bodySprite[partSublayer].Rsi);
            partSprite.LayerSetColor(idx, bodySprite[partSublayer].Color);
        }

        var bodySpritePartLayer = bodySprite[basePartLayer];
        // Set the skin color in the body part
        partSprite.Color = bodySpritePartLayer.Color;
    }

    private void ApplyPartMarkingSet(Entity<HumanoidAppearanceComponent, SpriteComponent> bodyEnt,
        Entity<BodyPartComponent, SpriteComponent> partEnt, HumanoidVisualLayers[] targetLayers)
    {
        var bodySprite = bodyEnt.Comp2;
        var partSprite = partEnt.Comp2;

        foreach (var markingList in bodyEnt.Comp1.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                if (!_markingManager.TryGetMarking(marking, out var markingPrototype))
                    return;

                // TODO BODY this check still passes if the part is an arm and targetLayers contains hands
                // Remember that we also want to add hair to heads
                if (!targetLayers.Contains(markingPrototype.BodyPart))
                    return;

                if (!bodySprite.LayerMapTryGet(markingPrototype.BodyPart, out _))
                    return;
                partSprite.LayerMapReserveBlank(markingPrototype.BodyPart);

                foreach (var markingSprite in markingPrototype.Sprites)
                {
                    if (markingSprite is not SpriteSpecifier.Rsi rsi)
                        continue;

                    var markingLayerId = $"{markingPrototype.ID}-{rsi.RsiState}";

                    if (!bodySprite.LayerMapTryGet(markingLayerId, out var bodyMarkingLayerIdx))
                        continue;

                    var partMarkingLayerIdx = partSprite.LayerMapReserveBlank(markingLayerId);
                    partSprite.LayerSetSprite(partMarkingLayerIdx, rsi);
                    partSprite.LayerSetColor(partMarkingLayerIdx, bodySprite[bodyMarkingLayerIdx].Color);
                }
            }
        }
    }

    private void OnHandleState(Entity<HumanoidAppearanceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateSprite((ent, ent.Comp, Comp<SpriteComponent>(ent)));
    }

    private void UpdateSprite(Entity<HumanoidAppearanceComponent, SpriteComponent> ent)
    {
        UpdateLayers(ent);
        ApplyMarkingSet(ent);
    }

    private static bool IsHidden(Entity<HumanoidAppearanceComponent> ent, HumanoidVisualLayers layer)
        => ent.Comp.HiddenLayers.Contains(layer) || ent.Comp.PermanentlyHidden.Contains(layer);

    private void UpdateLayers(Entity<HumanoidAppearanceComponent, SpriteComponent> ent)
    {
        var (_, humanoid, sprite) = ent;

        // add default species layers
        var speciesProto = _prototypeManager.Index(humanoid.Species);
        var baseSprites = _prototypeManager.Index<HumanoidSpeciesBaseSpritesPrototype>(speciesProto.SpriteSet);
        var oldLayers = new HashSet<HumanoidVisualLayers>(humanoid.BaseLayers.Keys);
        humanoid.BaseLayers.Clear();

        foreach (var (key, id) in baseSprites.Sprites)
        {
            oldLayers.Remove(key);
            if (!humanoid.CustomBaseLayers.ContainsKey(key))
                SetLayerData(ent, key, id, sexMorph: true);
        }

        // add custom layers
        foreach (var (key, info) in humanoid.CustomBaseLayers)
        {
            oldLayers.Remove(key);
            SetLayerData(ent, key, info.Id, sexMorph: false, color: info.Color);
        }

        // hide old layers
        // TODO maybe just remove them altogether?
        foreach (var key in oldLayers)
        {
            if (sprite.LayerMapTryGet(key, out var index))
                sprite[index].Visible = false;
        }
    }

    private void SetLayerData(
        Entity<HumanoidAppearanceComponent, SpriteComponent> ent,
        HumanoidVisualLayers key,
        string? protoId,
        bool sexMorph = false,
        Color? color = null)
    {
        var (_, humanoid, sprite) = ent;

        var layerIndex = sprite.LayerMapReserveBlank(key);
        var layer = sprite[layerIndex];
        layer.Visible = !IsHidden(ent, key);

        sprite[sprite.LayerMapReserveBlank(HumanoidVisualLayers.Eyes)].Color = humanoid.EyeColor;

        if (color != null)
            layer.Color = color.Value;

        if (protoId == null)
            return;

        if (sexMorph)
            protoId = HumanoidVisualLayersExtension.GetSexMorph(key, humanoid.Sex, protoId);

        var proto = _prototypeManager.Index<HumanoidSpeciesSpriteLayer>(protoId);
        humanoid.BaseLayers[key] = proto;

        if (proto.MatchSkin)
            layer.Color = humanoid.SkinColor.WithAlpha(proto.LayerAlpha);

        if (proto.BaseSprite != null)
            sprite.LayerSetSprite(layerIndex, proto.BaseSprite);
    }

    /// <summary>
    ///     Loads a profile directly into a humanoid.
    /// </summary>
    /// <param name="ent">The humanoid entity's UID</param>
    /// <param name="profile">The profile to load.</param>
    /// <param name="ent">The humanoid entity's humanoid component.</param>
    /// <remarks>
    ///     This should not be used if the entity is owned by the server. The server will otherwise
    ///     override this with the appearance data it sends over.
    /// </remarks>
    public override void LoadProfile(Entity<HumanoidAppearanceComponent?> ent, HumanoidCharacterProfile? profile)
    {
        if (profile == null)
            return;

        if (!Resolve(ent, ref ent.Comp))
            return;

        var customBaseLayers = new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>();

        var speciesPrototype = _prototypeManager.Index<SpeciesPrototype>(profile.Species);
        var markings = new MarkingSet(speciesPrototype.MarkingPoints, _markingManager, _prototypeManager);

        // Add markings that doesn't need coloring. We store them until we add all other markings that doesn't need it.
        var markingFColored = new Dictionary<Marking, MarkingPrototype>();
        foreach (var marking in profile.Appearance.Markings)
        {
            if (_markingManager.TryGetMarking(marking, out var prototype))
            {
                if (!prototype.ForcedColoring)
                    markings.AddBack(prototype.MarkingCategory, marking);
                else
                    markingFColored.Add(marking, prototype);
            }
        }

        // legacy: remove in the future?
        //markings.RemoveCategory(MarkingCategories.Hair);
        //markings.RemoveCategory(MarkingCategories.FacialHair);

        // We need to ensure hair before applying it or coloring can try depend on markings that can be invalid
        var hairColor = _markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.Hair, out var hairAlpha, _prototypeManager)
            ? profile.Appearance.SkinColor.WithAlpha(hairAlpha)
            : profile.Appearance.HairColor;
        var hair = new Marking(profile.Appearance.HairStyleId,
            new[] { hairColor });

        var facialHairColor = _markingManager.MustMatchSkin(profile.Species, HumanoidVisualLayers.FacialHair, out var facialHairAlpha, _prototypeManager)
            ? profile.Appearance.SkinColor.WithAlpha(facialHairAlpha)
            : profile.Appearance.FacialHairColor;
        var facialHair = new Marking(profile.Appearance.FacialHairStyleId,
            new[] { facialHairColor });

        if (_markingManager.CanBeApplied(profile.Species, profile.Sex, hair, _prototypeManager))
            markings.AddBack(MarkingCategories.Hair, hair);
        if (_markingManager.CanBeApplied(profile.Species, profile.Sex, facialHair, _prototypeManager))
            markings.AddBack(MarkingCategories.FacialHair, facialHair);

        // Finally adding marking with forced colors
        foreach (var (marking, prototype) in markingFColored)
        {
            var markingColors = MarkingColoring.GetMarkingLayerColors(
                prototype,
                profile.Appearance.SkinColor,
                profile.Appearance.EyeColor,
                markings
            );
            markings.AddBack(prototype.MarkingCategory, new Marking(marking.MarkingId, markingColors));
        }

        markings.EnsureSpecies(profile.Species, profile.Appearance.SkinColor, _markingManager, _prototypeManager);
        markings.EnsureSexes(profile.Sex, _markingManager);
        markings.EnsureDefault(
            profile.Appearance.SkinColor,
            profile.Appearance.EyeColor,
            _markingManager);

        DebugTools.Assert(IsClientSide(ent));

        ent.Comp.MarkingSet = markings;
        ent.Comp.PermanentlyHidden = new HashSet<HumanoidVisualLayers>();
        ent.Comp.HiddenLayers = new HashSet<HumanoidVisualLayers>();
        ent.Comp.CustomBaseLayers = customBaseLayers;
        ent.Comp.Sex = profile.Sex;
        ent.Comp.Gender = profile.Gender;
        ent.Comp.Age = profile.Age;
        ent.Comp.Species = profile.Species;
        ent.Comp.SkinColor = profile.Appearance.SkinColor;
        ent.Comp.EyeColor = profile.Appearance.EyeColor;

        UpdateSprite((ent, ent.Comp, Comp<SpriteComponent>(ent)));
    }

    private void ApplyMarkingSet(Entity<HumanoidAppearanceComponent, SpriteComponent> ent)
    {
        // I am lazy and I CBF resolving the previous mess, so I'm just going to nuke the markings.
        // Really, markings should probably be a separate component altogether.
        ClearAllMarkings(ent);

        foreach (var markingList in ent.Comp1.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                if (_markingManager.TryGetMarking(marking, out var markingPrototype))
                    ApplyMarking(ent, markingPrototype, marking.MarkingColors, marking.Visible);
            }
        }

        ent.Comp1.ClientOldMarkings = new MarkingSet(ent.Comp1.MarkingSet);
    }

    private void ClearAllMarkings(Entity<HumanoidAppearanceComponent, SpriteComponent> ent)
    {
        foreach (var markingList in ent.Comp1.ClientOldMarkings.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                RemoveMarking(marking, ent.Comp2);
            }
        }

        ent.Comp1.ClientOldMarkings.Clear();

        foreach (var markingList in ent.Comp1.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                RemoveMarking(marking, ent.Comp2);
            }
        }
    }

    private void RemoveMarking(Marking marking, SpriteComponent spriteComp)
    {
        if (!_markingManager.TryGetMarking(marking, out var prototype))
            return;

        foreach (var sprite in prototype.Sprites)
        {
            if (sprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{marking.MarkingId}-{rsi.RsiState}";
            if (!spriteComp.LayerMapTryGet(layerId, out var index))
                continue;

            spriteComp.LayerMapRemove(layerId);
            spriteComp.RemoveLayer(index);
        }
    }
    private void ApplyMarking(Entity<HumanoidAppearanceComponent, SpriteComponent> ent,
        MarkingPrototype markingPrototype,
        IReadOnlyList<Color>? colors,
        bool visible)
    {
        var (_, humanoid, sprite) = ent;

        if (!sprite.LayerMapTryGet(markingPrototype.BodyPart, out var targetLayer))
            return;

        visible &= !IsHidden(ent, markingPrototype.BodyPart);
        visible &= humanoid.BaseLayers.TryGetValue(markingPrototype.BodyPart, out var setting)
           && setting.AllowsMarkings;

        for (var j = 0; j < markingPrototype.Sprites.Count; j++)
        {
            var markingSprite = markingPrototype.Sprites[j];

            if (markingSprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var layerId = $"{markingPrototype.ID}-{rsi.RsiState}";

            if (!sprite.LayerMapTryGet(layerId, out _))
            {
                var layer = sprite.AddLayer(markingSprite, targetLayer + j + 1);
                sprite.LayerMapSet(layerId, layer);
                sprite.LayerSetSprite(layerId, rsi);
            }

            sprite.LayerSetVisible(layerId, visible);

            if (!visible || setting == null) // this is kinda implied
                continue;

            // Okay so if the marking prototype is modified but we load old marking data this may no longer be valid
            // and we need to check the index is correct.
            // So if that happens just default to white?
            if (colors != null && j < colors.Count)
                sprite.LayerSetColor(layerId, colors[j]);
            else
                sprite.LayerSetColor(layerId, Color.White);
        }
    }

    public override void SetSkinColor(Entity<HumanoidAppearanceComponent?> ent, Color skinColor, bool sync = true, bool verify = true)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.SkinColor == skinColor)
            return;

        base.SetSkinColor(ent, skinColor, false, verify);

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        foreach (var (layer, spriteInfo) in ent.Comp.BaseLayers)
        {
            if (!spriteInfo.MatchSkin)
                continue;

            var index = sprite.LayerMapReserveBlank(layer);
            sprite[index].Color = skinColor.WithAlpha(spriteInfo.LayerAlpha);
        }
    }

    protected override void SetLayerVisibility(
        Entity<HumanoidAppearanceComponent> ent,
        HumanoidVisualLayers layer,
        bool visible,
        bool permanent,
        ref bool dirty)
    {
        base.SetLayerVisibility(ent, layer, visible, permanent, ref dirty);

        var sprite = Comp<SpriteComponent>(ent);
        if (!sprite.LayerMapTryGet(layer, out var index))
        {
            if (!visible)
                return;
            else
                index = sprite.LayerMapReserveBlank(layer);
        }

        var spriteLayer = sprite[index];
        if (spriteLayer.Visible == visible)
            return;

        spriteLayer.Visible = visible;

        // I fucking hate this. I'll get around to refactoring sprite layers eventually I swear

        foreach (var markingList in ent.Comp.MarkingSet.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                if (_markingManager.TryGetMarking(marking, out var markingPrototype) && markingPrototype.BodyPart == layer)
                    ApplyMarking((ent, ent.Comp, sprite), markingPrototype, marking.MarkingColors, marking.Visible);
            }
        }
    }
}
