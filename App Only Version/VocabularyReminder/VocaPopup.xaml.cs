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

            //this.Opacity = 0.5;
            //this.AllowsTransparency = true;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;

            tmr = new System.Windows.Forms.Timer();
            tmr.Tick += delegate {
                this.Close();
            };
            tmr.Interval = (int)TimeSpan.FromSeconds(20).TotalMilliseconds;
            tmr.Start();
        }

        public void SetVocabulary(Vocabulary _item)
        {
            _vocabulary = _item;
            MappingDisplay();
        }

        private void MappingDisplay()
        {
            this.Label_Word.Content = this._vocabulary.Word;
            this.Label_IPA.Content = this._vocabulary.Ipa;
            if (this._vocabulary.Ipa2 != this._vocabulary.Ipa)
                this.Label_IPA.Content = this._vocabulary.Ipa + " " + this._vocabulary.Ipa2;

            this.Label_Type.Content = this._vocabulary.Type;

            this.Label_Translate1.Content = "VI: " + this._vocabulary.Translate;
            this.Label_Translate2.Content = "EN: " + this._vocabulary.Define;
            this.Label_Example.Content = this._vocabulary.Example + this._vocabulary.Example;
            this.Label_Same.Content = this._vocabulary.Related;
        }
    }
}
