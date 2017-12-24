using Guiet.kQuatre.Business.Firework;
using Guiet.kQuatre.Business.Receptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.UI.ViewModel
{
    public class LineViewModel : INotifyPropertyChanged
    {
        private bool _isDirty = false;

        private FireworkManager _fireworkManager = null;

        private Line _line = null;

        private Line _lineClone = null;

        private WindowMode _mode;

        private List<ComboBoxItem> _lineLocation = new List<ComboBoxItem>();

        private const string DO_NOT_MOVE_ID = "-1";

        public List<ComboBoxItem> LineLocation
        {
            get
            {
                return _lineLocation;
            }
        }

        private string _selectedLineLocation;
        public string SelectedLineLocation
        {
            get
            {
                return _selectedLineLocation;
            }
            set
            {
                if (_selectedLineLocation != value)
                {
                    _selectedLineLocation = value;
                }
            }
        }

        public List<ReceptorAddress> ReceptorAddressesAvailable
        {
            get
            {
                if (_line.ReceptorAddress != null)
                {
                    List<ReceptorAddress> ra = new List<ReceptorAddress>();
                    ra.AddRange(_fireworkManager.ReceptorAddressesAvailable);
                    ra.Add(_line.ReceptorAddress);

                    return ra;
                }
                else
                    return _fireworkManager.ReceptorAddressesAvailable;
            }

        }

        //private bool _isLineNumberReadOnly;

        public bool IsLineNumberEnabled
        {
            get
            {
                return (_mode == WindowMode.Add);
            }
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

        public bool IsModeCreation
        {
            get
            {
                return (_mode == WindowMode.Add);
            }
        }

        public bool IsModeUpdate
        {
            get
            {
                return (_mode == WindowMode.Modify);
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

        public LineViewModel(FireworkManager fireworkManager, Line line)
        {
            _fireworkManager = fireworkManager;
            _line = line;           

            if (line == null)
            {
                _mode = WindowMode.Add;
                _line = new Line();
                _lineClone = new Line();
            }
            else
            {
                _mode = WindowMode.Modify;
                _lineClone = line.PartialClone();
            }

            InitializeLineLocation();
            _lineClone.PropertyChanged += LineClone_PropertyChanged;            
        }

        private void LineClone_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsDirty = true;
        }

        public Line Line
        {
            get
            {
                return _lineClone;
            }
        }

        public FireworkManager FireworkManager
        {
            get
            {
                return _fireworkManager;
            }
        }

        public void Dissociate()
        {
            _lineClone.UnassignReceptorAddress();
        }

        public void UpdateLineNumber()
        {
            //Update line number
            if (SelectedLineLocation != DO_NOT_MOVE_ID)
                _lineClone.Number = SelectedLineLocation;
        }

        public void Save()
        {            
            if (_mode == WindowMode.Add)
                _fireworkManager.AddOrUpdateLine(true, _line, _lineClone);
            else
                _fireworkManager.AddOrUpdateLine(false, _line, _lineClone);

        }

        private void InitializeLineLocation()
        {
            int nbLines = _fireworkManager.Lines.Count;
            string verb = string.Empty;
            ComboBoxItem cbi = null;
            string message = string.Empty;
            if (_mode == WindowMode.Add)
            {
                verb = "Insérer";
                cbi = new ComboBoxItem("0", "Insérer en première position");
                _lineLocation.Add(cbi);

                //Insert on first position selected by default
                SelectedLineLocation = "0";
                //SelectedLineLocation = (nbLines + 1).ToString();

                if (nbLines == 0)
                    return;
            }
            else
            {
                verb = "Déplacer";
                cbi = new ComboBoxItem(DO_NOT_MOVE_ID, "Ne pas déplacer");
                _lineLocation.Add(cbi);

                SelectedLineLocation = DO_NOT_MOVE_ID;

                if (nbLines == 1) return;

                if (_line.Number != "1")
                {
                    cbi = new ComboBoxItem("0", "Déplacer en première position");
                    _lineLocation.Add(cbi);
                }
            }

            if (nbLines > 2)
            {
                for (int i = 1; i < nbLines; i++)
                {
                    if (i.ToString() != _line.Number && (i + 1).ToString() != _line.Number)
                    {
                        message = string.Format("{0} entre la ligne {1} et {2}", verb, i, i + 1);
                        cbi = new ComboBoxItem(i.ToString(), message);
                        _lineLocation.Add(cbi);
                    }
                }
            }

            if (_line.Number != nbLines.ToString())
            {
                message = string.Format("{0} en dernière position", verb);
                cbi = new ComboBoxItem((nbLines + 1).ToString(), message);
                _lineLocation.Add(cbi);
            }
        }
    }
}
