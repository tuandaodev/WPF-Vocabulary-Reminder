using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VR.Domain;
using VR.Domain.Models;
using VR.Dto;
using VR.Infrastructure;
using VR.Services;

namespace VR
{
    public partial class LearnedWordsWindow : Window
    {
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public LearnedWordsWindow()
        {
            InitializeComponent();
            MyMapper.Initialize();
            LoadDictionariesAsync().ConfigureAwait(false);
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null || headerClicked.Role == GridViewColumnHeaderRole.Padding) return;

            ListSortDirection direction;

            if (headerClicked != _lastHeaderClicked)
            {
                direction = ListSortDirection.Ascending;
            }
            else
            {
                direction = _lastDirection == ListSortDirection.Ascending ?
                    ListSortDirection.Descending : ListSortDirection.Ascending;
            }

            var sortBy = "";
            var header = headerClicked.Column.Header as string ?? string.Empty;

            // Map display columns to their sortable properties
            switch (header)
            {
                case "Next Review":
                    sortBy = "NextReviewDate";
                    break;
                default:
                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    sortBy = columnBinding?.Path.Path ?? header;
                    break;
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                Sort(sortBy, direction);
            }

            if (direction == ListSortDirection.Ascending)
            {
                headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
            }
            else
            {
                headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
            }

            // Remove arrow from previously sorted header
            if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
            {
                _lastHeaderClicked.Column.HeaderTemplate = null;
            }

            _lastHeaderClicked = headerClicked;
            _lastDirection = direction;
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            var dataView = CollectionViewSource.GetDefaultView(View_ListLearnedWords.Items);
            dataView.SortDescriptions.Clear();
            
            var sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private async Task LoadDictionariesAsync()
        {
            var dictionaries = await DataService.GetDictionariesAsync();
            DictionaryFilter.Items.Clear();
            DictionaryFilter.Items.Add(new ComboBoxItem { Content = "All", Tag = 0 });
            foreach (var dictionary in dictionaries)
            {
                DictionaryFilter.Items.Add(new ComboBoxItem { Content = dictionary.Name, Tag = dictionary.Id });
            }
            DictionaryFilter.SelectedIndex = 0;
        }

        private async Task ReloadAsync()
        {
            if (Filter == null || FilterContent == null) return;

            bool? isRead = null;
            if (!string.IsNullOrEmpty(Filter.Text))
                isRead = Filter.Text.Equals("Read");
            var searchContent = FilterContent.Text?.Trim();
            
            var selectedDictionary = DictionaryFilter.SelectedItem as ComboBoxItem;
            int dictionaryId = selectedDictionary != null ? (int)selectedDictionary.Tag : 0;

            var vocabularyList = await DataService.GetListLearndedAsync(isRead, searchContent, dictionaryId);
            var mapVocabularyList = vocabularyList.Select(x => MyMapper.Mapper.Map<VocabularyDisplayDto>(x)).ToList();
            View_ListLearnedWords.Items.Clear();

            foreach (var item in mapVocabularyList)
            {
                // Add IsDueForReview property
                var dueForReview = SpacedRepetitionService.IsDueForReview(item);
                item.IsDueForReview = dueForReview;
                View_ListLearnedWords.Items.Add(item);
            }

            // Restore sorting if there was a previous sort
            if (_lastHeaderClicked != null)
            {
                var columnBinding = _lastHeaderClicked.Column.DisplayMemberBinding as Binding;
                var sortBy = columnBinding?.Path.Path ?? 
                            (_lastHeaderClicked.Column.Header as string ?? string.Empty);
                Sort(sortBy, _lastDirection);
            }
        }

        private async void Frm_LearnedWords_Activated(object sender, EventArgs e)
        {
            await ReloadAsync();
        }

        private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ReloadAsync();
        }

        private async void Btn_OnFilter_Click(object sender, RoutedEventArgs e)
        {
            await ReloadAsync();
        }

        private async void FilterContent_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                await ReloadAsync();
        }

        private async void DictionaryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ReloadAsync();
        }

        private async void BtnReviewAgain_Click(object sender, RoutedEventArgs e)
        {
            await ProcessReview(sender, 1);
        }

        private async void BtnReviewHard_Click(object sender, RoutedEventArgs e)
        {
            await ProcessReview(sender, 2);
        }

        private async void BtnReviewGood_Click(object sender, RoutedEventArgs e)
        {
            await ProcessReview(sender, 3);
        }

        private async void BtnReviewEasy_Click(object sender, RoutedEventArgs e)
        {
            await ProcessReview(sender, 4);
        }

        private async Task ProcessReview(object sender, int quality)
        {
            var button = sender as Button;
            if (button?.DataContext is VocabularyDisplayDto vocaInfo)
            {
                var vocabulary = await DataService.GetVocabularyByIdAsync(vocaInfo.Id);
                if (vocabulary != null)
                {
                    SpacedRepetitionService.ProcessReview(vocabulary, quality);
                    await DataService.UpdateVocabularyAsync(vocabulary);
                }
                
                await ReloadAsync();
            }
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            bool requiresQuoting = field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n");
            if (!requiresQuoting)
                return field;

            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        private async void Btn_BackupLearned_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backupList = await DataService.GetBackupAsync();
                if (backupList == null || !backupList.Any())
                {
                    MessageBox.Show("No learned words found to backup.", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string backupPath = Path.Combine(ApplicationIO.GetApplicationFolderPath(), $"learned_words_backup_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                using (var writer = new StreamWriter(backupPath, false, System.Text.Encoding.UTF8))
                {
                    // Write header
                    await writer.WriteLineAsync("Word,WordId,Type,IPA (UK),IPA (US),Translation,Definition,Example,Example2," +
                                              "Status,ViewedDate,LearnedDate,CreatedDate," +
                                              "NextReviewDate,EaseFactor,Interval,ReviewCount,LapseCount");

                    // Write data
                    foreach (var word in backupList)
                    {
                        var fields = new[]
                        {
                            EscapeCsvField(word.Word),
                            EscapeCsvField(word.WordId),
                            EscapeCsvField(word.Type),
                            EscapeCsvField(word.Ipa),
                            EscapeCsvField(word.Ipa2),
                            EscapeCsvField(word.Translate),
                            EscapeCsvField(word.Define),
                            EscapeCsvField(word.Example),
                            EscapeCsvField(word.Example2),
                            //EscapeCsvField(word.PlayURL),
                            //EscapeCsvField(word.PlayURL2),
                            //EscapeCsvField(word.Related),
                            EscapeCsvField(word.Status?.ToString()),
                            EscapeCsvField(word.ViewedDate?.ToString()),
                            EscapeCsvField(word.LearnedDate?.ToString()),
                            EscapeCsvField(word.CreatedDate?.ToString()),
                            EscapeCsvField(word.NextReviewDate?.ToString()),
                            EscapeCsvField(word.EaseFactor?.ToString()),
                            EscapeCsvField(word.Interval?.ToString()),
                            EscapeCsvField(word.ReviewCount?.ToString()),
                            EscapeCsvField(word.LapseCount?.ToString())
                        };
                        await writer.WriteLineAsync(string.Join(",", fields));
                    }
                }

                MessageBox.Show($"Successfully backed up {backupList.Count} learned words to:\n{backupPath}", "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error backing up learned words: {ex.Message}", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Btn_OpenBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string folderPath = ApplicationIO.GetApplicationFolderPath();
                Process.Start("explorer.exe", folderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening backup folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
