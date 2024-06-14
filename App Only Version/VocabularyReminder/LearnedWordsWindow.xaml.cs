using System;
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
            //Reload();
        }

        private async Task ReloadAsync()
        {
            if (Filter == null || FilterContent == null) return;

            bool? isRead = null;
            if (!string.IsNullOrEmpty(Filter.Text))
                isRead = Filter.Text.Equals("Read");
            var searchContent = FilterContent.Text?.Trim();

            var VocabularyList = await DataAccess.GetListLearndedAsync(isRead, searchContent);
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
    }
}
