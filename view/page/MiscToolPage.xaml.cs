using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using AryxDevLibrary.utils;
using Microsoft.Win32;
using NetFwTypeLib;
using PocFwIpApp.business;
using PocFwIpApp.constant;
using PocFwIpApp.dto;
using PocFwIpApp.utils;
using PocFwIpApp.view.viewinterface;
using Binding = System.Windows.Data.Binding;
using MenuItem = System.Windows.Controls.MenuItem;
using Path = System.IO.Path;

namespace PocFwIpApp.view.page
{
    /// <summary>
    /// Logique d'interaction pour MiscToolPage.xaml
    /// </summary>
    public partial class MiscToolPage : Page, MiscToolPage.IMiscToolPage
    {
        private MenuItem _autoCorrectMenuItem;
        private MenuItem _correctOneMenuItem;
        private MenuItem _deleteMenuItem;
        private List<FwRuleInvalid> _listInvalidRulesSourceDg;

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

            chkSrchIvlRlsAuto.Click += (sender, args) =>
            {
                bool isChecked = chkSrchIvlRlsAuto.IsChecked ?? false;

            };
        }

        private void InitTabLinkBroken()
        {
            dgRuleLinkBroken.AutoGenerateColumns = false;
            dgRuleLinkBroken.IsReadOnly = true;


            DataGridTextColumn colRuleName = new DataGridTextColumn();
            colRuleName.Header = "Nom";
            colRuleName.Binding = new Binding("RuleName");
            dgRuleLinkBroken.Columns.Add(colRuleName);

            DataGridTextColumn colProgram = new DataGridTextColumn();
            colProgram.Header = "Programme";
            colProgram.Binding = new Binding("Filepath");
            dgRuleLinkBroken.Columns.Add(colProgram);

            DataGridTextColumn colEnable = new DataGridTextColumn();
            colEnable.Header = "Règle activée";
            colEnable.Binding = new Binding("Enabled");
            dgRuleLinkBroken.Columns.Add(colEnable);

            DataGridTextColumn colAction = new DataGridTextColumn();
            colAction.Header = "Action";
            colAction.Binding = new Binding("Action");
            dgRuleLinkBroken.Columns.Add(colAction);

            DataGridTextColumn colDirection = new DataGridTextColumn();
            colDirection.Header = "Direction";
            colDirection.Binding = new Binding("Direction");
            dgRuleLinkBroken.Columns.Add(colDirection);


            DataGridTextColumn colMessage = new DataGridTextColumn();
            colMessage.Header = "";
            colMessage.Binding = new Binding("Message");
            dgRuleLinkBroken.Columns.Add(colMessage);
            dgRuleLinkBroken.UpdateLayout();



            _autoCorrectMenuItem = new MenuItem();
            _autoCorrectMenuItem.Header = "Corriger";
            _autoCorrectMenuItem.Click += (sender, args) =>
            {
                var list = GetDgSelectedItems();
                if (!list.Any()) return;

                if (list.TrueForAll(r => r.IsWindowsApp))
                {
                    foreach (FwRuleInvalid fwRule in list.Where(r => File.Exists(r.FilepathCorrected)))
                    {
                        if (fwRule.IsRealRule)
                        {
                            fwRule.FwRule.ApplicationName = fwRule.FilepathCorrected;
                        }
                        else
                        {
                            fwRule.ProcessFileFwRule.FilePath = new FileInfo(fwRule.FilepathCorrected);
                            Superior.GetConfManager().UpdProcessFileFwRule(fwRule.ProcessFileFwRule);
                            AppPages.MonitorProcessPage.RefreshRules();
                        }
                        bool isDeleted = _listInvalidRulesSourceDg.Remove(fwRule);
                        dgRuleLinkBroken.Items.Refresh();


                    }
                }
            };

            _correctOneMenuItem = new MenuItem();
            _correctOneMenuItem.Header = "Corriger manuellement...";
            _correctOneMenuItem.Click += (sender, args) =>
            {
                var list = GetDgSelectedItems();
                if (!list.Any() || list.Count > 1) return;

                FwRuleInvalid fwRule = list[0];

                OpenFileDialog of = new OpenFileDialog();
                of.Filter = "Tous les fichiers|*.*";
                of.AddExtension = true;
                of.Multiselect = false;
                of.CheckFileExists = true;
                of.InitialDirectory = MiscAppUtils.GetLastValidDirectory(fwRule.Filepath);
                if (of.ShowDialog() == true)
                {

                    if (fwRule.IsRealRule)
                    {
                        fwRule.FwRule.ApplicationName = of.FileName;
                    }
                    else
                    {
                        fwRule.ProcessFileFwRule.FilePath = new FileInfo(of.FileName);
                        Superior.GetConfManager().UpdProcessFileFwRule(fwRule.ProcessFileFwRule);
                        AppPages.MonitorProcessPage.RefreshRules();
                    }

                    bool isDeleted = _listInvalidRulesSourceDg.Remove(fwRule);
                    dgRuleLinkBroken.Items.Refresh();
                }


            };

            _deleteMenuItem = new MenuItem();
            _deleteMenuItem.Header = "Supprimer";
            _deleteMenuItem.Click += (sender, args) =>
            {
                var list = GetDgSelectedItems();
                if (!list.Any()) return;

                foreach (FwRuleInvalid fw in list)
                {
                    String originalRuleName = fw.FwRule.Name;
                    try
                    {
                        if (fw.IsRealRule)
                        {
                            String newName = StringUtils.RandomString(16, ensureUnique: true);
                            fw.FwRule.Name = newName;
                            FwUtils.RemoveRule(newName, FwUtils.FwRuleDirectionToDirectionEnum(fw.FwRule.Direction));
                            bool isDeleted = _listInvalidRulesSourceDg.Remove(fw);
                        }

                        dgRuleLinkBroken.Items.Refresh();

                    }
                    catch (Exception e)
                    {
                        ExceptionHandlingUtils.LogAndHideException(e, "DeleteInvalidFwRule");
                        fw.FwRule.Name = originalRuleName;

                    }
                }
            };

            ContextMenu dgCtxMenu = new ContextMenu();
            dgCtxMenu.Items.Add(_autoCorrectMenuItem);
            dgCtxMenu.Items.Add(_correctOneMenuItem);
            dgCtxMenu.Items.Add(new Separator());
            dgCtxMenu.Items.Add(_deleteMenuItem);


            dgRuleLinkBroken.ContextMenu = dgCtxMenu;



            dgRuleLinkBroken.SelectedCellsChanged += (sender, args) =>
            {
                List<FwRuleInvalid> selItems = GetDgSelectedItems();

                _correctOneMenuItem.IsEnabled = selItems.Count == 1;

                _autoCorrectMenuItem.IsEnabled =
                    selItems.TrueForAll(r => r.IsWindowsApp);

                _deleteMenuItem.Header = $"Supprimer ({selItems.Count})";
            };
        }

