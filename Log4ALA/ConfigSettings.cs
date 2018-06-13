#if !NETSTANDARD2_0 && !NETCOREAPP2_0
using Microsoft.Azure;
#else
using Microsoft.Extensions.Configuration;
using System.IO;
#endif
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
        private const string ALA_QUEUE_READ_TIMEOUT_PROP = "queueReadTimeout";
        private const string ABORT_TIMEOUT_SECONDS_PROP = "abortTimeoutSeconds";


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
        public const int DEFAULT_QUEUE_READ_TIMEOUT = 500;
        public const string DEFAULT_TIMEOUT_SECONDS = "20";

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



#if NETSTANDARD2_0 || NETCOREAPP2_0
        private static IConfigurationRoot CloudConfigurationManager = (new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")).Build();
#endif


        public ConfigSettings(string propPrefix)
        {
            this.propPrefix = propPrefix;
        }


        public string ALAWorkspaceId
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_WORKSPACE_ID_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_WORKSPACE_ID_PROP}"];
#endif
            }
        }

        public string ALASharedKey
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_SHAREDKEY_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_SHAREDKEY_PROP}"];
#endif
            }
        }

        public string ALALogType
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_LOGTYPE_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_LOGTYPE_PROP}"];
#endif
            }
        }

        public string ALAAzureApiVersion
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_AZURE_API_VERSION_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_AZURE_API_VERSION_PROP}"];
#endif
            }
        }

        public int? ALAHttpDataCollectorRetry
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAHttpDataCollectorRetry = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_HTTP_DATACOLLECTOR_RETRY_PROP}");
#else
                string aLAHttpDataCollectorRetry = CloudConfigurationManager[$"{this.propPrefix}:{ALA_HTTP_DATACOLLECTOR_RETRY_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAHttpDataCollectorRetry) ? (int?)null : int.Parse(aLAHttpDataCollectorRetry));
            }
        }

        public int? ALALoggingQueueSize
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLALoggingQueueSize = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_LOGGING_QUEUE_SIZE_PROP}");
#else
                string aLALoggingQueueSize = CloudConfigurationManager[$"{this.propPrefix}:{ALA_LOGGING_QUEUE_SIZE_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLALoggingQueueSize) ? (int?)null : int.Parse(aLALoggingQueueSize));
            }
        }

        public int? LoggingQueueSize { get; set; }


        public bool? ALALogMessageToFile
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLALogMessageToFile = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_LOG_MESSAGE_TOFILE_PROP}");
#else
                string aLALogMessageToFile = CloudConfigurationManager[$"{this.propPrefix}:{ALA_LOG_MESSAGE_TOFILE_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLALogMessageToFile) ? (bool?)null : Boolean.Parse(aLALogMessageToFile));
            }
        }
        public bool? ALAAppendLogger
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAAppendLogger = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_APPEND_LOGGER_PROP}");
#else
                string aLAAppendLogger = CloudConfigurationManager[$"{this.propPrefix}:{ALA_APPEND_LOGGER_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAAppendLogger) ? (bool?)null : Boolean.Parse(aLAAppendLogger));
            }
        }

        public bool? ALAAppendLogLevel
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAAppendLogLevel = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_APPEND_LOG_LEVEL_PROP}");
#else
                string aLAAppendLogLevel = CloudConfigurationManager[$"{this.propPrefix}:{ALA_APPEND_LOG_LEVEL_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAAppendLogLevel) ? (bool?)null : Boolean.Parse(aLAAppendLogLevel));
            }
        }

        public string ALAErrLoggerName
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_ERR_LOGGER_NAME_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_ERR_LOGGER_NAME_PROP}"];
#endif
            }
        }

        public string ALAErrAppenderFile
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_ERR_APPENDER_FILE_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_ERR_APPENDER_FILE_PROP}"];
#endif
            }
        }

        public string ALAInfoAppenderFile
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_INFO_APPENDER_FILE_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_INFO_APPENDER_FILE_PROP}"];
#endif
            }
        }

        public static int LogQueueSizeInterval
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string queueSizeLogInterval = CloudConfigurationManager.GetSetting(QUEUE_SIZE_LOG_INTERVAL_PROP);
#else
                string queueSizeLogInterval = CloudConfigurationManager[QUEUE_SIZE_LOG_INTERVAL_PROP];
#endif
                return int.Parse((string.IsNullOrWhiteSpace(queueSizeLogInterval) ? DEFAULT_QUEUE_SIZE_LOG_INTERVAL_MINUTES : queueSizeLogInterval));
            }
        }

        public static bool IsLogQueueSizeInterval
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string isLogQueueSizeInterval = CloudConfigurationManager.GetSetting(QUEUE_SIZE_LOG_INTERVAL_ENABLED_PROP);
#else
                string isLogQueueSizeInterval = CloudConfigurationManager[QUEUE_SIZE_LOG_INTERVAL_ENABLED_PROP];
