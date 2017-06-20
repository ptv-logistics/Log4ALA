using log4net;
using System;

namespace Log4ALATest
{
    class LoggerTests
    {

        private static ILog alaLogger1 = LogManager.GetLogger("Log4ALALogger_1");
        private static ILog alaLogger2 = LogManager.GetLogger("Log4ALALogger_2");
        private static ILog alaLogger3 = LogManager.GetLogger("Log4ALALogger_3");

        static void Main(string[] args)
        {
 
            for (int i = 0; i < 10; i++)
            {
                alaLogger1.Info(new { id = $"log-{i}", message = $"test-{i}" });
            }

            System.Console.WriteLine("done1");

            for (int i = 0; i < 10; i++)
            {
                alaLogger2.Info(new { id = $"log-{i}", message = $"test-{i}" });
            }

            System.Console.WriteLine("done2");

            for (int i = 0; i < 10; i++)
            {
                alaLogger3.Info(new { id = $"log-{i}", message = $"test-{i}" });
            }


            System.Console.WriteLine("done3");
            System.Threading.Thread.Sleep(new TimeSpan(0, 5, 0));
        }
    }
}
