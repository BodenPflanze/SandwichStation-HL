// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Shared._Goobstation.Weapons.DelayedKnockdown;   // moved this to Content.Shared because why the hell does goob devide their stuff in 8 different Content Files

[RegisterComponent]
public sealed partial class DelayedKnockdownComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float Time = float.MaxValue;

    [ViewVariables(VVAccess.ReadWrite)]
    public float KnockdownTime = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Refresh = true;
}