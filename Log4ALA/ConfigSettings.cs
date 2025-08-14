#if !NETSTANDARD2_0 && !NETCOREAPP2_0
using Microsoft.Azure;
#else
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
        private const string ALA_DISABLE_INFO_APPENDER_FILE_PROP = "disableInfoLogFile";
        private const string ALA_ENABLE_DEBUG_CONSOLE_LOG_PROP = "enableDebugConsoleLog";
        private const string ALA_APPEND_LOGGER_PROP = "appendLogger";
        private const string ALA_APPEND_LOG_LEVEL_PROP = "appendLogLevel";
        private const string ALA_ERR_LOGGER_NAME_PROP = "errLoggerName";
        private const string ALA_ERR_APPENDER_FILE_PROP = "errAppenderFile";
        private const string ALA_INFO_APPENDER_FILE_PROP = "infoAppenderFile";
        public const int DEFAULT_LOGGER_QUEUE_SIZE = 1000000;
        private const string QUEUE_SIZE_LOG_INTERVAL_PROP = "alaQueueSizeLogIntervalInSec";
        private const string QUEUE_SIZE_LOG_INTERVAL_ENABLED_PROP = "alaQueueSizeLogIntervalEnabled";
        private const string ALA_KEY_VALUE_DETECTION_PROP = "keyValueDetection";
        private const string ALA_KEY_TO_LOWER_CASE_PROP = "KeyToLowerCase";
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
        private const string ALA_KEY_VALUE_SEPARATOR_PROP = "keyValueSeparator";
        private const string ALA_KEY_VALUE_PAIR_SEPARATOR_PROP = "keyValuePairSeparator";
        private const string ALA_LOG_ANALYTICS_DNS_PROP = "logAnalyticsDNS";
        private const string ALA_DEBUG_HTTP_REQ_URI_PROP = "debugHTTPReqURI";
        private const string ALA_DISABLE_ANONYMOUS_PROPS_PREFIX_PROP = "disableAnonymousPropsPrefix";
        private const string ALA_HTTP_CLIENT_TIMEOUT_PROP = "httpClientTimeout";
        private const string ALA_HTTP_CLIENT_REQUEST_TIMEOUT_PROP = "httpClientRequestTimeout";

        //https://learn.microsoft.com/en-us/azure/azure-monitor/essentials/data-collection-rule-samples#logs-ingestion-api
        //https://learn.microsoft.com/en-us/azure/azure-monitor/essentials/data-collection-rule-structure#properties
        private const string ALA_INGESTION_API_PROP = "ingestionApi";
        private const string ALA_INGESTION_IDENTITY_LOGIN_PROP = "ingestionIdentityLogin";
        private const string ALA_TENANT_ID_PROP = "tenantId";
        private const string ALA_APP_ID_PROP = "appId";
        private const string ALA_APP_SECRET_PROP = "appSecret";
        private const string ALA_DC_ENDPOINT_PROP = "dcEndpoint";
        private const string ALA_DCR_ID_PROP = "dcrId";
        private const string ALA_DC_ENDPOINT_API_VERSION_PROP = "dcEndpointApiVersion";
        private const string ALA_INGESTION_API_GZIP_PROP = "ingestionApiGzip";
        private const string ALA_INGESTION_API_DEBUG_HEADER_VALUE_PROP = "ingestionApiDebugHeaderValue";
        private const string ALA_INGESTION_API_GZIP_LEGACY_MANAGED_DEFLATE_STREAM_PROP = "ingestionApiGzipLegacyManagedDeflateStream";

        private const string ALA_MSI_ENDPOINT_ENV_NAME_PROP = "msiEndpointEnvName";
        private const string ALA_MSI_SECRET_ENV_NAME_PROP = "msiSecretEnvName";
        private const string ALA_MSI_IDENTITY_HEADER_NAME_PROP = "msiIdentityHeaderName";
        private const string ALA_MSI_API_VERSION_PROP = "msiApiVersion";
        private const string ALA_USER_MANAGED_IDENTITY_CLIENT_ID_PROP = "userManagedIdentityClientId";

        private const string ALA_ENABLE_PASSTHROUGH_TIMESTAMP_FIELD_PROP = "enablePassThroughTimeStampField";

        private const string ALA_DISABLE_NUMBER_TYPE_CONVERTION_PROP = "disableNumberTypeConvertion";


        public const int DEFAULT_HTTP_DATA_COLLECTOR_RETRY = 50;
        public const int DEFAULT_BATCH_WAIT_MAX_SECONDS = 60;
        public const string DEFAULT_AZURE_API_VERSION = "2016-04-01";
        public const string DEFAULT_QUEUE_SIZE_LOG_INTERVAL_SECONDS = "120";
        public const int DEFAULT_BATCH_SIZE_BYTES = 0;
        public const int DEFAULT_BATCH_NUM_ITEMS = 1;
        public const int DEFAULT_BATCH_WAIT_SECONDS = 0;
        public const bool DEFAULT_APPEND_LOGGER = true;
        public const bool DEFAULT_APPEND_LOGLEVEL = true;
        public const bool DEFAULT_LOG_MESSAGE_TOFILE = false;
        public const bool DEFAULT_DISABLE_INFO_APPENDER_FILE = false;
        public const bool DEFAULT_ENABLE_DEBUG_CONSOLE_LOG = false;
        public const bool DEFAULT_KEY_VALUE_DETECTION = true;
        public const bool DEFAULT_KEY_TO_LOWER_CASE = false;
        public const bool DEFAULT_JSON_DETECTION = true;
        public const int DEFAULT_MAX_FIELD_BYTE_LENGTH = 1024 * 32;
        public const int INGESTION_API_DEFAULT_MAX_FIELD_BYTE_LENGTH = 1024 * 64;
        public const int DEFAULT_MAX_FIELD_NAME_LENGTH = 100;
        public const int INGESTION_API_DEFAULT_MAX_FIELD_NAME_LENGTH = 45;
        public const int DEFAULT_QUEUE_READ_TIMEOUT = 500;
        public const string DEFAULT_TIMEOUT_SECONDS = "10";

        public const string DEFAULT_DATE_FIELD_NAME = "DateValue";
        public const string INGESTION_API_DEFAULT_DATE_FIELD_NAME = "TimeGenerated";
        public const string DEFAULT_MISC_MSG_FIELD_NAME = "MiscMsg";
        public const string DEFAULT_LOGGER_FIELD_NAME = "Logger";
        public const string DEFAULT_LEVEL_FIELD_NAME = "Level";
        public const string DEFAULT_THREAD_PRIORITY = "Lowest";

        public const string DEFAULT_KEY_VALUE_SEPARATOR = "=";
        public const string DEFAULT_KEY_VALUE_PAIR_SEPARATOR = ";";
        public const string DEFAULT_LOGANALYTICS_DNS = "ods.opinsights.azure.com";

        public const bool DEFAULT_DISABLE_ANONYMOUS_PROPS_PREFIX = false;
        public static bool DEFAULT_ENABLE_PASSTHROUGH_TIMESTAMP_FIELD = false;

        public const int DEFAULT_HTTP_CLIENT_TIMEOUT = 20000;
        public const int DEFAULT_HTTP_CLIENT_REQUEST_TIMEOUT = 20000;

        public const bool DEFAULT_INGESTION_API = false;
        public const bool DEFAULT_INGESTION_IDENTITY_LOGIN = true;
        public const string DEFAULT_DC_ENDPOINT_API_VERSION = "2023-01-01";
        public const bool DEFAULT_INGESTION_API_GZIP = true;
        public const bool DEFAULT_INGESTION_API_GZIP_LEGACY_MANAGED_DEFLATE_STREAM = false;
        public const bool DEFAULT_DISABLE_NUMBER_TYPE_CONVERTION = false;
        private const string DEFAULT_MSI_ENDPOINT_ENV_NAME = "MSI_ENDPOINT";
        private const string DEFAULT_MSI_SECRET_ENV_NAME = "MSI_SECRET";
        public const string DEFAULT_MSI_IDENTITY_HEADER_NAME = "X-IDENTITY-HEADER";
        public const string DEFAULT_MSI_API_VERSION = "2019-08-01";

        public static int BATCH_SIZE_MAX = 29000000; //quota limit per post 
        public static int INGESTION_API_BATCH_SIZE_MAX = 1024 * 1024; //quota limit per post 

        // Minimal delay between attempts to reconnect in milliseconds. 
        public const int MIN_DELAY = 100;

        // Maximal delay between attempts to reconnect in milliseconds. 
        public const int MAX_DELAY = 10000;

        public const string LOG_ERR_APPENDER = "Log4ALAErrorAppender";
        public const string LOG_INFO_APPENDER = "Log4ALAInfoAppender";

        public const string LOG_ERR_DEFAULT_FILE = "log4ALA_error.log";
        public const string LOG_INFO_DEFAULT_FILE = "log4ALA_info.log";

        public const char COMMA = ',';
        public const char SQUARE_BRACKET_OPEN = '[';
        public const char SQUARE_BRACKET_CLOSE = ']';


        private string propPrefix = string.Empty;



