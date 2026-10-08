/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core;

import com.janetfilter.core.Environment;
import com.janetfilter.core.commons.DebugInfo;
import com.janetfilter.core.plugin.MyTransformer;
import java.lang.instrument.ClassFileTransformer;
import java.lang.instrument.IllegalClassFormatException;
import java.security.ProtectionDomain;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.TreeSet;

public final class Dispatcher
implements ClassFileTransformer {
    private final Environment environment;
    private final Set<String> classSet = new TreeSet<String>();
    private final Map<String, List<MyTransformer>> transformerMap = new HashMap<String, List<MyTransformer>>();
    private final List<MyTransformer> globalTransformers = new ArrayList<MyTransformer>();
    private final List<MyTransformer> manageTransformers = new ArrayList<MyTransformer>();

    public Dispatcher(Environment environment) {
        this.environment = environment;
    }

    /*
     * WARNING - Removed try catching itself - possible behaviour change.
     */
    public void addTransformer(MyTransformer transformer) {
        if (null == transformer) {
            return;
        }
        if (this.environment.isAttachMode() && !transformer.attachMode()) {
            DebugInfo.debug("Transformer: " + transformer.getClass().getName() + " is set to not load in attach mode, ignored.");
            return;
        }
        if (this.environment.isJavaagentMode() && !transformer.javaagentMode()) {
            DebugInfo.debug("Transformer: " + transformer.getClass().getName() + " is set to not load in -javaagent mode, ignored.");
            return;
        }
        Dispatcher dispatcher = this;
        synchronized (dispatcher) {
            String className = transformer.getHookClassName();
            if (null == className) {
                this.globalTransformers.add(transformer);
                if (transformer.isManager()) {
                    this.manageTransformers.add(transformer);
                }
                return;
            }
            this.classSet.add(className.replace('/', '.'));
            List transformers = this.transformerMap.computeIfAbsent(className, k -> new ArrayList());
            transformers.add(transformer);
        }
    }

    public void addTransformers(List<MyTransformer> transformers) {
        if (null == transformers) {
            return;
        }
        for (MyTransformer transformer : transformers) {
            this.addTransformer(transformer);
        }
    }

    public void addTransformers(MyTransformer[] transformers) {
        if (null == transformers) {
            return;
        }
        this.addTransformers(Arrays.asList(transformers));
    }

    public Set<String> getHookClassNames() {
        return this.classSet;
    }

    @Override
    public byte[] transform(ClassLoader loader, String className, Class<?> classBeingRedefined, ProtectionDomain protectionDomain, byte[] classFileBuffer) throws IllegalClassFormatException {
        if (null == className) {
            return classFileBuffer;
        }
        List<MyTransformer> transformers = this.transformerMap.get(className);
        List<MyTransformer> globalTransformers = null == transformers ? this.manageTransformers : this.globalTransformers;
        int order = 0;
        try {
            for (MyTransformer transformer : globalTransformers) {
                transformer.before(loader, classBeingRedefined, protectionDomain, className, classFileBuffer);
            }
            for (MyTransformer transformer : globalTransformers) {
                classFileBuffer = transformer.preTransform(loader, classBeingRedefined, protectionDomain, className, classFileBuffer, order++);
            }
            if (null != transformers) {
                for (MyTransformer transformer : transformers) {
                    classFileBuffer = transformer.transform(loader, classBeingRedefined, protectionDomain, className, classFileBuffer, order++);
                }
            }
            for (MyTransformer transformer : globalTransformers) {
                classFileBuffer = transformer.postTransform(loader, classBeingRedefined, protectionDomain, className, classFileBuffer, order++);
            }
            for (MyTransformer transformer : globalTransformers) {
                transformer.after(loader, classBeingRedefined, protectionDomain, className, classFileBuffer);
            }
        }
        catch (Throwable e) {
            DebugInfo.error("Transform class failed: " + className, e);
        }
        return classFileBuffer;
    }
}

