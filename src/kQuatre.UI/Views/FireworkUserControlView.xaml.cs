using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.ui.viewmodel;
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
using Telerik.Windows.Controls;
using Line = fr.guiet.kquatre.business.firework.Line;

namespace fr.guiet.kquatre.ui.views
{
    /// <summary>
    /// Logique d'interaction pour FireworkUserControlView.xaml
    /// </summary>
    public partial class FireworkUserControlView : UserControl
    {
        #region Private property

        private FireworkUserControlViewModel _viewModel = null;

        #endregion

        #region Public property

        #region Public Members

        public static readonly DependencyProperty FireworkUserControlViewModelProperty
            = DependencyProperty.Register("ViewModel", typeof(FireworkUserControlViewModel), typeof(FireworkUserControlView));

        public FireworkUserControlViewModel ViewModel
        {
            get { return (FireworkUserControlViewModel)GetValue(FireworkUserControlViewModelProperty); }
            set
            {
                SetValue(FireworkUserControlViewModelProperty, (FireworkUserControlViewModel)value);
            }
        }

        #endregion

        #endregion

        #region Constructor

        public FireworkUserControlView()
        {
            InitializeComponent();

            this.Loaded += FireworkUserControlView_Loaded;
        }

        #endregion

        #region Events
       
        private void FireworkUserControlView_Loaded(object sender, RoutedEventArgs e)
        {
            //http://paulstovell.com/blog/mvvm-instantiation-approaches
            //With datatemplate usercontrol is loaded everytime...so viewmodel must be instanciate ones 
            //but outside of usercontrol

            //Initialize private memeber
            _viewModel = ViewModel;
            _viewModel.SetRadtimeline(_fireworkTimeline);

            DataContext = _viewModel;

            //Reset control each time it is loaded
            ResetControlPanel();
        }

        private void FireworkTimeline_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                Firework f = (Firework)e.AddedItems[0];
                //DialogBoxHelper.ShowInformationMessage("Ligne sélectionnée : ");

                _viewModel.LaunchFailedLine(f.AssignedLine.Number);

                _fireworkTimeline.SelectedItem = null;
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Reset Control panel to initial state
        /// </summary>
        private void ResetControlPanel()
        {
            _viewModel.ResetUserControlUI();
        }

        #endregion
    }
}
