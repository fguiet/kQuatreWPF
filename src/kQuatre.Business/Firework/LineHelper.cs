using fr.guiet.kquatre.business.receptor;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Shapes;

namespace fr.guiet.kquatre.business.firework
{
    /// <summary>
    /// This is a helper class.
    /// 
    /// It prepares the next line to launch with associated fireworks
    /// </summary>
    public class LineHelper
    {
        private Line _line = null;
        private Dictionary<string, List<Firework>> _messages = new Dictionary<string, List<Firework>>();
       
        public LineHelper(Line line)
        {
            _line = line;

            //This line will be launched next!!
            line.SetImminentLaunch();

            //Group firework with same receptor
            GroupFireworksWithSameReceptorAddress();
        }

        public Dictionary<string, List<Firework>> FireworksGroupByReceptorAddress
        {
            get
            {
                return _messages;
            }
        }

        public Line GetLine
        {
            get
            {
                return _line;
            }
        }

        public double Ignition
        {
            get
            {
                return _line.Ignition.TotalMilliseconds;
            }
        }

        /// <summary>
        /// TODO : [2023.1.0.0] - 2023/07/09 - Rework that part of code...
        /// Group fireworks wih same receptor address
        /// </summary>
        private void GroupFireworksWithSameReceptorAddress()
        {
            foreach (Firework f in _line.Fireworks)
            {
                if (_messages.ContainsKey(f.ReceptorAddress.Address))
                {
                    //line already treated
                    continue;
                }
                else
                {
                    List<Firework> fireworksSameAddress = (from firework in _line.Fireworks
                                                           where firework.ReceptorAddress.Address == f.ReceptorAddress.Address
                                                           select firework).ToList();

                    _messages.Add(f.ReceptorAddress.Address, fireworksSameAddress);
                }
            }            
        }
    }
}
