using System;
using System.Windows;
using VocabularyReminder.DataAccessLibrary;

namespace VocabularyReminder
{
    /// <summary>
    /// Interaction logic for VocaPopup.xaml
    /// </summary>
    public partial class VocaPopup : Window
    {
        private Vocabulary _vocabulary { get; set; }
        private System.Windows.Forms.Timer tmr;

        public VocaPopup()
        {
            InitializeComponent();

            tmr = new System.Windows.Forms.Timer();
            tmr.Tick += delegate {
                this.Close();
            };
            tmr.Interval = (int)TimeSpan.FromSeconds(3).TotalMilliseconds;
            tmr.Start();
        }

        public void SetVocabulary(Vocabulary _item)
        {
            _vocabulary = _item;
            MappingDisplay();
        }

        private void MappingDisplay()
        {
            this.Label_Translate.Content = this._vocabulary.Word;
        }
    }
}
