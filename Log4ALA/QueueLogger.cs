using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        protected readonly BlockingCollection<string> Queue;
        protected readonly Thread WorkerThread;
        protected readonly Random Random = new Random();

        protected bool IsRunning = false;

        private HTTPDataCollectorAPI.ICollector aLACollector;

        public string WorkspaceId { get; set; }
        public string SharedKey { get; set; }
        public string LogType { get; set; }
        public string AzureApiVersion { get; set; }
        public int? HttpDataCollectorRetry { get; set; }

        public bool LogMessageToFile { get; set; }
        public bool? AppendLogger { get; set; }
        public bool? AppendLogLevel { get; set; }

        private Log4ALAAppender appender;

        private Timer logQueueSizeTimer = null;


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
            logQueueSizeTimer = new Timer(new TimerCallback(LogQueueSize), this, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(ConfigSettings.LogQueueSizeInterval));
        }

        private void LogQueueSize(object state)
        {
            try
            {
                QueueLogger queueLogger = (QueueLogger)state;
                string message = $"{queueLogger.appender.Name}-Size={queueLogger.Queue.Count}";
                queueLogger.appender.log.Inf(message, queueLogger.appender.logMessageToFile);

                if (aLACollector != null)
                {
                    Task task = Task.Run(async () => { await this.aLACollector.Collect("Log4ALAConnection", $"{{\"Msg\":\"{message}\",\"DateValue\":\"{DateTime.UtcNow.ToString("o")}\"}}", AzureApiVersion, "DateValue"); });
                }

            }
            catch (Exception)
            {
                //continue
            }
        }


        protected virtual void Run()
        {
            try
            {
                // Open connection.
                ReopenConnection();

                // Send data in queue.
                while (true)
                {
                    // Take data from queue.
                    var line = Queue.Take();


                    // Send data, reconnect if needed.
                    while (true)
                    {
                        try
                        {
                            if (aLACollector != null)
                            {
                                Task task = Task.Run(async () => { await this.aLACollector.Collect(LogType, line, AzureApiVersion, "DateValue"); });
                                task.Wait();
                                appender.log.Inf($"[{appender.Name}] - {line}", appender.logMessageToFile);
                            }
                            else
                            {
                                appender.log.War($"[{appender.Name}] - HTTP Data Collector not ininialized..try initilaizing");
                                ReopenConnection();
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Reopen the lost connection.
                            appender.log.War($"[{appender.Name}] - reopen lost connection. [{ex}]");
                            ReopenConnection();
                            continue;
                        }

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

        protected virtual void ReopenConnection()
        {
            CloseConnection();

            var rootDelay = MinDelay;
            while (true)
            {
                try
                {
                    OpenConnection();
                    try
                    {
                        appender.log.Inf($"[{appender.Name}] - successfully reconnected to Azure Log Analytics HTTP Data Collector API", true);
                    }
                    catch (Exception)
                    {
                        //continue
                    }
                    return;
                }
                catch (Exception ex)
                {
                    string errMessage = $"[{appender.Name}] - Unable to connect to Azure Log Analytics HTTP Data Collector API => [{ex}]";
                    appender.log.Err(errMessage);
                    appender.extraLog.Err(errMessage);
                    CloseConnection();
                }

                rootDelay *= 2;
                if (rootDelay > MaxDelay)
                    rootDelay = MaxDelay;

                var waitFor = rootDelay + Random.Next(rootDelay);

                try
                {
                    Thread.Sleep(waitFor);
                }
                catch
                {
                    throw new ThreadInterruptedException();
                }
            }
        }

        protected virtual void OpenConnection()
        {
            try
            {
                if (aLACollector == null)
                {
                    aLACollector = new HTTPDataCollectorAPI.Collector(WorkspaceId, SharedKey);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"An error occurred while init/ping Azure Log Analytics HTTP Data Collector API.", ex);
            }
        }

        protected virtual void CloseConnection()
        {
            if (aLACollector != null)
            {
                aLACollector = null;
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


    }
}
