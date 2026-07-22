using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CodexQuotaMonitor.Controls
{
    public partial class QuotaCard : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title",
                typeof(string),
                typeof(QuotaCard),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                "Icon",
                typeof(object),
                typeof(QuotaCard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty UsedValueProperty =
            DependencyProperty.Register(
                "UsedValue",
                typeof(double),
                typeof(QuotaCard),
                new PropertyMetadata(0.0, OnNumericValueChanged));

        public static readonly DependencyProperty MaximumValueProperty =
            DependencyProperty.Register(
                "MaximumValue",
                typeof(double),
                typeof(QuotaCard),
                new PropertyMetadata(0.0, OnNumericValueChanged));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(
                "Unit",
                typeof(string),
                typeof(QuotaCard),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty AccentBrushProperty =
            DependencyProperty.Register(
                "AccentBrush",
                typeof(Brush),
                typeof(QuotaCard),
                new PropertyMetadata(null));

        public static readonly DependencyProperty PercentageProperty =
            DependencyProperty.Register(
                "Percentage",
                typeof(double),
                typeof(QuotaCard),
                new PropertyMetadata(0.0, OnNumericValueChanged));

        private static readonly DependencyProperty AnimatedUsedValueProperty =
            DependencyProperty.Register(
                "AnimatedUsedValue",
                typeof(double),
                typeof(QuotaCard),
                new PropertyMetadata(0.0));

        private static readonly DependencyProperty AnimatedMaximumValueProperty =
            DependencyProperty.Register(
                "AnimatedMaximumValue",
                typeof(double),
                typeof(QuotaCard),
                new PropertyMetadata(0.0));

        private static readonly DependencyProperty AnimatedPercentageProperty =
            DependencyProperty.Register(
                "AnimatedPercentage",
                typeof(double),
                typeof(QuotaCard),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty ProgressSubtitleProperty =
            DependencyProperty.Register(
                "ProgressSubtitle",
                typeof(string),
                typeof(QuotaCard),
                new PropertyMetadata("已使用"));

        public static readonly DependencyProperty IsCompactProperty =
            DependencyProperty.Register(
                "IsCompact",
                typeof(bool),
                typeof(QuotaCard),
                new PropertyMetadata(false));

        public QuotaCard()
        {
            InitializeComponent();

            if (ReadLocalValue(AccentBrushProperty) == DependencyProperty.UnsetValue)
            {
                SetResourceReference(AccentBrushProperty, "AccentBlue");
            }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public object Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public double UsedValue
        {
            get { return (double)GetValue(UsedValueProperty); }
            set { SetValue(UsedValueProperty, value); }
        }

        public double MaximumValue
        {
            get { return (double)GetValue(MaximumValueProperty); }
            set { SetValue(MaximumValueProperty, value); }
        }

        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public Brush AccentBrush
        {
            get { return (Brush)GetValue(AccentBrushProperty); }
            set { SetValue(AccentBrushProperty, value); }
        }

        public double Percentage
        {
            get { return (double)GetValue(PercentageProperty); }
            set { SetValue(PercentageProperty, value); }
        }

        public double AnimatedUsedValue
        {
            get { return (double)GetValue(AnimatedUsedValueProperty); }
            private set { SetValue(AnimatedUsedValueProperty, value); }
        }

        public double AnimatedMaximumValue
        {
            get { return (double)GetValue(AnimatedMaximumValueProperty); }
            private set { SetValue(AnimatedMaximumValueProperty, value); }
        }

        public double AnimatedPercentage
        {
            get { return (double)GetValue(AnimatedPercentageProperty); }
            private set { SetValue(AnimatedPercentageProperty, value); }
        }

        public string ProgressSubtitle
        {
            get { return (string)GetValue(ProgressSubtitleProperty); }
            set { SetValue(ProgressSubtitleProperty, value); }
        }

        public bool IsCompact
        {
            get { return (bool)GetValue(IsCompactProperty); }
            set { SetValue(IsCompactProperty, value); }
        }

        private static void OnNumericValueChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (QuotaCard)dependencyObject;
            DependencyProperty animatedProperty;
            bool isPercentage = false;

            if (e.Property == UsedValueProperty)
            {
                animatedProperty = AnimatedUsedValueProperty;
            }
            else if (e.Property == MaximumValueProperty)
            {
                animatedProperty = AnimatedMaximumValueProperty;
            }
            else
            {
                animatedProperty = AnimatedPercentageProperty;
                isPercentage = true;
            }

            control.AnimateNumericValue(
                animatedProperty,
                NormalizeNumber((double)e.NewValue, isPercentage));
        }

        private void AnimateNumericValue(
            DependencyProperty animatedProperty,
            double targetValue)
        {
            double currentValue = (double)GetValue(animatedProperty);
            BeginAnimation(animatedProperty, null);
            SetValue(animatedProperty, targetValue);

            if (!IsLoaded || Math.Abs(currentValue - targetValue) < 0.001)
            {
                return;
            }

            var animation = new DoubleAnimation
            {
                From = currentValue,
                To = targetValue,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            };
            BeginAnimation(
                animatedProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private static double NormalizeNumber(double value, bool isPercentage)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0.0;
            }

            value = Math.Max(0.0, value);
            return isPercentage ? Math.Min(100.0, value) : value;
        }
    }
}
