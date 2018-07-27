using Guiet.kQuatre.Business.Configuration;
using Guiet.kQuatre.Business.Firework;
using Guiet.kQuatre.Business.Receptor;
using Guiet.kQuatre.Business.Transceiver;
using Guiet.kQuatre.UI.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Timeline;

namespace Guiet.kQuatre.UI.ViewModel
{

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Private Members  

        public enum RefreshControlPanelEventType
        {
            FireworkStateChangedEvent,
            FireworkLoadedEvent,
            FireworkModifiedEvent,
            DeviceConnectionChangedEvent,
            FireworkArmedEvent
        }

        private bool _automaticTimelineScroll = true;

        /// <summary>
        /// Timeline control
        /// </summary>
        private RadTimeline _fireworkTimeline = null;

        private Receptor _selectedTestReceptor = null;
        private Receptor _previousTestReceptor = null;

        private DeviceManager _deviceManager = null;

        private FireworkManager _fireworkManager = null;

        private SoftwareConfiguration _configuration = null;

        private Dispatcher _dispatcher = null;

        private const string DEFAULT_NOT_TRANSCEIVER_CONNECTED_MESSAGE = "Aucun émetteur connecté...";
        private const string DEFAULT_TRANSCEIVER_CONNECTED_MESSAGE = "Emetteur connecté sur le port {0}";
        private const string DEFAULT_ERROR_TRANSCEIVER_WHEN_CONNECTING_MESSAGE = "Connexion avec l'émetteur impossible (erreur : {0}) - Essayer de relancer l'application";
        private const string DEFAULT_USB_CONNECTING_MESSAGE = "Connexion avec l'émetteur en cours...Etape(s) {0}/{1}";

        private string _deviceConnectionInfo = DEFAULT_NOT_TRANSCEIVER_CONNECTED_MESSAGE;

        private const string SOFTWARE_TITLE = "kQuatre";

        //private string _windowTitle = string.Empty;

        private bool _isFireworkArmed = false;        

        #endregion

        #region Public Members

        public bool IsStopFireworkEnable
        {
            get
            {
                return (FireworkManager.State == FireworkManagerState.FireInProgress);
            }
        }        

