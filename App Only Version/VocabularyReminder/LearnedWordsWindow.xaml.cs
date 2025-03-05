﻿﻿﻿using System;
using System.IO;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VocabularyReminder.DataAccessLibrary;
using VocabularyReminder.Services;

namespace VocabularyReminder
{
    public partial class LearnedWordsWindow : Window
    {
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public LearnedWordsWindow()
        {
            InitializeComponent();
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
            var dictionaries = await DataAccess.GetDictionariesAsync();
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

            var vocabularyList = await DataAccess.GetListLearndedAsync(isRead, searchContent, dictionaryId);
            View_ListLearnedWords.Items.Clear();

            foreach (var item in vocabularyList)
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
            if (button?.DataContext is Vocabulary vocabulary)
            {
                SpacedRepetitionService.ProcessReview(vocabulary, quality);
                
                using (var context = new VocaDbContext())
                {
                    var dbVocab = await context.Vocabularies.FindAsync(vocabulary.Id);
                    if (dbVocab != null)
                    {
                        dbVocab.NextReviewDate = vocabulary.NextReviewDate;
                        dbVocab.Interval = vocabulary.Interval;
                        dbVocab.ReviewCount = vocabulary.ReviewCount;
                        dbVocab.EaseFactor = vocabulary.EaseFactor;
                        dbVocab.LapseCount = vocabulary.LapseCount;
                        await context.SaveChangesAsync();
                    }
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
                var learnedWords = await DataAccess.GetListLearndedAsync(true, null);
                if (learnedWords == null || !learnedWords.Any())
                {
                    MessageBox.Show("No learned words found to backup.", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string backupPath = Path.Combine(ApplicationIO.GetApplicationFolderPath(), $"learned_words_backup_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                using (var writer = new StreamWriter(backupPath, false, System.Text.Encoding.UTF8))
                {
                    // Write header
                    await writer.WriteLineAsync("Word,Type,IPA (US),IPA (UK),Translation");

                    // Write data
                    foreach (var word in learnedWords)
                    {
                        var fields = new[]
                        {
                            EscapeCsvField(word.Word),
                            EscapeCsvField(word.Type),
                            EscapeCsvField(word.Ipa2),
                            EscapeCsvField(word.Ipa),
                            EscapeCsvField(word.Translate)
                        };
                        await writer.WriteLineAsync(string.Join(",", fields));
                    }
                }

                MessageBox.Show($"Successfully backed up {learnedWords.Count} learned words to:\n{backupPath}", "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error backing up learned words: {ex.Message}", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
