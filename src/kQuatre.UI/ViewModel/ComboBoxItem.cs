using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.kquatre.ui.viewsmodel
{
    public class ComboBoxItem
    {
        private string _id;
        private string _value;

        public ComboBoxItem(string id, string value)
        {
            _id = id;
            _value = value;
        }

        public string Id
        {
            get
            {
                return _id;
            }
        }

        public string Value
        {
            get
            {
                return _value;
            }
        }
    }
}
