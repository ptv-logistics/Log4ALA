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
        public ILog log;
        public ILog extraLog;

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

        public ConfigSettings configSettings;

        public string WorkspaceId { get; set; }
        public string SharedKey { get; set; }
        public string LogType { get; set; }
        public string AzureApiVersion { get; set; }
        public int? HttpDataCollectorRetry { get; set; }

        public bool logMessageToFile = false;
        public bool LogMessageToFile { get; set; }

        public bool appendLogger = false;
        public bool? AppendLogger { get; set; }
 
        public bool appendLogLevel = false;
        public bool? AppendLogLevel { get; set; }

        public string ErrLoggerName { get; set; }

        public string ErrAppenderFile { get; set; }
        public string InfoAppenderFile { get; set; }

        public int? LoggingQueueSize { get; set; }

        public bool keyValueDetection = false;
        public bool? KeyValueDetection { get; set; }

        public bool jsonDetection = false;
        public bool? JsonDetection { get; set; }

        public Log4ALAAppender()
        {
        }


        public override void ActivateOptions()
        {

            try
            {
                configSettings = new ConfigSettings(this.Name);
                queueLogger = new QueueLogger(this);

                logMessageToFile = configSettings.ALALogMessageToFile != null ? (bool)configSettings.ALALogMessageToFile : LogMessageToFile;

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Log4ALA.internalLog4net.config"))
                {
                    XmlConfigurator.Configure(stream);
                }

                log = LogManager.GetLogger("Log4ALAInternalLogger");

                string setErrAppFileNameMessage, setInfoAppFileNameMessage;
                bool isErrFile = SetAppenderFileNameIfAvailable(string.IsNullOrWhiteSpace(configSettings.ALAErrAppenderFile) ? ErrAppenderFile : configSettings.ALAErrAppenderFile, LOG_ERR_APPENDER, out setErrAppFileNameMessage);
                bool isInfoFile = SetAppenderFileNameIfAvailable(string.IsNullOrWhiteSpace(configSettings.ALAInfoAppenderFile) ? InfoAppenderFile : configSettings.ALAInfoAppenderFile, LOG_INFO_APPENDER, out setInfoAppFileNameMessage);

                if (isErrFile)
                {
                    log.Inf(setErrAppFileNameMessage, true);
                }
                else
                {
                    log.Err(setErrAppFileNameMessage);
                    extraLog.Err(setErrAppFileNameMessage);
                }

                if (isInfoFile)
                {
                    log.Inf(setInfoAppFileNameMessage, true);
                }
                else
                {
                    log.Err(setInfoAppFileNameMessage);
                    extraLog.Err(setInfoAppFileNameMessage);
                }

                log.Inf($"[{this.Name}] - loggerName:[{this.Name}]", true);
                log.Inf($"[{this.Name}] - logMessageToFile:[{logMessageToFile}]", true);


                if (!string.IsNullOrWhiteSpace(configSettings.ALAErrLoggerName))
                {
                    extraLog = LogManager.GetLogger(configSettings.ALAErrLoggerName);
                    log.Inf($"[{this.Name}] - errLoggerName:[{configSettings.ALAErrLoggerName}]", true);
                }
                else if (!string.IsNullOrWhiteSpace(ErrLoggerName))
                {
                    extraLog = LogManager.GetLogger(ErrLoggerName);
                    log.Inf($"[{this.Name}] - errLoggerName:[{ErrLoggerName}]", true);
                }




                if (string.IsNullOrWhiteSpace(configSettings.ALAWorkspaceId) && string.IsNullOrWhiteSpace(WorkspaceId))
                {
                    throw new Exception($"the Log4ALAAppender property workspaceId [{WorkspaceId}] shouldn't be empty");
                }

                queueLogger.WorkspaceId = string.IsNullOrWhiteSpace(configSettings.ALAWorkspaceId) ? WorkspaceId : configSettings.ALAWorkspaceId;
                log.Inf($"[{this.Name}] - workspaceId:[{queueLogger.WorkspaceId}]", true);


                if (string.IsNullOrWhiteSpace(configSettings.ALASharedKey) && string.IsNullOrWhiteSpace(SharedKey))
                {
                    throw new Exception($"the Log4ALAAppender property sharedKey [{SharedKey}] shouldn't be empty");
                }

                queueLogger.SharedKey = string.IsNullOrWhiteSpace(configSettings.ALASharedKey) ? SharedKey : configSettings.ALASharedKey;
                log.Inf($"[{this.Name}] - sharedKey:[{queueLogger.SharedKey.Remove(15)}...]", true);

                if (string.IsNullOrWhiteSpace(configSettings.ALALogType) && string.IsNullOrWhiteSpace(LogType))
                {
                    throw new Exception($"the Log4ALAAppender property logType [{LogType}] shouldn't be empty");
                }

                queueLogger.LogType = string.IsNullOrWhiteSpace(configSettings.ALALogType) ? LogType : configSettings.ALALogType;
                log.Inf($"[{this.Name}] - logType:[{queueLogger.LogType}]", true);


                queueLogger.AzureApiVersion = string.IsNullOrWhiteSpace(configSettings.ALAAzureApiVersion) ? (string.IsNullOrWhiteSpace(AzureApiVersion) ? "2016-04-01" : AzureApiVersion) : configSettings.ALAAzureApiVersion;
                log.Inf($"[{this.Name}] - azureApiVersion:[{queueLogger.AzureApiVersion}]", true);

                queueLogger.HttpDataCollectorRetry = configSettings.ALAHttpDataCollectorRetry == null ? (HttpDataCollectorRetry == null ? 6 : HttpDataCollectorRetry) : configSettings.ALAHttpDataCollectorRetry;
                log.Inf($"[{this.Name}] - httpDataCollectorRetry:[{queueLogger.HttpDataCollectorRetry}]", true);

                queueLogger.LoggingQueueSize = configSettings.ALALoggingQueueSize != null && configSettings.ALALoggingQueueSize > 0 ? configSettings.ALALoggingQueueSize : (LoggingQueueSize != null && LoggingQueueSize > 0 ? LoggingQueueSize : ConfigSettings.DEFAULT_LOGGER_QUEUE_SIZE);
                log.Inf($"[{this.Name}] - loggingQueueSize:[{queueLogger.LoggingQueueSize}]", true);

                serializer = new LoggingEventSerializer();

                if ((configSettings.ALAAppendLogger == null || (bool)configSettings.ALAAppendLogger) && (AppendLogger == null || (bool)AppendLogger))
                {
                    this.appendLogger = true;
                }

                log.Inf($"[{this.Name}] - appendLogger:[{this.appendLogger}]", true);

                if ((configSettings.ALAAppendLogLevel == null || (bool)configSettings.ALAAppendLogLevel) && (AppendLogLevel == null || (bool)AppendLogLevel))
                {
                    this.appendLogLevel = true;
                }
                log.Inf($"[{this.Name}] - appendLogLevel:[{this.appendLogLevel}]", true);

                if ((configSettings.ALAKeyValueDetection == null || (bool)configSettings.ALAKeyValueDetection) && (KeyValueDetection == null || (bool)KeyValueDetection))
                {
                    this.keyValueDetection = true;
                }
                log.Inf($"[{this.Name}] - keyValueDetection:[{this.keyValueDetection}]", true);

                if ((configSettings.ALAJsonDetection == null || (bool)configSettings.ALAJsonDetection) && (JsonDetection == null || (bool)JsonDetection))
                {
                    this.jsonDetection = true;
                }
                log.Inf($"[{this.Name}] - jsonDetecton:[{this.jsonDetection}]", true);


                log.Inf($"[{this.Name}] - alaQueueSizeLogIntervalEnabled:[{ConfigSettings.IsLogQueueSizeInterval}]", true);
                log.Inf($"[{this.Name}] - alaQueueSizeLogIntervalInMin:[{ConfigSettings.LogQueueSizeInterval}]", true);


            }
            catch (Exception ex)
            {
                queueLogger = null;
                string message = $"[{this.Name}] - Unable to activate Log4ALAAppender: [{ex.Message}]";
                log.Err(message);
                extraLog.Err(message);
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

                    errMessage = $"[{this.Name}] - successfully configured {internalAppenderName} with file {appender.File}";
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

                    errMessage = $"[{this.Name}] - No explicit file configuration ({(internalAppenderName.Equals(LOG_ERR_APPENDER) ? "errAppenderFile" : "infoAppenderFile")}) found for {internalAppenderName} use ({appender.File}) as default";
                }
                 return true;
            }
            catch (Exception e)
            {
                errMessage = $"[{this.Name}] - Error during configuration of the internal {internalAppenderName} with file {appenderFile} : {e.Message}";
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
                    queueLogger.AddLine(content);
                }
            }
            catch (Exception ex)
            {
                string message = $"[{this.Name}] - Unable to send data to Azure Log Analytics: {ex.Message}";
                log.Err(message);
                extraLog.Err(message);
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


    public static class LogExtensions
    {

        public static void Err(this ILog log, string logMessage)
        {
            if (log != null)
            {
                log.Error(logMessage);
            }
        }

        public static void Inf(this ILog log, string logMessage, bool logMessage2File = false)
        {
            if (logMessage2File && log != null)
            {
                log.Info(logMessage);
            }
        }

        public static void War(this ILog log, string logMessage)
        {
            if (log != null)
            {
                log.Warn(logMessage);
            }
        }

    }
}