﻿﻿﻿﻿﻿﻿﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VocabularyReminder.DataAccessLibrary;

namespace VocabularyReminder
{
    /// <summary>
    /// Interaction logic for LearnedWordsWindow.xaml
    /// </summary>
    public partial class LearnedWordsWindow : Window
    {
        public LearnedWordsWindow()
        {
            InitializeComponent();
            LoadDictionariesAsync().ConfigureAwait(false);
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

            var VocabularyList = await DataAccess.GetListLearndedAsync(isRead, searchContent, dictionaryId);
            View_ListLearnedWords.Items.Clear();

            foreach (var _item in VocabularyList)
                View_ListLearnedWords.Items.Add(_item);
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
