using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using AryxDevLibrary.extensions;
using AryxDevLibrary.utils.logger;
using PocFwIpApp.business;
using PocFwIpApp.constant;
using PocFwIpApp.dto;
using PocFwIpApp.utils;
using PocFwIpApp.view.viewinterface;

namespace PocFwIpApp.view.page
{
    /// <summary>
    /// Logique d'interaction pour ListenerPage.xaml
    /// </summary>
    public partial class ListenerPage : Page, ListenerPage.IListenerPage
    {
        public interface IListenerPage : AppPages.IChildView
        {
        }

        private readonly ObservableCollection<TcpUdpListenerBg> _listeners = new ObservableCollection<TcpUdpListenerBg>();

        private TcpUdpSenderBg tcpListenerBg = null;

        public IMainWindow Superior { get; set; }
        public bool IsBusy => IsPageBusy();


        public Page Page { get; }

        public ListenerPage(IMainWindow mainWindow)
        {
            Page = this;
            Superior = mainWindow;
            InitializeComponent();

            listListeners.ItemsSource = _listeners;
            listListeners.DisplayMemberPath = "DisplayInList";

            AppPages.ClosePage += (sender, args) =>
            {
                if (!_listeners.Any()) return;

                foreach (TcpUdpListenerBg tcpListenerBg in _listeners)
                {
                    tcpListenerBg.StopBg();
                }
            };

            chkRespondTo.Click += (sender, args) =>
            {
                bool currValue = chkRespondTo.IsChecked ?? false;
                tbResponseMsg.IsEnabled = currValue;
            };

            listListeners.MouseDoubleClick += (sender, args) =>
            {
                TcpUdpListenerBg listener = listListeners.SelectedItem as TcpUdpListenerBg;
                if (listener == null) return;

                if (listener.SourceCreated == EnumBgSource.This)
                {
                    listener.StopBg();
                }
                else if (listener.SourceCreated == EnumBgSource.OtherApp)
                {
                    String appName = NetUtils.GetAppListeningOnTcpPort(listener.PortListened);
                    MultipleLog($"{listener.Protocole}-{listener.PortListened} : impossible de terminer l'écoute démarrée par une autre application ('{appName}')", Logger.LogLvl.ERROR);
                }
            };


            Loaded += (sender, args) =>
            {
                GetActiveTcpPorts();
            };
        }



        private bool IsPageBusy()
        {
            return _listeners.Any();
        }

        private void btnAddListener_Click(object sender, RoutedEventArgs e)
        {
            String textPort = tAddPort.Text;
            if (String.IsNullOrWhiteSpace(textPort))
            {
                MultipleLog("Aucun port", Logger.LogLvl.WARN);
                return;
            }

            String[] multiplePortA = textPort.Split(';');
            List<int> portsToListen = new List<int>(multiplePortA.Length);
            foreach (string s in multiplePortA)
            {
                if (s.Contains(":"))
                {
                    String[] multiplePortB = s.Split(':');
                    if (multiplePortB.Length > 2)
                    {
                        MultipleLog("Etendu de ports invalide", Logger.LogLvl.ERROR);
                        return;
                    }

                    int bMin = IntParse(multiplePortB[0]);
                    int bMax = IntParse(multiplePortB[1]);
                    if (bMin == -1 || bMax == -1 || bMin > bMax)
                    {
                        MultipleLog("Etendu de ports invalide", Logger.LogLvl.ERROR);
                        return;
                    }

                    for (int i = bMin; i <= bMax; i++)
                    {
                        portsToListen.Add(i);
                    }
                }
                else
                {
                    int portInt = IntParse(s);
                    if (portInt > 0)
                    {
                        portsToListen.Add(portInt);
                    }
                    else
                    {
                        MultipleLog("Port invalide détecté", Logger.LogLvl.ERROR);
                        return;
                    }


                }
            }



            ProtocoleEnum protocole = radioListReceiveTcp.IsChecked ?? false ? ProtocoleEnum.TCP : ProtocoleEnum.UDP;

            String textToRespond = chkRespondTo.IsChecked ?? false ? tbResponseMsg.Text : null;


            foreach (int port in portsToListen)
            {
                BackgroundWorker b = CreateBgWorker(port, protocole, textToRespond);
                if (b != null)
                    b.RunWorkerAsync();
            }

            tAddPort.Text = null;
            tbResponseMsg.Text = null;
            chkRespondTo.IsChecked = true;
            tbResponseMsg.IsEnabled = false;


        }

