using Content.Shared.Damage;
using Content.Shared.Physics;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.FixedPoint; // Sandwich-HL

namespace Content.Shared.Weapons.Ranged;

[Prototype]
public sealed partial class HitscanPrototype : IPrototype, IShootable
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("staminaDamage")]
    public float StaminaDamage;

    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public DamageSpecifier? Damage;

    [ViewVariables(VVAccess.ReadOnly), DataField("muzzleFlash")]
    public SpriteSpecifier? MuzzleFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField("travelFlash")]
    public SpriteSpecifier? TravelFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField("impactFlash")]
    public SpriteSpecifier? ImpactFlash;

    [DataField("collisionMask")]
    public int CollisionMask = (int) (CollisionGroup.Opaque | CollisionGroup.HitscanImpassable);

    /// <summary>
    /// What we count as for reflection.
    /// </summary>
    [DataField("reflective")] public ReflectType Reflective = ReflectType.Energy;

    /// <summary>
    /// Sound that plays upon the thing being hit.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Force the hitscan sound to play rather than potentially playing the entity's sound.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("forceSound")]
    public bool ForceSound;

    /// <summary>
    /// Try not to set this too high.
    /// </summary>
    [DataField("maxLength")]
    public float MaxLength = 60f;

    // Sandwich-HL start: Hitscan Penetration
    /// <summary>
    /// The maximum amount of damage the projectile can "absorb" before the beam collapses completely.
    /// </summary>
    [DataField("penetrationThreshold")]
    public FixedPoint2 PenetrationThreshold = FixedPoint2.Zero;

    /// <summary>
    /// Optional damage type condition (as with projectiles).
    /// </summary>
    [DataField("penetrationDamageTypeRequirement")]
    public List<string>? PenetrationDamageTypeRequirement;
    // Sandwich-HL end
}
