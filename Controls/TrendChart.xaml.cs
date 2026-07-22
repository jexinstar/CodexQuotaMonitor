using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CodexQuotaMonitor.Models;

namespace CodexQuotaMonitor.Controls
{
    public partial class TrendChart : UserControl
    {
        private const double MinimumAxisMaximum = 8000.0;
        private const int AxisIntervalCount = 4;
        private const double PlotLeft = 46.0;
        private const double PlotRight = 28.0;
        private const double PlotTop = 24.0;
        private const double PlotBottom = 28.0;

        private static readonly DependencyProperty AnimationProgressProperty =
            DependencyProperty.Register(
                "AnimationProgress",
                typeof(double),
                typeof(TrendChart),
                new PropertyMetadata(0.0, OnAnimationProgressChanged));

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                "ItemsSource",
                typeof(ObservableCollection<UsageTrendPoint>),
                typeof(TrendChart),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty LineBrushProperty =
            DependencyProperty.Register(
                "LineBrush",
                typeof(Brush),
                typeof(TrendChart),
                new PropertyMetadata(null, OnVisualPropertyChanged));

        public static readonly DependencyProperty FillBrushProperty =
            DependencyProperty.Register(
                "FillBrush",
                typeof(Brush),
                typeof(TrendChart),
                new PropertyMetadata(null));

        public static readonly DependencyProperty GridBrushProperty =
            DependencyProperty.Register(
                "GridBrush",
                typeof(Brush),
                typeof(TrendChart),
                new PropertyMetadata(null, OnVisualPropertyChanged));

        public static readonly DependencyProperty ShowLabelsProperty =
            DependencyProperty.Register(
                "ShowLabels",
                typeof(bool),
                typeof(TrendChart),
                new PropertyMetadata(true, OnVisualPropertyChanged));

        private readonly List<UsageTrendPoint> _observedPoints =
            new List<UsageTrendPoint>();

        private List<Point> _displayPoints = new List<Point>();
        private List<Point> _animationStartPoints;
        private List<Point> _animationTargetPoints;
        private IList<UsageTrendPoint> _renderedItems;
        private bool _hasRenderedData;

        public TrendChart()
        {
            InitializeComponent();

            if (ReadLocalValue(LineBrushProperty) == DependencyProperty.UnsetValue)
            {
                SetResourceReference(LineBrushProperty, "AccentBlue");
            }

            if (ReadLocalValue(FillBrushProperty) == DependencyProperty.UnsetValue)
            {
                SetResourceReference(FillBrushProperty, "AccentBlue");
            }

            if (ReadLocalValue(GridBrushProperty) == DependencyProperty.UnsetValue)
            {
                SetResourceReference(GridBrushProperty, "CaptionText");
            }

            Loaded += TrendChart_OnLoaded;
            Unloaded += TrendChart_OnUnloaded;
            SizeChanged += TrendChart_OnSizeChanged;
        }

        public ObservableCollection<UsageTrendPoint> ItemsSource
        {
            get { return (ObservableCollection<UsageTrendPoint>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public Brush LineBrush
        {
            get { return (Brush)GetValue(LineBrushProperty); }
            set { SetValue(LineBrushProperty, value); }
        }

        public Brush FillBrush
        {
            get { return (Brush)GetValue(FillBrushProperty); }
            set { SetValue(FillBrushProperty, value); }
        }

        public Brush GridBrush
        {
            get { return (Brush)GetValue(GridBrushProperty); }
            set { SetValue(GridBrushProperty, value); }
        }

        public bool ShowLabels
        {
            get { return (bool)GetValue(ShowLabelsProperty); }
            set { SetValue(ShowLabelsProperty, value); }
        }

        private double AnimationProgress
        {
            get { return (double)GetValue(AnimationProgressProperty); }
            set { SetValue(AnimationProgressProperty, value); }
        }

        private static void OnItemsSourceChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var chart = (TrendChart)dependencyObject;
            chart.DetachCollection(e.OldValue as ObservableCollection<UsageTrendPoint>);
            chart.AttachCollection(e.NewValue as ObservableCollection<UsageTrendPoint>);
            chart._hasRenderedData = false;
            chart.RequestRender(false);
        }

        private static void OnVisualPropertyChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            ((TrendChart)dependencyObject).RequestRender(false);
        }

        private static void OnAnimationProgressChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            ((TrendChart)dependencyObject).DrawInterpolatedFrame((double)e.NewValue);
        }

        private void TrendChart_OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachCollection(ItemsSource);
            RenderChart(false);
        }

