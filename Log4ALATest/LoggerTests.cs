using log4net;
using System;

namespace Log4ALATest
{
    class LoggerTests
    {

        private static ILog alaLogger = LogManager.GetLogger("Log4ALALogger");

        static void Main(string[] args)
        {
 
            for (int i = 0; i < 100; i++)
            {
                alaLogger.Info(new { id = $"log-{i}", message = $"test-{i}" });

            }

            System.Console.WriteLine("done");
            System.Threading.Thread.Sleep(new TimeSpan(0, 10, 0));
        }
    }
}
