using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PocFwIpApp.constant;
using PocFwIpApp.dto;
using PocFwIpApp.utils;

namespace PocFwIpApp.business
{
    class TcpUdpListenerBg : IEquatable<TcpUdpListenerBg>
    {

        public EnumBgSource SourceCreated { get; set; }
        public ProtocoleEnum Protocole { get; private set; }
        public int PortListened { get; private set; }

        public String TextReponse { get; private set; }

        public String DisplayInList => GetDisplayString();

        private List<Thread> listThreadLaunched = new List<Thread>();

        private BackgroundWorker BgLinked { get; set; }

        public TcpUdpListenerBg(int port, ProtocoleEnum protocole, string textToRespond)
        {
            PortListened = port;
            Protocole = protocole;
            TextReponse = textToRespond;
            SourceCreated = EnumBgSource.This;
        }

        public void Listen(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bg = sender as BackgroundWorker;
            if (bg == null) return;
            BgLinked = bg;
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = $"Listen:{PortListened}:{Protocole}";

            switch (Protocole)
            {
                case ProtocoleEnum.TCP:
                    ListenTcp();
                    break;
                case ProtocoleEnum.UDP:
                    ListenUdp();
                    break;
            }

            EndBg(e);
        }

        private string GetDisplayString()
        {
            return $"{Protocole} : {PortListened}";
        }

        private void ListenTcp()
        {
            TcpListener listener = null;
            Encoding encoMsg = Encoding.ASCII;

            try
            {
                listener = new TcpListener(IPAddress.Any, PortListened);

                listener.Start();
                ReportProgress(ListenerMsgType.StartListening);



                while (!BgLinked.CancellationPending) // <--- boolean flag to exit loop
                {
                    if (listener.Pending())
                    {
                        ReportProgress(ListenerMsgType.MessagePending);

                        Thread tmpThread = new Thread(new ThreadStart(() =>
                        {
                            string msg = null;

                            TcpClient client = null;
                            try
                            {
                                client = listener.AcceptTcpClient();
                                // TODO RemoteAdresse
                                using (NetworkStream ns = client.GetStream())
                                {

                                    msg = NetUtils.ReadIncomingMsgString(ns, Encoding.ASCII, client.ReceiveBufferSize);
                                    ReportProgress(ListenerMsgType.MessageReceived, msg, client.Client.RemoteEndPoint);
                                    if (TextReponse != null)
                                    {
                                        NetUtils.SendMsgString(ns, TextReponse, Encoding.ASCII, true);
                                    }
                                    else
                                    {
                                        byte[] buffer = new byte[] { 1 };
                                        ns.Write(buffer, 0, buffer.Length);
                                    }

                                    client.Close();
                                }
                            }
                            catch (Exception te)
                            {
                                if (client != null)
                                {
                                    client.Close();
                                }
                            }




                        }));

                        tmpThread.Start();
                        listThreadLaunched.Add(tmpThread);


                    }
                    else
                    {
                        Thread.Sleep(100); //<--- timeout
                    }
                }

            }
            catch (SocketException ex)
            {
                if (ex.NativeErrorCode == 10048)
                {
                    ReportProgress(ListenerMsgType.ErrorExpected);

                    var appName = NetUtils.GetAppListeningOnTcpPort(PortListened);
                    ReportProgress(ListenerMsgType.AlreadyListened, appName);
                }
                else
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (listener != null)
                {
                    listener.Stop();
                    foreach (Thread t in listThreadLaunched.Where(r => r != null && r.IsAlive))
                    {
                        t.Abort();
                    }
                }
            }



        }



        private void ListenUdp()
        {
            UdpClient udpClient = null;
            try
            {
                udpClient = new UdpClient(PortListened);
                ReportProgress(ListenerMsgType.StartListening);

                var remoteEP = new IPEndPoint(IPAddress.Any, PortListened);

                while (!BgLinked.CancellationPending) // <--- boolean flag to exit loop
                {
                    if (udpClient.Available > 0)
                    {
                        ReportProgress(ListenerMsgType.MessagePending);

                        Thread tmpThread = new Thread(new ThreadStart(() =>
                        {

                            byte[] data = udpClient.Receive(ref remoteEP);
                            string msg = Encoding.UTF8.GetString(data);

                            if (TextReponse == null)
                            {
                                udpClient.Send(new byte[] { 1 }, 1, remoteEP);
                            }
                            else
                            {
                                byte[] sendMessage = Encoding.UTF8.GetBytes(TextReponse);
                                udpClient.Send(sendMessage, sendMessage.Length, remoteEP);
                            }

                            ReportProgress(ListenerMsgType.MessageReceived, msg);
                        }));

                        tmpThread.Start();
                        listThreadLaunched.Add(tmpThread);
                    }
                    else
                    {
                        Thread.Sleep(100); //<--- timeout
                    }
                }

            }
            catch (SocketException ex)
            {
                if (ex.NativeErrorCode == 10048)
                {
                    ReportProgress(ListenerMsgType.ErrorExpected);
                    ReportProgress(ListenerMsgType.AlreadyListened);
                }
                else
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (udpClient != null)
                {
                    udpClient.Close(); ;
                    foreach (Thread t in listThreadLaunched.Where(r => r != null && r.IsAlive))
                    {
                        t.Abort();
                    }
                }
            }
        }

        private void ReportProgress(ListenerMsgType typeMsg, String msg = null, EndPoint remoteAddress= null)
        {

            BgLinked.ReportProgress(0, new ListenerMsgFromBg(typeMsg, PortListened, Protocole, msg, remoteAddress));

        }

        private void EndBg(DoWorkEventArgs e)
        {
            e.Result = new ListenerMsgFromBg(ListenerMsgType.StopListening, PortListened, Protocole);
        }

        public void StopBg()
        {
            if (BgLinked != null)
            {
                BgLinked.CancelAsync();
            }
        }


        public bool Equals(TcpUdpListenerBg other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Protocole == other.Protocole && PortListened == other.PortListened;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TcpUdpListenerBg)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Protocole * 397) ^ PortListened;
            }
        }

        public static bool operator ==(TcpUdpListenerBg left, TcpUdpListenerBg right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TcpUdpListenerBg left, TcpUdpListenerBg right)
        {
            return !Equals(left, right);
        }
    }
}
