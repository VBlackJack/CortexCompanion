# Copyright 2026 Julien Bombled
# Licensed under the Apache License, Version 2.0.
"""Pass Python-produced Confluence documents through the actual C# contract consumer.

The desktop client parses these four documents with strict records that refuse an
unmapped member and pin the contract version. Nothing else compares the producer with
that consumer, so a field renamed on one side stays green on both until a user meets it.
"""

import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path

COMPANION = Path(__file__).resolve().parents[2]
# CORTEX_CHECKOUT points the proof at a branch checkout instead of the sibling clone, as
# the command-line proof already allows; a contract change is proved against the checkout
# that carries it before either side is on main.
CORTEX = Path(os.environ.get("CORTEX_CHECKOUT", COMPANION.parent / "Cortex")).resolve()
sys.path.insert(0, str(CORTEX))

from confluence_writer.models import (  # noqa: E402
    CatalogPageContract,
    ConfiguredPageContract,
    ConfiguredSpaceContract,
    LastSyncContract,
    PagesContract,
    ResolvedPageContract,
    ScopeChoiceContract,
    ScopePreviewContract,
    ScopeSummaryContract,
    SourceCatalogContract,
    SourceStatusContract,
)

def _documents() -> dict[str, tuple[object, dict[str, object]]]:
    """Return one document per contract with the values most likely to break a consumer."""
    resolved = ResolvedPageContract(
        contract_version=1,
        page_id="2134730685",
        title="Équipe 東京 : plan d'action",
        space_key="PN",
        configured=True,
    )
    # A space the caller can see no page in measures zero, which is a value the enumerating
    # implementation could never emit and no released client had ever been handed.
    preview = ScopePreviewContract(
        contract_version=2,
        page_id="2134730685",
        title="Équipe 東京 : plan d'action",
        space_key="PN",
        recommended_selection="pages",
        coverage="subtree",
        covering_root="1286970214",
        page_only=ScopeChoiceContract(page_count=1, estimated_bytes=393_216),
        subtree=ScopeChoiceContract(page_count=1, estimated_bytes=393_216),
        whole_space=ScopeChoiceContract(page_count=0, estimated_bytes=0),
        storage_root="C:\\Users\\Example\\AppData\\Local\\Cortex",
        retention_generations=2,
    )
    pages = PagesContract(
        contract_version=2,
        spaces=(
            ConfiguredSpaceContract(
                space_key="PN",
                selection="subtree",
                target="confluence/PN",
                classification="pro-confidentiel",
                pages=(
                    ConfiguredPageContract(page_id="2134730685", title="Équipe 東京"),
                    ConfiguredPageContract(page_id="1286970214", title=None),
                ),
            ),
            ConfiguredSpaceContract(
                space_key="ANSIBLE",
                selection="whole_space",
                target="confluence/ANSIBLE",
                classification="perso-non-sensible",
                pages=None,
            ),
        ),
        last_sync=LastSyncContract(
            last_success_at=None,
            status=None,
            error_code=None,
            scope_summaries=(
                ScopeSummaryContract(
                    space_key="PN",
                    selection="subtree",
                    selected_page_count=8,
                    available_page_count=None,
                    excluded_descendant_count=None,
                ),
            ),
        ),
    )
    catalog = SourceCatalogContract(
        space_key="PN",
        pages=(
            CatalogPageContract(page_id="2134730685", title="Équipe 東京", ancestor_ids=()),
            CatalogPageContract(
                page_id="1286970214", title="2023 Roadmaps", ancestor_ids=("2134730685",)
            ),
        ),
    )
    status = SourceStatusContract(
        selection_current=False, generation_id=None, status="degraded"
    )
    # The consumer serializes with the contract names, not the record property names.
    # Every value that crosses the boundary is checked, not just an identifier: a proof that
    # only asks whether the document parsed would stay green while the text arrived mangled.
    return {
        "resolve": (
            resolved,
            {
                "contract_version": 1,
                "page_id": "2134730685",
                "title": "Équipe 東京 : plan d'action",
                "space_key": "PN",
                "configured": True,
            },
        ),
        "preview": (
            preview,
            {
                "contract_version": 2,
                "page_id": "2134730685",
                "title": "Équipe 東京 : plan d'action",
                "space_key": "PN",
                "recommended_selection": "pages",
                "coverage": "subtree",
                "covering_root": "1286970214",
                "storage_root": "C:\\Users\\Example\\AppData\\Local\\Cortex",
                "retention_generations": 2,
            },
        ),
        "pages": (pages, {"contract_version": 2}),
        "catalog": (catalog, {"contract_version": 1, "space_key": "PN"}),
        "status": (
            status,
            {"contract_version": 1, "selection_current": False,
             "generation_id": None, "status": "degraded"},
        ),
    }


def main() -> int:
    """Send each document to the desktop consumer and refuse a silent rejection."""
    probe = (
        COMPANION
        / "tests/CortexCompanion.LockProbe/bin/Debug/net10.0-windows/CortexCompanion.LockProbe.exe"
    )
    if not probe.is_file():
        print(f"PROOF RESULT=FAIL missing probe {probe}")
        return 1

    with tempfile.TemporaryDirectory(prefix="cortex-confluence-contract-") as directory:
        for kind, (document, expected) in _documents().items():
            fixture = Path(directory) / f"{kind}.json"
            # Written exactly as the command writes it, indentation included.
            fixture.write_text(document.model_dump_json(indent=2) + "\n", encoding="utf-8")
            completed = subprocess.run(
                [str(probe), "validate-confluence", kind, str(fixture)],
                capture_output=True,
                text=True,
                encoding="utf-8",
                timeout=60,
                check=False,
            )
            if completed.returncode != 0:
                print(f"PROOF RESULT=FAIL {kind} refused: {completed.stderr.strip()}")
                return 1
            consumed = json.loads(completed.stdout)
            for field, value in expected.items():
                if consumed.get(field) != value:
                    print(
                        f"PROOF RESULT=FAIL {kind} field {field} "
                        f"reached the consumer as {consumed.get(field)!r}, not {value!r}"
                    )
                    return 1
            if kind == "pages" and len(consumed["spaces"]) != 2:
                print("PROOF RESULT=FAIL pages lost a space on the way through")
                return 1
            if kind == "catalog" and len(consumed["pages"]) != 2:
                print("PROOF RESULT=FAIL catalog lost a page on the way through")
                return 1
            print(f"{kind}: accepted by the desktop consumer")

    print("PROOF RESULT=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
