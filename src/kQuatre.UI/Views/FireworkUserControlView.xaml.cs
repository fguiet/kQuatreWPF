using fr.guiet.kquatre.business.firework;
using fr.guiet.kquatre.ui.viewmodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Timeline;

namespace fr.guiet.kquatre.ui.views
{
    /// <summary>
    /// Logique d'interaction pour FireworkUserControlView.xaml
    /// </summary>
    public partial class FireworkUserControlView : UserControl
    {
        #region Private property

        private FireworkUserControlViewModel _viewModel = null;

        #endregion

        #region Public property

        #region Public Members       

        public static readonly DependencyProperty FireworkUserControlViewModelProperty
            = DependencyProperty.Register("ViewModel", typeof(FireworkUserControlViewModel), typeof(FireworkUserControlView));

        public FireworkUserControlViewModel ViewModel
        {
            get { return (FireworkUserControlViewModel)GetValue(FireworkUserControlViewModelProperty); }
            set
            {
                SetValue(FireworkUserControlViewModelProperty, (FireworkUserControlViewModel)value);
            }
        }

        #endregion

        #endregion

        #region Constructor

        public FireworkUserControlView()
        {
            InitializeComponent();

            this.Loaded += FireworkUserControlView_Loaded;            
        }

        #endregion

        #region Events

