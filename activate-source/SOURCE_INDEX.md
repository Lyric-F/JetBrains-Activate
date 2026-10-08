# 源码索引

本索引对应本地样本的恢复结果；所有 Java 文件保留 CFR 原样输出。

## 原始脚本与产品映射

| 脚本 | 产品目录标识 | vmoptions | key |
| --- | --- | --- | --- |
| [CLion激活.vbs](D:/Project/unlock/activate-source/original-package/CLion激活.vbs) | `CLion` | `clion64.exe.vmoptions` | `clion.key` |
| [DataGrip激活.vbs](D:/Project/unlock/activate-source/original-package/DataGrip激活.vbs) | `DataGrip` | `datagrip64.exe.vmoptions` | `datagrip.key` |
| [DataSpell激活.vbs](D:/Project/unlock/activate-source/original-package/DataSpell激活.vbs) | `DataSpell` | `dataspell64.exe.vmoptions` | `dataspell.key` |
| [GoLand激活.vbs](D:/Project/unlock/activate-source/original-package/GoLand激活.vbs) | `GoLand` | `goland64.exe.vmoptions` | `goland.key` |
| [IDEA激活.vbs](D:/Project/unlock/activate-source/original-package/IDEA激活.vbs) | `IntelliJIdea` | `idea64.exe.vmoptions` | `idea.key` |
| [PhpStorm激活.vbs](D:/Project/unlock/activate-source/original-package/PhpStorm激活.vbs) | `PhpStorm` | `phpstorm64.exe.vmoptions` | `phpstorm.key` |
| [PyCharm激活.vbs](D:/Project/unlock/activate-source/original-package/PyCharm激活.vbs) | `PyCharm` | `pycharm64.exe.vmoptions` | `pycharm.key` |
| [Rider激活.vbs](D:/Project/unlock/activate-source/original-package/Rider激活.vbs) | `Rider` | `rider64.exe.vmoptions` | `rider.key` |
| [WebStorm激活.vbs](D:/Project/unlock/activate-source/original-package/WebStorm激活.vbs) | `WebStorm` | `webstorm64.exe.vmoptions` | `webstorm.key` |

## core：35 个 class → 30 个 Java 文件

来源：`active-agt.jar`；SHA-256：`3acc4e9d91793f6909458a4761b75b6da45c8868e75dca33c9fec63659202995`。

- [com/janetfilter/core/Dispatcher.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/Dispatcher.java)
- [com/janetfilter/core/Environment.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/Environment.java)
- [com/janetfilter/core/Initializer.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/Initializer.java)
- [com/janetfilter/core/Launcher.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/Launcher.java)
- [com/janetfilter/core/attach/VMDescriptor.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/attach/VMDescriptor.java)
- [com/janetfilter/core/attach/VMLauncher.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/attach/VMLauncher.java)
- [com/janetfilter/core/attach/VMSelector.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/attach/VMSelector.java)
- [com/janetfilter/core/commons/ConfigParser.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/commons/ConfigParser.java)
- [com/janetfilter/core/commons/DebugInfo.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/commons/DebugInfo.java)
- [com/janetfilter/core/enums/RuleType.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/enums/RuleType.java)
- [com/janetfilter/core/models/FilterRule.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/models/FilterRule.java)
- [com/janetfilter/core/plugin/MyTransformer.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/plugin/MyTransformer.java)
- [com/janetfilter/core/plugin/PluginClassLoader.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/plugin/PluginClassLoader.java)
- [com/janetfilter/core/plugin/PluginConfig.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/plugin/PluginConfig.java)
- [com/janetfilter/core/plugin/PluginEntry.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/plugin/PluginEntry.java)
- [com/janetfilter/core/plugin/PluginManager.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/plugin/PluginManager.java)
- [com/janetfilter/core/rulers/EqualICRuler.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/rulers/EqualICRuler.java)
- [com/janetfilter/core/rulers/EqualRuler.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/rulers/EqualRuler.java)
- [com/janetfilter/core/rulers/KeywordICRuler.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/rulers/KeywordICRuler.java)
- [com/janetfilter/core/rulers/KeywordRuler.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/rulers/KeywordRuler.java)
- [com/janetfilter/core/rulers/PrefixICRuler.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/rulers/PrefixICRuler.java)
- [com/janetfilter/core/rulers/PrefixRuler.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/rulers/PrefixRuler.java)
- [com/janetfilter/core/rulers/RegExpRuler.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/rulers/RegExpRuler.java)
- [com/janetfilter/core/rulers/Ruler.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/rulers/Ruler.java)
- [com/janetfilter/core/rulers/SuffixICRuler.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/rulers/SuffixICRuler.java)
- [com/janetfilter/core/rulers/SuffixRuler.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/rulers/SuffixRuler.java)
- [com/janetfilter/core/utils/DateUtils.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/utils/DateUtils.java)
- [com/janetfilter/core/utils/ProcessUtils.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/utils/ProcessUtils.java)
- [com/janetfilter/core/utils/StringUtils.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/utils/StringUtils.java)
- [com/janetfilter/core/utils/WhereIsUtils.java](D:/Project/unlock/activate-source/modules/core/src/main/java/com/janetfilter/core/utils/WhereIsUtils.java)

原样提取的资源：

- [META-INF/MANIFEST.MF](D:/Project/unlock/activate-source/modules/core/src/main/resources/META-INF/MANIFEST.MF)
- [6c81ec87e55d331c267262e892427a3d93d76683.txt](D:/Project/unlock/activate-source/modules/core/src/main/resources/6c81ec87e55d331c267262e892427a3d93d76683.txt)

