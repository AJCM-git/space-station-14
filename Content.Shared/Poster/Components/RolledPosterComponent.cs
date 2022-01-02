using Content.Shared.Poster.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Poster.Components;

[RegisterComponent]
[ComponentProtoName("RolledPoster")]
[Friend(typeof(RolledPosterSystem))]
public class RolledPosterComponent : Component
{
    [ViewVariables]
    [DataField("key")]
    public string? Key = default!;

    [ViewVariables]
    [DataField("spawn")]
    public EntityPrototype Spawn = new EntityPrototype();
}