        private void FireworkUserControlView_Loaded(object sender, RoutedEventArgs e)
        {
            //http://paulstovell.com/blog/mvvm-instantiation-approaches
            //With datatemplate usercontrol is loaded everytime...so viewmodel must be instanciate ones 
            //but outside of usercontrol

            //Initialize private memeber
            _viewModel = ViewModel;
            _viewModel.SetRadtimeline(_fireworkTimeline);


            DataContext = _viewModel;

            //Reset control each time it is loaded
            ResetControlPanel();
        }

       
        private void FireworkTimeline_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
        {


            if (e.AddedItems.Count > 0)
            {
                Firework f = (Firework)e.AddedItems[0];
                //DialogBoxHelper.ShowInformationMessage("Ligne sélectionnée : ");

                _viewModel.LaunchFailedLine(f.AssignedLine.Number);

                _fireworkTimeline.SelectedItem = null;
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Reset Control panel to initial state
        /// </summary>
        private void ResetControlPanel()
        {
            _viewModel.ResetUserControlUI();
        }

        #endregion

        /// <summary>
        /// Useful to make some tests :-)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _btnTest_Click(object sender, RoutedEventArgs e)
        {
            TimelineScrollBar verticalSlider = _fireworkTimeline.FindChildByType<TimelineScrollBar>();
            double start = verticalSlider.SelectionStart;
            double end = verticalSlider.SelectionEnd;


            double newStart =  (double)50 / 67 - (verticalSlider.SelectionRange / 2); // ou 0.3 / 0.6 / 
            double newEnd = ((double)50 / 67 + verticalSlider.SelectionRange / 2);

            verticalSlider.Selection = new SelectionRange<double>(newStart, newEnd);


            //verticalSlider.Selection = verticalSlider.SelectionRange + (20 * 1 / 67 - (verticalSlider.SelectionRange / 2), )

            
            return;


            List<TimelineStripLineControl> timeLineStripLineControlList = _fireworkTimeline.ChildrenOfType<TimelineStripLineControl>().ToList();


            Style t = timeLineStripLineControlList[0].ElementStyle;
            Style s = timeLineStripLineControlList[0].NormalStyle;
            Style p = timeLineStripLineControlList[0].AlternateStyle;

            Style t1 = timeLineStripLineControlList[1].ElementStyle;
            Style s1 = timeLineStripLineControlList[1].NormalStyle;
            Style p1 = timeLineStripLineControlList[1].AlternateStyle;

            Type toto = t.Setters[0].GetType();
            PropertyInfo pi = toto.GetProperty("Value");
            object obj = pi.GetValue(t.Setters[0], null);

            //FluentResourceKey.AlternativeBrush

           /* FluentResourceKey dd = (FluentResourceKey)FluentResourceKey.AlternativeBrush;

            FluentResourceKey.PrimaryBrush

            FluentResourceExtension gd = new FluentResourceExtension();
            gd.ResourceKey = dd;*/


            /*FluentResourceExtension<> ty;

            ty.ResourceKey = FluentResourceKey.AlternativeBrush;
            ty.
            //m.ResourceKey = FluentResourceKey.AlternativeBrush;*/






            Telerik.Windows.Controls.FluentResourceExtension u = (Telerik.Windows.Controls.FluentResourceExtension)obj;
            

            //object y = toto.GetProperties()[4].GetValue(timeLineStripLineControlList[0].ElementStyle);



            //System.Windows.Controls.Border border =  (System.Windows.Controls.Border)t;

            // System.Windows.Controls.Border tot = (System.Windows.Controls.Border)timeLineStripLineControlList[0].ElementStyle;






            /*Style s = new Style
                {
                    TargetType = typeof(System.Windows.Controls.Border)
                };

                s.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Blue));

                //timeLineStripLineControlList[0].NormalStyle = s;
                //timeLineStripLineControlList[0].AlternateStyle = s;
                timeLineStripLineControlList[0].ElementStyle = s;*/




            //Style s  = timeLineStripLineControlList[0].NormalStyle;
            //Style p = timeLineStripLineControlList[0].AlternateStyle;

            //s.Setters Add(new Setter(Border.BackgroundProperty, Brushes.Blue));
            //p.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Red));



            //timeLineStripLineControlList[0].Background = 


            //List<TimelinePeriodControl> tytyList = _fireworkTimeline.ChildrenOfType<TimelinePeriodControl>().ToList();

            //Grid gr = tytyList[0].FindChildByType<Grid>();
            //gr.Background = Brushes.Blue;

            //List<TimelineStripLineControl> tytyList = _fireworkTimeline.ChildrenOfType<TimelineStripLineControl>().ToList();
            //TimelineStripLineControl to = test.FindChildByType<TimelineStripLineControl>();

            /*Grid gr = tytyList[0].FindChildByType<Grid>();
            gr.Background = Brushes.Blue;*/
            // gr.InvalidateVisual();
            /* tytyList[0].Background = Brushes.Red;

             Style s = new Style
             {
                 TargetType = typeof(System.Windows.Controls.Border)
             };

             s.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Blue));

             tytyList[0].AlternateStyle = s;

             s.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Coral));

             tytyList[0].NormalStyle = s;

             tytyList[0].InvalidateVisual();*/

            //if (_viewModel.FireworkManager.State == FireworkManagerState.FireworkRunning)
            //{
            //   TimelineStripLineContainer test = _fireworkTimeline.FindChildByType<TimelineStripLineContainer>();


            //Telerik.Windows.Controls.TimeBar.PeriodSpan y = (PeriodSpan)test.Items.CurrentItem;


            // List<TimelinePeriodControl> tytyList = _fireworkTimeline.ChildrenOfType<TimelinePeriodControl>().ToList();
            //TimelineStripLineControl to = test.FindChildByType<TimelineStripLineControl>();

            //            Grid gr = tytyList[0].FindChildByType<Grid>();
            //           gr.Background = Brushes.Blue;
            //          gr.InvalidateVisual();

            /*Style st = new Style
            {
                TargetType = typeof(System.Windows.Controls.Border)
            };

            st.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Blue));

            gr.Background =

            st.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Coral));

            t.NormalStyle = s;

            tt.InvalidateVisual();*/


            //VirtualizingTimeBarPanel titit = test.ChildrenOfType<VirtualizingTimeBarPanel>().FirstOrDefault();



            /*TimelinePeriodControl u = toto.FindChild<TimelinePeriodControl>(_fireworkTimeline);

            Style s = new Style
            {
                TargetType = typeof(System.Windows.Controls.Border)
            };

            s.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Blue));

            u.AlternateStyle = s;

            s.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Coral));

            u.NormalStyle = s;

            u.InvalidateVisual();

            /*List<TimelinePeriodControl> tytyList = _fireworkTimeline.ChildrenOfType<TimelinePeriodControl>().ToList();

            if (tytyList != null && tytyList.Count > 0)
            {
                foreach (TimelinePeriodControl t in tytyList)
                {


                    t.v
                    Style s = new Style
                    {
                        TargetType = typeof(System.Windows.Controls.Border)
                    };

                    s.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Blue));

                    t.AlternateStyle = s;

                    s.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Coral));

                    t.NormalStyle = s;

                    t.InvalidateVisual();
                }
            }*/
        }

       
    }
}
