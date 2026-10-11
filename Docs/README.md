# 项目维护文档

项目代码位于 `Assets/Scripts`，回归测试位于 `Tests`，构建和验证工具位于 `Tools`。美术源工程、生成脚本与原始图集位于 `ArtSource`，运行时资源位于 `Assets/Resources`。

- [存档与幂等提交](Idempotent-Persistence.md)
- [角色删除与存储边界](Save-Safety-Storage.md)
- [暂停、保存和读取](Pause-Save-Load.md)
- [章节经验预算](Chapter-Experience-Budget.md)
- [回归测试说明](../Tests/PLAYTEST.md)
- [验证工具用法](../Tools/cloud-validation.md)

验证截图、录像、日志、冻结源码副本和单次交付报告不作为项目源文件维护。需要时通过现有测试和预览工具重新生成，输出到 Git 忽略的本地目录。
