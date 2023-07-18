using fr.guiet.kquatre.business.exceptions;
using fr.guiet.kquatre.business.receptor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace fr.guiet.kquatre.business.firework
{
    /// <summary>
    /// Handle firework line (line contains is a set of fireworks
    /// </summary>
    public class Line : INotifyPropertyChanged
    {
        #region Event

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler LineStarted;
        public event EventHandler LineFailed;

        private void OnLineStartedEvent()
        {
            _launchedTimeCounter++;
            LineStarted?.Invoke(this, new EventArgs());
        }

        private void OnLineFailedEvent()
        {
            _launchedTimeCounter++;
            LineFailed?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Private Members

        private bool _isRescueLine = false;

        /// <summary>
        /// Number of time this line has been launched (user can relaunched a ligne when it has failed)
        /// </summary>
        private int _launchedTimeCounter = 0;

        //private bool _isDirty = false;

        //Line number
        private int _number = -1;

        //Time at which firework(s) is/are launched
        private TimeSpan _ignition;

        //Use on the UI
        private string _ignitionUI = "00:00:00"; //default value

        //Firework(s) linked to this line
        private readonly ObservableCollection<Firework> _fireworks = new ObservableCollection<Firework>();

        /// <summary>
        /// Line state
        /// </summary>
        private LineState _state = LineState.Standby;
       
        #endregion

        #region Public Members

        public int LaunchTimeCounter
        {
            get
            {
                return _launchedTimeCounter;
            }
        }

        public String IsRescueLineText
        {
            get
            {
                if (_isRescueLine)
                {
                    return "Oui";
                }
                else
                {
                    return "Non";
                }
            }
        }

        public bool IsRescueLine
        {
            get
            {
                return _isRescueLine;
            }
            set
            {
                _isRescueLine = value;

                if (value == true)
                {
                    Ignition = new TimeSpan();
                }

                OnPropertyChanged();
            }
        }

        public LineState State
        {
            get
            {
                return _state;
            }
        }

        public string Number
        {
            get
            {
                return _number.ToString();
            }

            set
            {
                if (_number.ToString() != value)
                {
                    _number = Convert.ToInt32(value);
                    OnPropertyChanged();                   
                }
            }
        }
        
        public string NumberUI
        {
            get
            {
                return string.Format("Ligne n° {0}", _number.ToString());
            }
        }

        [RegularExpression(@"^(?:2[0-3]|[01][0-9]):[0-5][0-9]:[0-5][0-9]$", ErrorMessage = "Mise à feu invalide")]
        public string IgnitionUI
        {
            get
            {
                return _ignitionUI;
            }

            set
            {
                if (_ignitionUI != value)
                {
                    if (String.IsNullOrEmpty(value))
                    {
                        Ignition = TimeSpan.Parse("00:00:00");
                        _ignitionUI = "00:00:00";
                    }
                    else
                    {

                        Validator.ValidateProperty(value, new ValidationContext(this, null, null) { MemberName = "IgnitionUI" });
                        Ignition = TimeSpan.Parse(value);
                        _ignitionUI = value;
                    }                    
                }
            }
        }
        
        public TimeSpan Ignition
        {
            get
            {
                return _ignition;
            }

            set
            {
                if (_ignition != value)
                {
                    _ignition = value;                    
                    OnPropertyChanged();
                }
            }
        }
       
        public ObservableCollection<Firework> Fireworks
        {
            get
            {
                return _fireworks;
            }

        }
        
        public TimeSpan? LongestFireworkDuration
        {
            get
            {
                if (_fireworks.Count == 0) return null;

                //Order List
                List<Firework> orderedList = _fireworks.OrderByDescending(t => t.Duration.TotalSeconds).ToList();

                return orderedList.Select(t => t.Duration).First();
            }
        }
        
        #endregion

        #region Constructor

        public Line(int number)
        {
            _number = number;
        }     
        
        public Line()
        {

        }

        #endregion

        #region Public Methods    

        /// <summary>
        /// Return a list of firework associated with the same receptor (but maybe not the same channel)
        /// </summary>
        /// <param name="receptorAddress"></param>
        /// <returns></returns>
        public List<Firework> GetFireworksWithSameReceptor(string receptorAddress)
        {
            return (from f in _fireworks
                    where f.ReceptorAddress.Address == receptorAddress
                    select f).ToList();
        }
        
               
        public void Reorder(int number)
        {
            Number = number.ToString();            
        }
       
        public bool IsFinished
        {
            get
            {
                //return (_state == LineState.Finished);
                //|| _state == LineState.LaunchFailed);

                //Une ligne est termine si tout ces feux sont termines ou failed
                //ou si c'est une ligne de secours et feux en attente 

                if (!_isRescueLine)
                {
                    var fList = (from f in _fireworks
                                 where f.State == FireworkState.Finished || f.State == FireworkState.LaunchFailed
                                 select f).ToList();

                    return (fList.Count == _fireworks.Count);
                }

                if (_isRescueLine)
                {
                    var fList = (from f in _fireworks
                                 where f.State == FireworkState.Standby 
                                 || f.State == FireworkState.LaunchFailed
                                 || f.State == FireworkState.Finished
                                 select f).ToList();

                    return (fList.Count == _fireworks.Count);
                }

                //Will never happen
                return true;
            }
        }
       
        public void SetImminentLaunch()
        {
            _state = LineState.ImminentLaunch;

            foreach (Firework firework in _fireworks)
            {
                firework.SetImminentLaunch();
            }
        }

        public void SetAsRescueLine()
        {
            IsRescueLine = true;
        }

        public void Reset()
        {
            _state = LineState.Standby;
            _launchedTimeCounter = 0;

            foreach (Firework firework in _fireworks)
            {
                firework.Reset();
            }
        }

        public void Stop()
        {        
            //Stop firework
            foreach (Firework firework in _fireworks)
            {
                firework.Stop();
            }
        }

        /// <summary>
        /// Start some fireworks of this line
        /// If line status != InProgress set it to InProgress Status
        /// </summary>
        public void StartFireworks(List<Firework> fireworks)
        {
            //Not in Progress?
            if (_state != LineState.InProgress)
            {
                OnLineStartedEvent();

                //Change line state
                _state = LineState.InProgress;
            }

            //Start firework
            foreach (Firework firework in fireworks)
            {
                firework.Start();
            }
        }

        /// <summary>
        /// Set this line as failed even thought some firework associated with different receptor are ok
        /// </summary>
        /// <param name="fireworks"></param>
        public void SetFailed(List<Firework> fireworks)
        {
            if (_state != LineState.LaunchFailed)
            {
                OnLineFailedEvent();

                _state = LineState.LaunchFailed;
            }

            //Set firework state to failed
            foreach (Firework firework in fireworks)
            {
                firework.SetFailed();
            }            
        }

        /// <summary>
        /// Add new firework to this line
        /// </summary>
        /// <param name="firework"></param>
        public void AddFirework(Firework firework)
        {
            firework.FireworkFinished += Firework_FireworkFinished;
            firework.AssignLine(this);
            _fireworks.Add(firework);
        }

        /// <summary>
        /// Remove a firework line association
        /// </summary>
        /// <param name="firework"></param>
        public void DeleteFirework(Firework firework)
        {
            //Delete Event
            firework.FireworkFinished-= Firework_FireworkFinished;

            //Remove receptor association
            firework.UnassignReceptorAddress();

            //Remove line association from firework
            firework.AssignLine(null);

            //Remove firework from list
            _fireworks.Remove(firework);
        }

        /// <summary>
        /// Get a partial clone of this object
        /// Only alterable property and property shown on screen are cloned
        /// </summary>
        /// <returns></returns>               
        public Line PartialClone()
        {
            Line l = new Line(_number)
            {
                Ignition = _ignition,                                            
                IsRescueLine = _isRescueLine
            };

            return l;
        }

        /// <summary>
        /// Update properties from line clone object
        /// </summary>
        /// <param name="lineClone"></param>
        public void UpdateFromClone(Line lineClone)
        {
            Number = lineClone.Number;
            Ignition = lineClone.Ignition;
            IsRescueLine = lineClone.IsRescueLine;          
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Event raised by firework when it is finished
        /// Allow to compute whehter line is finishied or not (ie all fireworks are finished and not in failed state for instance)
        /// Line state is not represented by a color on the UI 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Firework_FireworkFinished(object sender, EventArgs e)
        {
            int nb = (from l in _fireworks
                      where l.IsFinished == true
                      select l).Count();

            if (nb == _fireworks.Count)
            {
                _state = LineState.Finished;                
            }
        }

        #endregion
    }
}
