using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Effects;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics; // Mono
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Server.Explosion.Components;
using Content.Shared.Explosion.Components;
using Content.Server.Explosion.EntitySystems; // Sandwich-HL

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly DestructibleSystem _destructibleSystem = default!;

    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    private EntityQuery<PhysicsComponent> _physQuery;
    private EntityQuery<FixturesComponent> _fixQuery;

    /// <summary>
    /// Minimum velocity for a projectile to be considered for raycast hit detection.
    /// Projectiles slower than this will rely on standard StartCollideEvent.
    /// </summary>
    private const float MinRaycastVelocity = 75f; // 100->75 Mono

    public override void Initialize()
    {
        base.Initialize();

        // Mono
        _physQuery = GetEntityQuery<PhysicsComponent>();
        _fixQuery = GetEntityQuery<FixturesComponent>();

        // Mono
        UpdatesBefore.Add(typeof(SharedPhysicsSystem));
    }

    public override DamageSpecifier? ProjectileCollide(Entity<ProjectileComponent, PhysicsComponent> projectile, EntityUid target, MapCoordinates? collisionCoordinates, bool predicted = false)
    {
        var (uid, component, ourBody) = projectile;
        // Check if projectile is already spent (server-specific check)
        if (component.ProjectileSpent)
            return null;

        var otherName = ToPrettyString(target);
        // Get damage required for destructible before base applies damage
        var damageRequired = FixedPoint2.Zero;
        if (TryComp(target, out DamageableComponent? damageableComponent))
        {
            damageRequired = _destructibleSystem.DestroyedAt(target);
            damageRequired -= damageableComponent.TotalDamage;
            damageRequired = FixedPoint2.Max(damageRequired, FixedPoint2.Zero);
        }
        var deleted = Deleted(target);

        // Call base implementation to handle damage application and other effects
        var modifiedDamage = base.ProjectileCollide(projectile, target, collisionCoordinates, predicted);

        if (modifiedDamage == null)
        {
            component.ProjectileSpent = true;
            if (component.DeleteOnCollide && component.ProjectileSpent)
                QueueDel(uid);
            return null;
        }

        Logger.Info($"[APHE-Debug] ProjectileCollide aufgerufen für {uid}. Position: {_transformSystem.GetWorldPosition(uid)}, Spent: {component.ProjectileSpent}");

        bool failedToDestroy = false; // Sandwich-HL

        if (component.PenetrationThreshold != 0)
        {
            // If a damage type is required, stop the bullet if the hit entity doesn't have that type.
            if (component.PenetrationDamageTypeRequirement != null)
            {
                var stopPenetration = false;
                foreach (var requiredDamageType in component.PenetrationDamageTypeRequirement)
                {
                    if (!modifiedDamage.DamageDict.Keys.Contains(requiredDamageType))
                    {
                        stopPenetration = true;
                        break;
                    }
                }

                if (stopPenetration)
                {
                    component.ProjectileSpent = true;
                    failedToDestroy = true; // Sandwich-HL
                }
            }

            // If the object won't be destroyed, it "tanks" the penetration hit.
            if (modifiedDamage.GetTotal() < damageRequired)
            {
                component.ProjectileSpent = true;
                failedToDestroy = true; // Sandwich-HL
            }

            if (!component.ProjectileSpent)
            {
                component.PenetrationAmount += damageRequired;
                // The projectile has dealt enough damage to be spent.
                if (component.PenetrationAmount >= component.PenetrationThreshold)
                {
                    component.ProjectileSpent = true;
                }
            }
        }
        else
        {
            component.ProjectileSpent = true;
            failedToDestroy = true; // Sandwich-HL
        }

        // Sandwich-HL start
        if (component.ProjectileSpent)
        {
            // FIX: set velocity directly to 0 (only if it has explosion or trigger component), so projectile doesnt glitch through the wall before deletion due to high speed
            if (HasComp<ExplodeOnTriggerComponent>(uid) || HasComp<TriggerOnCollideComponent>(uid))
            {
                Logger.Info($"[APHE-Debug] Kugel {uid} ist SPENT. Setze Velocity auf 0.");
                _physics.SetLinearVelocity(uid, Vector2.Zero, body: ourBody);
                _physics.SetAngularVelocity(uid, 0f, body: ourBody);

                if (collisionCoordinates.HasValue)
                {
                    _transformSystem.SetMapCoordinates(uid, collisionCoordinates.Value);
                }
            }
        }

        if (component.ProjectileSpent)
        {
            bool hasTimerDelay = TryComp<TriggerOnCollideComponent>(uid, out var triggerCollide) && triggerCollide.Delay > 0f;

            if (failedToDestroy || (component.DeleteOnCollide && !hasTimerDelay))
            {
                Logger.Info($"[APHE-Debug] Zerstöre Projektil {uid} sofort via QueueDel.");
                QueueDel(uid);
            }
            else
            {
                Logger.Info($"[APHE-Debug] Überspringe Löschung für {uid}. Kugel wartet auf Timer-Explosion.");
            }
        }
        Logger.Info($"[APHE-Debug] Vor QueueDel End-Check. spent: {component.ProjectileSpent}, failedToDestroy: {failedToDestroy}, DeleteOnCollide: {component.DeleteOnCollide}, HasActiveTimer: {HasComp<ActiveTimerTriggerComponent>(uid)}");
        // Sandwich-HL end

        return modifiedDamage;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ProjectileComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var projectileComp, out var physicsComp, out var xform))
        {
            if (HasComp<TriggerOnCollideComponent>(uid)) // Sandwich-HL start
            {
                var currentPos = _transformSystem.GetWorldPosition(xform);
                Logger.Info($"[APHE-Debug] Update Tick - Proj: {uid}, Pos: {currentPos}, Vel: {physicsComp.LinearVelocity.Length()}, Spent: {projectileComp.ProjectileSpent}");
            } // Sandwich-HL end

            if (projectileComp.ProjectileSpent || TerminatingOrDeleted(uid))
                continue;

            var currentVelocity = physicsComp.LinearVelocity;
            if (currentVelocity.Length() < MinRaycastVelocity)
                continue;

            var lastPosition = _transformSystem.GetWorldPosition(xform, GetEntityQuery<TransformComponent>());
            var rayDirection = currentVelocity.Normalized();
            // Ensure rayDistance is not zero to prevent issues with IntersectRay if frametime or velocity is zero.
            var rayDistance = currentVelocity.Length() * frameTime;
            if (rayDistance <= 0f)
                continue;

            if (!_fixQuery.TryComp(uid, out var fix) || !fix.Fixtures.TryGetValue(ProjectileFixture, out var projFix))
                continue;

            var collisionMask = projFix.CollisionMask;

            var hits = _physics.IntersectRay(xform.MapID,
                new CollisionRay(lastPosition, rayDirection, collisionMask),
                rayDistance,
                uid, // Entity to ignore (self)
                false) // IncludeNonHard = false
                .ToList();

            // If IgnoreShooter is true, remove the shooter from the list of potential hits.
            if (projectileComp.IgnoreShooter && projectileComp.Shooter.HasValue)
            {
                hits.RemoveAll(hit => hit.HitEntity == projectileComp.Shooter.Value);
            }

            if (hits.Count > 0)
            {
                // Process the closest hit
                // IntersectRay results are not guaranteed to be sorted by distance, so we sort them.
                hits.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                var closestHit = hits.First();

                // teleport us so we hit it
                // this is cursed but i don't think there's a better way to force a collision here
                Logger.Info($"[APHE-Debug] Raycast Hit registriert für {uid} auf Target {closestHit.HitEntity}. Teleportiere zu Target-Pos.");
                _transformSystem.SetWorldPosition(uid, _transformSystem.GetWorldPosition(closestHit.HitEntity));
                // Sandwich-HL: start
                if (projectileComp.RaycastResetVelocity)
                {
                    var oldVelocity = physicsComp.LinearVelocity.Length();
                    var newVelocity = MinRaycastVelocity * 0.99f;

                    _physics.SetLinearVelocity(uid, rayDirection * newVelocity);

                    if (TryComp<TriggerOnCollideComponent>(uid, out var triggerCollide) && triggerCollide.Delay > 0f)
                    {
                        float requiredTotalTime = (oldVelocity / newVelocity) * triggerCollide.Delay;

                        triggerCollide.Delay = requiredTotalTime;

                        Logger.Info($"[APHE-Debug] Geschwindigkeit reduziert von {oldVelocity} auf {newVelocity}. Prototyp-Delay auf {requiredTotalTime}s erhöht.");
                    }
                }

                // Sandwich-HL End
                continue;
            }
        }
    }
}
