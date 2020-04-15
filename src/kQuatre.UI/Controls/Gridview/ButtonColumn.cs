using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;

namespace fr.guiet.kquatre.ui.controls.gridview
{
    public class CustomButtonColumn : Telerik.Windows.Controls.GridViewColumn
    {
        public override FrameworkElement CreateCellElement(GridViewCell cell, object dataItem)
        {
            RadButton button = cell.Content as RadButton;
            if (button == null)
            {
                button = new RadButton();
                button.Content = "Ajouter un artifice";                
            }

            button.CommandParameter = dataItem;

            return button;
        }
    }
}
