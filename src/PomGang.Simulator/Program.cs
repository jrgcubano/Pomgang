using System;
using System.Reactive.Concurrency;
using PomGang.Common.Services;
using static System.Console;

namespace PomGang.Simulator
{
    static class Program
    {
        static int DisplayMenu()
        {
            WriteLine("Pomodoro");
            WriteLine();
            WriteLine("1. Start");
            WriteLine("2. ShortBreak");
            WriteLine("3. Stop");
            WriteLine("4. Exit");

            var result = ReadLine();

            return Convert.ToInt32(result);
        }

        public static void Main(string[] args)
        {
            // var timeService = new TimeService(Scheduler.Default);
            var logger = new LoggerService();
            var pomodoro = new Pomodoro(Scheduler.Default, logger, 5);

            var input = 0;

            do
            {
                input = DisplayMenu();
                if (input.Equals(1)) pomodoro.Start();
                else if (input.Equals(2)) pomodoro.ShortBreak();
                else if (input.Equals(3)) pomodoro.Stop();
            } while (input != 4);

            Clear();
        }
    }
}