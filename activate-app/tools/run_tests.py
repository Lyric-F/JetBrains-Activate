"""Run tests in an isolated, case-normalized child environment on Windows."""
from pathlib import Path
import os
import subprocess

root = Path(__file__).resolve().parents[1]
environment = {key.upper(): value for key, value in os.environ.items()}
result = subprocess.run([str(root / "dist" / "Activate.Tests.exe"),
                         str(root / "reports" / "integration-tests.json")],
                        cwd=root, env=environment, capture_output=True)
log = result.stdout.decode("utf-8-sig", errors="replace") + result.stderr.decode("utf-8-sig", errors="replace")
(root / "reports" / "integration-tests.log").write_text(log, encoding="utf-8")
print(log)
raise SystemExit(result.returncode)
