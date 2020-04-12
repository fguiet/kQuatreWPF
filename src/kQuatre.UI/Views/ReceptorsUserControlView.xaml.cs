using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.business.receptor;
using fr.guiet.kquatre.ui.viewmodel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace fr.guiet.kquatre.ui.views
{
    /// <summary>
    /// Logique d'interaction pour ReceptorsUserControlView.xaml
    /// </summary>
    public partial class ReceptorsUserControlView : UserControl
    {
        #region Private property

        private ReceptorsUserControlViewModel _viewModel = null;

        #endregion

        public static readonly DependencyProperty FireworkManagerProperty
            = DependencyProperty.Register("FireworkManager", typeof(FireworkManager), typeof(ReceptorsUserControlView));

        public FireworkManager FireworkManager
        {
            get { return (FireworkManager)GetValue(FireworkManagerProperty); }
            set { SetValue(FireworkManagerProperty, (FireworkManager)value); }
        }  
        
        public ReceptorsUserControlView()
        {
            InitializeComponent();

            this.Loaded += ReceptorsUserControlView_Loaded;            
        }

        #region Events

        /// <summary>
        /// Triggers each time, user control is shown...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceptorsUserControlView_Loaded(object sender, RoutedEventArgs e)
        {
            //Careful - Initialiaze ViewModel only once!
            if (_viewModel == null)
            {
                _viewModel = new ReceptorsUserControlViewModel(FireworkManager, this.Dispatcher);
                DataContext = _viewModel;
            }
        }

        private void CbxTestReceptors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.OnReceptorSelectionChanged();
        }

        private void BtnTestResistance_Click(object sender, RoutedEventArgs e)
        {
            /*if (_viewModel.SelectedTestReceptor.IsResistanceTestInProgress)
            {
                DialogBoxHelper.ShowWarningMessage("Un test est déjà en cours d'éxécution !");
                return;
            }

            DataRecord dataRecord = _receptorChannelsDatagrid.ActiveRecord as DataRecord;
            ReceptorAddress ra = dataRecord.DataItem as ReceptorAddress;

            _viewModel.SelectedTestReceptor.TestResistance(ra);*/
        }

        #endregion
    }
}
