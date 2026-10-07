# 剩余失败复核

最新汇总为274/294通过，20项失败。4项符号链接权限限制，16项旧夹具/断言及展示兼容问题。
没有将同名检查自动判断为同一故障；下表注明了签名变化。所有原日志保留。

| 检查 | 分类 | 复核说明 |
| --- | --- | --- |
| [asset-meta-guid-controls](full/asset-meta-guid-controls.log) | Windows环境限制 | 创建符号链接需要系统特权（WinError1314 / CreateSymbolicLink）；相应检查未完成，没有跳过保护断言。 |
| [progression](full/progression.log) | 既有断言，同签名 | boss loot has at least rare quality；原基线也在断言 35 失败。 |
| [save-deletion](full/save-deletion.log) | Windows环境限制 | 创建符号链接需要系统特权（WinError1314 / CreateSymbolicLink）；相应检查未完成，没有跳过保护断言。 |
| [save-idempotence](full/save-idempotence.log) | Windows环境限制 | 创建符号链接需要系统特权（WinError1314 / CreateSymbolicLink）；相应检查未完成，没有跳过保护断言。 |
| [enemy-kill-rewards](full/enemy-kill-rewards.log) | 既有断言，同签名 | post-reward loot uses the gained level while rolling alone remains read-only；旧连续等级 oracle。 |
| [reforgeselection](full/reforgeselection.log) | 旧夹具修复后暴露下层断言，签名不同 | three distinct actual choices；原基线先因缺失按钮 helper 编译失败。实际生成等级按档位，不保证旧 15/19/50 三选项。重铸生产实现未改。 |
| [returning-counter-persistence](full/returning-counter-persistence.log) | 旧夹具修复后暴露下层断言，签名不同 | reforge retainsB；原基线先因缺失 NavigationButton 编译失败。夹具要求重铸到25，但实际生成档位为20；不是变体B丢失的直接证据。 |
| [chest-choice-presentation](full/chest-choice-presentation.log) | 旧源码 oracle，同签名 | 仍要求 ResultWithCollection 出现在旧结果文字位置。 |
| [build-plan-page-geometry](full/build-plan-page-geometry.log) | 旧源码 oracle，同签名 | button font policy changed; update explicit boundary；仍要求旧 Button 的字体条件表达式。真实字体样式变更由103项原生验证覆盖。 |
| [scenery-presentation-production](full/scenery-presentation-production.log) | 既有宿主编译错误，同成员签名 | HubNpcIdle 缺 GameSession.Instance/Player、HubNpcKind、Time.unscaledDeltaTime、Mathf.MoveTowards/Clamp01、Quaternion.LookRotation/RotateTowards/Slerp 等17项错误。 |
| [fixed-scenery-production](full/fixed-scenery-production.log) | 既有宿主编译错误，同成员签名 | 同 HubNpcIdle 17项缺失成员；场景逻辑未执行。 |
| [fixed-scenery-enabled-integration](full/fixed-scenery-enabled-integration.log) | 既有宿主编译错误，同成员签名 | 同 HubNpcIdle 17项缺失成员；场景逻辑未执行。 |
| [concentrated-venom-production](full/concentrated-venom-production.log) | 既有断言，同签名 | reforge retainsB；旧25级重铸 oracle。前面的 front guard 异常为已通过的编译负对照，不能当正向故障。 |
| [opportunity-channels-round2](full/opportunity-channels-round2.log) | 既有宿主编译错误，同成员签名 | 缺 CanUseMovementSkillAt 与 SkillBudgetReady。 |
| [camp-practice-session](full/camp-practice-session.log) | 既有宿主编译错误，同成员签名 | Shutdown.cs 缺 EndHubNpcConversation。 |
| [camp-practice-production](full/camp-practice-production.log) | 旧负对照标记，同签名 | 脚本 assert old in copies[f] 失败；前面的实践隔离正向和三项编译负对照已通过。 |
| [g07-factory-inventory](full/g07-factory-inventory.log) | 既有宿主编译错误，同成员签名 | 其固定场景子进程同 HubNpcIdle 17项缺失成员。 |
| [reward-presentation-exceptions](full/reward-presentation-exceptions.log) | 既有问题加本次展示兼容，签名扩展 | 原基线12项历史回执缺少明确未知提示；本次45项=原12项+33项旧文字断言。奖励数额移到图标卡，旧断言仍要求长结算文字包含碎片/通关经验。原子持久金额与结构快照断言仍通过；此项未通过，未将新增33项称为原签名。 |
| [reward-revision](full/reward-revision.log) | Windows环境限制 | 创建符号链接需要系统特权（WinError1314 / CreateSymbolicLink）；相应检查未完成，没有跳过保护断言。 |
| [practice-potion-input](full/practice-potion-input.log) | 既有宿主编译错误，同成员签名 | Actual.cs 缺 EndHubNpcConversation。 |
