using fr.guiet.kquatre.business.firework;
using System.Collections.Generic;
using Telerik.Windows.Controls.Timeline;

namespace fr.guiet.kquatre.ui.timeline
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
