using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.business.receptor;
using fr.guiet.kquatre.ui.helpers;
using fr.guiet.kquatre.ui.viewmodel;
using fr.guiet.kquatre.ui.ViewModel;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace fr.guiet.kquatre.ui.views
{
    /// <summary>
    /// Logique d'interaction pour FireworkWindow.xaml
    /// </summary>
    public partial class FireworkWindow : Window
    {

        #region Private Members

        private FireworkWindowViewModel _viewModel = null;
        
        private Firework _firework = null;

        private FireworkManager _fireworkManager = null;

        #endregion

        #region Constructeur

        public FireworkWindow(FireworkManager fireworkManager, Firework firework)
        {
            InitializeComponent();

            _fireworkManager = fireworkManager;

            _firework = firework;            

            this.Loaded += FireworkWindow_Loaded;
        }

        #endregion
       
        #region Events

        private void FireworkWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this._viewModel = new FireworkWindowViewModel(_fireworkManager, _firework);
            this.DataContext = _viewModel;
        }

        private void _btnDissociate_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DissociateReceptorFromFirework();            
        }

        private void _btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                _viewModel.Save();                
            }

            this.Close();            
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

        #endregion        
    }
}
