﻿using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.business.receptor;
using fr.guiet.kquatre.ui.command;
using fr.guiet.kquatre.ui.helpers;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace fr.guiet.kquatre.ui.viewmodel
{
    public class TestUserControlViewModel : INotifyPropertyChanged
    {
        #region Private property

        private FireworkManager _fireworkManager = null;
        private RelayCommand _startTestingReceptorCommand;
        private RelayCommand _stopTestingReceptorCommand;
        private RelayCommand _testConductiviteCommand;
        private Receptor _selectedTestReceptor = null;
        private Receptor _previousTestReceptor = null;
        private readonly Dispatcher _userControlDispatcher = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="receptors"></param>
        public TestUserControlViewModel(FireworkManager fireworkManager, Dispatcher userControlDispatcher)
        {
            _fireworkManager = fireworkManager;
            _fireworkManager.FireworkLoaded += FireworkManager_FireworkLoaded;
            _fireworkManager.TransceiverConnected += FireworkManager_TransceiverConnected;
            _fireworkManager.TransceiverDisconnected += FireworkManager_TransceiverDisconnected;

            _userControlDispatcher = userControlDispatcher;
        }

        #endregion

        #region Public Property        

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
                _fireworkManager = value;
            }
        }

        public RelayCommand TestConductiviteCommand
        {
            get
            {
                if (_testConductiviteCommand == null)
                {
                    _testConductiviteCommand = new RelayCommand(new Action<object>((ra) => TestConductivite(ra)), () => IsTestConductivityAllowed());                    
                }

                return _testConductiviteCommand;
            }
        }

        public RelayCommand StartTestingReceptorCommand
        {
            get
            {
                if (_startTestingReceptorCommand == null)
                {
                    _startTestingReceptorCommand = new RelayCommand(new Action(() => StartTestingReceptor()), () => IsStartPingTestAllowed());
                }

                return _startTestingReceptorCommand;
            }
        }

        public RelayCommand StopTestingReceptorCommand
        {
            get
            {
                if (_stopTestingReceptorCommand == null)
                {
                    _stopTestingReceptorCommand = new RelayCommand(new Action(() => StopTestingReceptor()), () => IsStopPingTestAllowed());
                }

                return _stopTestingReceptorCommand;
            }
        }

        #endregion

        #region Events

        private void FireworkManager_TransceiverConnected(object sender, EventArgs e)
        {
            RefreshGUI();
        }

        private void FireworkManager_TransceiverDisconnected(object sender, EventArgs e)
        {
            RefreshGUI();
        }

        private void FireworkManager_FireworkLoaded(object sender, EventArgs e)
        {
            RefreshGUI();
        }

        //TODO : Put this in base class
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Public methods

        public void OnReceptorSelectionChanged()
        {
            //Stop automatically reception test when user change receptor
            Receptor r = _previousTestReceptor;
            if (r != null && r.IsPingTestRunning)
            {
                r.StopPingTest();
            }

            RefreshGUI();
        }

        private void SelectedTestReceptor_PingTestStopped(object sender, EventArgs e)
        {
            RefreshGUI();
        }

        #endregion

        private void StopTestingReceptor()
        {
            _selectedTestReceptor.StopPingTest();
        }

        #region Private methods

        private void StartTestingReceptor()
        {
            _selectedTestReceptor.StartPingTest();

            //Trick not to subscribe 2 times to the event
            _selectedTestReceptor.PingTestStopped -= SelectedTestReceptor_PingTestStopped;
            _selectedTestReceptor.PingTestStopped += SelectedTestReceptor_PingTestStopped;

            RefreshGUI();
        }

        private void TestConductivite(object ra)
        {
            if (_selectedTestReceptor.IsCondTestRunning)
            {
                DialogBoxHelper.ShowWarningMessage("Un test est déjà en cours d'éxécution !");
                return;
            }

            if (ra is ReceptorAddress receptorAddress)
            {
                _selectedTestReceptor.TestConductivite(receptorAddress);
            }
        }
        
        private void RefreshGUI()
        {
            OnPropertyChanged("FireworkManager");

            //Careful must be called from UI Thread
            _userControlDispatcher.Invoke(() =>
            {
                if (_startTestingReceptorCommand != null)
                    _startTestingReceptorCommand.RaiseCanExecuteChanged();

                if (_stopTestingReceptorCommand != null)
                    _stopTestingReceptorCommand.RaiseCanExecuteChanged();

                if (_testConductiviteCommand != null)
                    _testConductiviteCommand.RaiseCanExecuteChanged();
            });
        }

        private bool IsTestConductivityAllowed()
        {
            if (_selectedTestReceptor != null)
            {
                return _selectedTestReceptor.IsCondTestAllowed();
            }

            return false;
        }

        private bool IsStartPingTestAllowed()
        {
            if (_selectedTestReceptor != null)
            {
                return _selectedTestReceptor.IsStartPingTestAllowed();
            }

            return false;
        }

        private bool IsStopPingTestAllowed()
        {
            if (_selectedTestReceptor != null)
            {
                return _selectedTestReceptor.IsStopPingAllowed();
            }

            return false;
        }

        #endregion

    }
}
