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

            StyleManager.SetTheme(_fireworkTimeline, new FluentTheme());

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

        /// <summary>
        /// Reset Control panel to initial state
        /// </summary>
        public void ResetControlPanel()
        {
            _viewModel.ResetUserControlUI();
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
    }
}
