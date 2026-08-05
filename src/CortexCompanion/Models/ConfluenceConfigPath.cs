// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Models;

/// <summary>Identifies the source that selected the effective Confluence configuration path.</summary>
public enum ConfluenceConfigPathOrigin
{
    /// <summary>The platform default selected the path.</summary>
    Default,

    /// <summary>The dedicated environment override selected the path.</summary>
    Environment,
}

/// <summary>Captures the one absolute Confluence configuration path used for a session.</summary>
public sealed record ConfluenceConfigPathResolution(
    string AbsolutePath,
    ConfluenceConfigPathOrigin Origin,
    string OriginName);

/// <summary>Describes one active root-field environment override for locked UI display.</summary>
public sealed record ConfluenceEnvironmentOverride(
    string FieldName,
    string EnvironmentName,
    string Value);
