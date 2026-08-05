// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Windows.Input;

namespace CortexCompanion.Commands;

/// <summary>Runs one non-reentrant asynchronous UI action.</summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isRunning;

    /// <summary>Initializes an asynchronous command.</summary>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke() ?? true);

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isRunning = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute();
        }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>Re-evaluates command availability.</summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
