# Four actual reward hosts: exception/retry/presentation evidence

Tests added only by this agent:

- Tests/RewardPresentationExceptionProductionTests.py
- Tests/RewardPresentationExceptionCases.cs

The runner extracts complete actual TrySettleDungeonReward, TrySettleRoomReward, TrySettleChapterReward and TrySettleArenaReward methods, plus actual TotalEarnedExperience / new presentation helpers. It compiles the real ProgressionService and partials, GameTypes, ChapterResultSnapshot, ChapterEntryPresentation and ExpeditionModeState. Ordinary scene-completion flags for room/chapter are narrow shells; their combat simulation is not under test. Arena victory uses actual phase/spawn/defeat transitions. Chapter receipts and the full enemy-XP registration budget are actual service calls. No reward calculation or persistence transaction is copied into the fixture.

## Reproducible before

`before.log` freezes production inputs at 6aeef550c7bfb73bd8abc17eb61c2c7a8ca8cc3a: 42 independently completed cases, 480 assertions, 129 failures, successful compilation with zero errors/warnings. It was captured before production edits. Each case catches the injected exception and retries the actual host; one failure never prevents later cases from running.

`before-final.log` reruns that same frozen production revision against the final expanded test: 66 independently completed cases, 811 assertions, 191 failures. The historical-detail subcases are explicitly skipped for this old implementation because its detail API does not exist. Both old runs exit 1 for behavioral failures, not compilation failures.

Confirmed examples:

- Dungeon Changed failure commits 150 gold / 120 XP / 3 materials, but actual retry displays 0 / 0 / 0.
- Capped dungeon commits 1 gold / 0 XP / 1 material, but retry displays zero.
- Room and chapter retry lose the already committed deltas; chapter additionally loses first-completion, next-node/difficulty and first-core display facts.
- Capped chapter with no observer exception displays planned completion XP although actual saved XP gain is zero.
- Successful Changed subscribers can deduct and save currency after the reward commit. The previous after-minus-before host calculation attributes that side effect to the reward. The immutable receipt must instead describe the original atomic commit.
- A throwing observer prevented following observers from being notified.

## Final current result

`current-final.log`: **66 primary cases, 1,069 assertions, zero failures, exit 0**, successful compilation with zero errors/warnings. Thirty-six additional historical replay subcases are included in that assertion count (12 no-observer primary cases × known / absent / wrong-ID persisted details).

The matrix covers dungeon, room, arena, and all three chapter nodes; each has uncapped and gold/material/level-capped cases. Observer modes are no subscriber, Changed throws once, LeveledUp throws once, Changed saves a balance mutation then throws, Changed successfully saves a balance mutation without throwing, and an actual atomic write failure followed by retry. A fully capped character cannot level up, so that impossible event combination is omitted.

Expected reward increments come from an independently reloaded committed save, captured inside the first observer before its mutation/throw. Tests compare actual host display fields and actual reward log strings to those deltas, then verify duplicate calls neither write/grant nor announce again. Chapter first completion, unlocked node/difficulty, first-core eligibility and durable progress sequence are checked against the initial and committed states. Actual ChapterEntryPresentation.Result text is checked for saved XP/material amounts.

Historical replays use a new ProgressionService loaded from disk, preserve the original reward ID, and invoke the actual host again. Known details restore exact capped increments and chapter facts without new writes. Persisted absent or wrong-ID details show unknown, not fabricated +0 rewards/unlocks. Actual ChapterEntryPresentation.Result must contain the missing-detail/no-regrant explanation and omit false first-core or unlock claims. Common nonchapter unknown text uses the actual RewardPresentationText helper; the parent separately validates the full recap UI.

The write-failure cases require LastError to remain present and disk to stay byte-identical before successful retry. Intermediate logs are retained: current-initial (42 cases), current-expanded-fixed (54), before-expanded (66 before final text assertions), and current-expanded (fixture compile attempt: Save returns void; corrected without production edits).

Commands:

```sh
python3 Tests/RewardPresentationExceptionProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet --source-ref 6aeef550c7bfb73bd8abc17eb61c2c7a8ca8cc3a
python3 Tests/RewardPresentationExceptionProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet
```

No production changes, suite registration, commit, push, iOS synchronization or full-suite run were performed by this agent. This is managed production-host and actual file-persistence validation, not Unity execution or device rendering.
