using Guiet.kQuatre.Business.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Guiet.kQuatre.UI.ViewModel
{
    public class ConfigurationWindowViewModel : INotifyPropertyChanged
    {
        private bool _isDirty = false;

        private SoftwareConfiguration _configuration = null;

        private const string EXCEL_FIRST_ROW_VALIDATION_ERROR = "La première ligne de donnée du fichier Excel doit être un entier positif";

        public List<ConfigFolderNode> ConfTreeViewDataSource
        {
            get
            {
                return _configuration.TreeViewDataSource;
            }
        }    
        
        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged();
                }
            }
        }        

        public ConfigurationWindowViewModel(SoftwareConfiguration configuration)
        {
            _configuration = configuration;
        }    

        public void Save()
        {
            try
            {
                _configuration.Save();
                IsDirty = false;
            }
            catch(Exception e)
            {
                MessageBox.Show("Une erreur est apparue lors de la sauvegarde de la configuration" + Environment.NewLine + "Erreur : " + e.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowErrorMessage(object propertyNode)
        {
            ConfigPropertyNode cpn = propertyNode as ConfigPropertyNode;

            switch (cpn.PropertyId)
            {
                case SoftwareConfiguration.EXCEL_FIRST_ROW_DATA_PROP_ID:
                    MessageBox.Show(EXCEL_FIRST_ROW_VALIDATION_ERROR, "Erreur de validation", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
                    break;
            }            
        }

        public bool CheckConfigPropertyValue(object propertyNode, string newValue) 
        {
            ConfigPropertyNode cpn = propertyNode as ConfigPropertyNode;

            if (SoftwareConfiguration.EXCEL_FIRST_ROW_DATA_PROP_ID == cpn.PropertyId)
            {
                int result;
                if (!int.TryParse(newValue, out result))
                {
                    return false;
                }
            }

            return true;
        }

        #region Event

        //TODO : Put this in base class
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

    }
}
