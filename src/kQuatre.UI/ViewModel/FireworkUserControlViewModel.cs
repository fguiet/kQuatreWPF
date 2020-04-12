using fr.guiet.kquatre.business.exceptions;
using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.ui.command;
using fr.guiet.kquatre.ui.helpers;
using fr.guiet.kquatre.ui.timeline;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.TimeBar;
using Telerik.Windows.Controls.Timeline;

namespace fr.guiet.kquatre.ui.viewmodel
{
    public class FireworkUserControlViewModel : INotifyPropertyChanged
    {
        #region Private Members

        private readonly Dispatcher _userControlDispatcher = null;
        private RelayCommand _startFireworkCommand = null;
        private RelayCommand _stopFireworkCommand = null;
        private RelayCommand _armFireworkCommand = null;

        private bool _automaticTimelineScroll = true;

        /// <summary>
        /// Timeline control
        /// </summary>
        private readonly RadTimeline _fireworkTimeline = null;

        private bool _isFireworkArmed = false;

        //private Dispatcher _dispatcher = null;

        private FireworkManager _fireworkManager = null;

        #endregion

        #region Public Members

        public RelayCommand StartFireworkCommand
        {
            get
            {
                if (_startFireworkCommand == null)
                {
                    _startFireworkCommand = new RelayCommand(new Action(() => StartFirework()), () => IsStartFireworkAllowed());
                }

                return _startFireworkCommand;
            }
        }

        public RelayCommand StopFireworkCommand
        {
            get
            {
                if (_stopFireworkCommand == null)
                {
                    _stopFireworkCommand = new RelayCommand(new Action(() => StopFirework()), () => IsStopFireworkAllowed());
                }

                return _stopFireworkCommand;
            }
        }