#if NETSTANDARD2_0 || NETCOREAPP2_0
        private static IConfigurationRoot CloudConfigurationManager = (new ConfigurationBuilder().SetBasePath(ContentRootPath)
            .AddJsonFile("appsettings.shared_lnk.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.env_{AspNetCoreEnvironment}.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.user_{System.Environment.UserName.ToLower()}.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.env_{AppsettingsSuffix}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables().Build());
#endif


#if NETSTANDARD2_0 || NETCOREAPP2_0
        private static IConfiguration EnvVars
        {
            get
            {
                return new ConfigurationBuilder().AddEnvironmentVariables().Build();
            }
        }

        private static string aspNetCoreEnvironment = null;
        public static string AspNetCoreEnvironment
        {
            get
            {
                if (aspNetCoreEnvironment != null)
                {
                    return aspNetCoreEnvironment;
                }
                aspNetCoreEnvironment = EnvVars["ASPNETCORE_ENVIRONMENT"];
                return aspNetCoreEnvironment;
            }
        }

        private static string appsettingsSuffix = null;
        public static string AppsettingsSuffix
        {
            get
            {
                if (appsettingsSuffix != null)
                {
                    return appsettingsSuffix;
                }
                appsettingsSuffix = EnvVars["APPSETTINGS_SUFFIX"];
                return appsettingsSuffix;
            }
        }

        public static string ContentRootPath
        {
            get
            {
                 return IsWindows() ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
        }

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

        public string ALATenantId
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_TENANT_ID_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_TENANT_ID_PROP}"];
#endif
            }
        }

        public string ALAAppId
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_APP_ID_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_APP_ID_PROP}"];
#endif
            }
        }

        public string ALAAppSecret
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_APP_SECRET_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_APP_SECRET_PROP}"];
#endif
            }
        }

        public string ALADcEndpoint
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_DC_ENDPOINT_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_DC_ENDPOINT_PROP}"];
#endif
            }
        }

        public string ALADcrId
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_DCR_ID_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_DCR_ID_PROP}"];
#endif
            }
        }

        public string ALADcEndpointApiVersion
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_DC_ENDPOINT_API_VERSION_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_DC_ENDPOINT_API_VERSION_PROP}"];
#endif
            }
        }

        public string ALAIngestionApiDebugHeaderValue
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_INGESTION_API_DEBUG_HEADER_VALUE_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_INGESTION_API_DEBUG_HEADER_VALUE_PROP}"];
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

        public bool ALADisableInfoAppenderFileCommon { get; set; } = false;

        public bool? ALADisableInfoAppenderFile
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLADisableInfoAppenderFile = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_DISABLE_INFO_APPENDER_FILE_PROP}");
                if (string.IsNullOrWhiteSpace(aLADisableInfoAppenderFile))
                {
                    ALADisableInfoAppenderFileCommon = true;
                    aLADisableInfoAppenderFile = CloudConfigurationManager.GetSetting(ALA_DISABLE_INFO_APPENDER_FILE_PROP);
                }
