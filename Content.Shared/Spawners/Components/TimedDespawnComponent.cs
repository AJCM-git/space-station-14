using Content.Shared.Spawners.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Spawners.Components;

/// <summary>
/// Put this component on something you would like to despawn after a certain amount of time
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTimedDespawnSystem))]
public sealed partial class TimedDespawnComponent : Component
{
    /// <summary>
    /// How long the entity will exist before despawning
    /// </summary>
    [DataField("lifetime"), AutoNetworkedField]
    [Access(typeof(SharedTimedDespawnSystem), Other = AccessPermissions.ReadWrite)]
    public float Lifetime = 5f;
}
