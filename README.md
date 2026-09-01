# Cortex Companion

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
5. Open **Pages Confluence** and paste the full URL of the first page. Companion
   detects the instance and space whenever the URL contains them. Choose the PAT
   expiry date and classification, then select **Initialiser et ajouter la page**.
   Legacy `viewpage.action` and short URLs require the space key to be entered.
6. The combined Cortex installer already provides the windowless Confluence
   converter. No path is required. The developer override remains under the
   collapsed advanced options and is accepted only after a five-second machine
   capability probe; the windowed `ConfluenceRAGBuilder.exe` is rejected.
7. Open **Base locale** and select **Synchroniser les documents locaux**. This
   primary action runs `cortex sync --json`; it does not require Confluence.

The **Pages Confluence** screen creates the initial configuration itself. Users do not
need to find or edit a TOML file. Existing configurations keep their exact advanced
values and continue through the compare-and-swap mutation path.
Configurations created by releases that omitted `console_path` are repaired
atomically on first load, after the embedded converter passes the same probe.

## What users can do

- connect to `cortex.exe` through automatic first-run discovery or a native file picker;
- choose the Cortex knowledge-base directory;
- synchronize local documents without a Confluence configuration;
- initialize Confluence from one page URL without editing TOML;
- optionally review configured Confluence pages, store a Confluence credential, run
  Confluence collection, and manage its owned Windows scheduled task.

The application shows its window before it performs the bounded Cortex handshake. If
Cortex is absent, incompatible, or unavailable, the Settings screen stays actionable
while mutation commands remain disabled.
Unexpected startup diagnostics are written under
`%LOCALAPPDATA%\CortexCompanion\logs`.

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
dotnet format CortexCompanion.sln --verify-no-changes --no-restore
dotnet build CortexCompanion.sln -c Release --no-restore -warnaserror
dotnet test CortexCompanion.sln -c Release --no-build --no-restore
```

The repository rejects implicit C# `var` declarations. Enable the local pre-push gate
once per clone:

```powershell
git config core.hooksPath .githooks
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
