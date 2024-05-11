using Content.Shared.Body.Systems;

namespace Content.Shared.Humanoid;

[RegisterComponent]
[Access(typeof(SharedBodySystem))]
public sealed partial class SkinComponent : Component
{
    [DataField]
    public Color SkinColor;
}
