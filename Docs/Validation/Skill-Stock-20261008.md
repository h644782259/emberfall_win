# Two-use skill stock — 2026-10-08

Candidate only; main awaits the user's visual/gameplay acceptance. No Android work.

| Class / skill | Stock | Sequential recovery | Energy per cast | Action lock |
|---|---:|---:|---:|---:|
| Vanguard / 破军突进 (5) | 2 | 12 s | 24 | .55 s |
| Arcanist / 雷霆锁链 (4) | 2 | 9 s | 24 | .25 s |
| Ranger / 逐风跃击 (4) | 2 | 14 s | 22 | .60 s |
| Summoner / 灵能冲击 (0) | 2 | 8 s | 16 | .25 s |

Recovery is fixed across ranks; existing run cooldown modifiers apply once when a queue begins (floor 70%). A second cast does not restart or alter the queue. Example: Vanguard casts at t=0 and t=3 recover at t=12 and t=24. Full stock has no hidden progress. Cooldown reduction carries across missing uses but cannot exceed two or shorten the action lock. Windup/hold `SkillChargeController` remains a separate system.

Per-cast damage and energy are unchanged. No additional stocks for fields, arrow rain, fire ride, healing, shields or ultimates. Movement keeps existing obstacle, landing, jumping, and owner/epoch guards. Empty/occluded lightning targets reject before charge/energy commitment. Same-frame repeated calls cannot emit another cast.

## Output and risk budget

For isolated rank-one casts, attack-normalized single-target coefficients are Vanguard 1.56, lightning 1.218, Ranger landing 1.584, Summoner 1.8. Lightning may hit up to six distinct targets at rank one; this does not multiply its single-target value. Existing higher-rank auxiliary/tail effects remain unchanged.

| Class | Casts in [0,3) / energy | Casts in [0,60) / energy | 3 s damage coefficient / DPS | 60 s coefficient / DPS |
|---|---:|---:|---:|---:|
| Vanguard | 2 / 48 | 6 / 144 | 3.12 / 1.04 | 9.36 / .156 |
| Arcanist | 2 / 48 | 8 / 192 | 2.436 / .812 | 9.744 / .1624 |
| Ranger | 2 / 44 | 6 / 132 | 3.168 / 1.056 | 9.504 / .1584 |
| Summoner | 2 / 32 | 9 / 144 | 3.6 / 1.2 | 16.2 / .27 |

These are analytical ceilings with all casts hitting one target, no armor/crit/modifier/pets/basic attacks and ample energy; not a rendered damage capture or total-build DPS. Actual `SkillRuntime` simulation verifies cast counts and energy; existing movement production tests verify landing/field single-cast budgets. The 48-scenario combo ledger still accounts for basics, energy, delayed hits and pets rather than adding every full skill duration into a short burst window.

Stock front-loads an extra paid action, increasing short-window burst. Dash/leap require the same movement commitment and risk; leap cannot repeat in the air, and action locks prevent overlapping dash protection. Lightning/impulse are immediate releases with short input occupancy, not persistent additional fields. Holding the second use retains escape/control flexibility at the expense of burst. Higher-rank echoes retain their existing bounded budgets.

## Persistence

Four per-class banks live in the existing profile. Legacy saves receive two once, with migration/version durably written before publication. Each spend writes debt before energy/emission; failed writes reject the action and surface the existing error. Recharge updates memory and is captured by ordinary saves; normal reload does not grant wall-clock recharge. A crash can lose unsaved recovery progress conservatively. This is not protection against manually restoring external backups.

Hotbar slot/preset changes cannot refill. Recharge progress does not invalidate merchant/build confirmations, and committing an edit retains live bank progress. Class changes restore that class's stored bank; inactive class banks are frozen, so switching is not an offline recharge mechanism. Retired owners cannot write. Committed new dungeon entry refills as the existing cooldown reset does; room transitions and cancelled selection do not.

## Validation scope

New production test: timing, two-use cap, reduction, pause/malformed dt, run modifier, full per-cast energy, save/reload, failed save, migration, malformed stock data, slot/preset and build draft behavior. Class-switch runtime and actual cast fixtures cover owner retirement, class banks, no-target/no-LOS/no-save/no-emission. HUD fixture checks 2/2,1/2,0/2 badge and ring containment. Updated existing progression/run/combo/blessing expectations explicitly recognize sequential stock.

Final full-suite results are recorded separately. All local tests use managed boundaries; no Unity Editor, real JsonUtility, rendering, physics/device playtest or platform package build is claimed.
