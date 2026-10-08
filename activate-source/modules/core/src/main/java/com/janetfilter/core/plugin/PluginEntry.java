/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.plugin;

import com.janetfilter.core.Environment;
import com.janetfilter.core.plugin.MyTransformer;
import com.janetfilter.core.plugin.PluginConfig;
import java.util.List;

public interface PluginEntry {
    default public void init(Environment environment, PluginConfig config) {
    }

    public String getName();

    public String getAuthor();

    default public String getVersion() {
        return "v1.0.0";
    }

    default public String getDescription() {
        return "A ja-netfilter plugin.";
    }

    public List<MyTransformer> getTransformers();
}

