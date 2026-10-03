# H03 — 奖励卡片与真实收藏进度

基线：`fce5efc4361614aed3eca070ed2574b7d7204a8e`。独立分支 `codex/planning-round3-rewards`。

## 已接入

- 桌面/移动选择页共用 `GameUI.ChestChoices`：兵刃、双翼、金币堆前景徽记，分别暖铜/冷蓝/金色边框。使用现有宝箱背景与即时绘制，无新增纹理、模型、材质或持久缓存。
- 首屏明示兵装/羽翼 40% 时装、非必出，补给更多金币、无时装；卡片明示补给金币 ×1.5 与一次基础星纹。具体品质概率继续使用原规则页。
- 兵装/羽翼分别从 `profile.fashions` 的有效部位和有效品质去重计数，显示当前收藏 / 4；不读装备槽，不把重复、错误记录或第三箱 choice 当作收藏。
- 外观结果继续走真实 `DrawChestRewardModel` → 已有模型预览入口，使用回执部位/品质。新增当前部位收藏进度；金币结果强调回执保存的实际到账数。没有增量字段的旧回执继续标明旧金币奖励。
- 仅 `rulesRevision == 1 && choice == 2 && 无时装` 标注新补给加成；历史第三箱可仍为羽翼。未重掷、未修改 GameTypes/ProgressionService/SafeSaveFlow，原子开箱和跨槽 pending context 不变。
- 揭晓移动起点与新选择卡宝箱区域共用计算；移动说明和卡片一起滚动，保留 48 点按钮与原布局尺寸。

## 执行证据

使用 `/workspace/shared/emberfall-tools/dotnet/dotnet`；所有项目、保存与 GUI 边界替身在临时目录。

| 命令 / 原始日志 | 结果 |
| --- | --- |
| `python3 Tests/ChestChoicePresentationTests.py <dotnet>` / `chest-choice.log` | 37 真实回执/收藏断言；18 次生产卡片绘制（桌面/移动尺寸 × 三种缩放 × 三种箱）；实际模型入口按回执传参。三负控均拒绝：列表长度假收藏、旧金币误标补给、重复徽记轮廓 |
| `python3 Tests/RewardRevisionTests.py <dotnet>` / `reward-regression.log` | 1514 奖励断言、37 实际 SaveSlotTransition 失败开箱上下文/跨槽隔离检查；章节、首奖迁移、重复结算、失败/重试/重载等原回归及负控通过 |
| `python3 Tests/ChestCompositeProductionTests.py <dotnet>` / `chest-composite.log` | 97 生产绘制/纹理缓存采样，双 alpha 回归负控通过 |
| `python3 Docs/Validation/Planning-sweep/compile-platforms.py . Docs/Validation/Planning-Round3/H03/api` | Windows/iOS/Android 条件分支针对固定 Unity 2021.3.33 API 编译成功；全部源哈希见 `api/report.json` |

`initial-fixture-name.log` 保留第一次失败：测试错误假设载入旧回执后仍保留任意自定义 name；生产规范化会恢复对应规范名称。修正为断言实际载入回执 Name，仍严格检验真实羽翼部位、品质和无补给解释；后续通过日志单列。

父任务需把 `Tests/ChestChoicePresentationTests.py` 注册进全量 runner，参数为 dotnet 路径；本批未修改全量注册器。共享消费者仅桌面/移动奖励界面与 `ChestRevealPresentation`（旧 `Result` API 保留）。新增 partial 的 `.meta` 一并提交，跨平台同步应保留 GUID。

## 未验收边界

这是托管 .NET + GUI 绘制记录及 API 编译证据。模型测试执行实际选择/回执入口，以记录器替代 Unity renderer；不是引擎截图、Play Mode、真实 JsonUtility 或设备验证。环境启动限制见 `Tools/cloud-validation-environment.md`。没有生成或冒充 Unity 实录、截图、MP4。

仍需 Unity 检查三种卡片前景与字体、最小支持手机画面的说明/按钮滚动、揭晓起点连续性、真实外观材质/遮挡及金币强调在各分辨率的可读性。此次无模型/贴图包体增量，无奖励数值/概率/时序修改。失败抽奖无法持久化且进程终止时的既有保留边界不变。
