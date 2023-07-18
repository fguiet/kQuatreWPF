using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.business.receptor;
using fr.guiet.kquatre.ui.command;
using fr.guiet.kquatre.ui.viewmodel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Data;

namespace fr.guiet.kquatre.ui.ViewModel
{
    public class FireworkWindowViewModel : INotifyPropertyChanged
    {
        #region Private Members

        /// <summary>
        /// Modification detected?
        /// </summary>
        private bool _isDirty = false;

        /// <summary>
        /// Current firework edited
        /// </summary>
        private Firework _firework = null;

        /// <summary>
        /// Clone on firework edited
        /// </summary>
        private Firework _fireworkClone = null;

        /// <summary>
        /// Access to firework manager
        /// </summary>
        private FireworkManager _fireworkManager = null;

        /// <summary>
        /// To handle list of receptor addresses available for a firework
        /// </summary>
        private List<ReceptorAddress> _receptorAddressesAvailable = new();

        //private ReceptorAddress _currentSelectedReceptorAddress = null;        

        private RelayCommand _modifyFireworkCommand;

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

        #region Public Member   

        public Firework Firework
        {
            get
            {
                return _fireworkClone;
            }
        }

        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged();
                }
            }

        }
       
        /// <summary>
        /// Get a list of receptor available for the current selected firework
        /// </summary>
        public List<ReceptorAddress> ReceptorAddressesAvailable
        {
            get
            {
                return _receptorAddressesAvailable;
            }

            set
            {
                _receptorAddressesAvailable = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Constructeur

        public FireworkWindowViewModel(FireworkManager fireworkManager, Firework firework)
        {
            _fireworkManager = fireworkManager;
            _firework = firework;

            //Get partial of edited line
            _fireworkClone = firework.PartialClone();

            _fireworkClone.PropertyChanged += FireworkClone_PropertyChanged;

            RefreshReceptorAddressesAvailable();            
        }

        private void FireworkClone_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsDirty = true;
        }

        #endregion

        #region Public Methods
        
        /// <summary>
        /// Remove Firework <=> Receptor association
        /// </summary>
        public void DissociateReceptorFromFirework()
        {

            _fireworkClone.UnassignReceptorAddress();
        }


        public void Save()
        {
            _fireworkManager.UpdateFirework(_firework, _fireworkClone);          
        }

        #endregion

        #region Private Methods 

        /// <summary>
        /// Refresh the list of receptor address available for the selected firework
        /// </summary>
        private void RefreshReceptorAddressesAvailable()
        {
            //if (_fireworkClone.ReceptorAddress != null)
          //  {
                List<ReceptorAddress> ra = new List<ReceptorAddress>();
                ra.AddRange(_fireworkManager.ReceptorAddressesAvailable);

                //Add adress of all firework of this line, only once
                //
                // Sample 
                // Line 1
                //  - Firework 1 - Receptor 1 - Channel 1
                //  - Firework 2 - Receptor 1 - Channel 2
                //  - Firework 3 - Receptor 1 - Channel 1
                foreach (Firework f in _firework.AssignedLine.Fireworks)
                {
                    if (f.ReceptorAddress != null)
                    {
                        if (!ra.Contains(f.ReceptorAddress))
                            ra.Add(f.ReceptorAddress);
                    }
                }

                ReceptorAddressesAvailable = ra;
            /*}
            else
            {
                ReceptorAddressesAvailable = _fireworkManager.ReceptorAddressesAvailable;
            }*/
        }

        #endregion
    }
}
