# Search, freshness and visual validation

[Français](validation.fr.md) | **English**

[Back to the README](../README.md)

These additions are available in unreleased source builds.

The **Recherche** destination queries the Cortex JSON search contract. It offers
an exact section filter, a source-kind filter, bounded excerpts and explicit
source opening. Empty results, degraded ranking, timeout and transport/contract
failure are separate states. Reconnecting settings cancels the obsolete runtime's
search; closing the window also cancels a read-only search. A compatible Cortex
build containing `search --json` is required; older CLIs fail visibly.

The **Base locale** screen distinguishes the last successful collection from the
published generation and the latest successful index generation observed in
Companion's durable runs. The reader examines at most 100 recent run directories.
A newer unfinished or failed run prevents a current confirmation. Missing,
unreadable or incomplete evidence is shown as unconfirmed. Synchronizations run
outside Companion are not inferred from a file timestamp. This is an observation
history, not an independent scan of the live Chroma index.

## Local validation

```powershell
dotnet test CortexCompanion.sln -c Release --no-restore
dotnet build tests/CortexCompanion.LockProbe/CortexCompanion.LockProbe.csproj --no-restore
python tests/interop/search_contract_proof.py
python tests/interop/renderer_differential_proof.py
python tests/interop/lock_interop_proof.py
```

The Python proofs expect a sibling `Cortex` checkout with its dependencies installed.
The `release-pair` workflow in either repository accepts two full commit SHAs,
verifies their checked-out identities and executes these proofs. Its job summary
records the source pair; it does not certify installer bytes.

To retain visual artifacts from the WPF smoke test:

```powershell
$env:CORTEX_VISUAL_ARTIFACTS = Join-Path $PWD 'local/visual-validation'
dotnet test CortexCompanion.sln -c Release --no-restore --filter FullyQualifiedName~MainWindowSmokeTests
```

The smoke test opens the real WPF shell, navigates to search, traverses from the
query to the section field with Tab and renders the minimum-size window at 96,
144 and 192 DPI. It uses synthetic content and temporary settings. These renders
exercise WPF layout and rasterization; they do not emulate Windows display changes.

Before claiming manual accessibility or multi-monitor coverage, still exercise:

- keyboard navigation through query, filters, results and source opening;
- Narrator announcements for empty, degraded and failed searches;
- moving the live window between displays at 100%, 150% and 200% scaling;
- resizing with long titles, excerpts and localized error messages.

The automated suite does not claim those physical or screen-reader checks passed.
