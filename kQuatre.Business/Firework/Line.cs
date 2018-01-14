using Guiet.kQuatre.Business.Exceptions;
using Guiet.kQuatre.Business.Receptor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Guiet.kQuatre.Business.Firework
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
            //Make it...dirty
            _isDirty = true;

            if (PropertyChanged != null)
            {                
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event EventHandler LineStarted;
        public event EventHandler LineFailed;

        private void OnLineStartedEvent()
        {
            if (LineStarted != null)
            {
                LineStarted(this, new EventArgs());
            }
        }

        private void OnLineFailedEvent()
        {
            if (LineFailed != null)
            {
                LineFailed(this, new EventArgs());
            }
        }

        #endregion

        #region Private Members

        private bool _isDirty = false;

        //Line number
        private int _number = -1;

        //Receptor address linked to this line
        private ReceptorAddress _receptorAddress = null;

        //Time at which firework(s) is/are launched
        private TimeSpan _ignition;

        //Firework(s) linked to this line
        private ObservableCollection<Firework> _fireworks = new ObservableCollection<Firework>();

        /// <summary>
        /// Line state
        /// </summary>
        private LineState _state = LineState.Standby;

        /// <summary>
        /// Elapsed time since line has been fired
        /// </summary>
        //private Stopwatch _elapsedTime = null;

        #endregion

        #region Public Members

        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }
            set
            {
                _isDirty = value;
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

        public string ReceptorAddressUI
        {                    
            get
            {
                if (_receptorAddress != null)
                {
                    return _receptorAddress.ReceptorAddressUI;                    
                }
                else
                {
                    return "Non renseigné";
                }
            }
        }

        public ReceptorAddress ReceptorAddress
        {
            get
            {
                return _receptorAddress;
            }   
            set
            {
                if (_receptorAddress != value)
                {
                    _receptorAddress = value;
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
        
        public void Reorder(int number)
        {
            Number = number.ToString();            
        }

        public void AssignReceptorAddress(ReceptorAddress ra)
        {
            if (this._receptorAddress != null)
            {                
                string message = string.Format("La line n°{0} est déjà associée au récepteur : {1} / calnal {2} ", _number, _receptorAddress.Receptor.Name, _receptorAddress.Channel);
                throw new LineAlreadyAssignedException(message);
            }

            this._receptorAddress = ra;
            ra.AssignLine(this);
        }

        public void UnassignReceptorAddress()
        {
            if (this._receptorAddress != null)
            {
                _receptorAddress.Unassign(this);
                ReceptorAddress = null;
                //this._receptorAddress = null;                
            }
        }

        public bool IsFinished
        {
            get
            {
                return (_state == LineState.Finished || _state == LineState.LaunchFailed);
            }
        }

        public void SetFailed()
        {
            OnLineFailedEvent();

            _state = LineState.LaunchFailed;

            foreach (Firework firework in _fireworks)
            {
                firework.SetFailed();
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

        public void Reset()
        {
            _state = LineState.Standby;

            foreach (Firework firework in _fireworks)
            {
                firework.Reset();
            }
        }

        public void Stop()
        {
        
            //Start firework
            foreach (Firework firework in _fireworks)
            {
                firework.Stop();
            }
        }

        public void Start()
        {                         
            OnLineStartedEvent();

            //Change line state
            _state = LineState.InProgress;

            //Start firework
            foreach (Firework firework in _fireworks)
            {
                firework.Start();
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
        /// Get a partial clone of this object
        /// Only alterable property are cloned
        /// </summary>
        /// <returns></returns>
        public Line PartialClone()
        {
            Line l = new Line(_number);
            l.Ignition = _ignition;
            l.ReceptorAddress = _receptorAddress;
            l.IsDirty = false;

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

            if (lineClone.ReceptorAddress == null)
                UnassignReceptorAddress();
            else
            {
                //First unassign current address if any
                UnassignReceptorAddress();

                //Then assign new address
                AssignReceptorAddress(lineClone.ReceptorAddress);
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Event raised by firework when it is finished
        /// Allow to compute whehter line is finishied or not (ie all fireworks are finished)
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
