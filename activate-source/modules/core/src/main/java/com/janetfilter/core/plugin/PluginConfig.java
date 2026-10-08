/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.plugin;

import com.janetfilter.core.models.FilterRule;
import java.io.File;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public class PluginConfig {
    private final File file;
    private final Map<String, List<FilterRule>> data;

    public PluginConfig(File file, Map<String, List<FilterRule>> data) {
        this.file = file;
        this.data = data;
    }

    public List<FilterRule> getBySection(String section) {
        return this.data.getOrDefault(section, new ArrayList());
    }

    public File getFile() {
        return this.file;
    }

    public Map<String, List<FilterRule>> getData() {
        return this.data;
    }
}

