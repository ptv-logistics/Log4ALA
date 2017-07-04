using System;
using System.IO;
using System.IO.Compression;
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
        public AlaTcpClient(string sharedKey, string workSpaceId)
        {
            sharedKeyBytes = Convert.FromBase64String(sharedKey);
            workSpaceID = workSpaceId;
            serverAddr = $"{workSpaceID}.{AlaApiUrl}";
        }

        private int tcpPort = 443;
        private TcpClient client = null;
        private Stream stream = null;
        private SslStream sslStream = null;
        private String serverAddr;

        private Stream ActiveStream
        {
            get
            {
                return sslStream;
            }
        }

        public void Connect()
        {
            client = new TcpClient(serverAddr, tcpPort);
            client.NoDelay = true;
            //client.SendTimeout = 500;
            //client.ReceiveTimeout = 1000;

            stream = client.GetStream();

            sslStream = new SslStream(stream);
            sslStream.AuthenticateAsClient(serverAddr);

        }

        public string Write(byte[] buffer, int offset, int count, bool isHeader = false)
        {
            ActiveStream.Write(buffer, offset, count);

            if (isHeader)
            {
                return "isHeader";
            }

            Flush();

            string result = string.Empty;

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
            ActiveStream.Flush();
        }

        public void Close()
        {
            if (client != null)
            {
                try
                {
                    client.Close();
                }
                catch
                {
                }
            }
        }
    }
}
