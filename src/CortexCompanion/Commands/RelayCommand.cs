// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Input;

namespace CortexCompanion.Commands;

/// <summary>
/// Provides the minimal strongly typed command required by the dependency-free navigation shell.
/// </summary>
public sealed class RelayCommand<T> : ICommand
    where T : struct
{
    private readonly Action<T> _execute;

    /// <summary>Initializes a command that is always available for valid typed parameters.</summary>
    public RelayCommand(Action<T> execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => parameter is T;

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (parameter is T value)
        {
            _execute(value);
        }
    }
}

