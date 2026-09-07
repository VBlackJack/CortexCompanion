# Copyright 2026 Julien Bombled
# Licensed under the Apache License, Version 2.0.
"""Parse every Cortex command line the desktop builds with the Cortex parsers that run it.

The document proofs check what Cortex answers; nothing checked what the desktop asks.
A subcommand renamed, a parent option moved after the subcommand or a required flag
dropped stays green on both sides and breaks the desktop at run time. Here the C#
probe captures each line from the code that builds it, and Cortex parses it with
the real parser, stopping before anything runs; every value is then checked in the
slot the desktop meant it for, not just accepted.
"""

import json
import os
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

COMPANION = Path(__file__).resolve().parents[2]
# The sibling checkout is the arrangement the workflows build; the variable lets a
# developer point at a branch checkout without moving it next to this repository.
CORTEX = Path(os.environ.get("CORTEX_CHECKOUT", COMPANION.parent / "Cortex")).resolve()
sys.path.insert(0, str(CORTEX))

from _version import __version__  # noqa: E402
from cli_surface import UnsupportedInvocationError, parse_invocation  # noqa: E402

PROBE = (
    COMPANION
    / "tests/CortexCompanion.LockProbe/bin/Debug/net10.0-windows/CortexCompanion.LockProbe.exe"
)
CONTRACT_VERSION = 1


@dataclass(frozen=True)
class FromInput:
    """The value the desktop was given under this input name must land here unchanged."""

    key: str
    as_path: bool = False


@dataclass(frozen=True)
class Expectation:
    """The command a line must select, and what its parser must make of each value."""

    command: str | None
    slots: dict[str, object]


_EXPECTED: dict[str, Expectation] = {
    "version": Expectation(None, {}),
    "search.filtered": Expectation(
        "search",
        {
            "query": FromInput("query"),
            "section": FromInput("section"),
            "source_kind": FromInput("source_kind"),
            "json": True,
        },
    ),
    "search.plain": Expectation(
        "search",
        {"query": FromInput("query"), "section": None, "source_kind": None, "json": True},
    ),
    "config.get": Expectation("config", {"operation": "get", "json": True}),
    "config.set.expected_hash": Expectation(
        "config",
        {
            "operation": "set",
            "json": True,
            "kb_path": FromInput("kb_path"),
            "expected_hash": FromInput("expected_hash"),
            "expect_absent": False,
        },
    ),
    "config.set.expect_absent": Expectation(
        "config",
        {
            "operation": "set",
            "json": True,
            "kb_path": FromInput("kb_path"),
            "expected_hash": None,
            "expect_absent": True,
        },
    ),
    "confluence.catalog": Expectation(
        "confluence",
        {
            "command": "catalog",
            "json": True,
            "config": FromInput("config_path", as_path=True),
            "space_key": FromInput("space_key"),
        },
    ),
    "confluence.source_status": Expectation(
        "confluence",
        {"command": "source-status", "json": True, "config": FromInput("config_path", as_path=True)},
    ),
    "confluence.pages": Expectation(
        "confluence",
        {"command": "pages", "json": True, "config": FromInput("config_path", as_path=True)},
    ),
    "confluence.resolve": Expectation(
        "confluence",
        {
            "command": "resolve",
            "json": True,
            "config": FromInput("config_path", as_path=True),
            "reference": FromInput("reference"),
        },
    ),
    "confluence.preview": Expectation(
        "confluence",
        {
            "command": "preview",
            "json": True,
            "config": FromInput("config_path", as_path=True),
            "reference": FromInput("reference"),
        },
    ),
    "sync.local": Expectation("sync", {"json": True, "section": None, "search": None}),
    "sync.confluence": Expectation(
        "confluence",
        {"command": "sync", "force": False, "config": FromInput("config_path", as_path=True)},
    ),
    "sync.confluence.force": Expectation(
        "confluence",
        {"command": "sync", "force": True, "config": FromInput("config_path", as_path=True)},
    ),
    "scheduled.guard": Expectation(
        "ingestion",
        {
            "command": "due",
            "config": FromInput("ingestion_config_path", as_path=True),
            "source_kind": FromInput("source_kind"),
        },
    ),
    "scheduled.sync": Expectation(
        "confluence",
        {
            "command": "sync",
            "force": False,
            "config": FromInput("confluence_config_path", as_path=True),
            "ingestion_config": FromInput("ingestion_config_path", as_path=True),
        },
    ),
}


def _fail(message: str) -> int:
    print(f"PROOF RESULT=FAIL {message}")
    return 1


def _expected_value(slot: object, inputs: dict[str, str]) -> object:
    if isinstance(slot, FromInput):
        value = inputs[slot.key]
        return Path(value) if slot.as_path else value
    return slot


def _check_line(name: str, inputs: dict[str, str], arguments: list[str]) -> str | None:
    """Return why the line fails, or None when Cortex accepts it as the desktop meant it."""
    expectation = _EXPECTED[name]
    rendered = " ".join(arguments)
    try:
        invocation = parse_invocation(arguments)
    except SystemExit as refusal:
        return f"{name}: cortex refuses `{rendered}` (exit {refusal.code})"
    except UnsupportedInvocationError as unsupported:
        return f"{name}: {unsupported}"
    if invocation.command != expectation.command:
        return (
            f"{name}: `{rendered}` selected {invocation.command!r}, "
            f"not {expectation.command!r}"
        )
    if expectation.command is None:
        if invocation.output != __version__:
            return f"{name}: `{rendered}` printed {invocation.output!r}, not the version"
        return None
    for slot, expected in expectation.slots.items():
        value = _expected_value(expected, inputs)
        actual = getattr(invocation.namespace, slot, _MISSING)
        if actual is _MISSING:
            return f"{name}: the parser of `{rendered}` has no {slot} slot"
        if actual != value:
            return f"{name}: `{rendered}` put {actual!r} in {slot}, the desktop meant {value!r}"
    return None


_MISSING = object()


def main() -> int:
    """Capture the desktop lines, parse each one, and refuse any that Cortex would."""
    if not PROBE.is_file():
        return _fail(f"missing probe {PROBE}")
    completed = subprocess.run(
        [str(PROBE), "dump-cli-arguments"],
        capture_output=True,
        text=True,
        encoding="utf-8",
        timeout=60,
        check=False,
    )
    if completed.returncode != 0:
        return _fail(f"the probe could not capture the lines: {completed.stderr.strip()}")
    document = json.loads(completed.stdout)
    if document.get("contract_version") != CONTRACT_VERSION:
        return _fail(f"the probe wrote contract {document.get('contract_version')!r}")
    lines = {line["name"]: line for line in document["lines"]}
    unknown = sorted(set(lines) - set(_EXPECTED))
    if unknown:
        return _fail(f"the desktop builds lines this proof does not know: {', '.join(unknown)}")
    missing = sorted(set(_EXPECTED) - set(lines))
    if missing:
        return _fail(f"the desktop no longer builds: {', '.join(missing)}")
    for name, line in lines.items():
        problem = _check_line(name, line["inputs"], line["arguments"])
        if problem is not None:
            return _fail(problem)
        print(f"{name}: {' '.join(line['arguments'])}")
    print(f"{len(lines)} desktop command lines accepted by the Cortex parsers")
    print("PROOF RESULT=PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
