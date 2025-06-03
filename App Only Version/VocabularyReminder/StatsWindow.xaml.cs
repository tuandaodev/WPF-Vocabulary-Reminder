using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
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
        private bool _isLoading = false;
        private readonly DispatcherTimer _refreshTimer;
        private DateTime _lastDataFetch = DateTime.MinValue;
        private const int CACHE_DURATION_MINUTES = 5;

        public StatsWindow()
        {
            InitializeComponent();
            
            // Initialize auto-refresh timer
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshIfNeeded();
            _refreshTimer.Start();
        }

        private async Task RefreshIfNeeded()
        {
            // Auto-refresh data every 5 minutes
            if (DateTime.Now - _lastDataFetch > TimeSpan.FromMinutes(CACHE_DURATION_MINUTES))
            {
                await LoadStats();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoadingState(true);
                await LoadDictionaries();
                await LoadStats();
            }
            finally
            {
                ShowLoadingState(false);
            }
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
            if (!_isLoading)
            {
                await LoadStats();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _lastDataFetch = DateTime.MinValue; // Force refresh
            await LoadStats();
        }

        private async Task LoadStats()
        {
            if (_isLoading) return;
            
            try
            {
                _isLoading = true;
                ShowLoadingState(true);
                
                var selectedDictionary = DictionaryComboBox.SelectedValue as int? ?? 0;
                
                // Use background thread for data loading
                var stats = await Task.Run(async () =>
                    await SpacedRepetitionStatsService.GetSpacedRepetitionStatsAsync(selectedDictionary));
                
                _currentStats = stats;
                _lastDataFetch = DateTime.Now;
                
                // Update UI on main thread
                await Dispatcher.InvokeAsync(() =>
                {
                    UpdateSummaryCards();
                    UpdateDetailedStats();
                });
                
                // Draw charts with delay to allow UI to update
                await Task.Delay(50);
                await Dispatcher.InvokeAsync(() => DrawCharts());
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                    MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                _isLoading = false;
                ShowLoadingState(false);
            }
        }

        private void ShowLoadingState(bool isLoading)
        {
            RefreshButton.IsEnabled = !isLoading;
            DictionaryComboBox.IsEnabled = !isLoading;
            
            if (isLoading)
            {
                this.Cursor = System.Windows.Input.Cursors.Wait;
            }
            else
            {
                this.Cursor = System.Windows.Input.Cursors.Arrow;
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
            DrawBarChart(IntervalChart, _currentStats?.IntervalDistribution,
                d => d.WordCount, d => d.IntervalRange,
                new[] { "#2196F3", "#4CAF50", "#FF9800", "#F44336", "#9C27B0", "#00BCD4", "#795548", "#607D8B" });
        }

        private void DrawEaseFactorChart()
        {
            DrawBarChart(EaseFactorChart, _currentStats?.EaseFactorDistribution,
                d => d.WordCount, d => d.EaseRange,
                new[] { "#F44336", "#FF9800", "#FFC107", "#4CAF50", "#2196F3" });
        }

        private void DrawBarChart<T>(Canvas canvas, IEnumerable<T> data,
            Func<T, int> valueSelector, Func<T, string> labelSelector, string[] colors) where T : class
        {
            canvas.Children.Clear();
            
            if (data == null || !data.Any())
                return;

            var dataList = data.ToList();
            var maxValue = dataList.Max(valueSelector);
            if (maxValue == 0) return;

            var chartWidth = Math.Max(canvas.ActualWidth > 0 ? canvas.ActualWidth - 60 : 300, 200);
            var chartHeight = Math.Max(canvas.ActualHeight > 0 ? canvas.ActualHeight - 80 : 220, 150);
            
            var barWidth = Math.Max((chartWidth / dataList.Count) - 10, 20);

            // Use a more efficient drawing approach
            var elements = new List<UIElement>();

            for (int i = 0; i < dataList.Count; i++)
            {
                var item = dataList[i];
                var value = valueSelector(item);
                var label = labelSelector(item);
                var barHeight = (value / (double)maxValue) * chartHeight;
                
                // Create bar
                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i % colors.Length])),
                    ToolTip = $"{label}: {value}"
                };
                
                Canvas.SetLeft(rect, 40 + i * (barWidth + 10));
                Canvas.SetTop(rect, chartHeight - barHeight + 20);
                elements.Add(rect);

                // Create value label
                if (barHeight > 15) // Only show label if bar is tall enough
                {
                    var valueLabel = new TextBlock
                    {
                        Text = value.ToString(),
                        FontSize = 10,
                        Foreground = Brushes.White,
                        FontWeight = FontWeights.Bold
                    };
                    
                    Canvas.SetLeft(valueLabel, 40 + i * (barWidth + 10) + barWidth / 2 - 8);
                    Canvas.SetTop(valueLabel, chartHeight - barHeight + 25);
                    elements.Add(valueLabel);
                }

                // Create x-axis label
                var axisLabel = new TextBlock
                {
                    Text = label,
                    FontSize = 9,
                    Width = barWidth + 20,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
                
                Canvas.SetLeft(axisLabel, 30 + i * (barWidth + 10));
                Canvas.SetTop(axisLabel, chartHeight + 25);
                elements.Add(axisLabel);
            }

            // Add all elements at once for better performance
            foreach (var element in elements)
            {
                canvas.Children.Add(element);
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

        protected override void OnClosed(EventArgs e)
        {
            // Clean up resources
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
            }
            
            // Clear canvas children to prevent memory leaks
            if (IntervalChart != null)
                IntervalChart.Children.Clear();
            if (EaseFactorChart != null)
                EaseFactorChart.Children.Clear();
            if (ProgressChart != null)
                ProgressChart.Children.Clear();
            
            base.OnClosed(e);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Redraw charts when window is resized, but with throttling
            if (!_isLoading && _currentStats != null)
            {
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    DrawCharts();
                };
                timer.Start();
            }
        }
    }
}