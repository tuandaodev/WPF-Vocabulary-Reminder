using System;
using System.Windows;
using System.Windows.Controls;
using VocabularyReminder.DataAccessLibrary;
using System.Threading.Tasks;

namespace VocabularyReminder
{
    /// <summary>
    /// Interaction logic for DeletedWords.xaml
    /// </summary>
    public partial class LearnedWordsWindow : Window
    {
        public LearnedWordsWindow()
        {
            InitializeComponent();
        }

        private async Task ReloadAsync()
        {
            int selected = comboTypeList.SelectedIndex;
            var vocabularies = await DataAccess.GetListLearndedAsync((ShowListType)selected);
            View_ListLearnedWords.Items.Clear();
            foreach (var _item in vocabularies)
            {
                View_ListLearnedWords.Items.Add(_item);
            }
        }

        private async void Frm_LearnedWords_Activated(object sender, EventArgs e)
        {
            await ReloadAsync();
        }

        private async void comboTypeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ReloadAsync();
        }
    }
}
