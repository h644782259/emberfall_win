H01 practice target gait — managed production replay

The real GameSession.MovePracticeTarget traversal is observed before the separate
knockback movement. Each practice frame clears walkingDisplacement; only accepted
active displacement reaches the existing EnemyController.AnimateModel and real
CombatModel.SetLocomotion/LocomotionPoseState. Movement speed, traversal radius,
status gating, target coordinates, health and gameplay clocks are unchanged.

PracticeLocomotionProductionTests: 18 assertions cover moving, stationary and
supplier scenes, frozen/stunned/down/airborne interruption and release, slowed
movement and release, reversal, partial/zero accepted traversal, stale-frame
reset and knockback exclusion. Three compiled mutations fail intended assertions.
negative-calibration.log retains an initial inadequate mutation (knock velocity
was sampled after decay); the final negative moves sampling before decay and
correctly fails. Production baseline was passing in that initial run as well.

Regression logs retain original tests and their negative controls: camp lifecycle,
knockdown/death handoff, threat fairness, blocked combat and locomotion source audit.
Shared fixture consumers were inspected; no public API or new fixture dependency
was introduced. Runner: Tests/PracticeLocomotionProductionTests.py <dotnet path>.

These are managed engine boundaries, not Unity physics/rendering or device
acceptance. Same-camera Unity recording remains unavailable in this environment.
