using Guiet.kQuatre.Business.Configuration;
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
        private SoftwareConfiguration _softwareConfiguration = null;

        public FireworkManagementViewModel(SoftwareConfiguration softwareConfiguration)
        {
            _softwareConfiguration = softwareConfiguration;
        }

        public SoftwareConfiguration SoftwareConfiguration
        {
            get
            {
                return _softwareConfiguration;
            }           
        }
    }
}
