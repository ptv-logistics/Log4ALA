using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Log4ALA
{
    public class QueueLogger
    {
        // Error message displayed when queue overflow occurs. 
        protected const String QueueOverflowMessage = "\n\nAzure Log Analytics buffer queue overflow. Message dropped.\n\n";

        protected readonly BlockingCollection<string> Queue;
        protected readonly Thread WorkerThread;
        protected static readonly Random Random = new Random();

        protected bool IsRunning = false;

        private byte[] SharedKeyBytes { get; set; }

        private Log4ALAAppender appender;

        private Timer logQueueSizeTimer = null;

        private HttpClient httpClient = null;
        private CancellationTokenSource tokenSource;
        private CancellationToken cToken;
        private ManualResetEvent manualResetEvent;

        public QueueLogger(Log4ALAAppender appender)
        {
            this.tokenSource = new CancellationTokenSource();
            this.cToken = tokenSource.Token;
            this.manualResetEvent = new ManualResetEvent(false);
            this.appender = appender;
            Queue = new BlockingCollection<string>(appender.LoggingQueueSize != null && appender.LoggingQueueSize > 0 ? (int)appender.LoggingQueueSize : ConfigSettings.DEFAULT_LOGGER_QUEUE_SIZE);
            SharedKeyBytes = Convert.FromBase64String(appender.SharedKey);

            WorkerThread = new Thread(new ThreadStart(Run));
            WorkerThread.Name = $"Azure Log Analytics Log4net Appender ({appender.Name})";
            WorkerThread.IsBackground = true;
            WorkerThread.Priority = (ThreadPriority)Enum.Parse(typeof(ThreadPriority), appender.ThreadPriority);
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
                queueLogger.appender.log.Inf(message, queueLogger.appender.LogMessageToFile);

                HttpRequest($"{{\"Msg\":\"{message}\",\"{appender.coreFields.DateFieldName}\":\"{DateTime.UtcNow.ToString("o")}\"}}", 1);

            }
            catch (Exception)
            {
                //continue
            }
        }

        private Stopwatch stopwatch = Stopwatch.StartNew();
        private StringBuilder buffer = new StringBuilder();//StringBuilderCache.Acquire();

        protected virtual void Run()
        {
            try
            {

                // Was cancellation already requested by AbortWorker?
                if (this.cToken.IsCancellationRequested == true)
                {
                    appender.log.Inf($"[{appender.Name}] was cancelled before it got started.");
                    cToken.ThrowIfCancellationRequested();
                }


                Connect(true);

                int qReadTimeout = (int)appender.QueueReadTimeout;

                // Send data in queue.
                while (true)
                {

                    buffer.Clear();

                    // Take data from queue.
                    string line = string.Empty;
                    int byteLength = 0;
                    int numItems = 0;
                    buffer.Append('[');
                    stopwatch.Restart();

                    while (((byteLength < appender.BatchSizeInBytes && (stopwatch.ElapsedMilliseconds / 1000) < appender.BatchWaitMaxInSec) ||
                           (numItems < appender.BatchNumItems && byteLength < ConfigSettings.BATCH_SIZE_MAX && (stopwatch.ElapsedMilliseconds / 1000) < appender.BatchWaitMaxInSec) ||
                           ((stopwatch.ElapsedMilliseconds / 1000) < appender.BatchWaitInSec && byteLength < ConfigSettings.BATCH_SIZE_MAX))
                          )
                    {
                        try
                        {
                            if (Queue.TryTake(out line, qReadTimeout))
                            {
                                byteLength += System.Text.Encoding.UTF8.GetByteCount(line);

                                if (numItems >= 1)
                                {
                                    buffer.Append(',');
                                }

                                buffer.Append(line);
                                ++numItems;
                                line = string.Empty;

                            }

                            if (Queue.IsCompleted)
                            {
                                break;
                            }

                        }
                        catch (Exception ee)
                        {
                            if (Queue.IsCompleted)
                            {
                                break;
                            }

                            if (this.cToken.IsCancellationRequested != true)
                            {
                                string errMessage = $"[{appender.Name}] - Azure Log Analytics problems take log message from queue: {ee.Message}";
                                appender.log.Err(errMessage);
                            }
                        }

                    }

                    buffer.Append(']');

                    var alaPayLoad = buffer.ToString();

                    if (alaPayLoad.Length <= 1 || alaPayLoad.Equals("[]"))
                    {
                        string infoMessage = $"[{appender.Name}] -  {nameof(appender.BatchWaitMaxInSec)} exceeded time out of {appender.BatchWaitMaxInSec} seconds there is no data to write to Azure Log Analytics at the moment";
                        appender.log.Inf(infoMessage, appender.LogMessageToFile);
                        continue;
                    }

                    HttpRequest(alaPayLoad, numItems);

                    //stop loop if background worker thread was canceled by AbortWorker
                    if (this.cToken.IsCancellationRequested == true)
                    {
                        break;
                    }

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

        protected void Connect(bool init = false)
        {
            CloseConnection();

            var rootDelay = ConfigSettings.MIN_DELAY;
            int retryCount = 0;

            while (true)
            {
                try
                {
                    OpenConnection();
                    try
                    {
                        appender.log.Inf($"[{appender.Name}] - successfully {(init ? "connected" : "reconnected")} to AlaClient", init ? true : appender.LogMessageToFile);
                    }
                    catch (Exception)
                    {
                        //continue
                    }
                    break;
                }
                catch (Exception ex)
                {
                    CloseConnection();
                    string errMessage = $"Unable to {(init ? "connect" : "reconnect")} to AlaClient => [{ex.Message}] retry [{(retryCount + 1)}]";
                    if (ConfigSettings.ALAEnableDebugConsoleLog)
                    {
                        System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|ERROR|[{nameof(QueueLogger)}.Connect] - [{errMessage}]");
                    }
                    appender.log.Err($"[{appender.Name}] - {errMessage}");
                    appender.extraLog.Err($"[{appender.Name}] - {errMessage}");
                }

                rootDelay *= 2;
                if (rootDelay > ConfigSettings.MAX_DELAY)
                    rootDelay = ConfigSettings.MAX_DELAY;

                var waitFor = rootDelay + Random.Next(rootDelay);

                ++retryCount;

                try
                {
                    Thread.Sleep(waitFor);
                }
                catch (Exception ex)
                {
                    string errMessage = $"Thread sleep exception => [{ex.StackTrace}]";
                    if (ConfigSettings.ALAEnableDebugConsoleLog)
                    {
                        System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|ERROR|[{nameof(QueueLogger)}.Connect] - [{errMessage}]");
                    }
                    appender.log.Err($"[{appender.Name}] - {errMessage}");
                    throw new ThreadInterruptedException();
                }
            }
        }

        protected virtual void OpenConnection()
        {
            try
            {
                if (httpClient == null)
                {
                    var handler = new TimeoutHandler
                    {
                        Appender = appender,
                        DefaultTimeout = TimeSpan.FromMilliseconds(appender.HttpClientRequestTimeout),
                        InnerHandler = new HttpClientHandler()
                    };
                    httpClient = new HttpClient(handler);
                    httpClient.Timeout = TimeSpan.FromMilliseconds(appender.HttpClientTimeout);
                }
            }
            catch (Exception ex)
            {
                if (ConfigSettings.ALAEnableDebugConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|ERROR|[{nameof(QueueLogger)}.OpenConnection] - [{ex.StackTrace}]");
                }
                throw new IOException($"An error occurred while init httpClient.", ex);
            }
        }

        protected virtual void CloseConnection()
        {
            try
            {
                if (httpClient != null)
                {
                    try
                    {
                        httpClient.CancelPendingRequests();
                    }
                    catch (Exception)
                    {
                        //contiune
                    }
                    finally
                    {
                        try
                        {
                            httpClient.Dispose();
                        }
                        catch (Exception)
                        {
                            //contiune
                        }
                        httpClient = null;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ConfigSettings.ALAEnableDebugConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|ERROR|[{nameof(QueueLogger)}.CloseConnection] - [{ex.StackTrace}]");
                }
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
            if (!Queue.IsCompleted && !Queue.TryAdd(line))
            {
                if (!Queue.TryAdd(line))
                {
                    appender.log.War($"[{appender.Name}] - QueueOverflowMessage", appender.LogMessageToFile);
                }
            }
        }

        public void AbortWorker()
        {
            if (WorkerThread != null)
            {
                //controlled cancelation of the background worker thread to trigger sending 
                //the queued data to ALA before abort the thread
                this.tokenSource.Cancel();

                Queue.CompleteAdding();

                //wait until the worker thread has flushed the locally queued log data
                //and has successfully sent the log data to Azur Log Analytics by HttpRequest(string log) or if
                //the timeout of 10 seconds reached
                manualResetEvent.WaitOne(TimeSpan.FromSeconds(ConfigSettings.AbortTimeoutSeconds));
            }
        }



        private StringBuilder headerBuilder = new StringBuilder();

        private void HttpRequest(string log, int numItems)
        {
    
            var maxRetryAttempts = appender.HttpDataCollectorRetry;
            RetryOnException(maxRetryAttempts, (id) =>
            {
                return PostData(DateTime.Now.ToUniversalTime().ToString("r"), log, numItems, id);
            });
        }


        // Send a request to the POST API endpoint
        public string PostData(string date, string json, int numItems, string id = null)
        {

            // for http debugging please install httpbin (https://github.com/requests/httpbin) locally as docker container:
            //
            //      docker pull kennethreitz/httpbin
            //
            // and start with:
            //
            //      docker run -p 80:80 kennethreitz / httpbin
            //
            // and replacefor alaServerAddr with http://localhost/anything by setting the property debugHTTPReqURI=http://localhost/anything to inspect the http request 
            // which will then be logged into log4ALA_info.log instead of sending to Azure Log Analytics data collector API.


            var alaQueryContext = $"?api-version={appender.AzureApiVersion}";
            string url = "https://" + appender.WorkspaceId + $".{appender.LogAnalyticsDNS}/api/logs{alaQueryContext}";

            if (!string.IsNullOrWhiteSpace(appender.DebugHTTPReqURI))
            {
                url = $"{appender.DebugHTTPReqURI}{alaQueryContext}";
            }


            var utf8Encoding = new UTF8Encoding();
            Byte[] content = utf8Encoding.GetBytes(json);
            var byteArrayContent = new ByteArrayContent(content);

            var signature = HashSignature("POST", content.Length, "application/json", date, "/api/logs");
            httpClient.DefaultRequestHeaders.Clear();

            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("Log-Type", appender.LogType);
            httpClient.DefaultRequestHeaders.Add("Authorization", signature);
            httpClient.DefaultRequestHeaders.Add("x-ms-date", date);
            httpClient.DefaultRequestHeaders.Add("time-generated-field", appender.coreFields.DateFieldName);

            HttpContent httpContent = byteArrayContent;
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            //Task<HttpResponseMessage> response = httpClient.PostAsync(new Uri(url), httpContent, this.cToken);

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
            httpRequestMessage.Content = httpContent;

            // Uncomment to test per-request timeout
            httpRequestMessage.SetTimeout(TimeSpan.FromMilliseconds(appender.HttpClientRequestTimeout));

            // Uncomment to test that cancellation still works properly
            //this.tokenSource.CancelAfter(TimeSpan.FromSeconds(2));

 
            Task<HttpResponseMessage> response;
            try
            {
                //https://stackoverflow.com/questions/14435520/why-use-httpclient-for-synchronous-connection
                response = httpClient.SendAsync(httpRequestMessage, this.cToken);

                if (string.IsNullOrWhiteSpace(id))
                {
                    id = $"{response.Id}";
                }

                if (ConfigSettings.ALAEnableDebugConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - start post numItems [{numItems}] id [{id}]");
                }

            }
            catch (Exception sasy)
            {
                if (ConfigSettings.ALAEnableDebugConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - [httpClient.SendAsync EXCEPTION. {sasy.StackTrace}]");
                }

                throw;
            }

            while (!response.IsFaulted && !response.IsCompleted && !response.IsCanceled)
            {
                System.Threading.Thread.Sleep(new TimeSpan(0, 0, 1));
            }

            var statusCode = "UNKNOWN";

            try
            {
                statusCode = response.Result.StatusCode.ToString();
            }
            catch (Exception)
            {
                //continue
            }

            if (ConfigSettings.ALAEnableDebugConsoleLog)
            {
                System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - numItems [{numItems}] - id/faulted/completed/canceled/statusCode [{id}/{response.IsFaulted}/{response.IsCompleted}/{response.IsCanceled}/{statusCode}]");
            }

            if (response.IsFaulted)
            {
                throw new Exception($"{id}|HTTPClient response isFaulted");
            }
            
            appender.log.Inf($"[{appender.Name}] - {json}", appender.LogMessageToFile);

            return $"{id}";

        }


        public void RetryOnException(int times, Func<string,string> operation)
        {
            var task = Task.Run(() =>
            {
                var attempts = 0;
                var rootDelay = ConfigSettings.MIN_DELAY;

                var id = string.Empty;

                do
                {
                    try
                    {
                        attempts++;
                        var idSuccess = operation(id);
                        string msg = $"post with id [{idSuccess}] succeeded";
                        if (ConfigSettings.ALAEnableDebugConsoleLog)
                        {
                            System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {msg}");
                        }

                        if (!string.IsNullOrWhiteSpace(id))
                        {
                             appender.log.Err($"[{appender.Name}] - {msg}");
                        }

                        break; // Sucess! Lets exit the loop!
                    }
                    catch (Exception ex)
                    {

                        id = ex.Message.Split("|".ToCharArray())[0];
                        var message = ex.Message.Split("|".ToCharArray())[1];

                        if (attempts == times)
                        {
                            string retryMsg = $"retry limit for id [{id}] reached";
                            if (ConfigSettings.ALAEnableDebugConsoleLog)
                            {
                                System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {retryMsg}");
                            }
                            appender.log.Err($"[{appender.Name}] - {retryMsg}");

                            break;
                        }

                        rootDelay *= 2;
                        if (rootDelay > ConfigSettings.MAX_DELAY)
                            rootDelay = ConfigSettings.MAX_DELAY;

                        var waitFor = rootDelay + Random.Next(rootDelay);

                        string msg = $"Exception caught on attempt {attempts} - will retry request with id [{id}]  after delay {waitFor}";

                        if (ConfigSettings.ALAEnableDebugConsoleLog)
                        {
                            System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {msg}");
                        }
                        appender.log.Err($"[{appender.Name}] - {msg}");


                        Task.Delay(TimeSpan.FromMilliseconds(waitFor)).Wait();
                    }

                   
                } while (true);

                //unblock AbortWorker if AbortWorker has canceld the background worker thread
                if (this.cToken.IsCancellationRequested == true)
                {
                    this.manualResetEvent.Set();
                }

            });
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
                return "SharedKey " + appender.WorkspaceId + ":" + stringHash;
            }
        }

    }

}
