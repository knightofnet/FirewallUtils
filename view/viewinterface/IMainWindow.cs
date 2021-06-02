using System.Windows.Controls;
using PocFwIpApp.business;

namespace PocFwIpApp.view.viewinterface
{
    public interface IMainWindow
    {
        void ToggleRectForTimer(bool stateVisible);
        void ToggleRectForCollectIp(bool stateVisible);
        void ShowStatusBarMessage(string messagte, string messageQualifier);

        bool NavigateToPage(Page pages);

        SQLiteConfManager GetConfManager();
    }
}