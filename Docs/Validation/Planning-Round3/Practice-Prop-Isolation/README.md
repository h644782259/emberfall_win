Practice scenery isolation

DestructibleProp.CanAffect now rejects practice sessions. All existing practice
scenarios have no destructible objectives; none may consume the retained world's
props or projectile interception. Future practice destructibles require an explicit
practice-owned epoch scope before this boundary can be relaxed.

PracticePropIsolationProductionTests executes the real prop class (debris rendering
is replaced), actual DestructiblePropRules and PropImpactGeometry. Direct impact,
area, cone, line and swept projectile tests preserve the old prop's Broken state,
visual, obstacle handle count and recovery; restoring ordinary ownership proves
ordinary projectile contact, breaking and recovery remain active. 26 assertions.
The compiled removal of the guard fails the intended isolation assertion.

Managed obstacle/visual boundaries are not real Unity collider or rendering proof.
