using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace fr.guiet.kquatre.ui.command
{
    public class NavigationContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DesignTemplate { get; set; }
        public DataTemplate TestTemplate { get; set; }
        public DataTemplate FireworkTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item !=  null)
            {
                RadNavigationViewItem model = (RadNavigationViewItem)item;

                switch(model.Name)
                {
                    case "_nviDesign":
                        return this.DesignTemplate;

                    case "_nviTest":
                        return this.TestTemplate;

                    case "_nviFirework":
                        return this.FireworkTemplate;
                }
            }
               
            //By default...
            return this.DesignTemplate;

        }
    }
}
