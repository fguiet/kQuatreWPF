using Guiet.kQuatre.UI.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace kQuatre.UI
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Guiet.kQuatre.UI.Views.SplashScreenWindow _splashScreen = new Guiet.kQuatre.UI.Views.SplashScreenWindow();

        public App()
        {
            this.MainWindow = new Guiet.kQuatre.UI.Views.MainWindow();
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        protected override void OnStartup(StartupEventArgs e)
        {            
            _splashScreen.Show();

            Thread.Sleep(3000);

            this.MainWindow.Show();

            _splashScreen.Close();
            _splashScreen = null;           
        }
    }
}
