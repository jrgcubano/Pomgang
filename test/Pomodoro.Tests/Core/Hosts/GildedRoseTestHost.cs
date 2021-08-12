using System.Collections.Generic;
using Pomodoro;
using Pomodoro.Entities;

namespace Pomodoro.Tests.Core.Hosts
{
    public class PomodoroTestHost
    {
        protected PomodoroTestHost(Item item) 
        {
            this.FirstItem = item;
            this.Pomodoro = new Pomodoro(new List<Item> { item });
        }

        public Pomodoro Pomodoro { get; protected set; }
        public Item FirstItem { get; protected set; }
        
        public static PomodoroTestHost CreatePomodoro(Item item) => new PomodoroTestHost(item);
    }
}