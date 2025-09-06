using System;

namespace Nake.Utility;

/// <summary>
/// Helps printing console messages with given color
/// </summary>
static class Color
{
    /// <summary>
    /// Temporarily switches console color to the given one, while executing specified action
    /// </summary>
    /// <param name="color">The color to switch to.</param>
    /// <param name="action">The action to execute.</param>
    public static void With(ConsoleColor color, Action action)
    {
        Console.ForegroundColor = color;
        action();
        Console.ResetColor();
    }
}