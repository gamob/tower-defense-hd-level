using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeDrawer.lib
{
    public class GameUI : IObserver
    {
        public void Update(string eventType, object data)
        {
            switch (eventType)
            {
                case "GameStarted":
                    Console.WriteLine("Game has started!");
                    break;
                case "WaveStarted":
                    Console.WriteLine("New wave has started!");
                    break;
                case "GameOver":
                    Console.WriteLine("Game Over! Better luck next time.");
                    break;
                default:
                    Console.WriteLine($"Unknown event: {eventType}");
                    break;
            }
        }
    }
}