#else
                string aLADisableInfoAppenderFile = CloudConfigurationManager[$"{this.propPrefix}:{ALA_DISABLE_INFO_APPENDER_FILE_PROP}"];
                if (string.IsNullOrWhiteSpace(aLADisableInfoAppenderFile))
                {
                    ALADisableInfoAppenderFileCommon = true;
                    aLADisableInfoAppenderFile = CloudConfigurationManager[ALA_DISABLE_INFO_APPENDER_FILE_PROP];
                }

#endif
                return (string.IsNullOrWhiteSpace(aLADisableInfoAppenderFile) ? (bool?)null : Boolean.Parse(aLADisableInfoAppenderFile));
            }
        }

        public static bool? ALAEnableDebugConsoleLog
        {
            get
            {
               

#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAEnableDebugConsoleLog = CloudConfigurationManager.GetSetting(ALA_ENABLE_DEBUG_CONSOLE_LOG_PROP);
#else
                string aLAEnableDebugConsoleLog = CloudConfigurationManager[ALA_ENABLE_DEBUG_CONSOLE_LOG_PROP];
#endif
                return (string.IsNullOrWhiteSpace(aLAEnableDebugConsoleLog) ? (bool?)null : Boolean.Parse(aLAEnableDebugConsoleLog));
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

        private static int? logQueueSizeInterval = null;
        public static int LogQueueSizeInterval
        {
            get
            {
                if (logQueueSizeInterval.HasValue)
                {
                    return logQueueSizeInterval.Value;
                }

#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string queueSizeLogInterval = CloudConfigurationManager.GetSetting(QUEUE_SIZE_LOG_INTERVAL_PROP);
#else
                string queueSizeLogInterval = CloudConfigurationManager[QUEUE_SIZE_LOG_INTERVAL_PROP];
#endif
                logQueueSizeInterval = int.Parse((string.IsNullOrWhiteSpace(queueSizeLogInterval) ? DEFAULT_QUEUE_SIZE_LOG_INTERVAL_SECONDS : queueSizeLogInterval));

                return logQueueSizeInterval.Value;
            }
        }

        private static bool? isLogQSizeInterval = null;
        public static bool IsLogQueueSizeInterval
        {
            get
            {
                if (isLogQSizeInterval.HasValue)
                {
                    return isLogQSizeInterval.Value;
                }

#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string isLogQueueSizeInterval = CloudConfigurationManager.GetSetting(QUEUE_SIZE_LOG_INTERVAL_ENABLED_PROP);
#else
                string isLogQueueSizeInterval = CloudConfigurationManager[QUEUE_SIZE_LOG_INTERVAL_ENABLED_PROP];
#endif
                isLogQSizeInterval = (string.IsNullOrWhiteSpace(isLogQueueSizeInterval) ? false : Boolean.Parse(isLogQueueSizeInterval));

                return isLogQSizeInterval.Value;
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

        public bool? ALAKeyToLowerCase
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAKeyToLowerCase = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_KEY_TO_LOWER_CASE_PROP}");
#else
                string aLAKeyToLowerCase = CloudConfigurationManager[$"{this.propPrefix}:{ALA_KEY_TO_LOWER_CASE_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAKeyToLowerCase) ? (bool?)null : Boolean.Parse(aLAKeyToLowerCase));
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

        public bool? ALAIngestionApiGzip
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAIngestionApiGzip = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_INGESTION_API_GZIP_PROP}");
#else
                string aLAIngestionApiGzip = CloudConfigurationManager[$"{this.propPrefix}:{ALA_INGESTION_API_GZIP_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAIngestionApiGzip) ? (bool?)null : Boolean.Parse(aLAIngestionApiGzip));
            }
        }


        public bool? ALAIngestionApiGzipLegacyMangedDeflateStream
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAIngestionApiGzipLegacyMangedDeflateStream = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_INGESTION_API_GZIP_LEGACY_MANAGED_DEFLATE_STREAM_PROP}");
#else
                string aLAIngestionApiGzipLegacyMangedDeflateStream = CloudConfigurationManager[$"{this.propPrefix}:{ALA_INGESTION_API_GZIP_LEGACY_MANAGED_DEFLATE_STREAM_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAIngestionApiGzipLegacyMangedDeflateStream) ? (bool?)null : Boolean.Parse(aLAIngestionApiGzipLegacyMangedDeflateStream));
            }
        }


        public bool? ALADisableNumberTypeConvertion
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLADisableNumberTypeConvertion = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_DISABLE_NUMBER_TYPE_CONVERTION_PROP}");
#else
                string aLADisableNumberTypeConvertion = CloudConfigurationManager[$"{this.propPrefix}:{ALA_DISABLE_NUMBER_TYPE_CONVERTION_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLADisableNumberTypeConvertion) ? (bool?)null : Boolean.Parse(aLADisableNumberTypeConvertion));
            }
        }


        public bool? ALAIngestionApi
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAIngestionApi = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_INGESTION_API_PROP}");
#else
                string aLAIngestionApi = CloudConfigurationManager[$"{this.propPrefix}:{ALA_INGESTION_API_PROP}"];
