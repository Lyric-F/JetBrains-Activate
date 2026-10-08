/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.plugin;

import com.janetfilter.core.Dispatcher;
import com.janetfilter.core.Environment;
import com.janetfilter.core.commons.ConfigParser;
import com.janetfilter.core.commons.DebugInfo;
import com.janetfilter.core.plugin.PluginClassLoader;
import com.janetfilter.core.plugin.PluginConfig;
import com.janetfilter.core.plugin.PluginEntry;
import com.janetfilter.core.utils.StringUtils;
import java.io.File;
import java.lang.instrument.Instrumentation;
import java.util.Arrays;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.jar.JarFile;
import java.util.jar.Manifest;

public final class PluginManager {
    private static final String ENTRY_NAME = "JANF-Plugin-Entry";
    private final Instrumentation inst;
    private final Dispatcher dispatcher;
    private final Environment environment;

    public PluginManager(Dispatcher dispatcher, Environment environment) {
        this.inst = environment.getInstrumentation();
        this.dispatcher = dispatcher;
        this.environment = environment;
    }

    public void loadPlugins() {
        long startTime = System.currentTimeMillis();
        File pluginsDirectory = this.environment.getPluginsDir();
        if (!pluginsDirectory.exists() || !pluginsDirectory.isDirectory()) {
            return;
        }
        File[] pluginFiles = pluginsDirectory.listFiles((d, n) -> n.endsWith(".jar"));
        if (null == pluginFiles) {
            return;
        }
        try {
            ExecutorService executorService = Executors.newCachedThreadPool();
            for (File pluginFile : pluginFiles) {
                executorService.submit(new PluginLoadTask(pluginFile));
            }
            executorService.shutdown();
            if (!executorService.awaitTermination(30L, TimeUnit.SECONDS)) {
                throw new RuntimeException("Load plugin timeout");
            }
            DebugInfo.debug(String.format("============ All plugins loaded, %.2fs elapsed ============", (double)(System.currentTimeMillis() - startTime) / 1000.0));
        }
        catch (Throwable e) {
            DebugInfo.error("Load plugin failed", e);
        }
    }

    private class PluginLoadTask
    implements Runnable {
        private final File pluginFile;

        public PluginLoadTask(File pluginFile) {
            this.pluginFile = pluginFile;
        }

        /*
         * WARNING - Removed try catching itself - possible behaviour change.
         */
        @Override
        public void run() {
            try {
                if (this.pluginFile.getName().endsWith(PluginManager.this.environment.getDisabledPluginSuffix())) {
                    DebugInfo.debug("Disabled plugin: " + this.pluginFile + ", ignored.");
                    return;
                }
                JarFile jarFile = new JarFile(this.pluginFile);
                Manifest manifest = jarFile.getManifest();
                String entryClass = manifest.getMainAttributes().getValue(PluginManager.ENTRY_NAME);
                if (StringUtils.isEmpty(entryClass)) {
                    return;
                }
                PluginClassLoader classLoader = new PluginClassLoader(jarFile);
                Class<?> klass = Class.forName(entryClass, false, classLoader);
                if (!Arrays.asList(klass.getInterfaces()).contains(PluginEntry.class)) {
                    return;
                }
                Instrumentation instrumentation = PluginManager.this.inst;
                synchronized (instrumentation) {
                    PluginManager.this.inst.appendToBootstrapClassLoaderSearch(jarFile);
                }
                PluginEntry pluginEntry = (PluginEntry)Class.forName(entryClass).newInstance();
                File configFile = new File(PluginManager.this.environment.getConfigDir(), pluginEntry.getName().toLowerCase() + ".conf");
                PluginConfig pluginConfig = new PluginConfig(configFile, ConfigParser.parse(configFile));
                pluginEntry.init(PluginManager.this.environment, pluginConfig);
                PluginManager.this.dispatcher.addTransformers(pluginEntry.getTransformers());
                DebugInfo.debug("Plugin loaded: {name=" + pluginEntry.getName() + ", version=" + pluginEntry.getVersion() + ", author=" + pluginEntry.getAuthor() + "}");
            }
            catch (Throwable e) {
                DebugInfo.error("Parse plugin info failed", e);
            }
        }
    }
}

