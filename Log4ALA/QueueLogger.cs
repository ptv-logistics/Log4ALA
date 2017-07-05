using CustomLibraries.Threading;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Log4ALA
{
    public class QueueLogger
    {
        // Error message displayed when queue overflow occurs. 
        protected const String QueueOverflowMessage = "\n\nAzure Log Analytics buffer queue overflow. Message dropped.\n\n";

        // Minimal delay between attempts to reconnect in milliseconds. 
        protected const int MinDelay = 100;

        // Maximal delay between attempts to reconnect in milliseconds. 
        protected const int MaxDelay = 10000;

        public const int BatchSizeMax = 31457280; //30 mb quota limit per post
        public const int BatchSizeTimeSecMax = 120; //30 mb quota limit per post

        protected readonly BlockingCollection<string> Queue;
        protected readonly Thread WorkerThread;
        protected readonly Random Random = new Random();

        protected bool IsRunning = false;

        public string WorkspaceId { get; set; }

        private byte[] SharedKeyBytes { get; set; }
        private string sharedKey;
        public string SharedKey
        {
            set
            {
                sharedKey = value;
                SharedKeyBytes = Convert.FromBase64String(sharedKey);
            }
            get
            {
                return sharedKey;
            }
        }


        public string LogType { get; set; }

        public string AzureApiVersion { get; set; }
        public int? HttpDataCollectorRetry { get; set; }

        public bool LogMessageToFile { get; set; }
        public bool? AppendLogger { get; set; }
        public bool? AppendLogLevel { get; set; }
        public int? BatchSizeInBytes { get; set; }
        public int? BatchNumItems { get; set; }
        public int? BatchWaitInSec { get; set; }

        private Log4ALAAppender appender;

        private Timer logQueueSizeTimer = null;

        //private AlaTcpClient alaClient = null;


        // Size of the internal event queue. 
        public int? LoggingQueueSize { get; set; } = ConfigSettings.DEFAULT_LOGGER_QUEUE_SIZE;

        public QueueLogger(Log4ALAAppender appender)
        {
            Queue = new BlockingCollection<string>(LoggingQueueSize != null && LoggingQueueSize > 0 ? (int)LoggingQueueSize : ConfigSettings.DEFAULT_LOGGER_QUEUE_SIZE);

            WorkerThread = new Thread(new ThreadStart(Run));
            WorkerThread.Name = $"Azure Log Analytics Log4net Appender ({appender.Name})";
            WorkerThread.IsBackground = true;
            this.appender = appender;
            if (ConfigSettings.IsLogQueueSizeInterval)
            {
                CreateLogQueueSizeTimer();
            }
        }

        private void CreateLogQueueSizeTimer()
        {
            if (logQueueSizeTimer != null)
            {
                logQueueSizeTimer.Dispose();
                logQueueSizeTimer = null;
            }
            //create scheduler to log queue size to Azure Log Analytics start after 10 seconds and then log size each (2 minutes default)
            logQueueSizeTimer = new Timer(new TimerCallback(LogQueueSize), this, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(ConfigSettings.LogQueueSizeInterval));
        }

        private void LogQueueSize(object state)
        {
            try
            {
                QueueLogger queueLogger = (QueueLogger)state;
                string message = $"{queueLogger.appender.Name}-Size={queueLogger.Queue.Count}";
                queueLogger.appender.log.Inf(message, queueLogger.appender.logMessageToFile);

                HttpRequest($"{{\"Msg\":\"{message}\",\"DateValue\":\"{DateTime.UtcNow.ToString("o")}\"}}");

            }
            catch (Exception)
            {
                //continue
            }
        }

        private Stopwatch stopwatch = Stopwatch.StartNew();

        protected virtual void Run()
        {
            try
            {
                // Send data in queue.
                while (true)
                {
                    // Take data from queue.
                    string line = string.Empty;
                    int byteLength = 0;
                    int numItems = 0;
                    StringBuilder buffer = new StringBuilder(); //StringBuilderCache.Acquire();
                    buffer.Append("[");
                    stopwatch.Restart();
                    while (
                           (byteLength < BatchSizeInBytes && (stopwatch.ElapsedMilliseconds / 1000) < BatchSizeTimeSecMax) ||
                           (numItems < BatchNumItems && byteLength < BatchSizeMax && (stopwatch.ElapsedMilliseconds / 1000) < BatchSizeTimeSecMax) ||
                           ((stopwatch.ElapsedMilliseconds / 1000) < BatchWaitInSec && byteLength < BatchSizeMax)
                          )
                    {
                        try
                        {

                            if (Queue.TryTake(out line) && !string.IsNullOrWhiteSpace(line))
                            {
                                byteLength += System.Text.Encoding.Unicode.GetByteCount(line);

                                buffer.Append(line);
                                buffer.Append(",");
                                ++numItems;
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }


                    string alaPayLoad = buffer.ToString().TrimEnd(",".ToCharArray()); //StringBuilderCache.GetStringAndRelease(buffer).TrimEnd(",".ToCharArray());
                    if (string.IsNullOrWhiteSpace(alaPayLoad) || alaPayLoad.Length == 1)
                    {
                        continue;
                    }

                    HttpRequest($"{alaPayLoad}]");

                    try
                    {
                        appender.log.Inf($"[{appender.Name}] - {alaPayLoad}", appender.logMessageToFile);
                    }
                    catch (Exception)
                    {
                        //continue
                    }
                    buffer.Clear();
                }
            }
            catch (ThreadInterruptedException ex)
            {
                string errMessage = $"[{appender.Name}] - Azure Log Analytics HTTP Data Collector API client was interrupted. {ex}";
                appender.log.Err(errMessage);
                appender.extraLog.Err(errMessage);
            }
        }


        private static IEnumerable<JToken> AllTokens(JObject obj)
        {
            var toSearch = new Stack<JToken>(obj.Children());
            while (toSearch.Count > 0)
            {
                var inspected = toSearch.Pop();
                yield return inspected;
                foreach (var child in inspected)
                {
                    toSearch.Push(child);
                }
            }
        }

        protected AlaTcpClient ReopenConnection(AlaTcpClient client = null)
        {
            CloseConnection(client);

            var rootDelay = MinDelay;
            int retryCount = 0;

            while (true)
            {
                AlaTcpClient alaClient = null;
                try
                {

                    if (retryCount > HttpDataCollectorRetry)
                    {
                        ConnectionPool.Initialized = false;
                        retryCount = 0;
                    }

                    alaClient = OpenConnection();
                    try
                    {
                        appender.log.Inf($"[{appender.Name}] - successfully reconnected to AlaClient", true);
                    }
                    catch (Exception)
                    {
                        //continue
                    }
                    return alaClient;
                }
                catch (Exception ex)
                {
                    CloseConnection(alaClient);
                    string errMessage = $"[{appender.Name}] - Unable to connect to AlaClient => [{ex.Message}]";
                    appender.log.Err(errMessage);
                    appender.extraLog.Err(errMessage);
                }

                rootDelay *= 2;
                if (rootDelay > MaxDelay)
                    rootDelay = MaxDelay;

                var waitFor = rootDelay + Random.Next(rootDelay);

                ++retryCount;

                try
                {
                    Thread.Sleep(waitFor);
                }
                catch (Exception ex)
                {
                    string errMessage = $"[{appender.Name}] - Thread sleep exception => [{ex}]";
                    appender.log.Err(errMessage);
                    throw new ThreadInterruptedException();
                }
            }
        }

        protected virtual AlaTcpClient OpenConnection()
        {
            try
            {
                // Create AlaClient instance providing all needed parameters.
                AlaTcpClient alaClient = new AlaTcpClient(sharedKey, WorkspaceId);

                alaClient.Connect();

                return alaClient;

            }
            catch (Exception ex)
            {
                throw new IOException($"An error occurred while init AlaTcpClient.", ex);
            }
        }

        protected virtual void CloseConnection(AlaTcpClient alaClient)
        {
            if (alaClient != null)
            {
                alaClient.Close();
            }
        }

        public virtual void AddLine(string line)
        {
            if (!IsRunning)
            {
                WorkerThread.Start();
                IsRunning = true;
            }


            // Try to append data to queue.
            if (!Queue.TryAdd(line))
            {
                if (!Queue.TryAdd(line))
                {
                    appender.log.War($"[{appender.Name}] - QueueOverflowMessage");
                }
            }
        }

        public void interruptWorker()
        {
            WorkerThread.Interrupt();
        }


        private void HttpRequest(string log)
        {
            while (true)
            {
                AlaTcpClient alaClient = null;
                try
                {
                    alaClient = OpenConnection();

                    string result = string.Empty;

                    var utf8Encoding = new UTF8Encoding();
                    Byte[] content = utf8Encoding.GetBytes(log);

                    var rfcDate = DateTime.Now.ToUniversalTime().ToString("r");
                    var signature = HashSignature("POST", content.Length, "application/json", rfcDate, "/api/logs");

                    string alaServerAddr = $"{WorkspaceId}.ods.opinsights.azure.com";
                    string alaServerContext = $"/api/logs?api-version={AzureApiVersion}";

                    // Send request headers
                    var builder = new StringBuilder();
                    builder.AppendLine($"POST {alaServerContext} HTTP/1.1");
                    builder.AppendLine($"Host: {alaServerAddr}");
                    builder.AppendLine($"Content-Length: " + content.Length);   // only for POST request
                    builder.AppendLine("Content-Type: application/json");
                    builder.AppendLine($"Log-Type: {LogType}");
                    builder.AppendLine($"x-ms-date: {rfcDate}");
                    builder.AppendLine($"Authorization: {signature}");
                    builder.AppendLine("time-generated-field: DateValue");
                    builder.AppendLine("Connection: close");
                    builder.AppendLine();
                    var header = Encoding.ASCII.GetBytes(builder.ToString());

                    // Send http headers
                    alaClient.Write(header, 0, header.Length, true);

                    // Send payload data
                    string httpResultBody = alaClient.Write(content, 0, content.Length);


                    if (!string.IsNullOrWhiteSpace(httpResultBody))
                    {
                        string errMessage = httpResultBody;
                        appender.log.Err(errMessage);
                        throw new Exception(errMessage);
                    }
                    alaClient.Put();

                }
                catch (Exception ex)
                {
                    // Reopen the lost connection.
                    appender.log.War($"[{appender.Name}] - reopen lost connection. [{ex.Message}]");
                    ReopenConnection(alaClient);
                    continue;
                }

                break;
            }

        }


        /// <summary>
        /// SHA256 signature hash
        /// </summary>
        /// <returns></returns>
        private string HashSignature(string method, int contentLength, string contentType, string date, string resource)
        {
            var stringtoHash = method + "\n" + contentLength + "\n" + contentType + "\nx-ms-date:" + date + "\n" + resource;
            var encoding = new System.Text.ASCIIEncoding();
            var bytesToHash = encoding.GetBytes(stringtoHash);
            using (var sha256 = new HMACSHA256(SharedKeyBytes))
            {
                var calculatedHash = sha256.ComputeHash(bytesToHash);
                var stringHash = Convert.ToBase64String(calculatedHash);
                return "SharedKey " + WorkspaceId + ":" + stringHash;
            }
        }

    }
    
}
