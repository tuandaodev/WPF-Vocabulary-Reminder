using System;
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
            Reload();
        }

        private void Reload()
        {
            bool? isRead = null;
            if (!string.IsNullOrEmpty(Filter.Text))
                isRead = Filter.Text.Equals("Read");
            var searchContent = FilterContent.Text?.Trim();

            var VocabularyList = DataAccess.GetListLearnded(isRead, searchContent);
            View_ListLearnedWords.Items.Clear();

            foreach (var _item in VocabularyList)
                View_ListLearnedWords.Items.Add(_item);
        }

        private void Frm_LearnedWords_Activated(object sender, EventArgs e)
        {
            Reload();
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Reload();
        }

        private void Btn_OnFilter_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void FilterContent_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                Reload();
        }
    }
}
