using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.ui.viewmodel;
using System.Windows;

namespace fr.guiet.kquatre.ui.views
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
