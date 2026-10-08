# activate 源码恢复与实现原理

已将 `D:\Project\unlock\activate` 中可恢复的源码整理到本目录。

**9 个 VBS 是原始脚本；48 个 Java 文件是从本地 5 个 JAR 反编译得到的可读源码，覆盖全部 53 个 class 文件。Java 源码未经手工修改。** 本次没有取得作者的完整原始工程，不能恢复原始注释、全部局部变量名、构建配置或提交历史。

原目录的 36 个文件保持不变，已用 SHA-256 校验；完整副本保存在 `original-package`。未运行激活脚本、未把 Agent 注入 IDE，也未执行附带文本中的删除操作。

## 从这里阅读

- [实现原理与后续封装注意点](D:/Project/unlock/activate-source/实现原理.md)
- [全部源码索引](D:/Project/unlock/activate-source/SOURCE_INDEX.md)
- [原始 IDEA 脚本](D:/Project/unlock/activate-source/original-package/IDEA激活.vbs)
- [Java Agent 入口 Launcher.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/Launcher.java)
- [插件加载器 PluginManager.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/plugin/PluginManager.java)
- [本地 Power 插件入口](D:/Project/unlock/activate-source/modules/power/src/main/java/com/janetfilter/plugins/power/PowerPlugin.java)

## 目录与来源

| 目录 | 内容 | 来源 |
| --- | --- | --- |
| `original-package/` | 9 个 VBS、5 个 JAR、3 个配置、9 个 key、9 个 vmoptions、1 个说明 | 原目录逐文件复制，未改内容 |
| `modules/core/src/main/java/` | 30 个 Java 文件 | `active-agt.jar` 的 35 个 class |
| `modules/dns/src/main/java/` | 3 个 Java 文件 | `plugins/dns.jar` |
| `modules/hideme/src/main/java/` | 5 个 Java 文件 | `plugins/hideme.jar` |
| `modules/power/src/main/java/` | 7 个 Java 文件 | `plugins/power.jar` |
| `modules/url/src/main/java/` | 3 个 Java 文件 | `plugins/url.jar` |
| `modules/*/src/main/resources/` | 原始 Manifest 和其他非 class 资源 | 从各 JAR 原样提取 |
| `tools/` | CFR 0.152、源码恢复脚本 | 恢复工具，不属于原 activate |
| `reports/` | 哈希、文件清单、反编译日志、字节码核对、编译结果 | 本次分析生成 |

内部类会合并到外部类的 Java 文件中，因此 53 个 class 对应 48 个 Java 文件。

## 验证结果与边界

- 5 个 JAR 均完成反编译，class 到 Java 文件的映射无缺失。
- 使用本机 JDK 25.0.4.1 的编译器、已安装的 JetBrains Runtime 21.0.8 系统类库，全部 48 个 Java 文件编译通过，产生 53 个 class。源码未修改，注解处理关闭。
- 直接使用 JDK 25 系统类库的尝试失败，缺少样本依赖的 `jdk.internal.org.objectweb.asm`。两次编译日志均保留。
- 原包 class 主版本为 52（Java 8），本次编译检查生成主版本 65（Java 21）；此次检查不证明 Java 8 重建成功、不证明二进制一致，也不证明 Agent 在任何 IDE/JBR 版本上可用。
- CFR 在两个同步代码块处留下恢复警告，在 7 个 Transformer 文件中提示未加载内部 ASM 类型。保留这些注释；关键方法的原始 `javap` 字节码另存于 `reports`。
- 原工程没有 `pom.xml`、Gradle 配置或源码包。本次没有编造一套作者的构建工程，也没有生成一体化软件。

详细结果：[最终校验](D:/Project/unlock/activate-source/reports/verification.json)、[源码恢复报告](D:/Project/unlock/activate-source/reports/recovery-report.json)、[通过的编译检查](D:/Project/unlock/activate-source/reports/compile-check-jbr21/result.json)。

如需在当前电脑重做编译检查，可在 PowerShell 执行以下命令。它只编译到 `reports/compile-check-jbr21/classes`，不启动 Agent：

```powershell
& 'D:\Java\jdk-25.0.4.1\bin\javac.exe' '-J-Duser.language=en' '-J-Duser.country=US' '@D:\Project\unlock\activate-source\reports\compile-check-jbr21\javac.args'
```

## 复现源码恢复

需要 Python 3.9+、可用的 `java` 命令和本目录中已经校验的 CFR。指定一个新输出目录；脚本会拒绝覆盖已有的 `original-package` 和 `modules`。

```powershell
python 'D:\Project\unlock\activate-source\tools\recover_source.py' --input 'D:\Project\unlock\activate' --output 'D:\Project\unlock\activate-source-recovered' --decompiler 'D:\Project\unlock\activate-source\tools\cfr-0.152.jar'
```

该脚本只恢复源码、提取资源和生成恢复报告；本文档、索引、字节码核对及编译检查是本次额外整理的交付材料。

反编译器使用 [CFR 官方说明](https://www.benf.org/other/cfr/) 中的 0.152 版本，下载自 Maven Central。已核对作者公布的 MD5，并保存本地 SHA-256；见 [工具来源](D:/Project/unlock/activate-source/reports/decompiler.json)。
