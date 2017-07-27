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
        private const string QUEUE_SIZE_LOG_INTERVAL_PROP = "alaQueueSizeLogIntervalInSec";
        private const string QUEUE_SIZE_LOG_INTERVAL_ENABLED_PROP = "alaQueueSizeLogIntervalEnabled";
        private const string ALA_KEY_VALUE_DETECTION_PROP = "keyValueDetection";
        private const string ALA_JSON_DETECTION_PROP = "jsonDetection";
        private const string ALA_BATCH_SIZE_BYTES_PROP = "batchSizeInBytes";
        private const string ALA_BATCH_NUM_ITEMS_PROP = "batchNumItems";
        private const string ALA_BATCH_WAIT_SECONDS_PROP = "batchWaitInSec";
        private const string ALA_BATCH_WAIT_MAX_SECONDS_PROP = "batchWaitMaxInSec";
        private const string ALA_MAX_FIELD_BYTE_LENGTH_PROP = "maxFieldByteLength";
        private const string ALA_CORE_FIELD_NAMES_PROP = "coreFieldNames";
        private const string ALA_MAX_FIELD_NAME_LENGTH_PROP = "maxFieldNameLength";
        private const string ALA_THREAD_PRIORITY_PROP = "threadPriority";


        public const int DEFAULT_HTTP_DATA_COLLECTOR_RETRY = 6;
        public const int DEFAULT_BATCH_WAIT_MAX_SECONDS = 60;
        public const string DEFAULT_AZURE_API_VERSION = "2016-04-01";
        public const string DEFAULT_QUEUE_SIZE_LOG_INTERVAL_MINUTES = "2";
        public const int DEFAULT_BATCH_SIZE_BYTES = 0;
        public const int DEFAULT_BATCH_NUM_ITEMS = 1;
        public const int DEFAULT_BATCH_WAIT_SECONDS = 0;
        public const bool DEFAULT_APPEND_LOGGER = true;
        public const bool DEFAULT_APPEND_LOGLEVEL = true;
        public const bool DEFAULT_LOG_MESSAGE_TOFILE = false;
        public const bool DEFAULT_KEY_VALUE_DETECTION = true;
        public const bool DEFAULT_JSON_DETECTION = true;
        public const int DEFAULT_MAX_FIELD_BYTE_LENGTH = 32000;
        public const int DEFAULT_MAX_FIELD_NAME_LENGTH = 500;

        public const string DEFAULT_DATE_FIELD_NAME = "DateValue";
        public const string DEFAULT_MISC_MSG_FIELD_NAME = "MiscMsg";
        public const string DEFAULT_LOGGER_FIELD_NAME = "Logger";
        public const string DEFAULT_LEVEL_FIELD_NAME = "Level";
        public const string DEFAULT_THREAD_PRIORITY = "Lowest";

        // Minimal delay between attempts to reconnect in milliseconds. 
        public const int MIN_DELAY = 100;

        // Maximal delay between attempts to reconnect in milliseconds. 
        public const int MAX_DELAY = 10000;

        public const string LOG_ERR_APPENDER = "Log4ALAErrorAppender";
        public const string LOG_INFO_APPENDER = "Log4ALAInfoAppender";

        public const string LOG_ERR_DEFAULT_FILE = "log4ALA_error.log";
        public const string LOG_INFO_DEFAULT_FILE = "log4ALA_info.log";


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

        public int? ALABatchSizeInBytes
        {
            get
            {
                string aLABatchSizeInBytes = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_BATCH_SIZE_BYTES_PROP}");
                return (string.IsNullOrWhiteSpace(aLABatchSizeInBytes) ? (int?)null : int.Parse(aLABatchSizeInBytes));
            }
        }

        public int? ALABatchNumItems
        {
            get
            {
                string aLABatchNumItems = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_BATCH_NUM_ITEMS_PROP}");
                return (string.IsNullOrWhiteSpace(aLABatchNumItems) ? (int?)null : int.Parse(aLABatchNumItems));
            }
        }

        public int? ALABatchWaitInSec
        {
            get
            {
                string aLABatchWaitInSec = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_BATCH_WAIT_SECONDS_PROP}");
                return (string.IsNullOrWhiteSpace(aLABatchWaitInSec) ? (int?)null : int.Parse(aLABatchWaitInSec));
            }
        }

        public int? ALABatchWaitMaxInSec
        {
            get
            {
                string aLABatchWaitMaxInSec = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_BATCH_WAIT_MAX_SECONDS_PROP}");
                return (string.IsNullOrWhiteSpace(aLABatchWaitMaxInSec) ? (int?)null : int.Parse(aLABatchWaitMaxInSec));
            }
        }

        public int? ALAMaxFieldByteLength
        {
            get
            {
                string aLAMaxFieldByteLength = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_MAX_FIELD_BYTE_LENGTH_PROP}");
                return (string.IsNullOrWhiteSpace(aLAMaxFieldByteLength) ? (int?)null : int.Parse(aLAMaxFieldByteLength));
            }
        }

        public int? ALAMaxFieldNameLength
        {
            get
            {
                string aLAMaxFieldNameLength = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_MAX_FIELD_NAME_LENGTH_PROP}");
                return (string.IsNullOrWhiteSpace(aLAMaxFieldNameLength) ? (int?)null : int.Parse(aLAMaxFieldNameLength));
            }
        }

        public string ALACoreFieldNames
        {
            get
            {
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_CORE_FIELD_NAMES_PROP}");
            }
        }

        public string ALAThreadPriority
        {
            get
            {
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_THREAD_PRIORITY_PROP}");
            }
        }

    }
}
