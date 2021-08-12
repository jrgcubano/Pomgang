using System.Collections.Generic;
using static System.Console;
using Pomodoro;
using Pomodoro.Entities;

namespace Pomodoro.Console.Core.Extensions
{
    public static class PomodoroConsoleExtensions
    {
        public static void Print(this IEnumerable<Item> items)
        {
            foreach(var item in items)
                WriteLine(item.ToString());
        }
    }
}