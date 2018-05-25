using log4net;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Log4ALATest
{
    class LoggerTests
    {

        //private static ILog alaLogger1 = LogManager.GetLogger("Log4ALALogger_1");
        private static ILog alaLogger2 = LogManager.GetLogger("Log4ALALogger_2");
        //private static ILog alaLogger3 = LogManager.GetLogger("Log4ALALogger_3");

        static void Main(string[] args)
        {

            //Log message as anonymous type... the properties will then be mapped to Azure Log Analytic properties/columns.
            //for (int i = 0; i < 10; i++)
            //{
            //    alaLogger1.Info(new { id = $"log-{i}", message = $"test-{i}" });
            //}

            //System.Console.WriteLine("done1");
            //LogManager.Shutdown();

            ////Log messages with semicolon separated key=value strings...the keys will then be mapped to Azure Log Analytic properties/columns.
            for (int i = 0; i < 100; i++)
            {
                alaLogger2.Info($"id=log-{i}; message=test-{i}");
            }
          

            System.Console.WriteLine("done2");

            //Log messages with semicolon separated key=value strings and duplicate key detection... the duplicate keys in the following example 
            //will be mapped to Azur Log Analytic properties/columns message_Duplicate0 and message_Duplicate1.

            //int iii = 0;
            //int i = 0;

            //for (int ii = 0; ii < 1000; ii++)
            //{
            //    Task.Run(async () =>
            //    {
            //        System.Console.WriteLine($"sleep{++iii}");
            //        string[] lines = File.ReadAllLines("c:\\users\\mob\\downloads\\EMCore_2017-07-19_001311_2017-07-19_061311.log");
            //        System.Console.WriteLine($"lines {lines.Length}");
            //        //System.Threading.Thread.Sleep(new TimeSpan(0, 0, 2));
            //        System.Console.WriteLine($"run{iii}");

            //    //foreach (var line in lines)
            //    //{


            //        Parallel.ForEach(lines, (line) =>
            //        {
            //        string test = line;//.Replace(" - ", "|").Split("|".ToCharArray())[1];
            //                           //System.Console.WriteLine(test);
            //        alaLogger2.Info($"{test}");
            //        System.Console.WriteLine($"step{i}");
            //        //alaLogger2.Debug($"id=log-{i}; message=test-{i}; message=test-{i}; message=test-{i}");

            //        });
            //    //}
            //        System.Console.WriteLine($"end {iii}");



            //    });




            //}

            //System.Console.WriteLine("done3");

            ////Log message as json string ...the json properties will then be mapped to Azure Log Analytic properties/columns.
            //for (int i = 0; i < 10; i++)
            //{
            //    alaLogger3.Info($"{{\"id\":\"log-{i}\", \"message\":\"test-{i}\"}}");
            //}



            System.Console.WriteLine("done4");

            //System.Threading.Thread.Sleep(new TimeSpan(0, 0, 10));


            //for (int i = 0; i < 100; i++)
            //{
            //    alaLogger2.Info($"id=log-{i}; message=test-{i}; percent={i}.{i}; count={i}; testdate={DateTime.Now.ToString()};testdate2={DateTime.Now.ToString("o")};testdate4=Monday, July 10, 2017 8:47 AM;testbool={Boolean.TrueString};testbool2=True");
            //}


            //for (int i = 0; i < 100; i++)
            //{
            //    System.Threading.Thread.Sleep(new TimeSpan(0, 0, 4));
            //    System.Console.WriteLine($"{i}...");
            //}


            System.Threading.Thread.Sleep(new TimeSpan(1, 5, 0));

        }
    }
}
