# 当前 0.4.0 验收状态

本轮已执行独立生产逻辑、源代码接线与 Unity API 编译检查，未运行 Unity 编辑器、真实画面、Windows 安装包或 iPhone/iPad 真机。当前待测项目见本页下方的“0.4.0 expanded acceptance”与 本轮整合说明。

以下 0.3.1 表格是仓库原有的历史记录，本次未复验；其 PASS 不能用于证明当前代码已通过引擎/设备测试。历史手动清单中的三页快捷栏和单独“另存为”入口已经被当前十格技能栏、单一保存入口与加载确认替代，不应按旧入口验收现版。

## 历史记录：Unity 0.3.1 实机验收

面向 Unity 6（6000.6.3f1）与 Windows 独立游戏的手动验收清单。自动化检查以 `Tests/TestResults` 报告为准；列表本身不代表全部人工通过。

2026-10-01 的 0.3.1 自动化验证与构建记录：

| 阶段 | 结果 | 报告 |
| --- | --- | --- |
| 独立成长/存档逻辑 | PASS：2,864 断言、150 隔离场景 | `Tests/Run-ProgressionTests.ps1` 本次运行结果 |
| 独立技能运行逻辑 | PASS：937 断言 | `Tests/Run-SkillRuntimeTests.ps1` 本次运行结果 |
| Unity JsonUtility 成长/存档 | PASS：2,006 断言，正式构建时执行 | `Tests/TestResults/unity-progression-3c181b1fccc0427ea183ad110aafdfa3/validation-report.json` |
| Unity Play Mode | PASS：1,546 断言、10 张世界截图、0 控制台/运行时错误 | `Tests/TestResults/PlayMode-217063f3608f447ba49511c31649e329/runtime-validation.json` |
| 安装器回归 | PASS：27 项检查 | `Tests/TestResults/installer-4341d0d4c90d409898f067745d7e237a/installer-test-report.json` |
| 独立 Player 完整帧 | PASS：441 检查、84 张整帧截图、0 控制台错误 | `Tests/TestResults/Visual-6a662641b9b3458db547d4f0e780a6a4/visual-validation-report.json` |
| Windows 正式构建与发行包 | 构建及打包成功，发行负载153个文件，已生成安装器、免安装ZIP和SHA256校验文件 | `Logs/windows-build.log`、`Builds/Release` |

