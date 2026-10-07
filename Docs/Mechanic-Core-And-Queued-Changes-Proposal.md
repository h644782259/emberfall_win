# Mechanic cores and queued changes — review proposal

Status: design only. No economy, save migration, class data, rewards, world geometry or skill damage changed. This document describes proposed rules, not shipped behavior. User-found Ember Staff is a closed discovery issue; no compensation is proposed.

## Core contract

- Twelve fragments buy one attachable class/slot-bound mechanic core, never an automatically equipped replacement item. Existing five mechanics retain their current weapon/relic compatibility.
- Core identity, mechanic and selected variant are separate from gear identity, level, rarity, training and base attributes. Proposed cores have no gear-stat quality or level. A core remains attached to its class equipment slot when that slot's gear changes; explicit replacement returns the previous core safely.
- A/B plans store core IDs per slot alongside existing equipment references. Applying a full plan resolves both sets before one transaction; failure rolls back both. Practice and camp drafts use projections and never detach live cores or consume inventory.
- Class bindings are class-local; inventory and currencies retain existing shared ownership. Variant knowledge remains keyed by its existing mechanic identity. Class switching restores compatible bindings without converting, buying, or destroying cores.
- Acquisition must return an authoritative receipt with destination (inventory or protected pending), ID and View action. A successful pending delivery must not be presented as failure because an informational LastError is present. Pending presence and claimability are distinct UI states.

## Legacy preservation and migration

- Preserve old equipment ID, level, rarity, base attributes, training anchors, equipped references and A/B references exactly. Extract only its mechanic identity/selected variant, once, with an old-item-to-core migration receipt. Disable the legacy item's mechanic only in the same durable transaction that establishes the core; never run item and core effects together.
- Existing ascension raises rarity and multiplies base stats by 25/18. There is no trustworthy per-item paid-ascension receipt. Preserve current values; do not infer payment from Legendary rarity, refund guessed costs, or multiply again.
- Proposed future rule: the legacy gear ascension remains an explicit gear investment with existing eligibility and cost; it cannot grant or improve a core. New cores are unranked. This future eligibility/UI rule requires review before implementation because today's ascension is tied to mechanic equipment.
- Migration uses a version marker plus durable per-item receipts, not only a global boolean. Load, retry, interrupted save, duplicated references and pending delivery must be idempotent. Failed saves restore the complete pre-migration snapshot. No automatic spending or replacing an occupied core.
- Selling/reforging gear never sells/rerolls its slot core. Selling a bound core is blocked until explicit detachment; detachment requires a safe inventory/pending destination. Duplicates are distinct inventory IDs; no automatic salvage. Capacity accounting includes the core destination before any mutation.

Acceptance matrix: old Epic/Legendary/trained gear, natural versus paid Legendary, full inventory/pending, both plans sharing equipment or cores, missing references, all classes and variants, active practice/draft, injected persistence failure at each boundary, repeat load/retry, sale/reforge/ascension. Verify preserved numeric stats and exactly one active mechanic, not merely successful load.

## Dungeon reward identity

Propose a 60/20/20 slot preference among nonmechanic gear: Hold favors armor, Timed favors weapons, Corridor favors relics. Keep existing drop counts, rarity weights, currency and fragment budgets. Ordinary mode keeps its cosmetic chest identity; Gauntlet keeps its three-boss identity. Publish actual probabilities and previews from the same policy, not an implied guaranteed item. Implement after agreeing how mechanic drops become cores.

## Elementalist ultimate

Current intermediate radii by rank are 5.5 / 6.325 / 7.425; final radii 6.5 / 7.475 / 8.775. Proposed intermediate base 7 and finale base 8.5, retaining rank multipliers 1 / 1.15 / 1.35: 7 / 8.05 / 9.45 and 8.5 / 9.775 / 11.475. This increases geometric area by about 62% / 71%; it needs balance review despite unchanged damage coefficients.

Keep damage, event counts, cast settlement and line-of-sight policy unchanged in the first change. Centralize the radius policy for actual hit queries, targeting, opportunity checks and telegraph/impact visuals; today's common 4.2 telegraph understates the final hit radius. Propose a clear expanding windup boundary, stronger single final impact, then a brief fading ground trace. Low-effects fallback retains truthful boundary and impact timing; never obscure enemy hazards. Test edge/blocked targets, each rank, once-only settlement and low-effects readability before Unity device acceptance.

## Less repetitive capture

Current corridor opening rotates Purify/Hunt/Escape across the first three rooms; explicit Seal side branches select Purify. Preserve dedicated Hold and opt-in Seal. Proposed main-route Purify completion uses an actual existing device interaction after its defenders are defeated; Escape opens after its real required enemies/device are settled, followed by physically reaching the exit. Remove only the post-clear standing timer, not combat requirements, rewards or the Seal branch. First audit chapter objectives, tutorial/mastery listeners, enemy counts, pathfinding, room retry and saved progression, then implement the main-route policy behind explicit objective rules. Generation failure remains a separate bug, never hidden by deleting the mode.

## Standable obstacles

First prototype only a few identified low platforms with authored top height and landing bounds. This requires shared support-surface rules, not turning collision off or raising a visual transform. Cover top/underside/lateral collision, edge step-off, jump-down, dodge/knockback, death/scene reset, target height, line of sight, camera, enemy/pet reachability and fallback paths. A bounded noncombat prototype can establish geometry first; no combat rollout until those systems agree. Do not make every obstacle standable.

## NPC clearings

