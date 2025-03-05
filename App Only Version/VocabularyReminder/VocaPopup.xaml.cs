using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using VocabularyReminder.DataAccessLibrary;
using VocabularyReminder.Services;

namespace VocabularyReminder
{
    public partial class VocaPopup : Window
    {
        private static IPA _ipaService;
        private Vocabulary _vocabulary { get; set; }
        private System.Windows.Forms.Timer autoCloseTimer;

        public VocaPopup()
        {
            InitializeComponent();

            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Topmost = true;
            this.Opacity = 0;
            this.KeyDown += VocaPopup_KeyDown;
            
            this.Loaded += (s, e) => {
                var workArea = System.Windows.SystemParameters.WorkArea;
                this.Left = workArea.Right - this.ActualWidth - 20;  // 20px margin from right
                this.Top = workArea.Bottom - this.ActualHeight - 20;  // 40px margin from bottom
                
                // Add subtle fade-in animation after positioning
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 0.95,
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

        private void VocaPopup_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.D1:
                case System.Windows.Input.Key.NumPad1:
                    Btn_Again_Click(null, null);
                    break;
                case System.Windows.Input.Key.D2:
                case System.Windows.Input.Key.NumPad2:
                    Btn_Hard_Click(null, null);
                    break;
                case System.Windows.Input.Key.D3:
                case System.Windows.Input.Key.NumPad3:
                    Btn_Good_Click(null, null);
                    break;
                case System.Windows.Input.Key.D4:
                case System.Windows.Input.Key.NumPad4:
                    Btn_Easy_Click(null, null);
                    break;
            }
        }

        private void UpdateSrsInfo()
        {
            if (_vocabulary == null) return;

            if (_vocabulary.NextReviewDate.HasValue)
            {
                var nextReview = DateTimeOffset.FromUnixTimeSeconds(_vocabulary.NextReviewDate.Value);
                Label_NextReview.Text = nextReview.LocalDateTime.ToString("g");
            }
            else
            {
                Label_NextReview.Text = "Not scheduled";
            }

            Label_Interval.Text = _vocabulary.Interval.HasValue && _vocabulary.Interval.Value > 0
                ? $"{_vocabulary.Interval.Value} days"
                : "New";
        }

        private async void ProcessReview(int quality)
        {
            if (_vocabulary == null) return;

            SpacedRepetitionService.ProcessReview(_vocabulary, quality);

            // Update the database
            using (var db = new VocaDbContext())
            {
                var vocab = db.Vocabularies.Find(_vocabulary.Id);
                if (vocab != null)
                {
                    vocab.NextReviewDate = _vocabulary.NextReviewDate;
                    vocab.EaseFactor = _vocabulary.EaseFactor;
                    vocab.Interval = _vocabulary.Interval;
                    vocab.ReviewCount = _vocabulary.ReviewCount;
                    vocab.LapseCount = _vocabulary.LapseCount;
                    await db.SaveChangesAsync();
                }
            }

            UpdateSrsInfo();
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
            await BackgroundService.NextVocabularyAsync();
            this.Close();
        }

        private async void Btn_NextAndDelete_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundService.NextAndDeleteVocabulary();
            this.Close();
        }

        private void Btn_Again_Click(object sender, RoutedEventArgs e)
        {
            ProcessReview(1);
        }

        private void Btn_Hard_Click(object sender, RoutedEventArgs e)
        {
            ProcessReview(2);
        }

        private void Btn_Good_Click(object sender, RoutedEventArgs e)
        {
            ProcessReview(3);
        }

        private void Btn_Easy_Click(object sender, RoutedEventArgs e)
        {
            ProcessReview(4);
        }

        private async void Btn_TranslateExample_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Label_Example.Text))
                {
                    Btn_TranslateExample.IsEnabled = false;
                    var translation = await DesktopNotifications.Services.TranslateService.GetGoogleTranslate(Label_Example.Text);
                    if (!string.IsNullOrEmpty(translation) && translation != Label_Example.Text)
                    {
                        Label_ExampleTranslation.Text = translation;
                        Label_ExampleTranslation.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Label_ExampleTranslation.Visibility = Visibility.Collapsed;
                    }
                    Btn_TranslateExample.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error translating text: " + ex.Message, "Translation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Btn_TranslateExample.IsEnabled = true;
            }
        }

        private async void Btn_ReadExample_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Label_Example.Text))
                {
                    await TextToSpeechService.SpeakTextAsync(Label_Example.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading text: " + ex.Message, "Text-to-Speech Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Btn_OpenCambridge_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_vocabulary?.Word))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = DesktopNotifications.Services.Helper.GetCambridgeWordUrl(_vocabulary.Word),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening Cambridge Dictionary: {ex.Message}", "Browser Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void Btn_OpenOxford_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_vocabulary?.Word))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = DesktopNotifications.Services.Helper.GetOxfordWordUrl(_vocabulary.Word),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening Oxford Dictionary: {ex.Message}", "Browser Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void Btn_GetExamplePhonetics_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Label_Example.Text))
                return;

            try
            {
                // Disable button while processing
                Btn_GetExamplePhonetics.IsEnabled = false;

                // Initialize IPA service if needed
                if (_ipaService == null)
                {
                    try
                    {
                        _ipaService = new IPA();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to initialize IPA service: {ex.Message}");
                        MessageBox.Show("Failed to load IPA dictionary.", "IPA Service Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                // Try IPA service
                if (_ipaService != null)
                {
                    string ipa = null;

                    try
                    {
                        ipa = _ipaService.EnglishToIPA(Label_Example.Text);
                        // If IPA service returns the same word, it means no phonetic found
                        if (ipa == Label_Example.Text.ToLower())
                        {
                            ipa = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"IPA lookup failed for word '{Label_Example.Text}': {ex.Message}");
                        ipa = null;
                    }

                    // Update UI with final result
                    if (!string.IsNullOrEmpty(ipa))
                    {
                        Label_ExamplePhonetic.Text = ipa;
                        Label_ExamplePhonetic.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Label_ExamplePhonetic.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting phonetics: {ex.Message}", "Phonetics Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                // Re-enable button
                Btn_GetExamplePhonetics.IsEnabled = true;
            }
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
            this.Label_IPA2.Content = string.IsNullOrEmpty(this._vocabulary.Ipa2) || this._vocabulary.Ipa2 == this._vocabulary.Ipa
                ? "-"
                : $"{this._vocabulary.Ipa2}";
            
            this.Label_Type.Content = this._vocabulary.Type;
            
            this.Label_Translate1.Text = this._vocabulary.Translate;
            this.Label_Translate2.Text = this._vocabulary.Define;
            this.Label_Example.Text = $"{this._vocabulary.Example}";
            this.Label_ExampleTranslation.Text = string.Empty;
            this.Label_ExampleTranslation.Visibility = Visibility.Collapsed;
            this.Label_ExamplePhonetic.Text = string.Empty;
            this.Label_ExamplePhonetic.Visibility = Visibility.Collapsed;
            
            var relatedWords = string.IsNullOrEmpty(this._vocabulary.Related)
                ? "None"
                : this._vocabulary.Related;
            this.Label_Same.Text = relatedWords;

            // Disable play buttons if their corresponding URLs are empty/null
            this.Btn_PlaySound1.IsEnabled = !string.IsNullOrEmpty(this._vocabulary.PlayURL2);
            this.Btn_PlaySound2.IsEnabled = !string.IsNullOrEmpty(this._vocabulary.PlayURL);

            // Update SRS information
            UpdateSrsInfo();
        }
    }
}
