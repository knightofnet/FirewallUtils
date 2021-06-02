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

namespace PocFwIpApp.view.page
{
    /// <summary>
    /// Logique d'interaction pour MonitorProcessPage.xaml
    /// </summary>
    public partial class MonitorProcessPage : Page, MonitorProcessPage.IMonitorProcessPage
    {
        public interface IMonitorProcessPage : AppPages.IChildView
        {
            void RefreshRules();
        }

        public IMainWindow Superior { get; set; }

        public bool IsBusy => IsPageBusy();
        public Page Page { get; }


        private static Logger log;

        private DispatcherTimer _fwProcessCheckTimer;

        public bool IsUpdateFwRules { get; set; }

        private ProcessFwruleManager _pfManager;

        
        public MonitorProcessPage(IMainWindow mainWindow)
        {
            Page = this;
            Superior = mainWindow;
            InitializeComponent();

            RunTrt();
            
        }

        private void RunTrt()
        {
            _pfManager = new ProcessFwruleManager();
            _pfManager.Gather(Superior.GetConfManager().ReadRules());

            gridEdit.IsEnabled = false;

            #region ListProcessCtxMenu
            ContextMenu listProcessCtxMenu = new ContextMenu();

            MenuItem killProcessLPM = new MenuItem()
            {
                Header = "Fin de tâche"
            };
            killProcessLPM.Click += (sender, args) =>
            {
                ProcessFileFwRule fwSel = listProcessToMonitor.SelectedItem as ProcessFileFwRule;
                if (fwSel == null) return;

                if (fwSel.Processes.Any())
                {
                    foreach (Process process in fwSel.Processes)
                    {
                        process.Kill();
                    }
                    AdaptUiForSelectedProcess();
                }
            };

            MenuItem revealInExplorerLPM = new MenuItem()
            {
                Header = "Réveler dans l'explorateur"
            };
            revealInExplorerLPM.Click += (sender, args) =>
            {
                ProcessFileFwRule fwSel = listProcessToMonitor.SelectedItem as ProcessFileFwRule;
                if (fwSel == null) return;

                FileUtils.ShowFileInWindowsExplorer(fwSel.FilePath);
            };

            MenuItem toggleModeManuelLPM = new MenuItem()
            {
                Header = "Activer/Désactiver la règle",
            };
            toggleModeManuelLPM.IsChecked = false;
            ProcessFileFwRule eltSel = listProcessToMonitor.SelectedItem as ProcessFileFwRule;
            if (eltSel != null)
            {
                toggleModeManuelLPM.IsChecked = eltSel.FwRule.Enabled;
            }
            toggleModeManuelLPM.Click += (sender, args) =>
            {
                MenuItem sMenuItem = sender as MenuItem;
                if (sMenuItem == null) return;

                bool state = !sMenuItem.IsChecked;

                ProcessFileFwRule fwSel = listProcessToMonitor.SelectedItem as ProcessFileFwRule;
                if (fwSel == null) return;

                sMenuItem.IsChecked = state;
                fwSel.FwRule.Enabled = state;
                fwSel.IsModeManuel = true;

                AdaptUiForSelectedProcess();

            };


            listProcessCtxMenu.Items.Add(killProcessLPM);
            listProcessCtxMenu.Items.Add(new Separator());
            listProcessCtxMenu.Items.Add(revealInExplorerLPM);
            listProcessCtxMenu.Items.Add(toggleModeManuelLPM);
            #endregion

            listProcessToMonitor.ItemsSource = _pfManager.FileFwRules;
            listProcessToMonitor.DisplayMemberPath = "DisplayItem";
            listProcessToMonitor.ContextMenu = listProcessCtxMenu;

            cbEnableMonitorProcess.IsEnabled = true;

            IsUpdateFwRules = true;
            cbEnableMonitorProcess.Click += (sender, args) =>
            {
                IsUpdateFwRules = cbEnableMonitorProcess.IsChecked ?? false;
            };

            _fwProcessCheckTimer = new DispatcherTimer();
            _fwProcessCheckTimer.Interval = new TimeSpan(0, 0, 5);
            _fwProcessCheckTimer.Tick += (sender, args) => DoMonitorFwProcess();
            _fwProcessCheckTimer.Start();

            listProcessToMonitor.SelectionChanged += ListProcessToMonitorOnSelectionChanged;

            tFilepath.LostFocus += (sender, args) =>
            {
                ProcessFileFwRule p = GetSelectedItem();
                if (p == null) return;

                String tFilePath = tFilepath.Text;
                if (!File.Exists(tFilePath))
                {
                    MessageBox.Show("Le fichier n'est pas disponible.", "Titre", MessageBoxButton.OK);
                    tFilepath.Focus();
                    
                    return;
                }

                p.FilePath = new FileInfo(tFilePath);
                Superior.GetConfManager().UpdProcessFileFwRule(p);
            };

            cIsEnableFilenameOnly.Click += (sender, args) =>
            {
                ProcessFileFwRule p = GetSelectedItem();
                if (p == null) return;
                p.IsEnableOnlyFileName = cIsEnableFilenameOnly.IsChecked ?? false;
                Superior.GetConfManager().UpdProcessFileFwRule(p);
            };

            cIsModeManuel.Click += (sender, args) =>
            {
                ProcessFileFwRule p = GetSelectedItem();
                if (p == null) return;
                p.IsModeManuel = cIsModeManuel.IsChecked ?? false;
                Superior.GetConfManager().UpdProcessFileFwRule(p);
            };

            AppPages.ClosePage += AppPagesOnClosePage;
        }




