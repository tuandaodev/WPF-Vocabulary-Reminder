﻿﻿﻿﻿﻿using System;
using System.Windows;
using VocabularyReminder.DataAccessLibrary;
using VocabularyReminder.Services;

namespace VocabularyReminder
{
    /// <summary>
    /// Interaction logic for VocaPopup.xaml
    /// </summary>
    public partial class VocaPopup : Window
    {
        private Vocabulary _vocabulary { get; set; }
        private System.Windows.Forms.Timer autoCloseTimer;

        public VocaPopup()
        {
            InitializeComponent();

            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Topmost = true;
            this.Opacity = 0;
            
            this.Loaded += (s, e) => {
                var workArea = System.Windows.SystemParameters.WorkArea;
                this.Left = workArea.Right - this.ActualWidth - 20;  // 20px margin from right
                this.Top = workArea.Bottom - this.ActualHeight - 40;  // 40px margin from bottom
                
                // Add subtle fade-in animation after positioning
                    var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(200)
                    };
                    this.BeginAnimation(Window.OpacityProperty, fadeIn);
                };
    
                // Initialize auto-close timer
                autoCloseTimer = new System.Windows.Forms.Timer();
            autoCloseTimer.Tick += delegate {
                this.Close();
            };
            autoCloseTimer.Interval = (int)TimeSpan.FromSeconds(20).TotalMilliseconds;
            autoCloseTimer.Start();
        }

        private void Border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            autoCloseTimer.Stop(); // Pause auto-close when user is viewing
        }

        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            autoCloseTimer.Start(); // Resume auto-close when user moves away
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Btn_PlaySound1_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundService.ActionPlay(1);
        }

        private async void Btn_PlaySound2_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundService.ActionPlay(2);
        }

        private async void Btn_Delete_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundService.DeleteVocabularyAsync();
            this.Close();
        }

        private async void Btn_Next_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundService.NextVocabulary();
            this.Close();
        }

        private async void Btn_NextAndDelete_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundService.NextAndDeleteVocabulary();
            this.Close();
        }

        public void SetVocabulary(Vocabulary _item)
        {
            _vocabulary = _item;
            MappingDisplay();
        }

        private void MappingDisplay()
        {
            this.Label_Word.Content = this._vocabulary.Word?.ToUpper();
            
            this.Label_IPA.Content = $"{this._vocabulary.Ipa}";
            this.Label_IPA2.Content = string.IsNullOrEmpty(this._vocabulary.Ipa2)
                ? "-"
                : $"{this._vocabulary.Ipa2}";
            
            this.Label_Type.Content = this._vocabulary.Type;
            
            this.Label_Translate1.Text = this._vocabulary.Translate;
            this.Label_Translate2.Text = this._vocabulary.Define;
            this.Label_Example.Text = this._vocabulary.Example;
            
            var relatedWords = string.IsNullOrEmpty(this._vocabulary.Related)
                ? "None"
                : this._vocabulary.Related;
            this.Label_Same.Text = relatedWords;
        }
    }
}