## dns：3 个 class → 3 个 Java 文件

来源：`plugins/dns.jar`；SHA-256：`d1150b1831b112b93d74a34a10ce6c11606e0d2255d532c29f91f1d92b40a552`。

- [com/janetfilter/plugins/dns/DNSFilter.java](D:/Project/unlock/activate-source/modules/dns/src/main/java/com/janetfilter/plugins/dns/DNSFilter.java)
- [com/janetfilter/plugins/dns/DNSFilterPlugin.java](D:/Project/unlock/activate-source/modules/dns/src/main/java/com/janetfilter/plugins/dns/DNSFilterPlugin.java)
- [com/janetfilter/plugins/dns/InetAddressTransformer.java](D:/Project/unlock/activate-source/modules/dns/src/main/java/com/janetfilter/plugins/dns/InetAddressTransformer.java)

原样提取的资源：

- [META-INF/MANIFEST.MF](D:/Project/unlock/activate-source/modules/dns/src/main/resources/META-INF/MANIFEST.MF)

## hideme：5 个 class → 5 个 Java 文件

来源：`plugins/hideme.jar`；SHA-256：`fa14c735ab9fed3f3a5df0dc78a5d38ae0a146099ddc858197e9f528bd996c40`。

- [com/janetfilter/plugins/hideme/ClassNameFilter.java](D:/Project/unlock/activate-source/modules/hideme/src/main/java/com/janetfilter/plugins/hideme/ClassNameFilter.java)
- [com/janetfilter/plugins/hideme/ClassNameTransformer.java](D:/Project/unlock/activate-source/modules/hideme/src/main/java/com/janetfilter/plugins/hideme/ClassNameTransformer.java)
- [com/janetfilter/plugins/hideme/HideMePlugin.java](D:/Project/unlock/activate-source/modules/hideme/src/main/java/com/janetfilter/plugins/hideme/HideMePlugin.java)
- [com/janetfilter/plugins/hideme/VMTransformer.java](D:/Project/unlock/activate-source/modules/hideme/src/main/java/com/janetfilter/plugins/hideme/VMTransformer.java)
- [com/janetfilter/plugins/hideme/VmArgumentFilter.java](D:/Project/unlock/activate-source/modules/hideme/src/main/java/com/janetfilter/plugins/hideme/VmArgumentFilter.java)

原样提取的资源：

- [META-INF/MANIFEST.MF](D:/Project/unlock/activate-source/modules/hideme/src/main/resources/META-INF/MANIFEST.MF)

## power：7 个 class → 7 个 Java 文件

来源：`plugins/power.jar`；SHA-256：`26db4a54482189dc989e59734318d5c12d656183d5394fe3c0fe5b33313cb785`。

- [com/janetfilter/plugins/power/ArgsFilter.java](D:/Project/unlock/activate-source/modules/power/src/main/java/com/janetfilter/plugins/power/ArgsFilter.java)
- [com/janetfilter/plugins/power/ArgsTransformer.java](D:/Project/unlock/activate-source/modules/power/src/main/java/com/janetfilter/plugins/power/ArgsTransformer.java)
- [com/janetfilter/plugins/power/GoForFreeFilter.java](D:/Project/unlock/activate-source/modules/power/src/main/java/com/janetfilter/plugins/power/GoForFreeFilter.java)
- [com/janetfilter/plugins/power/GoForFreeTransformer.java](D:/Project/unlock/activate-source/modules/power/src/main/java/com/janetfilter/plugins/power/GoForFreeTransformer.java)
- [com/janetfilter/plugins/power/PowerPlugin.java](D:/Project/unlock/activate-source/modules/power/src/main/java/com/janetfilter/plugins/power/PowerPlugin.java)
- [com/janetfilter/plugins/power/ResultFilter.java](D:/Project/unlock/activate-source/modules/power/src/main/java/com/janetfilter/plugins/power/ResultFilter.java)
- [com/janetfilter/plugins/power/ResultTransformer.java](D:/Project/unlock/activate-source/modules/power/src/main/java/com/janetfilter/plugins/power/ResultTransformer.java)

原样提取的资源：

- [META-INF/MANIFEST.MF](D:/Project/unlock/activate-source/modules/power/src/main/resources/META-INF/MANIFEST.MF)

## url：3 个 class → 3 个 Java 文件

来源：`plugins/url.jar`；SHA-256：`ce5a83aee31153cca30274ac94467b316edea8cb28acf72f52f5a72d455b1b43`。

- [com/janetfilter/plugins/url/HttpClientTransformer.java](D:/Project/unlock/activate-source/modules/url/src/main/java/com/janetfilter/plugins/url/HttpClientTransformer.java)
- [com/janetfilter/plugins/url/URLFilter.java](D:/Project/unlock/activate-source/modules/url/src/main/java/com/janetfilter/plugins/url/URLFilter.java)
- [com/janetfilter/plugins/url/URLFilterPlugin.java](D:/Project/unlock/activate-source/modules/url/src/main/java/com/janetfilter/plugins/url/URLFilterPlugin.java)

原样提取的资源：

- [META-INF/MANIFEST.MF](D:/Project/unlock/activate-source/modules/url/src/main/resources/META-INF/MANIFEST.MF)

内部类与 Java 文件的逐项映射可查看 `reports/*-inventory.json`。
