using fr.guiet.kquatre.ui.helpers;
using fr.guiet.kquatre.ui.viewmodel;
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

        #endregion

        #region Public property

        public static readonly DependencyProperty DesignUserControlViewModelProperty
            = DependencyProperty.Register("ViewModel", typeof(DesignUserControlViewModel), typeof(DesignUserControlView));

        public DesignUserControlViewModel ViewModel
        {
            get { return (DesignUserControlViewModel)GetValue(DesignUserControlViewModelProperty); }
            set
            {
                SetValue(DesignUserControlViewModelProperty, (DesignUserControlViewModel)value);
            }
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
            //http://paulstovell.com/blog/mvvm-instantiation-approaches
            //With datatemplate usercontrol is loaded everytime...so viewmodel must be instanciate ones 
            //but outside of usercontrol

            //Initialize private memeber
            _viewModel = ViewModel;

            DataContext = _viewModel;
        }

        #endregion

        #region Private Method

        //TODO : ajouter un row context menu avec options ajout, delete, modify) à la gridview (voir exemple telerik)
              
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
