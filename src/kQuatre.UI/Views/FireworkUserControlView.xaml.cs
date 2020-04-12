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

        public static readonly DependencyProperty FireworkManagerProperty
            = DependencyProperty.Register("FireworkManager", typeof(FireworkManager), typeof(FireworkUserControlView));

        public FireworkManager  FireworkManager
        {
            get
            {
                return (FireworkManager)GetValue(FireworkManagerProperty);
            }
            set
            {
                SetValue(FireworkManagerProperty, (FireworkManager)value);                
            }
        }

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
            //Careful - Initialiaze ViewModel only once!
            if (_viewModel == null)
            {
                _viewModel = new FireworkUserControlViewModel(FireworkManager, _fireworkTimeline, this.Dispatcher);
                DataContext = _viewModel;                
            }
        }

        /*private void _btnStart_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StartFirework();
        }*/

        /*private void _btnStop_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StopFirework();
        }*/

        /// <summary>
        /// Reset Control panel to initial state
        /// </summary>
        public void ResetControlPanel()
        {
            _viewModel.ResetUserControlUI();
        }

        /*private void _chkArming_Checked(object sender, RoutedEventArgs e)
        {

            MessageBoxResult result = MessageBox.Show("Vous êtes sur le point d'armer le feu d'artifice ! Voulez-vous continuer ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            if (result == MessageBoxResult.Yes)
            {
                //begins by reseting line and firework (case when user stop and restart firework)
                _viewModel.ResetUI();

                //TODO : A revoir
                //_viewModel.RefreshControlPanelUI(MainWindowViewModel.RefreshControlPanelEventType.FireworkArmedEvent);
            }
            else
            {
                //Cancel event
                _viewModel.IsFireworkArmed = false;
            }
        }*/

        /*private void _chkArming_Unchecked(object sender, RoutedEventArgs e)
        {
            //TODO : A revoir
            //_viewModel.RefreshControlPanelUI(MainWindowViewModel.RefreshControlPanelEventType.FireworkArmedEvent);
        }*/

        private void _btnRedoFailed_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ActivateRedoFailedLine();
        }

        private void _fireworkTimeline_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
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

        #region Private Members

        #endregion
    }
}
