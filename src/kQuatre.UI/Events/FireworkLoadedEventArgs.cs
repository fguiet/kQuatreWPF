using fr.guiet.kquatre.business.firework;
using System;

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
