# H06 — venom identity and truthful contact feedback

Base: H04 commit 0f0a20e43e6b81349257372928b8a2a45e77ddb9, following fce5efc4361614aed3eca070ed2574b7d7204a8e.

BuildCatalog owns the fan / single-shot modifier vocabulary and ranked skill text. Equipped relic eligibility and the unlocked B guard remain authoritative. Build draft, equipment badge, desktop and mobile skill descriptions retain the original skill name. B suppresses legacy fan-target-cap and generic range-growth lines. Rank damage remains the shared 2.4 / 3.6 / 4.8 coefficient; cooldown and energy are unchanged.

The existing authored projectile mesh is reused. Only its visual child narrows; the 0.14 collision radius, first entity/wall interception, bounded steering, cast identity, nonpiercing flag, and zero explosion budget are unchanged. The B trail width is 0.045 with 0.1 second duration (0.06 on reduced effects). A keeps its existing fan visuals. Original nonvariant Update hash remains guarded, with exactly the newly authorized contact call and B ring condition normalized by the test.

Physical B damage produces one small body-position nick. Three inward-shrinking seed marks occur only inside the actual successful ConsumePoison branch; an empty target and repeated consumption cannot announce success. No expanding splash or ground damage footprint is added. Ordinary poison consumption stays distinct from the projectile contact. VFX is paused with input, retired on owner death, epoch change, session end or owner replacement, and releases its owned material. Standard depth-tested material preserves wall occlusion; GPU behavior remains unverified.

## Budget and fallback

No new model or texture file is introduced; existing authored arrow resources and their editable source remain unchanged. The contact indicator uses existing procedural cube geometry: one cube for a 0.12 second physical nick, three for 0.32 second consumption, at most 36 triangles and one owned material per indication; zero textures. It uses the existing shared visual lease budget and reduced trail duration. This runtime status-feedback geometry does not warrant an offline Blender mesh. Gameplay remains valid if the visual lease is unavailable. Stable new script GUID is checked into its meta file.

## Evidence

Raw logs in this directory retain initial failures and successful retries. The initial draft assertion expected the old renamed skill prefix; it now explicitly requires the original skill identity plus modifier and retains all behavior negatives. Two runners initially attempted the read-only default dotnet home; retries set DOTNET_CLI_HOME to a writable scratch path. Neither needed access changes.

- Real cast/Friendly launch: 24 assertions, compiled piercing negative.
- Full VenomContactVisual: 13 assertions, compiled nick/success confusion and outward expansion negatives.
- Actual poison resolver/status: 10 assertions, old propagation-path negative.
- Real equipped draft summaries: shared UI/draft text, authoritative budgets, persistence, A/B and locked/unequipped/wrong-class checks; three compiled negatives.
- Existing concentrated collision/persistence, authored projectile, contact replay, blocked updates and H04 status-anchor regressions rerun.
- Mechanic badge: 223 eligibility / variant assertions.
- Complete Windows, iOS and Android source API compilation: zero warnings/errors.

These are managed production-path tests with Unity API boundaries and reference-assembly compilation. No Unity process, device, GPU, frame capture, screenshot or MP4 was produced or accepted. Visual scale, readability and actual wall rendering require Unity/device review. Root owns runner registration and cross-platform integration.
