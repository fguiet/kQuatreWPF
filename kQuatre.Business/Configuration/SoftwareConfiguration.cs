﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Guiet.kQuatre.Business.Configuration
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

        private int _transceiverACKTimeout;

        private int _transceiverRetryMessageEmission;

        private string _transceiverAddress;

        List<ConfigFolderNode> _treeViewDataSource = null;

        private const string KQUATRE_CONFIGURATION_NAME = "kQuatreConfiguration.xml";

        private List<Guiet.kQuatre.Business.Receptor.Receptor> _receptors = new List<Receptor.Receptor>();
        
        #endregion

        public List<Guiet.kQuatre.Business.Receptor.Receptor> DefaultReceptors
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

        public int TransceiverACKTimeout
        {
            get
            {
                return _transceiverACKTimeout;
            }
        }

        public int TransceiverRetryMessageEmission
        {
            get
            {
                return _transceiverRetryMessageEmission;
            }
        }

        public string TransceiverAddress
        {
            get
            {
                return _transceiverAddress;
            }
        }

        public const string EXCEL_FIRST_ROW_DATA_PROP_ID = "1";
        public const string EXCEL_FIREWORK_NAME_PROP_ID = "2";
        public const string EXCEL_SHEET_NB_PROP_ID = "5";
        public const string TRANSCEIVER_ACK_TIMEOUT_PROP_ID = "3";
        public const string TRANSCEIVER_RETRY_MESSAGE_EMISSION_PROP_ID = "4";
        public const string TRANSCEIVER_ADDRESS = "6";


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
            transceiver.Element("ACKReceptionTimeout").Value = cpn.PropertyValue;
            
            cpn = GetPropertyNodeById(SoftwareConfiguration.TRANSCEIVER_RETRY_MESSAGE_EMISSION_PROP_ID);
            transceiver.Element("RetryMessageEmission").Value = cpn.PropertyValue;

            cpn = GetPropertyNodeById(SoftwareConfiguration.TRANSCEIVER_ADDRESS);
            transceiver.Element("address").Value = cpn.PropertyValue;

            confFile.Save(GetConfigFileName());

            //Refresh properties
            Load();
            
        }

        private void Load()
        {
            //TODO : Handle exeption  if conf. file does not exists

            //Load config file
            XDocument confFile = XDocument.Load(GetConfigFileName());
            
            //Load default receptors definition
            List<XElement> receptors = (from r in confFile.Descendants("Receptor")
                                        select r).ToList();

            foreach (XElement r in receptors)
            {
                 Guiet.kQuatre.Business.Receptor.Receptor recep = new Guiet.kQuatre.Business.Receptor.Receptor(r.Attribute("name").Value, r.Attribute("address").Value.ToString(), Convert.ToInt32(r.Attribute("nbOfChannels").Value.ToString()));
                _receptors.Add(recep);
            }

            //Excel 
            XElement excelFile = confFile.Descendants("ExcelFile").First();
            _excelFireworkName = excelFile.Element("FireworkName").Value.ToString();
            _excelFirstRowData = Convert.ToInt32(excelFile.Element("FireworkDataRow").Value.ToString());
            _excelSheetNumber = Convert.ToInt32(excelFile.Element("FireworkSheetNumber").Value.ToString());

            //Transceiver
            XElement transceiver = confFile.Descendants("Transceiver").First();
            _transceiverACKTimeout = Convert.ToInt32(transceiver.Element("ACKReceptionTimeout").Value.ToString());
            _transceiverRetryMessageEmission = Convert.ToInt32(transceiver.Element("RetryMessageEmission").Value.ToString());
            _transceiverAddress = transceiver.Element("Address").Value.ToString();
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

            cpn = new ConfigPropertyNode(EXCEL_SHEET_NB_PROP_ID, "Numéro de la feuille EXcel", _excelSheetNumber.ToString());

            fn.AddNode(cpn);

            list.Add(fn);

            //Transceiver
            fn = new ConfigFolderNode("Transceiver");
            cpn = new ConfigPropertyNode(TRANSCEIVER_ACK_TIMEOUT_PROP_ID, "Expiration réception ACK (ms)", _transceiverACKTimeout.ToString());
            fn.AddNode(cpn);
            cpn = new ConfigPropertyNode(TRANSCEIVER_RETRY_MESSAGE_EMISSION_PROP_ID, "Nb de renvoie du message en cas d'échec", _transceiverRetryMessageEmission.ToString());
            fn.AddNode(cpn);
            cpn = new ConfigPropertyNode(TRANSCEIVER_ADDRESS, "Adresse de l'émetteur/récepteur", _transceiverRetryMessageEmission.ToString());
            fn.AddNode(cpn);

            list.Add(fn);

            _treeViewDataSource = list;
        }

        #endregion

        private string  GetConfigFileName()
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