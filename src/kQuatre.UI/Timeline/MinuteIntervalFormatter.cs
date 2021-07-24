using System;
using Telerik.Windows.Controls.TimeBar;

namespace fr.guiet.kquatre.ui.timeline
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
