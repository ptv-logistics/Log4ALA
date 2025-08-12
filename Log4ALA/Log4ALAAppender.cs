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
        public int HttpDataCollectorRetry { get; set; } = ConfigSettings.DEFAULT_HTTP_DATA_COLLECTOR_RETRY;
        public bool LogMessageToFile { get; set; } = ConfigSettings.DEFAULT_LOG_MESSAGE_TOFILE;
        public bool DisableInfoLogFile { get; set; } = ConfigSettings.DEFAULT_DISABLE_INFO_APPENDER_FILE;
        public bool AppendLogger { get; set; } = ConfigSettings.DEFAULT_APPEND_LOGGER;
        public bool AppendLogLevel { get; set; } = ConfigSettings.DEFAULT_APPEND_LOGLEVEL;

        public string ErrLoggerName { get; set; }

        public string ErrAppenderFile { get; set; }
        public string InfoAppenderFile { get; set; }

        // Size of the internal event queue. 
        public int LoggingQueueSize { get; set; } = ConfigSettings.DEFAULT_LOGGER_QUEUE_SIZE;
        public bool KeyValueDetection { get; set; } = ConfigSettings.DEFAULT_KEY_VALUE_DETECTION;
        public bool JsonDetection { get; set; } = ConfigSettings.DEFAULT_JSON_DETECTION;
        public int BatchSizeInBytes { get; set; } = ConfigSettings.DEFAULT_BATCH_SIZE_BYTES;

        public bool EnableDebugConsoleLog { get; set; } = ConfigSettings.DEFAULT_ENABLE_DEBUG_CONSOLE_LOG;

        public int BatchNumItems { get; set; } = ConfigSettings.DEFAULT_BATCH_NUM_ITEMS;
        public int BatchWaitInSec { get; set; } = ConfigSettings.DEFAULT_BATCH_WAIT_SECONDS;
        public int BatchWaitMaxInSec { get; set; } = ConfigSettings.DEFAULT_BATCH_WAIT_MAX_SECONDS;
        public int MaxFieldByteLength { get; set; }
        public int MaxFieldNameLength { get; set; }
        public int HttpClientTimeout { get; set; } = ConfigSettings.DEFAULT_HTTP_CLIENT_TIMEOUT;
        public int HttpClientRequestTimeout { get; set; } = ConfigSettings.DEFAULT_HTTP_CLIENT_REQUEST_TIMEOUT;

        public string KeyValueSeparator { get; set; } = ConfigSettings.DEFAULT_KEY_VALUE_SEPARATOR;
        public string KeyValuePairSeparator { get; set; } = ConfigSettings.DEFAULT_KEY_VALUE_PAIR_SEPARATOR;
        public string LogAnalyticsDNS { get; set; } = ConfigSettings.DEFAULT_LOGANALYTICS_DNS;

        public bool DisableAnonymousPropsPrefix { get; set; } = ConfigSettings.DEFAULT_DISABLE_ANONYMOUS_PROPS_PREFIX;
        public bool EnablePassThroughTimeStampField { get; set; } = ConfigSettings.DEFAULT_ENABLE_PASSTHROUGH_TIMESTAMP_FIELD;
        public bool KeyToLowerCase { get; set; } = ConfigSettings.DEFAULT_KEY_TO_LOWER_CASE;
        public bool IngestionApi { get; set; } = ConfigSettings.DEFAULT_INGESTION_API;
        public bool IngestionIdentityLogin { get; set; } = ConfigSettings.DEFAULT_INGESTION_IDENTITY_LOGIN;
        public string TenantId { get; set; }
        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public string DcEndpoint { get; set; }
        public string DcrId { get; set; }
        public string DcEndpointApiVersion { get; set; } = ConfigSettings.DEFAULT_DC_ENDPOINT_API_VERSION;
        public string MsiEndpointEnvName { get; set; } = ConfigSettings.DEFAULT_MSI_ENDPOINT_ENV_NAME;
        public string MsiSecretEnvName { get; set; } = ConfigSettings.DEFAULT_MSI_SECRET_ENV_NAME;
        public string UserManagedIdentityClientId { get; set; }
        public string MsiIdentityHeaderName { get; set; } = ConfigSettings.DEFAULT_MSI_IDENTITY_HEADER_NAME;
        public string MsiApiVersion { get; set; } = ConfigSettings.DEFAULT_MSI_API_VERSION;
        public bool IngestionApiGzip { get; set; } = ConfigSettings.DEFAULT_INGESTION_API_GZIP;
        public bool IngestionApiGzipLegacyMangedDeflateStream { get; set; } = ConfigSettings.DEFAULT_INGESTION_API_GZIP_LEGACY_MANAGED_DEFLATE_STREAM;
        public bool DisableNumberTypeConvertion { get; set; } = ConfigSettings.DEFAULT_DISABLE_NUMBER_TYPE_CONVERTION;


        public string IngestionApiDebugHeaderValue { get; set; }


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
                    tempValue = JsonConvert.SerializeObject(new CoreFieldNames() { DateFieldName = (IngestionApi ? ConfigSettings.INGESTION_API_DEFAULT_DATE_FIELD_NAME : ConfigSettings.DEFAULT_DATE_FIELD_NAME), MiscMessageFieldName = ConfigSettings.DEFAULT_MISC_MSG_FIELD_NAME, LevelFieldName = ConfigSettings.DEFAULT_LEVEL_FIELD_NAME, LoggerFieldName = ConfigSettings.DEFAULT_LOGGER_FIELD_NAME });
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

        public string DebugHTTPReqURI { get; set; } = null;

        public Log4ALAAppender()
        {
        }


        public override void ActivateOptions()
        {

            try
            {
                configSettings = new ConfigSettings(this.Name);

                EnableDebugConsoleLog = ConfigSettings.ALAEnableDebugConsoleLog == null ? EnableDebugConsoleLog : (bool)ConfigSettings.ALAEnableDebugConsoleLog;


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

                if(EnableDebugConsoleLog)
                {
                    var message1 = $@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|TRACE|[{this.Name}] - appsettings directory:[{ConfigSettings.ContentRootPath}]";
                    var message2 = $@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|TRACE|[{this.Name}] - ASPNETCORE_ENVIRONMENT:[{ConfigSettings.AspNetCoreEnvironment}]";
                    var message3 = $@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|TRACE|[{this.Name}] - APPSETTINGS_SUFFIX:[{ConfigSettings.AppsettingsSuffix}]";
                    log.Deb($"{message1}", EnableDebugConsoleLog);
                    log.Deb($"{message2}", EnableDebugConsoleLog);
                    log.Deb($"{message3}", EnableDebugConsoleLog);
                    System.Console.WriteLine(message1);
                    System.Console.WriteLine(message2);
                    System.Console.WriteLine(message3);
                }
#endif
                IngestionApi = configSettings.ALAIngestionApi == null ? IngestionApi : (bool)configSettings.ALAIngestionApi;
                log.Inf($"[{this.Name}] - ingestionApi:[{IngestionApi}]", true);

                IngestionIdentityLogin = configSettings.ALAIngestionIdentityLogin == null ? IngestionIdentityLogin : (bool)configSettings.ALAIngestionIdentityLogin;
                log.Inf($"[{this.Name}] - ingestionIdentityLogin:[{IngestionIdentityLogin}]", true);

                IngestionApiGzip = configSettings.ALAIngestionApiGzip == null ? IngestionApiGzip : (bool)configSettings.ALAIngestionApiGzip;
                log.Inf($"[{this.Name}] - ingestionApiGzip:[{IngestionApiGzip}]", true);

                IngestionApiGzipLegacyMangedDeflateStream = configSettings.ALAIngestionApiGzipLegacyMangedDeflateStream == null ? IngestionApiGzipLegacyMangedDeflateStream : (bool)configSettings.ALAIngestionApiGzipLegacyMangedDeflateStream;
                log.Inf($"[{this.Name}] - ingestionApiGzipLegacyMangedDeflateStream:[{IngestionApiGzipLegacyMangedDeflateStream}]", true);

                DisableNumberTypeConvertion = configSettings.ALADisableNumberTypeConvertion == null ? DisableNumberTypeConvertion : (bool)configSettings.ALADisableNumberTypeConvertion;
                log.Inf($"[{this.Name}] - disableNumberTypeConvertion:[{DisableNumberTypeConvertion}]", true);

                


#if NETSTANDARD2_0 || NETCOREAPP2_0
                if (IngestionApiGzipLegacyMangedDeflateStream)
                {
                    // Enable the legacy DeflateStream implementation
                    // https://learn.microsoft.com/en-us/dotnet/api/system.appcontext.setswitch?#examples
                    AppContext.SetSwitch("NetFx45_LegacyManagedDeflateStream", true);
                }
#endif


                if (!IngestionApi && string.IsNullOrWhiteSpace(configSettings.ALAWorkspaceId) && string.IsNullOrWhiteSpace(WorkspaceId))
                {
                    throw new Exception($"the Log4ALAAppender property workspaceId [{WorkspaceId}] shouldn't be empty");
                }

                WorkspaceId = string.IsNullOrWhiteSpace(configSettings.ALAWorkspaceId) ? WorkspaceId : configSettings.ALAWorkspaceId;
                log.Inf($"[{this.Name}] - workspaceId:[{WorkspaceId}]", true);

                if (IngestionApi && !IngestionIdentityLogin && string.IsNullOrWhiteSpace(configSettings.ALATenantId) && string.IsNullOrWhiteSpace(TenantId))
                {
                    throw new Exception($"the Log4ALAAppender property tenantId [{TenantId}] shouldn't be empty");
                }

                TenantId = string.IsNullOrWhiteSpace(configSettings.ALATenantId) ? TenantId : configSettings.ALATenantId;
                log.Inf($"[{this.Name}] - tenantId:[{TenantId}]", true);

                if (IngestionApi && !IngestionIdentityLogin && string.IsNullOrWhiteSpace(configSettings.ALAAppId) && string.IsNullOrWhiteSpace(AppId))
                {
                    throw new Exception($"the Log4ALAAppender property appId [{AppId}] shouldn't be empty");
                }

                AppId = string.IsNullOrWhiteSpace(configSettings.ALAAppId) ? AppId : configSettings.ALAAppId;
                log.Inf($"[{this.Name}] - appId:[{AppId}]", true);

                if (IngestionApi && !IngestionIdentityLogin && string.IsNullOrWhiteSpace(configSettings.ALAAppSecret) && string.IsNullOrWhiteSpace(AppSecret))
                {
                    throw new Exception($"the Log4ALAAppender property appSecret [{AppSecret}] shouldn't be empty");
                }

                AppSecret = string.IsNullOrWhiteSpace(configSettings.ALAAppSecret) ? AppSecret : configSettings.ALAAppSecret;
                log.Inf($"[{this.Name}] - appSecret:[{(string.IsNullOrWhiteSpace(AppSecret) ? string.Empty : AppSecret.Remove(20))}...]", true);

                if (IngestionApi && string.IsNullOrWhiteSpace(configSettings.ALADcEndpoint) && string.IsNullOrWhiteSpace(DcEndpoint))
                {
                    throw new Exception($"the Log4ALAAppender property dcEndpoint [{DcEndpoint}] shouldn't be empty");
                }

                DcEndpoint = string.IsNullOrWhiteSpace(configSettings.ALADcEndpoint) ? DcEndpoint : configSettings.ALADcEndpoint;
                log.Inf($"[{this.Name}] - dcEndpoint:[{DcEndpoint}]", true);

                if (IngestionApi && string.IsNullOrWhiteSpace(configSettings.ALADcrId) && string.IsNullOrWhiteSpace(DcrId))
                {
                    throw new Exception($"the Log4ALAAppender property dcrId [{DcrId}] shouldn't be empty");
                }

                DcrId = string.IsNullOrWhiteSpace(configSettings.ALADcrId) ? DcrId : configSettings.ALADcrId;
                log.Inf($"[{this.Name}] - dcrId:[{(string.IsNullOrWhiteSpace(DcrId) ? string.Empty: DcrId.Remove(20))}...]", true);

                if (IngestionApi && string.IsNullOrWhiteSpace(configSettings.ALADcEndpointApiVersion) && string.IsNullOrWhiteSpace(DcEndpointApiVersion))
                {
                    throw new Exception($"the Log4ALAAppender property dcEndpointApiVersion [{DcEndpointApiVersion}] shouldn't be empty");
                }

                DcEndpointApiVersion = string.IsNullOrWhiteSpace(configSettings.ALADcEndpointApiVersion) ? DcEndpointApiVersion : configSettings.ALADcEndpointApiVersion;
                log.Inf($"[{this.Name}] - dcEndpointApiVersion:[{DcEndpointApiVersion}]", true);

                MsiEndpointEnvName = string.IsNullOrWhiteSpace(configSettings.ALAMsiEndpointEnvName) ? MsiEndpointEnvName : configSettings.ALAMsiEndpointEnvName;
                log.Inf($"[{this.Name}] - msiEndpointEnvName:[{MsiEndpointEnvName}]", true);

                MsiSecretEnvName = string.IsNullOrWhiteSpace(configSettings.ALAMsiSecretEnvName) ? MsiSecretEnvName : configSettings.ALAMsiSecretEnvName;
                log.Inf($"[{this.Name}] - msiSecretEnvName:[{MsiSecretEnvName}]", true);

                UserManagedIdentityClientId = string.IsNullOrWhiteSpace(configSettings.ALAUserManagedIdentityClientId) ? UserManagedIdentityClientId : configSettings.ALAUserManagedIdentityClientId;
                log.Inf($"[{this.Name}] - UserManagedIdentityClientId:[{UserManagedIdentityClientId}]", true);

                MsiIdentityHeaderName = string.IsNullOrWhiteSpace(configSettings.ALAMsiIdentityHeaderName) ? MsiIdentityHeaderName : configSettings.ALAMsiIdentityHeaderName;
                log.Inf($"[{this.Name}] - msiIdentityHeaderName:[{MsiIdentityHeaderName}]", true);

                MsiApiVersion = string.IsNullOrWhiteSpace(configSettings.ALAMsiApiVersion) ? MsiApiVersion : configSettings.ALAMsiApiVersion;
                log.Inf($"[{this.Name}] - msiApiVersion:[{MsiApiVersion}]", true);

                IngestionApiDebugHeaderValue = string.IsNullOrWhiteSpace(configSettings.ALAIngestionApiDebugHeaderValue) ? IngestionApiDebugHeaderValue : configSettings.ALAIngestionApiDebugHeaderValue;
                log.Inf($"[{this.Name}] - ingestionApiDebugHeaderValue:[{IngestionApiDebugHeaderValue}]", true);
                

                if (!IngestionApi && string.IsNullOrWhiteSpace(configSettings.ALASharedKey) && string.IsNullOrWhiteSpace(SharedKey))
                {
                    throw new Exception($"the Log4ALAAppender property sharedKey [{SharedKey}] shouldn't be empty");
                }

                SharedKey = string.IsNullOrWhiteSpace(configSettings.ALASharedKey) ? SharedKey : configSettings.ALASharedKey;
                log.Inf($"[{this.Name}] - sharedKey:[{(string.IsNullOrWhiteSpace(SharedKey) ? SharedKey : SharedKey.Remove(15))}...]", true);

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

                DebugHTTPReqURI = string.IsNullOrWhiteSpace(configSettings.ALADebugHttpReqUri) ? DebugHTTPReqURI : configSettings.ALADebugHttpReqUri;

                DisableAnonymousPropsPrefix = configSettings.ALADisableAnonymousPropsPrefix == null ? DisableAnonymousPropsPrefix : (bool)configSettings.ALADisableAnonymousPropsPrefix;


                EnablePassThroughTimeStampField = configSettings.ALAEnablePassThroughTimeStampField == null ? EnablePassThroughTimeStampField : (bool)configSettings.ALAEnablePassThroughTimeStampField;
                log.Inf($"[{this.Name}] - enablePassThroughTimeStampField:[{EnablePassThroughTimeStampField}]", true);

                ThreadPriority = string.IsNullOrWhiteSpace(configSettings.ALAThreadPriority) ? ThreadPriority : configSettings.ALAThreadPriority;
                ThreadPriority priority;
                if (!Enum.TryParse(ThreadPriority, out priority))
                {
                    throw new Exception($"the Log4ALAAppender wrong threadPriority value [{ThreadPriority}] possible values -> Lowest/BelowNormal/Normal/AboveNormal/Highest");
                }
                log.Inf($"[{this.Name}] - threadPriority:[{ThreadPriority}]", true);

                HttpDataCollectorRetry = configSettings.ALAHttpDataCollectorRetry == null ? HttpDataCollectorRetry : (int)configSettings.ALAHttpDataCollectorRetry;
                log.Inf($"[{this.Name}] - httpDataCollectorRetry:[{HttpDataCollectorRetry}]", true);

                BatchSizeInBytes = configSettings.ALABatchSizeInBytes == null ? BatchSizeInBytes : (int)configSettings.ALABatchSizeInBytes;
                log.Inf($"[{this.Name}] - batchSizeInBytes:[{BatchSizeInBytes}]", true);

                BatchNumItems = configSettings.ALABatchNumItems == null ? BatchNumItems : (int)configSettings.ALABatchNumItems;

                BatchWaitInSec = configSettings.ALABatchWaitInSec == null ? BatchWaitInSec : (int)configSettings.ALABatchWaitInSec;
                log.Inf($"[{this.Name}] - batchWaitInSec:[{BatchWaitInSec}]", true);

                LoggingQueueSize = configSettings.ALALoggingQueueSize == null ? LoggingQueueSize : (int)configSettings.ALALoggingQueueSize;
                log.Inf($"[{this.Name}] - loggingQueueSize:[{LoggingQueueSize}]", true);

                BatchWaitMaxInSec = configSettings.ALABatchWaitMaxInSec == null ? BatchWaitMaxInSec : (int)configSettings.ALABatchWaitMaxInSec;
                log.Inf($"[{this.Name}] - batchWaitMaxInSec:[{BatchWaitMaxInSec}]", true);

                var defaultMaxFieldByteLen = (IngestionApi ? ConfigSettings.INGESTION_API_DEFAULT_MAX_FIELD_BYTE_LENGTH : ConfigSettings.DEFAULT_MAX_FIELD_BYTE_LENGTH);

                MaxFieldByteLength = configSettings.ALAMaxFieldByteLength == null ? defaultMaxFieldByteLen : (int)configSettings.ALAMaxFieldByteLength;
                if(MaxFieldByteLength > defaultMaxFieldByteLen)
                {
                    MaxFieldByteLength = defaultMaxFieldByteLen;
                }
                log.Inf($"[{this.Name}] - maxFieldByteLength:[{MaxFieldByteLength}]", true);

                var defaultMaxFieldNameLength = (IngestionApi ? ConfigSettings.INGESTION_API_DEFAULT_MAX_FIELD_NAME_LENGTH : ConfigSettings.DEFAULT_MAX_FIELD_NAME_LENGTH);

                MaxFieldNameLength = configSettings.ALAMaxFieldNameLength == null ? defaultMaxFieldNameLength : (int)configSettings.ALAMaxFieldNameLength;
                if (MaxFieldNameLength > defaultMaxFieldNameLength)
                {
                    MaxFieldNameLength = defaultMaxFieldNameLength;
                }
                log.Inf($"[{this.Name}] - maxFieldNameLength:[{MaxFieldNameLength}]", true);


                HttpClientTimeout = configSettings.ALAHttpClientTimeout == null ? HttpClientTimeout : (int)configSettings.ALAHttpClientTimeout;
                log.Inf($"[{this.Name}] - httpClientTimeout:[{HttpClientTimeout}]", true);

                HttpClientRequestTimeout = configSettings.ALAHttpClientRequestTimeout == null ? HttpClientRequestTimeout : (int)configSettings.ALAHttpClientRequestTimeout;
                log.Inf($"[{this.Name}] - httpClientRequestTimeout:[{HttpClientRequestTimeout}]", true);



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

                KeyToLowerCase = configSettings.ALAKeyToLowerCase == null ? KeyToLowerCase : (bool)configSettings.ALAKeyToLowerCase;
                log.Inf($"[{this.Name}] - keyToLowerCase:[{KeyToLowerCase}]", true);

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
                

                LogAnalyticsDNS = string.IsNullOrWhiteSpace(configSettings.ALALogAnalyticsDNS) ? LogAnalyticsDNS : configSettings.ALALogAnalyticsDNS;
                log.Inf($"[{this.Name}] - logAnalyticsDNS:[{LogAnalyticsDNS}]", true);



                string configDisableInfoLogFile = configSettings.ALADisableInfoAppenderFileCommon ? "CommonConfiguration" : this.Name;
                log.Inf($"[{configDisableInfoLogFile}] - disableInfoLogFile:[{DisableInfoLogFile}]", true);

                string configDisableAnonymousPropsPrefix = configSettings.ALADisableAnonymousPropsPrefixCommon ? "CommonConfiguration" : this.Name;
                log.Inf($"[{configDisableAnonymousPropsPrefix}] - disableAnonymousPropsPrefix:[{DisableAnonymousPropsPrefix}]", true);

                if (!string.IsNullOrWhiteSpace(DebugHTTPReqURI))
                {
                    string configDebugHTTPReqURI = configSettings.ALADebugHttpReqUriCommon ? "CommonConfiguration" : this.Name;
                    log.Inf($"[{configDebugHTTPReqURI}] - debugHTTPReqURI:[{DebugHTTPReqURI}]", true);
                }

                log.Inf($"[CommonConfiguration] - alaQueueSizeLogIntervalEnabled:[{ConfigSettings.IsLogQueueSizeInterval}]", true);
                log.Inf($"[CommonConfiguration] - alaQueueSizeLogIntervalInSec:[{ConfigSettings.LogQueueSizeInterval}]", true);
                log.Inf($"[CommonConfiguration] - enableDebugConsoleLog:[{EnableDebugConsoleLog}]", true);


                serializer = new LoggingEventSerializer(this);

                queueLogger = new QueueLogger(this);

            }
            catch (Exception ex)
            {
                queueLogger = null;
                string message = $"[{this.Name}] - Unable to activate Log4ALAAppender: [{ex.StackTrace}]";
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

        public static void Deb(this ILog log, string logMessage, bool logMessage2File = false)
        {
            logMessage = logMessage.Replace("|INFO|", "|DEBUG|").Substring(35);
            
            if (logMessage2File && log != null)
            {
                log.Err(logMessage);
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