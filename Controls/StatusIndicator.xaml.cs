using System;
using System.Windows;
using System.Windows.Controls;
using CodexQuotaMonitor.Models;

namespace CodexQuotaMonitor.Controls
{
    public partial class StatusIndicator : UserControl
    {
        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register(
                "StatusText",
                typeof(string),
                typeof(StatusIndicator),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty StatusTypeProperty =
            DependencyProperty.Register(
                "StatusType",
                typeof(SyncStatusType),
                typeof(StatusIndicator),
                new PropertyMetadata(SyncStatusType.Normal));

        public static readonly DependencyProperty LastUpdateProperty =
            DependencyProperty.Register(
                "LastUpdate",
                typeof(DateTime),
                typeof(StatusIndicator),
                new PropertyMetadata(DateTime.MinValue));

        public StatusIndicator()
        {
            InitializeComponent();
        }

        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        public SyncStatusType StatusType
        {
            get { return (SyncStatusType)GetValue(StatusTypeProperty); }
            set { SetValue(StatusTypeProperty, value); }
        }

        public DateTime LastUpdate
        {
            get { return (DateTime)GetValue(LastUpdateProperty); }
            set { SetValue(LastUpdateProperty, value); }
        }
    }
}
