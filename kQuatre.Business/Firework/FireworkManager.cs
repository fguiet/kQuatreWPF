﻿using Guiet.kQuatre.Business.Configuration;
using Guiet.kQuatre.Business.Exceptions;
using Guiet.kQuatre.Business.Receptor;
using Guiet.kQuatre.Business.Transceiver;
using Guiet.kQuatre.Business.Transceiver.Frames;
using Infragistics.Documents.Excel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Timers;
using System.Xml.Linq;

namespace Guiet.kQuatre.Business.Firework
{

    /// <summary>
    /// Handle firework system
    /// TODO : Create base class for INotifyPropertyChanged
    /// </summary>
    public class FireworkManager : INotifyPropertyChanged
    {
        #region Private members        

        /// <summary>
        /// Each receptor owns a mac address. Each receptor has got x channels. A channel is linked to a firework line
        /// </summary>
        private ObservableCollection<Guiet.kQuatre.Business.Receptor.Receptor> _receptors = new ObservableCollection<Guiet.kQuatre.Business.Receptor.Receptor>();

        /// <summary>
        /// Firework is a collection of lines, each lines is made up of firework(s)
        /// A line is attached to an address of a receptor (receptor address + channel)
        /// </summary>
        private ObservableCollection<Line> _lines = new ObservableCollection<Line>();

        /// <summary>
        /// Software configuration
        /// </summary>
        private SoftwareConfiguration _configuration = null;

        /// <summary>
        /// Firework name
        /// </summary>
        private string _name = null;

        /// <summary>
        /// Backgoundworker which manages firework
        /// </summary>
        private BackgroundWorker _fireworkWorker = null;

        /// <summary>
        /// Device manager (radio controller)
        /// </summary>
        private DeviceManager _deviceManager = null;

        /// <summary>
        /// Elapsed time since firework has begun
        /// </summary>
        private Stopwatch _elapsedTime = null;

        private System.Timers.Timer _timerHelper = null;

        private string _elapsedTimeString = null;

        private bool _isLoadingFromFile = false;

        private ObservableCollection<Firework> _allFireworks = new ObservableCollection<Firework>();

        /// <summary>
        /// Tells whether firework has been edited by user
        /// </summary>
        private bool _isDirty = false;

        private FireworkManagerState _state = FireworkManagerState.Editing;

        private string _fireworkFullFileName = DEFAULT_FIREWORK_NAME;

        private const string DEFAULT_K4_EXTENSION = ".k4";
        private const string DEFAULT_FIREWORK_NAME = "NouveauFeu";

        #endregion

        #region Events

        public event EventHandler LineStarted;
        public event EventHandler LineFailed;

        private void OnLineFailedEvent(object sender)
        {
            if (LineFailed != null)
            {
                LineFailed(sender, new EventArgs());
            }
        }

        private void OnLineStartedEvent(object sender)
        {
            if (LineStarted != null)
            {
                LineStarted(sender, new EventArgs());
            }
        }

        public event EventHandler<FireworkStateChangedEventArgs> FireworkStateChanged;

        private void OnFireworkStateChanged(Firework firework, string propertyName)
        {
            if (FireworkStateChanged != null)
            {
                FireworkStateChanged(this, new FireworkStateChangedEventArgs(firework, propertyName));
            }
        }

        #endregion

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

        public bool IsNew
        {
            get
            {
                return (_fireworkFullFileName == DEFAULT_FIREWORK_NAME);
            }
        }

