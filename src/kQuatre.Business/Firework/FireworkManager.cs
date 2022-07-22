using fr.guiet.lora.events;
using fr.guiet.lora.frames;
using fr.guiet.kquatre.business.configuration;
using fr.guiet.kquatre.business.exceptions;
using fr.guiet.kquatre.business.receptor;
using fr.guiet.kquatre.business.transceiver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using OfficeOpenXml;

namespace fr.guiet.kquatre.business.firework
{

    /// <summary>
    /// Handle firework system
    /// TODO : Create base class for INotifyPropertyChanged
    /// </summary>
    public class FireworkManager : INotifyPropertyChanged
    {
        #region Private members           

        private LineHelper _lineHelperRescue = null;
        private LineHelper _lineHelperFailed = null;

        /// <summary>
        /// Each receptor owns a mac address. Each receptor has got x channels. A channel is linked to a firework line
        /// </summary>
        private ObservableCollection<Receptor> _receptors = null; //new ObservableCollection<fr.guiet.kquatre.business.receptor.Receptor>();

        /// <summary>
        /// Firework is a collection of lines, each lines is made up of firework(s)
        /// A line is attached to an address of a receptor (receptor address + channel)
        /// </summary>
        private ObservableCollection<Line> _lines = null; //= new ObservableCollection<Line>();

        /// <summary>
        /// Software configuration
        /// </summary>
        private readonly SoftwareConfiguration _configuration = null;

        /// <summary>
        /// Firework name
        /// </summary>
        private string _name = string.Empty;

        /// <summary>
        /// Token used to cancel firework
        /// </summary>
        private CancellationTokenSource _fireworkCancellationToken = null;

        /// <summary>
        /// Device manager (radio controller)
        /// Initialized once
        /// </summary>
        private readonly DeviceManager _deviceManager = null;

        /// <summary>
        /// Handles FireworkSoundTrack
        /// </summary>
        private SoundTrackManager _soundTrackManager = new SoundTrackManager();

        /// <summary>
        /// Elapsed time since firework has begun
        /// </summary>
        private Stopwatch _elapsedTime = null;

        private System.Timers.Timer _timerHelper = null;

        private string _elapsedTimeString = null;

        private string _nextLaunchCountDownString = null;

        private bool _isLoadingFromFile = false;

        private bool _isSanityCheckOk = false;

        private TimeSpan _nextIgnition = new TimeSpan(0, 0, 0);

        //private Dictionary<string, List<Line>> _lastLinesLaunchFailed = null; 

        //private bool _isRedoFailedEnable = false;

        //private bool _activateRedoFailedLine = false;

        //private ObservableCollection<Firework> _allFireworks = null;

        private List<string> _sanityCheckErrorsList = null;

        /// <summary>
        /// Tells whether firework has been edited by user
        /// </summary>
        private bool _isDirty = false;

        /// <summary>
        /// Firework state
        /// </summary>
        private FireworkManagerState _state = FireworkManagerState.FireworkStopped;

        private string _fireworkFullFileName = DEFAULT_FIREWORK_NAME;

        private const string DEFAULT_K4_EXTENSION = ".k4";
        private const string DEFAULT_FIREWORK_NAME = "NouveauFeu";

        #endregion

        #region Events

        public event EventHandler TimerElapsed;
        public event EventHandler LineStarted;
        public event EventHandler LineFailed;
        public event EventHandler FireworkFinished;
        public event EventHandler FireworkStarted;
        public event EventHandler FireworkLoaded;
        public event EventHandler FireworkSaved;
        public event EventHandler FireworkDefinitionModified;
        public event EventHandler<TransceiverInfoEventArgs> TransceiverInfoChanged;
        public event EventHandler TransceiverDisconnected;
        public event EventHandler TransceiverConnected;
        public event EventHandler ReceptorTestStarted;
        public event EventHandler ReceptorTestFinished;

        private void OnReceptorTestStartedEvent()
        {
            ReceptorTestStarted?.Invoke(this, new EventArgs());
        }