        public RelayCommand ArmFireworkCommand
        {
            get
            {
                if (_armFireworkCommand == null)
                {
                    _armFireworkCommand = new RelayCommand(new Action(() => ArmFirework()), () => IsArmFireworkAllowed());
                }

                return _armFireworkCommand;
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

        public FireworkUserControlViewModel(FireworkManager fireworkManager, RadTimeline fireworkTimeline, Dispatcher userControlDispatcher)
        {
            _fireworkTimeline = fireworkTimeline;

            MinuteInterval mi = new MinuteInterval
            {
                FormatterProvider = new MinuteIntervalFormatter()
            };

            SecondInterval si = new SecondInterval
            {
                FormatterProvider = new SecondIntervalFormatter()
            };

            _fireworkTimeline.Intervals.Add(mi);
            _fireworkTimeline.Intervals.Add(si);

            _fireworkManager = fireworkManager;
            _fireworkManager.FireworkLoaded += FireworkManager_FireworkLoaded;
            _fireworkManager.LineStarted += FireworkManager_LineStarted;
            _fireworkManager.LineFailed += FireworkManager_LineFailed;
            _fireworkManager.FireworkFinished += FireworkManager_FireworkFinished;
            _fireworkManager.FireworkStarted += FireworkManager_FireworkStarted;
            _fireworkManager.TransceiverDisconnected += FireworkManager_TransceiverDisconnected;
            _fireworkManager.TransceiverConnected += FireworkManager_TransceiverConnected;

            _userControlDispatcher = userControlDispatcher;
        }

        #endregion

        #region Events

        private void FireworkManager_TransceiverConnected(object sender, EventArgs e)
        {
            //Refresh firework UI
            RefreshControlPanelUI();
        }

        private void FireworkManager_TransceiverDisconnected(object sender, EventArgs e)
        {
            //Reset firework UI
            ResetUserControlUI();
        }

        /// <summary>
        /// New firework? refresh gui binding please
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FireworkManager_FireworkLoaded(object sender, EventArgs e)
        {
            //Reset firework UI
            ResetUserControlUI();            
        }

        private void FireworkManager_FireworkStarted(object sender, EventArgs e)
        {
            //Refresh control panel possibility
            RefreshControlPanelUI();
        }

        //private void FireworkManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //TODO : A revoir
        /*if (e.PropertyName == "IsDirty")
        {

            //Firework has changed so control panel must be reset
            RefreshControlPanelUI(RefreshControlPanelEventType.FireworkModifiedEvent);
        }*/
        //  }

        /// <summary>
        /// Occurs when fireworks is finished
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FireworkManager_FireworkFinished(object sender, EventArgs e)
        {
            //Refresh control panel possibility
            RefreshControlPanelUI();

            string message = "Le feu d'artifice est terminé !\r\n\r\n" + _fireworkManager.GetFireworkStatistics();
            DialogBoxHelper.ShowInformationMessage(message);
        }

        /*private void FireworkManager_StateChanged(object sender, EventArgs e)
        {
            RefreshControlPanelUI(RefreshControlPanelEventType.FireworkStateChangedEvent);
        }*/

        private void FireworkManager_LineFailed(object sender, EventArgs e)
        {
            Line line = sender as Line;

            ComputeVisiblePeriod(line);
        }

        private void FireworkManager_LineStarted(object sender, EventArgs e)
        {
            Line line = sender as Line;

            ComputeVisiblePeriod(line);
        }

        //TODO : Put this in base class
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Refresh fire tab ui when selected
        /// </summary>
        /*public void RefreshFireTabUI()
        {
            //Object FireworkManager may have changed so GUI must be updated
            OnPropertyChanged("FireworkManager");
        }*/

        /// <summary>
        /// Use to reset control panel when navigating from on panel to another
        /// or when firework is loaded....
        /// </summary>
        public void ResetUserControlUI()
        {
            IsFireworkArmed = false;
            RefreshControlPanelUI();
            RefreshFireworkUI();
        }

        /// <summary>
        /// Refresh Control Panel UI
        /// </summary>
        private void RefreshControlPanelUI()
        {
            //Careful must be called from UI Thread
            _userControlDispatcher.Invoke(() =>
            {
                _armFireworkCommand.RaiseCanExecuteChanged();
                _startFireworkCommand.RaiseCanExecuteChanged();
                _stopFireworkCommand.RaiseCanExecuteChanged();
            });
        }

        #endregion

        #region Public Members

        /*public void ResetFireworkUI()
        {
            ResetScrollBar();
            FireworkManager.Restart();
        }*/

        /// <summary>
        /// Lets stop firework!!
        /// </summary>
        public void StopFirework()
        {
            MessageBoxResult result = MessageBox.Show("Attention, vous êtes sur le point d'arrêter le feu d'artifice, voulez-vous continuer ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            _fireworkManager.Stop();
        }       

        public void LaunchFailedLine(string lineNumber)
        {
            try
            {
                _fireworkManager.LaunchLine(lineNumber);
            }
            catch (CannotLaunchLineException ex)
            {
                DialogBoxHelper.ShowErrorMessage(ex.Message);
            }
        }

        #endregion

        #region Private Members 

        private void ArmFirework()
        {
            //User turn ON ARM BUTTON
            if (IsFireworkArmed)
            {
                MessageBoxResult result = MessageBox.Show("Vous êtes sur le point d'armer le feu d'artifice ! Voulez-vous continuer ?", "Information", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if (result == MessageBoxResult.Yes)
                {
                    //Reset view
                    ResetScrollBar();

                    //Enable / disable button
                    RefreshControlPanelUI();
                }
                else
                {
                    //Reset control panel to initial state
                    ResetUserControlUI();
                }
            }

            //User turn OFF ARM BUTTON
            if (!IsFireworkArmed)
            {
                //Enable / disable button
                RefreshControlPanelUI();
            }
        }

        /// <summary>
        /// Lets start firework!!
        /// </summary>
        private void StartFirework()
        {
            //In case of user has stop and then want to restart firework
            //_fireworkManager.Restart();

            //Reset scroll so user can see begin of firework
            ResetScrollBar();

            //No need to do sanity check...it has been done before            
            _fireworkManager.Start();

            //Refresh Control panel, user cannot unarm for exemple
            RefreshControlPanelUI();
        }

        private bool IsArmFireworkAllowed()
        {
            //sanity check ok (check if transceiver is connected)
            if (_fireworkManager.State == FireworkManagerState.FireworkStopped &&
                _fireworkManager.IsSanityCheckOk)
            {
                return true;
            }

            return false;
        }

        private bool IsStopFireworkAllowed()
        {
            if (_fireworkManager.State == FireworkManagerState.FireworkRunning)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsStartFireworkAllowed()
        {
            //Bouton armé ON, sanity check ok (check if transceiver is connected), non lancé

            if (IsFireworkArmed
                && _fireworkManager.State == FireworkManagerState.FireworkStopped
                && _fireworkManager.IsSanityCheckOk)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void RefreshFireworkUI()
        {
            OnPropertyChanged("FireworkManager");
        }

        /// <summary>
        /// Compute visible timeline automatically (based on current line started)
        /// </summary>
        /// <param name="line"></param>
        private void ComputeVisiblePeriod(Line line)
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
                    _userControlDispatcher.BeginInvoke((Action)(() =>
                    {
                        _fireworkTimeline.VisiblePeriod = new Telerik.Windows.Controls.SelectionRange<DateTime>(visiblePeriodStart, visiblePeriodEnd);
                    }));
                }

                _userControlDispatcher.BeginInvoke((Action)(() =>
                {
                    TimelineScrollBar verticalSlider = _fireworkTimeline.FindChildByType<TimelineScrollBar>();
                    //Vertical part
                    if (verticalSlider != null)
                    {
                        int nbOfElementVisiblePerRange = Convert.ToInt32(Math.Truncate((_fireworkManager.AllActiveFireworks.Count * verticalSlider.SelectionRange)));
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

        #endregion
    }
}
