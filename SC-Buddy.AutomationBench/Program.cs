using System;
using System.Threading.Tasks;

namespace SC_Buddy.AutomationBench
{
    class Program
    {
        static void Main(string[] _) => Run().GetAwaiter().GetResult();

        static async Task Run()
        {
            var hook = await MouseHook.Singleton;
            var automation = new UIAutomation();
            _ = hook.Stream.Subscribe(automation.Ingress);
            _ = automation.Egress.Subscribe(x => Console.WriteLine($"{x}"));

            while (true)
            {
                var key = Console.ReadKey();
                if ((key.Key & ConsoleKey.C) == ConsoleKey.C &&
                    (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) break;
            }
        }
    }
}
