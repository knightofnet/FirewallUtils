using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PocFwIpApp.view.usercontrol
{
    /// <summary>
    /// Logique d'interaction pour SidebarIcon.xaml
    /// </summary>
    public partial class SidebarIcon : UserControl
    {
        private ImageSource _source;

        private Brush _currentBrush;

        private Brush _hoverBrush;

        private bool _enabled;

        private object objectLock = new object();

        public Brush EnabledColorBrush { get; set; }
        public Brush DisabledColorBrush { get; set; }

        public Brush HoverColorBrush
        {
            get => _hoverBrush ?? EnabledColorBrush;
            set => _hoverBrush = value;
        }


        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                CurrentBrush = _enabled ? EnabledColorBrush : DisabledColorBrush;
            }
        }


        public Brush CurrentBrush
        {
            get => _currentBrush;

            set
            {
                _currentBrush = value;
                rectBg.Fill = _currentBrush;
            }
        }

        public ImageSource Source
        {
            get => _source;

            set
            {
                _source = value;
                imgIcon.Source = _source;
            }
        }


        public event MouseButtonEventHandler Click
        {
            add
            {
                lock (objectLock)
                {
                    grid.MouseUp += value;
                    // imgIcon.MouseUp += value;
                }
            }
            remove
            {
                lock (objectLock)
                {
                    grid.MouseUp -= value;
                }
            }
        }

        public SidebarIcon()
        {
            InitializeComponent();

            grid.MouseEnter += (sender, args) =>
            {
                CurrentBrush = HoverColorBrush;
            };

            grid.MouseLeave += (sender, args) =>
            {
                CurrentBrush = Enabled ? EnabledColorBrush : DisabledColorBrush;
            };
        }



    }
}