#endif
                var ingApi = (string.IsNullOrWhiteSpace(aLAIngestionApi) ? (bool?)null : Boolean.Parse(aLAIngestionApi));

                if (ingApi.HasValue && (bool)ingApi)
                {
                    DEFAULT_ENABLE_PASSTHROUGH_TIMESTAMP_FIELD = true;
                }

                return ingApi;
            }
        }

        public bool? ALAIngestionIdentityLogin
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAIngestionIdentityLogin = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_INGESTION_IDENTITY_LOGIN_PROP}");
#else
                string aLAIngestionIdentityLogin = CloudConfigurationManager[$"{this.propPrefix}:{ALA_INGESTION_IDENTITY_LOGIN_PROP}"];
#endif
                var ingIdentityLogin = (string.IsNullOrWhiteSpace(aLAIngestionIdentityLogin) ? (bool?)null : Boolean.Parse(aLAIngestionIdentityLogin));
                              

                return ingIdentityLogin;
            }
        }


        public string ALAMsiEndpointEnvVar
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0

                string msiEndointEnvVarName = CloudConfigurationManager.GetSetting($"{this.propPrefix}:{ALA_MSI_ENDPOINT_ENV_NAME_PROP}");
                msiEndointEnvVarName = string.IsNullOrWhiteSpace(msiEndointEnvVarName) ? DEFAULT_MSI_ENDPOINT_ENV_NAME : msiEndointEnvVarName;

                return System.Environment.GetEnvironmentVariable(msiEndointEnvVarName);
