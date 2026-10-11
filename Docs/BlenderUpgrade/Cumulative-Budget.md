# Cumulative runtime-source budget and GUID closure

Runtime resource hashes and GUIDs are verified at the final combined source freeze: Windows `bc6a572a86fe4376f6f346b0fe52f20d9678aec3`, iOS `30f748b1bf6da7def9d91118aa756cd225c03e79`. It contains the finite F1–F6 roster and separately authorized planning changes, based on parent-reviewed main Windows `4214b885b5346b4e4987e9d50a3abf27f329db5c` / iOS `c1a49acd52cac28d1408a6d9bb25af971f7e3343`.

The cumulative comparison requested here is against the **original** main snapshots Windows `25096ba7dbf6e9d7ba9463ec29104c2c462da426` and iOS `1cd7ce36c77064757899788e23520fb796266888`. This is a source inventory/GUID audit. The exact final combined source passed **233/233 full checks** and three explicit platform API compilations; see `Docs/Validation/Final-Combined-Frozen`. Parent review/merge and engine/device acceptance remain separate.

## Source bytes and unique geometry

[ResourceBudget.json](ResourceBudget.json) now enumerates **every tracked non-meta Assets/Resources file**, its current SHA-256, GUID, original baseline bytes and status. It replaces the earlier partial snapshot, including recomputed TacticalAttachments hashes. Files are read from the recorded Git objects, not copied from old budget hashes.

- Original Windows Resources input: **2,354,995 bytes**; integrated input: **3,449,531 bytes**.
- **112 added files / 1,094,536 added bytes**, zero modified or deleted pre-existing resource inputs.
- **111 mesh resources / 11,866 unique source triangles**: 108 binary mesh files plus 3 FBX files. The remaining file is 46,348 bytes of Vanguard action samples, not triangles.
- New standalone material assets **0**; new texture files/bytes **0 / 0**. Original pilot atlas/metallic textures remain unchanged (731,849 source bytes combined). This does not mean every runtime effect allocates zero material instances.
- No source resource is new in F6; its body-envelope correction reuses the existing 280-triangle ProtectionCage.

| Resource group | Added files | Source bytes | Unique source triangles | Largest source mesh triangles |
|---|---:|---:|---:|---:|
| ActorModules | 24 | 267,480 | 2474 | 188 |
| ActorSilhouettes | 12 | 112,680 | 1042 | 172 |
| BlenderProjectiles | 6 | 37,224 | 344 | 116 |
| BlenderScenery | 3 | 70,228 | 1104 | 564 |
| BlenderSkillIdentities | 4 | 25,792 | 876 | 372 |
| BlenderSpellBases | 11 | 34,004 | 1100 | 204 |
| BlenderVfx | 4 | 11,624 | 402 | 196 |
| EnemySilhouettes | 9 | 89,964 | 832 | 228 |
| FixedScenery | 27 | 322,596 | 2984 | 384 |
| TacticalAttachments | 3 | 25,092 | 232 | 112 |
| VanguardActions | 1 | 46,348 | 0 | 0 |
| WeaponModules | 8 | 51,504 | 476 | 140 |

The three FBX counts are Pot480/Rubble60/OpenCanopyTree564, taken from `ArtSource/BlenderScenery/budget.json` only after its FBX SHA-256 and size match the audited files. Their roundtrip source verification is `validate-exports.log`; this is not a Unity-imported vertex count. EFM1/EFS4 binary counts are read directly from each resource header, verifying `12 + vertices*32 + indexCount*4 == byteLength` and triangular index divisibility. `VanguardActions` is action data and is explicitly excluded from mesh totals.

**11,866 is a library sum, not a per-frame/per-scene triangle count.** Cached meshes are instantiated repeatedly, some resources are optional/fallback alternatives, retained procedural geometry remains, and cover clipping may generate additional triangles. It is not valid to add the old entire procedural world to this library figure and call that a rendered scene budget.

## Materials, instances and low-tier limits

