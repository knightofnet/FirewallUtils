using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Management.Deployment;
using AryxDevLibrary.utils;
using NetFwTypeLib;
using PocFwIpApp.constant;
using PocFwIpApp.dto;
using PocFwIpApp.utils;
using PocFwIpApp.view.viewinterface;
using Binding = System.Windows.Data.Binding;
using Path = System.IO.Path;

namespace PocFwIpApp.view.page
{
    /// <summary>
    /// Logique d'interaction pour MiscToolPage.xaml
    /// </summary>
    public partial class MiscToolPage : Page, MiscToolPage.IMiscToolPage
    {
        public interface IMiscToolPage : AppPages.IChildView
        {
        }

        public IMainWindow Superior { get; set; }
        public bool IsBusy => IsPageBusy();



        public Page Page { get; }

        public MiscToolPage(IMainWindow mainWindow)
        {
            Superior = mainWindow;
            Page = this;

            InitializeComponent();
            InitTabLinkBroken();
        }

        private void InitTabLinkBroken()
        {
            dgRuleLinkBroken.AutoGenerateColumns = false;
            dgRuleLinkBroken.IsReadOnly = true;
            

            

            DataGridTextColumn colRuleName = new DataGridTextColumn();
            colRuleName.Header = "Nom de la règle";
            colRuleName.Binding = new Binding("RuleName");
            dgRuleLinkBroken.Columns.Add(colRuleName);

            DataGridTextColumn colDirection = new DataGridTextColumn();
            colDirection.Header = "Direction";
            colDirection.Binding = new Binding("Direction");
            dgRuleLinkBroken.Columns.Add(colDirection);

            DataGridTextColumn colProgram = new DataGridTextColumn();
            colProgram.Header = "Programme";
            colProgram.Binding = new Binding("Filepath");
            dgRuleLinkBroken.Columns.Add(colProgram);

            DataGridTextColumn colMessage = new DataGridTextColumn();
            colMessage.Header = "";
            colMessage.Binding = new Binding("Message");
            dgRuleLinkBroken.Columns.Add(colMessage);
            dgRuleLinkBroken.UpdateLayout();

        }

        private bool IsPageBusy()
        {
            return false;
        }

        private void btnSearchInvalideRules_Click(object sender, RoutedEventArgs e)
        {
            List<INetFwRule> retList = new List<INetFwRule>();
            List<FwRuleInvalid> filteredList = new List<FwRuleInvalid>();

            retList.AddRange(FwUtils.GetRulesWithProgram(DirectionsEnum.NULL));
            
            foreach (INetFwRule rule in retList)
            {
                if (MiscAppUtils.IsValidFilepath(rule.ApplicationName) && !File.Exists(rule.ApplicationName))
                {
                    FwRuleInvalid fwRuleInvalid = new FwRuleInvalid()
                    {
                        RuleName = rule.Name,
                        Filepath = rule.ApplicationName,
                        FwRule = rule,
                        Direction = FwUtils.FwRuleDirectionToDirectionEnum(rule.Direction).ToString()
                    };

                    if (Cst.WindowsAppRegex.IsMatch(fwRuleInvalid.Filepath))
                    {
                        Match m = Cst.WindowsAppRegex.Match(fwRuleInvalid.Filepath);

                        PackageManager f = new PackageManager();
                        var packagesFound = f.FindPackages().Where(r => r.Id.Name.Equals(m.Groups["name"].Value, StringComparison.OrdinalIgnoreCase)).ToArray();

                        if (packagesFound.Any())
                        {
                            String fp = Path.Combine(packagesFound[0].InstalledLocation.Path, m.Groups["subDir"].Value);
                            if (File.Exists(fp))
                            {
                                fwRuleInvalid.Message = "Nouvelle version WindowsApp trouvée.";
                                fwRuleInvalid.FilepathCorrected = fp;
                            }
                        }

                    }


                    filteredList.Add(fwRuleInvalid);
                }
            }

            dgRuleLinkBroken.ItemsSource = filteredList;
            dgRuleLinkBroken.Items.Refresh();

        }

        private void btnModify_Click(object sender, RoutedEventArgs e)
        {
            List<FwRuleInvalid> list = new List<FwRuleInvalid>();
            foreach (FwRuleInvalid f in dgRuleLinkBroken.SelectedItems.Cast<FwRuleInvalid>())
            {
                if (!String.IsNullOrWhiteSpace(f.FilepathCorrected))
                {
                    list.Add(f);
                }
            }


            if (!list.Any()) return;

            foreach (FwRuleInvalid fwRule in list)
            {
                fwRule.FwRule.ApplicationName = fwRule.FilepathCorrected;

            }
        }
    }
}
