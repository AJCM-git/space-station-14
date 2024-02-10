﻿using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Prototypes;
using Content.Shared.Medical.Wounding.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Wounding.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(WoundSystem))]
public sealed partial class WoundableComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Body;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid RootWoundable;

    public const string WoundableContainerId = "Wounds";

    /// <summary>
    /// Should we spread damage to child and parent woundables when we gib this part
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SplatterDamageOnDestroy = true;

    [DataField, AutoNetworkedField]
    public EntProtoId? AmputationWoundProto = null;

    [DataField(required:true, customTypeSerializer: typeof(PrototypeIdDictionarySerializer<WoundingMetadata,DamageTypePrototype>)), AutoNetworkedField]
    public Dictionary<string, WoundingMetadata> Config = new();

    /// <summary>
    /// Last applied Damagetype
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public ProtoId<DamageTypePrototype> LastAppliedDamageType;

    /// <summary>
    /// What percentage of CURRENT HEALTH should be healed each healing update.
    /// This is only used if a healableComponent is also present.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 HealPercentage = 1.5;


    /// <summary>
    /// This woundable's current health, this is tracked separately from damagable's health and will differ!
    /// Health will slowly regenerate overtime.
    /// When health reaches 0, all damage will be taken as integrity, which does not heal natural.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Health = -1; //this is set during comp init or overriden when defined

    /// <summary>
    /// The current cap of health.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 HealthCap = -1; //this is set during comp init

    /// <summary>
    /// The absolute maximum possible health
    /// </summary>
    [DataField(required: true),ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxHealth = 50;

    /// <summary>
    /// This woundable's current integrity, if integrity reaches 0, this entity is gibbed/destroyed!
    /// Integrity does NOT heal naturally and must be treated to heal.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Integrity = -1; //this is set during comp init or overriden when defined

    /// <summary>
    /// The current cap of integrity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 IntegrityCap = -1; //this is set during comp init

    /// <summary>
    /// The absolute maximum possible integrity
    /// </summary>
    [DataField(required: true),ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxIntegrity = 10;


    /// <summary>
    /// Helper property for getting Health and Integrity together as a hitpoint pool.
    /// Don't show this to players as we want to avoid presenting absolute numbers for health/medical status.
    /// </summary>
    public FixedPoint2 HitPoints => Health + Integrity;

}