/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.commons;

import com.janetfilter.core.utils.DateUtils;
import com.janetfilter.core.utils.ProcessUtils;
import com.janetfilter.core.utils.StringUtils;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.PrintStream;
import java.util.Date;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public class DebugInfo {
    public static final long OUTPUT_CONSOLE = 1L;
    public static final long OUTPUT_FILE = 2L;
    public static final long OUTPUT_WITH_PID = 4L;
    private static final ExecutorService CONSOLE_EXECUTOR = Executors.newSingleThreadExecutor();
    private static final ExecutorService FILE_EXECUTOR = Executors.newSingleThreadExecutor();
    private static final String CLASS_NAME = DebugInfo.class.getName();
    private static final String LOG_TEMPLATE = "%s %-5s [%s@%-5s] %s-%d : %s%n";
    private static final String PID = ProcessUtils.currentId();
    private static final Level LOG_LEVEL;
    private static final Long LOG_OUTPUT;
    private static File logDir;

    public static void useFile(File dir) {
        if (Level.NONE == LOG_LEVEL || 0L == (LOG_OUTPUT & 2L) || null == dir) {
            return;
        }
        if (!dir.exists() && !dir.mkdirs()) {
            DebugInfo.error("Can't make directory: " + dir);
            return;
        }
        if (!dir.isDirectory()) {
            DebugInfo.error("It's not a directory: " + dir);
            return;
        }
        if (!dir.canWrite()) {
            DebugInfo.error("Read-only directory: " + dir);
            return;
        }
        logDir = dir;
    }

    public static Level getLogLevel() {
        return LOG_LEVEL;
    }

    public static long getLogOutput() {
        return LOG_OUTPUT;
    }

    public static void debug(String content, Throwable e) {
        DebugInfo.output(Level.DEBUG, content, e);
    }

    public static void debug(String content) {
        DebugInfo.debug(content, null);
    }

    public static void info(String content, Throwable e) {
        DebugInfo.output(Level.INFO, content, e);
    }

    public static void info(String content) {
        DebugInfo.info(content, null);
    }

    public static void warn(String content, Throwable e) {
        DebugInfo.output(Level.WARN, content, e);
    }

    public static void warn(String content) {
        DebugInfo.warn(content, null);
    }

    public static void error(String content, Throwable e) {
        DebugInfo.output(Level.ERROR, content, e);
    }

    public static void error(String content) {
        DebugInfo.error(content, null);
    }

    public static void output(String content) {
        DebugInfo.debug(content);
    }

    public static void output(String content, Throwable e) {
        DebugInfo.debug(content, e);
    }

    public static void output(Level level, String content, Throwable e) {
        if (Level.NONE == LOG_LEVEL || level.ordinal() < LOG_LEVEL.ordinal()) {
            return;
        }
        if (0L != (LOG_OUTPUT & 1L)) {
            CONSOLE_EXECUTOR.execute(new ConsoleWriteTask(PID, level, content, e));
        }
        if (null != logDir) {
            FILE_EXECUTOR.execute(new FileWriteTask(logDir, PID, level, content, e));
        }
    }

    static {
        Level level = Level.of(System.getProperty("janf.debug"));
        LOG_LEVEL = Level.NONE == level ? Level.of(System.getenv("JANF_DEBUG")) : level;
        Long output = StringUtils.toLong(System.getProperty("janf.output"));
        if (null == output) {
            output = StringUtils.toLong(System.getenv("JANF_OUTPUT"));
        }
        LOG_OUTPUT = null == output ? 1L : output;
    }

    private static class FileWriteTask
    extends ConsoleWriteTask {
        private final File logDir;

        FileWriteTask(File logDir, String pid, Level level, String content, Throwable exception) {
            super(pid, level, content, exception);
            this.logDir = logDir;
        }

        @Override
        public void run() {
            String fileName = String.format("%s%s.log", DateUtils.formatDate(this.getDateTime()), 0L != (LOG_OUTPUT & 4L) ? "-" + this.getPID() : "");
            try (PrintStream ps = new PrintStream(new FileOutputStream(new File(this.logDir, fileName), true));){
                this.setPrintStream(ps);
                super.run();
            }
            catch (FileNotFoundException e) {
                FileWriteTask.writeException("log file not found!", e, System.err);
            }
        }
    }

    private static class ConsoleWriteTask
    implements Runnable {
        private final String pid;
        private final Level level;
        private final String content;
        private final Throwable exception;
        private final Throwable stackException;
        private final String threadName;
        private final Date dateTime;
        private PrintStream ps;

        ConsoleWriteTask(String pid, Level level, String content, Throwable exception) {
            this.pid = pid;
            this.level = level;
            this.content = content;
            this.exception = exception;
            this.stackException = new Throwable();
            this.threadName = Thread.currentThread().getName();
            this.dateTime = new Date();
            this.setPrintStream(null == exception ? System.out : System.err);
        }

        protected static void writeContent(String content, PrintStream ps) {
            if (null == ps) {
                return;
            }
            ps.print(content);
        }

        protected static void writeException(String content, Throwable e, PrintStream ps) {
            if (null == ps) {
                return;
            }
            ps.print(content);
            e.printStackTrace(ps);
        }

        protected static void write(String content, Throwable e, PrintStream stream) {
            if (null == e) {
                ConsoleWriteTask.writeContent(content, stream);
                return;
            }
            ConsoleWriteTask.writeException(content, e, stream);
        }

        protected PrintStream getPrintStream() {
            return this.ps;
        }

        protected void setPrintStream(PrintStream ps) {
            this.ps = ps;
        }

        protected String getPID() {
            return this.pid;
        }

        public Date getDateTime() {
            return this.dateTime;
        }

        @Override
        public void run() {
            int line = 0;
            String caller = "UNKNOWN";
            StackTraceElement[] traces = this.stackException.getStackTrace();
            int l = traces.length;
            for (int i = 1; i < l; ++i) {
                StackTraceElement element = traces[i];
                if (CLASS_NAME.equals(element.getClassName())) continue;
                line = element.getLineNumber();
                caller = element.getClassName();
                break;
            }
            String outContent = String.format(DebugInfo.LOG_TEMPLATE, new Object[]{DateUtils.formatDateTimeMicro(this.dateTime), this.level, this.threadName, this.pid, caller, line, this.content});
            ConsoleWriteTask.write(outContent, this.exception, this.getPrintStream());
        }
    }

    public static enum Level {
        NONE,
        DEBUG,
        INFO,
        WARN,
        ERROR;


        public static Level of(String valueStr) {
            int value;
            if (null == valueStr) {
                return NONE;
            }
            try {
                value = Integer.parseInt(valueStr);
            }
            catch (NumberFormatException e) {
                return NONE;
            }
            for (Level level : Level.values()) {
                if (level.ordinal() != value) continue;
                return level;
            }
            return NONE;
        }
    }
}

