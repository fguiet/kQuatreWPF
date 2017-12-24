using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Configuration
{
    public class ConfigPropertyNode 
    {
        private string _propertyId;
        private string _propertyName;
        private string _propertyValue;       

        public string PropertyId
        {
            get
            {
                return _propertyId;
            }
        }

        public string PropertyName
        {
            get
            {
                return _propertyName;
            }           
        }

        public string PropertyValue
        {
            get
            {
                return _propertyValue;
            }
            set
            {
                if (_propertyValue != value)
                {
                    _propertyValue = value;                    
                }
            }
        }

        public ConfigPropertyNode(string propertyId, string propertyName, string propertyValue)
        {
            _propertyId = propertyId;
            _propertyName = propertyName;
            _propertyValue = propertyValue;
        }
    }
}
