# Copyright 2026 Julien Bombled
# Licensed under the Apache License, Version 2.0.
"""Pass Python-produced desktop results through the actual C# contract consumer."""

import json
import subprocess
import sys
import tempfile
from pathlib import Path

COMPANION = Path(__file__).resolve().parents[2]
CORTEX = COMPANION.parent / "Cortex"
sys.path.insert(0, str(CORTEX))

from search_command import CONTRACT_VERSION, present_hit  # noqa: E402


def main() -> int:
    probe = (
        COMPANION
        / "tests/CortexCompanion.LockProbe/bin/Debug/net10.0-windows/CortexCompanion.LockProbe.exe"
    )
    with tempfile.TemporaryDirectory(prefix="cortex-search-contract-") as directory:
        for mode in ("vector-only", "hybrid", "hybrid+rerank"):
            hit = present_hit(
                {
                    "id": "unicode",
                    "text": "Réponse pour l'équipe 東京",
                    "metadata": {
                        "path": "équipe/document.md",
                        "source_kind": "doc",
                        "canonical_uri": "https://example.test/wiki/page",
                    },
                },
                None,
            )
            expected = {
                "contract_version": CONTRACT_VERSION,
                "operation": "search",
                "status": "succeeded",
                "mode": mode,
                "degraded": mode != "hybrid+rerank",
                "results": [hit],
            }
            fixture = Path(directory) / "response.json"
            fixture.write_text(json.dumps(expected), encoding="utf-8")
            result = subprocess.run(
                [str(probe), "validate-search", str(fixture)],
                capture_output=True,
                text=True,
                encoding="utf-8",
                check=True,
                timeout=30,
            )
            assert json.loads(result.stdout) == expected, mode
            print(f"PASS desktop search contract: {mode}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
