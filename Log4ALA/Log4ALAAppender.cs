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
  
        private LoggingEventSerializer serializer;

        private QueueLogger queueLogger;

        public ConfigSettings configSettings;

        public string WorkspaceId { get; set; }
        public string SharedKey { get; set; }
        public string LogType { get; set; }
        public string AzureApiVersion { get; set; } = ConfigSettings.DEFAULT_AZURE_API_VERSION;
        public int? HttpDataCollectorRetry { get; set; } = ConfigSettings.DEFAULT_HTTP_DATA_COLLECTOR_RETRY;
        public bool LogMessageToFile { get; set; } = ConfigSettings.DEFAULT_LOG_MESSAGE_TOFILE;
        public bool? AppendLogger { get; set; } = ConfigSettings.DEFAULT_APPEND_LOGGER;
        public bool? AppendLogLevel { get; set; } = ConfigSettings.DEFAULT_APPEND_LOGLEVEL;

        public string ErrLoggerName { get; set; }

        public string ErrAppenderFile { get; set; }
        public string InfoAppenderFile { get; set; }

        // Size of the internal event queue. 
        public int? LoggingQueueSize { get; set; } = ConfigSettings.DEFAULT_LOGGER_QUEUE_SIZE;
        public bool? KeyValueDetection { get; set; } = ConfigSettings.DEFAULT_KEY_VALUE_DETECTION;
        public bool? JsonDetection { get; set; } = ConfigSettings.DEFAULT_JSON_DETECTION;
        public int? BatchSizeInBytes { get; set; } = ConfigSettings.DEFAULT_BATCH_SIZE_BYTES;

        public int? BatchNumItems { get; set; } = ConfigSettings.DEFAULT_BATCH_NUM_ITEMS;
        public int? BatchWaitInSec { get; set; } = ConfigSettings.DEFAULT_BATCH_WAIT_SECONDS;
        public int? BatchWaitMaxInSec { get; set; } = ConfigSettings.DEFAULT_BATCH_WAIT_MAX_SECONDS;
 
        public Log4ALAAppender()
        {
        }


        public override void ActivateOptions()
        {

            try
            {
                configSettings = new ConfigSettings(this.Name);

                LogMessageToFile = configSettings.ALALogMessageToFile == null ? LogMessageToFile : (bool)configSettings.ALALogMessageToFile;


                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Log4ALA.internalLog4net.config"))
                {
                    XmlConfigurator.Configure(stream);
                }

                log = LogManager.GetLogger("Log4ALAInternalLogger");

                string setErrAppFileNameMessage, setInfoAppFileNameMessage;
                bool isErrFile = SetAppenderFileNameIfAvailable(string.IsNullOrWhiteSpace(configSettings.ALAErrAppenderFile) ? ErrAppenderFile : configSettings.ALAErrAppenderFile, ConfigSettings.LOG_ERR_APPENDER, out setErrAppFileNameMessage);
                bool isInfoFile = SetAppenderFileNameIfAvailable(string.IsNullOrWhiteSpace(configSettings.ALAInfoAppenderFile) ? InfoAppenderFile : configSettings.ALAInfoAppenderFile, ConfigSettings.LOG_INFO_APPENDER, out setInfoAppFileNameMessage);

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
                log.Inf($"[{this.Name}] - logMessageToFile:[{LogMessageToFile}]", true);


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

                WorkspaceId = string.IsNullOrWhiteSpace(configSettings.ALAWorkspaceId) ? WorkspaceId : configSettings.ALAWorkspaceId;
                log.Inf($"[{this.Name}] - workspaceId:[{WorkspaceId}]", true);


                if (string.IsNullOrWhiteSpace(configSettings.ALASharedKey) && string.IsNullOrWhiteSpace(SharedKey))
                {
                    throw new Exception($"the Log4ALAAppender property sharedKey [{SharedKey}] shouldn't be empty");
                }

                SharedKey = string.IsNullOrWhiteSpace(configSettings.ALASharedKey) ? SharedKey : configSettings.ALASharedKey;
                log.Inf($"[{this.Name}] - sharedKey:[{SharedKey.Remove(15)}...]", true);

                if (string.IsNullOrWhiteSpace(configSettings.ALALogType) && string.IsNullOrWhiteSpace(LogType))
                {
                    throw new Exception($"the Log4ALAAppender property logType [{LogType}] shouldn't be empty");
                }

                LogType = string.IsNullOrWhiteSpace(configSettings.ALALogType) ? LogType : configSettings.ALALogType;
                log.Inf($"[{this.Name}] - logType:[{LogType}]", true);


                AzureApiVersion = string.IsNullOrWhiteSpace(configSettings.ALAAzureApiVersion) ? AzureApiVersion : configSettings.ALAAzureApiVersion;
                log.Inf($"[{this.Name}] - azureApiVersion:[{AzureApiVersion}]", true);

                HttpDataCollectorRetry = configSettings.ALAHttpDataCollectorRetry == null ? HttpDataCollectorRetry : configSettings.ALAHttpDataCollectorRetry;
                log.Inf($"[{this.Name}] - httpDataCollectorRetry:[{HttpDataCollectorRetry}]", true);

                BatchSizeInBytes = configSettings.ALABatchSizeInBytes == null ? BatchSizeInBytes : configSettings.ALABatchSizeInBytes;
                log.Inf($"[{this.Name}] - batchSizeInBytes:[{BatchSizeInBytes}]", true);

                BatchNumItems = configSettings.ALABatchNumItems == null ? BatchNumItems : configSettings.ALABatchNumItems;

                BatchWaitInSec = configSettings.ALABatchWaitInSec == null ? BatchWaitInSec : configSettings.ALABatchWaitInSec;
                log.Inf($"[{this.Name}] - batchWaitInSec:[{BatchWaitInSec}]", true);

                BatchWaitMaxInSec = configSettings.ALABatchWaitMaxInSec == null ? BatchWaitMaxInSec : configSettings.ALABatchWaitMaxInSec;
                log.Inf($"[{this.Name}] - batchWaitMaxInSec:[{BatchWaitMaxInSec}]", true);

                if(BatchSizeInBytes > 0 || BatchWaitInSec > 0)
                {
                    BatchNumItems = 0;
                }

                log.Inf($"[{this.Name}] - batchNumItems:[{BatchNumItems}]", true);

                serializer = new LoggingEventSerializer();

                AppendLogger = configSettings.ALAAppendLogger == null ? AppendLogger : (bool)configSettings.ALAAppendLogger;
                log.Inf($"[{this.Name}] - appendLogger:[{AppendLogger}]", true);

                AppendLogLevel = configSettings.ALAAppendLogLevel == null ? AppendLogLevel : (bool)configSettings.ALAAppendLogLevel;
                log.Inf($"[{this.Name}] - appendLogLevel:[{AppendLogLevel}]", true);

                KeyValueDetection = configSettings.ALAKeyValueDetection == null ? KeyValueDetection : (bool)configSettings.ALAKeyValueDetection;
                log.Inf($"[{this.Name}] - keyValueDetection:[{KeyValueDetection}]", true);

                JsonDetection = configSettings.ALAJsonDetection == null ? JsonDetection : (bool)configSettings.ALAJsonDetection;
                log.Inf($"[{this.Name}] - jsonDetecton:[{JsonDetection}]", true);

                log.Inf($"[{this.Name}] - alaQueueSizeLogIntervalEnabled:[{ConfigSettings.IsLogQueueSizeInterval}]", true);
                log.Inf($"[{this.Name}] - alaQueueSizeLogIntervalInSec:[{ConfigSettings.LogQueueSizeInterval}]", true);

                queueLogger = new QueueLogger(this);

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
                        if (internalAppenderName.Equals(ConfigSettings.LOG_ERR_APPENDER))
                        {
                            appender.File = ConfigSettings.LOG_ERR_DEFAULT_FILE;
                        }
                        else
                        {
                            appender.File = ConfigSettings.LOG_INFO_DEFAULT_FILE;

                        }
                        appender.ActivateOptions();
                    }

                    errMessage = $"[{this.Name}] - No explicit file configuration ({(internalAppenderName.Equals(ConfigSettings.LOG_ERR_APPENDER) ? "errAppenderFile" : "infoAppenderFile")}) found for {internalAppenderName} use ({appender.File}) as default";
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