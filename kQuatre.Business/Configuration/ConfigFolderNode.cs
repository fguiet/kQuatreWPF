using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Configuration
{
    public class ConfigFolderNode
    {
        private string _folderName;
        private ObservableCollection<ConfigPropertyNode> _propertyNodes = new ObservableCollection<ConfigPropertyNode>();        

        public string FolderName
        {
            get
            {
                return _folderName;
            }
            set
            {
                _folderName = value;
            }
        }

        public ConfigFolderNode(string folderName)
        {            
            _folderName = folderName;
        }

        public void AddNode(ConfigPropertyNode cpn)
        {
            _propertyNodes.Add(cpn);
        }

        public ObservableCollection<ConfigPropertyNode> PropertyNodes
        {
            get { return this._propertyNodes; }
        }
    }
}
