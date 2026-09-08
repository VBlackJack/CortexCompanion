# Cortex Companion

**English** | [Francais](README.fr.md)

Cortex Companion is the Windows desktop interface for Cortex. It is designed for
people who should not need to edit TOML files or use a terminal for everyday setup,
synchronization, or scheduling.

## Readable scope window and honest timeouts (2026.0907.00)

The scope selection window drew its three options in the system text colour over
the dark background, because they declared neither a style nor a foreground and
the only RadioButton style in the theme is keyed. They are readable again, and an
automated guard now requires every RadioButton and CheckBox in a view to declare
its own colour.

A timeout now says that it is a timeout. Loading a page tree no longer reports a
connection failure, and the advice names the highest delay you can actually pick.
Values outside 15, 30, 60 and 120 seconds revert to 30, so an unqualified
"raise the timeout" could not be followed.

Paired Cortex 2026.0907.00 measures a Confluence scope with indexed counts, so adding
a source in a large space answers in about a second instead of exceeding the
timeout with nothing on screen.

## Interface language

Companion speaks English and French. It follows the Windows language by default, and
Settings offers an explicit choice that overrides it. Each language is listed under its
own name.

A change applies the next time the application starts, and the setting says so. Several
localized values are formats parsed once when they are first read, so switching them in
place would leave part of the interface in the previous language.

English is the neutral resource set, so a Windows language Companion does not ship reads
English rather than showing raw resource keys.

## Manage Confluence sources (2026.0906.02)

**Mes sources** appears first. Search by space or page title, open the original
in Confluence, or select **Modifier la selection**. **Ajouter une source** opens
the link form when needed.

The editor preserves selected roots. **Charger les pages de cet espace** loads
a searchable remote tree and identifies pages implicitly covered by a selected
ancestor. Scope is shared by all roots in the space. The catalogue is bounded
to 10,000 pages; partial reads are never presented as complete. Failed loading
preserves the selection draft.

The review distinguishes changed roots from effectively added/removed documents,
including descendants and overlapping subtrees. Unavailable measurements are
explicitly labelled. **Enregistrer et mettre a jour** collects all sources and
indexes only after success. **Enregistrer pour plus tard** only saves selection.

Cards show pending, updating, available, action required, or unverified status.
Readiness is conservative for the shared source generation: both published
selection and indexed generation identity must match. Collection success alone
never implies that search is current.

**Retirer de Cortex** never deletes remote originals. The last removal in the
session can be undone only if no configuration byte has changed; applying again
may be required after restoration. Persistent error feedback offers retry,
Confluence reconnection and detailed results. Reconnection validates expiry and
resumes the failed action without adding duplicate sources.

Cortex and Companion 2026.0906.02 or newer are both required. WPF smoke and automated tests do not replace testing
with a person discovering Cortex.

## Overview and getting started

This experience is available in the paired Cortex and Companion 2026.0906.01 release.

**Home** shows observed indexing and Confluence collection times, the next
scheduled collection, current activity and a contextual next step. Its four-step
guide links to document setup, optional Confluence setup, synchronization and a
first search. Configuration completion uses the saved state, never an unsaved
path draft. Indexing completion is an observation of a retained successful run.

**Search** keeps advanced filters collapsed initially. Select a result to read
its preview, open the source, or copy the excerpt with its title and reference.
Changing the criteria clears the previous selection and disables copying it.

**History** reads retained manual and scheduled runs for the current Windows
account. It does not include external CLI runs or already pruned records. The
normal worker retention is ten completed runs per category; the reader caps
inspection at 100 directories per category. Missing terminal results remain
unconfirmed. Available sync counters preserve the CLI meaning: published files
combine additions and modifications. Older and Confluence collection records
may lack these counters. Error lists may be sampled; the screen says when they
are incomplete. Recovery buttons open the current operational screen without
automatically rerunning an old operation.

