# 技能表现优化 · 2026-10-11

本轮同步 iOS / Windows，在已有角色、怪物技能表现上增加持续过程、立体主体、武器联动和受击回稳。未提交或推送。

## 表现与节奏

- 龙卷风：约 4.3 米高的扰动半透明风壁，旋转能量带、上升粒子；位置跟随实际攻击区域和人物移动。
- 黑洞：吸收核心、倾斜反向旋转的吸积环和向心粒子，沿真实攻击时间持续吸附。满级攻击跨度约 6.9 秒，人物引导姿势约 7.25 秒。
- 天灾终章：每轮实体陨石从约 12 米高度落下，配合已有元素爆发与终结；满级攻击跨度约 5.15 秒。
- 万箭归星：箭矢从真实弓锚点发射，经过约 9 米高弧线落到下一次真实攻击落点；满级攻击跨度约 4.8 秒。
- 剑阵和地裂：沿原有真实事件连续展开；满级剑阵攻击跨度约 3.51 秒。
- 火、冰、毒、电与召唤区域补充不同高度的实体、带细节的表面、轨道粒子及有上限的点光源。
- 雷霆锁链：命中后约 2.2 秒的体表分叉电弧，随怪物移动；没有新增伤害或控制。
- 施法动作：蓄力武器聚能、释放、持续引导、每轮释放与回稳。普通攻击在早期恢复阶段优先显示，随后恢复持续引导。旧施法 ID 不影响新的施法姿势。
- 怪物受击：保留原有导航约束的真实击退，模型增加有阻尼的位移和躯干、头部、手臂后仰回稳。没有改变 Boss 控制免疫策略。
- 快速命中效果保持快速释放，尾迹、碎片和冲击层延长到能看清的区间；怪物实际释放补充元素碎屑。

角色姿势与特效不回调伤害。持续技能调整了攻击间隔，事件数和各阶系数总预算不变；分布时间变长，因此没有宣称每秒伤害不变。冷却和基础恢复逻辑保留。

## 已完成验证

- 最终稳定快照：iOS 运行时与编辑器 API 编译通过；Windows 运行时与编辑器 API 编译通过。两个 report 的 passed=true，sourceChangedDuringRun 为空。
- 100 项生产伤害预算断言通过，包含三阶终极技能总预算和延长后的事件跨度。
- Unity 6000.6.4f1 / Metal 实际摄像机回归：64 次主动释放、8 次防御被动触发、8 项属性被动不施放检查；普通与低特效模式，共 408 张时序画面。持续效果暂停、旧施法隔离、最多 3 盏共享灯光、每组最多 96 / 36 个粒子检查通过。
- 最终连续录制：龙卷风、黑洞、陨石、箭雨四项真实技能；元素师与游侠终极技能使用真实蓄力、取消、重新蓄力和释放流程，取消未消耗施法 ID 或能量预算。武器聚能添加后最终编译与该录制再次通过。
- 怪物回归：史莱姆近战、哥布林近战、幽魂法弹、守卫重击、Boss 重击/冲锋/扇形/扫击，共 16 个普通/低特效场景，96 张实际预警与释放画面，进程成功退出。
- 双仓库 git diff --check 通过；平台已有技能范围及其他并行修改保留。

## 本地画面

- `ArtSource/Review/SkillPerformance-20261011/AllClasses.png`：所有职业主动技能总览。
- `ArtSource/Review/SkillPerformance-20261011/actual-casts.txt`：所有技能实际施放记录。
- `ArtSource/Review/SkillPerformanceShowcase-20261011/SustainedSkills.gif`：四项持续技能的连续实际摄像机采样。
- `ArtSource/Review/SkillPerformanceShowcase-20261011/*-Charge-*.png`：真实蓄力姿势与武器聚能。
- `ArtSource/Review/EnemySkills-20261011/actual-casts.txt`：怪物实际攻击记录。
- `ArtSource/Review/SkillPerformance-20261011/{ios,win}-api-report.json`：稳定编译及预算报告。

画面来自独立 Unity 测试工程与隔离存档。全职业回归调用真实核心释放；连续录制补充了两项真实蓄力流程。录制以固定模拟步长采样，不代表设备帧率；尚未以真机拥挤战斗进行帧率和手感验收。

## iOS 模拟器

- 独立 Unity 工程模拟器导出成功：`/private/tmp/emberfall-chain-review-project/Builds/iOS/SkillPerformanceSimulator-20261011`。
- Xcode Release / iphonesimulator ARM64 构建成功，日志明确 `BUILD SUCCEEDED`：`/private/tmp/skill-performance-xcode.log`。
- `Emberfall.app` 已安装至 iPhone 16 Pro，UDID `36D0E0CE-37F5-42DC-B3BF-39DFAE115331`。
- `com.h644782259.emberfall.ios` 启动成功，PID 74171；`ArtSource/Review/SkillPerformance-20261011/SimulatorLaunch.png` 确认为实际标题页，保留原有元素师存档。
- 此步骤确认安装与启动，未将标题页当作模拟器内技能战斗验收。
