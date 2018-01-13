using Guiet.kQuatre.Business.Configuration;
using Guiet.kQuatre.Business.Firework;
using Guiet.kQuatre.UI.ViewModel;
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
using System.Windows.Shapes;

namespace Guiet.kQuatre.UI.Views
{
    /// <summary>
    /// Logique d'interaction pour FireworkManagementWindow.xaml
    /// </summary>
    public partial class FireworkManagementWindow : Window
    {
        private FireworkManagementViewModel _viewModel = null;

        private SoftwareConfiguration _softwareConfiguration = null;

        public FireworkManagementWindow(SoftwareConfiguration softwareConfiguration)
        {
            InitializeComponent();

            _softwareConfiguration = softwareConfiguration;

            this.Loaded += FireworkManagementWindow_Loaded;
        }

        private void FireworkManagementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this._viewModel = new FireworkManagementViewModel(_softwareConfiguration);
            this.DataContext = _viewModel;
        }
    }
}
