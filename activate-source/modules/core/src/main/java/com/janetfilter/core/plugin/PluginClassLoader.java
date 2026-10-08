/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.plugin;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.util.jar.JarFile;
import java.util.zip.ZipEntry;

public final class PluginClassLoader
extends ClassLoader {
    private final JarFile jarFile;

    public PluginClassLoader(JarFile jarFile) {
        this.jarFile = jarFile;
    }

    @Override
    public Class<?> findClass(String name) throws ClassNotFoundException {
        byte[] bytes = this.loadClassFromFile(name);
        return this.defineClass(name, bytes, 0, bytes.length);
    }

    private byte[] loadClassFromFile(String fileName) throws ClassNotFoundException {
        String classFile = fileName.replace('.', '/') + ".class";
        ZipEntry entry = this.jarFile.getEntry(classFile);
        if (null == entry) {
            throw new ClassNotFoundException("Class not found: " + fileName);
        }
        byte[] buffer = new byte[1024];
        ByteArrayOutputStream byteStream = new ByteArrayOutputStream();
        try (InputStream is = this.jarFile.getInputStream(entry);){
            int length;
            while (-1 != (length = is.read(buffer))) {
                byteStream.write(buffer, 0, length);
            }
        }
        catch (IOException e) {
            throw new ClassNotFoundException("Can't access class: " + fileName, e);
        }
        return byteStream.toByteArray();
    }
}

