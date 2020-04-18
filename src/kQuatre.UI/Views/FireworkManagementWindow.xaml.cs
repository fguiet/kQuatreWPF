using fr.guiet.kquatre.business.configuration;
using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.ui.helpers;
using fr.guiet.kquatre.ui.viewmodel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using Line = fr.guiet.kquatre.business.firework.Line;

namespace fr.guiet.kquatre.ui.views
{
    /// <summary>
    /// Logique d'interaction pour FireworkManagementWindow.xaml
    /// </summary>
    public partial class FireworkManagementWindow : Window
    {
        #region Private Members

        private FireworkManagementViewModel _viewModel = null;
        private FireworkManager _fireworkManager = null;
        private SoftwareConfiguration _softwareConfiguration = null;
        private Line _line = null;

        #endregion

        #region Constructor

        public FireworkManagementWindow(FireworkManager fm, SoftwareConfiguration softwareConfiguration)
        {
            InitializeComponent();

            _fireworkManager = fm;            
            _softwareConfiguration = softwareConfiguration;

            this.Loaded += FireworkManagementWindow_Loaded;            
        }

        #endregion

        public FireworkManagementWindow(FireworkManager fm, SoftwareConfiguration softwareConfiguration, Line line) : this(fm, softwareConfiguration)
        {            
            _line = line;
        }

        private void FireworkManagementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this._viewModel = new FireworkManagementViewModel(_fireworkManager, _softwareConfiguration, _line);
            this.DataContext = _viewModel;
        }

        private void _btnSelect_Click(object sender, RoutedEventArgs e)
        {
            List<Firework> fireworkList = new List<Firework>();

            foreach(var firework in _dvFireworks.SelectedItems)
            {
                fireworkList.Add((Firework)firework);
            }            

            if (fireworkList.Count == 0)
            {
                DialogBoxHelper.ShowWarningMessage("Veuillez sélectionner au moins un feu d'artifice");
                return;
            }

            foreach(Firework fr in fireworkList)
            {
                Firework alreadyThere = _line.Fireworks.FirstOrDefault(f => f.Reference == fr.Reference);

                if (alreadyThere != null)
                {
                    DialogBoxHelper.ShowWarningMessage(string.Format("Le feu d'artifice avec la référence {0} est déjà associé à cette ligne", alreadyThere.Reference));
                    return;
                }
            }

            foreach (Firework fr in fireworkList)
            {
                Firework fireworkClone = fr.GetClone();
                _fireworkManager.AddFireworkToLine(fireworkClone, _line);
            }

            this.Close();
            
        }        
    }
}
