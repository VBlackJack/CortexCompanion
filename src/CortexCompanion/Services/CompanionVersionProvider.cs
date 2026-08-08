// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Reflection;

namespace CortexCompanion.Services;

/// <summary>Exposes the build-owned CalVer used by installer payload validation.</summary>
public static class CompanionVersionProvider
{
    /// <summary>Returns exactly the assembly informational version.</summary>
    public static string GetCurrent()
    {
        Assembly assembly = typeof(CompanionVersionProvider).Assembly;
        AssemblyInformationalVersionAttribute? attribute =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (string.IsNullOrWhiteSpace(attribute?.InformationalVersion))
        {
            throw new InvalidOperationException("The Companion informational version is missing.");
        }

        return attribute.InformationalVersion;
    }
}
