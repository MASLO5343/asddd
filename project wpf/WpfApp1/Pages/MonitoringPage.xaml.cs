using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1.Pages
{
    public partial class MonitoringPage : Page
    {
        public ISeries[] CpuSeries { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public MonitoringPage()
        {
            InitializeComponent();
            LoadChartData();
            DataContext = this;
        }

        private void LoadChartData()
        {
            CpuSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = new List<double> { 10, 20, 15, 30, 50, 40, 60 },
                    Stroke = new SolidColorPaint(SKColors.Lime, 2),
                    GeometrySize = 10,
                    Fill = null
                }
            };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new List<string> { "10:00", "10:05", "10:10", "10:15", "10:20", "10:25", "10:30" },
                    LabelsPaint = new SolidColorPaint(SKColors.White)
                }
            };

            YAxes = new Axis[]
            {
                new Axis
                {
                    MinLimit = 0,
                    MaxLimit = 100,
                    LabelsPaint = new SolidColorPaint(SKColors.White)
                }
            };
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Обновление данных... (заглушка)");
            // тут можно симулировать обновление графика и статусов серверов
        }
    }
}

