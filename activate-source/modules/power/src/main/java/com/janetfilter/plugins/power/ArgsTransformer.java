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
 *  jdk.internal.org.objectweb.asm.tree.InsnNode
 *  jdk.internal.org.objectweb.asm.tree.JumpInsnNode
 *  jdk.internal.org.objectweb.asm.tree.LabelNode
 *  jdk.internal.org.objectweb.asm.tree.MethodInsnNode
 *  jdk.internal.org.objectweb.asm.tree.MethodNode
 *  jdk.internal.org.objectweb.asm.tree.VarInsnNode
 */
package com.janetfilter.plugins.power;

import com.janetfilter.core.models.FilterRule;
import com.janetfilter.core.plugin.MyTransformer;
import com.janetfilter.plugins.power.ArgsFilter;
import java.util.List;
import jdk.internal.org.objectweb.asm.ClassReader;
import jdk.internal.org.objectweb.asm.ClassVisitor;
import jdk.internal.org.objectweb.asm.ClassWriter;
import jdk.internal.org.objectweb.asm.tree.AbstractInsnNode;
import jdk.internal.org.objectweb.asm.tree.ClassNode;
import jdk.internal.org.objectweb.asm.tree.InsnList;
import jdk.internal.org.objectweb.asm.tree.InsnNode;
import jdk.internal.org.objectweb.asm.tree.JumpInsnNode;
import jdk.internal.org.objectweb.asm.tree.LabelNode;
import jdk.internal.org.objectweb.asm.tree.MethodInsnNode;
import jdk.internal.org.objectweb.asm.tree.MethodNode;
import jdk.internal.org.objectweb.asm.tree.VarInsnNode;

public class ArgsTransformer
implements MyTransformer {
    public ArgsTransformer(List<FilterRule> rules) {
        ArgsFilter.setRules(rules);
    }

    @Override
    public String getHookClassName() {
        return "java/math/BigInteger";
    }

    @Override
    public byte[] transform(String className, byte[] classBytes, int order) throws Exception {
        ClassReader reader = new ClassReader(classBytes);
        ClassNode node = new ClassNode(327680);
        reader.accept((ClassVisitor)node, 0);
        for (MethodNode mn : node.methods) {
            if (!"oddModPow".equals(mn.name) || !"(Ljava/math/BigInteger;Ljava/math/BigInteger;)Ljava/math/BigInteger;".equals(mn.desc)) continue;
            InsnList list = new InsnList();
            list.add((AbstractInsnNode)new VarInsnNode(25, 0));
            list.add((AbstractInsnNode)new VarInsnNode(25, 1));
            list.add((AbstractInsnNode)new VarInsnNode(25, 2));
            list.add((AbstractInsnNode)new MethodInsnNode(184, "com/janetfilter/plugins/power/ArgsFilter", "testFilter", "(Ljava/math/BigInteger;Ljava/math/BigInteger;Ljava/math/BigInteger;)[Ljava/math/BigInteger;", false));
            list.add((AbstractInsnNode)new VarInsnNode(58, 3));
            list.add((AbstractInsnNode)new InsnNode(1));
            list.add((AbstractInsnNode)new VarInsnNode(25, 3));
            LabelNode label0 = new LabelNode();
            list.add((AbstractInsnNode)new JumpInsnNode(165, label0));
            list.add((AbstractInsnNode)new VarInsnNode(25, 3));
            list.add((AbstractInsnNode)new InsnNode(3));
            list.add((AbstractInsnNode)new InsnNode(50));
            list.add((AbstractInsnNode)new VarInsnNode(58, 1));
            list.add((AbstractInsnNode)new VarInsnNode(25, 3));
            list.add((AbstractInsnNode)new InsnNode(4));
            list.add((AbstractInsnNode)new InsnNode(50));
            list.add((AbstractInsnNode)new VarInsnNode(58, 2));
            list.add((AbstractInsnNode)label0);
            mn.instructions.insert(list);
        }
        ClassWriter writer = new ClassWriter(3);
        node.accept((ClassVisitor)writer);
        return writer.toByteArray();
    }
}

