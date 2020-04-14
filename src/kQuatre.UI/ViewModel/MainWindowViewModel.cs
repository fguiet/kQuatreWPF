using fr.guiet.kquatre.business.configuration;
using fr.guiet.kquatre.business.exceptions;
using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.business.transceiver;
using fr.guiet.kquatre.ui.events;
using fr.guiet.kquatre.ui.helpers;
using fr.guiet.kquatre.ui.views;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Timeline;

namespace fr.guiet.kquatre.ui.viewsmodel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {        
        #region Private Members          

        private FireworkManager _fireworkManager = null;

        private SoftwareConfiguration _configuration = null;        

        private string _deviceConnectionInfo = DeviceManager.DEFAULT_NOT_TRANSCEIVER_CONNECTED_MESSAGE;

        private const string SOFTWARE_TITLE = "kQuatre";        
        
        #endregion

        #region Public Members

        public string Title
        {
            get
            {
                if (FireworkManager.IsDirty)
                    return string.Format("{0} - {1} {2}", SOFTWARE_TITLE, FireworkManager.FireworkFullFileName, "*");
                else
                    return string.Format("{0} - {1}", SOFTWARE_TITLE, FireworkManager.FireworkFullFileName);
            }
        }

        /// <summary>
        /// Used to bind on UserControls
        /// </summary>
        public SoftwareConfiguration SoftwareConfiguration
        {
            get
            {
                return _configuration;
            }
            set
            {
                if (_configuration != value)
                {
                    _configuration = value;                    
                }
            }
        }

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

        public string DeviceConnectionInfo
        {
            set
            {
                if (_deviceConnectionInfo != value)
                {
                    _deviceConnectionInfo = value;
                    OnPropertyChanged();
                }
            }
            get
            {
                return _deviceConnectionInfo;
            }
        }

        #endregion

        #region Constructor 

        public MainWindowViewModel()
        {                       
            //Initialize Software configuration
            _configuration = new SoftwareConfiguration();

            //Initialize new firework
            _fireworkManager = new FireworkManager(_configuration);
            _fireworkManager.FireworkLoaded += FireworkManager_FireworkLoaded;
            _fireworkManager.FireworkDefinitionModified += FireworkManager_FireworkDefinitionModified;
            _fireworkManager.TransceiverInfoChanged += FireworkManager_TransceiverInfoChanged;

            //Trasnceiver already plugged?
            _fireworkManager.DiscoverDevice();
        }

        #endregion

        #region Event

        private void FireworkManager_TransceiverInfoChanged(object sender, TransceiverInfoEventArgs e)
        {
            DeviceConnectionInfo = e.TransceiverInfo;
        }

        private void FireworkManager_FireworkDefinitionModified(object sender, EventArgs e)
        {
            RefreshGUI();
        }

        private void FireworkManager_FireworkLoaded(object sender, EventArgs e)
        {
            //Refresh GUI
            RefreshGUI();
        }

        //TODO : Put this in base class
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Private Members 
        
        private void RefreshGUI()
        {
            OnPropertyChanged("FireworkManager");
            OnPropertyChanged("Title");
        }

        #endregion

        #region Public Members

        public void KeyPress(Key key)
        {
            try
            {
                switch (key)
                {
                    case Key.NumPad1:
                        _fireworkManager.LaunchRescueLine("1");
                        break;

                    case Key.NumPad2:
                        _fireworkManager.LaunchRescueLine("2");
                        break;

                    case Key.NumPad3:
                        _fireworkManager.LaunchRescueLine("3");
                        break;

                    case Key.NumPad4:
                        _fireworkManager.LaunchRescueLine("4");
                        break;

                    case Key.NumPad5:
                        _fireworkManager.LaunchRescueLine("5");
                        break;

                    case Key.NumPad6:
                        _fireworkManager.LaunchRescueLine("6");
                        break;

                    case Key.NumPad7:
                        _fireworkManager.LaunchRescueLine("7");
                        break;

                    case Key.NumPad8:
                        _fireworkManager.LaunchRescueLine("8");
                        break;

                    case Key.NumPad9:
                        _fireworkManager.LaunchRescueLine("9");
                        break;
                }
            }
            catch (CannotLaunchLineException ex)
            {
                DialogBoxHelper.ShowErrorMessage(ex.Message);
            }
        }
        