#else
                var msiEndointEnvVarName = CloudConfigurationManager[$"{this.propPrefix}:{ALA_MSI_ENDPOINT_ENV_NAME_PROP}"];
                msiEndointEnvVarName = string.IsNullOrWhiteSpace(msiEndointEnvVarName) ? DEFAULT_MSI_ENDPOINT_ENV_NAME : msiEndointEnvVarName;
                
                return EnvVars[msiEndointEnvVarName];
#endif
            }
        }

        public string ALAMsiSecretEnvVar
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string msiSecretEnvVarName = CloudConfigurationManager.GetSetting($"{this.propPrefix}:{ALA_MSI_SECRET_ENV_NAME_PROP}");
                msiSecretEnvVarName = string.IsNullOrWhiteSpace(msiSecretEnvVarName) ? DEFAULT_MSI_SECRET_ENV_NAME : msiSecretEnvVarName;

                return System.Environment.GetEnvironmentVariable(msiSecretEnvVarName);
#else

                var msiSecretEnvVarName = CloudConfigurationManager[$"{this.propPrefix}:{ALA_MSI_SECRET_ENV_NAME_PROP}"];
                msiSecretEnvVarName = string.IsNullOrWhiteSpace(msiSecretEnvVarName) ? DEFAULT_MSI_SECRET_ENV_NAME : msiSecretEnvVarName;
                
                return EnvVars[msiSecretEnvVarName];
#endif
            }
        }

        public string ALAUserManagedIdentityClientId
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_USER_MANAGED_IDENTITY_CLIENT_ID_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_USER_MANAGED_IDENTITY_CLIENT_ID_PROP}"];
#endif
            }
        }

        public string ALAMsiIdentityHeaderName
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_MSI_IDENTITY_HEADER_NAME_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_MSI_IDENTITY_HEADER_NAME_PROP}"];
#endif
            }
        }

        public string ALAMsiApiVersion
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_MSI_API_VERSION_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_MSI_API_VERSION_PROP}"];
#endif
            }
        }

        public bool? ALAEnablePassThroughTimeStampField
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAEnablePassThroughTimeStampField = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_ENABLE_PASSTHROUGH_TIMESTAMP_FIELD_PROP}");
#else
                string aLAEnablePassThroughTimeStampField = CloudConfigurationManager[$"{this.propPrefix}:{ALA_ENABLE_PASSTHROUGH_TIMESTAMP_FIELD_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAEnablePassThroughTimeStampField) ? (bool?)null : Boolean.Parse(aLAEnablePassThroughTimeStampField));
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
                return (string.IsNullOrWhiteSpace(aLABatchSizeInBytes) ? (int?)null : ( int.Parse(aLABatchSizeInBytes) >= ConfigSettings.BATCH_SIZE_MAX ? ConfigSettings.BATCH_SIZE_MAX : int.Parse(aLABatchSizeInBytes)));
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

        public int? ALAHttpClientTimeout
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAHttpClientTimeout = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_HTTP_CLIENT_TIMEOUT_PROP}");
#else
                string aLAHttpClientTimeout = CloudConfigurationManager[$"{this.propPrefix}:{ALA_HTTP_CLIENT_TIMEOUT_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAHttpClientTimeout) ? (int?)null : int.Parse(aLAHttpClientTimeout));
            }
        }

        public int? ALAHttpClientRequestTimeout
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLAHttpClientRequestTimeout = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_HTTP_CLIENT_REQUEST_TIMEOUT_PROP}");
#else
                string aLAHttpClientRequestTimeout = CloudConfigurationManager[$"{this.propPrefix}:{ALA_HTTP_CLIENT_REQUEST_TIMEOUT_PROP}"];
