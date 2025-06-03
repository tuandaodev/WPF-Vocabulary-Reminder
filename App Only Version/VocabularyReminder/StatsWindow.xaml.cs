using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VR.Domain;
using VR.Domain.Models;
using VR.Dto;
using VR.Services;

namespace VR
{
    public partial class StatsWindow : Window
    {
        private SpacedRepetitionStatsDto _currentStats;
        private List<Dictionary> _dictionaries;

        public StatsWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDictionaries();
            await LoadStats();
        }

        private async Task LoadDictionaries()
        {
            using (var context = new VocaDbContext())
            {
                _dictionaries = await context.Dictionaries.ToListAsync();
                
                // Add "All Dictionaries" option
                var allDictionaries = new List<Dictionary>
                {
                    new Dictionary { Id = 0, Name = "All Dictionaries" }
                };
                allDictionaries.AddRange(_dictionaries);
                
                DictionaryComboBox.ItemsSource = allDictionaries;
                DictionaryComboBox.SelectedIndex = 0;
            }
        }

        private async void DictionaryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await LoadStats();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadStats();
        }

        private async Task LoadStats()
        {
            try
            {
                var selectedDictionary = DictionaryComboBox.SelectedValue as int? ?? 0;
                _currentStats = await SpacedRepetitionStatsService.GetSpacedRepetitionStatsAsync(selectedDictionary);
                
                UpdateSummaryCards();
                UpdateDetailedStats();
                DrawCharts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummaryCards()
        {
            if (_currentStats == null) return;

            TotalWordsText.Text = _currentStats.TotalWords.ToString();
            NewWordsText.Text = _currentStats.NewWords.ToString();
            LearningWordsText.Text = _currentStats.LearningWords.ToString();
            DueWordsText.Text = _currentStats.DueWords.ToString();
            LearnedWordsText.Text = _currentStats.LearnedWords.ToString();
        }

        private void UpdateDetailedStats()
        {
            if (_currentStats == null) return;

            AverageEaseFactorText.Text = _currentStats.AverageEaseFactor.ToString("F2");
            TotalReviewsText.Text = _currentStats.TotalReviews.ToString();
            TotalLapsesText.Text = _currentStats.TotalLapses.ToString();

            // Calculate success rate
            double successRate = _currentStats.TotalReviews > 0 ? 
                ((double)(_currentStats.TotalReviews - _currentStats.TotalLapses) / _currentStats.TotalReviews) * 100 : 0;
            SuccessRateText.Text = $"{successRate:F1}%";

            // Calculate retention rate (learned / total)
            double retentionRate = _currentStats.TotalWords > 0 ? 
                ((double)_currentStats.LearnedWords / _currentStats.TotalWords) * 100 : 0;
            RetentionRateText.Text = $"{retentionRate:F1}%";

            // Update progress bar
            LearningProgressBar.Value = retentionRate;

            // Update data grids
            IntervalDistributionGrid.ItemsSource = _currentStats.IntervalDistribution;
            EaseFactorDistributionGrid.ItemsSource = _currentStats.EaseFactorDistribution;
        }

        private void DrawCharts()
        {
            DrawIntervalChart();
            DrawEaseFactorChart();
            DrawProgressChart();
        }

        private void DrawIntervalChart()
        {
            IntervalChart.Children.Clear();
            
            if (_currentStats?.IntervalDistribution == null || !_currentStats.IntervalDistribution.Any())
                return;

            var data = _currentStats.IntervalDistribution;
            var maxValue = data.Max(d => d.WordCount);
            var chartWidth = IntervalChart.ActualWidth > 0 ? IntervalChart.ActualWidth - 60 : 300;
            var chartHeight = IntervalChart.ActualHeight > 0 ? IntervalChart.ActualHeight - 80 : 220;
            
            var barWidth = chartWidth / data.Count - 10;
            var colors = new[] { "#2196F3", "#4CAF50", "#FF9800", "#F44336", "#9C27B0", "#00BCD4", "#795548", "#607D8B" };

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var barHeight = maxValue > 0 ? (item.WordCount / (double)maxValue) * chartHeight : 0;
                
                // Draw bar
                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i % colors.Length]))
                };
                
                Canvas.SetLeft(rect, 40 + i * (barWidth + 10));
                Canvas.SetTop(rect, chartHeight - barHeight + 20);
                IntervalChart.Children.Add(rect);

                // Draw value label
                var valueLabel = new TextBlock
                {
                    Text = item.WordCount.ToString(),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                Canvas.SetLeft(valueLabel, 40 + i * (barWidth + 10) + barWidth / 2 - 10);
                Canvas.SetTop(valueLabel, chartHeight - barHeight + 5);
                IntervalChart.Children.Add(valueLabel);

                // Draw x-axis label
                var label = new TextBlock
                {
                    Text = item.IntervalRange,
                    FontSize = 9,
                    Width = barWidth + 20,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
                
                Canvas.SetLeft(label, 30 + i * (barWidth + 10));
                Canvas.SetTop(label, chartHeight + 25);
                IntervalChart.Children.Add(label);
            }
        }

        private void DrawEaseFactorChart()
        {
            EaseFactorChart.Children.Clear();
            
            if (_currentStats?.EaseFactorDistribution == null || !_currentStats.EaseFactorDistribution.Any())
                return;

            var data = _currentStats.EaseFactorDistribution;
            var maxValue = data.Max(d => d.WordCount);
            var chartWidth = EaseFactorChart.ActualWidth > 0 ? EaseFactorChart.ActualWidth - 60 : 300;
            var chartHeight = EaseFactorChart.ActualHeight > 0 ? EaseFactorChart.ActualHeight - 80 : 220;
            
            var barWidth = chartWidth / data.Count - 10;
            var colors = new[] { "#F44336", "#FF9800", "#FFC107", "#4CAF50", "#2196F3" };

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var barHeight = maxValue > 0 ? (item.WordCount / (double)maxValue) * chartHeight : 0;
                
                // Draw bar
                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i % colors.Length]))
                };
                
                Canvas.SetLeft(rect, 40 + i * (barWidth + 10));
                Canvas.SetTop(rect, chartHeight - barHeight + 20);
                EaseFactorChart.Children.Add(rect);

                // Draw value label
                var valueLabel = new TextBlock
                {
                    Text = item.WordCount.ToString(),
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                
                Canvas.SetLeft(valueLabel, 40 + i * (barWidth + 10) + barWidth / 2 - 10);
                Canvas.SetTop(valueLabel, chartHeight - barHeight + 5);
                EaseFactorChart.Children.Add(valueLabel);

                // Draw x-axis label
                var label = new TextBlock
                {
                    Text = item.EaseRange,
                    FontSize = 9,
                    Width = barWidth + 20,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
                
                Canvas.SetLeft(label, 30 + i * (barWidth + 10));
                Canvas.SetTop(label, chartHeight + 25);
                EaseFactorChart.Children.Add(label);
            }
        }

        private void DrawProgressChart()
        {
            ProgressChart.Children.Clear();
            
            if (_currentStats?.ReviewHistory == null || !_currentStats.ReviewHistory.Any())
                return;

            var data = _currentStats.ReviewHistory;
            var chartWidth = ProgressChart.ActualWidth > 0 ? ProgressChart.ActualWidth - 80 : 800;
            var chartHeight = ProgressChart.ActualHeight > 0 ? ProgressChart.ActualHeight - 80 : 320;
            
            var maxValue = Math.Max(data.Max(d => d.ReviewCount), Math.Max(data.Max(d => d.NewCount), data.Max(d => d.LapseCount)));
            if (maxValue == 0) maxValue = 1;

            var stepX = chartWidth / Math.Max(data.Count - 1, 1);

            // Draw grid lines
            for (int i = 0; i <= 5; i++)
            {
                var y = chartHeight * i / 5 + 40;
                var line = new Line
                {
                    X1 = 60,
                    Y1 = y,
                    X2 = chartWidth + 60,
                    Y2 = y,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                ProgressChart.Children.Add(line);

                // Y-axis labels
                var label = new TextBlock
                {
                    Text = (maxValue * (5 - i) / 5).ToString("F0"),
                    FontSize = 10
                };
                Canvas.SetLeft(label, 30);
                Canvas.SetTop(label, y - 8);
                ProgressChart.Children.Add(label);
            }

            // Draw lines for each data series
            DrawProgressLine(data, maxValue, chartWidth, chartHeight, stepX, d => d.ReviewCount, Brushes.Blue, "Reviews");
            DrawProgressLine(data, maxValue, chartWidth, chartHeight, stepX, d => d.NewCount, Brushes.Green, "New");
            DrawProgressLine(data, maxValue, chartWidth, chartHeight, stepX, d => d.LapseCount, Brushes.Red, "Lapses");

            // Draw legend
            DrawLegend(chartWidth, chartHeight);
        }

        private void DrawProgressLine(List<ReviewCountOverTimeDto> data, int maxValue, double chartWidth, double chartHeight, 
                                    double stepX, Func<ReviewCountOverTimeDto, int> valueSelector, Brush brush, string label)
        {
            var points = new List<Point>();
            
            for (int i = 0; i < data.Count; i++)
            {
                var value = valueSelector(data[i]);
                var x = 60 + i * stepX;
                var y = chartHeight - (value / (double)maxValue) * chartHeight + 40;
                points.Add(new Point(x, y));
            }

            // Draw line segments
            for (int i = 0; i < points.Count - 1; i++)
            {
                var line = new Line
                {
                    X1 = points[i].X,
                    Y1 = points[i].Y,
                    X2 = points[i + 1].X,
                    Y2 = points[i + 1].Y,
                    Stroke = brush,
                    StrokeThickness = 2
                };
                ProgressChart.Children.Add(line);
            }

            // Draw points
            foreach (var point in points)
            {
                var ellipse = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = brush
                };
                Canvas.SetLeft(ellipse, point.X - 3);
                Canvas.SetTop(ellipse, point.Y - 3);
                ProgressChart.Children.Add(ellipse);
            }
        }

        private void DrawLegend(double chartWidth, double chartHeight)
        {
            var legendItems = new[]
            {
                new { Color = Brushes.Blue, Label = "Reviews" },
                new { Color = Brushes.Green, Label = "New Words" },
                new { Color = Brushes.Red, Label = "Lapses" }
            };

            for (int i = 0; i < legendItems.Length; i++)
            {
                var item = legendItems[i];
                var x = chartWidth - 150 + (i * 50);
                var y = 10;

                // Legend color box
                var rect = new Rectangle
                {
                    Width = 12,
                    Height = 12,
                    Fill = item.Color
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                ProgressChart.Children.Add(rect);

                // Legend label
                var label = new TextBlock
                {
                    Text = item.Label,
                    FontSize = 10
                };
                Canvas.SetLeft(label, x + 16);
                Canvas.SetTop(label, y - 1);
                ProgressChart.Children.Add(label);
            }
        }
    }
}