        public bool QuitApplication()
        {
            if (_fireworkManager.IsDirty)
            {
                MessageBoxResult result = MessageBox.Show("Le feu actuellement en cours d'édition comporte des modifications non enregistrées, voulez-vous continuer et perdre les modifications ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return false;
            }

            if (_fireworkManager.State == FireworkManagerState.FireworkRunning)
            {
                MessageBoxResult result = MessageBox.Show("Un feu d'artifice est actuellement tiré !! Voulez-vous stopper ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return false;
            }

            if (_fireworkManager.State == FireworkManagerState.FireworkRunning)
                _fireworkManager.Stop();

            //Stop serial properly
            _fireworkManager.StopDeviceManager();

            return true;
        }

        /// <summary>
        /// Make a new firework
        /// </summary>
        public void NewFirework()
        {
            //TODO : Vérifier qu'un feu n'est en cours ici

            if (_fireworkManager.IsDirty)
            {
                MessageBoxResult result = MessageBox.Show("Le feu actuellement en cours d'édition comporte des modifications non enregistrées, voulez-vous continuer et perdre les modifications ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return;
            }            

            _fireworkManager.LoadEmptyFirework();
        }

        /// <summary>
        /// Load Firework definition from Excel file
        /// </summary>
        public void LoadFireWork(bool fromExcelFile)
        {
            //TODO : Vérifier qu'un feu n'est en cours ici

            if (_fireworkManager.IsDirty)
            {
                MessageBoxResult result = MessageBox.Show("Le feu actuellement en cours d'édition comporte des modifications non enregistrées, voulez-vous continuer et perdre les modifications ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return;
            }

            try
            {

                OpenFileDialog ofd = new OpenFileDialog
                {
                    Multiselect = false
                };

                if (fromExcelFile)
                    ofd.Filter = "Fichier Excel (*.xlsx)|*.xlsx";
                else
                    ofd.Filter = "Fichier kQuatre (*.k4)|*.k4";


                if (ofd.ShowDialog() == true)
                {
                    //InitializeNewFirework();

                    if (fromExcelFile)
                        _fireworkManager.LoadFireworkFromExcel(ofd.FileName);
                    else
                        _fireworkManager.LoadFirework(ofd.FileName);

                    //OnPropertyChanged("FireworkManager");

                    //TODO : A revoir
                    //RefreshControlPanelUI(RefreshControlPanelEventType.FireworkLoadedEvent);
                }
            }
            catch (Exception e)
            {
                //TODO : Faire un helper pour présenter les erreurs de manière uniforme
                MessageBox.Show("Une erreur est apparue lors du chargement du fichier de définition du feu" + Environment.NewLine + "Erreur : " + e.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Saves firework with current name
        /// </summary>
        public void SaveFirework()
        {
            if (FireworkManager.IsNew)
            {
                SaveAsFirework();
            }
            else
            {
                _fireworkManager.SaveFirework();
                DialogBoxHelper.ShowInformationMessage("Enregistrement effectué avec succès");
            }
        }

        public void SaveAsFirework()
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Fichier kQuatre (*.k4)|*.k4",
                RestoreDirectory = true
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    _fireworkManager.SaveFirework(sfd.FileName);

                    MessageBox.Show("Le feu d'artifice a été sauvegardé avec succès", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Une erreur est apparue lors de la sauvegarde du feu d'artifice" + Environment.NewLine + "Erreur : " + e.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void OpenConfigurationWindow()
        {
            ConfigurationWindow window = new ConfigurationWindow(_configuration);
            window.ShowDialog();
        }

        public void OpenFireworkManagementWindow()
        {
            FireworkManagementWindow window = new FireworkManagementWindow(_fireworkManager, _configuration);
            window.ShowDialog();
        }

        public void OpenTestRadTimeline()
        {
            RadTimelineTest window = new RadTimelineTest(_fireworkManager);
            window.ShowDialog();
        }        

        #endregion

        #region Private Members


        #endregion
    }
}