        private bool IsPageBusy()
        {
            return false;
        }

        private void btnSearchInvalideRules_Click(object sender, RoutedEventArgs e)
        {
            List<INetFwRule> retList = new List<INetFwRule>();
            _listInvalidRulesSourceDg = new List<FwRuleInvalid>();

            retList.AddRange(FwUtils.GetRulesWithProgram(DirectionsEnum.NULL));

            List<ProcessFileFwRule> listInnerRule = Superior.GetConfManager().ReadRules();
            foreach (ProcessFileFwRule pFwRule in listInnerRule)
            {
                INetFwRule fwRule = FwUtils.GetRule(pFwRule.RuleName, pFwRule.DirectionProtocol.Direction,
                    pFwRule.DirectionProtocol.Protocol);
                if (fwRule == null)
                {
                    continue;
                }
                pFwRule.HydrateWithFwRule(fwRule);

                if (!MiscAppUtils.IsValidFilepath(pFwRule.FilePath.FullName) || File.Exists(pFwRule.FilePath.FullName)) continue;

                FwRuleInvalid fwRuleInvalid = new FwRuleInvalid()
                {
                    RuleName = pFwRule.RuleName,
                    Filepath = pFwRule.FilePath.FullName,
                    FwRule = pFwRule.FwRule,
                    Direction = pFwRule.DirectionProtocol.Direction.ToString(),

                };
                fwRuleInvalid.ProcessFileFwRule = pFwRule;

                TreatWindowsAppRule(fwRuleInvalid);

                _listInvalidRulesSourceDg.Add(fwRuleInvalid);
            }

            foreach (INetFwRule rule in retList)
            {
                if (!MiscAppUtils.IsValidFilepath(rule.ApplicationName) || File.Exists(rule.ApplicationName)) continue;

                FwRuleInvalid fwRuleInvalid = new FwRuleInvalid()
                {
                    RuleName = rule.Name,
                    Filepath = rule.ApplicationName,
                    FwRule = rule,
                    Direction = FwUtils.FwRuleDirectionToDirectionEnum(rule.Direction).ToString(),

                };

                TreatWindowsAppRule(fwRuleInvalid);


                _listInvalidRulesSourceDg.Add(fwRuleInvalid);
            }


            dgRuleLinkBroken.ItemsSource = _listInvalidRulesSourceDg;


            dgRuleLinkBroken.Items.Refresh();

        }

        private static void TreatWindowsAppRule(FwRuleInvalid fwRuleInvalid)
        {
            if (Cst.WindowsAppRegex.IsMatch(fwRuleInvalid.Filepath))
            {
                Match m = Cst.WindowsAppRegex.Match(fwRuleInvalid.Filepath);

                PackageManager f = new PackageManager();
                Package[] packagesFound = f.FindPackages()
                    .Where(r => r.Id.Name.Equals(m.Groups["name"].Value, StringComparison.OrdinalIgnoreCase)).ToArray();

                if (packagesFound.Any())
                {
                    String fp = Path.Combine(packagesFound[0].InstalledLocation.Path, m.Groups["subDir"].Value);
                    if (File.Exists(fp))
                    {
                        fwRuleInvalid.Message = "Nouvelle version WindowsApp trouvée.";
                        fwRuleInvalid.FilepathCorrected = fp;
                    }
                }

                fwRuleInvalid.IsWindowsApp = true;
            }
        }

        private void btnModify_Click(object sender, RoutedEventArgs e)
        {
            var list = GetDgSelectedItems();


            if (!list.Any()) return;

            foreach (FwRuleInvalid fwRule in list)
            {
                fwRule.FwRule.ApplicationName = fwRule.FilepathCorrected;

            }
        }

        private List<FwRuleInvalid> GetDgSelectedItems()
        {
            List<FwRuleInvalid> list = dgRuleLinkBroken.SelectedItems.Cast<FwRuleInvalid>().ToList();
            return list;
        }
    }
}