#endif
                return (string.IsNullOrWhiteSpace(isLogQueueSizeInterval) ? false : Boolean.Parse(isLogQueueSizeInterval));
            }
        }

        public bool? ALAKeyValueDetection
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAKeyValueDetection = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_KEY_VALUE_DETECTION_PROP}");
#else
                string aLAKeyValueDetection = CloudConfigurationManager[$"{this.propPrefix}:{ALA_KEY_VALUE_DETECTION_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAKeyValueDetection) ? (bool?)null : Boolean.Parse(aLAKeyValueDetection));
            }
        }

        public bool? ALAJsonDetection
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAJsonDetection = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_JSON_DETECTION_PROP}");
#else
                string aLAJsonDetection = CloudConfigurationManager[$"{this.propPrefix}:{ALA_JSON_DETECTION_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAJsonDetection) ? (bool?)null : Boolean.Parse(aLAJsonDetection));
            }
        }

        public int? ALABatchSizeInBytes
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLABatchSizeInBytes = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_BATCH_SIZE_BYTES_PROP}");
#else
                string aLABatchSizeInBytes = CloudConfigurationManager[$"{this.propPrefix}:{ALA_BATCH_SIZE_BYTES_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLABatchSizeInBytes) ? (int?)null : int.Parse(aLABatchSizeInBytes));
            }
        }

        public int? ALABatchNumItems
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLABatchNumItems = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_BATCH_NUM_ITEMS_PROP}");
#else
                string aLABatchNumItems = CloudConfigurationManager[$"{this.propPrefix}:{ALA_BATCH_NUM_ITEMS_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLABatchNumItems) ? (int?)null : int.Parse(aLABatchNumItems));
            }
        }

        public int? ALABatchWaitInSec
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLABatchWaitInSec = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_BATCH_WAIT_SECONDS_PROP}");
#else
                string aLABatchWaitInSec = CloudConfigurationManager[$"{this.propPrefix}:{ALA_BATCH_WAIT_SECONDS_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLABatchWaitInSec) ? (int?)null : int.Parse(aLABatchWaitInSec));
            }
        }

        public int? ALABatchWaitMaxInSec
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLABatchWaitMaxInSec = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_BATCH_WAIT_MAX_SECONDS_PROP}");
#else
                string aLABatchWaitMaxInSec = CloudConfigurationManager[$"{this.propPrefix}:{ALA_BATCH_WAIT_MAX_SECONDS_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLABatchWaitMaxInSec) ? (int?)null : int.Parse(aLABatchWaitMaxInSec));
            }
        }

        public int? ALAMaxFieldByteLength
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAMaxFieldByteLength = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_MAX_FIELD_BYTE_LENGTH_PROP}");
#else
                string aLAMaxFieldByteLength = CloudConfigurationManager[$"{this.propPrefix}:{ALA_MAX_FIELD_BYTE_LENGTH_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAMaxFieldByteLength) ? (int?)null : int.Parse(aLAMaxFieldByteLength));
            }
        }

        public int? ALAMaxFieldNameLength
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAMaxFieldNameLength = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_MAX_FIELD_NAME_LENGTH_PROP}");
#else
                string aLAMaxFieldNameLength = CloudConfigurationManager[$"{this.propPrefix}:{ALA_MAX_FIELD_NAME_LENGTH_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAMaxFieldNameLength) ? (int?)null : int.Parse(aLAMaxFieldNameLength));
            }
        }

        public string ALACoreFieldNames
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_CORE_FIELD_NAMES_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_CORE_FIELD_NAMES_PROP}"];
#endif
            }
        }

        public string ALAThreadPriority
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_THREAD_PRIORITY_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_THREAD_PRIORITY_PROP}"];
#endif
            }
        }
        public int? ALAQueueReadTimeout
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAQueueReadTimeout = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_QUEUE_READ_TIMEOUT_PROP}");
#else
                string aLAQueueReadTimeout = CloudConfigurationManager[$"{this.propPrefix}:{ALA_QUEUE_READ_TIMEOUT_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAQueueReadTimeout) ? (int?)null : int.Parse(aLAQueueReadTimeout));
            }
        }
        

        public static int AbortTimeoutSeconds
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string abortTimeout = CloudConfigurationManager.GetSetting($"{ABORT_TIMEOUT_SECONDS_PROP}");
#else
                string abortTimeout = CloudConfigurationManager[$"{ABORT_TIMEOUT_SECONDS_PROP}"];
#endif
                return int.Parse((string.IsNullOrWhiteSpace(abortTimeout) ? DEFAULT_TIMEOUT_SECONDS : abortTimeout));
            }
        }

    }
}