Audit each actual NPC standing point and interaction approach against authored obstacles and navigable space. Move obstructed NPCs to visible clear ground; move decorative facilities behind them. Reserve both standing footprint and an approach corridor using shared geometry, then verify player/pet routes, interaction radius and camera visibility. Keep this a separate world-layout commit. Reference image transfer failed locally, so this proposal does not claim visual comparison with that image.

## Growth and chapter result follow-ups

Replace manual goals with a finite current-class chain based on real progression and stable goal IDs. Rewards are one-time automatic receipts; never auto-equip or spend. Define shared/class-local ownership, migration, historical eligibility, lost item and class-switch handling before coding. Preserve first-clear/chapter/ascension receipts and prevent A/B or retry duplicate grants.

Chapter results should lead with completion, reward icons/counts and next progression cards; story and detailed statistics may collapse. Distinguish completed difficulty, newly unlocked difficulty and next node. Carried potion count is not a reward. Image-specific visual acceptance remains pending successful authorized materialization or device evidence.

## Clickable current goal — new navigation requirement

The current goal card/text becomes the same actionable entry on desktop and mobile. Do not reopen the manual goal picker. Introduce a pure next-step result shared by automatic goal text, availability and navigation: stable goal ID, character/class identity, destination kind, skill/item/core ID or node+difficulty, required NPC, availability reason, and explicit return target. Resolve again at activation; stale snapshots never select another character's item or spend resources.

Existing FollowCampRouteStep mixes navigation with SetSpecialization/SetSummonerRoute mutations and TrackCore opens the manual list. Do not reuse it wholesale. Reuse OpenRouteSkill for exact skill/detail/scroll positioning, but route equipment/core/ascension to the actual matching item and operation pane. Dungeon actions only preselect the proper node/difficulty and show the entry confirmation; they never enter automatically. NPC-only actions show a location/route hint and required condition, not teleportation. Battle-restricted camp actions display the concrete return-to-camp requirement.

Desktop needs a visible hover/focus state and keyboard activation; mobile needs at least the existing 44-logical-unit control target and release-based activation that yields to scrolling. Capture a return location/scroll anchor; Back and panel Close restore or invalidate it correctly. Reconcile missing/sold items, completed goals, no eligible next step, class/character change, inventory/pending location and scene changes. A target click is navigation only: zero currency, reward, equipment or specialization mutations.

Tests should execute the real resolver and destinations for each goal kind, stale identity and unavailable state, then verify actual mouse/focus and touch/scroll ownership, exact selected skill/item/node/difficulty, no automatic entry/spending, and repeated Back/Close. The new reference image libfile_f0168546f7b481918b2258257380aefe had one official consumer-local materialization attempt, which failed; it was not visually inspected.

## Separate package: equipment and fashion interaction

Unify the interaction sequence: slot icon → item image card → actual isolated single-model preview → explicit Equip. Use the same current/candidate/unowned labels, selection, return and cancel semantics on desktop and narrow mobile list/detail views. Retain useful stat deltas, mechanic benefit/tradeoffs and fashion override rules; image-led presentation must not hide them. Equipment changes attributes; fashion overrides presentation; the new slot core controls mechanics. Explain those three layers concisely rather than implying identical ownership/equip rules.

Reuse the fixed comparison camera and isolated preview state: trying a candidate does not write a save or affect the live character. Equip revalidates ownership, slot/class eligibility and current state in the existing transaction, including repeated clicks. Test Back/Cancel, already equipped, unavailable items, filters, pending/inventory capacity, view switches, class switch and preview cleanup. Deliver as its own UI commit after current bug/input work, then integrate with the reviewed core-slot model.

## Separate package: high-tier wings

Create a distinct high-tier silhouette with visibly larger effective span, multiple structural layers, readable material/craft detail and restrained motion; do not merely scale or brighten low-tier assets. First present low/mid/high gray silhouettes from the fixed comparison camera and normal gameplay camera. Use the existing Blender asset workflow if appropriate, with source asset and import settings retained.

Validate all four classes with high-tier shoulders/back weapons/cloaks, avoiding obvious interpenetration and excessive screen coverage. Keep enemy hazard/target silhouettes readable while moving, attacking and turning. Preserve collision and combat range exactly. Compare normal/low effect settings and record geometry/material/animation cost; a low-effects fallback must preserve the premium silhouette. Native rendering/performance acceptance is required before claiming completion. No downloadable build or routine video-upload batch is requested.

## Entry naming, pending inventory and completed-goal destination

Parent supplied the actual screenshot text: the top-left HUD says “保存第二套配装·已完成” / “两份方案已保存”. This completed receipt must navigate to build-plan management with the second plan selected, without saving again or opening a map. The resolver must retain a completed step's inspection destination separately from the automatic chain's next actionable step.

When core migration is implemented, replace “机制图鉴” with “机制核心” and expose Exchange / Attach within it. Until then use wording that honestly describes the existing standalone equipment exchange; do not advertise a core attachment model that does not exist. Replace the ambiguous pending entry with “待领取物品”, real item icons and count, a concise reason for pending delivery, and View / Claim actions. Pending existence controls the indicator; capacity/eligibility controls Claim and its reason. Full inventory must not hide pending items.

Badge bounds belong inside the visible parent/header/scroll and safe-area rectangle, including selected and unselected states. Reserve measured width for multi-digit counts rather than negative offsets outside a clipped parent. Cover phone/tablet/desktop, narrow screens, increased font scale, 0/1/9/10/99/100+ counts, scrolling and repeated selection. Deliver clipping/pending visibility as a small UI fix; core naming and Exchange/Attach semantics ship with the actual migration.
