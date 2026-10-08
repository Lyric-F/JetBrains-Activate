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
 *  jdk.internal.org.objectweb.asm.tree.MethodInsnNode
 *  jdk.internal.org.objectweb.asm.tree.MethodNode
 *  jdk.internal.org.objectweb.asm.tree.VarInsnNode
 */
package com.janetfilter.plugins.hideme;

import com.janetfilter.core.Environment;
import com.janetfilter.core.plugin.MyTransformer;
import com.janetfilter.plugins.hideme.VmArgumentFilter;
import jdk.internal.org.objectweb.asm.ClassReader;
import jdk.internal.org.objectweb.asm.ClassVisitor;
import jdk.internal.org.objectweb.asm.ClassWriter;
import jdk.internal.org.objectweb.asm.tree.AbstractInsnNode;
import jdk.internal.org.objectweb.asm.tree.ClassNode;
import jdk.internal.org.objectweb.asm.tree.FieldInsnNode;
import jdk.internal.org.objectweb.asm.tree.InsnList;
import jdk.internal.org.objectweb.asm.tree.MethodInsnNode;
import jdk.internal.org.objectweb.asm.tree.MethodNode;
import jdk.internal.org.objectweb.asm.tree.VarInsnNode;

public class VMTransformer
implements MyTransformer {
    private final Environment environment;

    public VMTransformer(Environment environment) {
        this.environment = environment;
    }

    @Override
    public String getHookClassName() {
        return "sun/management/VMManagementImpl";
    }

    @Override
    public byte[] transform(String className, byte[] classBytes, int order) throws Exception {
        VmArgumentFilter.setEnvironment(this.environment);
        ClassReader reader = new ClassReader(classBytes);
        ClassNode node = new ClassNode(327680);
        reader.accept((ClassVisitor)node, 0);
        block0: for (MethodNode mn : node.methods) {
            if (!"getVmArguments".equals(mn.name) || !"()Ljava/util/List;".equals(mn.desc)) continue;
            InsnList list = new InsnList();
            list.add((AbstractInsnNode)new VarInsnNode(25, 0));
            list.add((AbstractInsnNode)new VarInsnNode(25, 0));
            list.add((AbstractInsnNode)new FieldInsnNode(180, "sun/management/VMManagementImpl", "vmArgs", "Ljava/util/List;"));
            list.add((AbstractInsnNode)new MethodInsnNode(184, "com/janetfilter/plugins/hideme/VmArgumentFilter", "testArgs", "(Ljava/util/List;)Ljava/util/List;", false));
            list.add((AbstractInsnNode)new FieldInsnNode(181, "sun/management/VMManagementImpl", "vmArgs", "Ljava/util/List;"));
            for (AbstractInsnNode in : mn.instructions) {
                if (0 != in.getType() || 176 != in.getOpcode()) continue;
                mn.instructions.insert(in.getPrevious().getPrevious(), list);
                continue block0;
            }
        }
        ClassWriter writer = new ClassWriter(3);
        node.accept((ClassVisitor)writer);
        return writer.toByteArray();
    }
}