        private bool IsPageBusy()
        {
            return IsUpdateFwRules;

        }

        private void DoMonitorFwProcess()
        {

            if (_pfManager.NeedRefresh())
            {
                _pfManager.Gather(Superior.GetConfManager().ReadRules());
            }

            if (!IsUpdateFwRules) return;

            foreach (ProcessFileFwRule fR in _pfManager.FileFwRules.Where(r => !r.IsModeManuel))
            {
                if (fR.FwRule.Enabled != fR.IsProcessUp)
                {
                    fR.RefreshRule();
                    fR.FwRule.Enabled = fR.IsProcessUp;
                    AppPages.CollectIpPage.StopCollectFor(fR.FilePath);
                }
            }

            listProcessToMonitor.Items.Refresh();
            AppPages.CollectIpPage.Refresh();
           AdaptUiForSelectedProcess();
        }


        private void listProcessToMonitor_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ProcessFileFwRule fwSel = listProcessToMonitor.SelectedItem as ProcessFileFwRule;
            if (fwSel == null) return;

            AppPages.CollectIpPage.SetTbExeTexts(fwSel.FilePath.FullName,
                "Chemin modifié : double-cliquez sur le champs pour appliquer la modification", true);

            Superior.NavigateToPage(AppPages.CollectIpPage.Page);

        }

        private void ListProcessToMonitorOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AdaptUiForSelectedProcess();
        }

        private void AdaptUiForSelectedProcess()
        {
            ProcessFileFwRule p = GetSelectedItem();
            if (p == null) return;

            AdaptUiForProcess(p);
            gridEdit.IsEnabled = true;
        }

        private void AdaptUiForProcess(ProcessFileFwRule p)
        {
            tbRuleName.Text = p.RuleName;

            if (!tFilepath.IsFocused)
            {
                tFilepath.Text = p.FilePath.FullName;
            }

            cIsModeManuel.IsChecked = p.IsModeManuel;
            cIsEnableFilenameOnly.IsChecked = p.IsEnableOnlyFileName;

            lblProcEnable.Content = p.IsProcessUp ? "actif" : "non lancé";
        }

        public ProcessFileFwRule GetSelectedItem()
        {
            ProcessFileFwRule p = (ProcessFileFwRule)listProcessToMonitor.SelectedItem as ProcessFileFwRule;
            if (p == null) return null;

            return p;
        }

        public void RefreshRules()
        {
            DoMonitorFwProcess();
        }

        private void AppPagesOnClosePage(object sender, ClosePageHandlerArgs args)
        {
           
            /*if (_pfManager == null || !_pfManager.FileFwRules.Any()) return;

            foreach (ProcessFileFwRule p in _pfManager.FileFwRules)
            {
                Superior.GetConfManager().UpdProcessFileFwRule(p);
            }
            */
        }
    }
}
