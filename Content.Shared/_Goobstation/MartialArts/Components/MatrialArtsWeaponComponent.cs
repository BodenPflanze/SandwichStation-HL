// This entire script is a addon from Sandwich-HL
// We have this so Deathsquad can actually use Krav Maga with their gloves

using Robust.Shared.GameObjects;

namespace Content.Shared._Goobstation.MartialArts.Components;

/// <summary>
// If a weapon uses this component, allow the carrier to continue using Matrial Arts (like Krav Maga or CQC)
/// </summary>
[RegisterComponent]
public sealed partial class MartialArtsWeaponComponent : Component
{
}