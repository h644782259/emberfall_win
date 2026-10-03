# Frozen v2 chest-choice-presentation retry

修复基线：`d0378da2bfe6f7ebfd138e00c9e3b7eafe44dda5`，独立工作树 `rewards-fixture`。

冻结原日志原样复制为 `frozen-failure.log`，未修改 freeze 或 root 工作树。失败为临时 C# 测试项目没有纳入新 `MasteryCoreRuntime` 所依赖的 `CastFirstHitReceipt` 与 `CastFirstHitHistory`；这两个真实类型均定义在 `Assets/Scripts/Core/CastFirstHitReceipt.cs`。

最小修复：仅将 `CastFirstHitReceipt` 加入 `Tests/ChestChoicePresentationTests.py` 的生产 core 源依赖列表。不增加替身、不修改生产、不删除或弱化断言/负控。

复测命令：

```sh
python3 Tests/ChestChoicePresentationTests.py /workspace/shared/emberfall-tools/dotnet/dotnet
```

退出码 0。`retest.log` 保留完整结果：37 回执/收藏断言、18 次生产卡片绘制通过，3 个预期回归负控均命中。此为托管源码验证，不是 Unity 引擎或实机验证。
