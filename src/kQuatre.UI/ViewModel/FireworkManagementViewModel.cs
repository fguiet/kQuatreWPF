﻿using fr.guiet.kquatre.business.configuration;
using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.ui.helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.kquatre.ui.viewmodel
{
    public class FireworkManagementViewModel
    {
        private readonly SoftwareConfiguration _softwareConfiguration = null;
        private readonly FireworkManager _fireworkManager = null;
        private readonly Line _line = null;         

        public bool CanSelect
        {
            get
            {
                return (_line != null);
            }
        }

        public FireworkManagementViewModel(FireworkManager fm, SoftwareConfiguration softwareConfiguration, Line line)
        {
            _softwareConfiguration = softwareConfiguration;
            _fireworkManager = fm;
            _line = line;
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
