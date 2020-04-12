using fr.guiet.kquatre.business.configuration;
using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.ui.helpers;
using fr.guiet.kquatre.ui.viewModel;
using Infragistics.Windows.DataPresenter;
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
using Line = fr.guiet.kquatre.business.firework.Line;

namespace fr.guiet.kquatre.ui.views
{
    /// <summary>
    /// Logique d'interaction pour DesignUserControlView.xaml
    /// </summary>
    public partial class DesignUserControlView : UserControl
    {
        #region Private property

        private DesignUserControlViewModel _viewModel = null;
        //public FireworkManager _fireworkManager = null;

        #endregion

        #region Public property

        public static readonly DependencyProperty FireworkManagerProperty
            = DependencyProperty.Register("FireworkManager", typeof(FireworkManager), typeof(DesignUserControlView));

        public FireworkManager FireworkManager
        {
            get
            {
                return (FireworkManager)GetValue(FireworkManagerProperty);
            }
            set
            {
                SetValue(FireworkManagerProperty, (FireworkManager)value);
                //_fireworkManager = value;
            }
        }

        public static readonly DependencyProperty SoftwareConfigurationManagerProperty
            = DependencyProperty.Register("SoftwareConfiguration", typeof(SoftwareConfiguration), typeof(DesignUserControlView));

        public SoftwareConfiguration SoftwareConfiguration
        {
            get { return (SoftwareConfiguration)GetValue(SoftwareConfigurationManagerProperty); }
            set { SetValue(SoftwareConfigurationManagerProperty, (SoftwareConfiguration)value); }
        }

        /// <summary>
        /// Update FireworkManager objet on usercontrol
        /// </summary>
        /// <param name="fireworkManager"></param>
        /*public void UpdateViewModel(FireworkManager fireworkManager)
        {
            this.FireworkManager = fireworkManager;            
            _viewModel.FireworkManager = fireworkManager;            
        }*/

        #endregion

        #region Constructor

        public DesignUserControlView()
        {
            InitializeComponent();

            this.Loaded += DesignUserControlView_Loaded;
        }

        #endregion

        #region Events

        private void _btnCheckFirework_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenSanityCheckWindow();
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

        private void DesignUserControlView_Loaded(object sender, RoutedEventArgs e)
        {
            //Careful - Initialiaze ViewModel only once!
            if (_viewModel == null)
            {
                _viewModel = new DesignUserControlViewModel(FireworkManager, SoftwareConfiguration);
                DataContext = _viewModel;

                //Datagrid
                _fireworkDatagrid.InitializeRecord += FireworkDatagrid_InitializeRecord;
            }
        }

        #endregion

        #region Private Method

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

        /// <summary>
        /// Add new firework to a line
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnAddFirework_Click(object sender, RoutedEventArgs e)
        {
            Line line = _fireworkDatagrid.ActiveDataItem as Line;

            _viewModel.OpenFireworkManagementWindow(line);
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
                DialogBoxHelper.ShowWarningMessage("Veuillez sélectionner une ligne");
            }

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

        private void _btnAddLine_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenLineWindow(null);
            RefreshDataGrid();
            ExpandAllLine();
        }

        #endregion

    }
}
