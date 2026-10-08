/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.utils;

import com.janetfilter.core.Launcher;
import java.io.File;
import java.io.IOException;
import java.net.URI;
import java.net.URL;

public class WhereIsUtils {
    private static final String JAVA_HOME = System.getProperty("java.home");

    public static File findJPS() {
        String[] paths;
        for (String path : paths = new String[]{"bin/jps", "bin/jps.exe", "../bin/jps", "../bin/jps.exe"}) {
            File file = new File(JAVA_HOME, path);
            if (!file.exists() || !file.isFile() || !file.canExecute()) continue;
            return WhereIsUtils.getCanonicalFile(file);
        }
        return null;
    }

    public static File findJava() {
        String[] paths;
        for (String path : paths = new String[]{"bin/java", "bin/java.exe", "../bin/java", "../bin/java.exe"}) {
            File file = new File(JAVA_HOME, path);
            if (!file.exists() || !file.isFile() || !file.canExecute()) continue;
            return WhereIsUtils.getCanonicalFile(file);
        }
        return null;
    }

    public static File findToolsJar() {
        String[] paths;
        for (String path : paths = new String[]{"lib/tools.jar", "../lib/tools.jar", "../../lib/tools.jar"}) {
            File file = new File(JAVA_HOME, path);
            if (!file.exists() || !file.isFile()) continue;
            return WhereIsUtils.getCanonicalFile(file);
        }
        return null;
    }

    public static URI getJarURI() throws Exception {
        URL url = Launcher.class.getProtectionDomain().getCodeSource().getLocation();
        if (null != url) {
            return url.toURI();
        }
        String resourcePath = "/6c81ec87e55d331c267262e892427a3d93d76683.txt";
        url = Launcher.class.getResource(resourcePath);
        if (null == url) {
            throw new Exception("Can not locate resource file.");
        }
        String path = url.getPath();
        if (!path.endsWith("!" + resourcePath)) {
            throw new Exception("Invalid resource path.");
        }
        path = path.substring(0, path.length() - resourcePath.length() - 1);
        return new URI(path);
    }

    private static File getCanonicalFile(File file) {
        try {
            return file.getCanonicalFile();
        }
        catch (IOException e) {
            return null;
        }
    }
}

