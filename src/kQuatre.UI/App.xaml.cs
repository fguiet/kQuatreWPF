using fr.guiet.kquatre.ui.views;
using System.Threading;
using System.Windows;

namespace fr.guiet.kquatre.ui
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        private SplashScreenWindow _splashScreen = new SplashScreenWindow();

        public App()
        {
            this.MainWindow = new MainWindow();          
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
