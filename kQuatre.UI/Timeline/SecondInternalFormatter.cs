using System;
using Telerik.Windows.Controls.TimeBar;

namespace Guiet.kQuatre.UI.Timeline
{
    public class SecondIntervalFormatter : IIntervalFormatterProvider
    {
        public Func<DateTime, string>[] GetFormatters(IntervalBase interval)
        {
            return new Func<DateTime, string>[]
            {
                date => string.Format("{0}", date.ToString("ss"))
            };
        }

        public Func<DateTime, string>[] GetIntervalSpanFormatters(IntervalBase interval)
        {
            return new Func<DateTime, string>[]
            {
            date => date.ToString("ss")
            };
        }
    }
}
