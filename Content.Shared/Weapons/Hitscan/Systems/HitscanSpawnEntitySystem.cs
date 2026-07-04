using Content.Shared.Damage;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged.Components; // Sandwich-HL fix
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Network;
using Robust.Shared.Prototypes; // Sandwich-HL fix

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanSpawnEntitySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!; // Sandwich-HL fix
    [Dependency] private readonly IComponentFactory _factory = default!; // Sandwich-HL fix

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(EntityUid uid, GunComponent component, ref HitscanRaycastFiredEvent args)
    {
        if (args.Canceled || args.HitEntity == null)
            return;

        if (_net.IsClient)
            return;

        if (!TryComp<HitscanBatteryAmmoProviderComponent>(uid, out var provider) || provider.Prototype == null) // Sandwich-HL fix
            return;

        if (!_proto.TryIndex<EntityPrototype>(provider.Prototype, out var proto)) // Sandwich-HL fix
            return;

        var compName = _factory.GetComponentName(typeof(HitscanSpawnEntityComponent)); // Sandwich-HL fix
        if (proto.Components.TryGetValue(compName, out var entry))
        {
            var comp = (HitscanSpawnEntityComponent) entry.Component;
            
            Spawn(comp.SpawnedEntity, Transform(args.HitEntity.Value).Coordinates);
        }
        // TODO: maybe split up the effects component or something - this wont play sounds and stuff (maybe that's ok?)
    }
}
