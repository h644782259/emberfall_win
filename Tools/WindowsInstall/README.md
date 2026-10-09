# Windows 一键构建与安装

本文件夹包含可复用的安装脚本和说明。请保留它在完整 Windows 代码仓库中的位置；脚本还会调用项目 `Tools` 中的构建、打包工具及安装器源码，不能只复制此文件夹到其他电脑。

## 构建电脑准备

- 64 位 Windows 10 / Windows 11，Windows PowerShell 5.1 或更新版本。
- 从 GitHub 获取完整 `emberfall_win` 仓库。需要最新代码时，先更新本地代码；脚本不会自动拉取、提交或推送 Git。
- 在 Unity Hub 安装 Unity 6，并安装 Windows Build Support (Mono)。版本参考项目 `ProjectSettings/ProjectVersion.txt`。
- 在 Unity Hub 登录并激活有效的 Unity Editor 许可证。首次构建可能需要联网解析项目依赖。
- 关闭游戏和打开此项目的 Unity 编辑器。

## 直接运行

双击本文件夹中的 `Install.cmd`。

默认安装到当前用户的 `%LOCALAPPDATA%\Programs\Emberfall`，无需固定盘符。安装器和 ZIP 默认保存在安装目录的 `Installer` 子目录，桌面会生成 `Emberfall` 快捷方式。

脚本依次执行：Unity 构建 → 打包并验证安装包 → 安装或更新 → 对比全部构建文件的 SHA-256 → 创建快捷方式。任何步骤失败都会停止，不会继续安装旧构建。安装和更新保留游戏的独立存档。

## 自定义路径

在仓库根目录打开 PowerShell：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\WindowsInstall\Build-Install-Windows.ps1 -InstallDirectory "E:\Emberfall" -PackageDirectory "E:\Emberfall\安装包"
```

其他电脑可以替换为存在的盘符和目录。安装目录及安装包目录必须位于代码仓库外，并且不能是磁盘根目录。默认用户目录通常不需要管理员权限；自定义路径需要对安装目录及其父目录具有写入权限，安装器会在父目录创建临时目录。

Unity 安装在自定义位置时，额外传入：

```powershell
-UnityPath "D:\Unity\Editor\Unity.exe"
```

不需要桌面快捷方式时，额外传入 `-SkipDesktopShortcut`。

## 输出与分发

- `Builds\Windows\Emberfall.exe`：游戏构建。必须与该目录的配套文件一起使用，不能只复制 EXE。
- 安装包目录中的 `Emberfall-Setup.exe`：可分发的安装器。
- 安装包目录中的 `Emberfall-Windows-x64.zip`：完整解压后可直接运行的免安装版。
- 安装包目录中的 `SHA256SUMS.txt`：安装包校验值。

只玩游戏的电脑不需要 Unity 或源码，直接运行安装器即可。安装器界面可选择安装位置和桌面快捷方式。

## 失败时检查

- 许可证错误：在 Unity Hub 激活许可证后重试。
- 找不到 Unity：安装编辑器，或用 `-UnityPath` 指定路径。
- 缺少 Windows 模块：在 Unity Hub 为所用编辑器添加 Windows Build Support (Mono)。
- 游戏正在运行：关闭游戏后重试。
- 无写入权限：改用默认用户安装目录，或选择有写入权限的目录。
- 编译或构建失败：查看仓库 `Logs\windows-build.log`。

脚本不会自动启动游戏；完成后双击桌面快捷方式。
