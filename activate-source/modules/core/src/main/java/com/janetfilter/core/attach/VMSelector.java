/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.attach;

import com.janetfilter.core.attach.VMDescriptor;
import com.janetfilter.core.attach.VMLauncher;
import com.janetfilter.core.utils.DateUtils;
import com.janetfilter.core.utils.ProcessUtils;
import com.janetfilter.core.utils.WhereIsUtils;
import java.io.BufferedReader;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.stream.Collectors;

public class VMSelector {
    private final File thisJar;
    private List<VMDescriptor> descriptors;

    public VMSelector(File thisJar) {
        this.thisJar = thisJar;
    }

    private List<VMDescriptor> getVMList() throws Exception {
        String line;
        File jpsCommand = WhereIsUtils.findJPS();
        if (null == jpsCommand) {
            throw new Exception("jps command not found");
        }
        ArrayList<String> list = new ArrayList<String>();
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        ProcessUtils.start(new ProcessBuilder(jpsCommand.getAbsolutePath(), "-lv"), bos);
        BufferedReader reader = new BufferedReader(new InputStreamReader(new ByteArrayInputStream(bos.toByteArray())));
        while ((line = reader.readLine()) != null) {
            list.add(line);
        }
        String processId = ProcessUtils.currentId();
        return list.stream().map(s -> {
            String[] section = (s + "   ").split(" ", 3);
            return new VMDescriptor(section[0].trim(), section[1].trim(), section[2].trim());
        }).filter(d -> !d.getId().equals(processId) && !"sun.tools.jps.Jps".equals(d.getClassName()) && !"jdk.jcmd/sun.tools.jps.Jps".equals(d.getClassName())).sorted(Comparator.comparingInt(d -> Integer.parseInt(d.getId()))).collect(Collectors.toList());
    }

    private String getInput() throws IOException {
        return new BufferedReader(new InputStreamReader(System.in)).readLine().trim();
    }

    private void processSelect() throws Exception {
        int index;
        String input;
        System.out.print("  Select: ");
        switch (input = this.getInput()) {
            case "Q": 
            case "q": {
                System.exit(0);
            }
            case "R": 
            case "r": {
                System.out.println("  =========================== " + DateUtils.formatDateTime() + " ============================");
                this.select();
                return;
            }
            case "": {
                this.processSelect();
                return;
            }
        }
        try {
            index = Integer.parseInt(input);
        }
        catch (NumberFormatException e) {
            this.invalidInput(input);
            return;
        }
        if (index < 1) {
            this.invalidInput(input);
            return;
        }
        if (index > this.descriptors.size()) {
            this.invalidInput(input);
            return;
        }
        System.out.print("  Agent args: ");
        input = this.getInput();
        try {
            VMLauncher.launch(this.thisJar, this.descriptors.get(index - 1), input);
        }
        catch (Exception e) {
            System.err.println("> Attach to: " + index + " failed.");
            e.printStackTrace(System.err);
            return;
        }
    }

    private void invalidInput(String input) throws Exception {
        System.err.println("> Invalid input: " + input);
        this.processSelect();
    }

    public void select() throws Exception {
        boolean first = null == this.descriptors;
        List<VMDescriptor> temp = this.getVMList();
        if (null != this.descriptors && !this.descriptors.isEmpty()) {
            temp.forEach(d -> d.setOld(this.descriptors.stream().anyMatch(d1 -> d.getId().equals(d1.getId()))));
        }
        this.descriptors = temp;
        System.out.println("  Java Virtual Machine List: (Select and attach" + (first ? "" : ", + means the new one") + ")");
        int index = 1;
        for (VMDescriptor d2 : this.descriptors) {
            System.out.printf("  %3d]:%s%s %s%n", index++, d2.getOld() != false ? " " : "+", d2.getId(), d2.getClassName());
        }
        System.out.println("    r]: <Refresh virtual machine list>");
        System.out.println("    q]: <Quit the ja-netfilter>");
        this.processSelect();
    }
}

