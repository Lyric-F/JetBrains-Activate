"""Copy the original package into an embedded ZIP, preserving every byte."""
import argparse
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

parser = argparse.ArgumentParser()
parser.add_argument("--source", type=Path, required=True)
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
source = args.source.resolve()
files = sorted(p for p in source.rglob("*") if p.is_file())
if any(p.is_symlink() for p in source.rglob("*")):
    raise SystemExit("Symbolic links are not supported")
manifest = {p.relative_to(source).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
if len(manifest) != 36 or len(list(source.glob("*.vbs"))) != 9:
    raise SystemExit("Input does not match the reviewed 36-file / 9-script package")
resources = root / "resources"
resources.mkdir(exist_ok=True)
with ZipFile(resources / "payload.zip", "w", ZIP_DEFLATED) as archive:
    for p in files:
        archive.write(p, p.relative_to(source).as_posix())
(resources / "payload.manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"Embedded {len(files)} original files, ZIP size {(resources / 'payload.zip').stat().st_size} bytes")
