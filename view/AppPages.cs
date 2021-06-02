using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using PocFwIpApp.view.page;
using PocFwIpApp.view.viewinterface;

namespace PocFwIpApp.view
{
    public class AppPages
    {
        public interface IChildView
        {
            IMainWindow Superior { get; set; }
            bool IsBusy { get; }
            Page Page { get; }
        }

        public static CollectIpPage.ICollectIpPage CollectIpPage { get; private set; }
        public static MonitorProcessPage.IMonitorProcessPage MonitorProcessPage { get; private set; }
        public static ListenerPage.IListenerPage ListenerPage { get; private set; }
        public static MiscToolPage.IMiscToolPage MiscToolPage { get; private set; }

        public static HashSet<IChildView> Pages { get; private set; }


        public static event ClosePageHandler ClosePage;

        public static void Init(IMainWindow mainWindow)
        {
            CollectIpPage = new CollectIpPage(mainWindow);
            ///CollectIpPage.Superior = mainWindow;

            MonitorProcessPage = new MonitorProcessPage(mainWindow);
            //MonitorProcessPage.Superior = mainWindow;

            ListenerPage = new ListenerPage(mainWindow);

            MiscToolPage = new MiscToolPage(mainWindow);

            Pages = new HashSet<IChildView>();
            Pages.Add(CollectIpPage);
            Pages.Add(MonitorProcessPage);
            Pages.Add(ListenerPage);
            Pages.Add(MiscToolPage);

        }

        public static bool OnClosePage(IMainWindow sender )
        {
            if (ClosePage != null)
            {
              
                ClosePageHandlerArgs cArgs = new ClosePageHandlerArgs();
                ClosePage(sender, cArgs);
                if (cArgs.CancelClose)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public delegate void ClosePageHandler(object sender, ClosePageHandlerArgs args);

    public class ClosePageHandlerArgs : EventArgs
    {

        public bool CancelClose { get; set; }

    }
}
