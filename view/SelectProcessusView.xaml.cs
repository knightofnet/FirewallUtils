using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AryxDevLibrary.extensions;
using PocFwIpApp.dto;
using PocFwIpApp.utils;

namespace PocFwIpApp.view
{
    /// <summary>
    /// Logique d'interaction pour SelectProcessusView.xaml
    /// </summary>
    public partial class SelectProcessusView : Window
    {
   

        private ObservableCollection<ProcessExtended> _resProcesses = new ObservableCollection<ProcessExtended>();
        private DataGridTextColumn _colId;
        private DataGridTextColumn _colMwT;
        private DataGridTextColumn _colProcessName;

        public Process ProcessSelected { get; set; }

        public SelectProcessusView()
        {
            InitializeComponent();
            InitDg();

            tbSearch.TextChanged += tbSearchOnTextChanged;

            btnOk.Click += (sender, args) => ValidPaysAndReturn();
            dg.MouseDoubleClick += (sender, args) =>
            {
                if (dg.SelectedItem != null) ValidPaysAndReturn();
            };

            btnCancel.Click += (sender, args) => Close();
            
            RefreshProcess();
            tbSearch.Focus();

        }

        private void InitDg()
        {
            dg.AutoGenerateColumns = false;
            dg.IsReadOnly = true;
            dg.SelectionMode = DataGridSelectionMode.Single;

            dg.ItemsSource = _resProcesses;

            _colId = new DataGridTextColumn();
            _colId.Header = "Id";
            _colId.Binding = new Binding("Id");
            dg.Columns.Add(_colId);

            Binding b = new Binding();
            b.Converter = new SampleConverter();
            _colMwT = new DataGridTextColumn();
            _colMwT.Header = "Titre";
            _colMwT.Binding = b;
            dg.Columns.Add(_colMwT);

            _colProcessName = new DataGridTextColumn();
            _colProcessName.Header = "Nom du Processus";
            _colProcessName.Binding = new Binding("ProcessName");
            dg.Columns.Add(_colProcessName);



        }

        private void tbSearchOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbSearch.Text.Length <= 4) return;

            RefreshProcess();
        }

        private void RefreshProcess()
        {
            List<ProcessExtended> listProcessExtendeds = MiscAppUtils.GetPorProcessExtendeds();



            _resProcesses.Clear();

            if (String.IsNullOrWhiteSpace(tbSearch.Text))
            {
                listProcessExtendeds.ForEach(r => _resProcesses.Add(r));
            }
            else
            {
                foreach (ProcessExtended process in listProcessExtendeds.Where(r => (r.FileVersionInfo != null && r.FileVersionInfo.ProductName != null && r.FileVersionInfo.ProductName.IndexOf(tbSearch.Text, StringComparison.CurrentCultureIgnoreCase) >= 0) ||  r.ProcessName.IndexOf(tbSearch.Text, StringComparison.CurrentCultureIgnoreCase) >= 0))
                {
                    _resProcesses.Add(process);
                }
            }

            dg.Items.Refresh();

            if (_resProcesses.Count == 1)
            {
                dg.SelectedIndex = 0;
            }
        }

        private void ValidPaysAndReturn()
        {
            if (dg.SelectedItem != null)
            {
                ProcessExtended pSel = (ProcessExtended)dg.SelectedItem;
                ProcessSelected = pSel.Process;


            }

            Close();
        }


        public class SampleConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value != null && value is ProcessExtended pValue)
                {
                    if (pValue.FileVersionInfo != null)
                    {
                        return pValue.FileVersionInfo.ProductName;
                    }
                }
                return null;
            }



            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                return null;
            }
        }
    }
}
