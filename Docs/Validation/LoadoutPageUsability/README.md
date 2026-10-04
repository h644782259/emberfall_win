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
python3 Docs/Validation/LoadoutPageUsability/navigation-replay.py /path/to/dotnet
```

`navigation-replay.py` 抽取实际 Open / Close / Reset / Reconcile 方法，在显式托管会话边界下执行：44 项返回、取消四种确认、草稿取消、引用选择返回、试招返回和角色切换断言。

扩展后的 `PracticeHudProductionTests` 执行实际试招绘制方法和生产按钮高度计算：1,852 项 HUD、冻结结果、重复展开／收起、240/360/568 宽度及 15/24/32 字号边界断言，另含六项已编译负向对照。字体测量和 GUI 是托管替身。

`BuildPlanUISourceTests`：25 项源码契约，检查入口顺序、名称、确认取消、服务调用及所有权边界。这是源码检查，不是引擎交互验收。

两仓各执行完整 **278 项**检查：Windows 276 项、iOS 275 项直接通过。两个旧 UI 测试夹具缺少新增的辅助方法／按钮绘制边界，另有 iOS 奖励回归硬编码 Windows 历史提交的问题。

修复后，两仓均定向复跑通过：草稿 461 项、完整方案 136 项、引用替换 45 项及全部原负向对照；奖励回归和其历史负向对照亦通过。历史方法分别来自 Windows `5d85e47b9fab2489d5b06963a0b896ec19112740`、iOS `e8b068cd29721db92fdc5f7b77166c2c9652019e`，逐字一致且以固定 SHA-256 校验，没有改奖励生产代码。

原全量报告保留 `passed=false`／退出码 1（包括运行期间三份测试文件变更提示），未将其改写为全绿；未再做一次无修改期间的完整重跑。逐文件 SHA-256 核实生产代码与其他测试均未变化，四份受影响夹具文件对应的测试均已复跑，无未解决检查失败。Windows 与 iOS 条件编译在两仓均通过，零警告、零错误。

详细覆盖和复跑证据见 `results.json`；保留原失败名、修复文件、复跑日志哈希、完整检查列表及源文件清单哈希。原始全量报告仍位于各仓 `Tests/TestResults/Cloud-Latest/report.json`。

## Unity 验收边界与待验项目

本环境没有 Unity 编辑器、Unity 6 平台构建组件、Xcode 或连接设备，不能运行 Unity Play Mode、Windows Player 或 iOS 真机。条件编译使用仓库固定的 Unity API 引用包，不能证明 Unity 6 实际渲染、平台打包、触摸事件或实机运行通过。未生成安装包、Xcode 工程、IPA 或上传视频。

需在具备环境后检查：

1. 桌面和最窄支持横屏／大字号打开方案页，确认 A / B 的记录与应用优先可达，按钮与固定页脚不重叠。
2. 空槽记录、覆盖已有方案、应用确认分别取消；角色、装备、点数、快捷栏及存档均不应变化。
3. 多次展开／收起试招；固定基准后再试招并返回，基准和结果表、冻结摘要仍存在。
4. 草稿进入试招再返回，草稿未应用；取消草稿回方案页；确认不能穿透到下面按钮。
5. 展开方案引用修复，预览后取消／返回；原引用不变。明确确认后仅所选方案部位发生变化。
6. 滚动、展开、确认取消与返回后，检查触控拖动终止、点击释放保护、实际字体裁切与横屏安全区。

推送后交独立复核；本变更不包含合并授权。
