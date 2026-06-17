// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Timing;                      //Sandwich-HL

using Content.Shared._Goobstation.Weapons.DelayedKnockdown;
using Content.Goobstation.Shared.Clothing;
/* No heretics so far
using Content.Server.Heretic.Components.PathSpecific;
using Content.Shared.Heretic.EntitySystems.PathSpecific;
using Content.Shared._Goobstation.Heretic.Components;
*/
using Content.Shared._Shitcode.Weapons.Misc;
using Content.Shared.Armor;
using Content.Shared.Damage.Events;
/* No heretics so far
using Content.Shared.Heretic.Components.PathSpecific;
*/
using Content.Shared.Inventory;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;          //Sandwich-HL
using Content.Shared.Item.ItemToggle.Components;    //Sandwich-HL

namespace Content.Goobstation.Shared.Weapons.DelayedKnockdown;

public sealed class DelayedKnockdownOnHitSystem : EntitySystem
{
    //[Dependency] private readonly Content.Shared.StatusEffectNew.StatusEffectsSystem _status = default!;    // Im not gonna import the weird "StatusEffectNew" stuff from goob - too much work
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    //[Dependency] private readonly ChampionStanceSystem _champion = default!;     // We dont have heretics
    [Dependency] private readonly IGameTiming _timing = default!;                  //Sandwich-HL

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<DelayedKnockdownOnHitComponent, StaminaDamageMeleeHitEvent>(OnHit); // Goob massively changed things to vanilla stamina-
        SubscribeLocalEvent<DelayedKnockdownOnHitComponent, MeleeHitEvent>(OnHit);                // we are gonna use this instead as long as we won't import more features from goob

        SubscribeLocalEvent<ModifyDelayedKnockdownComponent, DelayedKnockdownAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ModifyDelayedKnockdownComponent, InventoryRelayedEvent<DelayedKnockdownAttemptEvent>>(
            OnInventoryAttempt);
        SubscribeLocalEvent<ModifyDelayedKnockdownComponent, ArmorExamineEvent>(OnExamine);

        /* We dont have heretics
        SubscribeLocalEvent<ChampionStanceComponent, DelayedKnockdownAttemptEvent>(OnChampionDelayedKnockdownAttempt);
        SubscribeLocalEvent<SilverMaelstromComponent, DelayedKnockdownAttemptEvent>(OnMaelstromDelayedKnockdownAttempt);
        */
    }

    /* We dont have heretics
    private void OnMaelstromDelayedKnockdownAttempt(Entity<SilverMaelstromComponent> ent,
        ref DelayedKnockdownAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnChampionDelayedKnockdownAttempt(Entity<ChampionStanceComponent> ent,
        ref DelayedKnockdownAttemptEvent args)
    {
        if (!_champion.Condition(ent))
            return;

        args.Cancel();
    }
    */

    private void OnExamine(Entity<ModifyDelayedKnockdownComponent> ent, ref ArmorExamineEvent args)
    {
        var comp = ent.Comp;

        if (comp.Cancel)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupOrThrow(Loc.GetString("armor-examine-cancel-delayed-knockdown"));
            return;
        }

        if (comp.DelayDelta != 0f)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupOrThrow(Loc.GetString("armor-examine-modify-delayed-knockdown-delay",
                ("amount", MathF.Abs(comp.DelayDelta)),
                ("deltasign", MathF.Sign(comp.DelayDelta))));
        }

        if (comp.KnockdownTimeDelta != 0f)
        {
            args.Msg.PushNewline();
            args.Msg.AddMarkupOrThrow(Loc.GetString("armor-examine-modify-delayed-knockdown-time",
                ("amount", MathF.Abs(comp.KnockdownTimeDelta)),
                ("deltasign", MathF.Sign(comp.KnockdownTimeDelta))));
        }
    }

    private void OnInventoryAttempt(Entity<ModifyDelayedKnockdownComponent> ent,
        ref InventoryRelayedEvent<DelayedKnockdownAttemptEvent> args)
    {
        OnAttempt(ent, ref args.Args);
    }

    private void OnAttempt(Entity<ModifyDelayedKnockdownComponent> ent, ref DelayedKnockdownAttemptEvent args)
    {
        var comp = ent.Comp;

        if (comp.Cancel)
        {
            args.Cancel();
            return;
        }

        args.DelayDelta += comp.DelayDelta;
        args.KnockdownTimeDelta += comp.KnockdownTimeDelta;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        // prevents visual client fall prediction
        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<DelayedKnockdownComponent, StatusEffectsComponent>();
        while (query.MoveNext(out var uid, out var comp, out var status))
        {
            comp.Time -= frameTime;

            if (comp.Time > 0)
                continue;

            _stun.TryKnockdown(uid, TimeSpan.FromSeconds(comp.KnockdownTime), comp.Refresh);

            RemCompDeferred(uid, comp);
        }
    }

    /*                                                                                                       // Had to replace this because of us still using vanilla stamina system, not goob
    private void OnHit(Entity<DelayedKnockdownOnHitComponent> ent, ref StaminaDamageMeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        var (uid, comp) = ent;

        if (!comp.ApplyOnHeavyAttack && args.Direction != null)
            return;

        if (TryComp(uid, out UseDelayComponent? delay))
            _delay.TryResetDelay((uid, delay), id: comp.UseDelay);

        foreach (var (hit, _) in args.HitEntities)
        {
            if (!_status.CanAddStatusEffect(hit, "StatusEffectStunned")) // holy fucking slop
                continue;

            var ev = new DelayedKnockdownAttemptEvent();
            RaiseLocalEvent(hit, ev);
            if (ev.Cancelled)
                continue;

            var delayedKnockdown = EnsureComp<DelayedKnockdownComponent>(hit);
            delayedKnockdown.Time = MathF.Min(comp.Delay + ev.DelayDelta, delayedKnockdown.Time);
            delayedKnockdown.KnockdownTime =
                MathF.Max(comp.KnockdownTime + ev.KnockdownTimeDelta, delayedKnockdown.KnockdownTime);
            delayedKnockdown.Refresh &= comp.Refresh;
        }
    }
    */
    // Replace:
    private void OnHit(Entity<DelayedKnockdownOnHitComponent> ent, ref MeleeHitEvent args)
    {
    // Check if success
    if (!args.IsHit || args.HitEntities.Count == 0)
        return;

    var (uid, comp) = ent;

    // Ignore heavy attacks
    if (!comp.ApplyOnHeavyAttack && args.Direction != null)
        return;

    if (TryComp(uid, out UseDelayComponent? delay))
        _delay.TryResetDelay((uid, delay), id: comp.UseDelay);

    // check if weapon is on
    if (TryComp<ItemToggleComponent>(ent, out var toggle) && !toggle.Activated)
        return;

    foreach (var hit in args.HitEntities)
    {
        if (!_status.CanApplyEffect(hit, "KnockedDown"))
            continue;

        var ev = new DelayedKnockdownAttemptEvent();
        RaiseLocalEvent(hit, ev);
        if (ev.Cancelled)
            continue;

        var delayedKnockdown = EnsureComp<DelayedKnockdownComponent>(hit);
        delayedKnockdown.Time = MathF.Min(comp.Delay + ev.DelayDelta, delayedKnockdown.Time);
        delayedKnockdown.KnockdownTime =
            MathF.Max(comp.KnockdownTime + ev.KnockdownTimeDelta, delayedKnockdown.KnockdownTime);
        delayedKnockdown.Refresh &= comp.Refresh;
        }
    }
    // Replace end
}