// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

namespace CortexCompanion.Services;

/// <summary>Signals that a Cortex CLI process or versioned JSON response was not trustworthy.</summary>
public sealed class CortexCliContractException : Exception
{
    /// <summary>Gets whether Cortex may have changed state before the result became unavailable.</summary>
    public bool OutcomeUnknown { get; }

    /// <summary>Gets whether the configured Cortex CLI timeout expired.</summary>
    public bool TimedOut { get; }

    /// <summary>Initializes a protocol-boundary exception with a non-sensitive message.</summary>
    public CortexCliContractException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a protocol-boundary exception with its parsing cause.</summary>
    public CortexCliContractException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a protocol failure whose mutation outcome is explicitly unknown.</summary>
    public CortexCliContractException(string message, bool outcomeUnknown)
        : this(message, outcomeUnknown, timedOut: false)
    {
    }

    /// <summary>Initializes a protocol failure with explicit outcome and timeout classifications.</summary>
    public CortexCliContractException(string message, bool outcomeUnknown, bool timedOut)
        : base(message)
    {
        OutcomeUnknown = outcomeUnknown;
        TimedOut = timedOut;
    }
}
