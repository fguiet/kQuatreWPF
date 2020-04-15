using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.kquatre.business.configuration
{
    public class ConfigPropertyNode
    {
        private readonly string _propertyId;
        private readonly string _propertyName;
        private string _propertyValue;

        public string PropertyId
        {
            get
            {
                return _propertyId;
            }
        }

        /// <summary>
        /// Used in RadTreeListView to display UI properly
        /// </summary>
        public string FolderName
        {

            get
            {
                return _propertyName;
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
