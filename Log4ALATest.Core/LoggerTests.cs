﻿using log4net;
using System;
using log4net.Config;
using System.IO;
using log4net.Repository;
using System.Reflection;

namespace Log4ALATest.Core
{

    class LoggerTests
    {
        private static ILoggerRepository REPOSITORY = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

        private static ILog alaLogger2 = LogManager.GetLogger(REPOSITORY.Name, "Log4ALALogger_2");

        static void Main(string[] args)
        {
            XmlConfigurator.Configure(REPOSITORY, new FileInfo("log4net.config"));

            ////Log messages with [;] separated key[=]value strings...the keys will then be mapped to Azure Log Analytic properties/columns.
            for (int i = 0; i < 3; i++)
            {
                alaLogger2.Info($" message[=]netstandard2-test-{i}[;] id[=]log{i}[;] intTest[=]{i}[;] doubleTest[=]{i}={5}");
            }


            System.Console.WriteLine("done");


            System.Threading.Thread.Sleep(new TimeSpan(1, 5, 0));


        }
    }
}
