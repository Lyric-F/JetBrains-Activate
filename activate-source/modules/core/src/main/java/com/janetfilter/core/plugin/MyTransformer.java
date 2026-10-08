/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.plugin;

import java.security.ProtectionDomain;

public interface MyTransformer {
    public String getHookClassName();

    default public boolean attachMode() {
        return true;
    }

    default public boolean javaagentMode() {
        return true;
    }

    default public boolean isManager() {
        return false;
    }

    default public void before(ClassLoader loader, Class<?> classBeingRedefined, ProtectionDomain protectionDomain, String className, byte[] classBytes) throws Exception {
        this.before(className, classBytes);
    }

    default public void before(String className, byte[] classBytes) throws Exception {
    }

    default public byte[] preTransform(ClassLoader loader, Class<?> classBeingRedefined, ProtectionDomain protectionDomain, String className, byte[] classBytes, int order) throws Exception {
        return this.preTransform(className, classBytes, order);
    }

    default public byte[] preTransform(String className, byte[] classBytes, int order) throws Exception {
        return this.transform(className, classBytes, order);
    }

    default public byte[] transform(ClassLoader loader, Class<?> classBeingRedefined, ProtectionDomain protectionDomain, String className, byte[] classBytes, int order) throws Exception {
        return this.transform(className, classBytes, order);
    }

    default public byte[] transform(String className, byte[] classBytes, int order) throws Exception {
        return classBytes;
    }

    default public byte[] postTransform(ClassLoader loader, Class<?> classBeingRedefined, ProtectionDomain protectionDomain, String className, byte[] classBytes, int order) throws Exception {
        return this.postTransform(className, classBytes, order);
    }

    default public byte[] postTransform(String className, byte[] classBytes, int order) throws Exception {
        return classBytes;
    }

    default public void after(ClassLoader loader, Class<?> classBeingRedefined, ProtectionDomain protectionDomain, String className, byte[] classBytes) throws Exception {
        this.after(className, classBytes);
    }

    default public void after(String className, byte[] classBytes) throws Exception {
    }
}

