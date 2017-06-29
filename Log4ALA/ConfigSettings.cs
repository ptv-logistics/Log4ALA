using Microsoft.Azure;
using System;

namespace Log4ALA
{
    public class ConfigSettings
    {
        private const string ALA_WORKSPACE_ID_PROP = "workspaceId";
        private const string ALA_SHAREDKEY_PROP = "SharedKey";
        private const string ALA_LOGTYPE_PROP = "logType";
        private const string ALA_AZURE_API_VERSION_PROP = "azureApiVersion";
        private const string ALA_HTTP_DATACOLLECTOR_RETRY_PROP = "httpDataCollectorRetry";
        private const string ALA_LOGGING_QUEUE_SIZE_PROP = "loggingQueueSize";
        private const string ALA_LOG_MESSAGE_TOFILE_PROP = "logMessageToFile";
        private const string ALA_APPEND_LOGGER_PROP = "appendLogger";
        private const string ALA_APPEND_LOG_LEVEL_PROP = "appendLogLevel";
        private const string ALA_ERR_LOGGER_NAME_PROP = "errLoggerName";
        private const string ALA_ERR_APPENDER_FILE_PROP = "errAppenderFile";
        private const string ALA_INFO_APPENDER_FILE_PROP = "infoAppenderFile";
        public const int DEFAULT_LOGGER_QUEUE_SIZE = 1000000;
        private const string QUEUE_SIZE_LOG_INTERVAL_PROP = "alaQueueSizeLogIntervalInMin";
        private const string DEFAULT_QUEUE_SIZE_LOG_INTERVAL_MINUTES = "2";
        private const string QUEUE_SIZE_LOG_INTERVAL_ENABLED_PROP = "alaQueueSizeLogIntervalEnabled";
        private const string ALA_KEY_VALUE_DETECTION_PROP = "keyValueDetection";
        private const string ALA_JSON_DETECTION_PROP = "jsonDetection";



        private string propPrefix = string.Empty;

        public ConfigSettings(string propPrefix)
        {
            this.propPrefix = propPrefix;
        }


        public string ALAWorkspaceId
        {
            get
            {
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_WORKSPACE_ID_PROP}");
            }
        }

        public string ALASharedKey
        {
            get
            {
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_SHAREDKEY_PROP}");
            }
        }

        public string ALALogType
        {
            get
            {
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_LOGTYPE_PROP}");
            }
        }

        public string ALAAzureApiVersion
        {
            get
            {
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_AZURE_API_VERSION_PROP}");
            }
        }

        public int? ALAHttpDataCollectorRetry
        {
            get
            {
                string aLAHttpDataCollectorRetry = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_HTTP_DATACOLLECTOR_RETRY_PROP}");
                return (string.IsNullOrWhiteSpace(aLAHttpDataCollectorRetry) ? (int?)null : int.Parse(aLAHttpDataCollectorRetry));
            }
        }

        public int? ALALoggingQueueSize
        {
            get
            {
                string aLALoggingQueueSize = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_LOGGING_QUEUE_SIZE_PROP}");
                return (string.IsNullOrWhiteSpace(aLALoggingQueueSize) ? (int?)null : int.Parse(aLALoggingQueueSize));
            }
        }

        public int? LoggingQueueSize { get; set; }


        public bool? ALALogMessageToFile
        {
            get
            {
                string aLALogMessageToFile = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_LOG_MESSAGE_TOFILE_PROP}");
                return (string.IsNullOrWhiteSpace(aLALogMessageToFile) ? (bool?)null : Boolean.Parse(aLALogMessageToFile));
            }
        }
        public bool? ALAAppendLogger
        {
            get
            {
                string aLAAppendLogger = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_APPEND_LOGGER_PROP}");
                return (string.IsNullOrWhiteSpace(aLAAppendLogger) ? (bool?)null : Boolean.Parse(aLAAppendLogger));
            }
        }

        public bool? ALAAppendLogLevel
        {
            get
            {
                string aLAAppendLogLevel = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_APPEND_LOG_LEVEL_PROP}");
                return (string.IsNullOrWhiteSpace(aLAAppendLogLevel) ? (bool?)null : Boolean.Parse(aLAAppendLogLevel));
            }
        }

        public string ALAErrLoggerName
        {
            get
            {
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_ERR_LOGGER_NAME_PROP}");
            }
        }

        public string ALAErrAppenderFile
        {
            get
            {
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_ERR_APPENDER_FILE_PROP}");
            }
        }

        public string ALAInfoAppenderFile
        {
            get
            {
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_INFO_APPENDER_FILE_PROP}");
            }
        }

        public static int LogQueueSizeInterval
        {
            get
            {
                string queueSizeLogInterval = CloudConfigurationManager.GetSetting(QUEUE_SIZE_LOG_INTERVAL_PROP);
                return int.Parse((string.IsNullOrWhiteSpace(queueSizeLogInterval) ? DEFAULT_QUEUE_SIZE_LOG_INTERVAL_MINUTES : queueSizeLogInterval));
            }
        }

        public static bool IsLogQueueSizeInterval
        {
            get
            {
                string isLogQueueSizeInterval = CloudConfigurationManager.GetSetting(QUEUE_SIZE_LOG_INTERVAL_ENABLED_PROP);
                return (string.IsNullOrWhiteSpace(isLogQueueSizeInterval) ? false : Boolean.Parse(isLogQueueSizeInterval));
            }
        }

        public bool? ALAKeyValueDetection
        {
            get
            {
                string aLAKeyValueDetection = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_KEY_VALUE_DETECTION_PROP}");
                return (string.IsNullOrWhiteSpace(aLAKeyValueDetection) ? (bool?)null : Boolean.Parse(aLAKeyValueDetection));
            }
        }

        public bool? ALAJsonDetection
        {
            get
            {
                string aLAJsonDetection = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_JSON_DETECTION_PROP}");
                return (string.IsNullOrWhiteSpace(aLAJsonDetection) ? (bool?)null : Boolean.Parse(aLAJsonDetection));
            }
        }


    }
}
