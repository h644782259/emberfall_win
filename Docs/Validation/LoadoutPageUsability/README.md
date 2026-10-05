# 配装方案页可用性调整

## 范围

Windows 基线 `639ca3026dd6d2ade3fad625ae7f92e22e23972a`；iOS 基线 `f0890218cf88ca33fad9160418ddc445faa9d66a`。
两仓分支均为 `codex/loadout-page-usability`，不合入 main。

- 方案 A / B 的记录、覆盖和应用移至页面前部；方案摘要和手动装备引用修复可展开。
- 默认收起营地试招，展开后保留五种场景、10/60 秒设置、结果表、固定基准和冻结摘要。
- 试招返回保持展开；关闭再打开方案页恢复折叠，结果与固定基准不清除。
- 统一方案卡、确认、反馈、桌面和手机入口的 A / B 名称。原存档槽仍为 0 / 1，没有迁移。
- 配点／试招的长按钮按文字高度换行，保持至少 48 逻辑单位触控高度。
- 完整方案的保存／应用事务、草稿状态、装备引用修复逻辑不改；不修改职业数据。

## 可复现验证

```bash
Tests/Run-CloudValidation.sh --dotnet /path/to/dotnet --download-references --compile-ios
python3 Tests/BuildPlanUISourceTests.py
python3 Tests/PracticeHudProductionTests.py /path/to/dotnet
python3 Tests/BuildPlanPageGeometryTests.py /path/to/dotnet
python3 Docs/Validation/LoadoutPageUsability/verify-evidence.py
python3 Docs/Validation/LoadoutPageUsability/navigation-replay.py /path/to/dotnet
```

[`navigation-replay.py`](navigation-replay.py) 抽取实际 Open / Request / Close / Reset / Reconcile 方法，在显式托管会话边界下执行 **49 项**断言与 **2 项已编译负向对照**：返回、取消四种确认、草稿取消、引用选择返回、试招返回和角色绑定重置。复核修复覆盖“展开 → 关闭 → 工坊免费重置快捷入口 → 取消”；外部入口恢复折叠，页面内部取消确认保留展开状态。桌面和手机入口源码均绑定到所回放的实际处理方法，未声称执行 Unity 点击事件。结果与固定基准身份不变。负向对照分别撤回外部入口重置与外层关闭重置，必须在相应行为断言失败而非编译失败。

[`BuildPlanPageGeometryTests.py`](../../../Tests/BuildPlanPageGeometryTests.py) 使用实际完整方案页、A/B 卡、确认页、固定页脚、RefreshLayout、TouchRatio、TouchFont 和 OnGUI 矩阵，执行 **8,066 项**断言。覆盖桌面／手机、240×135 至 2560×1440 的七种物理视口、六种 DPI、折叠／方案详情／试招展开三种状态；检查四个记录／应用主操作、滚动内容边界、测量与绘制高度一致、固定页脚与滚动裁剪区分离。桌面按钮策略固定为 15 逻辑字号；手机按实际 TouchFont 缩放，包含 15／24／32。字体测量、GUI 与滚动裁剪仍为托管替身；极小视口只证明边界关系，不证明物理可读性或触摸尺寸合格。

扩展后的 `PracticeHudProductionTests` 执行实际试招绘制方法和生产按钮高度计算：1,852 项 HUD、冻结结果、重复展开／收起、240/360/568 宽度及 15/24/32 字号边界断言，另含六项已编译负向对照。字体测量和 GUI 是托管替身。

`BuildPlanUISourceTests`：25 项源码契约，检查入口顺序、名称、确认取消、服务调用及所有权边界。这是源码检查，不是引擎交互验收。

两仓各执行完整 **278 项**检查：Windows 276 项、iOS 275 项直接通过。两个旧 UI 测试夹具缺少新增的辅助方法／按钮绘制边界，另有 iOS 奖励回归硬编码 Windows 历史提交的问题。

修复后，两仓均定向复跑通过：草稿 461 项、完整方案 136 项、引用替换 45 项及全部原负向对照；奖励回归和其历史负向对照亦通过。历史方法分别来自 Windows `5d85e47b9fab2489d5b06963a0b896ec19112740`、iOS `e8b068cd29721db92fdc5f7b77166c2c9652019e`，逐字一致且以固定 SHA-256 校验，没有改奖励生产代码。

原全量报告保留 `passed=false`／退出码 1（包括运行期间三份测试文件变更提示），没有改写首次失败。RewardRevisionTests.py 是完成后修改的第四份夹具；初始报告与修复基线的输入对比恰好只有这四份测试文件，生产源码未变。

