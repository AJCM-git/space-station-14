using Content.Shared.Body.Systems;

namespace Content.Shared.Humanoid;

[RegisterComponent]
[Access(typeof(SharedBodySystem))]
public sealed partial class OrganEyeComponent : Component
{
    [DataField]
    public Color EyeColor;
}
