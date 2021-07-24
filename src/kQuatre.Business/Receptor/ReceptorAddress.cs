using fr.guiet.kquatre.business.exceptions;
using fr.guiet.kquatre.business.firework;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace fr.guiet.kquatre.business.receptor
{
    /// <summary>
    /// Handle receptor address (availability, channel, etc...)
    /// </summary>
    public class ReceptorAddress : INotifyPropertyChanged
    {
        #region Private Members

        /// <summary>
        /// Link to the receptor entity
        /// </summary>
        private Receptor _receptor = null;

        /// <summary>
        /// Channel number
        /// </summary>
        private int _channel;

        /// <summary>
        /// Address
        /// </summary>
        private string _address;

        /// <summary>
        /// Line assigned to this address (null is not assigned)
        /// </summary>
        private Line _line = null;

        /// <summary>
        /// Current conductivity of this receptor address
        /// </summary>
        private string _conductivite = "";

        #endregion

        public bool IsAvailable
        {
            get
            {
                return (_line == null);
            }
        }

       

        #region Constructor

        public ReceptorAddress(Receptor receptor, int channel)
        {
            _receptor = receptor;
            _channel = channel;
            _address = receptor.Address;
        }

        #endregion

        #region Public Members

        public string Conductivite
        {
            get
            {
                return _conductivite;
            }

            set
            {
                if (_conductivite != value) {
                    _conductivite = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LineAssociatedNumberUI
        {
            get
            {
                return _line.NumberUI;
            }
        }

        public string ReceptorAddressUI
        {
            get
            {
                return string.Format("{0} / Canal {1}", _receptor.Name, _channel);
            }
        }

        /// <summary>
        /// Assign line to this address
        /// </summary>
        /// <param name="line"></param>
        public void AssignLine(Line line)
        {
            if (_line != null)
            {
                string message = string.Format("Le récepteur : {0} / calnal {1} est déjà occupé par la ligne n° {2} ", _receptor.Name, _channel, _line.Number);
                throw new ReceptorAddressAlreadyAssignedException(message);
            }

            _line = line;
        }

        public void Unassign(Line line)
        {
            _line = null;
        }

        public string Address
        {
            get
            {
                return _address;
            }
        }

        public int Channel
        {
            get
            {
                return _channel;
            }
        }

        public Receptor Receptor
        {
            get
            {
                return _receptor;
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
