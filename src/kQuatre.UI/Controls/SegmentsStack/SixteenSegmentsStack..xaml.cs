using System.Collections.ObjectModel;
using System.Windows;

namespace fr.guiet.kquatre.ui.controls.segments
{
    /// <summary>
    /// Interaction logic for SixteenSegmentsStack.xaml
    /// </summary>
    public partial class SixteenSegmentsStack : SegmentsStackBase
    {
        /// <summary>
        /// Stores chars from the splitted value string
        /// </summary>
        private ObservableCollection<CharItem> ValueChars;

        public SixteenSegmentsStack()
        {
            InitializeComponent();

            VertSegDivider = defVertDividerSixteen;
            HorizSegDivider = defHorizDividerSixteen;
        }

        public override void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ValueChars = GetCharsArray();
            SegmentsArray.ItemsSource = ValueChars;
        }
    }
}
