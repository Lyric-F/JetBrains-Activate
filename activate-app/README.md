# Activate Desktop 1.0.0

这是对原 `activate` 包的 Windows 桌面封装。一个下拉菜单选择产品，显示安装版本、配置检查、许可文件及文件声明日期。9 个原始 VBS 与配套资源逐字节嵌入 EXE，未重写或重新编译原 JAR。

直接运行 `D:\Project\unlock\Activate.exe`。工程内 `dist\Activate.exe` 是同一个文件；独立使用只需这一个 EXE。原始包仍在 `D:\Project\unlock\activate`，反编译源码与原理说明在 `D:\Project\unlock\activate-source`。

## 使用

1. 先启动一次目标 IDE，让它生成用户配置，然后退出该产品的所有窗口。
2. 打开 Activate，在下拉菜单选择 IntelliJ IDEA、PyCharm、WebStorm、PhpStorm、GoLand、CLion、DataGrip、DataSpell 或 Rider。
3. 检查版本与将处理的配置目录。自动检测不到安装位置时，点击“定位安装”，选择 IDE 的启动 EXE 或 `product-info.json`，再刷新。
4. 点击“开始激活”。程序先备份，随后运行原 VBS。原脚本弹窗保留，关闭弹窗后等待主界面完成文件校验。
5. 显示“配置已部署”后，重新打开 IDE，在 IDE 内确认实际授权结果。

原脚本按产品目录名匹配并处理该产品的所有匹配配置目录。显示的安装版本优先来自 `product-info.json`，多个安装版本时显示最高版本；没有安装信息时会标注版本由用户配置目录推断。下拉菜单选择产品，不选择单个版本。

原脚本会覆盖对应 `.vmoptions` 和 `.key`，并重建该产品的运行依赖目录。本封装保持这一行为，执行前自动备份。不会把“脚本返回成功”、文件声明日期或原弹窗中的 2099 当作授权验证结果。不同 IDE 版本能否接受原包，仍取决于原包自身兼容性。

## 临时文件与恢复

程序为每次执行创建 `%TEMP%\ActivateDesktop\work\<随机编号>`，保留原目录结构释放资源，校验 SHA-256 后执行脚本。脚本进程退出后删除本次解包目录。普通失败和取消会尝试恢复执行前的数据并清理临时文件。

按原方式部署的 `%APPDATA%\<产品目录>\active-agt.jar`、`config`、`plugins`，以及 `%APPDATA%\JetBrains\<产品版本目录>` 中的许可和启动配置需要保留；IDE 后续启动依赖这些文件。原始 `D:\Project\unlock\activate` 不参与运行时清理。

备份与日志位于 `%LOCALAPPDATA%\ActivateDesktop`，可从“操作记录 → 打开备份与日志目录”访问。“恢复上次配置”按最近一次操作恢复；若部署后又有其他修改，或备份内容损坏，程序会停止覆盖并给出说明。

若系统强制终止程序、文件被占用，或无法确认脚本已经停止，临时文件可能保留。程序会在可记录时给出路径；确认相关脚本已退出后，可按日志处理残留。没有自动删除全部历史临时目录的功能。

## 运行环境

- Windows 桌面环境，.NET Framework 4.8，以及可用的 Windows Script Host / VBScript 组件。
- 程序按当前用户权限运行；不要求安装 Python、Java 开发工具或 .NET SDK。
- 构建产物没有数字签名。本封装没有新增在线更新或下载步骤；原包的运行行为保持原样。

## 源码与构建

`src/MainWindow.xaml` 是界面；`src/App.cs` 负责交互；`src/Models.cs` 负责产品、版本、许可读取与文件工具；`src/Payload.cs` 负责内置资源校验与释放；`src/Deployment.cs` 负责原脚本执行、备份、恢复和清理。

`resources/payload.zip` 包含全部 36 个原始文件，`resources/payload.manifest.json` 记录逐文件 SHA-256。旧 `.key` 使用的特殊编码标记仅在界面读取时兼容，写入磁盘的原始字节不变。

在工程目录运行 PowerShell：

```powershell
.\build.ps1
```

脚本使用 Windows 64 位 .NET Framework 自带 C# 编译器和 WPF 库，不下载 NuGet 包。输出 `dist\Activate.exe`。也提供 `Activate.Desktop.csproj`，可在安装相应 .NET Framework 开发组件的 Visual Studio 中打开。

更新内置资源时才需要 Python：

```powershell
python .\tools\prepare_payload.py --source ..\activate
.\build.ps1
```

## 验证

```powershell
.\build.ps1 -WithTests
python .\tools\run_tests.py
.\tools\test_ui.ps1
```

集成测试在带标记的临时测试用户目录运行，重定向脚本的 `APPDATA`。覆盖 9 个真实原 VBS 的文件操作、资源校验、清理、取消、备份恢复与异常分支。UI 测试加载最终 EXE 的实际控制器和内嵌 XAML，检查 9 个产品的版本、许可日期显示，并渲染界面预览。

测试结果见 `reports/integration-tests.json`、`reports/ui-smoke.json` 和 `reports/delivery-verification.json`；界面预览为 `reports/ui-preview.png`。早期人工交互测试记录在 `reports/ui-verification.json`，覆盖原脚本弹窗、部署完成及界面恢复流程。测试没有执行 JAR、启动真实 IDE 或验证实际授权有效性。

原包来源与 Java 实现说明见相邻的 `..\activate-source\README.md` 和 `..\activate-source\实现原理.md`。
