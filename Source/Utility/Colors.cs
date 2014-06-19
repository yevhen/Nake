using System;
using System.Linq;

namespace Nake
{
    public static class With
    {
        public static void Color(ConsoleColor color, Action action)
        {
            Console.ForegroundColor = color;
            action();
            Console.ResetColor();
        }
    }
}