/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.plugins.hideme;

import com.janetfilter.core.Environment;
import java.io.File;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Iterator;
import java.util.List;

public class VmArgumentFilter {
    private static Environment environment;

    public static void setEnvironment(Environment env) {
        environment = env;
    }

    public static List<String> testArgs(List<String> vmArgs) {
        boolean modified = false;
        ArrayList<String> list = new ArrayList<String>(vmArgs);
        Iterator it = list.iterator();
        while (it.hasNext()) {
            String[] sections;
            String arg = (String)it.next();
            if (arg.startsWith("-Djanf.debug=")) {
                it.remove();
                modified = true;
                continue;
            }
            if (!arg.startsWith("-javaagent:") || (sections = arg.substring(11).split("=", 2)).length < 1) continue;
            File f = new File(sections[0].toLowerCase());
            File d = new File(environment.getAgentFile().getPath().toLowerCase());
            if (!d.equals(f)) continue;
            it.remove();
            modified = true;
        }
        return modified ? Collections.unmodifiableList(list) : vmArgs;
    }
}

