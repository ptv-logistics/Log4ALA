using log4net;
using System;

namespace Log4ALATest.Core
{

    class LoggerTests
    {

        private static ILog alaLogger2 = LogManager.GetLogger("Log4ALALogger_2");

        static void Main(string[] args)
        {
            ////Log messages with semicolon separated key=value strings...the keys will then be mapped to Azure Log Analytic properties/columns.
            for (int i = 0; i < 100; i++)
            {
                alaLogger2.Info($"id=log-{i}; message=netstandard2-test-{i}; intTest={i}; doubleTest={i}.{5}");
            }

            System.Console.WriteLine("done");

            // sleep to give the Azure Log Analytics (ALA) log4net Appender thread a chance
            // to send the log data HTTP POST request to the Azure Log Analytics Data Collector API
            System.Threading.Thread.Sleep(new TimeSpan(1, 0, 0));

        }
    }
}
