using Guiet.kQuatre.Business.Gantt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Firework
{
    public class FireworkStateChangedEventArgs : EventArgs
    {
        private Firework _firework = null;
        private string _propertyName = null;

        public Firework Firework
        {
            get
            {
                return _firework;
            }
        }

        public string PropertyName
        {
            get
            {
                return _propertyName;
            }
        }

        public FireworkStateChangedEventArgs(Firework firework, string pn)
        {
            _firework = firework;
            _propertyName = pn;
        }
    }
}
