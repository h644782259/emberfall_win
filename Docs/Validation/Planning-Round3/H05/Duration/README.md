# H05 — 真实机会窗口总时长专项

命令：`python3 Tests/OpportunityDurationProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet`。

- 全部执行真实 `EnemyStatusEffects`、`ScheduledTickWindow`、`BurnFinaleReceipts`、`CombatOpportunityState`。
- 按源提取实际 `ElementalOpportunityWindow`、`NotifyPerfectDodge`、回旋刃 proc 完整分支和反击时钟衰减语句。常量 CounterWindow/PerfectDodgeEnergy 从实际 PlayerUpgradeRules 提取。
- 检查冻结/霜痕/毒/火授予与衰减、短刷新保留原分母、长刷新更新分母、消费与重新授予；boss 硬控拒绝后的霜痕时钟；两个目标剩余时间/总时长必须来自同一目标；死亡/超范围/异主灼烧排除；真实 1.8 秒回旋刃、2 秒普通闪避、3 秒变体闪避授予及衰减/再生。
- 四个已编译负控：短霜痕刷新无条件覆盖分母、短毒刷新无条件覆盖分母、多目标用 remaining 代替 duration、普通闪避遗漏 counterWindowDuration 赋值。全部在指定断言失败，原始输出完整保留于 `duration.log`。

只新增指定测试与本目录日志，未修改生产源码、既有测试夹具，未提交。首次临时测试编译漏带 CombatDamage 源依赖，原输出保留 `initial-fixture-compile.log`；补上真实依赖后正例和负控通过，没有弱化断言。

边界：Unity MonoBehaviour/伤害接收/控制授予/场景目标点/能量/VFX 为托管替身；boss 替身显式拒绝硬控，验证实际 Freeze 的软标记逻辑，不重新验证 boss ApplyControl 实现。多目标 query 执行实际圆形范围选择，但场景射线/目标点解析是边界。此专项不运行 Unity、GUI 绘制、物理或真实设备；未重复三平台编译。

依赖夹具清单（已告知父任务、未修改）：

1. `Tests/ReturningCounterFixture.cs` 的 PlayerController 需 `counterWindowDuration` 字段；实际 NotifyPerfectDodge / 回旋刃 proc 已引用它。
2. `Tests/StatusFeedbackProductionTests.py` 内嵌 PlayerController 同样需 `counterWindowDuration`，实际 OnBasicAttackHitTarget 已引用。
3. `Tests/ShatterAvailabilityTests.cs` 已观察到父任务补齐 FrostWindowDuration/BurnWindowDuration/PoisonWindowDuration/CounterOpportunityDuration。
4. `ReturningCounterDesktopFixture.cs` 仅工作台，无本次反击时长依赖，不需要新增。
