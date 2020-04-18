using fr.guiet.kquatre.business.firework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.kquatre.ui.viewmodel
{

    public class RadTimelineTestView : INotifyPropertyChanged
    {
        private FireworkManager _fireworkManager = null;

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

        public RadTimelineTestView(FireworkManager fm)
        {
            _fireworkManager = fm;
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
    }
}
