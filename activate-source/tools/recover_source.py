"""Recover readable source without running or changing the input package.

Requires Python 3.9+, Java, and a verified CFR 0.152 JAR. No third-party Python
dependencies. Output directories original-package/ and modules/ must not exist.
"""

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import struct
import subprocess
from datetime import datetime, timezone
from zipfile import ZipFile


MODULES = {
    "core": "active-agt.jar",
    "dns": "plugins/dns.jar",
    "hideme": "plugins/hideme.jar",
    "power": "plugins/power.jar",
    "url": "plugins/url.jar",
}
CFR_MD5 = "8a85ada8cec494121246805a5562b82b"


def manifest(root):
    return [
        {"path": p.relative_to(root).as_posix(), "bytes": p.stat().st_size,
         "sha256": hashlib.sha256(p.read_bytes()).hexdigest()}
        for p in sorted(root.rglob("*")) if p.is_file()
    ]


def save_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n",
                    encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--decompiler", type=Path, required=True)
    args = parser.parse_args()
    source, output, cfr = (p.resolve() for p in
                           (args.input, args.output, args.decompiler))
    if not source.is_dir():
        raise SystemExit("Input directory does not exist")
    if source == output or source in output.parents or output in source.parents:
        raise SystemExit("Input and output must be separate directory trees")
    if hashlib.md5(cfr.read_bytes()).hexdigest() != CFR_MD5:
        raise SystemExit("Decompiler does not match the published CFR 0.152 MD5")
    for name in ("original-package", "modules"):
        if (output / name).exists():
            raise SystemExit(f"Refusing to overwrite {output / name}")
    for relative in MODULES.values():
        if not (source / relative).is_file():
            raise SystemExit(f"Missing input: {relative}")

    before = manifest(source)
    output.mkdir(parents=True, exist_ok=True)
    reports = output / "reports"
    reports.mkdir(exist_ok=True)
    save_json(reports / "recovery-input-manifest.json", before)
    snapshot = output / "original-package"
    shutil.copytree(source, snapshot, copy_function=shutil.copy2)
    if before != manifest(snapshot):
        raise SystemExit("Input snapshot mismatch")
    classpath = os.pathsep.join(str(snapshot / rel) for rel in MODULES.values())
    rows = []
    for module, relative in MODULES.items():
        jar = snapshot / relative
        base = output / "modules" / module
        java = base / "src" / "main" / "java"
        resources = base / "src" / "main" / "resources"
        java.mkdir(parents=True)
        resources.mkdir(parents=True)
        with ZipFile(jar) as archive:
            classes = sorted(n for n in archive.namelist() if n.endswith(".class"))
            versions = sorted({struct.unpack(">H", archive.read(n)[6:8])[0]
                               for n in classes})
            resource_names = []
            for entry in archive.infolist():
                if entry.is_dir() or entry.filename.endswith(".class"):
                    continue
                name = PurePosixPath(entry.filename)
                target = (resources / name).resolve()
                if name.is_absolute() or ".." in name.parts or resources not in target.parents:
                    raise SystemExit(f"Unsafe ZIP resource: {entry.filename}")
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(archive.read(entry))
                resource_names.append(entry.filename)
        command = ["java", "-jar", str(cfr), str(jar), "--outputdir", str(java),
                   "--extraclasspath", classpath, "--silent", "true"]
        result = subprocess.run(command, capture_output=True, text=True,
                                encoding="utf-8", errors="replace", timeout=120)
        (reports / f"{module}-cfr.log").write_text(
            result.stdout + result.stderr, encoding="utf-8")
        java_files = sorted(p.relative_to(java).as_posix() for p in java.rglob("*.java"))
        class_map = {name: name.split("$", 1)[0].removesuffix(".class") + ".java"
                     for name in classes}
        missing = [name for name, src in class_map.items() if src not in java_files]
        row = {"module": module, "input_jar": relative,
               "jar_sha256": hashlib.sha256(jar.read_bytes()).hexdigest(),
               "class_count": len(classes), "class_major_versions": versions,
               "java_file_count": len(java_files), "classes": classes,
               "java_files": java_files, "class_to_source": class_map,
               "resources": resource_names, "cfr_exit_code": result.returncode,
               "missing_source_mappings": missing, "command": command}
        rows.append(row)
        save_json(reports / f"{module}-inventory.json", row)
        print(f"{module}: {len(classes)} class files -> {len(java_files)} Java files; "
              f"exit={result.returncode}; missing={len(missing)}", flush=True)

    report = {"created_utc": datetime.now(timezone.utc).isoformat(),
              "original_file_count": len(before), "modules": rows,
              "original_unchanged": before == manifest(source),
              "snapshot_identical": before == manifest(snapshot),
              "total_class_files": sum(r["class_count"] for r in rows),
              "total_java_files": sum(r["java_file_count"] for r in rows),
              "original_vbs_files": len(list(snapshot.glob("*.vbs"))),
              "target_code_executed": False, "compilation_verified": False}
    save_json(reports / "recovery-report.json", report)
    if (not report["original_unchanged"] or not report["snapshot_identical"]
            or any(r["cfr_exit_code"] or r["missing_source_mappings"] for r in rows)):
        raise SystemExit("Recovery verification failed; inspect reports")
    print("Recovery complete. Original content hashes are unchanged.")


if __name__ == "__main__":
    main()
