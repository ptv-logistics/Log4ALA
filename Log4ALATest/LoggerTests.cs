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

            //Log message as anonymous type... the properties will then be mapped to Azure Log Analytic properties/columns.
            for (int i = 0; i < 10; i++)
            {
                alaLogger1.Info(new { id = $"log-{i}", message = $"test-{i}" });
            }

            System.Console.WriteLine("done1");

            //Log messages with semicolon separated key=value strings...the keys will then be mapped to Azure Log Analytic properties/columns.
            for (int i = 0; i < 10; i++)
            {
                alaLogger2.Info($"id=log-{i}; message=test-{i}");
            }

            System.Console.WriteLine("done2");

            //Log messages with semicolon separated key=value strings and duplicate key detection... the duplicate keys in the following example 
            //will be mapped to Azur Log Analytic properties/columns message_Duplicate0 and message_Duplicate1.
            for (int i = 0; i < 10; i++)
            {
                alaLogger2.Info($"id=log-{i}; message=test-{i}; message=test-{i}; message=test-{i}");
            }

            System.Console.WriteLine("done3");

            //Log message as json string ...the json properties will then be mapped to Azure Log Analytic properties/columns.
            for (int i = 0; i < 10; i++)
            {
                alaLogger3.Info($"{{\"id\":\"log-{i}\", \"message\":\"test-{i}\"}}");
            }

            System.Console.WriteLine("done4");

            System.Threading.Thread.Sleep(new TimeSpan(0, 5, 0));
        }
    }
}
