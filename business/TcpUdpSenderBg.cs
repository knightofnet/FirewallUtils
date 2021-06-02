using PocFwIpApp.constant;
using PocFwIpApp.dto;
using PocFwIpApp.utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PocFwIpApp.business
{
    class TcpUdpSenderBg
    {

        public ProtocoleEnum Protocole { get; private set; }
        public IPAddress IpAddress { get; private set; }

        public int PortToSend { get; private set; }

        public String TextToSend { get; private set; }

        public String DisplayInList => GetDisplayString();


        private BackgroundWorker BgLinked { get; set; }

        public TcpUdpSenderBg(IPAddress ipAddress, int port, ProtocoleEnum protocole, string textToSend)
        {
            IpAddress = ipAddress;
            PortToSend = port;
            Protocole = protocole;
            TextToSend = textToSend;
        }

        public void Send(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bg = sender as BackgroundWorker;
            if (bg == null) return;
            BgLinked = bg;
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = $"Sender:{PortToSend}:{Protocole}";

            switch (Protocole)
            {
                case ProtocoleEnum.TCP:
                    SendWithTcp();
                    break;
                case ProtocoleEnum.UDP:
                    SendWithUdp();
                    break;
            }

            EndBg(e);
        }

        private string GetDisplayString()
        {
            return String.Format("{0} : {1}", Protocole, PortToSend);
        }

        private void SendWithTcp() {

           IPEndPoint remoteEP = new IPEndPoint(IpAddress, PortToSend);

            Encoding encoMsg = Encoding.ASCII;

            TcpClient client = new TcpClient();


            try
            {
                ReportProgress(SenderMsgType.Connecting);
                client.Connect(remoteEP);
                ReportProgress(SenderMsgType.Connected);

                using (NetworkStream nwStream = client.GetStream())
                {
                    //---send the text---
                    ReportProgress(SenderMsgType.SendingMsg);
                    NetUtils.SendMsgString(nwStream, TextToSend, encoMsg, true);
                    ReportProgress(SenderMsgType.MsgSended);

                    /*
                    byte[] bytesToSend = encoMsg.GetBytes(TextToSend);
                    //---send the text---
                    ReportProgress(SenderMsgType.SendingMsg);
                    nwStream.Write(bytesToSend, 0, bytesToSend.Length);
                    ReportProgress(SenderMsgType.MsgSended);
                    */

                    //---read back the text---
                    ReportProgress(SenderMsgType.ReadingReturnMsg);
                    String messageReturn = NetUtils.ReadIncomingMsgString(nwStream, encoMsg, client.ReceiveBufferSize);
                    /*
                    byte[] bytesToRead = new byte[client.ReceiveBufferSize];
                    int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
                    */
                    ReportProgress(SenderMsgType.Received, messageReturn);

                    client.Close();
                    ReportProgress(SenderMsgType.Disconnected);

                }

            }
            catch (SocketException ex)
            {
                /*
                if (ex.NativeErrorCode == 10048)
                {
                    ReportProgress(ListenerMsgType.ErrorExpected);

                    Process p = NetUtils.GetFirstProcessUsing(PortToSend);
                    String appName = "indeterminée";
                    if (p != null)
                    {
                        appName = p.ProcessName;
                    }
                    ReportProgress(ListenerMsgType.AlreadyListened, appName);
                }
                else
                {
                    throw ex;
                }
                */
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {

                if (client.Connected)
                {
                    client.Close();
                }
 
            }




        }

        private void SendWithUdp()
        {


            IPEndPoint remoteEP = new IPEndPoint(IpAddress, PortToSend);

            Encoding encoMsg = Encoding.ASCII;

            UdpClient client = new UdpClient();

            ReportProgress(SenderMsgType.Connecting);
            client.Connect(remoteEP);
            ReportProgress(SenderMsgType.Connected);
            try
            {
                //---send the text---
                ReportProgress(SenderMsgType.SendingMsg);
                byte[] sendMessage = Encoding.UTF8.GetBytes(TextToSend);
                client.Send(sendMessage, sendMessage.Length);
                ReportProgress(SenderMsgType.MsgSended);

                //---read back the text---
                ReportProgress(SenderMsgType.ReadingReturnMsg);
                byte[] bytesToRead = client.Receive(ref remoteEP);
                ReportProgress(SenderMsgType.Received, encoMsg.GetString(bytesToRead, 0, bytesToRead.Length));


               
                
                /*
                using (NetworkStream nwStream = client.GetStream())
                {
                    byte[] bytesToSend = encoMsg.GetBytes(TextToSend);

                    //---send the text---
                    ReportProgress(SenderMsgType.SendingMsg);
                    nwStream.Write(bytesToSend, 0, bytesToSend.Length);
                    ReportProgress(SenderMsgType.MsgSended);


                    //---read back the text---
                    ReportProgress(SenderMsgType.ReadingReturnMsg);
                    byte[] bytesToRead = new byte[client.ReceiveBufferSize];
                    int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
                    ReportProgress(SenderMsgType.Received, encoMsg.GetString(bytesToRead, 0, bytesRead));

                    client.Close();
                    ReportProgress(SenderMsgType.Disconnected);

                }
                */

            }
            catch (SocketException ex)
            {
                /*
                if (ex.NativeErrorCode == 10048)
                {
                    ReportProgress(ListenerMsgType.ErrorExpected);

                    Process p = NetUtils.GetFirstProcessUsing(PortToSend);
                    String appName = "indeterminée";
                    if (p != null)
                    {
                        appName = p.ProcessName;
                    }
                    ReportProgress(ListenerMsgType.AlreadyListened, appName);
                }
                else
                {
                    throw ex;
                }
                */
            }
            catch (Exception ex)
            {

            }
            finally
            {

                if (client != null)
                {
                    client.Close();
                    ReportProgress(SenderMsgType.Disconnected);
                }

            }




        }

        private void ReportProgress(SenderMsgType typeMsg, String msg = null)
        {

            BgLinked.ReportProgress(0, new SenderMsgBg(typeMsg, IpAddress, PortToSend, Protocole, msg));

        }

        private void EndBg(DoWorkEventArgs e)
        {
            //e.Result = new SenderMsgBg(, PortToSend, Protocole);
        }

        public void StopBg()
        {
            if (BgLinked != null)
            {
                BgLinked.CancelAsync();
            }
        }


    }
}
