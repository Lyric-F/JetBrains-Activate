/*
 * Decompiled with CFR 0.152.
 * 
 * Could not load the following classes:
 *  jdk.internal.org.objectweb.asm.ClassReader
 *  jdk.internal.org.objectweb.asm.ClassVisitor
 *  jdk.internal.org.objectweb.asm.ClassWriter
 *  jdk.internal.org.objectweb.asm.tree.AbstractInsnNode
 *  jdk.internal.org.objectweb.asm.tree.ClassNode
 *  jdk.internal.org.objectweb.asm.tree.InsnList
 *  jdk.internal.org.objectweb.asm.tree.MethodInsnNode
 *  jdk.internal.org.objectweb.asm.tree.MethodNode
 *  jdk.internal.org.objectweb.asm.tree.VarInsnNode
 */
package com.janetfilter.plugins.power;

import com.janetfilter.core.models.FilterRule;
import com.janetfilter.core.plugin.MyTransformer;
import java.util.List;
import jdk.internal.org.objectweb.asm.ClassReader;
import jdk.internal.org.objectweb.asm.ClassVisitor;
import jdk.internal.org.objectweb.asm.ClassWriter;
import jdk.internal.org.objectweb.asm.tree.AbstractInsnNode;
import jdk.internal.org.objectweb.asm.tree.ClassNode;
import jdk.internal.org.objectweb.asm.tree.InsnList;
import jdk.internal.org.objectweb.asm.tree.MethodInsnNode;
import jdk.internal.org.objectweb.asm.tree.MethodNode;
import jdk.internal.org.objectweb.asm.tree.VarInsnNode;

public class GoForFreeTransformer
implements MyTransformer {
    public GoForFreeTransformer(List<FilterRule> rules) {
    }

    @Override
    public String getHookClassName() {
        return "java/text/SimpleDateFormat";
    }

    @Override
    public byte[] transform(String className, byte[] classBytes, int order) throws Exception {
        System.out.println("your yd=lsl, ----------------");
        ClassReader reader = new ClassReader(classBytes);
        ClassNode node = new ClassNode(327680);
        reader.accept((ClassVisitor)node, 0);
        for (MethodNode mn : node.methods) {
            if (!"parse".equals(mn.name)) continue;
            System.out.println(mn.desc);
            InsnList list = new InsnList();
            list.add((AbstractInsnNode)new VarInsnNode(25, 1));
            list.add((AbstractInsnNode)new MethodInsnNode(184, "com/janetfilter/plugins/power/GoForFreeFilter", "testFilter", "(Ljava/lang/String;)Ljava/lang/String;", false));
            list.add((AbstractInsnNode)new VarInsnNode(58, 1));
            mn.instructions.insert(list);
        }
        ClassWriter writer = new ClassWriter(3);
        node.accept((ClassVisitor)writer);
        return writer.toByteArray();
    }
}

