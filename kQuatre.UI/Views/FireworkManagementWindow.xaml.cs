using Guiet.kQuatre.Business.Configuration;
using Guiet.kQuatre.Business.Firework;
using Guiet.kQuatre.UI.Helpers;
using Guiet.kQuatre.UI.ViewModel;
using Infragistics.Windows.DataPresenter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Guiet.kQuatre.UI.Views
{
    /// <summary>
    /// Logique d'interaction pour FireworkManagementWindow.xaml
    /// </summary>
    public partial class FireworkManagementWindow : Window
    {
        private FireworkManagementViewModel _viewModel = null;
        private FireworkManager _fireworkManager = null;
        private SoftwareConfiguration _softwareConfiguration = null;
        private Business.Firework.Line _line = null;        

        public FireworkManagementWindow(FireworkManager fm, SoftwareConfiguration softwareConfiguration)
        {
            InitializeComponent();

            _fireworkManager = fm;            
            _softwareConfiguration = softwareConfiguration;

            this.Loaded += FireworkManagementWindow_Loaded;            

            //Datagrid
            _dgFireworks.DataSourceChanged += DgFireworks_DataSourceChanged; ;
        }

        private void DgFireworks_DataSourceChanged(object sender, RoutedPropertyChangedEventArgs<System.Collections.IEnumerable> e)
        {
            //Initialize Checkbox unbound colum
            foreach (Record r in _dgFireworks.Records)
            {
                DataRecord dr = r as DataRecord;
                if (dr != null)
                {
                    Cell cell = dr.Cells["Select"];
                    cell.Value = false;
                }
            }
        }         
        
        public FireworkManagementWindow(FireworkManager fm, SoftwareConfiguration softwareConfiguration, Business.Firework.Line line) : this(fm, softwareConfiguration)
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

            foreach (Record r in _dgFireworks.Records)
            {
                DataRecord dr = r as DataRecord;
                if (dr != null)
                {
                    if (Convert.ToBoolean(dr.Cells["Select"].Value) == true)
                    {
                        fireworkList.Add(dr.DataItem as Firework);
                    }                    
                }
            }

            if (fireworkList.Count == 0)
            {
                DialogBoxHelper.ShowWarningMessage("Veuillez sélectionner au moins un feu d'aritfice");
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

        /// <summary>
        /// Hide Select Column in certain circumstances
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _dgFireworks_LayoutUpdated(object sender, EventArgs e)
        {
            if ((_line == null))
            {
                _dgFireworks.FieldLayouts["Firework"].Fields["Select"].Visibility = Visibility.Collapsed;
            }            
        }
    }
}
