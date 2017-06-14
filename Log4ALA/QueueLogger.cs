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

        // Size of the internal event queue. 
        protected const int QueueSize = 1000000;

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


        public QueueLogger()
        {
            Queue = new BlockingCollection<string>(QueueSize);

            WorkerThread = new Thread(new ThreadStart(Run));
            WorkerThread.Name = "Azure Log Analytics Log4net Appender";
            WorkerThread.IsBackground = true;
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
                            Task task = Task.Run(async () => { await this.aLACollector.Collect(LogType, line, AzureApiVersion, "DateValue"); });
                            task.Wait();
                        }
                        catch (Exception)
                        {
                            // Reopen the lost connection.
                            ReopenConnection();
                            continue;
                        }

                        break;
                    }
                }
            }
            catch (ThreadInterruptedException ex)
            {
                Log4ALAAppender.Error($"Azure Log Analytics HTTP Data Collector API client was interrupted. {ex}", false);
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
                    Log4ALAAppender.Info("successfully reconnected to Azure Log Analytics HTTP Data Collector API");
                    return;
                }
                catch (Exception ex)
                {
                    Log4ALAAppender.Error($"Unable to connect to Azure Log Analytics HTTP Data Collector API => [{ex}]");
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
                    Task task = Task.Run(async () => { await this.aLACollector.Collect("Log4ALAConnection", $"{{\"Message\":\"ping OpenConnection\",\"DateValue\":\"{DateTime.UtcNow.ToString("o")}\"}}", AzureApiVersion, "DateValue"); });
                    task.Wait();
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
                    Log4ALAAppender.Warn(QueueOverflowMessage);
                }
            }
        }

        public void interruptWorker()
        {
            WorkerThread.Interrupt();
        }


    }
}