        private BackgroundWorker CreateBgWorker(int port, ProtocoleEnum protocole, String textToRespond = null)
        {
            if (_listeners.Any(r => r.PortListened == port && r.Protocole == protocole))
            {
                String appName = appName = NetUtils.GetAppListeningOnTcpPort(port);
                MultipleLog($"{protocole}-{port} : une application ('{appName}') écoute déjà sur ce port", Logger.LogLvl.ERROR);
                return null;
            }

            TcpUdpListenerBg tcpListenerBg = new TcpUdpListenerBg(port, protocole, textToRespond);


            BackgroundWorker bg = new BackgroundWorker();
            bg.WorkerSupportsCancellation = true;
            bg.WorkerReportsProgress = true;
            bg.DoWork += tcpListenerBg.Listen;
            bg.ProgressChanged += BgListenOnProgressChanged;
            bg.RunWorkerCompleted += BgListenerOnRunCompleted;

            //bg.RunWorkerAsync();
            _listeners.Add(tcpListenerBg);

            return bg;
        }


        private void btnSendMsg_Click(object sender, RoutedEventArgs e)
        {

            IPAddress ipAdd = IPAddress.Parse(tbSendMessageTarget.Text);
            String textSend = tbSendMessage.Text;

            Encoding encoMsg = Encoding.ASCII;

            String textPort = tbSendMessageTargetPort.Text;
            if (String.IsNullOrWhiteSpace(textPort))
            {
                MultipleLog("Aucun port", Logger.LogLvl.WARN);
                return;
            }

            int port = IntParse(textPort);
            if (port < 0)
            {
                MultipleLog("Port invalide détecté", Logger.LogLvl.ERROR);
                return;
            }


            ProtocoleEnum protocole = radioListSendTcp.IsChecked ?? false ? ProtocoleEnum.TCP : ProtocoleEnum.UDP;



            if (tcpListenerBg != null)
            {
                return;
            }
            tcpListenerBg = new TcpUdpSenderBg(ipAdd, port, protocole, textSend);


            BackgroundWorker bg = new BackgroundWorker();
            bg.WorkerSupportsCancellation = true;
            bg.WorkerReportsProgress = true;
            bg.DoWork += tcpListenerBg.Send;
            bg.ProgressChanged += BgSendOnProgressChanged;
            bg.RunWorkerCompleted += BgSendOnRunCompleted;

            //bg.RunWorkerAsync();
            ////_listeners.Add(tcpListenerBg);
            bg.RunWorkerAsync();

            btnSendMsg.IsEnabled = false;

            /*
            tAddPort.Text = null;
            tbResponseMsg.Text = null;
            chkRespondTo.IsChecked = true;
            tbResponseMsg.IsEnabled = false;
            */

        }




        private void tAddPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(tAddPort.Text))
            {
                e.Handled = true;
                return;

            }

            String text = tAddPort.Text;

            int carretPos = tAddPort.CaretIndex;
            StringBuilder newString = new StringBuilder();
            bool isReplace = false;

            foreach (char c in text)
            {
                string cStr = c.ToString();
                if (cStr.Matches("^[\\d;:]$"))
                {
                    newString.Append(cStr);
                }
                else
                {
                    isReplace = true;
                }

            }

