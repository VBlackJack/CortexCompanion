# Copyright 2026 Julien Bombled
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
"""Run the required bidirectional Windows byte-lock interoperability proof."""

from __future__ import annotations

import subprocess
import tempfile
from pathlib import Path

import filelock


def main() -> int:
    repo = Path(__file__).resolve().parents[2]
    probe = (
        repo
        / "tests"
        / "CortexCompanion.LockProbe"
        / "bin"
        / "Debug"
        / "net10.0-windows"
        / "CortexCompanion.LockProbe.dll"
    )
    with tempfile.TemporaryDirectory(prefix="CortexCompanion.LockInterop.") as raw_temp:
        temp = Path(raw_temp)
        config = temp / "confluence.toml"
        config.write_text(
            'schema_version = 1\ncredential_target = "probe"\n'
            "max_attachment_size_mb = 50\nfailure_threshold = 0.1\n",
            encoding="utf-8",
            newline="\n",
        )
        lock_path = config.with_name(config.name + ".mutation.lock")

        print(f"FILELOCK VERSION={filelock.__version__}")
        print("DIRECTION python->csharp")
        with filelock.FileLock(lock_path, timeout=1):
            print("PYTHON LOCK ACQUIRED")
            refused = subprocess.run(
                ["dotnet", str(probe), "mutate", str(config)],
                check=False,
                capture_output=True,
                text=True,
                timeout=15,
            )
            print(refused.stdout, end="")
            print(refused.stderr, end="")
            print(f"C# EXIT={refused.returncode}")
        if refused.returncode != 2:
            print("PROOF RESULT=FAIL")
            return 1

        print("DIRECTION csharp->python")
        holder = subprocess.Popen(
            ["dotnet", str(probe), "hold", str(config), "1500"],
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
        )
        assert holder.stdout is not None
        print(holder.stdout.readline(), end="")
        try:
            with filelock.FileLock(lock_path, timeout=0.3):
                print("PYTHON ACQUIRED LOCK UNEXPECTEDLY")
                holder.kill()
                return 1
        except filelock.Timeout:
            print("PYTHON BLOCKED: Timeout")
        remaining = holder.communicate(timeout=5)[0]
        print(remaining, end="")
        if holder.returncode != 0:
            print(f"C# HOLDER EXIT={holder.returncode}")
            print("PROOF RESULT=FAIL")
            return 1

    print("PROOF RESULT=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
