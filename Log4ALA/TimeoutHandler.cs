﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Log4ALA
{
    public class TimeoutHandler : DelegatingHandler
    {
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(100);

        public Log4ALAAppender Appender { get; set; }


        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (var cts = GetCancellationTokenSource(request, cancellationToken))
            {
                try
                {
                    return await base.SendAsync(request, cts?.Token ?? cancellationToken);
                }
                catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
                {
                    if (Appender.EnableDebugConsoleLog)
                    {
                        var message = $@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{Appender.Name}]|ERROR|[{nameof(TimeoutHandler)}.SendAsync] - {nameof(OperationCanceledException)}: [{oce.StackTrace}]";
                        Appender.log.Deb($"{message}", Appender.EnableDebugConsoleLog);
                        System.Console.WriteLine(message);
                    }

                    throw new TimeoutException();
                }
            }
        }

        private CancellationTokenSource GetCancellationTokenSource(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var timeout = request.GetTimeout() ?? DefaultTimeout;
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                // No need to create a CTS if there's no timeout
                return null;
            }
            else
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);
                return cts;
            }
        }
    }
}
