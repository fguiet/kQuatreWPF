using fr.guiet.kquatre.ui.viewmodel;
using System.Windows;
using System.Windows.Controls;

namespace fr.guiet.kquatre.ui.views
{
    /// <summary>
    /// Logique d'interaction pour TestUserControlView.xaml
    /// </summary>
    public partial class TestUserControlView : UserControl
    {
        #region Private property

        private TestUserControlViewModel _viewModel = null;

        #endregion

        #region Public Members

        public static readonly DependencyProperty TestUserControlViewModelProperty
            = DependencyProperty.Register("ViewModel", typeof(TestUserControlViewModel), typeof(TestUserControlView));

        public TestUserControlViewModel ViewModel
        {
            get { return (TestUserControlViewModel)GetValue(TestUserControlViewModelProperty); }
            set
            { 
                SetValue(TestUserControlViewModelProperty, (TestUserControlViewModel)value);                
            }
        }

        #endregion

        #region Constructor

        public TestUserControlView()
        {
            InitializeComponent();            

            this.Loaded += TestUserControlView_Loaded;            
        }

        #endregion

        #region Events

        /// <summary>
        /// Triggers each time, user control is shown...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestUserControlView_Loaded(object sender, RoutedEventArgs e)
        {
            //http://paulstovell.com/blog/mvvm-instantiation-approaches
            //With datatemplate usercontrol is loaded everytime...so viewmodel must be instanciate ones 
            //but outside of usercontrol

            //Initialize private memeber
            _viewModel = ViewModel;

            DataContext = _viewModel;
        }

        private void CbxTestReceptors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.OnReceptorSelectionChanged();
        }

        #endregion
    }
}
