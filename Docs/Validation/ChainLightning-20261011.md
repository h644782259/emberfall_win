# 雷霆锁链贴身电流 · 2026-10-11

雷霆锁链（元素师技能 4）的每个实际损血目标现在附带 1.6 秒蓝白电流余效。电流以纵向锯齿弧绕过身体，约每 55 毫秒改变形状，亮度持续脉冲，最后 0.4 秒淡出。它挂在怪物实际身体节点上，跟随移动、浮空和身体动画。普通模式三条蓝弧加白色细芯；低特效模式保留两条蓝弧。

这是表现余效：没有新增伤害、麻痹、僵直或持续伤害，也没有修改原连锁目标选择、等级伤害预算、控制时长和三阶辅助范围伤害。死亡目标不创建电流；未造成实际损血的目标不触发新效果。重复命中刷新同一目标的电流。使用既有 CombatVisualLease 共享预算，繁忙时优先保留命中等更高优先级反馈；不使用游戏随机数生成动画。

## 验证

- Unity 6000.6.4f1 / Metal，独立项目与独立存档，真实 CastSkill 调用。普通、低特效各一次雷霆锁链，覆盖守卫、史莱姆、幽魂。
- 真实相机采样命中后 0.1–1.8 秒；确认长于瞬时连锁闪电的身体电流。动图是实际相机画面的局部放大。
- 余效期生命值没有额外变化；到期消失、暂停时年龄不增长、重复命中只刷新不叠加、死亡和 CombatEpoch 变化时清理均通过。
- iOS 运行时 API 编译、编辑器编译通过；Windows 运行时 API 编译、编辑器编译通过。最终两份报告 passed=true、sourceChangedDuringRun=[]。
- 前序独立副本运行遇到 Unity 搜索索引异常及验证场地视线遮挡；补齐副本搜索设置并选取符合真实视线规则的场地后，最终运行退出码 0，无异常。没有更改游戏视线规则。
- iOS 独立副本导出成功；Xcode Release / iphonesimulator ARM64 构建成功。iPhone 16 Pro（iOS 26.5）安装返回 0，启动返回 PID 16250，截图确认标题／继续冒险页正常显示。没有在模拟器内人工施放技能；特效证据来自上述 Unity 实际施法录像。
- 共用运行时文件和专用编辑器验证工具已同步两端；保留既有其他界面、地图和测试修改。未提交或推送。

## 证据

- [真实画面动图](../../ArtSource/Review/ChainLightning-20261011/ChainLightning.gif)
- [未裁切相机截图](../../ArtSource/Review/ChainLightning-20261011/ActualCast.png)
- [施法及生命周期检查](../../ArtSource/Review/ChainLightning-20261011/actual-casts.txt)
- [模拟器启动截图](../../ArtSource/Review/ChainLightning-20261011/SimulatorStartup.png)
- [两端编译摘要](../../ArtSource/Review/ChainLightning-20261011/compile-summary.json)

编辑器菜单：Emberfall → 预览雷霆锁链持续电流。先保存当前场景并停止 Play Mode。

上述渲染验证没有测量设备帧率，也不是人工在模拟器内操作技能的验收。
