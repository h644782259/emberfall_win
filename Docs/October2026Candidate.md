# Emberfall candidate — 2026-10-07

Local review candidate only. Publication is paused by the latest user instruction. No push/merge, no candidate GitHub CI, no Unity Editor, Metal, Xcode build or device execution has occurred in this Linux environment. Android repository untouched. No save schema change or historical WIP merge.

## Scope implemented

- Existing main already grows class base stats on level-up. Added explicit growth feedback and tests for every class through level 100, including reload non-duplication. Per level HP/attack/armor: Vanguard 20/3/1.4; Arcanist 15/3.5/1; Ranger 17/3.2/1.1; Summoner 13/2.3/0.75.
- Mobile bag: persistent current-character preview and equipment slots, equipment/supplies switches inside bag, adaptive category grid, inline wear/compare, small padlock with 44-unit hit target. Attributes/mechanics comparison stays inside bag. Equipment appearance comparison removed from both platforms. Actual current equipment and owned/unowned fashion try-on remain.
- PC grid expands to six columns when comparison is closed. PC supplies and fashion now use the same internal bag area, reached from small bag switches or current-wear controls.
- Bag usage consumes existing potions; purchasing/selling belongs to merchant; reinforcement belongs to smith. Locked/worn sale protection retained. iOS now has dedicated merchant and smith views.
- Drag input consumes accidental clicks without globally disabling/recoloring GUI. Reinforcement no longer invokes transition lock. Preview instances receive isolated render stages and recover resources on focus/lifecycle transitions. These are code changes, not proof that the reported blank Metal preview is fixed.
- Context action appears only near a real interaction, moved away from potion. iOS skill tree uses compact nodes. Camp NPCs moved off central road; actual collision/reachability still requires Unity playthrough.
- Arcanist Burn path gets visible flame riding geometry and capped trail damage; Ranger movement slot becomes forward leap with landing damage/knockback. Skill icons have differentiated silhouettes and colors. Existing ranged arrow rain is retained.

## Skill budget (A = attack snapshot, no crit/armor/passive modifiers)

| Skill ranks 1/2/3 | Total per target | Cooldown | Occupancy / risk |
|---|---|---|---|
| Burn flame ride | 2.16 / 2.88 / 3.60 A maximum | 26 / 24.18 / 22.36 s | 6/8/10 s detached producer, 0.5 s ticks at 0.18 A; overlapping footprints do not stack, walls checked. Must remain near enemies/path; 30% guard DR, +20% move. |
| Ranger forward leap | 1.584 / 2.7456 / 4.224 A landing | 12 / 11.16 / 10.32 s | 0.55 s real jump blocks basics; 0.18 s initial invulnerability; radius 3.2 × rank range, knockback 2.4. Rank 3 retains old origin tail, 1.024 A extra. |
| Ranger existing arrow rain | 3.8 / 5.4 / 7.2 A including rank-3 finisher | 19 / 17.67 / 16.34 s | Detached 4/5/6 s field after 0.3 s startup; 0.0884 s basic recovery. Enemies may leave the zone. |

Normalized contribution: flame ride 0.083/0.119/0.161 A/s; leap landing 0.132/0.246/0.409 A/s (rank-3 retained tail +0.099 A/s); arrow rain 0.200/0.306/0.441 A/s. Ranger basics are 0.78 A per 0.34 s = 2.294 A/s ideal. A leap forfeits about 1.262 A of theoretical basic opportunity; its net incremental contribution over uninterrupted basics is roughly 0.027/0.133/0.287 A/s plus retained rank-3 tail. Rain forfeits about 0.203 A per cast from recovery, net roughly 0.189/0.294/0.428 A/s. These sums assume every hit connects and sufficient energy, exclude crit/procs and movement losses, and are not observed device DPS. Detached damage can overlap basics; leap damage cannot while airborne. Flame costs 28 energy, leap 22, rain 34; no energy-income bypass introduced.

