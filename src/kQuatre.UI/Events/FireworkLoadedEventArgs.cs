using fr.guiet.kquatre.business.firework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.kquatre.ui.events
{
    public class FireworkLoadedEventArgs : EventArgs
    {
        private FireworkManager _fireworkManager = null;

        public FireworkLoadedEventArgs(FireworkManager fireworkManager)
        {
            _fireworkManager = fireworkManager;
        }

        public FireworkManager FireworkManager
        {
            get
            {
                return _fireworkManager;
            }
        }
    }
}
