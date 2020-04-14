using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;


namespace fr.guiet.kquatre.ui.controls.segments
{
    /// <summary>
    /// A panel children of which fill maximum space
    /// </summary>
    [DesignTimeVisible(false)] 
    public class ArrangedPanel : Panel
    {
        protected override Size ArrangeOverride(Size finalSize)
        {
            double x = 0;
            double y = 0;
            double w = finalSize.Width / InternalChildren.Count;
            double h = finalSize.Height;

            foreach (UIElement child in InternalChildren)
            {
                child.Arrange(new Rect(new Point(x, y), new Size(w, h)));
                x += w;
            }
            return finalSize;
        }
    }
}
