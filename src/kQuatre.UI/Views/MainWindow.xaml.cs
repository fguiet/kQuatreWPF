using fr.guiet.kquatre.ui.helpers;
using fr.guiet.kquatre.ui.viewsmodel;
using System;
using System.Windows;
using Telerik.Windows.Controls;

namespace fr.guiet.kquatre.ui.views
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Members

        private MainWindowViewModel _viewModel = null;        

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;            
        }         

        #endregion

        #region Events

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this._viewModel = new MainWindowViewModel();
            this.DataContext = _viewModel;

            //KeyPressed handler
            this.KeyDown += MainWindow_KeyDown;                   
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {            
            _viewModel.KeyPress(e.Key);            
        }

        private void MiOpenExcel_Click(object sender, System.EventArgs e)
        {
            _viewModel.LoadFireWork(true);    
        }

        private void XamMenuItem_Click(object sender, System.EventArgs e)
        {
            _viewModel.OpenConfigurationWindow();
        }

        private void MiOpenK4_Click(object sender, EventArgs e)
        {
            _viewModel.LoadFireWork(false);
        }

        /// <summary>
        /// Save firework definition in k4 format
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MiSave_Click(object sender, EventArgs e)
        {
            try
            {
                _viewModel.SaveFirework();
            }
            catch
            {
                DialogBoxHelper.ShowErrorMessage("Une erreur est apparue lors de la sauvegarde du fichier k4");
            }
        }

        private void MiSaveAs_Click(object sender, EventArgs e)
        {
            _viewModel.SaveAsFirework();
        }

        private void MiNewFirework_Click(object sender, EventArgs e)
        {
            _viewModel.NewFirework();
        }

        private void MiFireworkManagement_Click(object sender, EventArgs e)
        {
            _viewModel.OpenFireworkManagementWindow();
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenTestRadTimeline();
        }

        private void MiQuit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel.QuitApplication())
            {
                Application.Current.Shutdown();
            }
            else
            {
                //User does not want to quit
                e.Cancel = true;
            }
        }

        /*private void _nvRadnavigationView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is RadNavigationViewItem rnvi) {
                /*switch (rnvi.Name)
                {
                    case "_nviFirework":
                        _ucFireworkUserControlView.ResetControlPanel();
                        break;
                }*/
          //  }          
        //}

        /*private void XamTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_tabFire.IsSelected)
            {
                _ucFireworkUserControlView.ResetControlPanel();
            }

            //TODO : Vérifier qu'un feu n'est pas en cours et sinon 
            //interdir le changement !!
        }*/

        #endregion


    }
}
