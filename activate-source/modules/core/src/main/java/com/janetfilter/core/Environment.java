/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core;

import com.janetfilter.core.utils.ProcessUtils;
import com.janetfilter.core.utils.StringUtils;
import java.io.File;
import java.lang.instrument.Instrumentation;

public final class Environment {
    private final String pid;
    private final String version;
    private final int versionNumber;
    private final String appName;
    private final File baseDir;
    private final File agentFile;
    private final File configDir;
    private final File pluginsDir;
    private final File logsDir;
    private final String nativePrefix;
    private final String disabledPluginSuffix;
    private final boolean attachMode;
    private final Instrumentation instrumentation;

    public Environment(Instrumentation instrumentation, File agentFile, boolean attachMode) {
        this(instrumentation, agentFile, null, attachMode);
    }

    public Environment(Instrumentation instrumentation, File agentFile, String app, boolean attachMode) {
        this.instrumentation = instrumentation;
        this.agentFile = agentFile;
        this.baseDir = agentFile.getParentFile();
        if (StringUtils.isEmpty(app)) {
            this.appName = "";
            this.configDir = new File(this.baseDir, "config");
            this.pluginsDir = new File(this.baseDir, "plugins");
            this.logsDir = new File(this.baseDir, "logs");
        } else {
            this.appName = app.toLowerCase();
            this.configDir = new File(this.baseDir, "config-" + this.appName);
            this.pluginsDir = new File(this.baseDir, "plugins-" + this.appName);
            this.logsDir = new File(this.baseDir, "logs-" + this.appName);
        }
        this.pid = ProcessUtils.currentId();
        this.version = "2022.2.0";
        this.versionNumber = 202201000;
        this.nativePrefix = StringUtils.randomMethodName(15) + "_";
        this.disabledPluginSuffix = ".disabled.jar";
        this.attachMode = attachMode;
    }

    public String getPid() {
        return this.pid;
    }

    public String getVersion() {
        return this.version;
    }

    public int getVersionNumber() {
        return this.versionNumber;
    }

    public String getAppName() {
        return this.appName;
    }

    public File getBaseDir() {
        return this.baseDir;
    }

    public File getAgentFile() {
        return this.agentFile;
    }

    public File getConfigDir() {
        return this.configDir;
    }

    public File getPluginsDir() {
        return this.pluginsDir;
    }

    public File getLogsDir() {
        return this.logsDir;
    }

    public String getNativePrefix() {
        return this.nativePrefix;
    }

    public String getDisabledPluginSuffix() {
        return this.disabledPluginSuffix;
    }

    public boolean isAttachMode() {
        return this.attachMode;
    }

    public boolean isJavaagentMode() {
        return !this.attachMode;
    }

    public Instrumentation getInstrumentation() {
        return this.instrumentation;
    }

    public String toString() {
        return "Environment: {\n\tpid = " + this.pid + ", \n\tversion = " + this.version + ", \n\tversionNumber = " + this.versionNumber + ", \n\tappName = " + this.appName + ", \n\tbaseDir = " + this.baseDir + ", \n\tagentFile = " + this.agentFile + ", \n\tconfigDir = " + this.configDir + ", \n\tpluginsDir = " + this.pluginsDir + ", \n\tlogsDir = " + this.logsDir + ", \n\tnativePrefix = " + this.nativePrefix + ", \n\tdisabledPluginSuffix = " + this.disabledPluginSuffix + ", \n\tattachMode = " + this.attachMode + "\n}";
    }
}

