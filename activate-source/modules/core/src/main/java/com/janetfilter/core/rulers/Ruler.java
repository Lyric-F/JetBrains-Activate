/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.rulers;

public interface Ruler {
    default public boolean test(String rule, String content) {
        return false;
    }
}

