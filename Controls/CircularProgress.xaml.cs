using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CodexQuotaMonitor.Controls
{
    public partial class CircularProgress : UserControl
    {
        private static readonly DependencyProperty AnimatedValueProperty =
            DependencyProperty.Register(
                "AnimatedValue",
                typeof(double),
                typeof(CircularProgress),
                new PropertyMetadata(0.0, OnAnimatedValueChanged));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                "Value",
                typeof(double),
                typeof(CircularProgress),
                new PropertyMetadata(0.0, OnProgressValueChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                "Maximum",
                typeof(double),
                typeof(CircularProgress),
                new PropertyMetadata(100.0, OnProgressValueChanged));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                "StrokeThickness",
                typeof(double),
                typeof(CircularProgress),
                new FrameworkPropertyMetadata(
                    10.0,
                    FrameworkPropertyMetadataOptions.AffectsMeasure,
                    OnStrokeThicknessChanged,
                    CoerceStrokeThickness));

        public static readonly DependencyProperty ProgressBrushProperty =
            DependencyProperty.Register(
                "ProgressBrush",
                typeof(Brush),
                typeof(CircularProgress),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TrackBrushProperty =
            DependencyProperty.Register(
                "TrackBrush",
                typeof(Brush),
                typeof(CircularProgress),
                new PropertyMetadata(null));

        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(
                "DisplayText",
                typeof(string),
                typeof(CircularProgress),
                new PropertyMetadata("0%"));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(
                "Subtitle",
                typeof(string),
                typeof(CircularProgress),
                new PropertyMetadata(string.Empty));

        public CircularProgress()
        {
            InitializeComponent();

            if (ReadLocalValue(ProgressBrushProperty) == DependencyProperty.UnsetValue)
            {
                SetResourceReference(ProgressBrushProperty, "AccentBlue");
            }

            if (ReadLocalValue(TrackBrushProperty) == DependencyProperty.UnsetValue)
            {
                SetResourceReference(TrackBrushProperty, "GlassCardBackground");
            }

            Loaded += CircularProgress_OnLoaded;
            SizeChanged += CircularProgress_OnSizeChanged;
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public Brush ProgressBrush
        {
            get { return (Brush)GetValue(ProgressBrushProperty); }
            set { SetValue(ProgressBrushProperty, value); }
        }

        public Brush TrackBrush
        {
            get { return (Brush)GetValue(TrackBrushProperty); }
            set { SetValue(TrackBrushProperty, value); }
        }

        public string DisplayText
        {
            get { return (string)GetValue(DisplayTextProperty); }
            set { SetValue(DisplayTextProperty, value); }
        }

        public string Subtitle
        {
            get { return (string)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }

        private double AnimatedValue
        {
            get { return (double)GetValue(AnimatedValueProperty); }
            set { SetValue(AnimatedValueProperty, value); }
        }

        internal static double NormalizeValue(double value, double maximum)
        {
            if (double.IsNaN(value)
                || double.IsInfinity(value)
                || double.IsNaN(maximum)
                || double.IsInfinity(maximum)
                || maximum <= 0)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(value, maximum));
        }

        private static void OnProgressValueChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularProgress)dependencyObject;
            if (control.IsLoaded)
            {
                control.AnimateToCurrentValue();
            }
            else
            {
                control.AnimatedValue = 0;
                control.UpdateGeometry();
            }
        }

        private static void OnStrokeThicknessChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            ((CircularProgress)dependencyObject).UpdateGeometry();
        }

        private static object CoerceStrokeThickness(
            DependencyObject dependencyObject,
            object baseValue)
        {
            double value = (double)baseValue;
            return double.IsNaN(value) || double.IsInfinity(value) || value < 0
                ? 0.0
                : value;
        }

        private static void OnAnimatedValueChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            ((CircularProgress)dependencyObject).UpdateGeometry();
        }

        private void CircularProgress_OnLoaded(object sender, RoutedEventArgs e)
        {
            AnimatedValue = 0;
            AnimateToCurrentValue();
        }

        private void CircularProgress_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGeometry();
        }

        private void AnimateToCurrentValue()
        {
            double targetValue = NormalizeValue(Value, Maximum);
            double currentValue = AnimatedValue;

            BeginAnimation(AnimatedValueProperty, null);
            AnimatedValue = targetValue;

            var animation = new DoubleAnimation
            {
                From = currentValue,
                To = targetValue,
                Duration = TimeSpan.FromMilliseconds(550),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            };

            BeginAnimation(
                AnimatedValueProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private void UpdateGeometry()
        {
            if (TrackPath == null || ProgressPath == null)
            {
                return;
            }

            double diameter = Math.Min(ActualWidth, ActualHeight);
            double radius = Math.Max(0, (diameter - StrokeThickness) / 2.0);
            if (radius <= 0)
            {
                TrackPath.Data = Geometry.Empty;
                ProgressPath.Data = Geometry.Empty;
                return;
            }

            var center = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
            TrackPath.Data = CreateArcGeometry(center, radius, 360.0);

            double normalizedValue = NormalizeValue(AnimatedValue, Maximum);
            double angle = Maximum <= 0 ? 0 : normalizedValue / Maximum * 360.0;
            ProgressPath.Data = angle <= 0
                ? Geometry.Empty
                : CreateArcGeometry(center, radius, angle);
        }

        private static PathGeometry CreateArcGeometry(Point center, double radius, double angle)
        {
            Point startPoint = PointOnCircle(center, radius, -90.0);
            var figure = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false,
                IsFilled = false
            };

            if (angle >= 359.999)
            {
                Point bottomPoint = PointOnCircle(center, radius, 90.0);
                figure.Segments.Add(new ArcSegment(
                    bottomPoint,
                    new Size(radius, radius),
                    0,
                    false,
                    SweepDirection.Clockwise,
                    true));
                figure.Segments.Add(new ArcSegment(
                    startPoint,
                    new Size(radius, radius),
                    0,
                    false,
                    SweepDirection.Clockwise,
                    true));
            }
            else
            {
                Point endPoint = PointOnCircle(center, radius, angle - 90.0);
                figure.Segments.Add(new ArcSegment(
                    endPoint,
                    new Size(radius, radius),
                    0,
                    angle > 180.0,
                    SweepDirection.Clockwise,
                    true));
            }

            return new PathGeometry(new[] { figure });
        }

        private static Point PointOnCircle(Point center, double radius, double angle)
        {
            double radians = angle * Math.PI / 180.0;
            return new Point(
                center.X + radius * Math.Cos(radians),
                center.Y + radius * Math.Sin(radians));
        }
    }
}
