// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++ and C#: http://www.viva64.com

using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Chambers.Core;
using OxyPlot.Axes;
using OxyPlot;
using OxyPlot.Wpf;
using DateTimeAxis = OxyPlot.Axes.DateTimeAxis;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineSeries = OxyPlot.Series.LineSeries;

namespace Chambers.Wpf;

public sealed partial class ChamberViewer
{
    #region InstalledTemperature
    public static readonly DependencyProperty InstalledTemperatureColorProperty;
    public Color InstalledTemperatureColor
    {
        get => (Color) GetValue(InstalledTemperatureColorProperty);
        set => SetValue(InstalledTemperatureColorProperty, value);
    }

    private static void TargetTemperatureColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChamberViewer viewer)
        {
            viewer.TargetTemperatureColorChanged();
        }
    }

    private void TargetTemperatureColorChanged()
    {
        _targetSeries.Color = InstalledTemperatureColor.ToOxyColor();
        View.InvalidatePlot(false);
    }
    #endregion

    #region CurrentTemperatureColor
    public static readonly DependencyProperty CurrentTemperatureColorProperty;
    public Color CurrentTemperatureColor
    {
        get => (Color) GetValue(CurrentTemperatureColorProperty);
        set => SetValue(CurrentTemperatureColorProperty, value);
    }

    private static void MonitoredTemperatureColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChamberViewer viewer)
        {
            viewer.MonitoredTemperatureColorChanged();
        }
    }

    private void MonitoredTemperatureColorChanged()
    {
        _monitoredSeries.Color = CurrentTemperatureColor.ToOxyColor();
        View.InvalidatePlot(false);
    }
    #endregion

    #region Points
    public static readonly DependencyProperty PointsProperty;
    public IEnumerable<IReadOnlyTemperaturePoint>? Points
    {
        get => (IEnumerable<IReadOnlyTemperaturePoint>) GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    private static void PointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChamberViewer viewer && e.NewValue != null)
        {
            viewer.PointsChanged(e.OldValue);
        }
    }

    private void PointsChanged(object old)
    {
        _monitoredSeries.ItemsSource = Points;
        _targetSeries.ItemsSource = Points;

        if (old is INotifyCollectionChanged oldCollectionChanged)
        {
            oldCollectionChanged.CollectionChanged -= UpdateChart;
        }

        if (Points is INotifyCollectionChanged collectionChanged)
        {
            collectionChanged.CollectionChanged += UpdateChart;
        }
    }
    #endregion

    static ChamberViewer()
    {
        InstalledTemperatureColorProperty = DependencyProperty.Register(
            nameof(InstalledTemperatureColor),
            typeof(Color),
            typeof(ChamberViewer),
            new FrameworkPropertyMetadata(Colors.Black, TargetTemperatureColorChanged)
        );

        CurrentTemperatureColorProperty = DependencyProperty.Register(
            nameof(CurrentTemperatureColor),
            typeof(Color),
            typeof(ChamberViewer),
            new FrameworkPropertyMetadata(Colors.Black, MonitoredTemperatureColorChanged)
        );

        PointsProperty = DependencyProperty.Register(
            nameof(Points),
            typeof(IEnumerable<IReadOnlyTemperaturePoint>),
            typeof(ChamberViewer),
            new FrameworkPropertyMetadata(default(IEnumerable<IReadOnlyTemperaturePoint>), PointsChanged)
        );
    }

    public ChamberViewer()
    {
        InitializeComponent();

        View.Model = new PlotModel();

        View.Model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = Properties.Resources.Time,
            IsPanEnabled = false,
            MajorGridlineStyle = LineStyle.Dot,
            IsZoomEnabled = false
        });

        View.Model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = Properties.Resources.Temperature,
            IsPanEnabled = false,
            MajorGridlineStyle = LineStyle.Dot,
            IsZoomEnabled = false
        });

        _monitoredSeries = new LineSeries
        {
            MarkerStrokeThickness = 2.0,
            Color = CurrentTemperatureColor.ToOxyColor(),
            DataFieldX = nameof(IReadOnlyTemperaturePoint.Time),
            DataFieldY = nameof(IReadOnlyTemperaturePoint.Monitored)
        };

        _targetSeries = new LineSeries
        {
            MarkerStrokeThickness = 2.0,
            Color = InstalledTemperatureColor.ToOxyColor(),
            DataFieldX = nameof(IReadOnlyTemperaturePoint.Time),
            DataFieldY = nameof(IReadOnlyTemperaturePoint.Target)
        };

        View.Model.Series.Add(_monitoredSeries);
        View.Model.Series.Add(_targetSeries);
    }

    private void UpdateChart(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DateTime.Now.Subtract(_lastUpdateTime) > Interval)
        {
            var last = Points?.LastOrDefault(default(IReadOnlyTemperaturePoint));
            MonitoredTemperatureTextBox.Text = last?.Monitored.ToString("N1", CultureInfo.CurrentCulture);
            TargetTemperatureTextBox.Text = last?.Target.ToString("N1", CultureInfo.CurrentCulture);
            DeltaTextBox.Text = (last?.Target - last?.Monitored)?.ToString("N1", CultureInfo.CurrentCulture);

            View.InvalidatePlot();
            _lastUpdateTime = DateTime.Now;
        }
    }
    private DateTime _lastUpdateTime = DateTime.Now;

    private readonly LineSeries _monitoredSeries;
    private readonly LineSeries _targetSeries;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(3.0);
}
