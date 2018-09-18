using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
#if NETSTANDARD2_0 || NETCOREAPP2_0
using log4net.Repository;
#endif
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

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
        public bool DisableInfoLogFile { get; set; } = ConfigSettings.DEFAULT_DISABLE_INFO_APPENDER_FILE;
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
        public int? MaxFieldByteLength { get; set; } = ConfigSettings.DEFAULT_MAX_FIELD_BYTE_LENGTH;
        public int? MaxFieldNameLength { get; set; } = ConfigSettings.DEFAULT_MAX_FIELD_NAME_LENGTH;

        public string KeyValueSeparator { get; set; } = ConfigSettings.DEFAULT_KEY_VALUE_SEPARATOR;
        public string KeyValuePairSeparator { get; set; } = ConfigSettings.DEFAULT_KEY_VALUE_PAIR_SEPARATOR;

        public string[] KeyValueSeparators { get; set; } = null;
        public string[] KeyValuePairSeparators { get; set; } = null;


#if NETSTANDARD2_0 || NETCOREAPP2_0
        private static ILoggerRepository REPOSITORY = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
#endif



        public CoreFieldNames coreFields;

        private string coreFieldNames;
        public string CoreFieldNames {
            get {
                return coreFieldNames;
            }
            set
            {
                string tempValue;
                if (string.IsNullOrWhiteSpace(value))
                {
                    tempValue = JsonConvert.SerializeObject(new CoreFieldNames() { DateFieldName = ConfigSettings.DEFAULT_DATE_FIELD_NAME, MiscMessageFieldName = ConfigSettings.DEFAULT_MISC_MSG_FIELD_NAME, LevelFieldName = ConfigSettings.DEFAULT_LEVEL_FIELD_NAME, LoggerFieldName = ConfigSettings.DEFAULT_LOGGER_FIELD_NAME });
                }
                else
                {
                    tempValue = value;
                }

                coreFields = JsonConvert.DeserializeObject<CoreFieldNames>(tempValue.Replace("'", "\""));
                coreFieldNames = tempValue.Replace("'", "\"");
            }
        }

        public string ThreadPriority { get; set; } = ConfigSettings.DEFAULT_THREAD_PRIORITY;
        public int? QueueReadTimeout { get; set; } = ConfigSettings.DEFAULT_QUEUE_READ_TIMEOUT;




        public Log4ALAAppender()
        {
        }


        public override void ActivateOptions()
        {

            try
            {
                configSettings = new ConfigSettings(this.Name);

                LogMessageToFile = configSettings.ALALogMessageToFile == null ? LogMessageToFile : (bool)configSettings.ALALogMessageToFile;
                DisableInfoLogFile = configSettings.ALADisableInfoAppenderFile == null ? DisableInfoLogFile : (bool)configSettings.ALADisableInfoAppenderFile;

                string internalLog4NetConfig = "Log4ALA.internalLog4net.config";
                if (DisableInfoLogFile)
                {
                    internalLog4NetConfig = "Log4ALA.internalLog4netOnlyErr.config";
                }

#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(internalLog4NetConfig))
                {
                    XmlConfigurator.Configure(stream);
                }
                 
                log = LogManager.GetLogger("Log4ALAInternalLogger");

#else
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(internalLog4NetConfig))
                {
                    XmlConfigurator.Configure(LogManager.GetRepository(Assembly.GetEntryAssembly()), stream);
                }

                log = LogManager.GetLogger(REPOSITORY.Name, "Log4ALAInternalLogger");
#endif

                string setErrAppFileNameMessage, setInfoAppFileNameMessage = null;
                bool isErrFile = SetAppenderFileNameIfAvailable(string.IsNullOrWhiteSpace(configSettings.ALAErrAppenderFile) ? ErrAppenderFile : configSettings.ALAErrAppenderFile, ConfigSettings.LOG_ERR_APPENDER, out setErrAppFileNameMessage);

                bool isInfoFile = false;
                if (!DisableInfoLogFile)
                {
                    isInfoFile = SetAppenderFileNameIfAvailable(string.IsNullOrWhiteSpace(configSettings.ALAInfoAppenderFile) ? InfoAppenderFile : configSettings.ALAInfoAppenderFile, ConfigSettings.LOG_INFO_APPENDER, out setInfoAppFileNameMessage);
                }

                if (isErrFile)
                {
                    log.Inf(setErrAppFileNameMessage, true);
                }
                else
                {
                    System.Console.WriteLine(setErrAppFileNameMessage);
                    log.Err(setErrAppFileNameMessage);
                    extraLog.Err(setErrAppFileNameMessage);
                }

                if (isInfoFile)
                {
                    log.Inf(setInfoAppFileNameMessage, true);
                }
                else
                {
                    if (!DisableInfoLogFile)
                    {
                        System.Console.WriteLine(setInfoAppFileNameMessage);
                        log.Err(setInfoAppFileNameMessage);
                        extraLog.Err(setInfoAppFileNameMessage);
                    }
                }
 
                if (!string.IsNullOrWhiteSpace(configSettings.ALAErrLoggerName))
                {
#if NETSTANDARD2_0 || NETCOREAPP2_0
                    extraLog = LogManager.GetLogger(REPOSITORY.Name, configSettings.ALAErrLoggerName);
#else
                    extraLog = LogManager.GetLogger(configSettings.ALAErrLoggerName);
#endif
                    log.Inf($"[{this.Name}] - errLoggerName:[{configSettings.ALAErrLoggerName}]", true);
                }
                else if (!string.IsNullOrWhiteSpace(ErrLoggerName))
                {
#if NETSTANDARD2_0 || NETCOREAPP2_0
                    extraLog = LogManager.GetLogger(REPOSITORY.Name, ErrLoggerName);
#else
                    extraLog = LogManager.GetLogger(ErrLoggerName);
#endif
                    log.Inf($"[{this.Name}] - errLoggerName:[{ErrLoggerName}]", true);
                }

#if NETSTANDARD2_0 || NETCOREAPP2_0
                log.Inf($"[{this.Name}] - appsettings directory:[{ConfigSettings.ContentRootPath}]", true);
                log.Inf($"[{this.Name}] - ASPNETCORE_ENVIRONMENT:[{ConfigSettings.AspNetCoreEnvironment}]", true);
                log.Inf($"[{this.Name}] - APPSETTINGS_SUFFIX:[{ConfigSettings.AppsettingsSuffix}]", true);

                if(ConfigSettings.ALAEnableDebugConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|TRACE|[{this.Name}] - appsettings directory:[{ConfigSettings.ContentRootPath}]");
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|TRACE|[{this.Name}] - ASPNETCORE_ENVIRONMENT:[{ConfigSettings.AspNetCoreEnvironment}]");
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|TRACE|[{this.Name}] - APPSETTINGS_SUFFIX:[{ConfigSettings.AppsettingsSuffix}]");
                }
#endif
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

                CoreFieldNames = string.IsNullOrWhiteSpace(configSettings.ALACoreFieldNames) ? CoreFieldNames : configSettings.ALACoreFieldNames;
                log.Inf($"[{this.Name}] - coreFieldNames:[{CoreFieldNames}]", true);

                ThreadPriority = string.IsNullOrWhiteSpace(configSettings.ALAThreadPriority) ? ThreadPriority : configSettings.ALAThreadPriority;
                ThreadPriority priority;
                if (!Enum.TryParse(ThreadPriority, out priority))
                {
                    throw new Exception($"the Log4ALAAppender wrong threadPriority value [{ThreadPriority}] possible values -> Lowest/BelowNormal/Normal/AboveNormal/Highest");
                }
                log.Inf($"[{this.Name}] - threadPriority:[{ThreadPriority}]", true);

                HttpDataCollectorRetry = configSettings.ALAHttpDataCollectorRetry == null ? HttpDataCollectorRetry : configSettings.ALAHttpDataCollectorRetry;
                log.Inf($"[{this.Name}] - httpDataCollectorRetry:[{HttpDataCollectorRetry}]", true);

                BatchSizeInBytes = configSettings.ALABatchSizeInBytes == null ? BatchSizeInBytes : configSettings.ALABatchSizeInBytes;
                log.Inf($"[{this.Name}] - batchSizeInBytes:[{BatchSizeInBytes}]", true);

                BatchNumItems = configSettings.ALABatchNumItems == null ? BatchNumItems : configSettings.ALABatchNumItems;

                BatchWaitInSec = configSettings.ALABatchWaitInSec == null ? BatchWaitInSec : configSettings.ALABatchWaitInSec;
                log.Inf($"[{this.Name}] - batchWaitInSec:[{BatchWaitInSec}]", true);

                BatchWaitMaxInSec = configSettings.ALABatchWaitMaxInSec == null ? BatchWaitMaxInSec : configSettings.ALABatchWaitMaxInSec;
                log.Inf($"[{this.Name}] - batchWaitMaxInSec:[{BatchWaitMaxInSec}]", true);

                MaxFieldByteLength = configSettings.ALAMaxFieldByteLength == null ? MaxFieldByteLength : configSettings.ALAMaxFieldByteLength;
                if(MaxFieldByteLength > ConfigSettings.DEFAULT_MAX_FIELD_BYTE_LENGTH)
                {
                    MaxFieldByteLength = ConfigSettings.DEFAULT_MAX_FIELD_BYTE_LENGTH;
                }
                log.Inf($"[{this.Name}] - maxFieldByteLength:[{MaxFieldByteLength}]", true);


                MaxFieldNameLength = configSettings.ALAMaxFieldNameLength == null ? MaxFieldNameLength : configSettings.ALAMaxFieldNameLength;
                if (MaxFieldNameLength > ConfigSettings.DEFAULT_MAX_FIELD_NAME_LENGTH)
                {
                    MaxFieldNameLength = ConfigSettings.DEFAULT_MAX_FIELD_BYTE_LENGTH;
                }
                log.Inf($"[{this.Name}] - maxFieldNameLength:[{MaxFieldNameLength}]", true);

                if (BatchSizeInBytes > 0 || BatchWaitInSec > 0)
                {
                    BatchNumItems = 0;
                }

                QueueReadTimeout = configSettings.ALAQueueReadTimeout == null ? QueueReadTimeout : configSettings.ALAQueueReadTimeout;
                log.Inf($"[{this.Name}] - queueReadTimeout:[{QueueReadTimeout}]", true);

                log.Inf($"[{this.Name}] - batchNumItems:[{BatchNumItems}]", true);


                AppendLogger = configSettings.ALAAppendLogger == null ? AppendLogger : (bool)configSettings.ALAAppendLogger;
                log.Inf($"[{this.Name}] - appendLogger:[{AppendLogger}]", true);

                AppendLogLevel = configSettings.ALAAppendLogLevel == null ? AppendLogLevel : (bool)configSettings.ALAAppendLogLevel;
                log.Inf($"[{this.Name}] - appendLogLevel:[{AppendLogLevel}]", true);

                KeyValueDetection = configSettings.ALAKeyValueDetection == null ? KeyValueDetection : (bool)configSettings.ALAKeyValueDetection;
                log.Inf($"[{this.Name}] - keyValueDetection:[{KeyValueDetection}]", true);

                JsonDetection = configSettings.ALAJsonDetection == null ? JsonDetection : (bool)configSettings.ALAJsonDetection;
                log.Inf($"[{this.Name}] - jsonDetecton:[{JsonDetection}]", true);

                log.Inf($"[{this.Name}] - abortTimeoutSeconds:[{ConfigSettings.AbortTimeoutSeconds}]", true);
                log.Inf($"[{this.Name}] - logMessageToFile:[{LogMessageToFile}]", true);

                KeyValueSeparator = string.IsNullOrWhiteSpace(configSettings.ALAKeyValueSeparator) ? KeyValueSeparator : configSettings.ALAKeyValueSeparator;
                log.Inf($"[{this.Name}] - keyValueSeparator:[{KeyValueSeparator}]", true);

                if(KeyValueSeparator.Length > 1 && KeyValueSeparators == null)
                {
                    KeyValueSeparators = new string[] { KeyValueSeparator };
                }
  
                KeyValuePairSeparator = string.IsNullOrWhiteSpace(configSettings.ALAKeyValuePairSeparator) ? KeyValuePairSeparator : configSettings.ALAKeyValuePairSeparator;
                log.Inf($"[{this.Name}] - keyValuePairSeparator:[{KeyValuePairSeparator}]", true);

                if (KeyValuePairSeparator.Length > 1 && KeyValuePairSeparators == null)
                {
                    KeyValuePairSeparators = new string[] { KeyValuePairSeparator };
                }

                log.Inf($"[CommonConfiguration] - alaQueueSizeLogIntervalEnabled:[{ConfigSettings.IsLogQueueSizeInterval}]", true);
                log.Inf($"[CommonConfiguration] - alaQueueSizeLogIntervalInSec:[{ConfigSettings.LogQueueSizeInterval}]", true);
                log.Inf($"[CommonConfiguration] - disableInfoLogFile:[{DisableInfoLogFile}]", true);
                log.Inf($"[CommonConfiguration] - enableDebugConsoleLog:[{ConfigSettings.ALAEnableDebugConsoleLog}]", true);

                serializer = new LoggingEventSerializer(this);

                queueLogger = new QueueLogger(this);

            }
            catch (Exception ex)
            {
                queueLogger = null;
                string message = $"[{this.Name}] - Unable to activate Log4ALAAppender: [{ex.Message}]";
                System.Console.WriteLine(message);
                log.Err(message);
                extraLog.Err(message);
            }
        }

        private bool SetAppenderFileNameIfAvailable(string appenderFile, string internalAppenderName, out string errMessage)
        {
            errMessage = null;

            try
            {
#if NETSTANDARD2_0 || NETCOREAPP2_0
                var appender = (log4net.Appender.RollingFileAppender)LogManager.GetRepository(REPOSITORY.Name).GetAppenders().Where(ap => ap.Name.Equals(internalAppenderName)).FirstOrDefault();
#else
                var appender = (log4net.Appender.RollingFileAppender)LogManager.GetRepository().GetAppenders().Where(ap => ap.Name.Equals(internalAppenderName)).FirstOrDefault();
#endif


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
                    var content = serializer.SerializeLoggingEvents(new[] { loggingEvent });
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
                queueLogger.AbortWorker();
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

        public static void War(this ILog log, string logMessage, bool logMessage2File = false)
        {
            if (logMessage2File && log != null)
            {
                log.Warn(logMessage);
            }
        }

    }

    public class CoreFieldNames
    {
        public string DateFieldName { get; set; } = ConfigSettings.DEFAULT_DATE_FIELD_NAME;
        public string MiscMessageFieldName { get; set; } = ConfigSettings.DEFAULT_MISC_MSG_FIELD_NAME;
        public string LoggerFieldName { get; set; } = ConfigSettings.DEFAULT_LOGGER_FIELD_NAME;
        public string LevelFieldName { get; set; } = ConfigSettings.DEFAULT_LEVEL_FIELD_NAME;

    }

}