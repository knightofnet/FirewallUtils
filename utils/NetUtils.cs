using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PocFwIpApp.utils
{
    public static class NetUtils
    {
        public static string SanitizeReadIpAdress(string address)
        {
            address = address.Trim();

            int indexOfSlash = address.IndexOf('/');
            if (indexOfSlash >= 0)
            {
                String[] splitted = address.Split('/');
                if (splitted.Length >= 2)
                {
                    address = splitted[0];
                }
            }

            int indexOfTraitUnion = address.IndexOf('-');
            if (indexOfTraitUnion >= 0)
            {
                String[] splitted = address.Split('-');
                if (splitted.Length >= 2 && splitted[0] == splitted[1])
                {
                    address = splitted[0];
                }
            }

            return address;
        }
        
        public static IEnumerable<uint> ProcessesUsingPorts(uint tcpPort)
        {
            PowerShell ps = PowerShell.Create();
            ps.AddCommand("Get-NetTCPConnection").AddParameter("LocalPort", tcpPort);
            return ps.Invoke().Select(p => (uint)p.Properties["OwningProcess"].Value);
        }

        public static Process GetFirstProcessUsing(int tcpPort)
        {
            var ports = ProcessesUsingPorts((uint) tcpPort);
            var enumerable = ports.ToList();

            if (enumerable.Any())
            {
                return Process.GetProcessById((int)enumerable.ElementAtOrDefault(0) );
            }

            return null;
        }

        public static void WriteNetworkStreamString(NetworkStream networkStream, string dataToClient)
        {
            Byte[] sendBytes = null;
            try
            {
                sendBytes = Encoding.ASCII.GetBytes(dataToClient);
                networkStream.Write(sendBytes, 0, sendBytes.Length);
                networkStream.Flush();
            }
            catch (SocketException e)
            {
                throw;
            }
        }

        public static void SendMsgString(NetworkStream networkStream, string message, Encoding encoding, bool isFirstByteMsgLenght=false)
        {
            byte[] bytesToSend = encoding.GetBytes(message);
            if (isFirstByteMsgLenght) {

                byte[] bytesMsgLen = new byte[0];
                bytesMsgLen = BitConverter.GetBytes(bytesToSend.Length);


                byte[] tmpBytes = new byte[bytesMsgLen.Length + bytesToSend.Length];
                Array.Copy(bytesMsgLen, tmpBytes, bytesMsgLen.Length);
                Array.Copy(bytesToSend, 0, tmpBytes, bytesMsgLen.Length, bytesToSend.Length);

                bytesToSend = tmpBytes;
            }



            //---send the text---
            networkStream.Write(bytesToSend, 0, bytesToSend.Length);
            
        }

        internal static string ReadIncomingMsgString(NetworkStream networkStream, Encoding encoding, int receiveBufferSize)
        {
            byte[] probableMsgLenght = new byte[4];
            int bytesRead = networkStream.Read(probableMsgLenght, 0, probableMsgLenght.Length);
            
            int msgLenght = -1;
            try
            {
                msgLenght = BitConverter.ToInt32(probableMsgLenght, 0);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // LOG
                return encoding.GetString(probableMsgLenght);
            }

            int readSoFar = 0;
            byte[] msg = new byte[msgLenght];

            while (readSoFar < msgLenght)
            {
                var read = networkStream.Read(msg, readSoFar, Math.Min(msg.Length - readSoFar, receiveBufferSize));
                readSoFar += read;
                if (read == 0)
                    break;   // connection was broken
            }

            return encoding.GetString(msg);
            /*
            byte[] bytesToRead = new byte[client.ReceiveBufferSize];
            int bytesRead = networkStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
            */
        }


        public static string GetAppListeningOnTcpPort(int port)
        {
            Process p = NetUtils.GetFirstProcessUsing(port);
            String appName = "indeterminée";
            if (p != null)
            {
                appName = p.ProcessName;
            }

            return appName;
        }
    }
}
