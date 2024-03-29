﻿using fr.guiet.kquatre.business.firework;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace fr.guiet.kquatre.ui.viewmodel
{
    public class SanityCheckViewModel : INotifyPropertyChanged
    {
        private FireworkManager _fireworkManager = null;

        private string _sanityCheckResult = null;

        public string SanityCheckResult
        {
            get
            {
                return _sanityCheckResult;
            }

            set
            {
                if (_sanityCheckResult != value)
                {
                    _sanityCheckResult = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public SanityCheckViewModel(FireworkManager fireworkManager)
        {
            _fireworkManager = fireworkManager;

            _fireworkManager.SanityCheck();

            if (_fireworkManager.IsSanityCheckOk)
            {
                _sanityCheckResult = "Aucun problème détecté. Le feu d'artifice peut être tiré";
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Problème(s) détecté(s) : ");
                sb.AppendLine();

                foreach (string error in _fireworkManager.SanityCheckErrorsList)
                {
                    sb.AppendLine(" - " + error);
                }

                _sanityCheckResult = sb.ToString();
            }
        }
    }
}
