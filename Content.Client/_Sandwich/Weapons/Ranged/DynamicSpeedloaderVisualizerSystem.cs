using Content.Shared._Sandwich.Weapons.Ranged;
using Robust.Client.GameObjects;

namespace Content.Client._Sandwich.Weapons.Ranged;

public sealed class DynamicSpeedloaderVisualizerSystem : VisualizerSystem<DynamicSpeedloaderVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, DynamicSpeedloaderVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<string[]>(uid, SpeedLoaderVisuals.AmmoPrefixes, out var prefixes, args.Component))
            return;

        for (int i = 0; i < 6; i++)
        {
            // Konvertiert den Index (0-5) in den passenden Layer (Chamber1 bis Chamber6)
            var layer = (SpeedLoaderVisualLayers) (i + 1); 
            
            if (!args.Sprite.LayerMapTryGet(layer, out var layerIndex))
                continue;

            if (i < prefixes.Length && prefixes[i] != "empty")
            {
                args.Sprite.LayerSetVisible(layerIndex, true);
                
                // Setzt den State, z.B. "uranium-1", "rubber-2" etc.
                args.Sprite.LayerSetState(layerIndex, $"{prefixes[i]}-{i + 1}");
            }
            else
            {
                args.Sprite.LayerSetVisible(layerIndex, false);
            }
        }
    }
}
