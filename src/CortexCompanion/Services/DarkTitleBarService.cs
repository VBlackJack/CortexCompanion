// Copyright 2026 Julien Bombled
// Licensed under the Apache License, Version 2.0.

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CortexCompanion.Services;

/// <summary>Requests the native immersive dark title bar without owning any color value.</summary>
public static partial class DarkTitleBarService
{
    private const int ImmersiveDarkMode = 20;
    private const int ImmersiveDarkModeBefore20H1 = 19;

    /// <summary>Applies the native dark-mode flag to an initialized WPF window.</summary>
    public static void Apply(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        nint handle = new WindowInteropHelper(window).Handle;
        if (handle == 0)
        {
            return;
        }

        int enabled = 1;
        int result = DwmSetWindowAttribute(handle, ImmersiveDarkMode, ref enabled, sizeof(int));
        if (result != 0)
        {
            _ = DwmSetWindowAttribute(handle, ImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));
        }
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(
        nint windowHandle,
        int attribute,
        ref int value,
        int valueSize);
}
