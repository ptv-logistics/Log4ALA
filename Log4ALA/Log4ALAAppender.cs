using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Log4ALA
{
    public class Log4ALAAppender : AppenderSkeleton
    {
        private static ILog log;
        private static ILog extraLog;
        public static bool isJobManagerInitialized = false;

        protected static readonly Random Random1 = new Random();
        // Minimal delay between attempts to reconnect in milliseconds. 
        protected const int MinDelay = 2500;

        // Maximal delay between attempts to reconnect in milliseconds. 
        protected const int MaxDelay = 80000;

        private const string LOG_ERR_APPENDER = "Log4ALAErrorAppender";
        private const string LOG_INFO_APPENDER = "Log4ALAInfoAppender";

        private const string LOG_ERR_DEFAULT_FILE = "log4ALA_error.log";
        private const string LOG_INFO_DEFAULT_FILE = "log4ALA_info.log";


        private LoggingEventSerializer serializer;

        private QueueLogger queueLogger;

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

        public string ErrAppenderFile { get; set; }
        public string InfoAppenderFile { get; set; }


        static Log4ALAAppender()
        {
        }


        public override void ActivateOptions()
        {

            try
            {

                queueLogger = new QueueLogger();

                logMessageToFile = LogMessageToFile;


                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Log4ALA.internalLog4net.config"))
                {
                    XmlConfigurator.Configure(stream);
                }

                log = LogManager.GetLogger("Log4ALAInternalLogger");

                string setErrAppFileNameMessage, setInfoAppFileNameMessage;
                bool isErrFile = SetAppenderFileNameIfAvailable(ErrAppenderFile, LOG_ERR_APPENDER, out setErrAppFileNameMessage);
                bool isInfoFile = SetAppenderFileNameIfAvailable(InfoAppenderFile, LOG_INFO_APPENDER, out setInfoAppFileNameMessage);

                if (isErrFile)
                {
                    Info(setErrAppFileNameMessage);
                }
                else
                {
                    Error(setErrAppFileNameMessage, false);
                }

                if (isInfoFile)
                {
                    Info(setInfoAppFileNameMessage);
                }
                else
                {
                    Error(setInfoAppFileNameMessage, false);
                }



                if (!string.IsNullOrWhiteSpace(ErrLoggerName))
                {
                    extraLog = LogManager.GetLogger(ErrLoggerName);
                }



                if (string.IsNullOrWhiteSpace(WorkspaceId))
                {
                    throw new Exception($"the Log4ALAAppender property workspaceId [{WorkspaceId}] shouldn't be empty");
                }

                queueLogger.WorkspaceId = WorkspaceId;

                if (string.IsNullOrWhiteSpace(SharedKey))
                {
                    throw new Exception($"the Log4ALAAppender property sharedKey [{SharedKey}] shouldn't be empty");
                }

                queueLogger.SharedKey = SharedKey;

                if (string.IsNullOrWhiteSpace(LogType))
                {
                    throw new Exception($"the Log4ALAAppender property logType [{LogType}] shouldn't be empty");
                }

                queueLogger.LogType = LogType;

                if (string.IsNullOrWhiteSpace(AzureApiVersion))
                {
                    AzureApiVersion = "2016-04-01";
                }

                queueLogger.AzureApiVersion = AzureApiVersion;

                if (HttpDataCollectorRetry == null)
                {
                    HttpDataCollectorRetry = 6;
                }

                queueLogger.HttpDataCollectorRetry = HttpDataCollectorRetry;

                serializer = new LoggingEventSerializer();


            }
            catch (Exception ex)
            {
                Error($"Unable to activate Log4ALAAppender: {ex.Message}");
            }
        }

        private bool SetAppenderFileNameIfAvailable(string appenderFile, string internalAppenderName, out string errMessage)
        {
            errMessage = null;

            try
            {
                var appender = (log4net.Appender.RollingFileAppender)LogManager.GetRepository().GetAppenders().Where(ap => ap.Name.Equals(internalAppenderName)).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(appenderFile))
                {
                    String dir = Path.GetDirectoryName(appenderFile);

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }


                    if (appender != null && !string.IsNullOrWhiteSpace(appender.Name))
                    {
                        appender.File = appenderFile;
                        appender.ActivateOptions();
                    }

                    errMessage = $"successfully configured {internalAppenderName} with file {appender.File}";
                }
                else
                {

                    if (appender != null && !string.IsNullOrWhiteSpace(appender.Name))
                    {
                        if (internalAppenderName.Equals(LOG_ERR_APPENDER))
                        {
                            appender.File = LOG_ERR_DEFAULT_FILE;
                        }
                        else
                        {
                            appender.File = LOG_INFO_DEFAULT_FILE;

                        }
                        appender.ActivateOptions();
                    }

                    errMessage = $"No expicit file configuration ({(internalAppenderName.Equals(LOG_ERR_APPENDER) ? "errAppenderFile" : "infoAppenderFile")}) found for {internalAppenderName} use ({appender.File}) as default";
                }
                return true;
            }
            catch (Exception e)
            {
                errMessage = $"Error during configuration of the internal {internalAppenderName} with file {appenderFile} : {e.Message}";
                return false;
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            try
            {
                if (queueLogger != null)
                {
                    var content = serializer.SerializeLoggingEvents(new[] { loggingEvent }, this);
                    Info(content);
                    queueLogger.AddLine(content);
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
                log.Error(logMessage);
                if (extraLog != null)
                {
                    extraLog.Error(logMessage);
                }
            }
        }

        public static void Info(string logMessage)
        {
            if (logMessageToFile && log != null)
            {
                log.Info(logMessage);
            }
        }

        public static void Warn(string logMessage)
        {
            if (log != null)
            {
                log.Warn(logMessage);
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

        protected override void OnClose()
        {
            if (queueLogger != null)
            {
                queueLogger.interruptWorker();
            }
        }




    }


    public class ALARollingFileAppender : RollingFileAppender
    {
        private bool isFirstTime = true;
        protected override void OpenFile(string fileName, bool append)
        {
            if (isFirstTime)
            {
                isFirstTime = false;
                return;
            }

            base.OpenFile(fileName, append);
        }
    }

}
