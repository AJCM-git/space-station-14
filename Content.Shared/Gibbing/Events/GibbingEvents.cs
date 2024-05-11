using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Gibbing.Events;



/// <summary>
/// Called just before we actually gib the target entity
/// </summary>
/// <param name="Target">The entity being gibed</param>
/// <param name="GibType">What type of gibbing is occuring</param>
/// <param name="SelectedContainers">Containers we are allow to gib</param>
/// <param name="DenyContainers">Whether to see the selectedContainers as a list to allow or to deny</param>
[ByRefEvent] public record struct AttemptEntityContentsGibEvent(
    EntityUid Target,
    GibContentsOption GibType,
    List<string>? SelectedContainers,
    bool DenyContainers
    );


/// <summary>
/// Called just before we actually gib the target entity
/// </summary>
/// <param name="Target">The entity being gibed</param>
/// <param name="GibletCount">how many giblets to spawn</param>
/// <param name="GibType">What type of gibbing is occuring</param>
[ByRefEvent] public record struct AttemptEntityGibEvent(EntityUid Target, int GibletCount, GibType GibType);

/// <summary>
/// Called immediately after we gib the target entity
/// </summary>
/// <param name="Target">The entity being gibbed</param>
/// <param name="DroppedEntities">Any entities that are spilled out (if any)</param>
[ByRefEvent] public record struct EntityGibbedEvent(EntityUid Target, List<EntityUid> DroppedEntities);

[Serializable, NetSerializable]
public enum GibType : byte
{
    Skip,
    Drop,
    Detach,
    Gib,
}

public enum GibContentsOption : byte
{
    Skip,
    Drop,
    Detach,
    Gib
}

/// <param name="Launch">Should we launch giblets or just drop them</param>
/// <param name="Direction">The direction to launch giblets</param>
/// <param name="Impulse">The impulse to launch giblets at</param>
/// <param name="ImpulseVariance">The variation in giblet launch impulse </param>
/// <param name="ScatterCone">The cone we are launching giblets in</param>
[DataRecord, Serializable, NetSerializable]
public partial record struct GibLaunchOptions(bool Launch = true, Vector2? Direction = null, float Impulse = 0f, float ImpulseVariance = 0f, Angle ScatterCone = default);
