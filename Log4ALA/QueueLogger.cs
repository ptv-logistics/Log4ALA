﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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

        private int httpClientId = 0;

        protected bool IsRunning = false;

        private byte[] SharedKeyBytes { get; set; }

        private Log4ALAAppender appender;

        private Timer logQueueSizeTimer = null;

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

        protected HttpClient Connect(bool init = false, string httpClientId = null)
        {
            HttpClient httpClient = null;

            var rootDelay = ConfigSettings.MIN_DELAY;
            int retryCount = 0;

            while (true)
            {
                try
                {
                    httpClient = OpenConnection();
                    try
                    {
                        appender.log.Inf($"[{appender.Name}] - successfully {(init ? "connected" : "reconnected")} to Log Analytics Data Collector API", init ? true : appender.LogMessageToFile);
                        if (ConfigSettings.ALAEnableDebugConsoleLog)
                        {
                            System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {(!string.IsNullOrWhiteSpace(httpClientId) ? $"httpClient [{httpClientId}] " : "")} successfully {(init ? "connected" : "reconnected")} to Log Analytics Data Collector API");
                        }
                    }
                    catch (Exception)
                    {
                        //continue
                    }
                    break;
                }
                catch (Exception ex)
                {
                    CloseConnection(httpClient);
                    string errMessage = $"{(!string.IsNullOrWhiteSpace(httpClientId) ? $"httpClient [{httpClientId}] " : "")} Unable to {(init ? "connect" : "reconnect")} to Log Analytics Data Collector API => [{ex.Message}] retry [{(retryCount + 1)}]";
                    if (ConfigSettings.ALAEnableDebugConsoleLog)
                    {
                        System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|ERROR|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {errMessage}");
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
                    string errMessage = $"{(!string.IsNullOrWhiteSpace(httpClientId) ? $"httpClient [{httpClientId}] " : "")} Thread sleep exception => [{ex.StackTrace}]";
                    if (ConfigSettings.ALAEnableDebugConsoleLog)
                    {
                        System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|ERROR|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {errMessage}");
                    }
                    appender.log.Err($"[{appender.Name}] - {errMessage}");
                    throw new ThreadInterruptedException();
                }
            }

            return httpClient;
        }

        protected virtual HttpClient OpenConnection()
        {
            try
            {
                HttpClient httpClient;

                var handler = new TimeoutHandler
                {
                    Appender = appender,
                    DefaultTimeout = TimeSpan.FromMilliseconds(appender.HttpClientRequestTimeout),
                    InnerHandler = new HttpClientHandler()
                };
                httpClient = new HttpClient(handler);
                httpClient.Timeout = TimeSpan.FromMilliseconds(appender.HttpClientTimeout);

                return httpClient;
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

        protected virtual void CloseConnection(HttpClient httpClient, string httpClientId = null)
        {
            try
            {
                if (httpClient != null)
                {
                    try
                    {
                        httpClient.CancelPendingRequests();
                        if (ConfigSettings.ALAEnableDebugConsoleLog)
                        {
                            System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {(!string.IsNullOrWhiteSpace(httpClientId) ? $"httpClient [{httpClientId}] " : "")} cancel pending requests");
                        }

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
                            if (ConfigSettings.ALAEnableDebugConsoleLog)
                            {
                                System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {(!string.IsNullOrWhiteSpace(httpClientId) ? $"httpClient [{httpClientId}] " : "")} disposed");
                            }
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
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|ERROR|[{nameof(QueueLogger)}.CloseConnection] - {(!string.IsNullOrWhiteSpace(httpClientId) ? $"httpClient [{httpClientId}] " : "")} - [{ex.StackTrace}]");
                }
            }

        }


        protected virtual void CancelPendingRequests(HttpClient httpClient, string httpClientId = null)
        {
            try
            {
                if (httpClient != null)
                {
                    try
                    {
                        httpClient.CancelPendingRequests();
                        if (ConfigSettings.ALAEnableDebugConsoleLog)
                        {
                            System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {(!string.IsNullOrWhiteSpace(httpClientId) ? $"httpClient [{httpClientId}] " : "")} cancel pending requests");
                        }

                    }
                    catch (Exception)
                    {
                        //contiune
                    }
                }
            }
            catch (Exception ex)
            {
                if (ConfigSettings.ALAEnableDebugConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|ERROR|[{nameof(QueueLogger)}.CancelPendingRequests] - {(!string.IsNullOrWhiteSpace(httpClientId) ? $"httpClient [{httpClientId}] " : "")} - [{ex.StackTrace}]");
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

        private HttpClient httpGlobalClient = null;

        private void HttpRequest(string log, int numItems)
        {

            if (httpClientId >= 100000)
            {
                httpClientId = 0;
            }

            var idStart = $"{++httpClientId}";
            var maxRetryAttempts = appender.HttpDataCollectorRetry;
            RetryOnException(maxRetryAttempts, (id, obj) =>
            {
                //do httpRequest
                return PostData(DateTime.Now.ToUniversalTime().ToString("r"), log, numItems, (string.IsNullOrWhiteSpace(id) ? idStart : id), obj);
            }, () =>
            {
                if (httpGlobalClient == null)
                {
                    httpGlobalClient = Connect(true, idStart);
                }
                //init httpClient
                return httpGlobalClient;

            });
        }


        // Send a request to the POST API endpoint
        public OperationResult PostData(string date, string json, int numItems, string id = null, object obj = null)
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

            Task<HttpResponseMessage> response;

            HttpClient httpClient = null;
            Exception httpClientEx = null;

            try
            {
                httpClient = (HttpClient)obj;

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

                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
                httpRequestMessage.Content = httpContent;

                // Uncomment to test per-request timeout
                httpRequestMessage.SetTimeout(TimeSpan.FromMilliseconds(appender.HttpClientRequestTimeout));

                // Uncomment to test that cancellation still works properly
                //this.tokenSource.CancelAfter(TimeSpan.FromSeconds(2));
                //https://stackoverflow.com/questions/14435520/why-use-httpclient-for-synchronous-connection
                response = httpClient.SendAsync(httpRequestMessage, this.cToken);


                if (ConfigSettings.ALAEnableDebugConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {(!string.IsNullOrWhiteSpace(id) ? $"httpClient [{id}] " : "")} start post numItems [{numItems}]");
                }

            }
            catch (Exception sasy)
            {
                CancelPendingRequests(httpClient, id);
                if (ConfigSettings.ALAEnableDebugConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {(!string.IsNullOrWhiteSpace(id) ? $"httpClient [{id}] " : "")} SendAsync EXCEPTION. [{sasy.StackTrace}]");
                }


                httpClientEx = sasy;
                return new OperationResult() { Id = $"{id}", Ex = httpClientEx, Obj = httpClient };

            }

            try
            {
                while (!response.IsFaulted && !response.IsCompleted && !response.IsCanceled)
                {
                    System.Threading.Thread.Sleep(new TimeSpan(0, 0, 1));
                }

            }
            catch (Exception)
            {
                if (ConfigSettings.ALAEnableDebugConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {(!string.IsNullOrWhiteSpace(id) ? $"httpClient [{id}] " : "")} error during wait for HttpResponseMessage");
                }
            }

            var statusCode = HttpStatusCode.BadRequest;
            var result = string.Empty;
            var httpStatusCode2Retry = false;

            try
            {
                statusCode = response.Result.StatusCode;

                if (!statusCode.Equals(HttpStatusCode.OK))
                {
                    result = response.Result.ReasonPhrase;
                    httpStatusCode2Retry = statusCode.Equals(HttpStatusCode.InternalServerError) || statusCode.Equals(HttpStatusCode.ServiceUnavailable) || statusCode.Equals((HttpStatusCode)429);
                }

            }
            catch (Exception)
            {
                //continue
            }

            if (ConfigSettings.ALAEnableDebugConsoleLog)
            {
                System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {(!string.IsNullOrWhiteSpace(id) ? $"httpClient [{id}] " : "")} numItems [{numItems}] - faulted/completed/canceled/statusCode [{response.IsFaulted}/{response.IsCompleted}/{response.IsCanceled}/{statusCode}{(string.IsNullOrWhiteSpace(result) ? string.Empty : $" - {result}")}]");
            }

            bool isFaulted = response.IsFaulted;

            if (isFaulted)
            {
                CancelPendingRequests(httpClient, id);
                httpClientEx = new Exception($"HTTPClient response {(isFaulted ? $"isFaulted {(response.Exception != null ? $"[{response.Exception.Message}]" : string.Empty)}" : "isCanceled")}");
            }
            else if (httpStatusCode2Retry)
            {
                CancelPendingRequests(httpClient, id);
                httpClientEx = new Exception($"HTTPClient response {statusCode} - {result}");
            }
            else if (!statusCode.Equals(HttpStatusCode.OK) && !httpStatusCode2Retry)
            {
                CancelPendingRequests(httpClient, id);
                httpClientEx = new HttpNoRetryException($"HTTPClient response {statusCode} - {result}");
            }


            appender.log.Inf($"[{appender.Name}] - {json}", appender.LogMessageToFile);

            return new OperationResult() { Id = $"{id}", Ex = httpClientEx, Obj = httpClient };

        }


        public void RetryOnException(int times, Func<string, object, OperationResult> operation, Func<object> createObject)
        {
            //var task = Task.Run(() =>
            //{
            var attempts = 0;
            var rootDelay = ConfigSettings.MIN_DELAY;

            var id = string.Empty;

            var obj = createObject();

            bool isRetry = false;

            do
            {
                try
                {
                    attempts++;
                    var opResult = operation(id, obj);

                    id = opResult.Id;
                    obj = opResult.Obj;

                    if (opResult.Ex != null)
                    {
                        throw opResult.Ex;
                    }

                    string msg = $"{(!string.IsNullOrWhiteSpace(id) ? $"httpClient [{id}] " : "")} post succeeded";
                    if (ConfigSettings.ALAEnableDebugConsoleLog)
                    {
                        System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {msg}");
                    }

                    if (isRetry)
                    {
                        isRetry = false;
                        appender.log.Err($"[{appender.Name}] - {msg}");
                    }

                    break; // Sucess! Lets exit the loop!
                }
                catch (Exception ex) when (ex is HttpNoRetryException)
                {
                    var message = ex.Message;

                    string retryMsg = $"{(!string.IsNullOrWhiteSpace(id) ? $"httpClient [{id}] " : "")} Exception [{message}] caught - retry canceled";
                    if (ConfigSettings.ALAEnableDebugConsoleLog)
                    {
                        System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {retryMsg}");
                    }
                    appender.log.Err($"[{appender.Name}] - {retryMsg}");

                    break;

                }
                catch (Exception ex)
                {
                    var message = ex.Message;

                    if (attempts == times)
                    {
                        string retryMsg = $"{(!string.IsNullOrWhiteSpace(id) ? $"httpClient [{id}] " : "")} retry limit reached";
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

                    string msg = $"{(!string.IsNullOrWhiteSpace(id) ? $"httpClient [{id}] " : "")} Exception [{message}] caught on attempt {attempts} - will retry request after delay {waitFor}";

                    if (ConfigSettings.ALAEnableDebugConsoleLog)
                    {
                        System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appender.Name}]|INFO|[{nameof(QueueLogger)}.httpClient-PostData-Result] - {msg}");
                    }
                    appender.log.Err($"[{appender.Name}] - {msg}");


                    Task.Delay(TimeSpan.FromMilliseconds(waitFor)).Wait();
                    isRetry = true;

                }

                //unblock AbortWorker if AbortWorker has canceld the background worker thread
                if (this.cToken.IsCancellationRequested == true)
                {
                    this.manualResetEvent.Set();
                }


            } while (true);


            //});
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

    public class OperationResult
    {
        public string Id { get; set; } = null;
        public object Obj { get; set; } = null;
        public Exception Ex { get; set; } = null;

    }


    public sealed class HttpService
    {
        private static readonly HttpClient instance = new HttpClient();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static HttpService()
        {
        }

        private HttpService()
        {
        }

        public static HttpClient Instance
        {
            get
            {
                return instance;
            }
        }
    }

    public class HttpNoRetryException : Exception
    {
        public HttpNoRetryException()
        {
        }

        public HttpNoRetryException(string message)
            : base(message)
        {
        }

        public HttpNoRetryException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

}
