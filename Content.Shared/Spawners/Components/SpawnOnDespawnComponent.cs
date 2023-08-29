using Content.Shared.Damage;
using Content.Shared.Spawners.EntitySystems;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Spawners.Components;

/// <summary>
/// Dependant on <see cref="TimedDespawnComponent"/>, spawns something when the component owner despawns
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SpawnOnDespawnSystem))]
public sealed class SpawnOnDespawnComponent : Component
{
    /// <summary>
    /// Which prototype to spawn on despawn
    /// </summary>
    [DataField("spawnEntries")]
    public List<EntitySpawnEntry> SpawnEntries = default!;

    [DataField("landingDamage")]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier LandingDamage = default!;

    [DataField("arrivingSound")]
    public SoundSpecifier? ArrivingSound;

    [DataField("landingSound")]
    public SoundSpecifier? LandingSound;

    public IPlayingAudioStream? AudioStream;
}