| Path | Increment / active contract | Evidence |
|---|---|---|
| ActorModules, F1, F2, F3 | Reuse existing renderers and palette; zero newly allocated material objects in these adapters. Same rigid meshes at low tier. | Respective `ArtSource/*/README.md`, production and factory budgets |
| F4 fixed modules | One existing palette slot per module, zero loader material allocation. Existing complete-town palettes have 23/24 distinct material instances; these are total factory counts, not new material costs. | `ArtSource/FixedScenery/README.md`, `factory-snapshots.json.gz` |
| Earlier scenery FBX | Pot2/Rubble1/Tree2 imported material slots, replaced through five named cached WorldResources palette keys (Clay/Ochre/Stone/Bark/Leaf). Actual extra material objects depend on already-cached keys; do not claim five new objects per prop. | `BlenderSceneryArt`, `WorldResources.Material`, hash-matched FBX manifest |
| Accepted opening skill meshes | One separate lazily cached Material using the existing FilledSpell shader; 12 desktop / 8 mobile / 5 reduced parts per cast. Whirlwind up to1,022 source triangles; ground shock up to68. | `BlenderSkillVfx.TryPlay` and `ArtSource/BlenderVfx/README.md` |
| Shared FilledSkillVfx bases / identities / F6 | Reuse existing one shared effect material and property blocks; existing14/10/7 part caps. Persistent Protection one source mesh on all tiers; body scaling adds zero triangles. Dynamic clipping remains separately bounded. | `FilledSkillVfx`, `DefenseIdentity/production.log` |
| Tactical attachments | Three lazily cached shared opaque materials; up to three identity renderers per live enemy, one per active role,232 unique source triangles. No added lights/transparency/textures. | `TacticalAttachmentArt`, `ArtSource/TacticalAttachments/README.md` |
| F5 ordinary arrows/bolts | Existing body+trail2 renderers/2 material objects, no increment; Arrow48/Caster24/Contract64/Hostile24 source triangles. Meteor116, existing1 renderer/material. Low-tier trail duration unchanged. | `ArtSource/BlenderProjectiles/README.md` |
| Optional first trap preparation | **+1 renderer/+1 owned material per live trap ornament**,68 source triangles, max128 after clipping. Uses shared lease pool, caps32 desktop/20 mobile/12 reduced across effects, not a separate32-trap allowance. Retired at existing preparation end, higher-priority eviction, area teardown. | `AuthoredTrapVisual`, F5 production logs |

These contracts are not a cumulative draw-call measurement: material reuse, shadow passes, shader variants, batching, overdraw, original live particles and simultaneous effects require Unity/device profiling. No reliable global material-instance total is asserted for an arbitrary generated scene.

Representative **instantiated** counts remain separately recorded: F3 16 weapon sets116–718 triangles including held arrows/weapon fashion (not back wings); F2 Goblin8,812/Guardian9,504/boss9,600 complete visible triangles, retained Slime2,768/Wisp2,392/astrolabe9,532; F4 full quarry/observatory authored instantiated triangles5,620/2,928 amid retained geometry. These are branch factory snapshots, not post-integration device counts. Their per-case manifests are authoritative; the final combined regression passed, without measuring device geometry or performance.

## GUID and platform audit

GuidPreservationAudit.json compares **all tracked Assets meta GUIDs**, including folder metas, independently against each platform's original baseline:

| Platform snapshot | Original GUIDs | Preserved | Changed / missing | New GUID paths | Duplicate GUIDs |
|---|---:|---:|---:|---:|---:|
| Windows bc6a572 |306|306|0 / 0|142|0|
| iOS 30f748b |309|309|0 / 0|142|0|

The142 new GUID paths (140 art paths plus two planning-source paths) include source scripts, resource files and folders, and must not be mistaken for142 runtime meshes. All112 new resource inputs and their metas are identical between the recorded Windows/iOS snapshots. The267-path complete Resources comparison differs only in the five existing Fonts paths; the manifest records the original baseline's same font-path differences. Platform-specific font contents are not overwritten for art parity. No Android repository, package or remote operation is part of this audit.

Audit method: `git ls-tree -r -l <recorded-ref> Assets` lists tracked blobs and sizes; `git cat-file blob <object-id>` supplies exact bytes for SHA-256 and meta GUID parsing. Baseline paths must still exist with the same GUID; all current GUID values are grouped to detect duplicates. This is stronger than checking only meshes touched by the latest batch. It does not claim UUID behavior for untracked files or future imports.

## Source / build / memory boundary

`.blend`, OBJ interchange, PNG review images, geometry exports and logs under ArtSource are editable/review provenance outside Assets, excluded from this runtime-source sum. Meta files are audited separately, not counted as payload bytes. Unity imports/copies resources and can add compression, platform shader variants, mesh/texture import data, serialization and package overhead; loaded TextAsset/decoded arrays/Unity Mesh/GPU buffers can coexist. Therefore **source byte delta is neither EXE/IPA/APK delta nor resident CPU/GPU memory**. No build package, imported mesh memory, Unity render capture, FPS or device acceptance is reported. No new textures does not mean the inherited project's textures consume zero memory.

Finite implementation outcomes are closed in Remaining-Art-Checklist.md; all40 skill routing decisions are in SkillCoverage.md. Remaining work is bounded final integration/PR/platform review and explicit engine/device acceptance, not another open-ended asset expansion.
