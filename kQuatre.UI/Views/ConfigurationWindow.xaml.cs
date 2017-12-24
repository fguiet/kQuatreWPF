using Guiet.kQuatre.Business.Configuration;
using Guiet.kQuatre.UI.ViewModel;
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
using System.Windows.Shapes;

namespace Guiet.kQuatre.UI.Views
{
    /// <summary>
    /// Logique d'interaction pour ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        #region Private Members

        private ConfigurationWindowViewModel _viewModel = null;

        private SoftwareConfiguration _configuration = null;

        #endregion

        #region Constructor

        public ConfigurationWindow(SoftwareConfiguration configuration)
        {
            InitializeComponent();

            _configuration = configuration;

            this.Loaded += ConfigurationWindow_Loaded;
        }

        #endregion

        #region Events

        private void ConfigurationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this._viewModel = new ConfigurationWindowViewModel(_configuration);
            this.DataContext = _viewModel;

        }

        /// <summary>
        /// Expand all TreeGrid Node
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _configTreeGrid_InitializeRecord(object sender, Infragistics.Windows.DataPresenter.Events.InitializeRecordEventArgs e)
        {
            _configTreeGrid.ExecuteCommand(DataPresenterCommands.ToggleRecordIsExpanded, e.Record);
        }

        #endregion

        private void _configTreeGrid_EditModeEnding(object sender, Infragistics.Windows.DataPresenter.Events.EditModeEndingEventArgs e)
        {
            string newValue = e.Editor.Value.ToString();
            bool isValid = _viewModel.CheckConfigPropertyValue(e.Cell.Record.DataItem, newValue);

            if (!isValid)
            {
                e.Cancel = true;
                _viewModel.ShowErrorMessage(e.Cell.Record.DataItem);
            }
        }

        private void _configTreeGrid_CellUpdated(object sender, Infragistics.Windows.DataPresenter.Events.CellUpdatedEventArgs e)
        {
            _viewModel.IsDirty = true;
        }

       
        private void _btnSave_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Save();
        }

        private void _btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                string message = string.Format("Les modifications n'ont pas été sauvegardées.{0}Voulez-vous continuer ?", Environment.NewLine);
                if (MessageBox.Show(message, "Information", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No)
                {
                    return;
                }
            }

            this.Close();
        }
    }
}
