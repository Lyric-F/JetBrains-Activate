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
package com.janetfilter.plugins.dns;

import com.janetfilter.core.models.FilterRule;
import com.janetfilter.core.plugin.MyTransformer;
import com.janetfilter.plugins.dns.DNSFilter;
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

public class InetAddressTransformer
implements MyTransformer {
    private final List<FilterRule> rules;

    public InetAddressTransformer(List<FilterRule> rules) {
        this.rules = rules;
    }

    @Override
    public String getHookClassName() {
        return "java/net/InetAddress";
    }

    @Override
    public byte[] transform(String className, byte[] classBytes, int order) throws Exception {
        DNSFilter.setRules(this.rules);
        ClassReader reader = new ClassReader(classBytes);
        ClassNode node = new ClassNode(327680);
        reader.accept((ClassVisitor)node, 0);
        for (MethodNode m : node.methods) {
            InsnList list;
            if ("getAllByName".equals(m.name) && "(Ljava/lang/String;Ljava/net/InetAddress;)[Ljava/net/InetAddress;".equals(m.desc)) {
                list = new InsnList();
                list.add((AbstractInsnNode)new VarInsnNode(25, 0));
                list.add((AbstractInsnNode)new MethodInsnNode(184, "com/janetfilter/plugins/dns/DNSFilter", "testQuery", "(Ljava/lang/String;)Ljava/lang/String;", false));
                list.add((AbstractInsnNode)new InsnNode(87));
                m.instructions.insert(list);
                continue;
            }
            if (!"isReachable".equals(m.name) || !"(Ljava/net/NetworkInterface;II)Z".equals(m.desc)) continue;
            list = new InsnList();
            list.add((AbstractInsnNode)new VarInsnNode(25, 0));
            list.add((AbstractInsnNode)new MethodInsnNode(184, "com/janetfilter/plugins/dns/DNSFilter", "testReachable", "(Ljava/net/InetAddress;)Ljava/lang/Object;", false));
            list.add((AbstractInsnNode)new VarInsnNode(58, 4));
            list.add((AbstractInsnNode)new InsnNode(1));
            list.add((AbstractInsnNode)new VarInsnNode(25, 4));
            LabelNode label1 = new LabelNode();
            list.add((AbstractInsnNode)new JumpInsnNode(165, label1));
            list.add((AbstractInsnNode)new InsnNode(3));
            list.add((AbstractInsnNode)new InsnNode(172));
            list.add((AbstractInsnNode)label1);
            m.instructions.insert(list);
        }
        ClassWriter writer = new ClassWriter(3);
        node.accept((ClassVisitor)writer);
        return writer.toByteArray();
    }
}

