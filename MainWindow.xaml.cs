using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using AryxDevLibrary.extensions;
using AryxDevLibrary.utils;
using AryxDevLibrary.utils.logger;
using AryxDevViewLibrary.controls.Simplifier;
using NetFwTypeLib;
using PocFwIpApp.business;
using PocFwIpApp.constant;
using PocFwIpApp.dto;
using PocFwIpApp.utils;
using PocFwIpApp.view;
using PocFwIpApp.view.viewinterface;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace PocFwIpApp
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainWindow
    {

        private static Logger log;


        public bool IsRealClose { get; set; }

        private NotifyIcon _notifyIcon;

        private SQLiteConfManager DataManager { get; set; }


        public MainWindow()
        {
            Directory.CreateDirectory("logs");
            log = new Logger(string.Format("logs/{0:yy-MM-dd-HHmmss}-log.log", DateTime.Now),
                Logger.LogLvl.DEBUG, Logger.LogLvl.DEBUG);
            ExceptionHandlingUtils.Logger = log;

            log.Info("Lancement du programme");

            DataManager = new SQLiteConfManager("app.db");

            InitializeComponent();
            InitPages();

            InitView();
        }

        private void InitPages()
        {
            AppPages.Init(this);

            mainFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            mainFrame.Navigate(AppPages.MonitorProcessPage);

        }


        private void InitView()
        {
            InitNotifyIcon();

            ToggleRectForCollectIp(false);
            ToggleRectForTimer(false);


            // CreateRuleForIpAdress("testRule", dpaMap.GetByDirectionProtocol(DirectionsEnum.Outbound, ProtocoleEnum.TCP, entry => entry.GetRemoteAddress()));

            Title = $"Firewall utils {Assembly.GetExecutingAssembly().GetName().Version}";

          
        }

        private void InitNotifyIcon()
        {
            System.Windows.Forms.ContextMenu notifyIconCtxMenu = new System.Windows.Forms.ContextMenu();
            System.Windows.Forms.MenuItem restoreMenuItem = new System.Windows.Forms.MenuItem("Agrandir");
            restoreMenuItem.DefaultItem = true;
            restoreMenuItem.Click += (sender, args) =>
            {
                RestoreWindow();
            };

            System.Windows.Forms.MenuItem quitMenuItem = new System.Windows.Forms.MenuItem("Quitter");
            quitMenuItem.Click += (sender, args) =>
            {
                IsRealClose = true;
                Close();
            };

            notifyIconCtxMenu.MenuItems.Add(restoreMenuItem);
            notifyIconCtxMenu.MenuItems.Add(quitMenuItem);
            

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenu = notifyIconCtxMenu;
            _notifyIcon.Icon = PocFwIpApp.Properties.Resources.ico;
            _notifyIcon.MouseDoubleClick += (s, a) => RestoreWindow();
            ;

            Closing += OnClosingApp;
            Deactivated += OnDeactivated;
        }

        private void OnDeactivated(object sender, EventArgs e)
        {

            _notifyIcon.Text = "PocFwIpApp chargé";

            if (WindowState == WindowState.Minimized)
            {
                MinimizeToTray();
            }
        }


        private void OnClosingApp(object sender, CancelEventArgs args)
        {
            /*
            if (!IsRealClose && AppPages.Pages.Any(r => r.IsBusy))
            {
                notifyIcon.Text = "PocFwIpApp chargé";
                notifyIcon.MouseDoubleClick += (s, a) => RestoreWindow();

                MinimizeToTray();

                args.Cancel = true;
                return;
            }
            */

            // TODO à tester !
            bool isCancelClose = AppPages.OnClosePage(this);
            args.Cancel = isCancelClose;

            DataManager.Save();

        }


        private void RestoreWindow()
        {
            this.WindowState = WindowState.Normal;
            Activate();
            ShowInTaskbar = true;
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        private void MinimizeToTray()
        {
            this.WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
            }
        }

        public SQLiteConfManager GetConfManager()
        {
            return DataManager;
        }



        public bool NavigateToPage(Page pages)
        {
            return mainFrame.Navigate(pages);
        }


        public void ToggleRectForTimer(bool stateVisible)
        {
            rectForTimer.Visibility = stateVisible ? Visibility.Visible : Visibility.Hidden;
        }

        public void ToggleRectForCollectIp(bool stateVisible)
        {
            rectForBg.Visibility = stateVisible ? Visibility.Visible : Visibility.Hidden;
        }

        public void ShowStatusBarMessage(string message, string messageQualifier)
        {
            lStatusBar.Content = message;
        }


        private void imgCollectIp_MouseUp(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(AppPages.CollectIpPage.Page);
        }

        private void imgMonitor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(AppPages.MonitorProcessPage.Page);
        }

        private void imgListener_MouseUp(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(AppPages.ListenerPage.Page);
        }

        private void imgMiscTool_MouseUp(object sender, MouseButtonEventArgs e)
        {
            NavigateToPage(AppPages.MiscToolPage.Page);
        }
    }
}
