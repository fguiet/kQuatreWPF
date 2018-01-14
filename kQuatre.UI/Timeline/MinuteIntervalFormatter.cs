using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Windows.Controls.TimeBar;

namespace Guiet.kQuatre.UI.Timeline
{
    public class MinuteIntervalFormatter : IIntervalFormatterProvider
    {
        public Func<DateTime, string>[] GetFormatters(IntervalBase interval)
        {
            return new Func<DateTime, string>[]
            {
                date => string.Format("Minute : {0}", date.ToString("mm"))
            };
        }

        public Func<DateTime, string>[] GetIntervalSpanFormatters(IntervalBase interval)
        {
            return new Func<DateTime, string>[]
            {
            date => date.ToString("mm")
            };
        }
    }
}
