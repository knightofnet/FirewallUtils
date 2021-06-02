using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PocFwIpApp.constant;

namespace PocFwIpApp.dto
{
    class SenderMsgBg
    {
        public IPAddress IPAddress { get; private set; }
        public SenderMsgType  MsgType { get; set; }

        public int Port { get; set; }

        public ProtocoleEnum Protocole { get; set; }

        public String Message { get; set; }

        public SenderMsgBg(SenderMsgType msgType, IPAddress ipAddress, int port, ProtocoleEnum protocole, string message=null)
        {
            IPAddress = ipAddress;
            MsgType = msgType;
            Port = port;
            Protocole = protocole;
            Message = message;
        }
    }
}
