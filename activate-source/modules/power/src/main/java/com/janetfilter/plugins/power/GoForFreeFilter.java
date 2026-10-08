/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.plugins.power;

public class GoForFreeFilter {
    public static String testFilter(String encode) {
        if (encode.equals("2023-07-01")) {
            return "2099-12-31";
        }
        return encode;
    }
}

