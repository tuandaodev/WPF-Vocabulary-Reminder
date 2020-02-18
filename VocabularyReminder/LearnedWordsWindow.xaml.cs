using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VocabularyReminder.DataAccessLibrary;

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
            Reload();
        }

        private void Reload()
        {
            var VocabularyList = DataAccess.GetListLearnded();
            this.View_ListLearnedWords.Items.Clear();
            foreach (var _item in VocabularyList)
            {
                this.View_ListLearnedWords.Items.Add(_item);
            }
        }

        private void Frm_LearnedWords_Activated(object sender, EventArgs e)
        {
            Reload();
        }
    }
}