Play Mode 的 10 张 `Camera.Render` 图片不包含 IMGUI；独立 Player 的84张完整帧截图从实际帧缓冲读取，包含界面。本机正式安装已验证：153个文件与发行包SHA256一致，原有2个存档文件哈希不变，启动日志确认为0.3.1。该结果不代表另一台电脑安装、物理扬声器试听或iPhone真机验收。触屏截图目前是桌面 Player 预览。iPhone 源码与导出工具只在独立的 `emberfall_ios` 仓库维护，应按该仓库的 [Mac/Xcode 安装说明](https://github.com/h644782259/emberfall_ios/blob/main/Docs/iOS.md) 使用自己的 Personal Team/开发团队签名后直接运行到设备；目前未生成 Xcode/IPA，也未记录 iPhone 真机通过。

以下保留为逐项手动验收清单：

1. **导入与启动**：Unity Hub 导入项目，Console 无红色错误；指南打开；点击开始试玩；Game 窗口可看到职业菜单。分别检查 1280×720、1600×900 和更宽的窗口，文字、按钮及提示没有遮挡。
2. **四个职业**：分别创建剑卫、元素师、游侠、唤灵师，每次应创建独立存档并保留已有角色；能移动、鼠标瞄准、持续普攻、滚轮缩放、右键拖动镜头。武器、普攻和技能特色可辨，唤灵师使用法器。保存并从列表重开每一种职业，身份、属性和技能不变。
3. **战斗**：原野普通怪靠近时不主动攻击，受击/控制后反击，远离后脱战；精英与首领主动追击。鼠标点怪身体及移动中普攻能正确选中并面向目标，元素师法弹追踪当次目标，游侠只短暂修正。Shift 瞬移有短暂无敌；药水生命已满时不消耗；打开 I/K 或 Esc 暂停时敌人不继续攻击。
4. **技能学习与进阶**：每职业恰有 8 主动、2 被动；分支树由上至下从 2 至 30 级开放，同级有双节点。新学高级技能必须先点亮连线前置；旧档已学技能保留。强化/觉醒分别需基础等级+8/+18；消耗技能点，原技能 ID 与快捷栏位置保持。检查数值、范围、特效、追加机制与预览一致；治疗不造成伤害，防御/增益不是纯输出，被动自动触发而不能主动施放。
5. **装备**：背包左侧穿戴、中间闲置、右侧详情比较。检查全部/武器/护甲/饰品分类及综合属性/等级/稀有度降序，默认最强在前；同分顺序稳定。逐行出售准确加金币且列表选择不跳错，已装备物品不能出售。未满足等级的物品不能装备；强化扣金币并提高数值；金币不足、+10 上限有说明；反复交换高/低生命护甲不能回血。
6. **副本**：2 级后从北方传送门按 T 进入；依次出现三波，波间回复生命；最终 Boss 有重击、冲锋、扇形弹幕；通关金币/经验和装备正确；T 返回后可再次挑战更高阶。
7. **状态边界**：施放持续技能后跨区或退回标题，旧技能不能伤害新场景；在波间离开副本，不应出现延迟生成的敌人；死亡不扣金币，能复活回营地，已赚金币、经验、装备和技能保留；失败不发未完成奖励。章节失败可原条件重试（种子/编成/难度/阶数/战术/限疗不变），从节点起点重置；返回营地重新探索可换编成。
8. **存档**：继续冒险打开按日期排序的角色列表；加载后恢复职业、等级、金币、技能、背包装备，进度回到营地。新建角色不覆盖旧档，自动保存只写当前档；暂停菜单另存为创建独立副本并选中新档，后续修改不影响源档。用隔离测试档检查坏档仍显示、可用备份能恢复并提示、完全坏档不可读取且不改变当前角色；旧版固定文件名仍可加载。
9. **音效与窗口**：标题/冒险中可听到轻背景乐；界面点击有短音。挥空声与命中撞击可辨，技能、拾取、升级、胜利和死亡有提示音；静音同时停止背景与效果，取消静音马上恢复背景并发出界面反馈；切到其他窗口后自动暂停。分别记录真实扬声器/耳机试听结果，不能用PCM或DSP断言替代实际听感。
10. **Windows 构建**：使用 Emberfall 菜单构建，检查 Console 和 Build 日志；运行 `Builds/Windows/Emberfall.exe`；场景材质不发紫、字体正常、输入/菜单/保存均可用，再完整通关一次。
11. **快捷栏与资源**：检查默认上排 12345、下排 ZXCVB，三页切换、自定义按键、冲突键交换与保留键拒绝。相同技能换页/换键/重新配置后冷却继续；能量不足不能施放，普攻命中回能，空挥不回能；被动不占主动格。
12. **换电脑与安装**：在第二个 Windows 用户/电脑安装发行包；没有存档能初始化。退出后复制全部 `emberfall-save*.json` 及其对应 `.bak` 到新存档目录，继续游戏应列出多个角色并恢复各自装备、进阶、页面和快捷键；程序安装目录改变不影响存档读取。不同编号可并存，同名文件须先备份再决定是否覆盖。
13. **技能释放方式**：自身范围、治疗、护盾、方向攻击、位移和锁定技能按技能键或点图标开始施放，无需额外选点；部分技能进入蓄力，其余直接释放，同帧不附带普攻或重复施法。需要落点的地面技能显示预览，左键确认后开始施放或蓄力；右键/Esc 取消预览不消耗。打开菜单、死亡和跨区会取消预览。
14. **状态与动作**：裂地击倒、冰霜冻结及减速、链雷眩晕、旋涡聚怪、毒蔓最多三层持续中毒、幻影连射锁定及易伤各有独立机制；首领有控制抗性。剑卫挥剑、元素师举杖、游侠拉弓，以及移动转身均可见连续动作；高阶技能有下落武器、晶片和冲击波。
15. **简洁界面**：战斗页与暂停页不显示大段快捷键说明；未学习技能只呈现为空槽，学习后显示图标/按键/冷却，悬停可查说明及释放方式。右下角问号和暂停页均能打开独立操作指南；键盘图中的十键同步自定义设置。从指南进入改键后，关闭返回指南。
16. **装备提升与属性来源**：背包比同部位当前穿戴评分更高的物品显示绿色“↑ 提升”，评分相等不显示，等级不足应说明；强化或换装后立即刷新。暴击率悬停显示职业基础和被动来源，不暗示装备已提供暴击属性。
17. **部位强化与自动继承**：武器、护甲、饰品分别保存强化等级，换装自动按新装备自身基础属性套用，不出现手动继承面板。候选预览、评分和装备后的实际属性必须一致；反复换装不能叠加属性或回血。选中未穿戴装备强化时，提高其部位及当前穿戴装备，不能误扣其他部位。旧档在背包、待领取和回收箱中按部位取最高历史等级，不叠加，保留原基础数值并锁定曾强化的旧装备。出售、替换、保存重开和再次读取均不丢部位强化，也不能重复迁移。
18. **蓄力承诺时机**：测试剑卫大地崩裂/终焉裁决、元素师陨星/终极、游侠终极、召唤师星灵/树灵。一次按键自动蓄力，过程有进度、动作、移动减速，不提前扣资源；完成时只扣一次并开始本技能冷却。右键、Esc、Shift 瞬移、死亡、跨区与退出标题会清理；取消没有延迟攻击、额外普攻或残留特效。失焦暂停期间进度不继续。
19. **技能拖动**：HUD与K面板中，按住已学主动图标拖到空槽即移动，拖到已占用主动槽即交换；拖到未学预设槽只保留拖入技能，源清空。同格、栏外放开或取消不施法、不花能量、不改配置；单击仍按对应交互工作。翻页后各页互不覆盖，保存重开保留摆放，拖动前后的剩余冷却连续。
20. **召唤协战**：灵狼近战、星灵远程、树灵范围重击和击倒均有效。普通召唤物总数不超过4，树灵不超过1；达到上限替换旧伙伴。伙伴有血量，可被攻击、拦截敌弹并被回春共鸣治疗；存在时间结束后消失。原野中不主动攻击未被激怒的普通怪。跨区、死亡、回标题、新建或继续角色后不得留下旧伙伴/投射物，也不能隔场景攻击或挡弹。
21. **副本地面掉落**：击杀后先看到品质光柱、装备小模型与名称，背包尚未增加；落地短暂保护后走近自动拾取一次。停留、反复靠近或重复退出不能复制同一装备。背包72格时转成对应金币并提示，不挤掉原装备。分别测试未拾取就返回营地、回标题、正常退出、死亡复活；重开检查物品或金币已到账。无存档写入错误时完成；强制杀进程/断电不视为已通过正常退出用例。
22. **敌人差异**：史莱姆跳扑/减速，哥布林绕侧/出刀后侧移，魔灵退让/双弹，普通守卫正面石甲与蓄力破绽清晰可辨。首领重击、冲锋和弹幕轮换，红圈可见并可闪避；击倒等控制能打断普通怪，首领控制时间较短。敌人死亡只结算一次，切区不留下红圈或延迟攻击。
23. **分区场景与障碍**：原野依次经木桥走过营地、西林地、东草地、北庭院并触发传送门；副本从入口穿过中央长廊、东西侧廊、横厅、祭坛。河流阻挡步行，木桥可通行；树干、岩石、石墙及低矮石块均阻挡，角色、怪物与召唤物不能穿过，AI可绕行抵达目标。射弹会被实体障碍拦截，但河面不挡射弹。观察铺装无闪烁，敌方红圈/技能预览/地面装备清楚可见；出生点、传送门、各刷怪点均可达。
24. **跳跃、瞬移与视角**：Space 原地起跳、历时0.55秒、无冷却，落地后即可再次起跳；检查静止或按住方向键时，起跳到落地均不产生水平位移，面朝河流或障碍也能原地跳跃，空中不能重复起跳。Shift 瞬移最远约4.8米、冷却2.1秒；检查安全落点跨窄河成功、河中落点拒绝、实体石墙/树干/岩石无法穿越，低矮石块也不能越过。镜头右键拖动可调水平朝向与俯仰，滚轮只缩放；面板交互、拖动快捷栏、技能选点或取消蓄力时不误触转镜，失焦与暂停后不残留拖动状态。Q保持可自定义绑定，Space/Shift不占技能格。
25. **药剂快捷栏**：I背包底部生命药剂行点击「放入快捷栏」，可选择当前页槽位或翻页配置；同页再次配置移动已有入口，点击原格可移除。HUD里药剂与已学技能互相拖动可交换，未学占位视为空槽。按槽位绑定键/点击图标/F均能使用同一份库存，满血或暂停时不误消耗；零数量仍保留灰色图标，补货后恢复使用。三页各自保留一个入口，移动不消耗数量、不改变技能冷却；保存重开与另存为后布局保持。


## 0.4.0 待运行验收

本轮最终检查通过不代表Play Mode/真机已运行。请执行 画面/触屏/删除/退出专项清单，仅在实际执行后记录通过。

## 0.4.0 expanded acceptance (pending actual Unity/device execution)

- Run all four classes through basic attack, charge cancel, filled crescent/ice/fire/summon impacts and reduced-effects mode. Check anticipation, contact, decay, silhouettes, dangerous floor telegraphs and simultaneous number/FX caps.
- iPhone compact/notched landscape and iPad: all ten skill positions visible, passives inert; tap area skills once, move/attack/interact with separate fingers. Blank-area vertical drag adjusts only camera; UI content drag scrolls without equipping, selling, deleting or dragging camera.
- Approach each town NPC/portal and every unlocked room exit while holding the movement thumb. Interaction opens once, cancel restores play. Test threshold just inside/outside and no precision model tapping.
- Save slot A, make B, reload A, confirm/cancel manual overwrite, Save-and-Load versus Discard-and-Load, broken target slot, rapid repeated clicks, focus/pause/quit coalescing, named deletion including active/last/corrupt backup. Use disposable fixtures only. Failed save must retain a usable pause/load route even after death or challenge settlement.
- Travel all three unlocked hubs; locked destinations stay unavailable. Buy/sell/upgrade/exchange through visible NPCs, confirm locked gear protection and failed-write rollback. Travel cannot reroll inventory or escape a nearby fight.
- Play ordinary dungeon, hold-point, timed breakthrough and boss gauntlet through win/failure/retry. Pause, inventory, blessing choice and app background must stop objective clocks. Check repeated/stale enemy callbacks never pay twice.
- Complete the five-room large expedition, optional crystal branch and rest choice. Doors unlock after actual required enemies; props do not count. No skipping locked rooms, no cooldown reset between rooms, no backtracking rewards, only one active room/world.
- Final large boss: original ring/core silhouette and footprint; 70%/35% anchors, windup interruption, beam preview/hit alignment/cover, exposure windows. Mobile boss-targeted area attacks must reach anchors; cleanup on death/leave/load/retry.
- Break crate/pot/rubble with melee, projectile and multi-tick area casts. One recovery reward per prop; collision/path opens promptly; no permanent currency farming or required-route blockage.
- Confirm red dots disappear after learning/claim/equip/review/sale and never mark level-locked gear. Recap cards remain within safe margins and scroll; objective and healing charge rows never overlap.
- Verify platform icon in installed app/taskbar/home screen. Measure build/download/install size and GPU/frame-time on target hardware; repository byte counts are not those measurements.

## 本轮可靠性回归：仍待真实引擎/设备执行

- 暂停时联合退点与两套配装切换：技能点严格守恒，已学1阶保留，装备按原编号恢复；缺失装备、确认后配置改变和写入失败都不应部分应用。灵狼/常驻星灵的阶数、生命上限与路线应立即跟随，不能靠退点保留高阶伙伴，也不重置技能冷却
- 野外背包和保护栏满、或模拟保存失败：已经产生的掉落应留在地面，以同一编号重试；恢复后只得到一次。整理之前不新增替补野怪，现有怪物仍可正常战斗；不自动出售珍贵装备
- 副本/房间过渡写入失败：原房间、伙伴、冷却和未拾取物品仍保留；成功过渡后旧对象不能补发奖励或伤害新场景
- 连续伤害在正常帧、一次200ms/500ms尖峰、击杀触发祝福选择暂停之后恢复：应结清应有事件而非重复或丢失，取消/死亡/跨场景仍终止旧效果。补算不应重复播放同一次命中的开场特效
- 固定同一套装备记录真实5秒爆发、10秒持续、能量支出和移动目标命中率；与CSV的理想事件模型分别保存，不能把CSV当作实测DPS。检查Boss在水面/障碍分隔、8.5–9.5米范围追近超时后仍能用有预警的合法远程攻击
- 手机最小横屏、刘海安全区与iPad：Boss条、交互按钮、十格技能、通知和存档错误之间没有遮挡；地图显示真实水面/桥/可破坏障碍，城镇门户与地图标记颜色一致
- 在隔离测试角色中检查一次击杀只产生一次合并的基础奖励保存；若回调或独立掉落确实修改资料，可另有保存。写入失败仍保留当前获得的进度，恢复后只重试保存而不再次发奖
- 完成一次真实Windows构建/iOS导出后，查看固定的构建体积报告与增量；分别测量发行包、签名IPA和实际安装占用，不能用源码字节数替代。所有失败/删除/覆盖用例均使用可丢弃的隔离角色，勿拿唯一的正式存档做破坏性测试
