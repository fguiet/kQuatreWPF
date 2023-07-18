using fr.guiet.kquatre.business.exceptions;
using fr.guiet.kquatre.business.firework;
using System;
using System.Collections.Generic;
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
        /// Fireworks assigned to this address 
        /// </summary>
        private List<Firework> _fireworks = new List<Firework>();

        /// <summary>
        /// Current conductivity of this receptor address
        /// </summary>
        private string _conductivite = "";

        #endregion

        /// <summary>
        /// Available Receptor Address are adresses not linked to any firework
        /// </summary>
        public bool IsAvailable
        {
            get
            {
                return (_fireworks.Count == 0);
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
                //Receptor Address can be associated with multiple firework but they are all on the same line
                //So we take the first firework to obtain the info
                return _fireworks[0].AssignedLine.NumberUI;                
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
        /// Assign a firework to this receptor address (multiple firework can be assigned to the same receptor address)
        /// </summary>
        /// <param name="firework"></param>
        /// <exception cref="ReceptorAddressAlreadyAssignedException"></exception>
        public void AssignFirework(Firework firework)
        {
            if (!_fireworks.Contains(firework))
            {
                _fireworks.Add(firework);
            }
            else
            {
                string message = string.Format("Le récepteur : {0} / calnal {1} est déjà lié avec le feu d'artifice de référence : {2}, désignation : {3} sur la ligne : {4}", _receptor.Name, _channel, firework.Reference, firework.Designation, firework.AssignedLine.Number);
                throw new ReceptorAddressAlreadyAssignedException(message);
            }                     
        }

        public void UnassignFirework(Firework firework)
        {
            _fireworks.Remove(firework);
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
