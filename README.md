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
4. Open **Base locale** and select **Synchroniser les documents locaux**. This
   primary action runs `cortex sync --json`; it does not require Confluence.

The **Pages Confluence** and **Planification Confluence** screens are optional advanced
integration features. They require a separate Confluence configuration and credential.

## What users can do

- connect to `cortex.exe` through automatic first-run discovery or a native file picker;
- choose the Cortex knowledge-base directory;
- synchronize local documents without a Confluence configuration;
- optionally review configured Confluence pages, store a Confluence credential, run
  Confluence collection, and manage its owned Windows scheduled task.

The application shows its window before it performs the bounded Cortex handshake. If
Cortex is absent, incompatible, or unavailable, the Settings screen stays actionable
while mutation commands remain disabled.
Unexpected startup diagnostics are written under
`%LOCALAPPDATA%\CortexCompanion\logs`.

## Configuration ownership

Companion stores its `cortex.exe` path and bounded startup-handshake timeout in
`%LOCALAPPDATA%\CortexCompanion\settings.json`. It does not write Cortex TOML files.
The knowledge-base setting is read and changed exclusively through the versioned
`cortex config get/set --json` compare-and-swap contract.

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
