using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Containers.WhitelistedContainer;

/// <summary>
/// Used for entities that have a <see cref="WhitelistedContainer"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(WhitelistedContainerSystem))]
public sealed class WhitelistedContainerComponent : Component
{
    [DataField("containers", readOnly: true)]
    [ViewVariables]
    public readonly IReadOnlyDictionary<string, WhitelistedContainer> Containers = default!;
}

/// <summary>
/// Wrapper for a <see cref="Container"/> that adds content functionality
/// like entity whitelists, insert/remove sounds and capacity
/// </summary>
[DataDefinition]
[Access(typeof(WhitelistedContainerSystem))]
[Serializable, NetSerializable]
public sealed class WhitelistedContainer
{
    [ViewVariables, NonSerialized]
    public Container? BaseContainer = new();

    /// <summary>
    /// How many entities we can store
    /// </summary>
    [DataField("capacity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Capacity = 50;

    [DataField("whitelistedEntities")]
    [ViewVariables]
    public EntityWhitelist? WhitelistedEntities;

    [DataField("blacklistedEntities")]
    [ViewVariables]
    public EntityWhitelist? BlacklistedEntities;

    [DataField("insertSounds")]
    [ViewVariables]
    public List<SoundSpecifier>? InsertSounds;

    [DataField("removeSounds")]
    [ViewVariables]
    public List<SoundSpecifier>? RemoveSounds;

    public WhitelistedContainer() { }

    public WhitelistedContainer(WhitelistedContainer other)
    {
        CopyFrom(other);
    }

    public void CopyFrom(WhitelistedContainer other)
    {
        // These fields are mutable reference types. But they generally don't get modified, so this should be fine.
        WhitelistedEntities = other.WhitelistedEntities;
        BlacklistedEntities = other.BlacklistedEntities;
        InsertSounds = other.InsertSounds;
        RemoveSounds = other.RemoveSounds;
    }
}
