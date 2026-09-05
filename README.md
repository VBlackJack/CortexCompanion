# Cortex Companion

**English** | [Francais](README.fr.md)

Cortex Companion is the Windows desktop interface for Cortex. It is designed for
people who should not need to edit TOML files or use a terminal for everyday setup,
synchronization, or scheduling.

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
4. To use Confluence, enter the PAT under **Réglages > Authentification
   Confluence** and select **Enregistrer le PAT**. On a new installation,
   Companion uses Cortex's default Windows credential target, `cortex-spike`.
5. Open **Pages Confluence** and paste the full URL of the first page. The URL must
   be `https`; a cleartext `http` instance is refused because the personal access
   token travels as a bearer header on every request. Loopback addresses stay
   allowed for local test instances. Companion
   detects the instance and space whenever the URL contains them. Choose the PAT
   expiry date and classification, then select **Initialiser et ajouter la page**.
   Companion counts page-only, subtree, and whole-space scope before writing the
   choice. Subtree is preselected when the page has descendants. Legacy
   `viewpage.action` and short URLs require the space key to be entered.
6. Review the measured page count, approximate storage, physical ingestion root,
   and generation retention. The configured `target` is a logical index prefix,
   not a directory inside the selected knowledge-base folder.
7. The combined Cortex installer already provides the windowless Confluence
   converter. No path is required. The developer override remains under the
   collapsed advanced options and is accepted only after a five-second machine
   capability probe; the windowed `ConfluenceRAGBuilder.exe` is rejected.
8. Open **Base locale**. **Synchroniser les documents locaux** indexes the local
   knowledge base and the current published ingestion generation; **Collecter
   Confluence** runs a manual collection immediately and displays phase plus
   numeric progress. Both actions sit on the main card, next to each other.
9. Use **Ouvrir la génération courante** to inspect the immutable published
   documents. A narrow successful scope reports excluded descendants and offers
   a one-click switch to subtree collection.

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

The first-run Pages card creates `%APPDATA%\Cortex\confluence.toml` through the same
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

Two Python scripts under `tests/interop/` prove the contract Companion shares with
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

Both need `dotnet` on the PATH, a Debug build of `tests/CortexCompanion.LockProbe`
and a Python interpreter with the Cortex dependencies installed; the renderer proof
also expects a Cortex checkout next to this repository, in `../Cortex`. Each script
prints `PROOF RESULT=PASS` and exits `0` on success.

```powershell
dotnet build tests/CortexCompanion.LockProbe/CortexCompanion.LockProbe.csproj
python tests/interop/lock_interop_proof.py
python tests/interop/renderer_differential_proof.py
```

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
