Review fixes on planning package 2

Only the two UsesEnemyAI scenarios keep ordinary initialized health. The three
legacy passive training scenarios retain their original million-health sustained
measurement target and do not finish the record early when all targets are defeated.
Their 10/60 second limit and passive AI behavior remain explicit in the UI.

Practice death marks the record terminal but keeps temporary session ownership
until the next TickPractice. Actual Guardian ResolveAttack continues to execute
its same-stack companion loop against the temporary owner, preventing damage to
original companions after a lethal player hit.

PracticePressureCombatProductionTests executes actual ordinary AI/ResolveAttack,
projectile Update and practice death branch. A managed damage-recipient callback
signals death into the actual branch, and the real post-player-damage Slam loop
then resolves its companion snapshot. The synchronous EndPractice mutation fails
because it exposes the original companion to that remaining Slam damage.
Normal pressure HP, legacy million HP and full 10/60 record endpoints are asserted.
CampPracticeSessionProductionTests executes the actual deferred TickPractice
restoration after death, alongside all prior lifecycle and negative controls.

Tests in this directory were rerun with H02 uncommitted changes removed. These are
managed gameplay-method tests, not Unity input, rendering or device validation.