        public bool IsFireFireworkEnable
        {
            get
            {
                if (_isFireworkArmed && FireworkManager.State == FireworkManagerState.Editing)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsArmingEnable
        {
            get
            {
                if (FireworkManager.State == FireworkManagerState.Editing && FireworkManager.IsSanityCheckOk) return true;
                else return false;
            }
        }

        public bool IsFireworkArmed
        {
            get
            {
                return _isFireworkArmed;
            }

            set
            {
                if (_isFireworkArmed != value)
                {
                    _isFireworkArmed = value;
                    OnPropertyChanged();
                }

            }
        }

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

        public bool AutomaticTimelineScroll
        {
            get
            {
                return _automaticTimelineScroll;
            }

            set
            {
                _automaticTimelineScroll = value;
            }

        }        

        public Receptor PreviousSelectedTestReceptor
        {
            get
            {
                return _previousTestReceptor;
            }
        }
        

        public Receptor SelectedTestReceptor
        {
            get
            {
                return _selectedTestReceptor;
            }
            set
            {
                if (_selectedTestReceptor != value)
                {
                    _previousTestReceptor = _selectedTestReceptor;
                    _selectedTestReceptor = value;
                    OnPropertyChanged();
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

        public void ActivateRedoFailedLine()
        {
            _fireworkManager.ActivateRedoFailedLine();
        }

        public MainWindowViewModel(RadTimeline fireworkTimeline)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            _fireworkTimeline = fireworkTimeline;

            //Software configuration
            _configuration = new SoftwareConfiguration();

            _deviceManager = new DeviceManager(_configuration);
            _deviceManager.DeviceConnected += DeviceManager_DeviceConnected; ;
            _deviceManager.DeviceDisconnected += DeviceManager_DeviceDisconnected;
            _deviceManager.DeviceErrorWhenConnecting += DeviceManager_DeviceErrorWhenConnecting;
            _deviceManager.USBConnection += DeviceManager_USBConnection;

            FireworkManager = InstantiateNewFirework();

            //Device already plugged?
            _deviceManager.DiscoverDevice();
        }

        private FireworkManager InstantiateNewFirework()
        {
            FireworkManager fm = new FireworkManager(_configuration, _deviceManager);

            fm.LineStarted += FireworkManager_LineStarted;
            fm.LineFailed += FireworkManager_LineFailed;
            fm.PropertyChanged += FireworkManager_PropertyChanged;
            fm.StateChanged += FireworkManager_StateChanged;
            fm.FireworkFinished += FireworkManager_FireworkFinished;

            OnPropertyChanged("Title");

            return fm;
        }

        /// <summary>
        /// Occurs when fireworks is finished
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FireworkManager_FireworkFinished(object sender, EventArgs e)
        {
            string message = "Le feu d'artifice est terminé !\r\n\r\n" + _fireworkManager.GetFireworkStatistics();
            ShowInformationMessage(message);
        }

        private void FireworkManager_StateChanged(object sender, EventArgs e)
        {
            RefreshControlPanelUI(RefreshControlPanelEventType.FireworkStateChangedEvent);
        }

        private void FireworkManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsDirty")
            {
                //Firework has changed so control panel must be reset
                RefreshControlPanelUI(RefreshControlPanelEventType.FireworkModifiedEvent);
            }
        }

        /// <summary>
        /// Refresh control panel when
        ///     - Firemanager state changed (start/stop)
        ///     - Load firework
        ///     - Device connect
        ///     - Device disconnect
        ///     - Arming changed
        /// </summary>
        public void RefreshControlPanelUI(RefreshControlPanelEventType eventType)
        {

            FireworkManager.SanityCheck();

            switch (eventType)
            {
                case RefreshControlPanelEventType.DeviceConnectionChangedEvent:
                    //Reset firework armed toggle
                    IsFireworkArmed = false;
                    OnPropertyChanged("IsArmingEnable");
                    OnPropertyChanged("IsFireFireworkEnable");
                    OnPropertyChanged("IsStopFireworkEnable");
                    break;

                case RefreshControlPanelEventType.FireworkArmedEvent:
                    OnPropertyChanged("IsArmingEnable");
                    OnPropertyChanged("IsFireFireworkEnable");
                    OnPropertyChanged("IsStopFireworkEnable");
                    break;

                case RefreshControlPanelEventType.FireworkLoadedEvent:
                    IsFireworkArmed = false;
                    OnPropertyChanged("IsArmingEnable");
                    OnPropertyChanged("IsFireFireworkEnable");
                    OnPropertyChanged("IsStopFireworkEnable");
                    //Update window title
                    OnPropertyChanged("Title");
                    break;

                case RefreshControlPanelEventType.FireworkModifiedEvent:
                    IsFireworkArmed = false;
                    OnPropertyChanged("IsArmingEnable");
                    OnPropertyChanged("IsFireFireworkEnable");
                    OnPropertyChanged("IsStopFireworkEnable");
                    //Update window title
                    OnPropertyChanged("Title");
                    break;

                case RefreshControlPanelEventType.FireworkStateChangedEvent:

                    if (FireworkManager.State == FireworkManagerState.Editing)
                    {
                        IsFireworkArmed = false;
                    }

                    OnPropertyChanged("IsFireFireworkEnable");
                    OnPropertyChanged("IsArmingEnable");
                    OnPropertyChanged("IsStopFireworkEnable");
                    break;
            }
        }

        private void ResetScrollBar()
        {
            //Horizontal
            _fireworkTimeline.VisiblePeriodStart = FireworkManager.DefaultPeriodStartUI;
            _fireworkTimeline.VisiblePeriodEnd = FireworkManager.DefaultPeriodEndUI;

            //Vertical
            TimelineScrollBar verticalSlider = _fireworkTimeline.FindChildByType<TimelineScrollBar>();

            if (verticalSlider != null)
            {
                var newStart = 0;
                var newEnd = verticalSlider.SelectionRange;

                verticalSlider.Selection = new SelectionRange<double>(newStart, newEnd);
            }
        }

        /// <summary>
        /// Compute visible timeline automatically (based on current line started)
        /// </summary>
        /// <param name="line"></param>
        private void ComputeVisiblePeriod(Business.Firework.Line line)
        {
            if (!_automaticTimelineScroll) return;

            TimeSpan lineIgnition = line.Ignition;

            //Put this into try/catch, just in case...
            //Don't want program to stop in the middle of firework
            try
            {

                DateTime visiblePeriodStart = DateTime.Now.Date.Add(line.Ignition).Subtract(new TimeSpan(0, 0, 20));
                DateTime visiblePeriodEnd = DateTime.Now.Date.Add(line.Ignition).Add(new TimeSpan(0, 0, 40));

                //Horizontal part

                //Change visible port view only if new visible period start - 20 s > period start ui
                if (visiblePeriodStart.CompareTo(_fireworkManager.PeriodStartUI) > 0)
                {
                    _dispatcher.BeginInvoke((Action)(() =>
                    {
                        _fireworkTimeline.VisiblePeriod = new Telerik.Windows.Controls.SelectionRange<DateTime>(visiblePeriodStart, visiblePeriodEnd);
                    }));
                }

                _dispatcher.BeginInvoke((Action)(() =>
                {
                    TimelineScrollBar verticalSlider = _fireworkTimeline.FindChildByType<TimelineScrollBar>();
                    //Vertical part
                    if (verticalSlider != null)
                    {
                        int nbOfElementVisiblePerRange = Convert.ToInt32(Math.Truncate((_fireworkManager.AllFireworks.Count * verticalSlider.SelectionRange)));
                        double range = (line.Fireworks[0].RadRowIndex * verticalSlider.SelectionRange / nbOfElementVisiblePerRange);

                        //End?
                        if (range + verticalSlider.SelectionRange - (verticalSlider.SelectionRange / 4) > 1)
                        {
                            verticalSlider.Selection = new SelectionRange<double>(1 - verticalSlider.SelectionRange, 1);
                            return;
                        }

                        //Mid screen reached?
                        if (range > (verticalSlider.SelectionRange / 4))
                        {
                            var newStart = range - (verticalSlider.SelectionRange / 4);
                            var newEnd = range + verticalSlider.SelectionRange - (verticalSlider.SelectionRange / 4);
                            verticalSlider.Selection = new SelectionRange<double>(newStart, newEnd);
                        }
                    }
                }));
            }
            catch
            {
                //NLog here
            }
        }

        #endregion

        #region Event

        private void FireworkManager_LineFailed(object sender, EventArgs e)
        {

            Business.Firework.Line line = sender as Business.Firework.Line;

            ComputeVisiblePeriod(line);
        }

        private void FireworkManager_LineStarted(object sender, EventArgs e)
        {
            Business.Firework.Line line = sender as Business.Firework.Line;

            ComputeVisiblePeriod(line);
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

        #region Private Members        

        private void DeviceManager_USBConnection(object sender, USBConnectionEventArgs e)
        {
            DeviceConnectionInfo = string.Format(DEFAULT_USB_CONNECTING_MESSAGE, e.ElapsedSecond, e.UsbReady);
        }

        private void DeviceManager_DeviceDisconnected(object sender, EventArgs e)
        {
            DeviceConnectionInfo = DEFAULT_NOT_TRANSCEIVER_CONNECTED_MESSAGE;

            //TODO : What happen if a firework is launched?
            //TODO : Test this!!
            if (_fireworkManager.State == FireworkManagerState.FireInProgress)
            {
                _fireworkManager.Stop();
            }

            //Device is not connected anymore...so let's refresh control bar
            RefreshControlPanelUI(RefreshControlPanelEventType.DeviceConnectionChangedEvent);
        }

        private void DeviceManager_DeviceConnected(object sender, ConnectionEventArgs e)
        {
            DeviceConnectionInfo = string.Format(DEFAULT_TRANSCEIVER_CONNECTED_MESSAGE, e.Port);

            //Device is connected...so let's refresh control bar
            RefreshControlPanelUI(RefreshControlPanelEventType.DeviceConnectionChangedEvent);
        }

        private void DeviceManager_DeviceErrorWhenConnecting(object sender, ConnectionErrorEventArgs e)
        {
            if (e.Exception != null)
            {
                DeviceConnectionInfo = string.Format(DEFAULT_ERROR_TRANSCEIVER_WHEN_CONNECTING_MESSAGE, e.Exception.Message);
            }
        }

        #endregion

        #region Public Members

        public void ResetUI()
        {
            ResetScrollBar();
            FireworkManager.Reset();
        }

        public bool QuitApplication()
        {
            if (FireworkManager.IsDirty)
            {
                MessageBoxResult result = MessageBox.Show("Le feu actuellement en cours d'édition comporte des modifications non enregistrées, voulez-vous continuer et perdre les modifications ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return false;
            }

            if (FireworkManager.State == FireworkManagerState.FireInProgress)
            {
                MessageBoxResult result = MessageBox.Show("Un feu d'artifice est actuellement tiré !! Voulez-vous stopper ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return false;
            }

            if (FireworkManager.State == FireworkManagerState.FireInProgress)
                FireworkManager.Stop();

            if (_deviceManager != null)
                _deviceManager.Close();

            return true;
        }

        public void StopTestingReceptor()
        {
            _selectedTestReceptor.StopTest();
        }

        public void StartTestingReceptor()
        {
            _selectedTestReceptor.StartTest();
        }

        /// <summary>
        /// Lets stop firework!!
        /// </summary>
        public void StopFirework()
        {
            MessageBoxResult result = MessageBox.Show("Attention, vous êtes sur le point d'arrêter le feu d'artifice, voulez-vous continuer ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            _fireworkManager.Stop();
        }

        /// <summary>
        /// Lets start firework!!
        /// </summary>
        public void StartFirework()
        {
            //Reset scroll so user can see begin of firework
            ResetScrollBar();

            //TODO : Sanity check
            _fireworkManager.Start();
        }

        /// <summary>
        /// Make a new firework
        /// </summary>
        public void NewFirework()
        {

            //TODO : Vérifier qu'un feu n'est en cours ici

            if (FireworkManager.IsDirty)
            {
                MessageBoxResult result = MessageBox.Show("Le feu actuellement en cours d'édition comporte des modifications non enregistrées, voulez-vous continuer et perdre les modifications ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return;
            }

            //InstantiateNewFirework();
            FireworkManager fm = InstantiateNewFirework();
            this.FireworkManager = fm;

        }

        /// <summary>
        /// Load Firework definition from Excel file
        /// </summary>
        public void LoadFireWork(bool fromExcelFile)
        {
            //TODO : Vérifier qu'un feu n'est en cours ici

            if (FireworkManager.IsDirty)
            {
                MessageBoxResult result = MessageBox.Show("Le feu actuellement en cours d'édition comporte des modifications non enregistrées, voulez-vous continuer et perdre les modifications ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return;
            }

            try
            {

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Multiselect = false;

                if (fromExcelFile)
                    ofd.Filter = "Fichier Excel (*.xlsx)|*.xlsx";
                else
                    ofd.Filter = "Fichier kQuatre (*.k4)|*.k4";


                if (ofd.ShowDialog() == true)
                {
                    FireworkManager fm = InstantiateNewFirework();

                    if (fromExcelFile)
                        fm.LoadFireworkFromExcel(ofd.FileName);
                    else
                        fm.LoadFirework(ofd.FileName);

                    this.FireworkManager = fm;

                    RefreshControlPanelUI(RefreshControlPanelEventType.FireworkLoadedEvent);
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
                ShowInformationMessage("Enregistrement effectué avec succès");
            }
        }

        /// <summary>
        /// Refresh fire tab ui when selected
        /// </summary>
        public void RefreshFireTabUI()
        {
            //Object FireworkManager may have changed so GUI must be updated
            OnPropertyChanged("FireworkManager");
        }

        public void SaveAsFirework()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Fichier kQuatre (*.k4)|*.k4";
            sfd.RestoreDirectory = true;

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

        public void OpenSanityCheckWindow()
        {
            SanityCheckWindow window = new SanityCheckWindow(_fireworkManager);
            window.ShowDialog();
        }

        public void OpenConfigurationWindow()
        {
            ConfigurationWindow window = new ConfigurationWindow(_configuration);
            window.ShowDialog();
        }

        public void OpenFireworkManagementWindow(Line line)
        {
            FireworkManagementWindow window = new FireworkManagementWindow(_fireworkManager, _configuration, line);
            window.ShowDialog();
        }

        public void OpenFireworkManagementWindow()
        {
            FireworkManagementWindow window = new FireworkManagementWindow(_fireworkManager, _configuration);
            window.ShowDialog();
        }

        public void OpenLineWindow(Line line)
        {
            LineWindow window = new LineWindow(_fireworkManager, line);
            window.ShowDialog();
        }

        public void OpenTestRadTimeline()
        {
            RadTimelineTest window = new RadTimelineTest(_fireworkManager);
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
                ShowWarningMessage("Veuillez sélectionner une ligne à supprimer.");
            }
            else
            {
                string message = string.Format("Validez-vous la suppression de la ligne {0} ?.{1}Attention, les lignes seront réordonnées.", line.Number, Environment.NewLine);

                if (line.Fireworks.Count > 0)
                {
                    message = string.Format("La ligne {0} est associée à des artifices. Les associations seront supprimées et les lignes seront réordonnées.{1}Voulez-vous continuer ?", line.Number, Environment.NewLine);
                }

                if (ShowQuestionMessage(message) == MessageBoxResult.Yes)
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

        private MessageBoxResult ShowQuestionMessage(string message)
        {
            return MessageBox.Show(message, "Question", MessageBoxButton.YesNo, MessageBoxImage.Question);
        }

        private void ShowWarningMessage(string message)
        {
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
        }

        private void ShowInformationMessage(string message)
        {
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        #endregion
    }
}
