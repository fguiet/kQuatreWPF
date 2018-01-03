using Guiet.kQuatre.Business.Configuration;
using Guiet.kQuatre.Business.Firework;
using Guiet.kQuatre.Business.Receptor;
using Guiet.kQuatre.UI.ViewModel;
using Infragistics.Controls.Schedules;
using Infragistics.Windows.DataPresenter;
using System;
using System.Windows;

namespace Guiet.kQuatre.UI.Views
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
            this._viewModel = new MainWindowViewModel(_fireworkTimeline);
            this.DataContext = _viewModel;

            //Datagrid
            _fireworkDatagrid.InitializeRecord += FireworkDatagrid_InitializeRecord;                        
        }

        /// <summary>
        /// Expand all rows
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FireworkDatagrid_InitializeRecord(object sender, Infragistics.Windows.DataPresenter.Events.InitializeRecordEventArgs e)
        {
            _fireworkDatagrid.ExecuteCommand(DataPresenterCommands.ToggleRecordIsExpanded, e.Record);
        }

        private void _miOpenExcel_Click(object sender, System.EventArgs e)
        {
            _viewModel.LoadFireWork(true);    
        }

        private void XamMenuItem_Click(object sender, System.EventArgs e)
        {
            _viewModel.OpenConfigurationWindow();
        }
        
        #endregion

        private void _btnStart_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StartFirework();
        }

        private void _miOpenK4_Click(object sender, EventArgs e)
        {
            _viewModel.LoadFireWork(false);
        }

        /// <summary>
        /// Save firework definition in k4 format
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _miSave_Click(object sender, EventArgs e)
        {
            _viewModel.SaveFirework();
        }

        /// <summary>
        /// Add new firework to a line
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnAddFirework_Click(object sender, RoutedEventArgs e)
        {

        }
        
        private void _miFireworkManagement_Click(object sender, EventArgs e)
        {
            _viewModel.OpenFireworkManagementWindow();
        }

        private void _btnDeleteLine_Click(object sender, RoutedEventArgs e)
        {
            //Get Active Row
            Line line = _fireworkDatagrid.ActiveDataItem as Line;

            if (_viewModel.DeleteLine(line))
            {
                RefreshDataGrid();
                ExpandAllLine();
            }           
        }

        private void ExpandAllLine()
        {
            //Expands all lines...
            //Updating a property (here NumberUI) removes expand state...dunno why
            //So force an expandall...
            _fireworkDatagrid.Records.ExpandAll(true);
        }

        //manually refresh the control UI when bound to a data source which is not raising property change notifications for its data items
        //Here Line.ReceptorAddressUI is not bound and not raising property change
        private void RefreshDataGrid()
        {
            foreach (Record record in this._fireworkDatagrid.Records)
            {
                if (record is DataRecord)
                {
                    ((DataRecord)record).RefreshCellValues();
                }
            }
        }

        private void _btnAlterLine_Click(object sender, RoutedEventArgs e)
        {
            //Get Active Row
            Line line = _fireworkDatagrid.ActiveDataItem as Line;

            if (line != null)
            {
                _viewModel.OpenLineWindow(line);
                RefreshDataGrid();
                ExpandAllLine();
            }
            else
            {
                ShowWarningMessage("Veuillez sélectionner une ligne");
            }

        }

        private void _btnAddLine_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenLineWindow(null);
        }

        private void ShowWarningMessage(string message)
        {
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
        }
        
        private void _btnStopTestReceptor_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StopTestingReceptor();
        }

        private void _btnStartTestReceptor_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StartTestingReceptor();
        }

        /// <summary>
        /// Test whether a ligne is well connected by testing is resistance
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnTestResistance_Click(object sender, RoutedEventArgs e)
        {
            DataRecord dataRecord = _receptorChannelsDatagrid.ActiveRecord as DataRecord;
            ReceptorAddress ra = dataRecord.DataItem as ReceptorAddress;

            _viewModel.SelectedTestReceptor.TestResistance(ra);


        }

        private void _btnTest_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenTestRadTimeline();
        }
    }
}
