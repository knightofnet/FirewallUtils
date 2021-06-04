using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using AryxDevLibrary.utils;
using AryxDevLibrary.utils.logger;
using NetFwTypeLib;
using PocFwIpApp.business;
using PocFwIpApp.constant;
using PocFwIpApp.dto;
using PocFwIpApp.utils;
using PocFwIpApp.view.viewinterface;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace PocFwIpApp.view.page
{
    /// <summary>
    /// Logique d'interaction pour CollectIpPage.xaml
    /// </summary>
    public partial class CollectIpPage : Page, CollectIpPage.ICollectIpPage
    {

        public interface ICollectIpPage : AppPages.IChildView
        {
            void SetTbExeTexts(string tboxExeText, string tboxExeTooltip, bool isReadonly = true);
            void StopCollectFor(FileInfo filePath);
            void Refresh();
        }

        public IMainWindow Superior { get; set; }
        public bool IsBusy => IsPageBusy();
        public Page Page { get; }


        private static Logger log = Logger.LastLoggerInstance;

        private BackgroundWorker collectBackgroundWorker;

        private EventsCollector eventsCollector;

        private DispatcherTimer autoCreateFwRuleTimer;

        public bool IsUpdateFwRules { get; set; }


        public FileInfo SessionFileInfo { get; set; }


        public CollectIpPage(IMainWindow mainWindow)
        {
            Page = this;
            Superior = mainWindow;

            InitializeComponent();
            RunTrt();
        }

        private void RunTrt()
        {

            AppPages.ClosePage += (sender, args) =>
            {
                StopCollecteBackgrounder();

            };



            cAutoCreate.Click += (sender, args) =>
            {
                bool cValue = cAutoCreate.IsChecked ?? false;


                if (!cValue && (autoCreateFwRuleTimer != null && autoCreateFwRuleTimer.IsEnabled))
                {
                    StopAutoCreateFwTimer();
                    btnCollect_Click(null, null);
                }
                else
                {
                    StartAutoCreateFwTimer();
                    btnCollect_Click(null, null);
                }
            };


            IsUpdateFwRules = true;

            gpCollect.IsEnabled = false;
            gpRuleFw.IsEnabled = false;

            linkIn.Click += (sender, args) => LinkClick(DirectionsEnum.Inboud, ProtocoleEnum.ALL);
            linkOut.Click += (sender, args) => LinkClick(DirectionsEnum.Outbound, ProtocoleEnum.ALL);


        }




        private void StartAutoCreateFwTimer()
        {
            if (autoCreateFwRuleTimer != null && autoCreateFwRuleTimer.IsEnabled)
            {
                autoCreateFwRuleTimer.Stop();
            }

            autoCreateFwRuleTimer = new DispatcherTimer();
            autoCreateFwRuleTimer.Interval = new TimeSpan(0, 0, 5);
            autoCreateFwRuleTimer.Tick += (o, a) => CreateAndUpdateRule();
            autoCreateFwRuleTimer.Start();

            Superior.ToggleRectForTimer(true);

            btnCreate.IsEnabled = false;
            btnCollect.Focus();
        }

        private void StopAutoCreateFwTimer()
        {
            if (autoCreateFwRuleTimer != null)
            {
                autoCreateFwRuleTimer.Stop();
            }

            Superior.ToggleRectForTimer(false);
            cAutoCreate.IsChecked = false;

            btnCreate.IsEnabled = true;
        }

        private bool IsPageBusy()
        {
            return collectBackgroundWorker != null && collectBackgroundWorker.IsBusy;

        }

        public void SetTbExeTexts(string tboxExeText, string tboxExeTooltip = null, bool isReadonly = true)
        {
            tExeName.Text = tboxExeText;
            if (tboxExeTooltip != null)
                tExeName.ToolTip = tboxExeTooltip;

            tExeName.IsReadOnly = isReadonly;
            if (isReadonly && !String.IsNullOrWhiteSpace(tboxExeText))
            {
                OpenExeForMonitor(tboxExeText);
            }

        }



        private void btnCollect_Click(object sender, RoutedEventArgs e)
        {
            if (collectBackgroundWorker != null && collectBackgroundWorker.IsBusy)
            {
                StopCollecteBackgrounder();
                cAutoCreate.IsChecked = false;

            }
            else
            {

                if (eventsCollector == null || !eventsCollector.AppName.Equals(tExeName.Text))
                {

                    eventsCollector = new EventsCollector
                    {
                        AppName = SessionFileInfo.Name,
                        ReportFilter = entry => entry.GetRemoteAddress()
                    };
                }

                collectBackgroundWorker = new BackgroundWorker();
                collectBackgroundWorker.WorkerSupportsCancellation = true;
                collectBackgroundWorker.WorkerReportsProgress = true;
                collectBackgroundWorker.DoWork += eventsCollector.DoLoopCollect;
                collectBackgroundWorker.RunWorkerCompleted += CollectBackgroundWorkerOnRunWorkerCompleted;
                collectBackgroundWorker.ProgressChanged += ReportProgress;

                collectBackgroundWorker.RunWorkerAsync();
                btnCollect.Content = "Stop";

                Superior.ToggleRectForCollectIp(true);

            }
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (eventsCollector != null && eventsCollector.Entries.Any())
            {
                CreateAndUpdateRule();
            }

        }

        private void CreateAndUpdateRule()
        {
            try
            {

                if (eventsCollector == null)
                {
                    return;
                }

                DirectionsEnum direction = DirectionsEnum.Outbound;

                CreateOrUpdateRule(direction, ProtocoleEnum.TCP);
                CreateOrUpdateRule(direction, ProtocoleEnum.UDP);
            }
            catch (Exception e)
            {
                ExceptionHandlingUtils.LogAndRethrows(e, "CreateAndUpdateRule");
            }
        }

        private void ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            EventLogEntry entry = e.UserState as EventLogEntry;
            if (entry == null) return;

            List<String> lstTcp = new List<string>();
            List<String> lstUdp = new List<string>();

            if (entry.GetProtocole() == ProtocoleEnum.TCP)
            {
                lstTcp.Add(eventsCollector.ReportFilter(entry));
            }
            else if (entry.GetProtocole() == ProtocoleEnum.UDP)
            {
                lstUdp.Add(eventsCollector.ReportFilter(entry));
            }

            RefreshListBoxes(lstTcp, lstUdp);
        }

        private void CollectBackgroundWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DirectionsEnum direction = DirectionsEnum.Outbound;

            List<String> lstTcp = eventsCollector.Entries.Where(r => direction == r.GetDirection() && ProtocoleEnum.TCP == r.GetProtocole()).Select(eventsCollector.ReportFilter).Distinct().ToList();
            List<String> lstUdp = eventsCollector.Entries.Where(r => direction == r.GetDirection() && ProtocoleEnum.UDP == r.GetProtocole()).Select(eventsCollector.ReportFilter).Distinct().ToList();

            if (lboxTcpOut.Items.Count > 0)
            {
                lboxTcpOut.Items.Clear();
            }

            RefreshListBoxes(lstTcp, lstUdp, true);

            Superior.ToggleRectForCollectIp(false); 



        }

        private void RefreshListBoxes(List<string> lstTcp, List<string> lstUdp, bool clearOnAdd = false)
        {
            // log.Debug("RefreshListBoxes(listTcp:{0}, listUdp:{1}, cleanOnAdd:{2}", MiscAppUtils.ListStrToStr(lstTcp), MiscAppUtils.ListStrToStr(lstUdp), clearOnAdd);
            if (clearOnAdd)
            {
                lboxTcpOut.Items.Clear();
                lboxUdpOut.Items.Clear();
            }

            foreach (string address in lstTcp)
            {
                lboxTcpOut.Items.Add(address);

                Superior.ShowStatusBarMessage(address, "CollecteIp - nouvelle IP-TCP");

            }
            lboxTcpOut.UpdateLayout();
            lboxTcpOut.Items.Refresh();
            lboxTcpOut.SelectedIndex = lboxTcpOut.Items.Count - 1;
            lboxTcpOut.ScrollIntoView(lboxTcpOut.SelectedItem);


            foreach (string address in lstUdp)
            {
                lboxUdpOut.Items.Add(address);

                Superior.ShowStatusBarMessage(address, "CollecteIp - nouvelle IP-UDP");
            }
            lboxUdpOut.UpdateLayout();
            lboxUdpOut.Items.Refresh();
            lboxUdpOut.SelectedIndex = lboxUdpOut.Items.Count - 1;
            lboxUdpOut.ScrollIntoView(lboxUdpOut.SelectedItem);
        }




        private void CreateOrUpdateRule(DirectionsEnum direction, ProtocoleEnum protocole)
        {
            HashSet<String> tmplistIpNewCollected = new HashSet<string>(eventsCollector.Entries
                .Where(r => direction == r.GetDirection() && protocole == r.GetProtocole())
                .Select(eventsCollector.ReportFilter));

            HashSet<IPAddress> listIpNewCollected = new HashSet<IPAddress>();

            foreach (string ip in tmplistIpNewCollected)
            {
                IPAddress ipAdd;
                if (IPAddress.TryParse(ip, out ipAdd))
                {
                    listIpNewCollected.Add(ipAdd);
                }

            }

            if (listIpNewCollected.Any())
            {
                String ruleName = String.Format("IP - {0}", eventsCollector.AppName);

                if (FwUtils.IsExistsRule(ruleName, direction, protocole))
                {
                    HashSet<IPAddress> listIpAlreadyExists = new HashSet<IPAddress>();
                    bool isNeedUpdate = true;

                    INetFwRule rule = FwUtils.GetRule(ruleName, direction, protocole);
                    if (!String.IsNullOrWhiteSpace(rule.RemoteAddresses))
                    {
                        foreach (string address in rule.RemoteAddresses.Split(','))
                        {
                            String addressT = NetUtils.SanitizeReadIpAdress(address);
                            IPAddress ipAdd;
                            if (IPAddress.TryParse(addressT, out ipAdd))
                            {
                                listIpNewCollected.Add(ipAdd);
                                listIpAlreadyExists.Add(ipAdd);
                            }
                            else
                            {
                                log.Debug("Control KO {0}", addressT);
                                continue;
                            }
                        }

                        //log.Debug("a:{0}", ListStrToStr(listIpAlreadyExists.Select(r=>r.ToString()).ToList()));
                        //log.Debug("n:{0}", ListStrToStr(listIpNewCollected.Select(r => r.ToString()).ToList()));

                        isNeedUpdate = !listIpAlreadyExists.SetEquals(listIpNewCollected);

                    }

                    if (isNeedUpdate)
                    {

                        rule.RemoteAddresses = String.Join(",", listIpNewCollected.ToList());
                        //FwUtils.UpdateRuleForRemoteAddresses(ruleName, direction, protocole, listIpNewCollected.ToList());
                        log.Debug("Règle {0}:{1} mise à jour ", protocole, ruleName);

                        Superior.ShowStatusBarMessage(String.Format("Règle {0} mise à jour", ruleName), "Règle pare-feu");

                    }
                    else
                    {
                        Superior.ShowStatusBarMessage(String.Format("Règle {0} : pas de mise à jour", ruleName), "Règle pare-feu");
                    }
                }
                else
                {
                    INetFwRule newFwRule = FwUtils.CreateRuleForRemoteAddresses(ruleName, direction, protocole, true, "", listIpNewCollected.Select(r => r.ToString()).ToList());

                    ProcessFileFwRule p = new ProcessFileFwRule();
                    p.RuleName = ruleName;
                    p.HydrateWithFwRule(newFwRule);
                    p.FilePath = SessionFileInfo;

                    if (!Superior.GetConfManager().IsExistProcessFileFwRule(p))
                    {
                        Superior.GetConfManager().AddProcessFileFwRule(p);
                    }

                    Superior.ShowStatusBarMessage(String.Format("Règle {0} créée", ruleName), "Règle pare-feu");
                    AppPages.MonitorProcessPage.RefreshRules();

                    //log.Debug("Règle {0}:{1} créée ({2})", protocole, ruleName, MiscAppUtils.ListStrToStr(listIpNewCollected.Select(r => r.ToString()).ToList()));

                }
            }
        }


        private void btnBrowseForFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                AddExtension = true,
                Filter = "Fichier exe|*.exe",

                //CheckPathExists = true,
                //CheckFileExists = true
            };

            of.ShowDialog();

            if (String.IsNullOrWhiteSpace(of.FileName))
            {
                return;
            }

            OpenExeForMonitor(of.FileName);
        }

        private void btnSelectProcess_Click(object sender, RoutedEventArgs e)
        {
            SelectProcessusView spv = new SelectProcessusView();
            spv.ShowDialog();

            if (spv.ProcessSelected == null) return;

            String filepath = MiscAppUtils.GetExecutablePath(spv.ProcessSelected);
            OpenExeForMonitor(filepath);


        }

        private void OpenExeForMonitor(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            if (!fi.Exists)
            {
                Superior.ShowStatusBarMessage("Le fichier n'existe pas", "Executable à surveiller");
                return;
            }

            FileVersionInfo fVi = FileVersionInfo.GetVersionInfo(fi.FullName);
            String fileNiceName = String.Format("{0} - {1}", fVi.ProductName, fVi.ProductVersion);

            tExeName.Text = fi.FullName;
            lExeName.Content = fileNiceName;

            using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(fi.FullName))
            {
                icoExe.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }

            void OnCompletedEventHandler(object sender, RunWorkerCompletedEventArgs args)
            {
                if (autoCreateFwRuleTimer != null) StopAutoCreateFwTimer();

                rectIcoNotSet.Visibility = Visibility.Collapsed;
                gpCollect.IsEnabled = true;

                eventsCollector = null;
                lboxTcpOut.Items.Clear();
                lboxUdpOut.Items.Clear();

                SessionFileInfo = fi;
                log.Debug("Session chargée avec : {0}", fi.FullName);

                Superior.ShowStatusBarMessage(String.Format("Session chargée avec : {0}", fi.FullName), "Executable à surveiller");

                InitLinks();

                if (!FwUtils.IsExistsRuleForProgram(fi.FullName, DirectionsEnum.Outbound))
                {
                    var rep = MessageBox.Show("Il n'existe aucune règle sortante associée à ce programme : voulez-vous la créer ?",
                        "Question", MessageBoxButton.YesNo);
                    if (rep == MessageBoxResult.Yes)
                    {

                        FwUtils.CreateRuleForProgram($"{fVi.ProductName} ({fi.Name})", DirectionsEnum.Outbound, ProtocoleEnum.ALL, true, "", fi.FullName);

                    }
                }
            }

            if (collectBackgroundWorker != null)
            {
                collectBackgroundWorker.RunWorkerCompleted += OnCompletedEventHandler;
                StopCollecteBackgrounder();
            }
            else
            {
                OnCompletedEventHandler(null, null);
            }

        }





        private void StopCollecteBackgrounder()
        {
            collectBackgroundWorker?.CancelAsync();

            StopAutoCreateFwTimer();

            btnCollect.Content = "Collecter";

            if (collectBackgroundWorker != null)
            {
                collectBackgroundWorker = null;
            }

        }

        private void tExeName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            bool cIsReadOnly = tExeName.IsReadOnly;
            if (!cIsReadOnly)
            {
                if (!String.IsNullOrWhiteSpace(tExeName.Text))
                {
                    OpenExeForMonitor(tExeName.Text);
                }

                tExeName.IsReadOnly = true;
                tExeName.ToolTip = "Double-cliquez sur le champs pour modifier le chemin";
            }
            else
            {
                tExeName.IsReadOnly = false;
                tExeName.ToolTip = "Double-cliquez sur le champs pour appliquer la modification du chemin";

            }
        }

        public void StopCollectFor(FileInfo filePath)
        {
            if (filePath == null || SessionFileInfo == null) return;
            if (filePath.FullName.Equals(SessionFileInfo.FullName) && IsBusy)
            {
                StopCollecteBackgrounder();
            }
        }

        public void Refresh()
        {
            InitLinks();
        }


        private void InitLinks()
        {
            if (SessionFileInfo == null) return;
            SetStateLink(linkIn, SessionFileInfo.FullName, DirectionsEnum.Inboud);
            SetStateLink(linkOut, SessionFileInfo.FullName, DirectionsEnum.Outbound);
            gpRuleFw.IsEnabled = true;

        }

        private void LinkClick(DirectionsEnum directions, ProtocoleEnum protocole)
        {
            if (!FwUtils.IsExistsRuleForProgram(SessionFileInfo.FullName, directions))
            {
                FileVersionInfo fVi = FileVersionInfo.GetVersionInfo(SessionFileInfo.FullName);
                FwUtils.CreateRuleForProgram($"{fVi.ProductName} ({SessionFileInfo.Name})", directions, ProtocoleEnum.ALL, true, "", SessionFileInfo.FullName);
            }
            else
            {
                INetFwRule[] rules = FwUtils.GetRulesForProgram(SessionFileInfo.FullName, directions);



                bool isAllEnabled = rules.All(r => r.Enabled);
                if (rules.Length > 1)
                {
                    var res = MessageBox.Show(String.Format("Plusieurs règles {0} existent. Voulez-vous vraiment toutes les {1} ?", directions, isAllEnabled ? "désactiver" : "activer"), "Question",
                        MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                foreach (INetFwRule rule in rules)
                {
                    rule.Enabled = !isAllEnabled;
                }

            }

            InitLinks();
        }

        private static void SetStateLink(Hyperlink link, string filepath, DirectionsEnum directions)
        {

            if (!FwUtils.IsExistsRuleForProgram(filepath, directions))
            {
                link.SetContent("Créer la règle");
            }
            else
            {
                INetFwRule[] rules = FwUtils.GetRulesForProgram(filepath, directions);
                bool isAllEnabled = rules.All(r => r.Enabled);
                link.SetContent(isAllEnabled ? "Désactiver la règle" : "Activer la règle");
            }
        }

    }
}