            if (isReplace)
            {
                tAddPort.Text = newString.ToString();
            }
        }




        private void BgListenOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ListenerMsgFromBg lMsg = e.UserState as ListenerMsgFromBg;
            if (lMsg == null) return;

            TcpUdpListenerBg lBg =
                _listeners.FirstOrDefault(r => r.PortListened == lMsg.Port && r.Protocole == lMsg.Protocole);
            if (lBg == null) return;

            String prefixe = $"{lMsg.Protocole}-{lMsg.Port} :";

            switch (lMsg.MsgType)
            {
                case ListenerMsgType.StartListening:
                    MultipleLog($"{prefixe} écoute démarrée");
                    break;

                case ListenerMsgType.MessagePending:
                    MultipleLog($"{prefixe} message en attente");
                    break;

                case ListenerMsgType.MessageReceived:
                    String msg = lMsg.Message;
                    msg = String.IsNullOrWhiteSpace(msg) ? "Message null ou vide reçu" : msg;
                    MultipleLog($"{prefixe} message reçu \"{msg}\" de {lMsg.RemoteIpAddress}");
                    break;

                case ListenerMsgType.StopListening:
                    MultipleLog($"{prefixe} fin de l'écoute");
                    break;

                case ListenerMsgType.ErrorExpected:
                    MultipleLog($"{prefixe} erreur -->");
                    break;

                case ListenerMsgType.AlreadyListened:
                    MultipleLog($"{prefixe} une application ('{lMsg.Message}') écoute déjà sur ce port", Logger.LogLvl.ERROR);
                    break;
            }

        }

        private void BgSendOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            SenderMsgBg lMsg = e.UserState as SenderMsgBg;
            if (lMsg == null) return;


            if (tcpListenerBg == null) return;

            String prefixe = $"{lMsg.Protocole}-{lMsg.IPAddress}:{lMsg.Port} :";

            switch (lMsg.MsgType)
            {
                case SenderMsgType.Connecting:
                    MultipleLog($"{prefixe} connexion en cours");
                    break;

                case SenderMsgType.Connected:
                    MultipleLog($"{prefixe} : connecté");
                    break;

                case SenderMsgType.SendingMsg:
                    MultipleLog($"{prefixe} : envoie du message");
                    break;

                case SenderMsgType.MsgSended:
                    MultipleLog($"{prefixe} : message envoyé");
                    break;

                case SenderMsgType.Received:
                    MultipleLog($"{prefixe} message retour : {lMsg.Message}");
                    break;

                case SenderMsgType.Disconnected:
                    MultipleLog($"{prefixe} déconnecté");
                    break;

            }

        }

        private void BgListenerOnRunCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ListenerMsgFromBg lMsg = e.Result as ListenerMsgFromBg;
            if (lMsg == null) return;

            TcpUdpListenerBg lBg =
                _listeners.FirstOrDefault(r => r.PortListened == lMsg.Port && r.Protocole == lMsg.Protocole);
            if (lBg == null) return;

            Dispatcher.BeginInvoke(DispatcherPriority.Send,
                (Action)(() =>
                {
                    _listeners.Remove(lBg);
                })
            );

            MultipleLog($"{lMsg.Protocole} : écoute terminée sur le port {lMsg.Port}");

        }


        private void BgSendOnRunCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            tcpListenerBg = null;
            btnSendMsg.IsEnabled = true;

            if (e.Error != null)
            {
                MultipleLog(e.Error.Message, Logger.LogLvl.ERROR);
            }

        }




        private void MultipleLog(String message, Logger.LogLvl lvl = Logger.LogLvl.INFO)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (Action)(() =>
                {
                    AddLineLog(message, lvl);
                    Superior.ShowStatusBarMessage(message, "PortListener");
                })
            );

        }

        private void AddLineLog(String message, Logger.LogLvl lvl)
        {
            Run inline = new Run(message);
            if (lvl == Logger.LogLvl.ERROR)
            {
                inline.Foreground = new SolidColorBrush(Colors.DarkRed);
            }
            else if (lvl == Logger.LogLvl.WARN)
            {
                inline.Foreground = new SolidColorBrush(Colors.Orange);
            }
            Paragraph para = new Paragraph(inline);
            para.Margin = new Thickness(0, 0, 0, 0);
            rJournal.Document.Blocks.Add(para);

            rJournal.ScrollToEnd();

        }
        private void GetActiveTcpPorts()
        {

            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] endPoints = properties.GetActiveTcpListeners();

            List<TcpUdpListenerBg> tmpList = new List<TcpUdpListenerBg>(endPoints.Length);

            foreach (IPEndPoint e in endPoints)
            {
                if (!_listeners.Any(r => r.PortListened == e.Port && r.Protocole == ProtocoleEnum.TCP))
                {
                    TcpUdpListenerBg listener = new TcpUdpListenerBg(e.Port, ProtocoleEnum.TCP, null)
                    {
                        SourceCreated = EnumBgSource.OtherApp
                    };
                    tmpList.Add(listener);
                }
            }

            foreach (TcpUdpListenerBg bg in tmpList.OrderBy(r => r.PortListened).Distinct())
            {
                _listeners.Add(bg);
            }

        }

        private static int IntParse(String s, int dft = -1)
        {
            int oInt;
            return Int32.TryParse(s, out oInt) ? oInt : dft;

        }

    }
}
