using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.guiet.kquatre.business.firework
{
    public class LineHelper
    {
        private List<Line> _lines = new List<Line>();
        private Dictionary<string, List<Line>> _messages = new Dictionary<string, List<Line>>();

        //private const string FIRE_MESSAGE_KEY = "FIRE";

        public LineHelper(List<Line> lines)
        {
            _lines = lines;

            //Group line with same receptor
            GroupLinesWithSameReceptorAddress();
        }

        public Dictionary<string, List<Line>> LinesGroupByReceptorAddress
        {
            get
            {
                return _messages;
            }
        }

        public double Ignition
        {
            get
            {
                return _lines[0].Ignition.TotalMilliseconds;
            }
        }

        private void GroupLinesWithSameReceptorAddress()
        {

            foreach (Line line in _lines)
            {
                line.SetImminentLaunch();

                if (_messages.ContainsKey(line.ReceptorAddress.Receptor.Address))
                {
                    //line already treated
                    continue;
                }
                else
                {
                    List<Line> linesSameAddress = (from l in _lines
                                                      where l.ReceptorAddress.Receptor.Address
                                                      == line.ReceptorAddress.Receptor.Address
                                                      select l).ToList();

                    //string channels = string.Join(";", linesSameAddress.Select(l => l.ReceptorAddress.Channel.ToString()));

                    //string message = string.Format("{0};{1}", linesSameAddress.Count(), channels);

                    _messages.Add(line.ReceptorAddress.Receptor.Address, linesSameAddress);
                   //_messages.Add(message, linesSameAddress);
                }
            }
        }
        
        /*public static string GetFireMessage(List<Line> lines)
        {
            string channels = string.Join("+", lines.Select(l => l.ReceptorAddress.Channel.ToString()));

            return string.Format("{0}+{1}", lines.Count(), channels);
        }*/
        /*public void AddLine(Line line)
        {
            List<Line> lineOnSameReceptor =
                                    (from l in _lines
                                     where l.ReceptorAddress.Receptor.Address == line.ReceptorAddress.Receptor.Address
                                     select l).ToList();
            _lines.Add(line);


        }*/
    }
}
