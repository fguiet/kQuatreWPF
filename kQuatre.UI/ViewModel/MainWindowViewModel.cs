﻿using Guiet.kQuatre.Business.Configuration;
using Guiet.kQuatre.Business.Transceiver;
using Guiet.kQuatre.Business.Firework;
using Guiet.kQuatre.Business.Gantt;
using Guiet.kQuatre.UI.Views;
using Infragistics.Controls.Schedules;
using Infragistics.Windows.DataPresenter;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Guiet.kQuatre.Business.Receptor;

namespace Guiet.kQuatre.UI.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Private Members        

        private Receptor _selectedTestReceptor = null;

        private DeviceManager _deviceManager = null;

        private FireworkManager _fireworkManager = null;

        private SoftwareConfiguration _configuration = null;

        private Dispatcher _dispatcher = null;

        private XamGantt _fireworkGantt = null;        

        private const string DEFAULT_NOT_TRANSCEIVER_CONNECTED_MESSAGE = "Aucun émetteur connecté...";
        private const string DEFAULT_TRANSCEIVER_CONNECTED_MESSAGE = "Emetteur connecté sur le port {0}";
        private const string DEFAULT_ERROR_TRANSCEIVER_WHEN_CONNECTING_MESSAGE = "Connexion avec l'émetteur impossible (erreur : {0}) - Essayer de relancer l'application";
        private const string DEFAULT_USB_CONNECTING_MESSAGE = "Connexion avec l'émetteur en cours...Etape(s) {0}/{1}";

        private string _deviceConnectionInfo = DEFAULT_NOT_TRANSCEIVER_CONNECTED_MESSAGE;

        #endregion

        #region Public Members 
        
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

        public MainWindowViewModel(XamGantt fireworkGantt)
        {
            _fireworkGantt = fireworkGantt;            

            _dispatcher = Dispatcher.CurrentDispatcher;

            //Software configuration
            _configuration = new SoftwareConfiguration();

            _deviceManager = new DeviceManager(_configuration.TransceiverAddress);
            _deviceManager.DeviceConnected += DeviceManager_DeviceConnected; ;
            _deviceManager.DeviceDisconnected += DeviceManager_DeviceDisconnected;
            _deviceManager.DeviceErrorWhenConnecting += DeviceManager_DeviceErrorWhenConnecting;
            _deviceManager.USBConnection += DeviceManager_USBConnection;

            //Device already plugged?
            _deviceManager.DiscoverDevice();
            
            FireworkManager = InstantiateNewFirework();

        }

        private FireworkManager InstantiateNewFirework()
        {
            if (_fireworkManager != null)
            {
                _fireworkManager.FireworkStateChanged -= FireworkManager_FireworkStateChanged;
                _fireworkManager = null;
            }

            FireworkManager fm = new FireworkManager(_configuration, _deviceManager);
            fm.FireworkStateChanged += FireworkManager_FireworkStateChanged;
            fm.LineStarted += FireworkManager_LineStarted;

            return fm;
        }

        private void FireworkManager_LineStarted(object sender, EventArgs e)
        {
            Line line = sender as Line;

            //Firework firework = line.FirstFirework;
            ProjectTask pt = new ProjectTask();

            _dispatcher.BeginInvoke((Action)(() =>
            {
                _fireworkGantt.ActiveRow = null;
                _fireworkGantt.SelectedRows.Clear();

                foreach (Firework firework in line.Fireworks)
                {
                    pt = GetProjectTask(_fireworkGantt.Project.RootTask.Tasks, firework.TaskModel);

                    if (pt != null)
                    {
                        GanttGridRow ggr = new GanttGridRow(pt);                        
                        _fireworkGantt.ActiveRow = ggr;                        
                        _fireworkGantt.SelectedRows.Add(ggr);                        
                    }
                }

                _fireworkGantt.ExecuteCommand(GanttCommandId.ScrollToTaskStart, pt);
            }));          
        }

        private ProjectTask GetProjectTask(IList<ProjectTask> listPt, TaskModel tm)
        {
            ProjectTask found = null;
            foreach (ProjectTask p in listPt)
            {
                if ((TaskModel)p.DataItem == tm)
                {
                    return p;
                }
                found = GetProjectTask(p.Tasks, tm);
                if (found != null) { break; }
            }
            return found;
        }

        /// <summary>
        /// Refresh GUI with firework change state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FireworkManager_FireworkStateChanged(object sender, FireworkStateChangedEventArgs e)
        {
            //Alternatives solutions
            //http://blog.benoitblanchon.fr/wpf-high-speed-mvvm/            

            if (e.PropertyName == "PercentComplete")
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    e.Firework.TaskModel.PercentComplete = e.Firework.PercentComplete;
                }));
            }


            if (e.PropertyName == "ColorPresentation")
            {
                e.Firework.TaskModel.TaskBrush = e.Firework.ColorPresentation;
                e.Firework.TaskModel.TaskBrush.Freeze();
            }
        }

        #endregion

        #region Event

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
        }

        private void DeviceManager_DeviceConnected(object sender, ConnectionEventArgs e)
        {
            DeviceConnectionInfo = string.Format(DEFAULT_TRANSCEIVER_CONNECTED_MESSAGE, e.Port);
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

        public void StopTestingReceptor()
        {
            _selectedTestReceptor.StopTest();
        }

        public void StartTestingReceptor()
        {
            _selectedTestReceptor.StartTest(_deviceManager.Transceiver);
        }

        /// <summary>
        /// Lets start firework!!
        /// </summary>
        public void StartFirework()
        {            
            //TODO : Sanity check
            _fireworkManager.Start(_deviceManager.Transceiver);
        }

        /// <summary>
        /// Load Firework definition from Excel file
        /// </summary>
        public void LoadFireWork(bool fromExcelFile)
        {
            //TODO : Check if another firework is not already loaded!

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
            }
        }

        public void SaveFirework()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Fichier kQuatre (*.k4)|*.k4";
            sfd.RestoreDirectory = true;

            if (sfd.ShowDialog() == true)
            {
                //TODO : Handle error here
                _fireworkManager.SaveFirework(sfd.FileName);

                MessageBox.Show("Le feu d'artifice a été sauvegardé avec succès", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void OpenConfigurationWindow()
        {
            ConfigurationWindow window = new ConfigurationWindow(_configuration);
            window.ShowDialog();
        }

        public void OpenFireworkManagementWindow()
        {
            FireworkManagementWindow window = new FireworkManagementWindow(_fireworkManager);
            window.ShowDialog();
        }

        public void OpenLineWindow(Line line)
        {
            LineWindow window = new LineWindow(_fireworkManager, line);
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

        #endregion
    }
}