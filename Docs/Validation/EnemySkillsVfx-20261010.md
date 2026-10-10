# 怪物技能特效优化 · 2026-10-10

覆盖普通怪物四种攻击、首领三种常规攻击及大型首领环流扫射，共八类攻击表现。

| 攻击 | 视觉调整 |
| --- | --- |
| 史莱姆近战 | 绿色黏液立体飞溅与竖向回落 |
| 哥布林近战 | 有明暗面的短促撞击碎片 |
| 魔灵能量弹 | 加厚并拉长敌方能量弹的可见子模型 |
| 守卫重砸 | 地面破裂实体及竖向碎岩 |
| 首领重砸 | 更大破裂基座，分散竖起的碎岩 |
| 首领冲锋 | 随攻击者移动的短促赤色能量体，暂停和失效时停止 |
| 首领扇形弹幕 | 与魔灵共用立体敌方弹体 |
| 首领环流扫射 | 预警期间不显示实体；进入真实伤害阶段后显示立体能量光柱，核心宽度位于原有危险胶囊内 |

地面碎片使用已有 authored 资源，并创建覆盖裁切后的独立网格。动画只改变垂直位置，保留已认证的 XZ 轮廓；独立裁切网格在销毁时释放，共享源网格保持借用。普通 / 重砸最多 5 / 8 个片段，低特效最多 3 个；冲锋普通 / 低特效为 4 / 2 个片段。效果沿用特效租约与战斗 epoch 生命周期。

## 验证

- Unity 6000.6.4f1 / Metal 真实控制器：普通和低特效模式各八组，共 16 组真实准备与释放；首领扫射实际进入 Beam 阶段。固定 60 Hz 游戏时间，96 张相机世界截图。使用隔离存档，不改玩家存档。
- 投射物管理代码测试：491 个资源、工厂、模拟快照、拖尾、归属及生命周期断言，2 个负向对照通过。
- 首领扫射裁切测试：231 个真实裁切、胶囊边界和安全区断言，旧中心射线负向对照通过。测试在平坦副本基准面运行；兼容近期 WorldTerrain 源码布局变化。
- 逐方法比较：两个平台的 CombatProjectile.Update、LargeExpeditionBoss.Tick / ClipBeam / BeamContains 与本轮前字节一致。EnemyController 本轮只增加一个冲锋视觉调用，移除该调用后与本轮前字节一致。
- Windows runtime / editor 的 Unity API 编译报告 passed=true，sourceChangedDuringRun=[]。iOS 各 API 编译变体均编译成功，报告检测到其他场景美术文件在检查期间变更，因此不把该聚合报告记为冻结源码通过；另行完成实际 Unity simulator 导出及 Xcode Release / iphonesimulator 构建（BUILD SUCCEEDED），安装到 iPhone 16 Pro / iOS 26.5 并启动成功（PID 61172）。导出目录 Builds/iOS/EnemySkillsSimulator，构建目录 Builds/iOS/EnemySkillsDerived。
- 编辑器 WorldArtVisualReview 的内部类型访问改用反射，以修复 Editor / runtime 程序集边界引起的编译错误，不扩大运行时代码可见性。

不把管理测试视为实际 GPU 验收；相机渲染检查不代表拥挤战斗 FPS、真机性能或完整人工操作验收。Windows 未运行实际播放器。

## 实际渲染预览

[八类攻击总览](../../ArtSource/Review/EnemySkills-20261010/AllMonsters.jpg)

[四组代表攻击动态预览](../../ArtSource/Review/EnemySkills-20261010/EnemySkills.gif)

原始帧及隔离存档留在本地并由目录内 .gitignore 排除。
