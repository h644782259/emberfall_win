# Allocation measurement investigation

The original frozen controls run failed the exact 112-byte-multiple allocation assertion. A candidate rerun also failed: the four-companion legacy subprocess allocated 256 more bytes than expected, while exact target/direction bit traces and open-query zero allocation held. Two default-environment main replays passed, so this is **not** claimed to be a reproduced main failure.

All four input files (test runner, math fixture, actual companion code and traversal code) are byte-identical to main; see inputs.json. With DOTNET_TieredCompilation=0, both candidate and main pass and save exactly 4480 / 18480 bytes in the one/four-companion blocked scenarios. This supports a runtime measurement sensitivity rather than a changed gameplay algorithm, but does not erase the original full-run failure. No test threshold or production algorithm was changed. Managed allocation evidence does not measure Unity/native frame cost.

The separate room-branch fixture failure is reproduced on the exact main tree (missing EndHubNpcConversation); its log is retained here. It was not present in the corridor baseline table because the corridor branch repairs that fixture independently.
