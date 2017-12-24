using Guiet.kQuatre.Business.Gantt;
using Infragistics.Controls.Schedules;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows.Media;
using System.Windows.Threading;

namespace Guiet.kQuatre.Business.Firework
{
    /// <summary>
    /// Handle firework 
    /// </summary>
    public class Firework
    {
        #region Private Members

        /// <summary>
        /// 
        /// </summary>
        private string _reference = null;

        /// <summary>
        /// Firework name
        /// </summary>
        private string _designation = null;

        /// <summary>
        /// Firework duration
        /// </summary>
        private TimeSpan _duration;

        /// <summary>
        /// Pourcentage completed once fired
        /// </summary>
        private decimal _percentComplete = 0;

        /// <summary>
        /// Firework state
        /// </summary>
        private FireworkState _state = FireworkState.Standby;

        /// <summary>
        /// Time passed since firework is launched
        /// </summary>
        private Stopwatch _elapsedTime = null;

        /// <summary>
        /// Timer helper
        /// </summary>
        private Timer _timerHelper = null;
        
        /// <summary>
        /// Color representation of firework on Gantt Diagramm
        /// </summary>
        private SolidColorBrush _colorPresentation = new SolidColorBrush(Colors.Gray);

        #endregion

        #region Constructor

        public Firework(string reference, string designation, TimeSpan duration)
        {
            _reference = reference;
            _designation = designation;
            _duration = duration;
        }

        #endregion

        #region Event

        public event EventHandler<FireworkStateChangedEventArgs> FireworkStateChanged;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            if (FireworkStateChanged != null)
            {
                FireworkStateChanged(this, new FireworkStateChangedEventArgs(this, propertyName));
            }
        }

        public event EventHandler FireworkFinished;

        private void OnFireworkFinishedEvent()
        {
            if (FireworkFinished != null)
            {
                FireworkFinished(this, new EventArgs());
            }
        }

        private TaskModel _taskModel = null;

        #endregion

        #region Public Members  

        public TaskModel TaskModel
        {
            get
            {
                return _taskModel;
            }
        }

        public bool IsFinished
        {
            get
            {
                return (this.State == FireworkState.Finished || this.State == FireworkState.LaunchFailed);
            }
        }

        public FireworkState State
        {
            set
            {
                _state = value;

                switch (_state)
                {
                    case FireworkState.Finished:
                        this.ColorPresentation = new SolidColorBrush(Colors.Green);
                        break;

                    case FireworkState.ImminentLaunch:
                        this.ColorPresentation = new SolidColorBrush(Colors.Orange);
                        break;

                    case FireworkState.InProgress:
                        this.ColorPresentation = new SolidColorBrush(Colors.Red);
                        break;

                    case FireworkState.LaunchFailed:
                        this.ColorPresentation = new SolidColorBrush(Colors.Black);
                        break;

                    case FireworkState.Standby:
                        this.ColorPresentation = new SolidColorBrush(Colors.Gray);
                        break;
                }
            }
            get
            {
                return _state;
            }
        }

        public TimeSpan ElapsedTime
        {
            get
            {
                if (_elapsedTime == null) return TimeSpan.Parse("00:00");

                return _elapsedTime.Elapsed;
            }
        }

        public SolidColorBrush ColorPresentation
        {
            get { return _colorPresentation; }
            set
            {
                if (_colorPresentation != value)
                {
                    _colorPresentation = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal PercentComplete
        {
            get { return _percentComplete; }
            set
            {
                if (_percentComplete != value)
                {
                    _percentComplete = value;
                    OnPropertyChanged();
                }
            }

        }

        public TimeSpan Duration
        {
            get
            {
                return _duration;
            }
        }

        public string Designation
        {
            get
            {
                return _designation;
            }
        }

        public string Reference
        {
            get
            {
                return _reference;
            }
        }


        #endregion

        #region Public Method 

        public void Start()
        {         
            // Start timer
            _elapsedTime = new Stopwatch();
            _elapsedTime.Start();

            //Timer helper
            _timerHelper = new Timer();
            _timerHelper.Interval = 500;
            _timerHelper.Elapsed += TimerHelp_Elapsed;
            _timerHelper.Start();

            //Change firework state
            this.State = FireworkState.InProgress;
        }

        public void SetTaskModel(TaskModel tm)
        {
            _taskModel = tm;
        }

        public void SetFailed()
        {
            this.State = FireworkState.LaunchFailed;
        }

        public void SetImminentLaunch()
        {
            this.State = FireworkState.ImminentLaunch;
        }

        #endregion

        #region Private methods 

        private void TimerHelp_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Compute percent complete
            //100% = duration in millisecond

            double complete = (100 * _elapsedTime.ElapsedMilliseconds) / _duration.TotalMilliseconds;

            if (complete >= 100)
            {
                _timerHelper.Stop();
                _elapsedTime.Stop();
                this.PercentComplete = 100;
                this.State = FireworkState.Finished;
                OnFireworkFinishedEvent();
            }
            else
            {
                this.PercentComplete = Convert.ToDecimal(complete);
            }
        }

       

        #endregion


    }
}