        private void TrendChart_OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachCollection(ItemsSource);
            BeginAnimation(AnimationProgressProperty, null);
            RevealClip.BeginAnimation(RectangleGeometry.RectProperty, null);
        }

        private void TrendChart_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded)
            {
                RenderChart(false);
            }
        }

        private void AttachCollection(ObservableCollection<UsageTrendPoint> collection)
        {
            if (collection == null)
            {
                ClearPointSubscriptions();
                return;
            }

            collection.CollectionChanged -= ItemsSource_OnCollectionChanged;
            collection.CollectionChanged += ItemsSource_OnCollectionChanged;
            RefreshPointSubscriptions(collection);
        }

        private void DetachCollection(ObservableCollection<UsageTrendPoint> collection)
        {
            if (collection != null)
            {
                collection.CollectionChanged -= ItemsSource_OnCollectionChanged;
            }

            ClearPointSubscriptions();
        }

        private void RefreshPointSubscriptions(
            IEnumerable<UsageTrendPoint> points)
        {
            ClearPointSubscriptions();

            if (points == null)
            {
                return;
            }

            foreach (UsageTrendPoint point in points)
            {
                if (point == null || _observedPoints.Contains(point))
                {
                    continue;
                }

                point.PropertyChanged += TrendPoint_OnPropertyChanged;
                _observedPoints.Add(point);
            }
        }

        private void ClearPointSubscriptions()
        {
            foreach (UsageTrendPoint point in _observedPoints)
            {
                point.PropertyChanged -= TrendPoint_OnPropertyChanged;
            }

            _observedPoints.Clear();
        }

        private void ItemsSource_OnCollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            RefreshPointSubscriptions(ItemsSource);
            RequestRender(true);
        }

        private void TrendPoint_OnPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == "Date"
                || e.PropertyName == "Value")
            {
                RequestRender(true);
            }
        }

        private void RequestRender(bool animateDataChange)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Render,
                    new Action<bool>(RequestRender),
                    animateDataChange);
                return;
            }

            RenderChart(animateDataChange);
        }

        private void RenderChart(bool animateDataChange)
        {
            double width = ActualWidth;
            double height = ActualHeight;
            if (width <= PlotLeft + PlotRight || height <= PlotTop + PlotBottom)
            {
                ClearChart();
                return;
            }

            List<UsageTrendPoint> items = SnapshotItems();
            double axisMaximum = CalculateAxisMaximum(items);
            DrawAxes(items, axisMaximum, width, height);

            if (items.Count == 0)
            {
                ClearDataVisuals();
                _displayPoints.Clear();
                _renderedItems = items;
                _hasRenderedData = false;
                SetFullRevealClip(width, height);
                return;
            }

            List<Point> targetPoints = CalculatePoints(items, axisMaximum, width, height);
            bool canInterpolate = animateDataChange
                                  && _hasRenderedData
                                  && _displayPoints.Count == targetPoints.Count;

            if (canInterpolate)
            {
                AnimateDataChange(targetPoints, items, width, height);
            }
            else
            {
                BeginAnimation(AnimationProgressProperty, null);
                _displayPoints = new List<Point>(targetPoints);
                _renderedItems = items;
                DrawDataFrame(targetPoints, items);

                if (!_hasRenderedData)
                {
                    AnimateInitialReveal(width, height);
                }
                else
                {
                    SetFullRevealClip(width, height);
                }
            }

            _hasRenderedData = true;
        }

        private List<UsageTrendPoint> SnapshotItems()
        {
            var items = new List<UsageTrendPoint>();
            ObservableCollection<UsageTrendPoint> source = ItemsSource;
            if (source == null)
            {
                return items;
            }

            foreach (UsageTrendPoint point in source)
            {
                if (point != null)
                {
                    items.Add(point);
                }
            }

            return items;
        }

        private static double CalculateAxisMaximum(IList<UsageTrendPoint> items)
        {
            double maximum = 0.0;
            foreach (UsageTrendPoint item in items)
            {
                maximum = Math.Max(maximum, NormalizeValue(item.Value));
            }

            if (maximum <= MinimumAxisMaximum)
            {
                return MinimumAxisMaximum;
            }

            double interval = NiceCeiling(maximum / AxisIntervalCount);
            return interval * AxisIntervalCount;
        }

        private static double NiceCeiling(double value)
        {
            if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
            {
                return 1.0;
            }

            double magnitude = Math.Pow(10.0, Math.Floor(Math.Log10(value)));
            double normalized = value / magnitude;
            double niceNormalized;

            if (normalized <= 1.0)
            {
                niceNormalized = 1.0;
            }
            else if (normalized <= 2.0)
            {
                niceNormalized = 2.0;
            }
            else if (normalized <= 2.5)
            {
                niceNormalized = 2.5;
            }
            else if (normalized <= 5.0)
            {
                niceNormalized = 5.0;
            }
            else
            {
                niceNormalized = 10.0;
            }

            return niceNormalized * magnitude;
        }

        private static List<Point> CalculatePoints(
            IList<UsageTrendPoint> items,
            double axisMaximum,
            double width,
            double height)
        {
            var points = new List<Point>(items.Count);
            double plotWidth = Math.Max(0.0, width - PlotLeft - PlotRight);
            double plotHeight = Math.Max(0.0, height - PlotTop - PlotBottom);

            for (int index = 0; index < items.Count; index++)
            {
                double x = items.Count == 1
                    ? PlotLeft + plotWidth / 2.0
                    : PlotLeft + plotWidth * index / (items.Count - 1.0);
                double ratio = axisMaximum <= 0
                    ? 0.0
                    : NormalizeValue(items[index].Value) / axisMaximum;
                ratio = Math.Max(0.0, Math.Min(1.0, ratio));
                double y = PlotTop + plotHeight * (1.0 - ratio);
                points.Add(new Point(x, y));
            }

            return points;
        }

        private void DrawAxes(
            IList<UsageTrendPoint> items,
            double axisMaximum,
            double width,
            double height)
        {
            double plotWidth = width - PlotLeft - PlotRight;
            double plotHeight = height - PlotTop - PlotBottom;
            var gridGeometry = new PathGeometry();

            LabelsCanvas.Children.Clear();

            for (int index = 0; index <= AxisIntervalCount; index++)
            {
                double y = PlotTop + plotHeight * index / AxisIntervalCount;
                var figure = new PathFigure
                {
                    StartPoint = new Point(PlotLeft, y),
                    IsClosed = false,
                    IsFilled = false
                };
                figure.Segments.Add(
                    new LineSegment(new Point(PlotLeft + plotWidth, y), true));
                gridGeometry.Figures.Add(figure);

                if (ShowLabels)
                {
                    double value = axisMaximum
                                   * (AxisIntervalCount - index)
                                   / AxisIntervalCount;
                    AddAxisLabel(FormatAxisValue(value), 0.0, y - 9.0, 38.0);
                }
            }

            GridPath.Data = gridGeometry;

            if (!ShowLabels)
            {
                return;
            }

            for (int index = 0; index < items.Count; index++)
            {
                double x = items.Count == 1
                    ? PlotLeft + plotWidth / 2.0
                    : PlotLeft + plotWidth * index / (items.Count - 1.0);
                AddAxisLabel(
                    items[index].Date.ToString("MM-dd", CultureInfo.InvariantCulture),
                    x - 28.0,
                    height - PlotBottom + 7.0,
                    56.0);
            }
        }

        private void AddAxisLabel(string text, double left, double top, double width)
        {
            var label = new TextBlock
            {
                Text = text,
                Width = width,
                TextAlignment = TextAlignment.Center,
                Foreground = GridBrush
            };
            label.SetResourceReference(FrameworkElement.StyleProperty, "CaptionStyle");
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, top);
            LabelsCanvas.Children.Add(label);
        }

        private void AnimateInitialReveal(double width, double height)
        {
            Rect fullRect = new Rect(PlotLeft, 0.0, width - PlotLeft, height);
            RevealClip.BeginAnimation(RectangleGeometry.RectProperty, null);
            RevealClip.Rect = fullRect;

            var animation = new RectAnimation
            {
                From = new Rect(PlotLeft, 0.0, 0.0, height),
                To = fullRect,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            };
            RevealClip.BeginAnimation(
                RectangleGeometry.RectProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private void AnimateDataChange(
            List<Point> targetPoints,
            IList<UsageTrendPoint> items,
            double width,
            double height)
        {
            List<Point> currentPoints = new List<Point>(_displayPoints);
            BeginAnimation(AnimationProgressProperty, null);
            SetFullRevealClip(width, height);

            _animationStartPoints = currentPoints;
            _animationTargetPoints = new List<Point>(targetPoints);
            _renderedItems = items;
            AnimationProgress = 0.0;

            var animation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            };

            animation.Completed += delegate
            {
                _displayPoints = new List<Point>(_animationTargetPoints);
                AnimationProgress = 1.0;
                BeginAnimation(AnimationProgressProperty, null);
                DrawDataFrame(_displayPoints, _renderedItems);
            };

            BeginAnimation(
                AnimationProgressProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private void DrawInterpolatedFrame(double progress)
        {
            if (_animationStartPoints == null
                || _animationTargetPoints == null
                || _renderedItems == null
                || _animationStartPoints.Count != _animationTargetPoints.Count)
            {
                return;
            }

            progress = Math.Max(0.0, Math.Min(1.0, progress));
            var currentPoints = new List<Point>(_animationTargetPoints.Count);

            for (int index = 0; index < _animationTargetPoints.Count; index++)
            {
                Point start = _animationStartPoints[index];
                Point target = _animationTargetPoints[index];
                currentPoints.Add(
                    new Point(
                        start.X + (target.X - start.X) * progress,
                        start.Y + (target.Y - start.Y) * progress));
            }

            _displayPoints = currentPoints;
            DrawDataFrame(currentPoints, _renderedItems);
        }

        private void DrawDataFrame(
            IList<Point> points,
            IList<UsageTrendPoint> items)
        {
            TrendLine.Points = new PointCollection(points);
            AreaPath.Data = CreateAreaGeometry(points, ActualHeight - PlotBottom);
            PointsCanvas.Children.Clear();

            for (int index = 0; index < points.Count; index++)
            {
                var pointMarker = new Ellipse
                {
                    Width = 8.0,
                    Height = 8.0,
                    Stroke = LineBrush,
                    StrokeThickness = 2.0
                };
                pointMarker.SetResourceReference(Shape.FillProperty, "PrimaryText");
                Canvas.SetLeft(pointMarker, points[index].X - 4.0);
                Canvas.SetTop(pointMarker, points[index].Y - 4.0);
                PointsCanvas.Children.Add(pointMarker);
            }

            RemoveMaximumLabel();
            if (ShowLabels && points.Count > 0 && items != null && items.Count == points.Count)
            {
                int maximumIndex = FindMaximumIndex(items);
                AddMaximumLabel(items[maximumIndex].Value, points[maximumIndex]);
            }
        }

        private static Geometry CreateAreaGeometry(
            IList<Point> points,
            double baseline)
        {
            if (points == null || points.Count < 2)
            {
                return Geometry.Empty;
            }

            var figure = new PathFigure
            {
                StartPoint = new Point(points[0].X, baseline),
                IsClosed = true,
                IsFilled = true
            };

            figure.Segments.Add(new LineSegment(points[0], true));
            for (int index = 1; index < points.Count; index++)
            {
                figure.Segments.Add(new LineSegment(points[index], true));
            }

            figure.Segments.Add(
                new LineSegment(new Point(points[points.Count - 1].X, baseline), true));
            return new PathGeometry(new[] { figure });
        }

        private void AddMaximumLabel(double value, Point point)
        {
            var label = new TextBlock
            {
                Name = "MaximumValueLabel",
                Text = NormalizeValue(value).ToString("N0", CultureInfo.CurrentCulture),
                MinWidth = 44.0,
                Padding = new Thickness(5.0, 2.0, 5.0, 2.0),
                TextAlignment = TextAlignment.Center,
                Background = LineBrush
            };
            label.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryText");
            label.SetResourceReference(FrameworkElement.StyleProperty, "CaptionStyle");
            Canvas.SetLeft(label, Math.Max(PlotLeft, point.X - 24.0));
            Canvas.SetTop(label, Math.Max(1.0, point.Y - 30.0));
            LabelsCanvas.Children.Add(label);
        }

        private void RemoveMaximumLabel()
        {
            for (int index = LabelsCanvas.Children.Count - 1; index >= 0; index--)
            {
                var label = LabelsCanvas.Children[index] as FrameworkElement;
                if (label != null && label.Name == "MaximumValueLabel")
                {
                    LabelsCanvas.Children.RemoveAt(index);
                }
            }
        }

        private static int FindMaximumIndex(IList<UsageTrendPoint> items)
        {
            int maximumIndex = 0;
            double maximum = double.MinValue;

            for (int index = 0; index < items.Count; index++)
            {
                double value = NormalizeValue(items[index].Value);
                if (value > maximum)
                {
                    maximum = value;
                    maximumIndex = index;
                }
            }

            return maximumIndex;
        }

        private static double NormalizeValue(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) || value < 0
                ? 0.0
                : value;
        }

        private static string FormatAxisValue(double value)
        {
            if (Math.Abs(value) < 0.001)
            {
                return "0";
            }

            if (Math.Abs(value) >= 1000.0)
            {
                return (value / 1000.0).ToString("0.#", CultureInfo.InvariantCulture) + "K";
            }

            return value.ToString("0", CultureInfo.InvariantCulture);
        }

        private void SetFullRevealClip(double width, double height)
        {
            RevealClip.BeginAnimation(RectangleGeometry.RectProperty, null);
            RevealClip.Rect = new Rect(0.0, 0.0, width, height);
        }

        private void ClearDataVisuals()
        {
            TrendLine.Points = new PointCollection();
            AreaPath.Data = Geometry.Empty;
            PointsCanvas.Children.Clear();
            RemoveMaximumLabel();
        }

        private void ClearChart()
        {
            GridPath.Data = Geometry.Empty;
            LabelsCanvas.Children.Clear();
            ClearDataVisuals();
            _displayPoints.Clear();
            _hasRenderedData = false;
        }
    }
}
