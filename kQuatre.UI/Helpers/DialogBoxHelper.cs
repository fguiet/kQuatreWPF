using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Guiet.kQuatre.UI.Helpers
{
    public static class DialogBoxHelper
    {
        public static void ShowWarningMessage(string message)
        {
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
        }
    }
}
