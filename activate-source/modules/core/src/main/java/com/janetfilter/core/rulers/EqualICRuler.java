/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.rulers;

import com.janetfilter.core.rulers.Ruler;

public class EqualICRuler
implements Ruler {
    @Override
    public boolean test(String rule, String content) {
        return content.equalsIgnoreCase(rule);
    }
}

