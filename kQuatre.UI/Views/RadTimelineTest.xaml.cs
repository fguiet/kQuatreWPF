using Guiet.kQuatre.Business.Firework;
using Guiet.kQuatre.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Timeline;

namespace Guiet.kQuatre.UI.Views
{
    /// <summary>
    /// Logique d'interaction pour RadTimelineTest.xaml
    /// </summary>
    public partial class RadTimelineTest : Window
    {
        private FireworkManager _fireworkManager = null;
        private RadTimelineTestView _viewModel = null;
        private Timer _timer = new Timer(5000);
        private DateTime _start = new DateTime(2018, 01, 01);
        private Dispatcher _dispatcher = null;
        private TimelineScrollBar _verticalSlider;
       // private int _oldDecalage = 0;

        public object RadTimelineDataItem { get; private set; }

        public RadTimelineTest(FireworkManager fm)
        {
            InitializeComponent();

            _fireworkManager = fm;
            _dispatcher = Dispatcher.CurrentDispatcher;
            this.Loaded += RadTimelineTest_Loaded;
        }

        private void RadTimelineTest_Loaded(object sender, RoutedEventArgs e)
        {
            this._viewModel = new RadTimelineTestView(_fireworkManager);
            this.DataContext = _viewModel;
            _timer.Elapsed += _timer_Elapsed;
            _fireworkTimeline.SelectionChanged += _fireworkTimeline_SelectionChanged;
            // _timer.Start();
            _verticalSlider = _fireworkTimeline.FindChildByType<TimelineScrollBar>();
            _fireworkManager.LineStarted += _fireworkManager_LineStarted;
            _fireworkManager.LineFailed += _fireworkManager_LineFailed;
        }

        private void _fireworkManager_LineFailed(object sender, EventArgs e)
        {
            Business.Firework.Line line = sender as Business.Firework.Line;

            ComputeVisiblePeriod(line);
        }

        private void _fireworkManager_LineStarted(object sender, EventArgs e)
        {
            Business.Firework.Line line = sender as Business.Firework.Line;

            ComputeVisiblePeriod(line);
        }

        private void ComputeVisiblePeriod(Business.Firework.Line line)
        {
            TimeSpan lineIgnition = line.Ignition;

            DateTime visiblePeriodStart = DateTime.Now.Date.Add(line.Ignition).Subtract(new TimeSpan(0, 0, 20));
            DateTime visiblePeriodEnd = DateTime.Now.Date.Add(line.Ignition).Add(new TimeSpan(0, 0, 40));

            //Change visible port view only if new visible period start - 20 s > period start ui
            if (visiblePeriodStart.CompareTo(_fireworkManager.PeriodStartUI) > 0)
            {
                _dispatcher.BeginInvoke((Action)(() =>
                {
                    _fireworkTimeline.VisiblePeriod = new Telerik.Windows.Controls.SelectionRange<DateTime>(visiblePeriodStart, visiblePeriodEnd);
                }));
            }

            _dispatcher.BeginInvoke((Action)(() =>
            {
                //Vertical part
                if (_verticalSlider != null)
                {
                    int nbOfElementVisiblePerRange = Convert.ToInt32(Math.Truncate((_fireworkManager.AllActiveFireworks.Count() * this._verticalSlider.SelectionRange)));
                    double range = (line.Fireworks[0].RadRowIndex * this._verticalSlider.SelectionRange / nbOfElementVisiblePerRange) ;


                    //End?
                    if (range + this._verticalSlider.SelectionRange - (this._verticalSlider.SelectionRange / 4) > 1)
                    {
                        this._verticalSlider.Selection = new SelectionRange<double>(1 - this._verticalSlider.SelectionRange, 1);
                        return;
                    }

                    //Mid screen reached?
                    if (range > (this._verticalSlider.SelectionRange / 4))
                    {

                        var newStart = range - (this._verticalSlider.SelectionRange / 4);
                        var newEnd = range + this._verticalSlider.SelectionRange - (this._verticalSlider.SelectionRange / 4);
                        this._verticalSlider.Selection = new SelectionRange<double>(newStart, newEnd);
                    }
                    


                    /* double thirdScreenScroll = this._verticalSlider.SelectionRange / 2;

                     int nbOfElementVisiblePerRange = Convert.ToInt32(Math.Truncate((_fireworkManager.AllFireworks.Count() * this._verticalSlider.SelectionRange) / 2));

                    // nbOfElementVisiblePerRange = nbOfElementVisiblePerRange - 3;

                     int decalage = Convert.ToInt32(Math.Truncate(Convert.ToDouble(line.Fireworks[0].RadRowIndex / nbOfElementVisiblePerRange)));

                     //double range = decalage * halfScreenScroll;

                     if (_oldDecalage != decalage)
                     {
                         var newStart = this._verticalSlider.SelectionStart + thirdScreenScroll;
                         var newEnd = this._verticalSlider.SelectionEnd + thirdScreenScroll;
                         this._verticalSlider.Selection = new SelectionRange<double>(newStart, newEnd);

                         _oldDecalage = decalage;
                     }*/
                }
            }));

        }

        private void _fireworkTimeline_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
        {
            //_fireworkTimelin = 
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _start = _start.Add(new TimeSpan(0, 1, 0));
            _dispatcher.BeginInvoke((Action)(() =>
            {
                _fireworkTimeline.VisiblePeriod = new Telerik.Windows.Controls.SelectionRange<DateTime>(_start, _start.Add(new TimeSpan(0, 1, 0)));

            }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            // _fireworkTimeline.
            //_fireworkTimeline.SelectedItem = _viewModel.FireworkManager.Lines[11];
            //_fireworkTimeline.FindResource()
            //_fireworkTimeline.sc
            var newStart = this._verticalSlider.SelectionStart + 0.1;
            var newEnd = this._verticalSlider.SelectionEnd + 0.1;
            this._verticalSlider.Selection = new SelectionRange<double>(newStart, newEnd);

        }
    }
}
