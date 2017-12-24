using Guiet.kQuatre.Business.Exceptions;
using Guiet.kQuatre.Business.Transceiver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Guiet.kQuatre.Business.Receptor
{
    public class Receptor
    {
        #region Private Members

        /// <summary>
        /// Device connected to serial port
        /// </summary>
        private TransceiverManager _transceiver = null;

        /// <summary>
        /// Background worker for testing receptor
        /// </summary>
        private BackgroundWorker _receptorWorker = null;

        /// <summary>
        /// Receptor name
        /// </summary>
        private string _name = null;

        /// <summary>
        /// Receptor address
        /// </summary>
        private string _address = null;

        /// <summary>
        /// List of receptor addresses associated with this receptor
        /// </summary>
        private List<ReceptorAddress> _receptorAddresses = new List<ReceptorAddress>();

        #endregion

        #region Public Members

        public List<ReceptorAddress> ReceptorAddressesAvailable
        {
            get
            {
                return (from ra in _receptorAddresses
                        where ra.IsAvailable == true
                        select ra).ToList();
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public string Address
        {
            get
            {
                return _address;
            }
        }

        public int NbOfChannel
        {
            get
            {
                return _receptorAddresses.Count;
            }
        }

        #endregion

        #region Constructor 

        public Receptor(string name, string address, int nbOfChannel)
        {
            _name = name;
            _address = address;

            for (int i = 1; i <= nbOfChannel; i++)
            {
                _receptorAddresses.Add(new ReceptorAddress(this, i));
            }
        }

        #endregion

        #region Public methods 

        public ReceptorAddress GetAddress(int channel)
        {
            ReceptorAddress receptorAddress = (from ra in _receptorAddresses
                                               where ra.Channel == channel
                                               select ra).FirstOrDefault();

            if (receptorAddress == null)
            {
                throw new CannotFindReceptorAddressException(string.Format("Cannot find address {0} on receptor with mac address {1}", channel.ToString(), _address));
            }

            return receptorAddress;
        }

        public void StartTest(TransceiverManager tm)
        {
            _transceiver = tm;

            _receptorWorker = new BackgroundWorker();
            _receptorWorker.DoWork += ReceptorWorker_DoWork;
            _receptorWorker.RunWorkerCompleted += ReceptorWorker_RunWorkerCompleted; ;
            _receptorWorker.RunWorkerAsync();
        }

        private void ReceptorWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ReceptorWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
