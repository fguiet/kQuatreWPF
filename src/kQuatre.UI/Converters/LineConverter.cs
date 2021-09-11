using fr.guiet.kquatre.business.firework;
using System;
using System.Windows.Data;

namespace fr.guiet.kquatre.ui.converters
{
    public class LineConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Line line = value as Line;

            if (line != null)
            {
                return line.IsRescueLine;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
