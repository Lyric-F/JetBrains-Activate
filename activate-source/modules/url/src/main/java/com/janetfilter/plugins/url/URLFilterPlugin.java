/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.plugins.url;

import com.janetfilter.core.Environment;
import com.janetfilter.core.plugin.MyTransformer;
import com.janetfilter.core.plugin.PluginConfig;
import com.janetfilter.core.plugin.PluginEntry;
import com.janetfilter.plugins.url.HttpClientTransformer;
import java.util.ArrayList;
import java.util.List;

public class URLFilterPlugin
implements PluginEntry {
    private static final String PLUGIN_NAME = "URL";
    private final List<MyTransformer> transformers = new ArrayList<MyTransformer>();

    @Override
    public void init(Environment environment, PluginConfig config) {
        this.transformers.add(new HttpClientTransformer(config.getBySection(PLUGIN_NAME)));
    }

    @Override
    public String getName() {
        return PLUGIN_NAME;
    }

    @Override
    public String getAuthor() {
        return "neo";
    }

    @Override
    public String getVersion() {
        return "v1.1.0";
    }

    @Override
    public String getDescription() {
        return "A plugin for the ja-netfilter, it can block http requests.";
    }

    @Override
    public List<MyTransformer> getTransformers() {
        return this.transformers;
    }
}

