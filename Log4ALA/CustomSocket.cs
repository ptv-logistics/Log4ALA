using System;
using System.Net.Sockets;


namespace CustomLibraries.Threading
{
    public class CustomSocket:TcpClient
    {
        private DateTime _TimeCreated;

        public DateTime TimeCreated
        {
            get { return _TimeCreated; }
            set { _TimeCreated = value; }
        }

        public CustomSocket(string host,int port)
            : base(host,port)
        {
            _TimeCreated = DateTime.Now;
        }
    }
}
