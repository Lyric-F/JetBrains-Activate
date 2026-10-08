/*
 * Decompiled with CFR 0.152.
 * 
 * Could not load the following classes:
 *  jdk.internal.org.objectweb.asm.ClassReader
 *  jdk.internal.org.objectweb.asm.ClassVisitor
 *  jdk.internal.org.objectweb.asm.ClassWriter
 *  jdk.internal.org.objectweb.asm.tree.AbstractInsnNode
 *  jdk.internal.org.objectweb.asm.tree.ClassNode
 *  jdk.internal.org.objectweb.asm.tree.FieldInsnNode
 *  jdk.internal.org.objectweb.asm.tree.InsnList
 *  jdk.internal.org.objectweb.asm.tree.InsnNode
 *  jdk.internal.org.objectweb.asm.tree.MethodInsnNode
 *  jdk.internal.org.objectweb.asm.tree.MethodNode
 *  jdk.internal.org.objectweb.asm.tree.VarInsnNode
 */
package com.janetfilter.plugins.url;

import com.janetfilter.core.models.FilterRule;
import com.janetfilter.core.plugin.MyTransformer;
import com.janetfilter.plugins.url.URLFilter;
import java.util.List;
import jdk.internal.org.objectweb.asm.ClassReader;
import jdk.internal.org.objectweb.asm.ClassVisitor;
import jdk.internal.org.objectweb.asm.ClassWriter;
import jdk.internal.org.objectweb.asm.tree.AbstractInsnNode;
import jdk.internal.org.objectweb.asm.tree.ClassNode;
import jdk.internal.org.objectweb.asm.tree.FieldInsnNode;
import jdk.internal.org.objectweb.asm.tree.InsnList;
import jdk.internal.org.objectweb.asm.tree.InsnNode;
import jdk.internal.org.objectweb.asm.tree.MethodInsnNode;
import jdk.internal.org.objectweb.asm.tree.MethodNode;
import jdk.internal.org.objectweb.asm.tree.VarInsnNode;

public class HttpClientTransformer
implements MyTransformer {
    private final List<FilterRule> rules;

    public HttpClientTransformer(List<FilterRule> rules) {
        this.rules = rules;
    }

    @Override
    public String getHookClassName() {
        return "sun/net/www/http/HttpClient";
    }

    @Override
    public byte[] transform(String className, byte[] classBytes, int order) throws Exception {
        URLFilter.setRules(this.rules);
        ClassReader reader = new ClassReader(classBytes);
        ClassNode node = new ClassNode(327680);
        reader.accept((ClassVisitor)node, 0);
        for (MethodNode mn : node.methods) {
            if (!"openServer".equals(mn.name) || !"()V".equals(mn.desc)) continue;
            InsnList list = new InsnList();
            list.add((AbstractInsnNode)new VarInsnNode(25, 0));
            list.add((AbstractInsnNode)new FieldInsnNode(180, "sun/net/www/http/HttpClient", "url", "Ljava/net/URL;"));
            list.add((AbstractInsnNode)new MethodInsnNode(184, "com/janetfilter/plugins/url/URLFilter", "testURL", "(Ljava/net/URL;)Ljava/net/URL;", false));
            list.add((AbstractInsnNode)new InsnNode(87));
            mn.instructions.insert(list);
        }
        ClassWriter writer = new ClassWriter(3);
        node.accept((ClassVisitor)writer);
        return writer.toByteArray();
    }
}

