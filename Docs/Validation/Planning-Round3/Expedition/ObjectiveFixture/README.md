# 冻结 v2 两项房间 UI fixture 兼容修复

基线 `b853f84`，未改 runtime 或中央注册。原失败日志从只读 validation-v2/Tests/TestResults/Cloud-Latest 原样保留。

- mobile-room-objective：旧测试在第二房完成后直接调用 Next，忽略 false；新规则要求明确选择第三房。下一轮仍在第二房，对已死亡敌人再次 Defeat，在旧第59行失败。现在通过真实 OpenBranchChoice / SelectBranch 选择，并断言每次 Next 成功；两条分支各执行完整房间显示、首领与护卫、最终胜利断言。
- room-blessing-preview：旧第38行同样忽略两次 Next 的返回值；由于第二房未选路，旧第39行并未到休憩房。现在两条分支各覆盖原全部种子集合，明确选路并验证第三房身份和进入休憩房。原所有预览、只读重复查询、首领护卫和终局检查保持。

专项构建使用 Tools/cloud-validation.py 的 write_project 与注册表中原生产源码集合，dotnet=/workspace/shared/emberfall-tools/dotnet/dotnet，临时项目与 CLI home，离线 NuGet config。

结果：mobile-room-objective-fixed.log 171 断言；room-blessing-preview-fixed.log 27,504 断言。两份日志亦分别执行 `git show b853f84:Tests/<原fixture>.cs` 与完全相同现行生产源码，精确重现冻结同一异常，证明是旧 fixture 未履行新分支契约，未放宽 runtime 推进条件。

仍可用已登记的 `Tests/Run-CloudValidation.sh --dotnet /workspace/shared/emberfall-tools/dotnet/dotnet` 重跑。这里是文本/状态/布局的 managed 检查，不是 Unity 渲染或实机验收。未修改根工作树和冻结目录。

## v2 后续两项源契约兼容

- room-preview-ui-contract：旧字符串 `Boss ? 3 : 6` 同时约束了 Boss 与非 Boss 房间人数，守印侧廊新增4敌后已不匹配。改为精确检查 `EnemyCount = Interlude ? 0 : Boss ? 3 :`，继续约束休憩0敌/首领3敌但不假定非首领分支人数；增加 Boss 人数3→4的源负控。桌面/移动实际副标题接线与原两个旧文案负控保留且通过。
- room-failure-evidence：原26条生产失败快照断言、654条复盘断言及两个编译负控均已通过，只有末尾源码排序检查仍寻找旧 ChangeZone 保存守卫。更新为包含 `!retryingRoomChain` 的现行精确守卫，继续要求 preflight 在 Abandoned 快照及 epoch 改变前；专项全部通过，未删除原断言。

对应 `*-original.log` 是冻结v2原样失败；`*-fixed.log` 是本tree专项原始输出。仍未改运行时源码。

## v2 chapter-entry-production 编译边界

真实 ClosePanel 新增方案引用装备的出售确认框取消分支，章节入口 UI 独立 fixture 缺 `presetSaleOpen` 与 `CancelPresetSale`，导致编译失败，尚未进入原章节断言。仅给此章节测试 shell 增加 `presetSaleOpen=>false`，取消方法若意外被调用则抛异常，明确该 fixture 不打开出售弹窗；不复制出售逻辑、不吞掉错误路径，也未修改 runtime。

`chapter-entry-production-original.log` 保留冻结失败；`chapter-entry-production-fixed.log` 原344条章节 UI/真实进度持久化重放断言通过，6个旧 Back/首领遮罩/位置/滚动/保存失败阅读位置的编译负控均按预期失败。原有3条 fixture 未使用字段编译警告保留，0错误。命令：`python Tests/ChapterEntryProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet`。无Unity执行。

## v2 room-seal-hud-production 共享 fixture 回归证据

冻结原日志先完成1085条实际HUD绘制/矩形/状态断言，随后复用旧 RoomObjectivePresentationTests，在其第59行重复击杀已死敌人失败，原因与 mobile-room-objective 相同。无需新增源码修改：提交6b9f00e的共享 fixture 显式选择第三房即可解决。

命令 `python Tests/RoomSealHudProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet` 在本独立tree通过1085条HUD断言、171条房间显示断言、原扩大行距的实际Draw矩形编译负控。原失败和此次输出分别为 `room-seal-hud-production-original.log` 与 `room-seal-hud-production-fixed.log`。这是managed绘制记录器，不是Unity画面验收；本次提交仅归档证据。
