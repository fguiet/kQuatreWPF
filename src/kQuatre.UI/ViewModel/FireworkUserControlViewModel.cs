using fr.guiet.kquatre.business.exceptions;
using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.ui.command;
using fr.guiet.kquatre.ui.helpers;
using fr.guiet.kquatre.ui.timeline;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private RelayCommand _centerTimeLineCommand = null;
        
        private Style _timeLineStyle = new Style
        {
            TargetType = typeof(Border)
        };

        private Style _currentTimeLineStyle = new Style
        {
            TargetType = typeof(Border)
        };

        //private FluentResourceExtension _primaryBackgroundBrush = null;
        //private FluentResourceExtension _alternativeBrush = null;        
        //private FluentResourceExtension _currentTimeLineColor = null;

        private double? _currentVerticalRange = null;

        private bool _automaticTimelineScroll = true;

        private bool _playSoundTrack = false;

        private int _oldIndex = -1;

        private double? _lastCentereredVerticalSliderFireworkPositionStart = null;

        private Line _line = null;

        /// <summary>
        /// Timeline control
        /// </summary>
        private RadTimeline _fireworkTimeline = null;

        private bool _isFireworkArmed = false;

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

        public RelayCommand CenterTimeLineCommand
        {
            get
            {
                if (_centerTimeLineCommand == null)
                {
                    _centerTimeLineCommand = new RelayCommand(new Action(() => CenterTimeLine()), () => IsCenterTimeLineAllowed());
                }

                return _centerTimeLineCommand;
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

        public bool PlaySoundTrack
        {
            get
            {
                return _playSoundTrack;
            }

            set
            {
                _playSoundTrack = value;
                OnPropertyChanged();
            }

        }

        public bool IsPlaySoundTrackEnabled
        {
            get
            {
                return _fireworkManager.HasSoundTrackToPlay && _fireworkManager.State == FireworkManagerState.FireworkStopped;
            }
        }

        public Uri SanityCheckStatusImagePath
        {
            get
            {
                if (_fireworkManager.IsSanityCheckOk)
                {
                    return new Uri("/kQuatre;component/Resources/valid.png", UriKind.RelativeOrAbsolute);
                }
                else
                {
                    return new Uri("/kQuatre;component/Resources/notvalid.png", UriKind.RelativeOrAbsolute);
                }
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

        //TODO: Remove testing purpose
        /*public string VisiblePeriodStart
        {
            get
            {
                return _fireworkTimeline.VisiblePeriodStart.ToString("T");
            }
        }*/

        #endregion

        #region Constructor

        public FireworkUserControlViewModel(FireworkManager fireworkManager, Dispatcher userControlDispatcher)
        {
            _fireworkManager = fireworkManager;

            _fireworkManager.FireworkLoaded += FireworkManager_FireworkLoaded;
            _fireworkManager.LineStarted += FireworkManager_LineStarted;
            _fireworkManager.LineFailed += FireworkManager_LineFailed;
            _fireworkManager.FireworkFinished += FireworkManager_FireworkFinished;
            _fireworkManager.FireworkStarted += FireworkManager_FireworkStarted;
            _fireworkManager.TransceiverDisconnected += FireworkManager_TransceiverDisconnected;
            _fireworkManager.TransceiverConnected += FireworkManager_TransceiverConnected;
            _fireworkManager.TimerElapsed += FireworkManager_TimerElapsed;

            _userControlDispatcher = userControlDispatcher;

            /*FluentResourceKey frk1 = (FluentResourceKey)FluentResourceKey.PrimaryBackgroundBrush;
            _primaryBackgroundBrush = new FluentResourceExtension();
            _primaryBackgroundBrush.ResourceKey = frk1;

            FluentResourceKey frk2 = (FluentResourceKey)FluentResourceKey.AlternativeBrush;
            _alternativeBrush = new FluentResourceExtension();
            _alternativeBrush.ResourceKey = frk2;   */            
                        
            _timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.White));

            FluentResourceKey frk3 = (FluentResourceKey)FluentResourceKey.AccentBrush;
            FluentResourceExtension currentTimeLineColor = new FluentResourceExtension();
            currentTimeLineColor.ResourceKey = frk3;            

            _currentTimeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, currentTimeLineColor));            
        }

        #endregion

        #region Events       

        private void FireworkManager_TimerElapsed(object sender, EventArgs e)
        {
            //Set background color of current timeline
            _userControlDispatcher.Invoke(() =>
            {
                try
                {

                    //Getting some info                    
                    //_fireworkTimeline.VisiblePeriodStart => can be fraction of time ! so need to Math.Floor
                    int visiblePeriodStart = Convert.ToInt32(Math.Floor(_fireworkTimeline.VisiblePeriodStart.TimeOfDay.TotalSeconds));
                    int visiblePeriodEnd = Convert.ToInt32(Math.Floor(_fireworkTimeline.VisiblePeriodEnd.TimeOfDay.TotalSeconds));
                    int fireworkElapsedTime = Convert.ToInt32(Math.Floor(_fireworkManager.ElapsedTime.TotalSeconds));

                    if (fireworkElapsedTime >= visiblePeriodStart && fireworkElapsedTime <= visiblePeriodEnd)
                    {
                        //Get number of seconds from visiblePeriodStart and fireworkElapsedTime
                        int nbOfSeconds = fireworkElapsedTime - visiblePeriodStart;

                        if (_oldIndex == -1)
                        {
                            _oldIndex = nbOfSeconds;
                        }

                        //May happen sometime ... dunno why...
                        if (nbOfSeconds - _oldIndex > 1)
                        {
                            //TODO : 2021/07/14 - Investigate why...
                            //2021/07/27 : It was because a part of this code was very time-consumming => 
                            //PropertyInfo pi = borderType.GetProperty("Value");

                            //object obj = pi.GetValue(elementStyle.Setters[0], null);

                            //Maybe not necessary anymore...but we can keep it anyway just in case...

                            //To avoid UI glitches...
                            RefreshTimelineUI();
                        }

                        _oldIndex = nbOfSeconds;

                        //What is visible on the screen at this time !
                        //One TimelineStripLineControl per second...
                        List<TimelineStripLineControl> timeLineStripLineControlList = _fireworkTimeline.ChildrenOfType<TimelineStripLineControl>().ToList();

                        //Revert old color
                        if (nbOfSeconds > 0)
                        {
                            //May occur during screen resize (number of interval may be very low...)
                            if (timeLineStripLineControlList != null && nbOfSeconds < timeLineStripLineControlList.Count)
                            {
                                /*Style timeLineStyle = new Style
                                {
                                    TargetType = typeof(Border)
                                };*/

                                /*if ((nbOfSeconds - 1) % 2 == 0)
                                {
                                    timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _primaryBackgroundBrush));
                                    
                                }
                                else
                                {
                                    timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _alternativeBrush));
                                }*/

                                //timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.White));

                                timeLineStripLineControlList[nbOfSeconds - 1].ElementStyle = _timeLineStyle;

                                //Retrieve current element style
                                /*Style elementStyle = timeLineStripLineControlList[nbOfSeconds].ElementStyle;

                                //Retrieve Setter of type System.Windows.Controls.Border
                                Type borderType = elementStyle.Setters[0].GetType();

                                //Get Value Property
                                PropertyInfo pi = borderType.GetProperty("Value");

                                object obj = pi.GetValue(elementStyle.Setters[0], null);

                                FluentResourceExtension fre = (FluentResourceExtension)obj;

                                Style timeLineStyle = new Style
                                {
                                    TargetType = typeof(Border)
                                };

                                if (fre.ResourceKey == _alternativeBrush.ResourceKey)
                                {
                                    timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _primaryBackgroundBrush));
                                }
                                else
                                {
                                    timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _alternativeBrush));
                                }

                                timeLineStripLineControlList[nbOfSeconds - 1].ElementStyle = timeLineStyle;*/
                            }
                        }

                        //May occur during screen resize (number of interval may be very low...)
                        if (timeLineStripLineControlList != null && nbOfSeconds < timeLineStripLineControlList.Count)
                        {                           
                            timeLineStripLineControlList[nbOfSeconds].ElementStyle = _currentTimeLineStyle;
                        }

                        //When next firework is later and out ot visible screen, let's move the screen on
                        if (nbOfSeconds >= timeLineStripLineControlList.Count - 2) //
                        {
                            if (_line != null)
                            {
                                DateTime visiblePeriodStart1 = DateTime.Now.Date.Add(_fireworkManager.ElapsedTime).Subtract(new TimeSpan(0, 0, 20));
                                DateTime visiblePeriodEnd1 = DateTime.Now.Date.Add(_fireworkManager.ElapsedTime).Add(new TimeSpan(0, 0, 40));

                                ComputeVisiblePeriod(visiblePeriodStart1, visiblePeriodEnd1, _line.Fireworks[0].RadRowIndex, false);
                            }

                        }
                    }
                }
                catch
                {

                }
            });

            //if (_timeLineUIRefreshing) return;

            //if (_computeVisiblePeriodComputing) return;

            /*List<TimelineStripLineControl> timeLineStripLineControlList = _fireworkTimeline.ChildrenOfType<TimelineStripLineControl>().ToList();


            Style t = timeLineStripLineControlList[0].ElementStyle;
            Style s = timeLineStripLineControlList[0].NormalStyle;
            Style p = timeLineStripLineControlList[0].AlternateStyle;

            Style t1 = timeLineStripLineControlList[1].ElementStyle;
            Style s1 = timeLineStripLineControlList[1].NormalStyle;
            Style p1 = timeLineStripLineControlList[1].AlternateStyle;

            Type toto = t.Setters[0].GetType();
            PropertyInfo pi = toto.GetProperty("Value");
            object obj = pi.GetValue(t.Setters[0], null);

            Telerik.Windows.Controls.FluentResourceExtension u = (Telerik.Windows.Controls.FluentResourceExtension)obj;*/

            //Return the Visible Period Start (actually displayed on the screen)
            //DateTime visiblePeriodStart =  _fireworkTimeline.VisiblePeriodStart;

            //Gets the total number of second of the begining of the timeline actually display at the moment
            //TimeSpan visiblePeriodStartSeconds = _fireworkTimeline.VisiblePeriodStart.Date.Second; //_fireworkTimeline.VisiblePeriodStart - _fireworkTimeline.VisiblePeriodStart.Date;

            /*  TimeSpan visiblePeriodStart = _fireworkTimeline.VisiblePeriodStart.Date.TimeOfDay;

              //Gets the total number of second of the end of the timeline actually display at the moment
              TimeSpan visiblePeriodEndSeconds = _fireworkTimeline.VisiblePeriodEnd - _fireworkTimeline.VisiblePeriodEnd.Date;

              //Current firework elapsed time in seconds
              //int currentFireworkSecond = _fireworkManager.ElapsedTimeSeconds;



              //We check that the visible part of the timeline graph correspond to the actual moment of the firework !
              if (visiblePeriodStartSeconds.TotalSeconds <= currentFireworkSecond && visiblePeriodEndSeconds.TotalSeconds >= currentFireworkSecond)
              {
                  //What is visible on the screen at this time !
                  //One TimelineStripLineControl per second...
                  List<TimelineStripLineControl> timeLineStripLineControlList = _fireworkTimeline.ChildrenOfType<TimelineStripLineControl>().ToList();

                  if (timeLineStripLineControlList != null && timeLineStripLineControlList.Count > 0)
                  {
                      int index = _fireworkManager.ElapsedTimeSeconds - Convert.ToInt32(Math.Floor(visiblePeriodStartSeconds.TotalSeconds));


                      if (_oldIndex == -1)
                      {
                          _oldIndex = index;
                      }

                      if (index - _oldIndex > 1)
                      {
                          //string bug = "fkfk";
                          //_currentTimeLineColor = Brushes.Red;

                          // index--;

                          //TODO : 2021/07/14 - Investigate why...

                          //To avoid UI glitches...
                          RefreshTimelineUI();
                      }

                      _oldIndex = index;

                      //Avoid IndexOutOfRangeException
                      if (index < timeLineStripLineControlList.Count && index >= 0)
                      {
                          if (timeLineStripLineControlList[index] != null)
                          {
                              //Retrieve current element style
                              Style elementStyle = timeLineStripLineControlList[index].ElementStyle;

                              //Retrieve Setter of type System.Windows.Controls.Border
                              Type borderType = elementStyle.Setters[0].GetType();

                              //Get Value Property
                              PropertyInfo pi = borderType.GetProperty("Value");

                              object obj = pi.GetValue(elementStyle.Setters[0], null);

                              FluentResourceExtension u = (FluentResourceExtension)obj;

                              //Restore old color value
                              if (_oldCurrentTimeLine != null)
                              {
                                  Style oldTimeLineStyle = new Style
                                  {
                                      TargetType = typeof(System.Windows.Controls.Border)
                                  };

                                  oldTimeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _oldCurrentTimeLine));

                                  //timeLineStripLineControlList[index - (index - _oldIndex)].ElementStyle = oldTimeLineStyle;
                                  timeLineStripLineControlList[index - 1].ElementStyle = oldTimeLineStyle;
                              }

                              //Save old color value
                              if (u.ResourceKey.ToString() == _alternativeBrush.ResourceKey.ToString())
                              {
                                  _oldCurrentTimeLine = _alternativeBrush;
                                  //oldTimeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _primaryBackgroundBrush));
                              }
                              else
                              {
                                  _oldCurrentTimeLine = _primaryBackgroundBrush;
                                  //oldTimeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _alternativeBrush));
                              }

                              //Reset old timeline background color value
                              //if (index - 1 >= 0)
                              //{
                              /*Style oldTimeLineStyle = new Style
                              {
                                  TargetType = typeof(System.Windows.Controls.Border)
                              };*/



            //timeLineStripLineControlList[index - 1].ElementStyle = oldTimeLineStyle;
            //}


            /*              Style currentTimeLineStyle = new Style
                          {
                              TargetType = typeof(System.Windows.Controls.Border)
                          };

                          currentTimeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _currentTimeLineColor));

                          timeLineStripLineControlList[index].ElementStyle = currentTimeLineStyle;


                          /*Grid grid = timeLineStripLineControlList[index].FindChildByType<Grid>();


                          //Style t = timeLineStripLineControlList[0].Style;



                          //Check current color
                          if (grid.Background == _normalColor)
                          {
                              if (index - 1 >= 0)
                              {
                                  //Reset old color value
                                  Grid oldValue = timeLinePeriodControlList[index - 1].FindChildByType<Grid>();
                                  oldValue.Background = _alternateColor;
                              }
                          }
                          else
                          {
                              if (index - 1 >= 0)
                              {
                                  //Reset old color value
                                  Grid oldValue = timeLinePeriodControlList[index - 1].FindChildByType<Grid>();
                                  oldValue.Background = _normalColor;
                              }
                          }

                          //set current timeline color
                          grid.Background = Brushes.GreenYellow;*/



            //Grid grid = timeLinePeriodControlList[index].FindChildByType<Grid>();

            //System.Windows.Controls.Border border = (System.Windows.Controls.Border)grid.Children[1];
            //border.Background = Brushes.Red;

            //System.Windows.Controls.Border border = (System.Windows.Controls.Border)grid.Children[0];

            //border.BorderBrush = Brushes.Red;

            /*Style s = new Style
            {
                TargetType = typeof(System.Windows.Controls.Border)
            };

            s.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.Blue));

            grid.Style = s;*/

            /* if (grid != null)
                 grid.Background = Brushes.White;

             grid = timeLinePeriodControlList[index].FindChildByType<Grid>();
             grid.st

             if (grid.Background == Brushes.Blue)
             {
                 string t = "tt";
             }

             if (grid != null)
                 grid.Background = Brushes.Blue;*/
            /*   }
           }

           //_oldIndex = index;
       }
   }
}
       catch
       {
           //Avoid UI crash

       }
       finally
       {

       }*/
        }

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

            //Reset Play sound track
            PlaySoundTrack = false;
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

        /// <summary>
        /// Compute visible period if line has failed 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FireworkManager_LineFailed(object sender, EventArgs e)
        {
            _line = sender as Line;

            //Update GUI only when normal line and launch only one time
            if (!_line.IsRescueLine && _line.LaunchTimeCounter == 1)
            {
                DateTime visiblePeriodStart = DateTime.Now.Date.Add(_line.Ignition).Subtract(new TimeSpan(0, 0, 20));
                DateTime visiblePeriodEnd = DateTime.Now.Date.Add(_line.Ignition).Add(new TimeSpan(0, 0, 40));

                ComputeVisiblePeriod(visiblePeriodStart, visiblePeriodEnd, _line.Fireworks[0].RadRowIndex, false);
            }
        }

        /// <summary>
        /// Compute visible period if line has started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void FireworkManager_LineStarted(object sender, EventArgs e)
        {
            _line = sender as Line;

            //Update GUI only when normal line and launch only one time
            if (!_line.IsRescueLine && _line.LaunchTimeCounter == 1)
            {
                DateTime visiblePeriodStart = DateTime.Now.Date.Add(_line.Ignition).Subtract(new TimeSpan(0, 0, 20));
                DateTime visiblePeriodEnd = DateTime.Now.Date.Add(_line.Ignition).Add(new TimeSpan(0, 0, 40));

                ComputeVisiblePeriod(visiblePeriodStart, visiblePeriodEnd, _line.Fireworks[0].RadRowIndex, false);
            }
        }

        //TODO : Put this in base class
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
                if (_armFireworkCommand != null)
                    _armFireworkCommand.RaiseCanExecuteChanged();

                if (_startFireworkCommand != null)
                    _startFireworkCommand.RaiseCanExecuteChanged();

                if (_stopFireworkCommand != null)
                    _stopFireworkCommand.RaiseCanExecuteChanged();

                if (_centerTimeLineCommand != null)
                    _centerTimeLineCommand.RaiseCanExecuteChanged();

                OnPropertyChanged("IsPlaySoundTrackEnabled");
                OnPropertyChanged("SanityCheckStatusImagePath");

                RefreshTimelineUI();
            });
        }

        #endregion

        #region Public Members

        public void LaunchFailedLine(string lineNumber)
        {
            try
            {
                _fireworkManager.LaunchFailedLine(lineNumber);
            }
            catch (CannotLaunchLineException ex)
            {
                DialogBoxHelper.ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// TODO : maybe something better can be done...
        /// </summary>
        /// <param name="rtl"></param>
        public void SetRadtimeline(RadTimeline rtl)
        {
            _fireworkTimeline = rtl;

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
            _fireworkTimeline.VisiblePeriodChanged += FireworkTimeline_VisiblePeriodChanged;
            _fireworkTimeline.SizeChanged += FireworkTimeline_SizeChanged;
        }

        private void FireworkTimeline_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Reset current vertical range...
            _currentVerticalRange = null;
            //RefreshTimelineUI();            
        }

        /// <summary>
        /// Refresh alternate color to avoid UI glitches
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FireworkTimeline_VisiblePeriodChanged(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            //Reset current vertical range...
            _currentVerticalRange = null;
            RefreshTimelineUI();                        
        }

        #endregion

        #region Private Members 

        /// <summary>
        /// Lets stop firework!!
        /// </summary>
        private void StopFirework()
        {
            MessageBoxResult result = MessageBox.Show("Attention, vous êtes sur le point d'arrêter le feu d'artifice, voulez-vous continuer ?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            _fireworkManager.Stop();
        }

        /// <summary>
        /// Center TimeLine UI
        /// </summary>
        private void CenterTimeLine()
        {
            if (_line != null)
            {

                DateTime visiblePeriodStart = DateTime.Now.Date.Add(_fireworkManager.ElapsedTime).Subtract(new TimeSpan(0, 0, 20));
                DateTime visiblePeriodEnd = DateTime.Now.Date.Add(_fireworkManager.ElapsedTime).Add(new TimeSpan(0, 0, 40));

                ComputeVisiblePeriod(visiblePeriodStart, visiblePeriodEnd, _line.Fireworks[0].RadRowIndex, true);
            }

        }

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
            _fireworkManager.Start(_playSoundTrack);

            //Refresh Control panel, user cannot unarm for exemple
            RefreshControlPanelUI();
        }

        private bool IsCenterTimeLineAllowed()
        {
            //sanity check ok (check if transceiver is connected)
            if (_fireworkManager.State == FireworkManagerState.FireworkRunning)
            {
                return true;
            }

            return false;
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

        private void RefreshTimelineUI()
        {
            //Do it only when a firework is running...otherwise useless
            if (_fireworkManager.State != FireworkManagerState.FireworkRunning) return;

            _userControlDispatcher.Invoke(() =>
            {
                //Refresh UI to avoid that timeline if keep in blue during intervals refresh 
                try
                {
                    int visiblePeriodStart = Convert.ToInt32(Math.Floor(_fireworkTimeline.VisiblePeriodStart.TimeOfDay.TotalSeconds));                    
                    int fireworkElapsedTime = Convert.ToInt32(Math.Floor(_fireworkManager.ElapsedTime.TotalSeconds));
                    int nbOfSeconds = fireworkElapsedTime - visiblePeriodStart;
                    //int index = 0;

                    List<TimelineStripLineControl> timelineStripLineControlList = _fireworkTimeline.ChildrenOfType<TimelineStripLineControl>().ToList();
                    foreach (TimelineStripLineControl tpc in timelineStripLineControlList)
                    {
                        //Refresh only part of ui that need to be refreshed!
                        //2021-08-18 - Do not work if user uses scroll bar to move forward and go backward
                        //if (index >= nbOfSeconds) break;

                        //index++;

                        /*Style timeLineStyle = new Style
                        {
                            TargetType = typeof(Border)
                        };

                        timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.White));

                        tpc.ElementStyle = timeLineStyle;*/
                        if (tpc.ElementStyle != _timeLineStyle)
                        {
                            tpc.ElementStyle = _timeLineStyle;
                        }
                    }
                }
                catch
                {
                    //Avoid UI crash
                }
            });
        }


        /// <summary>
        /// TODO: Maybe one day it will be removed...blank background : https://docs.telerik.com/devtools/silverlight/controls/radtimeline/how-to/howto-change-striplines-background
        /// </summary>
        /* private void RefreshTimelineUI()
         {
             _userControlDispatcher.Invoke(() =>
             {
                 //Refresh UI to avoid that timeline if keep in blue during intervals refresh 
                 try
                 {
                     //FluentResourceExtension previousFluentResourceExt = null;

                     int index = 0;

                     List<TimelineStripLineControl> timelineStripLineControlList = _fireworkTimeline.ChildrenOfType<TimelineStripLineControl>().ToList();
                     foreach (TimelineStripLineControl tpc in timelineStripLineControlList)
                     {
                         Style timeLineStyle = new Style
                         {
                             TargetType = typeof(Border)
                         };

                         if (index % 2 == 0)
                         {
                             timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _primaryBackgroundBrush));
                         }
                         else
                         {
                             timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _alternativeBrush));
                         }

                         tpc.ElementStyle = timeLineStyle;

                         index++;


                         /*
                         //Retrieve current element style
                         Style elementStyle = tpc.ElementStyle;

                         //Retrieve Setter of type System.Windows.Controls.Border
                         Type borderType = elementStyle.Setters[0].GetType();

                         //Get Value Property
                         PropertyInfo pi = borderType.GetProperty("Value");

                         object obj = pi.GetValue(elementStyle.Setters[0], null);

                         FluentResourceExtension fre = (FluentResourceExtension)obj;

                         //Find 
                         if (fre.ResourceKey == FluentResourceKey.AccentBrush && previousFluentResourceExt != null)
                         {
                             Style timeLineStyle = new Style
                             {
                                 TargetType = typeof(Border)
                             };

                             if (previousFluentResourceExt.ResourceKey == _alternativeBrush.ResourceKey)
                             {
                                 timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _primaryBackgroundBrush));
                             }
                             else
                             {
                                 timeLineStyle.Setters.Add(new Setter(Border.BackgroundProperty, _alternativeBrush));
                             }

                             tpc.ElementStyle = timeLineStyle;
                         }

                         previousFluentResourceExt = fre;    
                         */
        //          }
        //       }
        //       catch
        //        {
        //Avoid UI crash
        //       }
        //   });
        /*_userControlDispatcher.Invoke(() =>
        {                
            try
            {
                //_timeLineUIRefreshing = true;
                int index = 1;

                List<TimelineStripLineControl> timelineStripLineControllList = _fireworkTimeline.ChildrenOfType<TimelineStripLineControl>().ToList();
                foreach (TimelineStripLineControl tpc in timelineStripLineControllList)
                {
                    //Style elementStyle = tpc.ElementStyle;

                    //Retrieve Setter of type System.Windows.Controls.Border
                    //Type borderType = elementStyle.Setters[0].GetType();

                    //Get Value Property
                    //PropertyInfo pi = borderType.GetProperty("Value");

                    //object obj = pi.GetValue(elementStyle.Setters[0], null);

                    Style newStyle = new Style
                    {
                        TargetType = typeof(Border)
                    };

                    if (index % 2 == 0)
                    {
                        newStyle.Setters.Add(new Setter(Border.BackgroundProperty, _primaryBackgroundBrush));
                    }
                    else
                    {
                        newStyle.Setters.Add(new Setter(Border.BackgroundProperty, _alternativeBrush));
                    }

                    tpc.ElementStyle = newStyle;

                    /*if (obj.GetType() == typeof(FluentResourceExtension))
                    {
                        FluentResourceExtension u = (FluentResourceExtension)obj;
                    }


                    if (obj.GetType() == typeof(SolidColorBrush))
                    {
                        SolidColorBrush u = (SolidColorBrush)obj;
                    }*/

        /*Grid grid = tpc.FindChildByType<Grid>();

        if (index % 2 == 0)
        {                            
            grid.Background = _normalColor;
        }
        else
        {                            
            grid.Background = _alternateColor;
        }*/

        /*index++;
    }
}
catch
{
    //Avoid UI crash
}
finally
{
  //  _timeLineUIRefreshing = false;
}
});*/
        //}

        private void RefreshFireworkUI()
        {
            OnPropertyChanged("FireworkManager");
        }

        /// <summary>
        /// Compute visible timeline automatically (based on current line started)
        /// </summary>
        /// <param name="line"></param>
        private void ComputeVisiblePeriod(DateTime visiblePeriodStart, DateTime visiblePeriodEnd, int radRowIndex, bool byPassAutomaticScrollSanityCheck)
        {
            if (!_automaticTimelineScroll && !byPassAutomaticScrollSanityCheck) return;

            //Sanity check  _line must be initialized!
            //if (_line == null) return;

            //Put this into try/catch, just in case...
            //Don't want program to stop in the middle of firework
            try
            {
                //_computeVisiblePeriodComputing = true;
                //DateTime visiblePeriodStart = DateTime.Now.Date.Add(_line.Ignition).Subtract(new TimeSpan(0, 0, 20));
                //DateTime visiblePeriodEnd = DateTime.Now.Date.Add(_line.Ignition).Add(new TimeSpan(0, 0, 40));

                _userControlDispatcher.BeginInvoke((Action)(() =>
                {

                    //***************
                    //Horizontal part
                    //***************

                    //Change visible port view only if new visible period start - 20 s > period start ui
                    //_fireworkTimeline.VisiblePeriod.Start
                    if (visiblePeriodStart.CompareTo(_fireworkManager.PeriodStartUI) > 0)
                    {
                        _fireworkTimeline.VisiblePeriod = new Telerik.Windows.Controls.SelectionRange<DateTime>(visiblePeriodStart, visiblePeriodEnd);
                    }
                    else
                    {
                        //Reset horizontal position in case user has changed it
                        _fireworkTimeline.VisiblePeriodStart = FireworkManager.DefaultPeriodStartUI;
                        _fireworkTimeline.VisiblePeriodEnd = FireworkManager.DefaultPeriodEndUI;
                    }

                    //*************
                    //Vertical part
                    //*************

                    //Center firework as soon as possible
                    TimelineScrollBar verticalSlider = _fireworkTimeline.FindChildByType<TimelineScrollBar>();
                    if (verticalSlider != null)
                    {
                        //Need to re-compute select range?
                        if (!_currentVerticalRange.HasValue)
                            _currentVerticalRange = verticalSlider.SelectionRange;

                        //Slider value is between 0 - 1
                        //So (firework index * 1) / nb total firework = vertical slider position
                        double verticalSliderFireworkPositionStart = (double)radRowIndex / _fireworkManager.AllActiveFireworks.Count;
                        double verticalSliderFireworkPositionEnd = (double)radRowIndex / _fireworkManager.AllActiveFireworks.Count;

                        double centereredVerticalSliderFireworkPositionStart = verticalSliderFireworkPositionStart - (_currentVerticalRange.Value / 2);
                        double centereredVerticalSliderFireworkPositionEnd = verticalSliderFireworkPositionEnd + (_currentVerticalRange.Value / 2);

                        if (centereredVerticalSliderFireworkPositionStart >= 0)
                        {
                            if (centereredVerticalSliderFireworkPositionEnd > 1)
                            {
                                //Do it only one time! so at the end the slider does not keep on going down
                                if (!_lastCentereredVerticalSliderFireworkPositionStart.HasValue)
                                {
                                    _lastCentereredVerticalSliderFireworkPositionStart = centereredVerticalSliderFireworkPositionStart;
                                }

                                verticalSlider.Selection = new SelectionRange<double>(_lastCentereredVerticalSliderFireworkPositionStart.Value, 1);
                            }
                            else
                            {
                                verticalSlider.Selection = new SelectionRange<double>(centereredVerticalSliderFireworkPositionStart, centereredVerticalSliderFireworkPositionEnd);
                            }
                        }
                        else
                        {
                            //Ensure to scroll up in case user has scroll down for instance
                            verticalSlider.Selection = new SelectionRange<double>(0, _currentVerticalRange.Value);
                        }
                    }


                    /*
                     * Below = old implementation (before 07/2021)
                    /*

                    /*TimelineScrollBar verticalSlider = _fireworkTimeline.FindChildByType<TimelineScrollBar>();                    
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
                    }*/
                }));
            }
            catch
            {
                //NLog here
            }
        }

        private void ResetScrollBar()
        {
            //Refresh useful value
            _lastCentereredVerticalSliderFireworkPositionStart = null;
            _currentVerticalRange = null;

            //Horizontal
            _fireworkTimeline.VisiblePeriodStart = FireworkManager.DefaultPeriodStartUI;
            _fireworkTimeline.VisiblePeriodEnd = FireworkManager.DefaultPeriodEndUI;

            //Vertical
            TimelineScrollBar verticalSlider = _fireworkTimeline.FindChildByType<TimelineScrollBar>();

            if (verticalSlider != null)
            {
                var newStart = 0;
                var newEnd = verticalSlider.SelectionEnd;

                verticalSlider.Selection = new SelectionRange<double>(newStart, newEnd);
            }
        }

        #endregion
    }
}
