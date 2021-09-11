using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.business.receptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace fr.guiet.kquatre.ui.viewmodel
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
            set
            {
                if (_lineLocation != value)
                {
                    _lineLocation = value;
                    OnPropertyChanged();
                }
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
                    OnPropertyChanged();
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
            if (SelectedLineLocation != DO_NOT_MOVE_ID && SelectedLineLocation != null)
            {
                _lineClone.Number = SelectedLineLocation;
                //IsDirty = true;
            }

        }

        public void Save()
        {
            if (_mode == WindowMode.Add)
                _fireworkManager.AddOrUpdateLine(true, _line, _lineClone);
            else
                _fireworkManager.AddOrUpdateLine(false, _line, _lineClone);

        }

        public void InitializeLineLocation()
        {
            //Algo
            //1 based index (follow line order 1,2,3, etc..)

            //RescueLine en dernière position (pour ne pas perturber l'ordre du feu avec couleur arrière plan différent)

            List<ComboBoxItem> item = new List<ComboBoxItem>();
            ComboBoxItem cbi = null;

            //int nbLine = 0;
            const string FIRST_POS = "1";

            string selectedItem = DO_NOT_MOVE_ID;
            string message = String.Empty;

            //LastPos = nbLine + 1
            int lastPos = _fireworkManager.ActiveLines.Count + 1;
            int lastPosWithRescue = _fireworkManager.ActiveLines.Count + _fireworkManager.RescueLines.Count + 1;

            int nbNormalLine = _fireworkManager.ActiveLines.Count;
            int nbRescueLine = _fireworkManager.RescueLines.Count;

            int nbLineTot = _fireworkManager.ActiveLines.Count + _fireworkManager.RescueLines.Count;
            
            if (_lineClone.IsRescueLine)
            {
                //Add rescule line
                if (_mode == WindowMode.Add)
                {

                    //CAS 1 - Pas de line existante
                    if (nbRescueLine == 0)
                    {
                        cbi = new ComboBoxItem(lastPos.ToString(), "Insérer en première position");
                        item.Add(cbi);

                        selectedItem = lastPos.ToString();
                    }

                    //CAS 2 - Une ligne
                    if (nbRescueLine == 1)
                    {
                        cbi = new ComboBoxItem(lastPos.ToString(), "Insérer en première position");
                        item.Add(cbi);

                        selectedItem = lastPos.ToString();

                        cbi = new ComboBoxItem(lastPosWithRescue.ToString(), "Insérer en dernière position");
                        item.Add(cbi);

                        selectedItem = lastPosWithRescue.ToString();
                    }

                    
                    //CAS 3 - Au moins deux lignes
                    if (nbRescueLine > 1)
                    {
                        cbi = new ComboBoxItem(lastPos.ToString(), "Insérer en première position");
                        item.Add(cbi);

                        for (int i = lastPos + 1; i < lastPosWithRescue; i++)
                        {
                            message = string.Format("Insérer entre la ligne {0} et {1}", i - 1, i);
                            cbi = new ComboBoxItem(i.ToString(), message);
                            item.Add(cbi);
                        }

                        cbi = new ComboBoxItem(lastPosWithRescue.ToString(), "Insérer en dernière position");
                        item.Add(cbi);

                        selectedItem = lastPosWithRescue.ToString();
                    }

                }
                // Move rescue line
                else
                {                    
                    //CAS 1 - Une ligne
                    if (nbRescueLine == 1)
                    {
                        cbi = new ComboBoxItem(DO_NOT_MOVE_ID, "Ne pas déplacer");
                        item.Add(cbi);

                        selectedItem = DO_NOT_MOVE_ID;
                    }

                    //Cas 2 - Deux lignes
                    if (nbRescueLine == 2)
                    {
                        cbi = new ComboBoxItem(DO_NOT_MOVE_ID, "Ne pas déplacer");
                        item.Add(cbi);

                        //User wants to edit first line
                        if (_line.Number == lastPos.ToString())
                        {
                            cbi = new ComboBoxItem(nbRescueLine.ToString(), "Déplacer en dernière position");
                            item.Add(cbi);
                        }

                        //User wants to edit second line
                        if (_line.Number == nbLineTot.ToString())
                        {
                            cbi = new ComboBoxItem(lastPos.ToString(), "Déplacer en première position");
                            item.Add(cbi);
                        }

                        selectedItem = DO_NOT_MOVE_ID;
                    }

                    //Cas 3 - > 2 lignes
                    if (nbRescueLine > 2)
                    {
                        cbi = new ComboBoxItem(DO_NOT_MOVE_ID, "Ne pas déplacer");
                        item.Add(cbi);

                        //User do not edit first line
                        if (_line.Number != lastPos.ToString())
                        {
                            cbi = new ComboBoxItem(lastPos.ToString(), "Déplacer en première position");
                            item.Add(cbi);
                        }

                        //User do not edit last line
                        if (_line.Number != nbLineTot.ToString())
                        {
                            cbi = new ComboBoxItem(nbLineTot.ToString(), "Déplacer en dernière position");
                            item.Add(cbi);
                        }

                        for(int i = lastPos; i < nbLineTot; i++)
                        {
                            if (i.ToString() != _line.Number && (i + 1).ToString() != _line.Number)
                            {

                                if (i > int.Parse(_line.Number))
                                {
                                    message = string.Format("Déplacer entre la ligne {0} et {1}", i, i + 1);
                                    cbi = new ComboBoxItem(i.ToString(), message);
                                    item.Add(cbi);
                                }
                                else
                                {
                                    message = string.Format("Déplacer entre la ligne {0} et {1}", i, i + 1);
                                    cbi = new ComboBoxItem((i + 1).ToString(), message);
                                    item.Add(cbi);
                                }
                            }
                        }

                        selectedItem = DO_NOT_MOVE_ID;
                    }
                }
            }
            //Not a rescue line
            else
            {
                //Add Normal line
                if (_mode == WindowMode.Add)
                {

                    //CAS 1 - Pas de line existante
                    if (nbNormalLine == 0)
                    {
                        cbi = new ComboBoxItem(FIRST_POS, "Insérer en première position");
                        item.Add(cbi);

                        selectedItem = FIRST_POS;
                    }

                    //CAS 2 - Une ligne
                    if (nbNormalLine == 1)
                    {
                        cbi = new ComboBoxItem(FIRST_POS, "Insérer en première position");
                        item.Add(cbi);

                        cbi = new ComboBoxItem(lastPos.ToString(), "Insérer en dernière position");
                        item.Add(cbi);

                        selectedItem = FIRST_POS;
                    }


                    //CAS 3 - Au moins deux lignes
                    if (nbNormalLine > 1)
                    {
                        cbi = new ComboBoxItem(FIRST_POS, "Insérer en première position");
                        item.Add(cbi);

                        for (int i = 2; i < lastPos; i++)
                        {
                            message = string.Format("Insérer entre la ligne {0} et {1}", i - 1, i);
                            cbi = new ComboBoxItem(i.ToString(), message);
                            item.Add(cbi);
                        }

                        cbi = new ComboBoxItem(lastPos.ToString(), "Insérer en dernière position");
                        item.Add(cbi);

                        selectedItem = FIRST_POS;
                    }

                }
                // Move line
                else
                {
                    //CAS 1 - Une ligne
                    if (nbNormalLine == 1)
                    {
                        cbi = new ComboBoxItem(DO_NOT_MOVE_ID, "Ne pas déplacer");
                        item.Add(cbi);

                        selectedItem = DO_NOT_MOVE_ID;
                    }

                    //Cas 2 - Deux lignes
                    if (nbNormalLine == 2)
                    {
                        cbi = new ComboBoxItem(DO_NOT_MOVE_ID, "Ne pas déplacer");
                        item.Add(cbi);

                        //User wants to edit first line
                        if (_line.Number == "1")
                        {
                            cbi = new ComboBoxItem(nbNormalLine.ToString(), "Déplacer en dernière position");
                            item.Add(cbi);
                        }

                        //User wants to edit second line
                        if (_line.Number == "2")
                        {
                            cbi = new ComboBoxItem(FIRST_POS, "Déplacer en première position");
                            item.Add(cbi);
                        }

                        selectedItem = DO_NOT_MOVE_ID;
                    }

                    //Cas 3 - > 2 lignes
                    if (nbNormalLine > 2)
                    {
                        cbi = new ComboBoxItem(DO_NOT_MOVE_ID, "Ne pas déplacer");
                        item.Add(cbi);

                        //User do not edit first line
                        if (_line.Number != "1")
                        {
                            cbi = new ComboBoxItem(FIRST_POS, "Déplacer en première position");
                            item.Add(cbi);
                        }

                        //User do not edit last line
                        if (_line.Number != nbNormalLine.ToString())
                        {
                            cbi = new ComboBoxItem(nbNormalLine.ToString(), "Déplacer en dernière position");
                            item.Add(cbi);
                        }

                        for (int i = 1; i < nbNormalLine; i++)
                        {
                            if (i.ToString() != _line.Number && (i + 1).ToString() != _line.Number)
                            {

                                if (i > int.Parse(_line.Number))
                                {
                                    message = string.Format("Déplacer entre la ligne {0} et {1}", i, i + 1);
                                    cbi = new ComboBoxItem(i.ToString(), message);
                                    item.Add(cbi);
                                }
                                else
                                {
                                    message = string.Format("Déplacer entre la ligne {0} et {1}", i, i + 1);
                                    cbi = new ComboBoxItem((i + 1).ToString(), message);
                                    item.Add(cbi);
                                }
                            }
                        }

                        selectedItem = DO_NOT_MOVE_ID;
                    }
                }
            }

            LineLocation = item;
            SelectedLineLocation = selectedItem;
                       

        }
    }
}