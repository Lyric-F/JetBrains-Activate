/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.utils;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.lang.management.ManagementFactory;
import java.util.ArrayList;

public class ProcessUtils {
    private static String processId;

    public static synchronized String currentId() {
        if (null == processId) {
            String name = ManagementFactory.getRuntimeMXBean().getName() + "@";
            processId = name.split("@", 2)[0];
        }
        return processId;
    }

    public static int start(ProcessBuilder pb) throws Exception {
        return ProcessUtils.start(pb, System.out, System.err);
    }

    public static int start(ProcessBuilder pb, OutputStream out) throws Exception {
        return ProcessUtils.start(pb, out, null);
    }

    public static int start(ProcessBuilder pb, OutputStream out, OutputStream err) throws Exception {
        Process p = pb.start();
        ArrayList<Thread> threads = new ArrayList<Thread>();
        if (null != out) {
            threads.add(new Thread(new RedirectOutput(p.getInputStream(), out)));
        }
        if (null != err) {
            threads.add(new Thread(new RedirectOutput(p.getErrorStream(), err)));
        }
        for (Thread thread : threads) {
            thread.start();
        }
        for (Thread thread : threads) {
            thread.join();
        }
        return p.waitFor();
    }

    static class RedirectOutput
    implements Runnable {
        private static final int BUFF_SIZE = 1024;
        private final InputStream origin;
        private final OutputStream dest;

        RedirectOutput(InputStream origin, OutputStream dest) {
            this.origin = origin;
            this.dest = dest;
        }

        @Override
        public void run() {
            byte[] buffer = new byte[1024];
            try {
                int length;
                while ((length = this.origin.read(buffer)) != -1) {
                    this.dest.write(buffer, 0, length);
                }
            }
            catch (IOException e) {
                throw new RuntimeException("ERROR: Redirect output failed.", e);
            }
        }
    }
}

