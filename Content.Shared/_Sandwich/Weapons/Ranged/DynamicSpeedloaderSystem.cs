using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared._Sandwich.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._Sandwich.Weapons.Ranged;

public sealed class DynamicSpeedloaderSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<DynamicSpeedloaderVisualsComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<DynamicSpeedloaderVisualsComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        
        SubscribeLocalEvent<DynamicSpeedloaderVisualsComponent, MapInitEvent>(OnMapInit, 
            after: new[] { typeof(SharedGunSystem) });

        SubscribeLocalEvent<DynamicSpeedloaderVisualsComponent, GunCycledEvent>(OnGunCycled);

        SubscribeLocalEvent<DynamicSpeedloaderVisualsComponent, TakeAmmoEvent>(OnTakeAmmo, after: new[] { typeof(SharedGunSystem) });
    }

    private void OnMapInit(EntityUid uid, DynamicSpeedloaderVisualsComponent component, MapInitEvent args)
    {
        UpdateAppearance(uid, component);
    }

    private void OnContainerModified(EntityUid uid, DynamicSpeedloaderVisualsComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID == "ballistic-ammo")
            UpdateAppearance(uid, component);
    }

    private void OnGunCycled(EntityUid uid, DynamicSpeedloaderVisualsComponent component, ref GunCycledEvent args)
    {
        UpdateAppearance(uid, component);
    }

    private void OnTakeAmmo(EntityUid uid, DynamicSpeedloaderVisualsComponent component, TakeAmmoEvent args)
    {
        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, DynamicSpeedloaderVisualsComponent component)
    {
        if (!TryComp<BallisticAmmoProviderComponent>(uid, out var ammoProvider))
            return;

        var prefixes = new string[6];
        var physicalCount = ammoProvider.Entities.Count;
        
        var unspawnedCount = ammoProvider.UnspawnedCount; 

        for (int i = 0; i < 6; i++)
        {
            // 1. Check physical entties in container
            if (i < physicalCount)
            {
                var ammoEnt = ammoProvider.Entities[i];
                var protoId = MetaData(ammoEnt).EntityPrototype?.ID;

                if (protoId != null && component.AmmoPrefixMap.TryGetValue(protoId, out var prefix))
                    prefixes[i] = prefix;
                else
                    prefixes[i] = "base"; 
            }
            // 2. then fill "virtual" pullets (Lazy Loading)
            else if (i < physicalCount + unspawnedCount)
            {
                var protoId = ammoProvider.Proto;

                if (protoId != null && component.AmmoPrefixMap.TryGetValue(protoId, out var prefix))
                    prefixes[i] = prefix;
                else
                    prefixes[i] = "base"; 
            }
            // 3. slot empty
            else
            {
                prefixes[i] = "empty";
            }
        }

        // Appearance-Update for the Client
        _appearance.SetData(uid, SpeedLoaderVisuals.AmmoPrefixes, prefixes);
    }
}

// Note: Der is a bug with fresh full speedloaders where the sprites behave weirdly or 2 bullets can be dropped
// This bug is not from us, its from standart wizden. Either they have it fixed but it aint ported or its still like this.