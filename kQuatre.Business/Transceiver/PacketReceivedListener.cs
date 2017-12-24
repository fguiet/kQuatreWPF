using Guiet.kQuatre.Business.Transceiver.Frames;
using System.Threading;

namespace Guiet.kQuatre.Business.Transceiver
{
    /// <summary>
    /// This class is used to handle received packet
    /// </summary>
    public class PacketReceivedListener
    {
        private FrameBase _packetSent = null;
        private FrameBase _packetReceived = null;
        private object _lockObject;

        public FrameBase PacketSent
        {
            get
            {
                return _packetSent;
            }
        }

        public FrameBase PacketReceived
        {
            get
            {
                return _packetReceived;
            }
        }

        public PacketReceivedListener(FrameBase packetSent, object lockObject)
        {
            _packetSent = packetSent;
            _lockObject = lockObject;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="receivedPacket"></param>
        public void FrameReceived(FrameBase packetReceived)
        {            
            _packetReceived = packetReceived; //ResponsePacketParser.ParseResponsePacket(receivedPacket);
            
            //Notify that packet has been received
            lock (_lockObject)
            {
                Monitor.Pulse(_lockObject);
            }
        }
    }
}