        private void OnReceptorTestFinishedEvent()
        {
            ReceptorTestFinished?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Occured when device manager send any events...
        /// </summary>
        private void OnTransceiverInfoChangedEvent(string transceiverInfo)
        {
            TransceiverInfoChanged?.Invoke(this, new TransceiverInfoEventArgs(transceiverInfo));
        }

        private void OnTransceiverConnectedEvent()
        {
            TransceiverConnected?.Invoke(this, new EventArgs());
        }

        private void OnTransceiverDisconnectedEvent()
        {
            TransceiverDisconnected?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Occured when firework definition is modified
        /// </summary>
        private void OnFireworkDefinitionModifiedEvent()
        {
            FireworkDefinitionModified?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Se produit lorsqu'un feu est chargé
        /// </summary>
        private void OnFireworkLoadedEvent()
        {
            FireworkLoaded?.Invoke(this, new EventArgs());
        }

        private void OnFireworkSavedEvent()
        {
            FireworkSaved?.Invoke(this, new EventArgs());
        }


        private void OnFireworkStartedEvent()
        {
            FireworkStarted?.Invoke(this, new EventArgs());
        }

        private void OnFireworkFinishedEvent()
        {
            FireworkFinished?.Invoke(this, new EventArgs());
        }

        private void OnLineFailedEvent(object sender)
        {
            LineFailed?.Invoke(sender, new EventArgs());
        }

        private void OnTimerElapsedEvent(object sender)
        {
            TimerElapsed?.Invoke(sender, new EventArgs());
        }

        public List<string> SanityCheckErrorsList
        {
            get
            {
                return _sanityCheckErrorsList;
            }
        }

        private void OnLineStartedEvent(object sender)
        {
            LineStarted?.Invoke(sender, new EventArgs());
        }

        #endregion

        public bool IsSanityCheckOk
        {
            get
            {
                SanityCheck();
                return _isSanityCheckOk;
            }
        }

        public bool IsDirty
        {
            get
            {
                return _isDirty;
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

        public bool HasSoundTrackToPlay
        {
            get
            {
                return _soundTrackManager.HasSoundTrackToPlay() && _soundTrackManager.IsSoundTrackSanityCheckOk();
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
                if (_state != value)
                {
                    _state = value;

                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Firework> AllActiveFireworks
        {
            get
            {
                IOrderedEnumerable<Firework> allFireworks = (ActiveLines.ToList().SelectMany(l => l.Fireworks).ToList()).OrderBy(y => y.FireworkNumber);

                return new ObservableCollection<Firework>(allFireworks);
            }
        }

        public ObservableCollection<Firework> AllRescueFireworks
        {
            get
            {
                IOrderedEnumerable<Firework> allFireworks = (RescueLines.ToList().SelectMany(l => l.Fireworks).ToList()).OrderBy(y => y.FireworkNumber);

                return new ObservableCollection<Firework>(allFireworks);
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
                if (ActiveLines == null || ActiveLines.Count == 0) return new TimeSpan(0, 0, 0);

                TimeSpan sp = new TimeSpan(0, 0, 0);
                TimeSpan maxSp = new TimeSpan(0, 0, 0);
                foreach (Line l in ActiveLines)
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
                return $"{TotalDuration:mm\\:ss}";
            }
        }

        /// <summary>
        /// Firework Elapsed time (TimeSpan)
        /// </summary>
        public TimeSpan ElapsedTime
        {
            get
            {
                return _elapsedTime.Elapsed;
            }
        }

        public string ElapsedTimeString
        {
            get
            {
                if (string.IsNullOrEmpty(_elapsedTimeString))
                    return "00:00";
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

        public string NextLaunchCountDownString
        {
            get
            {
                if (string.IsNullOrEmpty(_nextLaunchCountDownString))
                    return "00:00";
                else
                    return _nextLaunchCountDownString;
            }
            set
            {
                if (_nextLaunchCountDownString != value)
                {
                    _nextLaunchCountDownString = value;
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

        public string SoundTrackUI
        {
            get
            {
                return _soundTrackManager.SoundTrackFilePath;
            }

            set
            {
                if (_soundTrackManager.SoundTrackFilePath != value)
                {
                    _soundTrackManager.SoundTrackFilePath = value;
                    _soundTrackManager.Load(value);
                    MakeItDirty(true);
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<fr.guiet.kquatre.business.receptor.Receptor> Receptors
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

                foreach (Receptor r in _receptors)
                {
                    ra.AddRange(r.ReceptorAddressesAvailable);
                }

                return ra;
            }
        }

        /// <summary>
        /// Return rescue and active lines
        /// </summary>
        public ObservableCollection<Line> AllLines
        {
            get
            {
                return _lines;
            }
        }

        /// <summary>
        /// Return only rescue lines
        /// </summary>
        public ObservableCollection<Line> RescueLines
        {
            get
            {
                var rescueLines = (from al in _lines
                                   where al.IsRescueLine == true
                                   select al).ToList();

                return new ObservableCollection<Line>(rescueLines);
            }

        }

        /// <summary>
        /// Return only active lines (not the rescue ones)
        /// </summary>
        public ObservableCollection<Line> ActiveLines
        {
            get
            {
                var activeLines = (from al in _lines
                                   where al.IsRescueLine == false
                                   select al).ToList();

                return new ObservableCollection<Line>(activeLines);
            }
        }


        #region Constructor

        public FireworkManager(SoftwareConfiguration configuration)
        {
            _configuration = configuration;

            _deviceManager = new DeviceManager(_configuration);
            _deviceManager.DeviceConnected += DeviceManager_DeviceConnected;
            _deviceManager.DeviceDisconnected += DeviceManager_DeviceDisconnected;
            _deviceManager.DeviceErrorWhenConnecting += DeviceManager_DeviceErrorWhenConnecting;
            _deviceManager.USBConnection += DeviceManager_USBConnection;

            BeginNewFirework();
        }

        /// <summary>
        /// Initialize a new firework...reset all properties
        /// </summary>
        private void BeginNewFirework()
        {
            _lineHelperRescue = null;
            _lineHelperFailed = null;

            _lines = new ObservableCollection<Line>();

            _name = string.Empty;

            _fireworkCancellationToken = null;

            _elapsedTime = null;

            _timerHelper = null;

            _elapsedTimeString = null;

            _nextLaunchCountDownString = null;

            _isLoadingFromFile = false;

            _isSanityCheckOk = false;

            _nextIgnition = new TimeSpan(0, 0, 0);

            _sanityCheckErrorsList = null;

            _isDirty = false;

            _state = FireworkManagerState.FireworkStopped;

            _fireworkFullFileName = DEFAULT_FIREWORK_NAME;

            //Set default receptors
            _receptors = new ObservableCollection<Receptor>();

            //Refresh default conf. (receptors...)
            _configuration.Load();

            foreach (Receptor r in _configuration.DefaultReceptors)
            {
                //Set Device manager
                r.SetDeviceManager(_deviceManager);
                r.PingTestStarted += PingTestStarted;
                r.PingTestStopped += PingTestStopped;

                _receptors.Add(r);
            }

            OnFireworkLoadedEvent();
        }

        private void PingTestStopped(object sender, EventArgs e)
        {
            OnReceptorTestFinishedEvent();
        }

        private void PingTestStarted(object sender, EventArgs e)
        {
            OnReceptorTestStartedEvent();
        }

        private async void Transceiver_FrameTimeOutEvent(object sender, FrameTimeOutEventArgs e)
        {
            if (e.FrameSent is FireFrame)
            {
                if (e.FrameSent.CanBeResent)
                {
                    //Transceiver can be disconnected anytime...
                    //So better check 
                    if (_deviceManager.IsTransceiverConnected)
                        await _deviceManager.Transceiver.SendFrame(e.FrameSent, _configuration.TotalTimeOut);
                }
                else
                {
                    foreach (string lineNumber in ((FireFrame)e.FrameSent).LineNumbers)
                    {
                        Line l = GetLineByNumber(lineNumber);
                        l.SetFailed();
                    }
                }
            }
        }

        private void Transceiver_FrameAckOkEvent(object sender, FrameAckOKEventArgs e)
        {
            if (e.AckOKFrame.SentFrame is FireFrame)
            {
                foreach (string lineNumber in ((FireFrame)e.AckOKFrame.SentFrame).LineNumbers)
                {
                    Line l = GetLineByNumber(lineNumber);
                    l.Start();
                }
            }

        }

        private async void Transceiver_FrameAckKoEvent(object sender, FrameAckKOEventArgs e)
        {
            if (e.AckKOFrame.HasSentFrame && e.AckKOFrame.SentFrame is FireFrame)
            {
                if (e.AckKOFrame.SentFrame.CanBeResent)
                {
                    //Transceiver can be disconnected anytime...
                    //So better check 
                    if (_deviceManager.IsTransceiverConnected)
                        await _deviceManager.Transceiver.SendFrame(e.AckKOFrame.SentFrame, _configuration.TotalTimeOut);
                }
                else
                {
                    foreach (string lineNumber in ((FireFrame)e.AckKOFrame.SentFrame).LineNumbers)
                    {
                        Line l = GetLineByNumber(lineNumber);
                        l.SetFailed();
                    }
                }
            }
        }

        #endregion

        #region Event

        private void DeviceManager_USBConnection(object sender, USBConnectionEventArgs e)
        {
            string info = string.Format(DeviceManager.DEFAULT_USB_CONNECTING_MESSAGE, e.ElapsedSecond, e.UsbReady);

            OnTransceiverInfoChangedEvent(info);
        }

        private void DeviceManager_DeviceDisconnected(object sender, EventArgs e)
        {
            string info = DeviceManager.DEFAULT_NOT_TRANSCEIVER_CONNECTED_MESSAGE;

            if (_state == FireworkManagerState.FireworkRunning)
            {
                Stop();
            }

            OnTransceiverDisconnectedEvent();
            OnTransceiverInfoChangedEvent(info);
        }

        private void DeviceManager_DeviceConnected(object sender, ConnectionEventArgs e)
        {
            string info = string.Format(DeviceManager.DEFAULT_TRANSCEIVER_CONNECTED_MESSAGE, e.Port);

            OnTransceiverConnectedEvent();
            OnTransceiverInfoChangedEvent(info);
        }

        private void DeviceManager_DeviceErrorWhenConnecting(object sender, ConnectionErrorEventArgs e)
        {
            string info = string.Format(DeviceManager.DEFAULT_ERROR_TRANSCEIVER_WHEN_CONNECTING_MESSAGE, "Erreur inconnue !?");

            if (e.Exception != null)
            {
                info = string.Format(DeviceManager.DEFAULT_ERROR_TRANSCEIVER_WHEN_CONNECTING_MESSAGE, e.Exception.Message);
            }

            OnTransceiverInfoChangedEvent(info);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Public Methods

        //Device already plugged?
        public void DiscoverDevice()
        {
            //Device already plugged?
            _deviceManager.DiscoverDevice();
        }

        public void StopDeviceManager()
        {
            if (_deviceManager != null)
                _deviceManager.Close();
        }

        /// <summary>
        /// User asked to redo failed line
        /// </summary>
        /*public void ActivateRedoFailedLine()
        {
            _activateRedoFailedLine = true;
        }*/

        /// <summary>
        /// Stops firework !!
        /// </summary>
        public void Stop()
        {
            if (_soundTrackManager.HasSoundTrackToPlay() && _soundTrackManager.IsSoundTrackSanityCheckOk())
            {
                //Stop soundtrack if any
                _soundTrackManager.Stop();
            }

            //User ask to stop firework in this case...So stop it properly
            //Stop firework and line properly
            foreach (Line l in AllLines)
            {
                l.Stop();
            }

            if (_fireworkCancellationToken != null)
                _fireworkCancellationToken.Cancel();
        }

        /// <summary>
        /// Start firework !!!
        /// </summary>
        public void Start(bool playSoundTrack)
        {
            if (playSoundTrack && _soundTrackManager.HasSoundTrackToPlay() && _soundTrackManager.IsSoundTrackSanityCheckOk())
            {
                _soundTrackManager.Play();
            }

            DoWorkAsync();
        }

        public void DeleteLine(Line line)
        {
            //Unassign receptor if any
            line.UnassignReceptorAddress();

            _lines.Remove(line);

            //Reorder lines!!
            ReorderLinesAndFireworks();

            MakeItDirty(true);
        }

        public void AddOrUpdateLine(bool isAdd, Line line, Line lineClone)
        {
            int oldIndex = int.Parse(line.Number);

            //Convert to 0 based index
            oldIndex = oldIndex - 1;

            line.UpdateFromClone(lineClone);

            if (isAdd)
            {
                int index = int.Parse(line.Number);
                //Convert to 0 based index
                index = index - 1;

                _lines.Insert(index, line);
               
            }
            else
            {
                int newIndex = int.Parse(line.Number);

                //Convert to 0 based index
                newIndex = newIndex - 1;

                if (oldIndex != newIndex)
                {
                    //Move to new place
                    _lines.Move(oldIndex, newIndex);
                }
            }

            //Reorder lines!!
            ReorderLinesAndFireworks();

            MakeItDirty(true);
        }

        /// <summary>
        /// Initialize empty firework
        /// </summary>
        public void LoadEmptyFirework()
        {
            BeginNewFirework();
        }

        /// <summary>
        /// Load a new firework in memory from xml definition file
        /// </summary>
        /// <param name="fullFileName"></param>
        public void LoadFirework(string fullFilename)
        {
            try
            {
                BeginNewFirework();

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
                    fr.guiet.kquatre.business.receptor.Receptor recep = new fr.guiet.kquatre.business.receptor.Receptor(r.Attribute("name").Value, r.Attribute("address").Value.ToString(), Convert.ToInt32(r.Attribute("nbOfChannels").Value.ToString()), _deviceManager);
                    _receptors.Add(recep);
                }

                //Reset SoundTrack
                SoundTrackUI = string.Empty;

                //v2021.2.1.0.0 - Add SoundTrack
                if (fireworkDefinition.Element("FireworkDefinition").Element("SoundTrack") != null)
                {
                    SoundTrackUI = fireworkDefinition.Element("FireworkDefinition").Element("SoundTrack").Attribute("soundTrackFilePath").Value.ToString();
                }

                //Parcours des lignes et création des artifices
                List<XElement> lines = (from l in fireworkDefinition.Descendants("Line")
                                        select l).ToList();

                foreach (XElement l in lines)
                {
                    int lineNumber = Convert.ToInt32(l.Attribute("number").Value.ToString());
                    Line line = new Line(lineNumber);

                    line.LineStarted += Line_LineStarted;
                    line.LineFailed += Line_LineFailed;
                    line.PropertyChanged += Line_PropertyChanged;

                    if (l.Attribute("rescue") != null)
                    {
                        string rescueLine = l.Attribute("rescue").Value.ToString();

                        if (rescueLine == "yes")
                            line.SetAsRescueLine();
                    }

                    if (l.Attribute("ignition") != null)
                    {
                        TimeSpan ignition = TimeSpan.Parse(l.Attribute("ignition").Value.ToString());
                        line.Ignition = ignition;
                    }

                    if (l.Element("ReceptorAddress") != null)
                    {
                        string address = l.Element("ReceptorAddress").Attribute("address").Value.ToString();
                        string channelNumber = l.Element("ReceptorAddress").Attribute("channel").Value.ToString();

                        Receptor receptor = GetReceptor(address);
                        ReceptorAddress ra = receptor.GetAddress(Convert.ToInt32(channelNumber));

                        line.AssignReceptorAddress(ra);
                    }

                    List<XElement> fireworks = (from f in l.Descendants("Firework")
                                                select f).ToList();
                    foreach (XElement f in fireworks)
                    {

                        string reference = f.Attribute("reference").Value.ToString();
                        string designation = f.Attribute("designation").Value.ToString();

                        TimeSpan duration = TimeSpan.Parse(f.Attribute("duration").Value.ToString());

                        Firework firework = new Firework(reference, designation, duration);

                        AddFireworkToLine(firework, line);
                    }

                    AddLine(line);
                }

                //Set new name here !
                _fireworkFullFileName = fullFilename;

                //Just loaded so not dirty..
                MakeItDirty(false);

                OnFireworkLoadedEvent();

            }
            catch (Exception e)
            {
                BeginNewFirework();

                throw e;
            }
            finally
            {
                _isLoadingFromFile = false;
            }
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
                BeginNewFirework();

                _isLoadingFromFile = true;

                _lines = new ObservableCollection<Line>();

                //https://epplussoftware.com/developers/licenseexception
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                OfficeOpenXml.ExcelPackage ep = new OfficeOpenXml.ExcelPackage(new System.IO.FileInfo(fullFileName));

                ExcelWorksheet excelWorkSheet = ep.Workbook.Worksheets[_configuration.ExcelSheetNumber];

                int rows = excelWorkSheet.Dimension.Rows;

                _name = excelWorkSheet.Cells[_configuration.ExcelFireworkName].Value.ToString();

                int firstRowDataIndex = _configuration.ExcelFirstRowData;

                Line line = null;
                for (int i = firstRowDataIndex; i <= rows; i++)
                {

                    //Last line read maybe empty
                    if (excelWorkSheet.Cells[i, 1].Value == null) continue;

                    string lineNumber = excelWorkSheet.Cells[i, 1].Value.ToString();

                    //First line number
                    if (GetLineByNumber(lineNumber) == null)
                    {
                        line = new Line(Convert.ToInt32(lineNumber));

                        line.LineStarted += Line_LineStarted;
                        line.LineFailed += Line_LineFailed;
                        line.PropertyChanged += Line_PropertyChanged;

                        line.Ignition = ((DateTime)excelWorkSheet.Cells[i, 2].Value).TimeOfDay;
                        AddLine(line);
                    }

                    //Get Data for firework
                    string reference = excelWorkSheet.Cells[i, 8].Value.ToString();
                    string designation = excelWorkSheet.Cells[i, 5].Value.ToString();
                    TimeSpan duration = ((DateTime)excelWorkSheet.Cells[i, 6].Value).TimeOfDay;
                    Firework firework = new Firework(reference, designation, duration);

                    //Add firework reference if it does not exists in the fireworks reference list
                    _configuration.TryAddFireworksReference(firework);

                    AddFireworkToLine(firework, line);

                }

                //Set new name here !
                _fireworkFullFileName = String.Format("{0}{1}", System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fullFileName), System.IO.Path.GetFileNameWithoutExtension(fullFileName)),
                                              DEFAULT_K4_EXTENSION);

                //Just loaded so not dirty..
                MakeItDirty(false);

                OnFireworkLoadedEvent();
            }
            catch (Exception e)
            {
                BeginNewFirework();

                throw e;
            }
            finally
            {
                _isLoadingFromFile = false;
            }
        }

        private void Line_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MakeItDirty(true);
        }

        private void MakeItDirty(bool isDirty)
        {
            //Don't make it dirty while loading from file
            if (_isLoadingFromFile) return;

            _isDirty = isDirty;

            if (_isDirty)
                OnFireworkDefinitionModifiedEvent();
        }

        public void LaunchFailedLine(string lineNumber)
        {
            //Check             
            if (_state == FireworkManagerState.FireworkRunning)
            {

                Line l = (from rl in ActiveLines
                          where rl.State == LineState.LaunchFailed && rl.Number == lineNumber
                          select rl).FirstOrDefault();

                if (l != null)
                {
                    List<Line> lines = new List<Line>
                    {
                        l
                    };
                    //lines.Add(l);
                    _lineHelperFailed = new LineHelper(lines);
                }
                else
                {
                    throw new CannotLaunchLineException(string.Format("Impossible de lancer la ligne : {0}, le tir de la ligne n'a pas échoué", lineNumber));
                }
            }
        }

        public void LaunchRescueLine(string lineNumber)
        {           
            //Check             
            if (_state == FireworkManagerState.FireworkRunning)
            {
                //Calcul du numéro de ligne reel car les lignes de secours sont mises à la fin du feu d'artifice
                string rescueLineNumber  = (ActiveLines.Count() + int.Parse(lineNumber)).ToString();

                Line l = (from rl in RescueLines
                          where rl.State == LineState.Standby && rl.Number == rescueLineNumber
                          select rl).FirstOrDefault();

                if (l != null)
                {

                    List<Line> lines = new List<Line> { l };
                    //lines.Add(l);
                    _lineHelperRescue = new LineHelper(lines);
                }
                else
                {
                    throw new CannotLaunchLineException(string.Format("Impossible de lancer la ligne de secours : {0}", lineNumber));
                }
            }
        }

        public void SaveFirework()
        {
            SaveFirework(_fireworkFullFileName);
        }

        /// <summary>
        /// Add Fireworks to a line
        /// </summary>
        /// <param name="firework"></param>
        /// <param name="line"></param>
        public void AddFireworkToLine(Firework firework, Line line)
        {
            line.AddFirework(firework);

            ReorderLinesAndFireworks();
        }

        public void AddLine(Line line)
        {
            _lines.Add(line);

            ReorderLinesAndFireworks();
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

                //SoundTrack
                XElement st = new XElement("SoundTrack", new XAttribute("soundTrackFilePath", _soundTrackManager.SoundTrackFilePath));

                //Lines
                XElement lines = new XElement("Lines");

                foreach (Line l in _lines)
                {
                    string rescue = "no";
                    if (l.IsRescueLine)
                        rescue = "yes";


                    XElement line = new XElement("Line",
                                                new XAttribute("number", l.Number),
                                                new XAttribute("ignition", $"{l.Ignition:hh\\:mm\\:ss}"),
                                                new XAttribute("rescue", rescue)
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
                                new XAttribute("reference", la.Reference),
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

                //add soundtrack
                fd.Add(st);

                //add lines
                fd.Add(lines);

                //add to xml doc
                doc.Add(fd);

                doc.Save(fullFilename);

                //Set new name here !
                _fireworkFullFileName = fullFilename;

                //No more dirty here
                MakeItDirty(false);

                //Throw saved event
                OnFireworkSavedEvent();

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Check sanity of firework definition
        /// </summary>
        /// <returns></returns>
        public void SanityCheck()
        {
            _sanityCheckErrorsList = new List<string>();
            bool isSanityCheckOk = true;

            //Check if device is connected
            if (!_deviceManager.IsTransceiverConnected)
            {
                _sanityCheckErrorsList.Add("Le logiciel ne détecte aucune connection avec l'émetteur");
                isSanityCheckOk = false;
            }

            //Check firework has got at least one line
            if (ActiveLines != null && ActiveLines.Count == 0)
            {
                _sanityCheckErrorsList.Add("Aucun plan de tir définit pour le feu d'artifice en cours d'édition");
                isSanityCheckOk = false;
            }

            //Active lines
            Line previousLine = null;
            foreach (Line l in ActiveLines)
            {
                //Checking lines with no firework
                if (l.Fireworks.Count == 0)
                {
                    _sanityCheckErrorsList.Add(string.Format("La ligne n°{0} est définie sans feu d'artifice associé", l.Number));
                    isSanityCheckOk = false;
                }

                if (l.ReceptorAddress == null)
                {
                    _sanityCheckErrorsList.Add(string.Format("La ligne n°{0} est définie sans adresse de récepteur associée", l.Number));
                    isSanityCheckOk = false;
                }

                //check line ignition time order!!
                if (previousLine != null)
                {

                    if (l.Ignition.CompareTo(previousLine.Ignition) < 0)
                    {
                        _sanityCheckErrorsList.Add(string.Format("La mise à feu de la ligne n°{0} est incohérent avec la mise à feu de la ligne n°{1} ", l.Number, previousLine.Number));
                        isSanityCheckOk = false;
                    }
                }

                previousLine = l;
            }

            //Rescue lines
            foreach (Line l in RescueLines)
            {
                //Checking lines with no firework
                if (l.Fireworks.Count == 0)
                {
                    _sanityCheckErrorsList.Add(string.Format("La ligne n°{0} est définie sans feu d'artifice associé", l.Number));
                    isSanityCheckOk = false;
                }

                if (l.ReceptorAddress == null)
                {
                    _sanityCheckErrorsList.Add(string.Format("La ligne n°{0} est définie sans adresse de récepteur associée", l.Number));
                    isSanityCheckOk = false;
                }
            }

            //SoundTrack Sanity Check
            if (_soundTrackManager.HasSoundTrackToPlay())
            {
                if (!_soundTrackManager.IsSoundTrackSanityCheckOk())
                {
                    _sanityCheckErrorsList.Add(string.Format("La bande sonore : {0} ne peut pas être lue correctement", _soundTrackManager.SoundTrackFilePath));
                    _isSanityCheckOk = false;
                }
            }

            _isSanityCheckOk = isSanityCheckOk;
        }

        private void Reset()
        {
            //begins by reseting line and firework (case when user stop and restart firework)
            foreach (Line l in _lines)
            {
                l.Reset();
            }
        }

        private void ReorderLinesAndFireworks()
        {
            int lineNumber = 1;
            int fireworkNumber = 0;
            int fireworkRadRowNumber = 0;

            foreach (Line line in _lines)
            {
                line.Reorder(lineNumber);

                lineNumber++;

                foreach (Firework f in line.Fireworks)
                {
                    if (line.IsRescueLine)
                    {
                        f.Reorder(fireworkNumber, null);
                    }
                    else
                    {
                        f.Reorder(fireworkNumber, fireworkRadRowNumber);
                        fireworkRadRowNumber++;
                    }

                    fireworkNumber++;
                }
            }
        }

        /// <summary>
        /// Returns true if all lines are in finished state
        /// </summary>
        /// <returns></returns>
        private bool IsAllLineFinished()
        {
            int nb = (from l in _lines
                      where (l.IsFinished == true && l.IsRescueLine == false) || (l.IsRescueLine == true && (l.IsFinished || l.State == LineState.Standby))
                      select l).Count();

            return (nb == _lines.Count);
        }

        private LineHelper PrepareNextLines()
        {
            //Gets next line to launch!
            Line line = (from l in ActiveLines
                         where l.State == LineState.Standby
                         orderby l.Ignition.Milliseconds
                         select l).FirstOrDefault();

            //Maybe no more line to launch...firework is finished...
            if (line == null)
            {
                _nextIgnition = new TimeSpan(0, 0, 0);
                return null;
            }

            //Set next ignition time
            _nextIgnition = line.Ignition;

            //Maybe another line is launched at the same time!
            List<Line> lines = (from l in ActiveLines
                                where l.State == LineState.Standby
                                && l.Ignition.TotalMilliseconds == line.Ignition.TotalMilliseconds
                                select l).ToList();

            LineHelper helper = new LineHelper(lines);

            return helper;
        }

        public string GetFireworkStatistics()
        {
            int nbTotal = ActiveLines.Count();

            int nbLaunchOK = (from l in ActiveLines
                              where l.State == LineState.Finished
                              select l).Count();

            int nbLaunchKO = (from l in ActiveLines
                              where l.State == LineState.LaunchFailed
                              select l).Count();

            int nbRescueOK = (from l in RescueLines
                              where l.State == LineState.Finished
                              select l).Count();

            int nbRescueKO = (from l in RescueLines
                              where l.State == LineState.LaunchFailed
                              select l).Count();

            int nbTotalRescue = RescueLines.Count();

            string stat = string.Format("Nombre de lignes OK : {0} sur {1}\r\nNombre de lignes KO : {2} sur {3}",
                                        nbLaunchOK.ToString(), nbTotal.ToString(), nbLaunchKO.ToString(), nbTotal.ToString());

            stat += string.Format("{0}Ligne(s) de secours tirée(s) OK : {1} sur {2}\r\nligne de secours tirée(s) KO : {3} sur {4}",
                                        Environment.NewLine, nbRescueOK.ToString(), nbTotalRescue.ToString(), nbRescueKO.ToString(), nbTotalRescue.ToString());

            return stat;
        }

        /// <summary>
        /// Main loop...everythings begins here....
        /// </summary>
        private void DoWorkAsync()
        {
            //Reset line and firework in case user start / stop firework
            Reset();

            State = FireworkManagerState.FireworkRunning;

            //New token
            _fireworkCancellationToken = new CancellationTokenSource();

            _elapsedTime = new Stopwatch();
            _elapsedTime.Start();

            _timerHelper = new System.Timers.Timer
            {
                Interval = 1000
            };
            _timerHelper.Elapsed += TimerHelper_Elapsed;
            _timerHelper.Start();

            LineHelper lineHelper = null;
            bool prepareNextLines = true;

            //Firework has started event
            OnFireworkStartedEvent();

            //Transceiver can be disconnected anytime
            if (_deviceManager.IsTransceiverConnected)
            {
                _deviceManager.Transceiver.FrameAckKoEvent += Transceiver_FrameAckKoEvent;
                _deviceManager.Transceiver.FrameAckOkEvent += Transceiver_FrameAckOkEvent;
                _deviceManager.Transceiver.FrameTimeOutEvent += Transceiver_FrameTimeOutEvent;
            }
            else
            {
                _fireworkCancellationToken.Cancel();
            }

            Task.Run(async () =>
            {
                //TODO : Remplacer IsAllLineFinished par ElapsedTime <= TotalDuration
                //Car sinon le feu s'arrête avant la fin si on a des lignes en statut launchedfailed à la fin du feu
                //A bien tester avec les lignes de secours

                while (!IsAllLineFinished() && !_fireworkCancellationToken.IsCancellationRequested)
                //while (ElapsedTime < TotalDuration && !_fireworkCancellationToken.IsCancellationRequested)
                {
                    //Prepare next lines?
                    if (prepareNextLines)
                    {
                        prepareNextLines = false;
                        //Get next lines with same launch time
                        lineHelper = PrepareNextLines();
                    }

                    //Failed lignes to launch?
                    //User can click on the screen 
                    //to launch failed line
                    if (_lineHelperFailed != null)
                    {
                        await Fire(_lineHelperFailed);

                        //Ok failed line launched!
                        _lineHelperFailed = null;
                    }

                    //Rescue lignes to launch?
                    if (_lineHelperRescue != null)
                    {
                        await Fire(_lineHelperRescue);

                        //Ok rescue line launched!
                        _lineHelperRescue = null;
                    }

                    //No more line to launch...wait for current firework to finish
                    if (lineHelper == null) continue;

                    if (_elapsedTime.ElapsedMilliseconds >= lineHelper.Ignition)
                    {
                        prepareNextLines = true;

                        await Fire(lineHelper);
                    }
                }
            }, _fireworkCancellationToken.Token).ContinueWith(t =>
            {
                State = FireworkManagerState.FireworkStopped;

                if (_deviceManager.IsTransceiverConnected)
                {
                    //Annulation abonnement des événements
                    _deviceManager.Transceiver.FrameAckKoEvent -= Transceiver_FrameAckKoEvent;
                    _deviceManager.Transceiver.FrameAckOkEvent -= Transceiver_FrameAckOkEvent;
                    _deviceManager.Transceiver.FrameTimeOutEvent -= Transceiver_FrameTimeOutEvent;
                }

                //First stop timer then elapsed time because elapsedtime is used in timer helper event
                //avoid null point exception
                _timerHelper.Stop();
                _timerHelper = null;

                _elapsedTime.Stop();
                _elapsedTime = null;

                OnFireworkFinishedEvent();

            }).ConfigureAwait(false);
        }

        private async Task Fire(LineHelper lineHelper)
        {
            //bool clearFailedLine = true;
            foreach (KeyValuePair<string, List<Line>> message in lineHelper.LinesGroupByReceptorAddress)
            {
                string receptorAddress = message.Key;
                List<string> receptorChannels = new List<string>();
                List<string> lineNumbers = new List<string>();

                //Get channel from 
                foreach (Line l in message.Value)
                {
                    receptorChannels.Add(l.ReceptorAddress.Channel.ToString());
                    lineNumbers.Add(l.Number);
                }

                //Transceiver can be disconnected anytime...so better checked
                if (_deviceManager.IsTransceiverConnected)
                    await _deviceManager.Transceiver.SendFireFrame(receptorAddress, receptorChannels, lineNumbers, _configuration.TotalTimeOut, _configuration.RetryFrameEmission);
            }
        }

        private void TimerHelper_Elapsed(object sender, ElapsedEventArgs e)
        {
            //May occur...
            //2021-07-27 - Not anymore I changed the order of stop 
            if (_elapsedTime == null) return;

            //Throw event !
            OnTimerElapsedEvent(sender);

            ElapsedTimeString = $"{_elapsedTime.Elapsed:mm\\:ss}";

            if (_nextIgnition.CompareTo(_elapsedTime.Elapsed) == -1)
            {
                NextLaunchCountDownString = "00:00";
            }
            else
            {
                TimeSpan nextIgnitionCountDown = _nextIgnition - _elapsedTime.Elapsed;
                NextLaunchCountDownString = $"{nextIgnitionCountDown:mm\\:ss}";
            }
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
        private Receptor GetReceptor(string address)
        {
            Receptor receptor = (from r in _receptors
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
