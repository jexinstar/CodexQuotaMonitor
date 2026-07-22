using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CodexQuotaMonitor.Controls
{
    public partial class UsageProgressItem : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title",
                typeof(string),
                typeof(UsageProgressItem),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty UsedProperty =
            DependencyProperty.Register(
                "Used",
                typeof(double),
                typeof(UsageProgressItem),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                "Maximum",
                typeof(double),
                typeof(UsageProgressItem),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty PercentageProperty =
            DependencyProperty.Register(
                "Percentage",
                typeof(double),
                typeof(UsageProgressItem),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty ProgressBrushProperty =
            DependencyProperty.Register(
                "ProgressBrush",
                typeof(Brush),
                typeof(UsageProgressItem),
                new PropertyMetadata(null));

        public UsageProgressItem()
        {
            InitializeComponent();

            if (ReadLocalValue(ProgressBrushProperty) == DependencyProperty.UnsetValue)
            {
                SetResourceReference(ProgressBrushProperty, "AccentBlue");
            }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public double Used
        {
            get { return (double)GetValue(UsedProperty); }
            set { SetValue(UsedProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double Percentage
        {
            get { return (double)GetValue(PercentageProperty); }
            set { SetValue(PercentageProperty, value); }
        }

        public Brush ProgressBrush
        {
            get { return (Brush)GetValue(ProgressBrushProperty); }
            set { SetValue(ProgressBrushProperty, value); }
        }
    }
}
