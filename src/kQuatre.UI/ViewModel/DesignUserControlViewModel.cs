using fr.guiet.kquatre.business.configuration;
using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.ui.helpers;
using fr.guiet.kquatre.ui.views;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace fr.guiet.kquatre.ui.viewModel
{

    public class DesignUserControlViewModel : INotifyPropertyChanged
    {
        #region Private Members

        private FireworkManager _fireworkManager = null;
        private SoftwareConfiguration _configuration = null;

        #endregion

        #region Public Members

        public FireworkManager FireworkManager
        {
            get
            {
                return _fireworkManager;
            }
            set
            {
                if (_fireworkManager != value)
                {
                    _fireworkManager = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Constructor

        public DesignUserControlViewModel(FireworkManager fireworkManager, SoftwareConfiguration configuration)
        {
            _fireworkManager = fireworkManager;
            _fireworkManager.FireworkLoaded += _fireworkManager_FireworkLoaded;

            _configuration = configuration;
        }

        #endregion

        #region Events

        private void _fireworkManager_FireworkLoaded(object sender, EventArgs e)
        {
            RefreshGUI();
        }

        private void _fireworkManager_FireworkReset(object sender, EventArgs e)
        {
            RefreshGUI();
        }

        //TODO : Put this in base class
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Public Members

        public void OpenLineWindow(Line line)
        {
            LineWindow window = new LineWindow(_fireworkManager, line);
            window.ShowDialog();
        }

        public void OpenSanityCheckWindow()
        {
            SanityCheckWindow window = new SanityCheckWindow(_fireworkManager);
            window.ShowDialog();
        }

        public void OpenFireworkManagementWindow(Line line)
        {
            FireworkManagementWindow window = new FireworkManagementWindow(_fireworkManager, _configuration, line);
            window.ShowDialog();
        }

        /// <summary>
        /// Returns true/false whether line has been deleted or not
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public bool DeleteLine(Line line)
        {
            if (line == null)
            {
                DialogBoxHelper.ShowWarningMessage("Veuillez sélectionner une ligne à supprimer.");
            }
            else
            {
                string message = string.Format("Validez-vous la suppression de la ligne {0} ?.{1}Attention, les lignes seront réordonnées.", line.Number, Environment.NewLine);

                if (line.Fireworks.Count > 0)
                {
                    message = string.Format("La ligne {0} est associée à des artifices. Les associations seront supprimées et les lignes seront réordonnées.{1}Voulez-vous continuer ?", line.Number, Environment.NewLine);
                }

                if (DialogBoxHelper.ShowQuestionMessage(message) == MessageBoxResult.Yes)
                {
                    //Delete line!!
                    _fireworkManager.DeleteLine(line);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Private Members 

        private void RefreshGUI()
        {
            OnPropertyChanged("FireworkManager");
        }

        #endregion

    }
}
