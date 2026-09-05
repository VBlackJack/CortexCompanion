# Copyright 2026 Julien Bombled
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
"""Compare canonical C# renderer bytes with the current Cortex Python renderer."""

from __future__ import annotations

import hashlib
import subprocess
import sys
import tempfile
from datetime import datetime, timedelta, timezone
from pathlib import Path


def _settings(version: int):
    from confluence_writer.config import (
        ConfluenceSettings,
        PageSelection,
        SpaceMapping,
    )

    common = {
        "schema_version": version,
        "base_url": "https://wiki.example.test:8443/confluence",
        "credential_target": "l'equipe-東京",
        "auth_expires_at": datetime(
            2026,
            8,
            5,
            12,
            13,
            14,
            123456,
            tzinfo=timezone(timedelta(hours=2, minutes=30)),
        ),
        "console_path": Path(r"C:\Program Files\Cortex\console.exe"),
        "max_attachment_size_mb": 1,
    }
    if version == 1:
        return ConfluenceSettings(
            **common,
            failure_threshold=1.0,
            spaces=(
                SpaceMapping(
                    space_key="DOC.UNICODE",
                    target="équipe/docs",
                    classification="pro-confidentiel",
                ),
            ),
        )
    if version == 3:
        return ConfluenceSettings(
            **common,
            failure_threshold=0.1,
            spaces=(
                SpaceMapping(
                    space_key="TREE", target="tree", classification="pro-confidentiel",
                    selection="subtree", pages=(PageSelection(page_id="123"),),
                ),
                SpaceMapping(
                    space_key="EMPTY", target="empty", classification="perso-non-sensible",
                    selection="subtree", pages=(),
                ),
            ),
        )
    return ConfluenceSettings(
        **common,
        failure_threshold=0.0000001,
        spaces=(
            SpaceMapping(
                space_key="DOC.UNICODE",
                target="équipe/docs",
                classification="pro-confidentiel",
                selection="pages",
                pages=(PageSelection(page_id="123"), PageSelection(page_id="987654321")),
            ),
            SpaceMapping(
                space_key="EMPTY",
                target="empty",
                classification="perso-non-sensible",
                selection="pages",
                pages=(),
            ),
            SpaceMapping(
                space_key="ALL",
                target="all",
                classification="pro-confidentiel",
                selection="whole_space",
            ),
        ),
    )


def main() -> int:
    repo = Path(__file__).resolve().parents[2]
    cortex = repo.parent / "Cortex"
    sys.path.insert(0, str(cortex))
    from confluence_writer.config_mutation import render_confluence_settings

    probe = (
        repo
        / "tests"
        / "CortexCompanion.LockProbe"
        / "bin"
        / "Debug"
        / "net10.0-windows"
        / "CortexCompanion.LockProbe.dll"
    )
    with tempfile.TemporaryDirectory(prefix="CortexCompanion.RendererDiff.") as raw_temp:
        temp = Path(raw_temp)
        for version in (1, 2, 3):
            csharp_path = temp / f"csharp-v{version}.toml"
            subprocess.run(
                ["dotnet", str(probe), "render-golden", str(csharp_path), f"v{version}"],
                check=True,
            )
            csharp = csharp_path.read_bytes()
            python = render_confluence_settings(_settings(version))
            python_hash = hashlib.sha256(python).hexdigest()
            csharp_hash = hashlib.sha256(csharp).hexdigest()
            matches = python == csharp
            print(
                f"CASE v{version} PYTHON_SHA256={python_hash} "
                f"CSHARP_SHA256={csharp_hash} MATCH={matches}"
            )
            if not matches:
                print("PROOF RESULT=FAIL")
                return 1
    print("PROOF RESULT=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