The retained Ranger landing buff must also be counted: for 5/6/7 s, basics gain 12/24/36% damage and their interval becomes 0.8 times normal (25% more attacks). Ideal buffed basic DPS is therefore 3.212/3.556/3.900 A/s. Averaged over the movement cooldown this adds 0.382/0.678/1.089 A/s. Including this existing buff, landing, rank-3 tail and the 0.55 s basic opportunity loss, total net incremental leap value is approximately 0.409/0.811/1.476 A/s. Rank-3 leap + rain + basics is about 4.19 A/s with continuous contact; rain recovery loses more basic damage if cast during the mobility buff. This is a steady-state upper estimate, excludes the ultimate, crits and build procs, and does not count the same basic stream twice. The buff is unchanged from main; forward landing makes maintaining it riskier than the old retreat.

Arcanist basics are 1.15/0.52 = 2.212 A/s. Flame ride's 0.1092 s basic recovery costs about 0.242 A per cast, so its net detached addition is approximately 0.074/0.109/0.150 A/s before contact losses. Guard reduces incoming damage but does not remove the close-range risk or make the character invulnerable.

## Verification and blockers

Targeted managed tests cover growth (761 assertions), skill coefficients (77), adaptive grid geometry (2904), preview lifecycle (1342), actual flame producer and extracted leap methods using engine doubles. Runtime source compiles against the runner's pinned Unity API reference, not an actual Unity player build. Frozen final logs/report accompany the bundle; skipped checks are not passing checks.

Immutable Windows main baseline: 287 checks, 271 pass, 16 fail. Baseline report is /tmp/emberfall-main-baseline/report.json. Baseline failures: progression, enemy-kill-rewards, reforgeselection, returning-counter-persistence, chest-choice-presentation, build-plan-page-geometry, scenery-presentation-production, fixed-scenery-production, fixed-scenery-enabled-integration, concentrated-venom-production, camp-practice-session, opportunity-channels-round2, camp-practice-production, g07-factory-inventory, reward-presentation-exceptions, practice-potion-input. Do not describe baseline or candidate as wholly green.

Combat HUD implemented from the latest authorized text requirements: eight differentiated active skill icons in a compact bottom-right cluster, 48-unit minimum targets, 54-unit ultimate with a distinct ready ring, safe-area right margin of 6 units, small top-right function icons, map and objectives at upper left, potion count below its glyph with full-health text above. Context actions only appear near actionable objects, away from potion. Both mobile trees use compact nodes. Camp service reachability and road/prop clearances have 304 managed assertions; HUD layout has 15,257 managed assertions. Production raster and geometry exports are diagnostic artifacts, not Unity screenshots.

Reference-image pixel matching remains unverified: official Library materialization returned metadata but no workspace_path; its unchanged transfer helper failed because the configured HTTP proxy refused CONNECT with HTTP 403 Forbidden (URLError wrapping OSError). Direct DNS failure was an incomplete earlier diagnosis. No bypass was attempted. Reference Library IDs: libfile_2106ee2b247881918ea6b04e3c7115ba, libfile_1dbe6ac8509881918d2547d07b021015, libfile_020b2c22c2b08191a0ac1095df6037aa. Mac candidate transfer is also blocked; the parent's successful unsigned baseline Xcode build does not validate this candidate.

Git fetch confirmed unchanged remote main bases. GitHub Actions read via gh failed Forbidden; remote CI status is unknown. Candidate is local-only.

## Mac QA handoff

Fetch candidate branch from supplied local git bundle into an isolated worktree, never replace dirty main. Import with Unity 6000.6.4f1, run repository Unity checks, build iOS Xcode scheme Emberfall, test iPhone 16 Pro and iPad Air 4. Preserve old save and verify load, each-class level-up, bag density and scroll/cancel/multitouch, inline equip/lock/compare/potion, model changing with actual gear, every fashion rarity trial (owned/unowned) and reopening/focus recovery, merchant protected sales/preset confirmation, repeated smith reinforcement, NPC approach/departure, flame overlap/pausing/death and leap walls/landing. Capture screenshots/video and actual errors. This candidate is not ready for final acceptance until remaining requirements and device checks are resolved.
