using Robust.Shared.Serialization;

namespace Content.Shared._Sandwich.Weapons.Ranged;

[Serializable, NetSerializable]
public enum SpeedLoaderVisualLayers : byte
{
    Base,
    Chamber1,
    Chamber2,
    Chamber3,
    Chamber4,
    Chamber5,
    Chamber6
}

[Serializable, NetSerializable]
public enum SpeedLoaderVisuals : byte
{
    AmmoPrefixes
}