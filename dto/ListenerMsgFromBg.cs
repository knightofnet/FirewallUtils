using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PocFwIpApp.constant;

namespace PocFwIpApp.dto
{
    class ListenerMsgFromBg
    {

        public EndPoint RemoteIpAddress { get; private set; }

        public ListenerMsgType  MsgType { get; set; }

        public int Port { get; set; }

        public ProtocoleEnum Protocole { get; set; }

        public String Message { get; set; }

        public ListenerMsgFromBg(ListenerMsgType msgType, int port, ProtocoleEnum protocole, string message=null, EndPoint remoteIp =null)
        {
            MsgType = msgType;
            Port = port;
            Protocole = protocole;
            Message = message;
            RemoteIpAddress = remoteIp;

        }
    }
}
