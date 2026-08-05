# Cortex Companion

Cortex Companion is a Windows desktop shell for the Cortex command-line interface. This scaffold validates a configured absolute `cortex.exe` path, performs a fail-closed version handshake, and exposes placeholder navigation for Pages, Sync, and Scheduling. It does not read Cortex configuration or credentials and does not perform synchronization or scheduling.

## Build

Requirements: Windows and the .NET 10 SDK.

```powershell
dotnet build -warnaserror
```

## Test

```powershell
dotnet test
```

## Run

```powershell
dotnet run --project src/CortexCompanion/CortexCompanion.csproj
```

Settings are stored in `%LOCALAPPDATA%\CortexCompanion\settings.json`. The initial schema contains one optional property, `cliPath`, which must be an absolute path to an existing `cortex.exe` file.

Licensed under the Apache License 2.0.

