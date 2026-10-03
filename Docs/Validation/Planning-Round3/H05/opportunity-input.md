# 外置机会提示输入边界

`MobileControls.OpportunityInput.cs` 仅查询实际 Player 的 `SkillOpportunityWindow` / `BasicOpportunityWindow`：在相应布局区域且 Window=true 时消费新手指。`ProcessPointer` 在 InputBlocked 拒绝之后、移动/普攻/世界分支之前分配 `Role.Consumed`；`IsScreenPointOverControls` 使用同一查询。不会扩大技能施放命中区，也不调用施法/修改瞄准。

窗口到期后，该位置恢复原世界交互；从可见窗口按下的手指即使到期仍持有 Consumed，松手不转为世界点击。窗口隐藏（阻塞）不拦截。窗口查询遵循现有 owner/epoch/死亡/可用性，缺能等灰窗口仍可见，故仍被消费。

测试：`python Tests/MobileOpportunityInputProductionTests.py /workspace/shared/emberfall-tools/dotnet/dotnet`。

- `opportunity-input-final.log`：828 实际 ProcessPointer / IsScreenPointOverControls 断言；12 布局位置 × 3 preset，可见/不可见/到期/拖离/瞄准/暂停/epoch/取消/原普攻按钮；删除消费分支、删除UI命中查询两种编译负控均在具体行为断言失败。
- `opportunity-input-pin-regression.log`：旧 MobilePinnedTargetProductionTests 的 56 项实际指针/瞄准/攻击/目标/蓄力断言与 6 个编译负控通过。
- `opportunity-input-first.log` 保留第一次执行缺真实 CastReceipts 编译依赖的失败；随后编译实际 Core/CastFirstHitReceipt.cs 和 PlayerController.CastReceipts.cs，提取实际 NewCastId。没有使用 NewCastId/HoldCastReceipt 假实现。
- `opportunity-input-second.log` 是增加全部槽位与原按钮断言前的中间通过记录。

窗口观察返回值、Unity 引擎、GameUI 触摸技能接口和目标选点输出记录为明确测试边界；新输入路由、区域查询、实际 layout、原游戏的指针/角色所有权逻辑执行生产源码。该 suite 验证“根据当前观察是否消费指针”；实际窗口生成/过期语义由 H05 其它生产测试覆盖。未运行 Unity 或真实触摸设备。

受影响原消费者：MobilePinnedTargetProductionTests.cs/.py。其实际 IsScreenPointOverControls 已替换旧局部替身，新增机会观察边界默认为空，实际新 partial 与 CombatOpportunityState 一起编译；旧UI源检查更新为统一 reason 文本。RestrictedHealingProductionTests.py 从该 runner 派生，在根集成 core 兼容提交时须保留 core 对旧假 NewCastId 的删除（否则与实际方法重复）。

根任务统一提交和登记新 runner；本子任务未提交、未修改 MobileControls.Feedback 或其它 H05 tests。
