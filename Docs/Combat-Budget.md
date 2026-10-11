> 后续实际组合、时序和资源账本见 第三轮检查 与 [Combo-Budget](Combo-Budget.md)。本文保留早期属性/防御设计尺的记录；其中2A输出率不是实测DPS或击杀时间，也不能代替当前技能组合。

# Combat budget proposal — round 2

These are explicit design assumptions and deterministic formula checks, not a
measured Unity playtest or a simulation of complete player/enemy rotations.
Production formulas are in `Assets/Scripts/Core/CombatBalance.cs`; focused
checks are in `Tests/CombatBalanceTests.cs`.

## Reproduced legacy failure

At level 100, Vanguard, three level-100 Epic items, all slots +10, rank-three
attack passive, no fashion/mastery: old 12%-per-step rounded compounding yields
weapon attack 1426, relic attack 573, armor defense 688, armor HP 2292, relic HP
1712. Therefore attack = (317 + 1426 + 573) × 1.22 = **2825.52**, armor =
145.6 + 688 + 7 = **840.6**, and HP = 2150 + 2292 + 1712 = **6154**.
Old level-100 Boss HP = 310 + 100 × 65 = **6810**, only **2.41 noncritical
basic hits** of panel damage. Old armor damage multiplier =
100 / (100 + 840.6 × 4) = **0.028882**, or **97.11% passive mitigation**,
before shields/passives. Level-derived stats stopped increasing at 100, so using
only `min(100, playerLevel + tier - 1)` erased tier difficulty after the cap.

## Joint correction

- Equipment: original baseline + round(original baseline × 5% × slot rank),
  with 1-per-rank minimum attack/defense or 2 HP on nonzero tiny attributes.
  +10 normally totals +50%, rather than approximately ×3.106. Absent attributes
  remain zero. Evaluate from immutable bases; do not compound the current cache.
- The exact old Epic+10 Vanguard example becomes attack **1564.04**, armor
  **484.6**, HP **4084** (no fashion/mastery), while retaining all slot ranks.
- Armor: K = 50 + 4 × defenderLevel; incoming multiplier = max(0.30,
  K/(K+armor)). Passive armor mitigation caps at 70%. After multiplying temporary
  shields/passives, clamp the final multiplier to at least 0.16 (84% total DR).
  Do not apply that floor to real dodge/invulnerability frames; return before it.
- Large advanced multi-event sequences multiply their existing damage budgets by
  0.24. Vanguard rank-three ultimate upper envelope changes from about 46.24×
  panel attack to 11.0976× across the whole sequence, not per strike. Healing is
  untouched by the damage normalization.
- Monster level is the actual progression level; do not also add dungeon tier.
  Use independent tier multipliers to avoid double scaling below level 100 and
  erased scaling after level 100.
- Boss base HP = 750 + 590L; base direct hit = 35 + 8L. Ordinary HP by kind:
  slime 80 + 30L, goblin 95 + 36L, wisp 70 + 28L, guardian 180 + 60L;
  ordinary base damage = 10 + 3.8L. Preserve existing telegraph, attack cooldown,
  charge/slam coefficients and tactical abilities.
- Let s = clamp(tier,1,100)-1. HP multiplier = 1 + .009s + .000035s².
  Damage multiplier = 1 + .014s + .00008s². Damage pressure grows faster than
  HP, so upper tiers ask for avoidance/defensive investment, not only longer DPS.

This is not an HP-only change: excess compounding, passive near-immunity,
stacked defenses, unbounded multi-hit budgets, and the missing independent tier
axis are all corrected together. The larger Boss HP is deliberate because the
legacy Boss had approximately one second of basic-attack health at level 100.

## Budget assumptions

High-investment characters are expected to farm low tiers quickly and choose
progression tiers for a repeated attack/avoid/defend loop. The table is not a
requirement that a maximal build must spend 35 seconds on a first-tier Boss.

All rows below use **Vanguard as the reproduced reference class**, not a claim
that four classes have identical practical DPS. Rolled bases are the current
production formulas, all three item slots use the same rarity/upgrade rank.
Attack passive is the legal maximum at each level: rank 2 at level 20, rank 3 at
50/100. Ordinary = Common+0; formed = Epic+5; high investment = Legendary+10
plus Legendary weapon/wing fashion. At level 100 only, high investment uses the
new competing mastery budget of Offense35/Vitality34 (69 total), with no Guard
or Technique points. One-off perfect-dodge core procs are **excluded**. Do not
sum mutually exclusive or unaffordable maximum mastery tracks.

The time column is only BossHP / (2 × panel attack), a clearly named common
output-rate ruler. It is **not measured TTK**: movement, target uptime, crits,
cast timing, energy economy, class mechanics, death and defensive play all
change it. A preliminary target is approximately 15–35 seconds for a formed
low-tier encounter, with roughly 12 seconds acceptable for a heavily invested
farm build; challenging pushes can reach 30–60 seconds with lost attack uptime.
The ordinary baseline is intentionally slower (~36 seconds by the ruler).

