# Frozen v2 failed run retained

Input Windows `b853f84ef3aedb7d7e0dbc9081767463c06b91bb`; equivalent iOS `5df99ad16cbd3eceb6c9c0f5b91eb5bda033593e`. Full suite ran 2026-10-03 16:11–16:29 UTC: **267 checks, 253 passed, 14 failed**. This is a failed run, not final acceptance. `cloud/report.json` lists every result and its raw log. `input-stability.json` confirms all 6,646 tracked inputs, 204 pinned reference files and SDK remained unchanged. The three explicit platform API branches compiled successfully, independently of the failed suite.

Failures identified old branch-selection fixture flows, extracted UI-host dependencies, receipt source-list omissions and stale source contracts. Individual repairs retain original failures and negative controls in adjacent package folders. A subsequent review identified a separate real practice terminal-action recording defect; that fix requires a new committed freeze and full rerun. Do not use this report as acceptance of that later source.

An earlier v1 manifest at source `92cce7d4b859545df2ffcff55d7e64ee79bab994` was abandoned before any full suite started to integrate parent review fixes. It is neither a passed nor failed suite.

All checks use managed engine boundaries and pinned API references. No Unity Editor/player, real physics, rendering, GPU, device build or recording was executed.
