using log4net;
using log4net.Appender;
using System;
using System.Reflection;
using log4net.Core;
using log4net.Config;
using System.Threading;
using FluentScheduler;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace Log4ALA
{
    public class Log4ALAAppender : AppenderSkeleton
    {
        private static ILog log;
        private static ILog extraLog;
        public static bool isJobManagerInitialized = false;
        private static RuntimeContext runtimeContext = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile.ToLower().EndsWith("web.config") ? RuntimeContext.WEB_APP : RuntimeContext.CONSOLE_APP;

        protected static readonly Random Random1 = new Random();
        // Minimal delay between attempts to reconnect in milliseconds. 
        protected const int MinDelay = 2500;

        // Maximal delay between attempts to reconnect in milliseconds. 
        protected const int MaxDelay = 80000;


        private LoggingEventSerializer serializer;

        private HTTPDataCollectorAPI.Collector httpDataCollectorAPI;

        public string WorkspaceId { get; set; }
        public string SharedKey { get; set; }
        public string LogType { get; set; }
        public string AzureApiVersion { get; set; }
        public int? HttpDataCollectorRetry { get; set; }

        private static bool logMessageToFile = false;
        public bool LogMessageToFile { get; set; }
        public bool? AppendLogger { get; set; }
        public bool? AppendLogLevel { get; set; }

        public string ErrLoggerName { get; set; }


        static Log4ALAAppender()
        {
        }


        public override void ActivateOptions()
        {

            try
            {
                if (runtimeContext.Equals(RuntimeContext.WEB_APP))
                {
                    InitJobManager();
                }

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Log4ALA.internalLog4net.config"))
                {
                    XmlConfigurator.Configure(stream);
                }

                log = LogManager.GetLogger("Log4ALAInternalLogger");

                if (!string.IsNullOrWhiteSpace(ErrLoggerName))
                {
                    extraLog = LogManager.GetLogger(ErrLoggerName);
                }


                logMessageToFile = LogMessageToFile;

                if (string.IsNullOrWhiteSpace(WorkspaceId))
                {
                    throw new Exception($"the Log4ALAAppender property workspaceId [{WorkspaceId}] shouldn't be empty");
                }

                if (string.IsNullOrWhiteSpace(SharedKey))
                {
                    throw new Exception($"the Log4ALAAppender property sharedKey [{SharedKey}] shouldn't be empty");
                }

                if (string.IsNullOrWhiteSpace(LogType))
                {
                    throw new Exception($"the Log4ALAAppender property logType [{LogType}] shouldn't be empty");
                }

                if (string.IsNullOrWhiteSpace(AzureApiVersion))
                {
                    AzureApiVersion = "2016-04-01";
                }

                if (HttpDataCollectorRetry == null)
                {
                    HttpDataCollectorRetry = 6;
                }

                httpDataCollectorAPI = new HTTPDataCollectorAPI.Collector(WorkspaceId, SharedKey);

                serializer = new LoggingEventSerializer();

            }
            catch (Exception ex)
            {
                Error($"Unable to activate Log4ALAAppender: {ex.Message}");
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            try
            {
                if (httpDataCollectorAPI != null)
                {
                    var content = serializer.SerializeLoggingEvents(new[] { loggingEvent }, this);
                    Info(content);

                    if (runtimeContext.Equals(RuntimeContext.CONSOLE_APP))
                    {
                        Task.Run(async () => {
                            try
                            {
                                await httpDataCollectorAPI.Collect(LogType, content, AzureApiVersion, "DateValue");
                            }
                            catch (Exception e)
                            {

                                int connectRetries = 0;
                                var rootDelay = MinDelay;

                                while (connectRetries <= HttpDataCollectorRetry)
                                {
                                    Warn($"HTTPDataCollectorAPI retry NumRetries:[{connectRetries}] exception:{FlattenException(e)}");

                                    rootDelay *= 2;
                                    if (rootDelay > MaxDelay)
                                        rootDelay = MaxDelay;

                                    var waitFor = rootDelay + Random1.Next(rootDelay);
                                    try
                                    {
                                        Thread.Sleep(waitFor);
                                    }
                                    catch
                                    {
                                    }
                                    connectRetries++;

                                    try
                                    {
                                        await httpDataCollectorAPI.Collect(LogType, content, AzureApiVersion, "DateValue");
                                    }
                                    catch (Exception)
                                    {
                                        continue;
                                    }
                                }
                                Error($"HTTPDataCollectorAPI failed after retry NumRetries:[{connectRetries}] exception:{FlattenException(e)}", false);
                            }

                        }).ContinueWith(t =>
                        {
                            var exception = t.Exception.InnerException;
                            if(exception != null)
                            {
                                Error($"HTTPDataCollectorAPI job exception [{exception.Message}]", async: false);
                            }
                        },TaskContinuationOptions.OnlyOnFaulted);

                    }
                    else
                    {
                        //How to run Background Tasks in ASP.NET
                        //http://www.hanselman.com/blog/HowToRunBackgroundTasksInASPNET.aspx
                        JobManager.AddJob(async () =>
                        {
                            try
                            {
                                await httpDataCollectorAPI.Collect(LogType, content, AzureApiVersion, "DateValue");
                            }
                            catch (Exception e)
                            {

                                int connectRetries = 0;
                                var rootDelay = MinDelay;

                                while (connectRetries <= HttpDataCollectorRetry)
                                {
                                    Warn($"HTTPDataCollectorAPI retry NumRetries:[{connectRetries}] exception:{FlattenException(e)}");

                                    rootDelay *= 2;
                                    if (rootDelay > MaxDelay)
                                        rootDelay = MaxDelay;

                                    var waitFor = rootDelay + Random1.Next(rootDelay);
                                    try
                                    {
                                        Thread.Sleep(waitFor);
                                    }
                                    catch
                                    {
                                    }
                                    connectRetries++;

                                    try
                                    {
                                        await httpDataCollectorAPI.Collect(LogType, content, AzureApiVersion, "DateValue");
                                    }
                                    catch (Exception)
                                    {
                                        continue;
                                    }
                                }
                                Error($"HTTPDataCollectorAPI failed after retry NumRetries:[{connectRetries}] exception:{FlattenException(e)}", false);
                            }
                        }, (s) => { s.WithName($"{Guid.NewGuid().ToString()}_AppendLog"); s.ToRunNow(); });

                    }
                }
            }
            catch (Exception ex)
            {
                Error($"Unable to send data to Azure Log Analytics: {ex.Message}");
            }
        }


        public static void Error(string logMessage, bool async = true)
        {
            if (log != null)
            {
                if (async)
                {
                    //http://www.ben-morris.com/using-asynchronous-log4net-appenders-for-high-performance-logging/
                    ThreadPool.QueueUserWorkItem(task => log.Error(logMessage));
                    if (extraLog != null)
                    {
                        if (runtimeContext.Equals(RuntimeContext.CONSOLE_APP))
                        {
                            //http://www.ben-morris.com/using-asynchronous-log4net-appenders-for-high-performance-logging/
                            ThreadPool.QueueUserWorkItem(task => extraLog.Error(logMessage));
                        }
                        else
                        {
                            //How to run Background Tasks in ASP.NET
                            //http://www.hanselman.com/blog/HowToRunBackgroundTasksInASPNET.aspx
                            JobManager.AddJob(() =>
                            {
                                extraLog.Error(logMessage);
                            }, (s) => { s.WithName($"{Guid.NewGuid().ToString()}_LogError"); s.ToRunNow(); });
                        }
                    }
                }
                else
                {
                    log.Error(logMessage);
                    if (extraLog != null)
                    {
                        extraLog.Error(logMessage);
                    }

                }
            }
        }

        public static void Info(string logMessage)
        {
            if (logMessageToFile && log != null)
            {
                if (runtimeContext.Equals(RuntimeContext.CONSOLE_APP))
                {
                    //http://www.ben-morris.com/using-asynchronous-log4net-appenders-for-high-performance-logging/
                    ThreadPool.QueueUserWorkItem(task => log.Info(logMessage));
                }
                else
                {
                    //How to run Background Tasks in ASP.NET
                    //http://www.hanselman.com/blog/HowToRunBackgroundTasksInASPNET.aspx
                    JobManager.AddJob(() =>
                    {
                        log.Info(logMessage);
                    }, (s) => { s.WithName($"{Guid.NewGuid().ToString()}_LogInfo"); s.ToRunNow(); });
                }
            }
        }

        public static void Warn(string logMessage)
        {
            if (log != null)
            {
                if (runtimeContext.Equals(RuntimeContext.CONSOLE_APP))
                {
                    //http://www.ben-morris.com/using-asynchronous-log4net-appenders-for-high-performance-logging/
                    ThreadPool.QueueUserWorkItem(task => log.Warn(logMessage));
                }
                else
                {
                    //How to run Background Tasks in ASP.NET
                    //http://www.hanselman.com/blog/HowToRunBackgroundTasksInASPNET.aspx
                    JobManager.AddJob(() =>
                    {
                        log.Warn(logMessage);
                    }, (s) => { s.WithName($"{Guid.NewGuid().ToString()}_LogWarn"); s.ToRunNow(); });
                }
            }
        }

        public static void InitJobManager()
        {
            if (!isJobManagerInitialized)
            {
                isJobManagerInitialized = true;
                JobManager.JobEnd += JobEndEvent;
                JobManager.JobException += JobExceptionEvent;
            }
        }

        private static void JobExceptionEvent(JobExceptionInfo obj)
        {
            if (obj.Exception != null)
            {
                Error($"Log4ALA JobException of job [{obj.Name}]  - [{obj.Exception.StackTrace}]", async: false);
                string jobName = obj.Name;
                RemoveJobManagerJob(jobName);
            }
        }

        public static void JobEndEvent(JobEndInfo job)
        {
            string jobName = job.Name;
            RemoveJobManagerJob(jobName);
        }

        private static void RemoveJobManagerJob(string jobName)
        {
            try
            {
                JobManager.RemoveJob(jobName);
            }
            catch (Exception)
            {
                Error($"Log4ALA job [{jobName}] couldn't removed", async: false);
            }
        }

        private static string FlattenException(Exception exception, bool logExcType = true, bool isStackTraceIgnored = false)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {

                if (logExcType)
                {
                    stringBuilder.AppendLine(string.Format("ExceptionType:[{0}]", exception.GetType().FullName));
                }
                stringBuilder.AppendLine(string.Format("Message:[{0}]", exception.Message));

                if (!isStackTraceIgnored)
                {
                    stringBuilder.AppendLine(string.Format("StackTrace:[{0}]", exception.StackTrace));
                }

                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }



    }

    public enum RuntimeContext
    {
        //Default
        CONSOLE_APP = 0,

        WEB_APP = 1
    }



}
