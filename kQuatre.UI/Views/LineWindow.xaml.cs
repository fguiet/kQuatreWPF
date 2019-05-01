using Guiet.kQuatre.Business.Firework;
using Guiet.kQuatre.UI.ViewModel;
using System;
using System.Windows;

namespace Guiet.kQuatre.UI.Views
{
    /// <summary>
    /// Logique d'interaction pour LineWindow.xaml
    /// </summary>
    public partial class LineWindow : Window
    {
        private LineViewModel _viewModel = null;

        private Line _line = null;

        private FireworkManager _fireworkManager = null;      

        public LineWindow(FireworkManager fireworkManager, Line line)
        {
            InitializeComponent();

            _fireworkManager = fireworkManager;

            _line = line;

            this.Loaded += LineWindow_Loaded;
        }

        private void LineWindow_Loaded(object sender, RoutedEventArgs e)
        {                    
            this._viewModel = new LineViewModel(_fireworkManager, _line);
            this.DataContext = _viewModel;            
        }

        private void _btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                string message = string.Format("Les modifications n'ont pas été sauvegardées.{0}Voulez-vous continuer ?", Environment.NewLine);
                if (MessageBox.Show(message, "Information", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No)
                {
                    return;
                }
            }

            this.Close();
        }

        /// <summary>
        /// Save modification
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnSave_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Save();

            this.Close();
        }

        private void _btnDissociate_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Dissociate();
        }

        private void _cbxLineLocation_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {            
            _viewModel.UpdateLineNumber();
        }

        private void _chkRescueLine_Checked(object sender, RoutedEventArgs e)
        {
            _mkeIgnition.IsEnabled = false;
            _viewModel.InitializeLineLocation();
        }

        private void _chkRescueLine_Unchecked(object sender, RoutedEventArgs e)
        {
            _mkeIgnition.IsEnabled = true;
            _viewModel.InitializeLineLocation();
        }
    }
}
