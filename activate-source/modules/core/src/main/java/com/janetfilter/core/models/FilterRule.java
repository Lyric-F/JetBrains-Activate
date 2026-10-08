/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.models;

import com.janetfilter.core.enums.RuleType;
import java.util.HashMap;
import java.util.Map;

public class FilterRule {
    private static final Map<String, RuleType> SUPPORTED_TYPE_MAP = new HashMap<String, RuleType>();
    private RuleType type;
    private String rule;

    public FilterRule(RuleType type, String rule) {
        this.type = type;
        this.rule = rule;
    }

    public static FilterRule of(String typeStr, String content) {
        RuleType type = SUPPORTED_TYPE_MAP.get(typeStr.toUpperCase());
        if (null == type) {
            return null;
        }
        return new FilterRule(type, content);
    }

    public RuleType getType() {
        return this.type;
    }

    public void setType(RuleType type) {
        this.type = type;
    }

    public String getRule() {
        return this.rule;
    }

    public void setRule(String rule) {
        this.rule = rule;
    }

    public boolean test(String content) {
        return this.type.getRuler().test(this.rule, content);
    }

    public String toString() {
        return "{type=" + (Object)((Object)this.type) + ", rule=" + this.rule + "}";
    }

    static {
        for (RuleType ruleType : RuleType.values()) {
            SUPPORTED_TYPE_MAP.put(ruleType.name(), ruleType);
        }
    }
}

