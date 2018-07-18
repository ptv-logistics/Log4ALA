using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Log4ALA
{
    public class AlaTcpClient
    {
        // Azure Log Analytics API server address. 
        protected const String AlaApiUrl = "ods.opinsights.azure.com";

        private byte[] sharedKeyBytes;
        private string workSpaceID;

        // Creates AlaClient instance. 
        public AlaTcpClient(string sharedKey, string workSpaceId, bool debugConsoleLog = false, string logAppenderName = null)
        {
            appenderName = logAppenderName;
            debConsoleLog = debugConsoleLog;
            sharedKeyBytes = Convert.FromBase64String(sharedKey);
            workSpaceID = workSpaceId;
            serverAddr = $"{workSpaceID}.{AlaApiUrl}";
            //will be ingored under dotnetcore https://github.com/dotnet/corefx/issues/10727
            ConfigureServiceEndpoint(serverAddr, true, true, false, debConsoleLog, appenderName);
        }

        private int tcpPort = 443;
        private TcpClient client = null;
        private SslStream sslStream = null;
        private String serverAddr;
        private bool debConsoleLog = false;
        private string appenderName;


        private Stream ActiveStream
        {
            get
            {
                return sslStream;
            }
        }

        public void Connect()
        {
            try
            {
                client = new TcpClient(serverAddr, tcpPort);
                client.NoDelay = true;

                sslStream = new SslStream(client.GetStream());
                sslStream.AuthenticateAsClient(serverAddr);
            }
            catch (Exception ex)
            {

                if(debConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appenderName}]|ERROR|[{nameof(AlaTcpClient)}.Connect] - [{ex.StackTrace}]");
                }
            }

        }

        public string Write(byte[] buffer, int offset, int count, bool isHeader = false)
        {

            string result = string.Empty;

            try
            {
                ActiveStream.Write(buffer, offset, count);

                if (isHeader)
                {
                    return "isHeader";
                }

                Flush();


                // receive data
                using (var memory = new MemoryStream())
                {
                    ActiveStream.CopyTo(memory);
                    memory.Position = 0;
                    var data = memory.ToArray();

                    if (data != null && data.Length > 0)
                    {

                        var index = BinaryMatch(data, Encoding.ASCII.GetBytes("\r\n\r\n")) + 4;
                        var headers = Encoding.ASCII.GetString(data, 0, index);
                        memory.Position = index;

                        if (headers.IndexOf("Content-Encoding: gzip") > 0)
                        {
                            using (GZipStream decompressionStream = new GZipStream(memory, CompressionMode.Decompress))
                            using (var decompressedMemory = new MemoryStream())
                            {
                                decompressionStream.CopyTo(decompressedMemory);
                                decompressedMemory.Position = 0;
                                result = Encoding.UTF8.GetString(decompressedMemory.ToArray());
                            }
                        }
                        else
                        {
                            result = Encoding.UTF8.GetString(data, index, data.Length - index);
                            //result = Encoding.GetEncoding("gbk").GetString(data, index, data.Length - index);
                        }
                    }
                    else
                    {
                        result = "couldn't read response from stream";
                    }
                }
            }
            catch (Exception e)
            {
                result = $"couldn't read response from stream: [{e.Message}]";

                if (debConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appenderName}]|ERROR|[{nameof(AlaTcpClient)}.Write] - [{e.StackTrace}]");
                }
            }

            return result;
        }

        private static int BinaryMatch(byte[] input, byte[] pattern)
        {
            int sLen = input.Length - pattern.Length + 1;
            for (int i = 0; i < sLen; ++i)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; ++j)
                {
                    if (input[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Flush()
        {
            try
            {
                ActiveStream.Flush();
            }
            catch (Exception ex)
            {
                if (debConsoleLog)
                {
                    System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appenderName}]|ERROR|[{nameof(AlaTcpClient)}.Flush] - [{ex.StackTrace}]");
                }
            }

        }


        public void Close()
        {
            if (client != null)
            {
                try
                {
                    if (ActiveStream != null)
                    {
                        ActiveStream.Dispose();
                    }
                    client.Close();
                    if (client != null)
                    {
                        client = null;
                    }
                }
                catch(Exception ex)
                {
                    if (debConsoleLog)
                    {
                        System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appenderName}]|ERROR|[{nameof(AlaTcpClient)}.Close] - [{ex.StackTrace}]");
                    }
                }
            }
        }
     

        private static ConcurrentDictionary<string, ServicePoint> ServiceEndpointConfigruations = new ConcurrentDictionary<string, ServicePoint>();

        public static void ConfigureServiceEndpoint(string seURL, bool useNagle = false, bool isSSL = false, bool isChanged = false, bool debConsoleLog = false, string appenderName = null)
        {
            if (!ServiceEndpointConfigruations.ContainsKey(seURL) || isChanged)
            {

                string protocol = "http";

                if (!seURL.Contains("://"))
                {
                    seURL = $"{(isSSL ? $"{protocol}s" : protocol)}://{seURL}";
                }

                ServicePoint instanceServicePoint = ServicePointManager.FindServicePoint(new Uri(seURL));
                //http://blogs.msdn.com/b/windowsazurestorage/archive/2010/06/25/nagle-s-algorithm-is-not-friendly-towards-small-requests.aspx
                instanceServicePoint.UseNagleAlgorithm = useNagle;
                //instanceServicePoint.ReceiveBufferSize = 45000192;
                instanceServicePoint.ConnectionLimit = 100 * (Environment.ProcessorCount > 0 ? Environment.ProcessorCount : 1);
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
                if (debConsoleLog)
                {
                   System.Console.WriteLine($@"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}|Log4ALA|[{appenderName}]|TRACE|[{nameof(AlaTcpClient)}.ConfigureServiceEndpoint] instanceServicePoint.ConnectionLimit - [{instanceServicePoint.ConnectionLimit}]");
                }
#endif
                //set to 0 to force ServicePoint connections to close after servicing a request
                instanceServicePoint.ConnectionLeaseTimeout = 0;
                ServiceEndpointConfigruations.AddOrUpdate(seURL, instanceServicePoint, (key, oldValue) => instanceServicePoint);
            }
        }

    }
}
