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

        private ObservableCollection<Firework> _allFireworks = new ObservableCollection<Firework>();

        private bool _isDirty = false;

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
        /// Start firework !!!
        /// </summary>
        public void Start()
        {
            _fireworkWorker = new BackgroundWorker();
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

                _isDirty = true;
            }
            else
            {
                int newIndex = int.Parse(line.Number);

                if (newIndex > _lines.Count)
                    newIndex = _lines.Count - 1;

                if (oldIndex != newIndex)
                {
                    _lines.Move(oldIndex - 1, newIndex);
                    _isDirty = true;
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
            //TODO : Surround with try/catch

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

            //GenerateGanttDataSource();
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
        public bool LoadFireworkFromExcel(string fullFileName)
        {
            try
            {
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
            catch
            {
                return false;
            }

            return true;
        }

        
        private void Line_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _isDirty = true;
        }

        /// <summary>
        /// Save firework definition in XML format
        /// </summary>
        /// <param name="fullFilename"></param>
        public void SaveFirework(string fullFilename)
        {

            //TODO : Surround with try/catch and handle it!!

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
        }

        #endregion

        #region Private Methods

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

                Thread.Sleep(250);
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
            _elapsedTime.Stop();
            _elapsedTime = null;

            _timerHelper.Stop();
            _timerHelper = null;
        }

        //TODO : !!! Delete Dispatcher UI from here, 
        /*private void GenerateGanttDataSource()
        {
            DateTime startTime = DateTime.Today.ToUniversalTime();

            ObservableCollection<TaskModel> ganttDataSource = new ObservableCollection<TaskModel>();

            int idTask = 0;

            TaskModel rootTask = new TaskModel();

            idTask++;
            rootTask.TaskID = idTask;
            rootTask.Name = _name;
            ganttDataSource.Add(rootTask);

            foreach (Line line in _lines)
            {
                DateTime lineStartTime = startTime.Add(line.Ignition);

                TaskModel tmLine = new TaskModel();

                idTask++;
                tmLine.TaskID = idTask;
                tmLine.Name = line.NumberUI;
                tmLine.IsManualTask = true;
                tmLine.StartTime = lineStartTime;

                tmLine.Format = ProjectDurationFormat.Seconds;

                if (line.LongestFireworkDuration.HasValue)
                {
                    tmLine.Length = line.LongestFireworkDuration.Value;
                }

                string childTask = string.Empty;
                foreach (Firework firework in line.Fireworks)
                {
                    idTask++;
                    TaskModel tm = new TaskModel();
                    tm.TaskID = idTask;
                    tm.IsManualTask = true;
                    tm.Name = firework.Designation;
                    tm.StartTime = lineStartTime;

                    tm.Length = firework.Duration;
                    tm.Format = ProjectDurationFormat.Seconds;
                    tm.TaskBrush = new SolidColorBrush(Colors.Gray);
                    firework.SetTaskModel(tm);
                    firework.FireworkStateChanged += Firework_FireworkStateChanged;

                    if (string.IsNullOrEmpty(childTask))
                    {
                        childTask = idTask.ToString();
                    }
                    else
                    {
                        childTask = childTask + "," + idTask.ToString();
                    }

                    ganttDataSource.Add(tm);

                }

                tmLine.ChildTasks = childTask;
                ganttDataSource.Add(tmLine);

                if (string.IsNullOrEmpty(rootTask.ChildTasks))
                    rootTask.ChildTasks = tmLine.TaskID.ToString();
                else
                    rootTask.ChildTasks = rootTask.ChildTasks + "," + tmLine.TaskID.ToString();
            }

            GanttDataSource = ganttDataSource;
        }*/

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