复核期间另一次完整检查因定向运行发现引用替换夹具缺少两个状态字段而主动中止；退出码 130，没有声明其完成或通过。[中止状态与原始日志](Evidence/interrupted-review/status.json) 保留原源码清单和定向编译错误；补齐字段后重新冻结并全量重跑。

Windows 另一次冻结全量执行 279 项，其中唯一失败是寻路分配量基准的精确字节差倍数；输入未变化，轨迹哈希一致。固定 `DOTNET_TieredCompilation=0` 的隔离复跑通过，随后用相同设置重跑完整聚合。该次原始退出码 1、报告及全部日志保存在 [review-aggregate/windows](Evidence/review-aggregate/windows/report.json)，没有改写为通过；这不涉及寻路或 UI 生产代码修改。

### 仓库内原始证据

- 首次全量：[Windows 报告](Evidence/initial/windows/report.json)、[iOS 报告](Evidence/initial/ios/report.json)，同目录包含报告引用的全部日志和原始 aggregate.log。
- 修复基线输入：[Windows](Evidence/initial/windows/repaired-baseline-sources.json)、[iOS](Evidence/initial/ios/repaired-baseline-sources.json)，记录可审查提交和逐文件哈希、四份夹具的差异集合。
- 合并执行草稿 **461 + 完整方案 136** 项的原始复跑日志：[Windows](Evidence/reruns/windows/draft.log)、[iOS](Evidence/reruns/ios/draft.log)。同目录 `preset.log` 是 45 项引用替换及负向对照，`reward.log` 是奖励回归及历史负向对照。136 项并非遗漏或单独重跑，实际包含在 CampBuildDraftProductionTests 的进程内。
- 本次冻结全量：[Windows 报告](Evidence/final/windows/report.json)、[iOS 报告](Evidence/final/ios/report.json)，同目录包括全部逐项日志、`frozen-sources.json` 和 `input-comparison.json`。输入对比仅含本次 BuildPlans 入口修复、两份整页几何测试、引用替换测试所需的折叠状态字段及聚合器注册。
- [导航回放日志](Evidence/navigation.log)；[全部证据哈希清单](Evidence/manifest.json)；[可执行校验器](verify-evidence.py)。校验器同时核实原始失败、复跑覆盖、冻结输入及当前检出源码；Windows/iOS 仅保留既有 IOSBuild.cs 与 MobileFeedbackFontSourceTests.py 两份平台差异。

冻结全量运行状态：正在执行。iOS 279 项全部通过；Windows 清洁复跑尚未完成，不声明全量通过。当前阶段可运行 `python Docs/Validation/LoadoutPageUsability/verify-evidence.py --allow-pending` 校验证据和源码；默认模式要求两个最终完整报告均存在。旧 [results.json](results.json) 保留首次验证与定向修复的历史说明；以最终原始报告为本次修复验证依据。没有借用远端 CI 结果。

## Unity 验收边界与待验项目

本环境没有 Unity 编辑器、Unity 6 平台构建组件、Xcode 或连接设备，不能运行 Unity Play Mode、Windows Player 或 iOS 真机。条件编译使用仓库固定的 Unity API 引用包，不能证明 Unity 6 实际渲染、平台打包、触摸事件或实机运行通过。未生成安装包、Xcode 工程、IPA 或上传视频。

需在具备环境后检查：

1. 桌面和最窄支持横屏／大字号打开方案页，确认 A / B 的记录与应用优先可达，按钮与固定页脚不重叠。
2. 空槽记录、覆盖已有方案、应用确认分别取消；角色、装备、点数、快捷栏及存档均不应变化。
3. 多次展开／收起试招；固定基准后再试招并返回，基准和结果表、冻结摘要仍存在。
4. 草稿进入试招再返回，草稿未应用；取消草稿回方案页；确认不能穿透到下面按钮。
5. 展开方案引用修复，预览后取消／返回；原引用不变。明确确认后仅所选方案部位发生变化。
6. 方案详情和试招均展开后关闭页面，从桌面／手机“免费重置配点”快捷入口打开并取消，应回到折叠的方案页；内部记录／应用确认取消则保留原展开状态。
7. 滚动、展开、确认取消与返回后，检查触控拖动终止、点击释放保护、实际字体裁切与横屏安全区。

推送后交独立复核；本变更不包含合并授权。
