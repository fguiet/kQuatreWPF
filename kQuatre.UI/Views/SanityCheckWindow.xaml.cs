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
    /// Logique d'interaction pour SanityCheckWindow.xaml
    /// </summary>
    public partial class SanityCheckWindow : Window
    {
        private FireworkManager _fireworkManager = null;

        private SanityCheckViewModel _viewModel = null;

        public SanityCheckWindow(FireworkManager fm)
        {           
            InitializeComponent();

            _fireworkManager = fm;

            this.Loaded += SanityCheckWindow_Loaded; ;
        }

        private void SanityCheckWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this._viewModel = new SanityCheckViewModel(_fireworkManager);
            this.DataContext = _viewModel;
        }

        private void _btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
