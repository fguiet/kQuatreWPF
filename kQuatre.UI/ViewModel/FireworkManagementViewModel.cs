using Guiet.kQuatre.Business.Firework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.UI.ViewModel
{
    public class FireworkManagementViewModel
    {
        private FireworkManager _fireworkManager = null;

        public FireworkManagementViewModel(FireworkManager fireworkManager)
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
