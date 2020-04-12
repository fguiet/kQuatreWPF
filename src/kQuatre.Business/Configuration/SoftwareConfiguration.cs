using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.business.receptor;
using fr.guiet.kquatre.business.transceiver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace fr.guiet.kquatre.business.configuration
{
    public class SoftwareConfiguration
    {
        #region Private Members

        /// <summary>
        /// First row of data in Excel file
        /// </summary>       
        private int _excelFirstRowData;

        private int _excelSheetNumber;

        private string _excelFireworkName;

        private int _ackTimeOut;

        private int _totalTimeOut;

        private int _retryFrameEmission;

        private string _transceiverAddress;

        private int _transceiverBaudrate;

        //private int _tranceiverRetryPing;

        List<ConfigFolderNode> _treeViewDataSource = null;

        private const string KQUATRE_CONFIGURATION_NAME = "kQuatreConfiguration.xml";

        private const string KQUATRE_FIREWORKS_NAME = "kQuatreFireworks.xml";

        private readonly List<Receptor> _receptors = new List<Receptor>();

        /// <summary>
        /// Fireworks available to create a new firework plan (dunno how to say this in English)
        /// </summary>
        private readonly ObservableCollection<Firework> _fireworks = new ObservableCollection<Firework>();

        #endregion

        public ObservableCollection<fr.guiet.kquatre.business.firework.Firework> Fireworks
        {
            get
            {
                return _fireworks;
            }
        }

        public List<fr.guiet.kquatre.business.receptor.Receptor> DefaultReceptors
        {
            get
            {
                return _receptors;
            }
        }

        public int ExcelFirstRowData
        {
            get
            {
                return _excelFirstRowData;
            }
        }

        public int ExcelSheetNumber
        {
            get
            {
                return _excelSheetNumber;
            }
        }

        public string ExcelFireworkName
        {
            get
            {
                return _excelFireworkName;
            }
        }

        /// <summary>
        /// Time to wait by the transceiver for an ACK from receiver after a frame has been sent
        /// </summary>
        public int AckTimeOut
        {
            get
            {
                return _ackTimeOut;
            }
        }

        /// <summary>
        /// Maximum time to wait for a frame to be sent and receiver
        /// </summary>
        public int TotalTimeOut
        {
            get
            {
                return _totalTimeOut;
            }
        }

        public int RetryFrameEmission
        {
            get
            {
                return _retryFrameEmission;
            }
        }

        public string TransceiverAddress
        {
            get
            {
                return _transceiverAddress;
            }
        }

        public int TranceiverBaudrate
        {
            get
            {
                return _transceiverBaudrate;
            }
        }

        /*public int TranceiverRetryPing
        {
            get
            {
                return _tranceiverRetryPing;
            }
        }*/

        public const string EXCEL_FIRST_ROW_DATA_PROP_ID = "1";
        public const string EXCEL_FIREWORK_NAME_PROP_ID = "2";
        public const string EXCEL_SHEET_NB_PROP_ID = "5";
        public const string TRANSCEIVER_ACK_TIMEOUT_PROP_ID = "3";
        public const string TRANSCEIVER_RETRY_FRAME_EMISSION_PROP_ID = "4";
        public const string TRANSCEIVER_ADDRESS_PROP_ID = "6";
        public const string TRANSCEIVER_BAUDRATE_PROP_ID = "7";
        //public const string TRANSCEIVER_RETRYPING_PROP_ID = "8";
        public const string TRANSCEIVER_TOTAL_TIMEOUT_PROP_ID = "9";

        public List<ConfigFolderNode> TreeViewDataSource
        {
            get
            {
                if (_treeViewDataSource == null)
                    GenerateTreeViewDataSource();

                return _treeViewDataSource;
            }
        }

        public SoftwareConfiguration()
        {
            Load();
        }


        #region Public Methods

        public void TryAddFireworksReference(Firework firework)
        {
            Firework f = (from fw in _fireworks
                                   where fw.Reference == firework.Reference
                                   select fw).FirstOrDefault();

            if (f == null)
            {
                Firework newFirework = firework.GetClone();
                _fireworks.Add(newFirework);
                SaveFireworks();
            }
        }

        /// <summary>
        /// Saves fireworks list 
        /// </summary>
        public void SaveFireworks()
        {            
            XDocument doc = new XDocument();

            //Firework name
            XElement fd = new XElement("kQuatreConfiguration");

            //Receptors
            XElement r = new XElement("Fireworks",
                        _fireworks.Select(x => new XElement("Firework", new XAttribute("reference", x.Reference), new XAttribute("designation", x.Designation), new XAttribute("duration", $"{x.Duration:hh\\:mm\\:ss}")))
                     );

            fd.Add(r);

            doc.Add(fd);

            doc.Save(GetFireworksFileName());
        }

        /// <summary>
        /// Save software configuration
        /// </summary>
        public void Save()
        {
            //TODO : Handle exeption 

            //Load config file
            XDocument confFile = XDocument.Load(GetConfigFileName());

            //Excel
            ConfigPropertyNode cpn = GetPropertyNodeById(SoftwareConfiguration.EXCEL_FIREWORK_NAME_PROP_ID);
            XElement excelFile = confFile.Descendants("ExcelFile").First();
            excelFile.Element("FireworkName").Value = cpn.PropertyValue;
            
            cpn = GetPropertyNodeById(SoftwareConfiguration.EXCEL_FIRST_ROW_DATA_PROP_ID);            
            excelFile.Element("FireworkDataRow").Value = cpn.PropertyValue;

            cpn = GetPropertyNodeById(SoftwareConfiguration.EXCEL_SHEET_NB_PROP_ID);            
            excelFile.Element("FireworkSheetNumber").Value = cpn.PropertyValue;

            //Transceiver
            cpn = GetPropertyNodeById(SoftwareConfiguration.TRANSCEIVER_ACK_TIMEOUT_PROP_ID);
            XElement transceiver = confFile.Descendants("Transceiver").First();
            transceiver.Element("AckTimeOut").Value = cpn.PropertyValue;

            cpn = GetPropertyNodeById(SoftwareConfiguration.TRANSCEIVER_TOTAL_TIMEOUT_PROP_ID);
            transceiver.Element("TotalTimeout").Value = cpn.PropertyValue;

            cpn = GetPropertyNodeById(SoftwareConfiguration.TRANSCEIVER_RETRY_FRAME_EMISSION_PROP_ID);
            transceiver.Element("RetryFrameEmission").Value = cpn.PropertyValue;

            cpn = GetPropertyNodeById(SoftwareConfiguration.TRANSCEIVER_ADDRESS_PROP_ID);
            transceiver.Element("Address").Value = cpn.PropertyValue;

            cpn = GetPropertyNodeById(SoftwareConfiguration.TRANSCEIVER_BAUDRATE_PROP_ID);
            transceiver.Element("Baudrate").Value = cpn.PropertyValue;

            //cpn = GetPropertyNodeById(SoftwareConfiguration.TRANSCEIVER_RETRYPING_PROP_ID);
            //transceiver.Element("RetryPingTransceiver").Value = cpn.PropertyValue;

            confFile.Save(GetConfigFileName());

            //Refresh properties
            Load();
            
        }

        public void Load()
        {
            //TODO : Handle exeption  if conf. file does not exists
            //TODO : Handle exeption  if fireworks file does not exists

            //Load config file
            XDocument confFile = XDocument.Load(GetConfigFileName());
            
            //Load default receptors definition
            List<XElement> receptors = (from r in confFile.Descendants("Receptor")
                                        select r).ToList();

            foreach (XElement r in receptors)
            {
                 fr.guiet.kquatre.business.receptor.Receptor recep = new fr.guiet.kquatre.business.receptor.Receptor(r.Attribute("name").Value, r.Attribute("address").Value.ToString(), Convert.ToInt32(r.Attribute("nbOfChannels").Value.ToString()));
                _receptors.Add(recep);
            }

            //Excel 
            XElement excelFile = confFile.Descendants("ExcelFile").First();
            _excelFireworkName = excelFile.Element("FireworkName").Value.ToString();
            _excelFirstRowData = Convert.ToInt32(excelFile.Element("FireworkDataRow").Value.ToString());
            _excelSheetNumber = Convert.ToInt32(excelFile.Element("FireworkSheetNumber").Value.ToString());

            //Transceiver
            XElement transceiver = confFile.Descendants("Transceiver").First();
            _ackTimeOut= Convert.ToInt32(transceiver.Element("AckTimeOut").Value.ToString());
            _totalTimeOut = Convert.ToInt32(transceiver.Element("TotalTimeout").Value.ToString());
            _retryFrameEmission = Convert.ToInt32(transceiver.Element("RetryFrameEmission").Value.ToString());            
            _transceiverAddress = transceiver.Element("Address").Value.ToString();
            _transceiverBaudrate = Convert.ToInt32(transceiver.Element("Baudrate").Value.ToString());
            //_tranceiverRetryPing = Convert.ToInt32(transceiver.Element("RetryPingTransceiver").Value.ToString());

            //*** Fireworks
            XDocument fireworksFile = XDocument.Load(GetFireworksFileName());

            List<XElement> fireworks = (from r in fireworksFile.Descendants("Firework")
                                        select r).ToList();

            foreach(XElement fw in fireworks)
            {
                TimeSpan duration = TimeSpan.Parse(fw.Attribute("duration").Value.ToString());
                Firework f = new Firework(fw.Attribute("reference").Value.ToString(), fw.Attribute("designation").Value.ToString(), duration);

                _fireworks.Add(f);
            }

        }

        private void GenerateTreeViewDataSource()
        {
            List<ConfigFolderNode> list = new List<ConfigFolderNode>();
               
            //Excel file
            ConfigFolderNode fn = new ConfigFolderNode("Fichier Excel");
            ConfigPropertyNode cpn = new ConfigPropertyNode(EXCEL_FIRST_ROW_DATA_PROP_ID, "Première ligne de données", _excelFirstRowData.ToString());

            fn.AddNode(cpn);

            cpn = new ConfigPropertyNode( EXCEL_FIREWORK_NAME_PROP_ID, "Titre du feu", _excelFireworkName);

            fn.AddNode(cpn);

            cpn = new ConfigPropertyNode(EXCEL_SHEET_NB_PROP_ID, "Numéro de la feuille Excel", _excelSheetNumber.ToString());

            fn.AddNode(cpn);

            list.Add(fn);

            //Transceiver
            fn = new ConfigFolderNode("Transceiver");
            cpn = new ConfigPropertyNode(TRANSCEIVER_TOTAL_TIMEOUT_PROP_ID, "Temps d'attente maximum (ms) pour l'envoi et la réception d'une frame", _totalTimeOut.ToString());
            fn.AddNode(cpn);
            cpn = new ConfigPropertyNode(TRANSCEIVER_ACK_TIMEOUT_PROP_ID, "Temps d'attente maximum (ms) d'un ACK en provenance d'un récepteur", _ackTimeOut.ToString());
            fn.AddNode(cpn);            
            cpn = new ConfigPropertyNode(TRANSCEIVER_RETRY_FRAME_EMISSION_PROP_ID, "Nb de renvoie du message en cas d'échec", _retryFrameEmission.ToString());
            fn.AddNode(cpn);
            cpn = new ConfigPropertyNode(TRANSCEIVER_ADDRESS_PROP_ID, "Adresse de l'émetteur/récepteur", _transceiverAddress.ToString());
            fn.AddNode(cpn);
            cpn = new ConfigPropertyNode(TRANSCEIVER_BAUDRATE_PROP_ID, "Baudrate", _transceiverBaudrate.ToString());
            fn.AddNode(cpn);
            //cpn = new ConfigPropertyNode(TRANSCEIVER_RETRYPING_PROP_ID, "Nb de ping défectueux du transceiver tolérés", _tranceiverRetryPing.ToString());
            //fn.AddNode(cpn);

            list.Add(fn);

            _treeViewDataSource = list;
        }

        #endregion

        private string GetFireworksFileName()
        {
            //Exe directory
            string directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            return Path.Combine(directory, KQUATRE_FIREWORKS_NAME);
        }

        private string GetConfigFileName()
        {
            //Exe directory
            string directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            return Path.Combine(directory, KQUATRE_CONFIGURATION_NAME);            
        }

        private ConfigPropertyNode GetPropertyNodeById(string nodeId)
        {
            foreach(ConfigFolderNode folderNode in _treeViewDataSource)
            {
                ConfigPropertyNode cpn = (from pn in folderNode.PropertyNodes
                                          where pn.PropertyId == nodeId
                                          select pn).FirstOrDefault();

                if (cpn != null)
                    return cpn;
            }

            return null;
        }
    }
}
