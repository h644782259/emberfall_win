# H04 — 身体状态挂点

基线 `fce5efc4361614aed3eca070ed2574b7d7204a8e`。读完 Tools/cloud-validation.md；工作树及祖先未找到额外 AGENTS.md/.instructions。此包不改变状态时长、控制许可、伤害、角色导航或击飞高度。

新增 CombatModel.StatusAnchors 按真实模型工厂的 body 或大型首领悬浮底盘返回挂点。史莱姆、魔灵用自身实体 body 的侧腹；哥布林/守卫用胸甲下侧；大型首领避开心核/发射器挂底盘侧下方。符记直接成为该移动身体的子对象：完整 Animate 的击飞/浮动/缩放以及 Knockdown 的后续 LateUpdate 都自动携带符记，不依赖执行顺序的世界高度补偿。符记按父级三轴世界长度保持小尺寸。旧模组隐藏换成大型首领时重新解析活动模型。

霜痕/易伤分别维持菱片/双斜纹；冻结晶体仍在敌人逻辑根节点的地脚，绝不挂身体飞起或倒伏。Boss不显示硬冻结晶体。保持Standard不透明材质和深度测试，不加穿墙显示或地面范围暗示。

消费仍由真实 VisualStateChanged 同步隐藏；同帧重施复用原对象、死亡清理。无新增模型网格/纹理/材质数量：最高6个复用立方体（72三角面）、2自有材质，不新增每帧分配。

## 验证

- `EnemyStatusAnchorProductionTests.py`：真实 Slime/Goblin/Wisp/Guardian/大型首领工厂、实际 Animate、Knockdown，加完整 EnemyStatusVisual，共37断言通过。移除真实身体父级、改回模型根高度两个编译负例都失败。
- `EnemyStatusVisualProductionTests.py`：原完整组件10项及真实EnemyStatusEffects通知6项、原3负例仍通过；仅补测试引擎/缺模型边界API。
- `api-compile.log`：Windows/iOS/Android三个宏的全runtime源对固定Unity2021 API编译通过。原始日志与构建脚本同目录。initial-fixture-api记录初次托管shadow enum边界缺Off，修正后无未决错误。

这些是生产源码执行与托管层级/TRS数学检查，不是Unity渲染、GPU遮挡、画面或设备验收。尚需Unity中确认各敌体型/相机角度的标识可读性及Windows/iOS设备画面。
