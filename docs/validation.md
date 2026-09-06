# Companion experience and validation

[Français](validation.fr.md) | **English**

[Back to the README](../README.md)

Search and freshness are available starting with release 2026.0906.00.
Home, the getting-started guide, history, the search preview/copy action and
on-demand update checks are available starting with release 2026.0906.01.

The **Recherche** destination queries the Cortex JSON search contract. It offers
an exact section filter, a source-kind filter, bounded excerpts and explicit
source opening. Empty results, degraded ranking, timeout and transport/contract
failure are separate states. Reconnecting settings cancels the obsolete runtime's
search; closing the window also cancels a read-only search. A compatible Cortex
version 2026.0906.00 or newer is required; older compatible CLIs keep their
other features while search is disabled with an upgrade explanation.

Advanced filters start collapsed. Select a result to inspect its preview, then
use **Copier l'extrait et sa référence** to copy the excerpt, title and source.
Editing the query or filters clears the obsolete results and selection.
**Interrompre** or Escape cancels an active search.

The **Base locale** screen distinguishes the last successful collection from the
published generation and the latest successful index generation observed in
Companion's durable runs. The reader examines at most 100 recent run directories.
A newer unfinished or failed run prevents a current confirmation. Missing,
unreadable or incomplete evidence is shown as unconfirmed. Synchronizations run
outside Companion are not inferred from a file timestamp. This is an observation
history, not an independent scan of the live Chroma index.

A successful local-only run exposes its completion timestamp without confirming
Confluence freshness. Home recommends synchronization after a newer unsuccessful
run or when publication and indexing do not match. The guide uses saved
configuration, not an unsaved path, to validate the document-setup step.

## Acceptance scenarios for 2026.0906.01

| Area | Scenario | Expected behavior |
|---|---|---|
| Home and guide | Start without configured documents, then save a valid path | Next action leads to Settings, then synchronization; the saved setup step is marked verified |
| Optional Confluence | Skip Confluence and index local documents | The indexing timestamp remains available; no Confluence freshness is invented |
| Search | Select a result, copy it, then change a filter | Clipboard includes excerpt/title/reference; old results and copy availability are cleared |
| History | Inspect successful, partial, interrupted and unfinished operations | Each retains its own outcome; missing terminal evidence is never called a success |
| History errors | Inspect a sampled error list or a malformed record | Missing details and truncation are explicit; other readable records remain available |
| Updates | Check while online, then retry without network access | Stable version appears on success; failure clears stale availability and allows retry |
| Updates | Open the official download | Browser opens the combined-installer release page; no installer executes automatically |

History reads the two retained Companion worker stores, not arbitrary CLI logs.
Published-file counts combine additions and modifications. Some older and
Confluence collection records have no detailed counters. Recovery links open
current operational screens and do not replay a historical command.

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

The smoke test opens the real WPF shell, exercises the guide's next action,
navigates to search, checks collapsed filters and Tab traversal after expansion,
and renders Home, Search and History at minimum size in 96, 144 and 192 DPI.
It uses synthetic content and temporary settings. The history render inserts a
fixture entry after the empty-state load; history service tests independently
exercise actual worker records. These renders
exercise WPF layout and rasterization; they do not emulate Windows display changes.

Before claiming manual accessibility or multi-monitor coverage, still exercise:

- keyboard navigation through query, filters, results and source opening;
- Narrator announcements for empty, degraded and failed searches;
- moving the live window between displays at 100%, 150% and 200% scaling;
- resizing with long titles, excerpts and localized error messages.

The automated suite does not claim those physical or screen-reader checks passed.


## Link-first Confluence flow (2026.0906.01)

The source form accepts a page or space link. Inline authentication retains the link. Preview uses a disposable non-secret TOML; only the confirmed measured selection reaches the atomic CAS writer. Tests cover first setup, additional spaces, cancellation, authentication/remote failures, concurrent edits and foreign origins. Collection and indexing report separate outcomes; unsuccessful collection cannot authorize automatic indexing. Legacy links still require a space key. Existing roots retain their collection mode unless the whole space is explicitly selected.

Space links require Cortex 2026.0906.01 or newer. Visual smoke renders the minimum-size window and scope confirmation at 100%, 150% and 200% raster scale. This is not a live Confluence account or native DPI test.