### Panel stats and tier-1 reference

| Lv | Build | Attack | Armor | HP | Armor DR | Boss HP | Boss slam / player HP | Output-rate ruler |
|---|---|---:|---:|---:|---:|---:|---:|---:|
|20|Common+0|175.56|64.60|706|33.20%|12550|25.83%|35.74s|
|20|Epic+5|286.14|98.60|902|43.13%|12550|17.21%|21.93s|
|20|High investment|456.03|150.77|1272.32|53.70%|12550|9.93%|13.76s|
|50|Common+0|425.78|145.60|1516|36.80%|30250|25.39%|35.52s|
|50|Epic+5|705.16|223.60|1974|47.21%|30250|16.29%|21.45s|
|50|High investment|1130.33|345.17|2825.76|58.00%|30250|9.05%|13.38s|
|100|Common+0|822.28|275.60|2866|37.98%|59750|25.30%|36.33s|
|100|Epic+5|1367.62|428.60|3762|48.78%|59750|15.92%|21.84s|
|100|High investment|2434.84|663.77|6337.09|59.60%|59750|7.45%|12.27s|

Slam uses the actual existing ×1.4 attack coefficient and armor only; an active
shield lowers it, while the risk blessing increases it. Elite guardian HP is
1380/3180/6180 at these levels before tier scaling; its frontal armor still
makes flank/interrupt choices matter.

### Level-cap tier axis

| Lv100 tier | Boss HP | Base damage | High-investment slam | High-investment output-rate ruler |
|---|---:|---:|---:|---:|
|1|59750|835|7.45% HP|12.27s|
|11|65336.62|958.58|8.56% HP|13.42s|
|21|71341.50|1095.52|9.78% HP|14.65s|
|100|133483.59|2647.02|23.63% HP|27.41s|

Tier 100 versus tier 11 has **2.043× HP and 2.761× base damage**, even with enemy
level fixed at 100. Formed Epic+5 instead takes ~50.45% HP from a tier-100 slam;
Common+0 takes ~80.19%. Those under-invested builds are explicitly not the
recommended tier-100 target. The high-investment build survives about four
unmitigated slams at tier 100 rather than passively ignoring them. The same
build's entire normalized ultimate envelope removes ~45.2% of tier-1 Boss HP
or ~20.2% at tier 100. A full-sequence critical roll raises those to about
74.6% / 33.3%. Conservative Arcanist/Ranger full-crit envelopes also remain
below 80% of a fresh tier-1 Boss (before external vulnerability/setup). Neither
a single strike nor one fresh critical ultimate deletes it. Setup combinations
and sustained summons still need class-specific playtests.

Full 30-row matrix, including demonstration tiers 1, floor(level/10)+1 and that
tier+10, is in `Combat-Budget.csv`. Those demonstration tiers are a test grid,
not an enforced unlock rule or a claim that the UI recommends them.

## Integration boundaries and remaining validation

Call `UpgradeValue(basis,rank,minGrowth,cap)` from item projection; keep the old
12% routine only for deterministic recovery of legacy bases, never new growth.
Call `EnemyHealth(level,tier,boss,kind)` and `EnemyDamage(level,tier,boss)` at
spawn. Wilderness uses tier 1. Clamp input level/tier in this helper, not by
reusing tier as a level increment. Use `ArmorDamageMultiplier(armor,level)` and
apply the combined multiplier floor after temporary mitigation. `RankPower`
retains the existing three skill ranks.

Pure checks cover finite outputs, malformed input, noncompounding rounding,
all valid level/tier pairs, monotonic progression, armor caps, and the tier-11 vs
100 distinction. The full project still needs the lead's integrated tests and
Unity playtests across classes; the output-rate ruler is not a substitute for
those. Migration must intentionally explain the new lower displayed numbers,
while preserving item bases, rarity, slot investment and skill choices.

### Production callsite review

The reviewed integration calls use player level and a separate tier at enemy
spawn; wilderness uses tier 1. The mitigation floor precedes the risk-contract
multiplier, and true invulnerability returns first. Item projection uses the new
linear helper, while old 12% inversion is retained only to recover legacy bases.
Summoner direct impulse is 1.8× rank power; gravity can tick up to 6 times, totaling
5.9× rank power before crits (only its 3.2× finisher crits). It does not need the
classic advanced-sequence factor: those are already lower bounded budgets.

Returning-blade four two-target basic attacks produce at least
8×0.92 +1.10 =8.46 attack units, versus8 without the mechanism, before its new
next-counter bonus. Integration invariant: a newly granted `counterTime` must not be immediately
consumed by the same melee call. Capture the pre-attack counter state and
preserve a newly generated one.
