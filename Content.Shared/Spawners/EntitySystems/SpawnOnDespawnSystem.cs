using Content.Shared.Damage;
using Content.Shared.Spawners.Components;
using Content.Shared.Storage;

namespace Content.Shared.Spawners.EntitySystems;

/// TODO command spawnondespawn <spawnprototypeid> <coordinates>
public sealed class SpawnOnDespawnSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnOnDespawnComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SpawnOnDespawnComponent, TimedDespawnEvent>(OnTimedDespawn);
    }

    /// <summary>
    /// "Arriving" phase
    /// </summary>
    private void OnMapInit(EntityUid uid, SpawnOnDespawnComponent component, MapInitEvent args)
    {
        component.AudioStream = _audioSystem.PlayPvs(component.ArrivingSound, uid);
    }

    /// <summary>
    /// "Landing" phase
    /// </summary>
    private void OnTimedDespawn(EntityUid uid, SpawnOnDespawnComponent component, ref TimedDespawnEvent args)
    {
        var coords = Transform(uid).Coordinates;
        if (component.LandingDamage.Total > 0)
        {
            foreach (var toDamage in _lookupSystem.GetEntitiesIntersecting(coords))
            {
                _damageableSystem.TryChangeDamage(toDamage, component.LandingDamage, true);
            }
        }

        var toSpawn = EntitySpawnCollection.GetSpawns(component.SpawnEntries);
        foreach (var spawnPrototype in toSpawn)
        {
            if (spawnPrototype is null)
                continue;
            Spawn(spawnPrototype, coords);
        }

        component.AudioStream?.Stop();
        component.AudioStream = _audioSystem.PlayPvs(component.LandingSound, uid);
    }
}

// public sealed class TeleportToCommand : LocalizedCommands
// {
//     [Dependency] private readonly ISharedPlayerManager _players = default!;
//     [Dependency] private readonly IEntityManager _entities = default!;
//
//     public override string Command => "tpto";
//     public override bool RequireServerOrSingleplayer => true;
//
//     public override void Execute(IConsoleShell shell, string argStr, string[] args)
//     {
//         if (args.Length == 0)
//             return;
//
//         var target = args[0];
//
//         if (!TryGetTransformFromUidOrUsername(target, shell, out _, out var targetTransform))
//             return;
//
//         var transformSystem = _entities.System<SharedTransformSystem>();
//         var targetCoords = targetTransform.Coordinates;
//
//         if (args.Length == 1)
//         {
//             var ent = shell.Player?.AttachedEntity;
//
//             if (!_entities.TryGetComponent(ent, out TransformComponent? playerTransform))
//             {
//                 shell.WriteError(Loc.GetString("cmd-failure-no-attached-entity"));
//                 return;
//             }
//
//             transformSystem.SetCoordinates(ent.Value, targetCoords);
//             playerTransform.AttachToGridOrMap();
//         }
//         else
//         {
//             foreach (var victim in args)
//             {
//                 if (victim == target)
//                     continue;
//
//                 if (!TryGetTransformFromUidOrUsername(victim, shell, out var uid, out var victimTransform))
//                     return;
//
//                 transformSystem.SetCoordinates(uid.Value, targetCoords);
//                 victimTransform.AttachToGridOrMap();
//             }
//         }
//     }
//
//     private bool TryGetTransformFromUidOrUsername(
//         string str,
//         IConsoleShell shell,
//         [NotNullWhen(true)] out EntityUid? victimUid,
//         [NotNullWhen(true)] out TransformComponent? transform)
//     {
//         if (EntityUid.TryParse(str, out var uid) && _entities.TryGetComponent(uid, out transform))
//         {
//             victimUid = uid;
//             return true;
//         }
//
//         if (_players.Sessions.TryFirstOrDefault(x => x.ConnectedClient.UserName == str, out var session)
//             && _entities.TryGetComponent(session.AttachedEntity, out transform))
//         {
//             victimUid = session.AttachedEntity;
//             return true;
//         }
//
//         shell.WriteError(Loc.GetString("cmd-tpto-parse-error", ("str",str)));
//
//         transform = null;
//         victimUid = default;
//         return false;
//     }
//
//     public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
//     {
//         if (args.Length == 0)
//             return CompletionResult.Empty;
//
//         var last = args[^1];
//
//         var users = _players.Sessions
//             .Select(x => x.Name ?? string.Empty)
//             .Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith(last, StringComparison.CurrentCultureIgnoreCase));
//
//         var hint = args.Length == 1 ? "cmd-tpto-destination-hint" : "cmd-tpto-victim-hint";
//         hint = Loc.GetString(hint);
//
//         var opts = CompletionResult.FromHintOptions(users, hint);
//         if (last != string.Empty && !EntityUid.TryParse(last, out _))
//             return opts;
//
//         return CompletionResult.FromHintOptions(opts.Options.Concat(CompletionHelper.EntityUids(last, _entities)), hint);
//     }
// }
