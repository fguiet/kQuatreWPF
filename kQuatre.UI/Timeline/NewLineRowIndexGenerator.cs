using Guiet.kQuatre.Business.Firework;
using System.Collections.Generic;
using Telerik.Windows.Controls.Timeline;

namespace Guiet.kQuatre.UI.Timeline
{
    public class NewLineRowIndexGenerator : IItemRowIndexGenerator
    {
        public void GenerateRowIndexes(List<TimelineRowItem> dataItems)
        {
            foreach (TimelineRowItem item in dataItems)
            {
                item.RowIndex = (item.DataItem as Firework).RadRowIndex;
            }
        }
    }
}
