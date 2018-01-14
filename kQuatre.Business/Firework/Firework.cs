using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows.Media;

namespace Guiet.kQuatre.Business.Firework
{
    /// <summary>
    /// Handle firework 
    /// </summary>
    public class Firework : INotifyPropertyChanged
    {
        #region Private Members

        /// <summary>
        /// Firework order number
        /// </summary>
        private int _number;

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
        private decimal _percentComplete = 100;

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

        /// <summary>
        /// Color representation of firework on Timeline Diagramm
        /// </summary>
        private Color _radColor = Colors.Gray;

        /// <summary>
        /// Line assigned to this firework
        /// </summary>
        private Line _assignedLine = null;        

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
        
        #endregion

        #region Public Members 

        public string SummaryUI
        {
            get
            {
                return string.Format("{0} ({1})", _assignedLine.NumberUI, _assignedLine.ReceptorAddressUI);
            }
        }

        public DateTime RadStart
        {
            get
            {
                return DateTime.Now.Date.Add(_assignedLine.Ignition);
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
                        this.RadColor = Colors.Green;
                        break;

                    case FireworkState.ImminentLaunch:
                        this.ColorPresentation = new SolidColorBrush(Colors.Orange);
                        this.RadColor = Colors.Orange;
                        break;

                    case FireworkState.InProgress:
                        this.ColorPresentation = new SolidColorBrush(Colors.Red);
                        this.RadColor = Colors.Red;
                        break;

                    case FireworkState.LaunchFailed:
                        this.ColorPresentation = new SolidColorBrush(Colors.Black);
                        this.RadColor = Colors.Black;
                        break;

                    case FireworkState.Standby:
                        this.ColorPresentation = new SolidColorBrush(Colors.Gray);
                        this.RadColor = Colors.Gray;
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

        public Color RadColor
        {
            get { return _radColor; }
            set
            {
                if (_radColor != value)
                {
                    _radColor = value;
                    OnPropertyChanged();
                }
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

        public int RadRowIndex
        {
            get
            {
                return _number;
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

        public void AssignLine(Line line)
        {
            _assignedLine = line;
        }

        public Firework GetClone()
        {
            Firework f = new Firework(_reference, _designation, _duration);
            return f;
        }

        public void Reorder(int order)
        {
            _number = order;
        }

        public void Reset()
        {
            State = FireworkState.Standby;
            PercentComplete = 100;
        }

        public void Stop()
        {
            //first timer help!!! it has importance here because elaptime time is used 
            if (_timerHelper != null)
            {
                _timerHelper.Stop();
                _timerHelper = null;
            }

            if (_elapsedTime != null)
            {
                _elapsedTime.Stop();
                _elapsedTime = null;
            }            
        }

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
