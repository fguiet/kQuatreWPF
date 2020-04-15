using fr.guiet.kquatre.business.configuration;
using fr.guiet.kquatre.ui.viewsmodel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace fr.guiet.kquatre.ui.views
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

        #endregion

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

        private void MarkCell(Control cell, string validationText)
        {
            ToolTipService.SetToolTip(cell, validationText);
        }

        private void RestoreCell(Control cell)
        {
            ToolTipService.SetToolTip(cell, null);
        }

        private void RadTreeListView_CellValidated(object sender, Telerik.Windows.Controls.GridViewCellValidatedEventArgs e)
        {
            _viewModel.IsDirty = true;
        }

        private void RadTreeListView_CellValidating(object sender, Telerik.Windows.Controls.GridViewCellValidatingEventArgs e)
        {
            string newValue = e.NewValue.ToString();
            string validationText = string.Empty;
            bool isValid = true;

            if (e.Cell.DataContext is ConfigPropertyNode)
            {
                isValid = _viewModel.CheckConfigPropertyValue(e.Cell.DataContext, newValue);
                if (!isValid)
                {
                    validationText = _viewModel.GetErrorMessage(e.Cell.DataContext);
                }
            }
            else
            {
                isValid = false;
                validationText = "Impossible de modifier cette cellule";
            }

            if (!isValid)
            {
                this.MarkCell(e.Cell, validationText);
            }
            else
            {
                this.RestoreCell(e.Cell);
            }

            e.ErrorMessage = validationText;
            e.IsValid = isValid;
        }
    }
}
