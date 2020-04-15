using fr.guiet.kquatre.business.configuration;
using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.ui.helpers;
using fr.guiet.kquatre.ui.viewModel;
using Infragistics.Windows.DataPresenter;
using System.Windows;
using System.Windows.Controls;
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
     
        #endregion

        #region Constructor

        public DesignUserControlView()
        {
            InitializeComponent();

            //Only in runtime mode, not design mode
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                this.Loaded += DesignUserControlView_Loaded;
            }
        }

        #endregion

        #region Events

        private void BtnCheckFirework_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenSanityCheckWindow();
        }

        private void DesignUserControlView_Loaded(object sender, RoutedEventArgs e)
        {
            //Careful - Initialiaze ViewModel only once!
            if (_viewModel == null)
            {
                _viewModel = new DesignUserControlViewModel(FireworkManager, SoftwareConfiguration);
                DataContext = _viewModel;                
            }
        }

        #endregion

        #region Private Method

        //TODO : ajouter un row context menu avec options ajout, delete, modify) à la gridview (voir exemple telerik)

        //private void ExpandAllLine()
       // {
            //TODO : A Remplacer Telerik
            //Expands all lines...
            //Updating a property (here NumberUI) removes expand state...dunno why
            //So force an expandall...
            //_fireworkDatagrid.Records.ExpandAll(true);
        //}

        //manually refresh the control UI when bound to a data source which is not raising property change notifications for its data items
        //Here Line.ReceptorAddressUI is not bound and not raising property change
        //private void RefreshDataGrid()
        //{
            /*
            foreach (Record record in this._fireworkDatagrid.Records)
            {
                if (record is DataRecord)
                {
                    ((DataRecord)record).RefreshCellValues();
                }
            }
            */
        //}

        /// <summary>
        /// Add new firework to a line
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddFirework_Click(object sender, RoutedEventArgs e)
        {
            //TODO : A Remplacer Telerik
            //Line line = _fireworkDatagrid.ActiveDataItem as Line;

            //_viewModel.OpenFireworkManagementWindow(line);
        }

        private void BtnAlterLine_Click(object sender, RoutedEventArgs e)
        {

            if (_fireworkGridView.SelectedItem is Line line)
            {
                _viewModel.OpenLineWindow(line);
            }
            else
            {
                DialogBoxHelper.ShowWarningMessage("Veuillez sélectionner une ligne");
            }
        }

        private void BtnDeleteLine_Click(object sender, RoutedEventArgs e)
        {
            if (_fireworkGridView.SelectedItem is Line line)
            {
                _viewModel.DeleteLine(line);
                //Rebind needed to refresh datagrid after deletion
                _fireworkGridView.Rebind();                
            }
        }

        private void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenLineWindow(null);
            //Rebind needed to refresh datagrid after deletion
            _fireworkGridView.Rebind();
            
        }

        #endregion        
    }
}
