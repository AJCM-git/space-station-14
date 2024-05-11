using Content.Shared.Gibbing.Events;
using Content.Shared.Gibbing.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Gibbing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GibbingSystem))]
public sealed partial class GibletComponent : Component
{
    /// <summary>
    /// What type of gibing are we performing
    /// </summary>
    [DataField, AutoNetworkedField]
    public GibType GibType;

    /// <summary>
    /// What type of gibing do we perform on any container contents?
    /// </summary>
    [DataField, AutoNetworkedField]
    public GibContentsOption GibContentsOption;
}
