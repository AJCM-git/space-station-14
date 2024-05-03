using Content.Shared.Gibbing.Events;
using Content.Shared.Gibbing.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Gibbing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(GibbingSystem))]
public sealed partial class GibbableComponent : Component
{
    /// <summary>
    /// Giblet entity prototypes to randomly select from when spawning additional giblets
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> GibPrototypes = new();

    /// <summary>
    /// Number of giblet entities to spawn in addition to entity contents
    /// </summary>
    [DataField, AutoNetworkedField]
    public int GibCount;

    /// <summary>
    /// Sound to be played when this entity is gibbed, only played when playsound is true on the gibbing function
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? GibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));

    /// <summary>
    /// Max distance giblets can be dropped from an entity when NOT using physics-based scattering
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GibScatterRange = 0.3f;

    /// <summary>
    /// How much to multiply the random spread on dropped giblets(if we are dropping them!)
    /// </summary>
    public float RandomSpreadMod = 1.0f;

    /// <summary>
    /// What type of gibing are we performing
    /// </summary>
    public GibType GibType;

    /// <summary>
    /// What type of gibing do we perform on any container contents?
    /// </summary>
    public GibContentsOption GibContentsOption;

    /// <summary>
    /// Dictates if we should launch the gibs, how much impulse to we apply to them, etc
    /// </summary>
    public GibLaunchOptions LaunchOptions;
}
