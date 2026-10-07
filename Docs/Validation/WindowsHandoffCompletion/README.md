# Windows 交接最终验证

实现范围与迁移约定见 [WINDOWS-HANDOFF-COMPLETION.md](../../WINDOWS-HANDOFF-COMPLETION.md)。
本目录保留最终完整检查、原生运行和定向复核的原始报告及日志。

## 执行命令

```powershell
$env:TMP='E:\emberfall_win\Tests\TestResults\Temp'
$env:TEMP=$env:TMP
$env:DOTNET_CLI_HOME=$env:TMP
$env:PYTHONUTF8='1'
$env:DOTNET='E:\emberfall_win\Tests\TestResults\Toolchain\dotnet\dotnet.exe'
& 'C:\Users\HYX\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' Tools/cloud-validation.py --dotnet $env:DOTNET --jobs 4 --compile --unity-editor 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe' --output Tests/TestResults/WindowsHandoffFrozen
& 'C:\Users\HYX\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' Tools/cloud-validation.py --dotnet $env:DOTNET --jobs 2 --only chapter-return-time-scale,effectpriorityproduction --output Tests/TestResults/Handoff-LeafFixtureRepairs
& pwsh -NoProfile -File Tools/Run-HandoffManagedPlayer.ps1 -LogicOnly
```

Python 3.12.14 / numpy 2.3.5；Unity API 引用版本 6000.6.3f1。
完整 suite 的源码摘要覆盖 Assets C#、Tests C#/Python、Tools Python 与 Run-CloudValidation.sh。
补充 manifest 覆盖 ArtSource Python、Tools PowerShell 与 Assets meta 文件。
`index-source-sha256.json` 另记录暂存Git内容的SHA256；已与实际执行的工作树逐文件比较，
只允许CRLF/LF规范化差异，避免把Git行尾转换误判为代码变更。原报告仍保留实际执行字节的哈希。

## 已完成的独立验证

- `full/report.json`：完整检查 **272/294 通过**，结束于 2026-10-07T13:15:56.379634+00:00，`sourceChangedDuringRun=[]`。所有原日志均归档；Unity 6000.6.3f1 七种条件编译检查通过。
- `fixture-repairs/report.json`：移除两个独立测试的重复方法声明后 **2/2 通过**，结束于 2026-10-07T13:17:39.037109+00:00，运行期间同样无源码漂移。时间缩放126项断言及2个编译负对照、特效优先级164项断言及5个编译负对照均通过。
- `final-summary.json`：两次运行的最新汇总 **274/294 通过，20项失败**。这是完整冻结运行加两项叶子夹具复跑的汇总，不是第二次完整运行。完整运行后仅修改 `Tests/ChapterReturnTimeScaleTests.py` 和 `Tests/EffectPriorityProductionTests.cs`；它们只影响上述两项检查。Assets、Tools及共享夹具没有再改动，游戏源码与完整运行哈希一致；当前全部源码哈希与最后复跑快照一致。
- `native/report.json`：103 项原生断言通过，0 控制台错误；包括字号 GUIStyle、挂件与旧方案迁移、保存回滚、奖励幂等、平台碰撞、限疗守印/断供路线和下一阶结算。
- `journey/report.json`：2/2 检查通过。综合流程 4,846 项断言，8 个四职业新旧存档流程，98 次章节尝试，6 个编译负对照；章节宿主另有 12 个编译负对照。
- `final-repairs/report.json`：4/4 检查通过，包括职业切换、挂件方案变体、奖励展示和 PC 快捷栏。
- `directed/room-side-branch.log`：1,740 项断言，162 条第 15 阶路线，108 次支线双敌生成。

## 验证边界

原生验证重新编译当前运行时脚本并使用同版本的既有播放器和缓存资产，存档隔离。
敌人 AI 与玩家输入在验证进程内禁用，通过注入清场/封印进度推进；逻辑模式没有截图。
完整 suite 是独立生产逻辑与 Unity API 编译检查，不包含编辑器运行、渲染和平台构建。
编辑器在 BuildReportRestService 启动阶段崩溃；全新资产构建未完成。
隐藏窗口的截图为黑屏，因此字体、翅膀材质和大招画面仍待可见窗口验收。
用户最初的守印故障尚未复现，原始根因仍未确认。

恢复分支原基线 `../ControlsReadiness/full/report.json` 为 251/288 通过，37 项失败。
剩余20项包含4项Windows符号链接权限限制，以及16项旧夹具、旧断言和展示兼容问题。
其中13项旧故障的诊断成员/断言签名保持一致；2项修复旧UI夹具后才暴露下层重铸档位断言，
另1项奖励展示检查由旧12项历史回执提示失败扩展为45项（新增33项仍要求旧长结算文字）。
这些签名变化没有被称为原基线完全一致。详见 [FAILURES.md](FAILURES.md) 和 `failure-classification.json`。
因此本次没有宣称完整suite全绿，也没有宣称全部玩法与画面已验收。