        public string FireworkFullFileName
        {
            get
            {                
                return _fireworkFullFileName;                
            }

            set
            {
                if (_fireworkFullFileName != value)
                {
                    _fireworkFullFileName = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsFireworkEditing
        {
            get
            {
                return (State == FireworkManagerState.Editing);
            }
        }

        public bool IsFireworkInProgress
        {
            get
            {
                return (State == FireworkManagerState.FireInProgress);
            }
        }

        public FireworkManagerState State
        {
            get
            {
                return _state;
            }

            set
            {
                _state = value;
                OnPropertyChanged();
                //So Gui is aware of changement...
                OnPropertyChanged("IsFireworkInProgress");
                OnPropertyChanged("IsFireworkEditing");
            }
        }

        public ObservableCollection<Firework> AllFireworks
        {
            get
            {
                return _allFireworks;
            }
        }

        public DateTime DefaultPeriodStartUI
        {
            get
            {
                return DateTime.Now.Date;
            }
        }

        public DateTime DefaultPeriodEndUI
        {
            get
            {
                return DateTime.Now.Date.Add(new TimeSpan(0, 1, 0));
            }
        }

        public DateTime PeriodStartUI
        {
            get
            {
                return DateTime.Now.Date;
            }
        }
        
        public DateTime PeriodEndUI
        {
            get
            {
                //Add 10 seconds so graph is does not end abrutly
                return DateTime.Now.Date.Add(TotalDuration).Add(new TimeSpan(0, 0, 5));
            }
        }

        public TimeSpan TotalDuration
        {
            get
            {
                if (_lines == null || _lines.Count == 0) return new TimeSpan(0, 0, 0);

                TimeSpan sp = new TimeSpan(0, 0, 0);
                TimeSpan maxSp = new TimeSpan(0, 0, 0);
                foreach (Line l in _lines)
                {
                    if (l.LongestFireworkDuration.HasValue)
                        sp = l.Ignition.Add(l.LongestFireworkDuration.Value);
                    else
                        sp = l.Ignition;

                    if (sp.CompareTo(maxSp) == 1)
                    {
                        maxSp = sp;
                    }
                }

                return maxSp;

            }
        }

        public string TotalDurationUI
        {
            get
            {
                return $"{TotalDuration:hh\\:mm\\:ss}";
            }
        }

        public string ElapsedTimeString
        {
            get
            {
                if (string.IsNullOrEmpty(_elapsedTimeString))
                    return "00:00:00";
                else
                    return _elapsedTimeString;
            }
            set
            {
                if (_elapsedTimeString != value)
                {
                    _elapsedTimeString = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (_name != value)
                {
                    _name = value;
                    MakeItDirty(true);
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Guiet.kQuatre.Business.Receptor.Receptor> Receptors
        {
            get
            {
                return _receptors;
            }
        }

        public List<ReceptorAddress> ReceptorAddressesAvailable
        {
            get
            {
                List<ReceptorAddress> ra = new List<ReceptorAddress>();

                foreach (Receptor.Receptor r in _receptors)
                {
                    ra.AddRange(r.ReceptorAddressesAvailable);
                }

                return ra;

            }
        }       

        public ObservableCollection<Line> Lines
        {
            get
            {
                return _lines;
            }

        }

        #region Constructor

        public FireworkManager(SoftwareConfiguration configuration,
                               DeviceManager deviceManager)
        {
            _configuration = configuration;
            _deviceManager = deviceManager;

            _deviceManager.DeviceConnected += DeviceManager_DeviceConnected;
            _deviceManager.DeviceDisconnected += DeviceManager_DeviceDisconnected;

            //Set default receptors
            _receptors = new ObservableCollection<Receptor.Receptor>();

            foreach (Receptor.Receptor r in _configuration.DefaultReceptors)
            {
                _receptors.Add(r);
            }
        }

        private void DeviceManager_DeviceDisconnected(object sender, EventArgs e)
        {
            foreach (Receptor.Receptor r in _receptors)
            {
                r.SetDeviceManager(null);
            }
        }

        private void DeviceManager_DeviceConnected(object sender, ConnectionEventArgs e)
        {
            foreach (Receptor.Receptor r in _receptors)
            {
                r.SetDeviceManager(_deviceManager);
            }
        }

        #endregion

        #region Event

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// TODO : Implement this
        /// </summary>
        /// <returns></returns>
        public bool SanityCheck()
        {
            //check no line with no fireworks!!
            //check line ignition time order!!
            //check line with no receptor address!!

            return false;
        }

        /// <summary>
        /// Stops firework !!
        /// </summary>
        public void Stop()
        {       
            //User ask to stop firework in this case...So stop it properly
            //Stop firework and line properly
            foreach(Line l in _lines)
            {
                l.Stop();
            }

            if (_fireworkWorker.IsBusy)
                _fireworkWorker.CancelAsync();
        }

        /// <summary>
        /// Start firework !!!
        /// </summary>
        public void Start()
        {
            //begins by reseting line and firework (case when user stop and restart firework)
            Reset();

            State = FireworkManagerState.FireInProgress;

            _fireworkWorker = new BackgroundWorker();
            _fireworkWorker.WorkerSupportsCancellation = true;
            _fireworkWorker.DoWork += FireworkWorker_DoWork;
            _fireworkWorker.RunWorkerCompleted += FireworkWorker_RunWorkerCompleted;
            _fireworkWorker.RunWorkerAsync();
        }

        public void DeleteLine(Line line)
        {
            //Unassign receptor if any
            line.UnassignReceptorAddress();

            _lines.Remove(line);

            //Reorder lines!!
            ReorderLines();
        }

        public void AddOrUpdateLine(bool isAdd, Line line, Line lineClone)
        {
            int oldIndex = int.Parse(line.Number);

            line.UpdateFromClone(lineClone);

            if (isAdd)
            {
                int index = int.Parse(line.Number);

                if (index > _lines.Count)
                    _lines.Add(line);
                else
                    _lines.Insert(int.Parse(line.Number), line);

                MakeItDirty(true);
            }
            else
            {
                int newIndex = int.Parse(line.Number);

                if (newIndex > _lines.Count)
                    newIndex = _lines.Count - 1;

                if (oldIndex != newIndex)
                {
                    _lines.Move(oldIndex - 1, newIndex);
                    MakeItDirty(true);
                }
            }

            //Reorder lines!!
            ReorderLines();
        }

        /// <summary>
        /// Load a new firework in memory from xml definition file
        /// </summary>
        /// <param name="fullFileName"></param>
        public void LoadFirework(string fullFilename)
        {
            
            try
            {
                _isLoadingFromFile = true;
                                
                //Clear default loaded receptors
                _receptors.Clear();

                _lines = new ObservableCollection<Line>();

                //Load a new firework xml definition file
                XDocument fireworkDefinition = XDocument.Load(fullFilename);

                //Firework name
                _name = fireworkDefinition.Element("FireworkDefinition").Attribute("fireworkName").Value.ToString();

                //Load firework receptors definition
                List<XElement> receptors = (from r in fireworkDefinition.Descendants("Receptor")
                                            select r).ToList();


                foreach (XElement r in receptors)
                {
                    Guiet.kQuatre.Business.Receptor.Receptor recep = new Guiet.kQuatre.Business.Receptor.Receptor(r.Attribute("name").Value, r.Attribute("address").Value.ToString(), Convert.ToInt32(r.Attribute("nbOfChannels").Value.ToString()));
                    _receptors.Add(recep);
                }

                //Parcours des lignes et création des artifices
                List<XElement> lines = (from l in fireworkDefinition.Descendants("Line")
                                        select l).ToList();

                int fireworkNumber = 0;
                foreach (XElement l in lines)
                {
                    int lineNumber = Convert.ToInt32(l.Attribute("number").Value.ToString());
                    Line line = new Line(lineNumber);

                    line.LineStarted += Line_LineStarted;
                    line.LineFailed += Line_LineFailed;
                    line.PropertyChanged += Line_PropertyChanged;

                    if (l.Attribute("ignition") != null)
                    {
                        TimeSpan ignition = TimeSpan.Parse(l.Attribute("ignition").Value.ToString());
                        line.Ignition = ignition;
                    }

                    if (l.Element("ReceptorAddress") != null)
                    {
                        string address = l.Element("ReceptorAddress").Attribute("address").Value.ToString();
                        string channelNumber = l.Element("ReceptorAddress").Attribute("channel").Value.ToString();

                        Guiet.kQuatre.Business.Receptor.Receptor receptor = GetReceptor(address);
                        Guiet.kQuatre.Business.Receptor.ReceptorAddress ra = receptor.GetAddress(Convert.ToInt32(channelNumber));

                        //TODO : A revoir
                        receptor.SetDeviceManager(_deviceManager);

                        line.AssignReceptorAddress(ra);
                    }

                    List<XElement> fireworks = (from f in l.Descendants("Firework")
                                                select f).ToList();
                    foreach (XElement f in fireworks)
                    {
                        //TODO : reactivate !!
                        //string reference = f.Attribute("reference").Value.ToString();
                        string reference = "";
                        string designation = f.Attribute("designation").Value.ToString();
                        TimeSpan duration = TimeSpan.Parse(f.Attribute("duration").Value.ToString());

                        Firework firework = new Firework(fireworkNumber, reference, designation, duration, line);

                        _allFireworks.Add(firework);
                        line.AddFirework(firework);

                        fireworkNumber++;

                    }

                    _lines.Add(line);
                }

            }
            catch(Exception e)
            {
                throw e;
            }
            finally
            {
                _isLoadingFromFile = false;
            }

            //Set new name here !
            _fireworkFullFileName = fullFilename;            
        }

        private void Line_LineFailed(object sender, EventArgs e)
        {
            OnLineFailedEvent(sender);
        }

        private void Line_LineStarted(object sender, EventArgs e)
        {
            OnLineStartedEvent(sender);
        }

        /// <summary>
        /// Load firework from adhoc Excel file 
        /// </summary>
        /// <param name="fullFileName"></param>
        /// <param name="ui"></param>
        /// <returns></returns>
        public void LoadFireworkFromExcel(string fullFileName)
        {            
            try
            {
                _isLoadingFromFile = true;
                
                _lines = new ObservableCollection<Line>();

                Workbook fireworkDefWb = Workbook.Load(fullFileName);

                Worksheet fireworkDefWs = fireworkDefWb.Worksheets[_configuration.ExcelSheetNumber];

                _name = fireworkDefWs.GetCell(_configuration.ExcelFireworkName).GetText();

                //TODO : Column number should be parameterized!!
                int firstRowDataIndex = _configuration.ExcelFirstRowData;
                int rowIndex = 1;
                Line line = null;
                int fireworkOrderNumber = 0;
                foreach (WorksheetRow row in fireworkDefWs.Rows)
                {
                    if (rowIndex >= firstRowDataIndex)
                    {
                        //end of firework definition...no more line number...
                        if (row.GetCellText(0) == string.Empty) break;

                        //First line number
                        if (GetLineByNumber(row.GetCellText(0)) == null)
                        {
                            line = new Line(Convert.ToInt32(row.GetCellText(0)));

                            line.LineStarted += Line_LineStarted;
                            line.LineFailed += Line_LineFailed;
                            line.PropertyChanged += Line_PropertyChanged;

                            line.Ignition = TimeSpan.Parse(row.GetCellText(1));
                            _lines.Add(line);
                        }

                        //Get Data for firework
                        string reference = row.GetCellText(4);
                        string designation = row.GetCellText(7);
                        TimeSpan duration = TimeSpan.Parse(row.GetCellText(5));
                        Firework firework = new Firework(fireworkOrderNumber, reference, designation, duration, line);

                        _allFireworks.Add(firework);
                        line.AddFirework(firework);

                        fireworkOrderNumber++;
                    }

                    rowIndex++;
                }                
            }
            catch(Exception e)
            {
                throw e;
            }
            finally
            {
                _isLoadingFromFile = false;
            }

            //Set new name here !
            _fireworkFullFileName = String.Format("{0}{1}", System.IO.Path.GetFileNameWithoutExtension(fullFileName), DEFAULT_K4_EXTENSION);                     
        }

        
        private void Line_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MakeItDirty(true);
        }

        private void MakeItDirty(bool isDirty)
        {
            if (_isLoadingFromFile) return;            

            IsDirty = isDirty;
        }

        public void SaveFirework()
        {
            SaveFirework(_fireworkFullFileName);
        }

        /// <summary>
        /// Save firework definition in XML format
        /// </summary>
        /// <param name="fullFilename"></param>
        public void SaveFirework(string fullFilename)
        {

            try
            {

                XDocument doc = new XDocument();

                //Firework name
                XElement fd = new XElement("FireworkDefinition", new XAttribute("fireworkName", _name));

                //Receptors
                XElement r = new XElement("Receptors",
                            _receptors.Select(x => new XElement("Receptor", new XAttribute("name", x.Name), new XAttribute("address", x.Address), new XAttribute("nbOfChannels", x.NbOfChannel)))
                         );

                //Lines
                XElement lines = new XElement("Lines");

                foreach (Line l in _lines)
                {
                    XElement line = new XElement("Line",
                                                new XAttribute("number", l.Number),
                                                new XAttribute("ignition", $"{l.Ignition:hh\\:mm\\:ss}")
                                                );

                    //Receptor if any...
                    if (l.ReceptorAddress != null)
                    {
                        line.Add(new XElement("ReceptorAddress", new XAttribute("address", l.ReceptorAddress.Address), new XAttribute("channel", l.ReceptorAddress.Channel.ToString())));
                    }

                    //Fireworks
                    XElement fireworks = new XElement("Fireworks");

                    foreach (Firework la in l.Fireworks)
                    {
                        XElement firework = new XElement("Firework",
                                new XAttribute("designation", la.Designation),
                                new XAttribute("duration", $"{la.Duration:hh\\:mm\\:ss}")
                                );


                        fireworks.Add(firework);
                    }

                    line.Add(fireworks);

                    lines.Add(line);
                }

                //add receptors
                fd.Add(r);

                //add lines
                fd.Add(lines);

                //add to xml doc
                doc.Add(fd);

                doc.Save(fullFilename);

                //No more dirty here
                MakeItDirty(false);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion

        #region Private Methods

        private void Reset()
        {
            //begins by reseting line and firework (case when user stop and restart firework)
            foreach (Line l in _lines)
            {
                l.Reset();
            }
        }

        private void ReorderLines()
        {
            int lineNumber = 1;

            foreach (Line line in _lines)
            {
                line.Reorder(lineNumber);

                lineNumber++;
            }
        }

        /// <summary>
        /// Returns true if all lines are in finished state
        /// </summary>
        /// <returns></returns>
        private bool IsAllLineFinished()
        {
            int nb = (from l in _lines
                      where l.IsFinished == true
                      select l).Count();

            return (nb == _lines.Count);
        }

        private LineHelper PrepareNextLines()
        {
            //Gets next line to launch!
            Line line = (from l in _lines
                         where l.State == LineState.Standby
                         orderby l.Ignition.Milliseconds
                         select l).FirstOrDefault();

            //Maybe no more line to launch...firework is finished...
            if (line == null) return null;

            //Maybe another line is launched at the same time!
            List<Line> lines = (from l in _lines
                                where l.State == LineState.Standby
                                && l.Ignition.TotalMilliseconds == line.Ignition.TotalMilliseconds
                                select l).ToList();

            LineHelper helper = new LineHelper(lines);

            return helper;
        }

        private void FireworkWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            _elapsedTime = new Stopwatch();
            _elapsedTime.Start();

            _timerHelper = new System.Timers.Timer();
            _timerHelper.Interval = 1000;
            _timerHelper.Elapsed += TimerHelper_Elapsed;
            _timerHelper.Start();


            LineHelper lineHelper = null;
            bool prepareNextLines = true;
            while (!IsAllLineFinished())
            {
                //Cancel pending?
                if (_fireworkWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                if (prepareNextLines)
                {
                    prepareNextLines = false;
                    //Get next lines with same launch time
                    lineHelper = PrepareNextLines();
                }

                //No more line to launch...wait for current firework to finish
                if (lineHelper == null) continue;

                if (_elapsedTime.ElapsedMilliseconds >= lineHelper.Ignition)
                {
                    prepareNextLines = true;

                    foreach (KeyValuePair<string, List<Line>> message in lineHelper.LinesGroupByReceptorAddress)
                    {
                        AckFrame af = null;

                        try
                        {
                            //Send Message here and get result
                            FireFrame pf = new FireFrame(_deviceManager.Transceiver.Address, message.Value[0].ReceptorAddress.Address, LineHelper.GetFireMessage(message.Value));
                            FrameBase fb = _deviceManager.Transceiver.SendPacketSynchronously(pf);

                            af = (AckFrame)fb;
                        }
                        catch (TimeoutPacketException)
                        {
                            af = null;
                        }

                        if (null != af && af.IsAckOk)
                        {
                            //if result ok, start line, otherwise, set status failed
                            //and proceed immediately to next firework (back in future)
                            //take into account retry
                            foreach (Line line in message.Value)
                            {
                                line.Start();
                            }
                        }
                        else
                        {
                            foreach (Line line in message.Value)
                            {
                                line.SetFailed();
                            }
                        }
                    }
                }

                //Dunno why it is here...
                //Thread.Sleep(250);
            }
        }

        private void TimerHelper_Elapsed(object sender, ElapsedEventArgs e)
        {
            ElapsedTimeString = $"{_elapsedTime.Elapsed:hh\\:mm\\:ss}";
        }

        /// <summary>
        /// Firework is over
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FireworkWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            State = FireworkManagerState.Editing;

            _elapsedTime.Stop();
            _elapsedTime = null;

            _timerHelper.Stop();
            _timerHelper = null;
        }

       
        private void Firework_FireworkStateChanged(object sender, FireworkStateChangedEventArgs e)
        {
            OnFireworkStateChanged(e.Firework, e.PropertyName);
        }

        /// <summary>
        /// Get line by its number
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private Line GetLineByNumber(string number)
        {
            return (from l in _lines
                    where l.Number == number
                    select l).FirstOrDefault();
        }

        /// <summary>
        /// Get receptor via its address        
        /// </summary>
        /// <param name="address"></param>
        private Guiet.kQuatre.Business.Receptor.Receptor GetReceptor(string address)
        {
            Guiet.kQuatre.Business.Receptor.Receptor receptor = (from r in _receptors
                                                                 where r.Address == address
                                                                 select r).FirstOrDefault();

            if (receptor == null)
            {
                throw new CannotFindReceptorException(string.Format("Cannot find receptor with address : {0}", address));
            }

            return receptor;
        }

        #endregion
    }
}
