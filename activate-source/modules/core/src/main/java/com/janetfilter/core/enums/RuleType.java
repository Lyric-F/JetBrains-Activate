/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.enums;

import com.janetfilter.core.rulers.EqualICRuler;
import com.janetfilter.core.rulers.EqualRuler;
import com.janetfilter.core.rulers.KeywordICRuler;
import com.janetfilter.core.rulers.KeywordRuler;
import com.janetfilter.core.rulers.PrefixICRuler;
import com.janetfilter.core.rulers.PrefixRuler;
import com.janetfilter.core.rulers.RegExpRuler;
import com.janetfilter.core.rulers.Ruler;
import com.janetfilter.core.rulers.SuffixICRuler;
import com.janetfilter.core.rulers.SuffixRuler;

public enum RuleType {
    PREFIX(new PrefixRuler()),
    PREFIX_IC(new PrefixICRuler()),
    SUFFIX(new SuffixRuler()),
    SUFFIX_IC(new SuffixICRuler()),
    KEYWORD(new KeywordRuler()),
    KEYWORD_IC(new KeywordICRuler()),
    EQUAL(new EqualRuler()),
    EQUAL_IC(new EqualICRuler()),
    REGEXP(new RegExpRuler());

    private final Ruler ruler;

    private RuleType(Ruler ruler) {
        this.ruler = ruler;
    }

    public Ruler getRuler() {
        return this.ruler;
    }
}

