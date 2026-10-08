/*
 * Decompiled with CFR 0.152.
 */
package com.janetfilter.core.attach;

public class VMDescriptor {
    private String id;
    private String className;
    private String args;
    private Boolean old = true;

    public VMDescriptor(String id, String className, String args) {
        this.id = id;
        this.className = className;
        this.args = args;
    }

    public String getId() {
        return this.id;
    }

    public void setId(String id) {
        this.id = id;
    }

    public String getClassName() {
        return this.className;
    }

    public void setClassName(String className) {
        this.className = className;
    }

    public String getArgs() {
        return this.args;
    }

    public void setArgs(String args) {
        this.args = args;
    }

    public Boolean getOld() {
        return this.old;
    }

    public void setOld(Boolean old) {
        this.old = old;
    }

    public String toString() {
        return this.id + " " + this.className;
    }
}

