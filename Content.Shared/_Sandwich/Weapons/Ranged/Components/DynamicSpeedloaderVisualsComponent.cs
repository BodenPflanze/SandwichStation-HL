namespace Content.Shared._Sandwich.Weapons.Ranged;

[RegisterComponent]
public sealed partial class DynamicSpeedloaderVisualsComponent : Component
{
    [DataField("needsAppearanceUpdate")]
    public bool NeedsAppearanceUpdate = false;

    /// <summary>
    /// Mapped from the ammunition prototype ID to the sprite prefix.
    /// Example: "Cartridge45_magnumUranium" -> "uranium"
    /// </summary>
    [DataField("ammoPrefixMap")]
    public Dictionary<string, string> AmmoPrefixMap = new()
    {
        {"Cartridge45_magnumFMJ", "base"},
        {"Cartridge45_magnumPractice", "practice"},
        {"Cartridge45_magnumAP", "piercing"},
        {"Cartridge45_magnumIncendiary", "incendiary"},
        {"Cartridge45_magnumUranium", "uranium"},
        {"Cartridge45_magnumRubber", "rubber"},
        {"Cartridge45_magnumHoly", "holy"}
    };
}