The **Versions and updates** card on Home contacts GitHub only when requested.
It compares the running Companion version and connected Cortex version with the
official stable release. The download action opens the official release page
for the combined installer; no installation runs automatically.

See the [validation guide](docs/validation.md) for automated checks and the
remaining manual acceptance scenarios.

## Install and synchronize local documents

1. Download the single Windows installer from the
   [latest Cortex release](https://github.com/VBlackJack/Cortex/releases/latest).
2. Run the installer, then open **Cortex Companion** from the Start menu. The current
   public build is not code-signed. If Microsoft Defender SmartScreen warns you,
   first compare the installer's SHA-256 with the checksum published in that release;
   only then choose **More info** and **Run anyway**.
3. Open **Réglages**. Companion normally detects the `cortex.exe` from the same
   Cortex installation, including the parent folder used by the combined installer.
   Choose an existing knowledge-base folder, then select **Enregistrer le dossier**.
4. Open **Pages Confluence** and paste the full HTTPS link of a page or space.
   Select **Voir les documents a ajouter**. Companion infers the instance and
   space. Legacy short links and `viewpage.action` require a space key.
5. If connection details are missing or refused, enter the token and expiry date
   in the same screen, then select **Enregistrer le jeton et continuer**. The pasted link
   stays in place. The token goes directly to Windows Credential Manager.
6. Choose this page, this page and its children, or the whole space. Review the
   measured page count and approximate storage, then confirm the count. A space
   link resolves its homepage; the whole-space choice also includes pages outside
   that homepage tree. Cancelling leaves the active configuration unchanged.
7. Select **Collecter maintenant**. Companion collects all configured Confluence
   sources, then indexes only after confirmed collection success. Follow the
   resulting status and use **Rechercher un document** when indexing is current.
8. Existing sources are visible in **Mes sources**. Advanced converter options remain collapsed.
   The combined installer supplies the converter; no manual path is needed.
   Independent collection and indexing actions remain available in **Base locale**.

The local synchronization action runs `cortex sync --json`; it does not require
Confluence. The Confluence collection action is distinct and always passes
`--force` because cadence must not override an explicit user gesture.

The **Pages Confluence** screen creates the initial configuration itself. Users do not
need to find or edit a TOML file. Existing configurations keep their exact advanced
values and continue through the compare-and-swap mutation path.
Configurations created by releases that omitted `console_path` are repaired
atomically on first load, after the embedded converter passes the same probe.

## Stopping a run

While a run is alive, **Interrompre** appears next to the two collection actions.
It asks for confirmation, states the exact consequence, then stops the detached
worker and the Cortex process it owns. A stopped run is recorded as stopped, not
as a failure: the previously published generation stays intact and the local index
is completed by the next synchronization. The stop only ever reaches the worker
whose recorded process identity still matches, so a reused process identifier is
never killed.

Closing the window during a run does not stop it. Companion says so and asks for
confirmation first, because the worker outlives the window and only the progress
display is lost.

## Keyboard

| Shortcut | Action |
|---|---|
| `F5` | Reload the current screen |
| `Ctrl+S` | Save and connect, on the Settings screen |
| `Tab` / `Shift+Tab` | Move between controls; the focused control is outlined |
| `Esc` | Cancel the open confirmation dialog |
| `Enter` | Submit the field being edited: page URL, space URL, PAT, folder, path, start time |

## What users can do

- connect to `cortex.exe` through automatic first-run discovery or a native file picker;
- choose the Cortex knowledge-base directory;
- synchronize local documents without a Confluence configuration;
- initialize Confluence from one page URL without editing TOML;
- compare page-only, subtree, and whole-space scope before saving it;
- follow long collections through enumeration, staging, conversion, and publication;
- open the current generation and see the configured storage retention;
- stop a running collection, with the consequence stated before it happens;
- optionally review configured Confluence pages, store a Confluence credential, run
  Confluence collection, and manage its owned Windows scheduled task.

The application shows its window before it performs the bounded Cortex handshake. If
Cortex is absent, incompatible, or unavailable, the Settings screen stays actionable
while mutation commands remain disabled.
Unexpected startup diagnostics are written under
`%LOCALAPPDATA%\CortexCompanion\logs`. If the shell cannot be created, the fatal
dialog also displays the exception type and message so support can identify the
failure without first locating the log file. The release gate opens the complete
main window to catch invalid WPF bindings before publication.

## Slow Cortex commands

The Settings screen provides a bounded Cortex CLI timeout of 15, 30, 60, or
120 seconds. The default is 30 seconds, including when Companion loads a
settings file created by an older release. Select a longer value before using
**Enregistrer et connecter** on a computer where `cortex.exe` needs more time
to answer.

The selected value is shared by the `cortex.exe --version` compatibility
handshake, Cortex configuration reads and writes, and Confluence page reads and
resolutions. If a read exceeds the limit, Companion keeps mutations fail-closed
and directs the user back to Settings instead of reporting that the CLI refused
the request. Timeout logs include the configured and elapsed durations without
recording command arguments or secrets.

## Configuration ownership

Companion stores its `cortex.exe` path and bounded shared CLI timeout in
`%LOCALAPPDATA%\CortexCompanion\settings.json`. The knowledge-base setting is read and
changed exclusively through the versioned `cortex config get/set --json`
compare-and-swap contract.

The confirmed source selection creates `%APPDATA%\Cortex\confluence.toml` through the same
locked, validated, atomic writer used by later page mutations. It refuses to overwrite a
file that appeared concurrently. The file contains the inferred base URL, declared PAT
expiry, explicit space allowlist, local target, classification, and the validated
embedded converter path. It never contains the PAT.

Companion refuses to write a `base_url` that is not `https` outside loopback, the
same rule Cortex enforces when it reads the file, so the two never disagree about
what a valid configuration is.

The Confluence PAT is never written to `settings.json` or `CONFLUENCE.toml`. The
masked Settings field writes it directly to the `credential_target` declared by the
validated Confluence configuration, or to Cortex's `cortex-spike` default when that
file does not exist yet. Cortex and Companion use the same generic entry in Windows
Credential Manager, protected by DPAPI for the current Windows account. If a later
configuration selects another target, save the PAT again for that displayed target.

## Build and test

Requirements: Windows and the .NET 10 SDK.

```powershell
dotnet restore CortexCompanion.sln --locked-mode
dotnet list CortexCompanion.sln package --vulnerable --include-transitive
dotnet format CortexCompanion.sln --verify-no-changes --no-restore
dotnet build CortexCompanion.sln -c Release --no-restore -warnaserror
dotnet test CortexCompanion.sln -c Release --no-build --no-restore
```

Layout values, colors, and user-facing text are guarded by tests: views may not
carry raw sizes or hex colors, every theme resource a view names must exist, every
exposed string must resolve to a real resource, and every text pair must clear
WCAG AA while borders and focus rings clear the 3:1 non-text floor.

The repository rejects implicit C# `var` declarations. Enable the local pre-push gate
once per clone:

```powershell
git config core.hooksPath .githooks
```

### Interoperability proofs

Five Python scripts under `tests/interop/` prove the contract Companion shares with
the Cortex CLI on one machine. The `interoperability` workflow in both repositories
runs them against the peer repository's `main` on every push and pull request.
For coordinated changes, its manual `peer_ref` input selects the matching peer
branch or commit. The scripts can also be run locally against sibling checkouts.

- `lock_interop_proof.py` takes the configuration lock from each side in turn and
  expects the other side to be refused (the C# probe exits with code `2`, the
  Python `filelock` times out).
- `renderer_differential_proof.py` renders the same configuration through the C#
  probe and the Python `confluence_writer` renderer and compares the bytes for
  schemas v1, v2, and v3, including empty selections and subtree roots.
- `search_contract_proof.py` passes Python JSON search results through the real C#
  parser and checks the three retrieval modes, including Unicode text.
- `confluence_contract_proof.py` passes the Python resolve, preview, pages, catalog
  and status documents through the C# records that parse them, and checks every
  value that crosses, not just that the document was accepted.
- `cli_surface_proof.py` goes the other way: the C# probe captures every Cortex
  command line the desktop builds, from the code that builds it, and Cortex parses
  each with the parser that runs it, stopping before anything runs. A renamed
  subcommand, a parent option moved after its subcommand or a dropped flag fails
  here instead of on the user's desktop.

All five need `dotnet` on the PATH, a Debug build of `tests/CortexCompanion.LockProbe`
and a Python interpreter with the Cortex dependencies installed; all but the lock
proof also expect a Cortex checkout next to this repository, in `../Cortex`. The
command-line proof reads `CORTEX_CHECKOUT` when a branch checkout lives elsewhere.
Each script prints `PROOF RESULT=PASS` and exits `0` on success.

```powershell
dotnet build tests/CortexCompanion.LockProbe/CortexCompanion.LockProbe.csproj
python tests/interop/lock_interop_proof.py
python tests/interop/renderer_differential_proof.py
python tests/interop/search_contract_proof.py
python tests/interop/confluence_contract_proof.py
python tests/interop/cli_surface_proof.py
```

The manual `release-pair` workflow validates an exact pair of source commits.
Supply full 40-character `cortex_sha` and `companion_sha` values; it does not
certify an installer. See the [validation guide](docs/validation.md).

## Windows release payload

The canonical self-contained payload used by the combined Cortex installer is:

```powershell
dotnet publish src/CortexCompanion/CortexCompanion.csproj `
  -c Release `
  --no-restore `
  -p:PublishProfile=win-x64 `
  -o artifacts/publish/win-x64
```

`artifacts/publish/win-x64/CortexCompanion.exe --version` writes exactly the build
CalVer to redirected standard output and exits with code `0`. The Cortex installer
uses this fail-closed contract before accepting the Companion payload.

During uninstall, the combined installer runs
`CortexCompanion.exe --uninstall-cleanup`. This process-only mode exits `0` with
`cleanup=deleted`, `cleanup=absent`, or `cleanup=foreign-preserved`. It deletes only
the exact `\CortexCompanion\Ingestion-doc` task whose immutable ownership token is
still present; an absent or foreign task is never deleted. Scheduler read failures
exit `1` with `cleanup=failed`.

The payload also contains the redistribution notices `LICENSE.txt`,
`ThirdPartyNotices.txt`, `WPF-LICENSE.txt`, `WPF-ThirdPartyNotices.txt`, and
`Tomlyn-LICENSE.txt`, plus the application's own `CortexCompanion-LICENSE.txt`.
Publishing fails if any required source notice is absent.

## Confirmation policy

Explicit confirmation is required before operations that remove or replace state,
including page removal, collection-mode changes, and scheduled-task deletion. Cancel
and window close remain non-authorizing actions.

Licensed under the Apache License 2.0.

## Search and freshness

The **Recherche** screen provides indexed excerpts, filters and source opening. **Base locale** distinguishes the published generation from the latest observed successful index run. See [validation commands and coverage limits](docs/validation.md).

Desktop search is available with every Cortex this build accepts, since the accepted
floor is the CLI it ships with. Use **Interrompre** or Escape to cancel a search. Editing its
criteria clears obsolete results, and the last submitted criteria remain visible.
Unavailable source opening includes an explanation. Freshness is shown directly in
Recherche and Base locale, with navigation to synchronization and settings.

Refreshing or revisiting settings and scheduling preserves unsaved path, time and
preset edits. To configure Confluence authentication for the first time, follow
**Pages Confluence** and complete the inline connection form.