#endif
                return (string.IsNullOrWhiteSpace(aLAHttpClientRequestTimeout) ? (int?)null : int.Parse(aLAHttpClientRequestTimeout));
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

        public bool ALADebugHttpReqUriCommon { get; set; } = false;

        public string ALADebugHttpReqUri
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLADebugHttpReqUri = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_DEBUG_HTTP_REQ_URI_PROP}");
                if (string.IsNullOrWhiteSpace(aLADebugHttpReqUri))
                {
                    ALADebugHttpReqUriCommon = true;
                    aLADebugHttpReqUri = CloudConfigurationManager.GetSetting($"{ALA_DEBUG_HTTP_REQ_URI_PROP}");
                }
#else
                string aLADebugHttpReqUri = CloudConfigurationManager[$"{this.propPrefix}:{ALA_DEBUG_HTTP_REQ_URI_PROP}"];
                if (string.IsNullOrWhiteSpace(aLADebugHttpReqUri))
                {
                    ALADebugHttpReqUriCommon = true;
                    aLADebugHttpReqUri = CloudConfigurationManager[ALA_DEBUG_HTTP_REQ_URI_PROP];
                }
#endif
                return string.IsNullOrWhiteSpace(aLADebugHttpReqUri) ? null : aLADebugHttpReqUri;
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

        public string ALAKeyValueSeparator
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_KEY_VALUE_SEPARATOR_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_KEY_VALUE_SEPARATOR_PROP}"];
#endif
            }
        }
        public string ALAKeyValuePairSeparator
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_KEY_VALUE_PAIR_SEPARATOR_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_KEY_VALUE_PAIR_SEPARATOR_PROP}"];
#endif
            }
        }
        public string ALALogAnalyticsDNS
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                return CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_LOG_ANALYTICS_DNS_PROP}");
#else
                return CloudConfigurationManager[$"{this.propPrefix}:{ALA_LOG_ANALYTICS_DNS_PROP}"];
#endif
            }
        }
      

        public bool ALADisableAnonymousPropsPrefixCommon { get; set; } = false;
        public bool? ALADisableAnonymousPropsPrefix
        {
            get
            {
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                string aLADisableAnonymousPropsPrefix = CloudConfigurationManager.GetSetting($"{this.propPrefix}.{ALA_DISABLE_ANONYMOUS_PROPS_PREFIX_PROP}");
                if (string.IsNullOrWhiteSpace(aLADisableAnonymousPropsPrefix))
                {
                    ALADisableAnonymousPropsPrefixCommon = true;
                    aLADisableAnonymousPropsPrefix = CloudConfigurationManager.GetSetting(ALA_DISABLE_ANONYMOUS_PROPS_PREFIX_PROP);
                }
#else
                string aLADisableAnonymousPropsPrefix = CloudConfigurationManager[$"{this.propPrefix}:{ALA_DISABLE_ANONYMOUS_PROPS_PREFIX_PROP}"];
                if (string.IsNullOrWhiteSpace(aLADisableAnonymousPropsPrefix))
                {
                    ALADisableAnonymousPropsPrefixCommon = true;
                    aLADisableAnonymousPropsPrefix = CloudConfigurationManager[ALA_DISABLE_ANONYMOUS_PROPS_PREFIX_PROP];
                }

#endif
                return (string.IsNullOrWhiteSpace(aLADisableAnonymousPropsPrefix) ? DEFAULT_DISABLE_ANONYMOUS_PROPS_PREFIX : Boolean.Parse(aLADisableAnonymousPropsPrefix));
            }
        }


#if NETSTANDARD2_0 || NETCOREAPP2_0
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#endif

    }
}
