using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using VR.Domain;
using VR.Domain.Models;
using VR.Services;
using VR.Utils;

namespace VR
{
    public partial class VocaPopup : Window
    {
        private static IPAService _ipaService;
        private Vocabulary _vocabulary { get; set; }
        private System.Windows.Forms.Timer autoCloseTimer;
        private int _currentDefinitionIndex = 0;

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
                case System.Windows.Input.Key.Escape:
                    this.Close();
                    break;
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
                try
                {
                    var vocab = db.Vocabularies.Find(_vocabulary.Id);
                    if (vocab != null)
                    {
                        // Update SRS fields
                        vocab.NextReviewDate = _vocabulary.NextReviewDate;
                        vocab.EaseFactor = _vocabulary.EaseFactor;
                        vocab.Interval = _vocabulary.Interval;
                        vocab.ReviewCount = _vocabulary.ReviewCount;
                        vocab.LapseCount = _vocabulary.LapseCount;

                        // Ensure required fields are preserved
                        if (string.IsNullOrEmpty(vocab.Word))
                            vocab.Word = _vocabulary.Word;
                        if (string.IsNullOrEmpty(vocab.WordId))
                            vocab.WordId = _vocabulary.WordId;
                        
                        try
                        {
                            await db.SaveChangesAsync();
                        }
                        catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                        {
                            string errorMessages = string.Join("\n",
                                ex.EntityValidationErrors
                                .SelectMany(x => x.ValidationErrors)
                                .Select(x => $"Property: {x.PropertyName}, Error: {x.ErrorMessage}"));
                            
                            MessageBox.Show($"Validation errors occurred:\n{errorMessages}",
                                "Validation Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving changes: {ex.Message}",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    throw;
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
                    var translation = await TranslateService.GetGoogleTranslate(Label_Example.Text);
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
                        FileName = Helper.GetCambridgeWordUrl(_vocabulary.Word),
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
                        FileName = Helper.GetOxfordWordUrl(_vocabulary.Word),
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
                        _ipaService = new IPAService();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to initialize IPA service: {ex.Message}");
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
                        Debug.WriteLine($"IPA lookup failed for word '{Label_Example.Text}': {ex.Message}");
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

        public void SetVocabulary(Vocabulary item)
        {
            _vocabulary = item ?? throw new ArgumentNullException(nameof(item));
            MappingDisplay();
        }

        private void Btn_PrevDefinition_Click(object sender, RoutedEventArgs e)
        {
            if (_vocabulary?.JsonData?.Definitions == null || _vocabulary.JsonData.Definitions.Count == 0) return;

            _currentDefinitionIndex--;
            if (_currentDefinitionIndex < 0)
                _currentDefinitionIndex = _vocabulary.JsonData.Definitions.Count - 1;

            UpdateDefinitionDisplay();
        }

        private void Btn_NextDefinition_Click(object sender, RoutedEventArgs e)
        {
            if (_vocabulary?.JsonData?.Definitions == null || _vocabulary.JsonData.Definitions.Count == 0) return;

            _currentDefinitionIndex++;
            if (_currentDefinitionIndex >= _vocabulary.JsonData.Definitions.Count)
                _currentDefinitionIndex = 0;

            UpdateDefinitionDisplay();
        }

        private void UpdateDefinitionDisplay()
        {
            if (_vocabulary?.JsonData?.Definitions == null || _vocabulary.JsonData.Definitions.Count == 0) return;

            var currentDef = _vocabulary.JsonData.Definitions[_currentDefinitionIndex];
            
            // Update definition and example
            Label_Translate2.Text = currentDef.Definition;
            Label_Example.Text = currentDef.Examples?.FirstOrDefault()?.Example ?? "";
            
            // Update metadata
            if (!string.IsNullOrEmpty(currentDef.PartOfSpeech))
            {
                Label_DefPartOfSpeech.Text = currentDef.PartOfSpeech;
                Label_DefPartOfSpeech.Visibility = Visibility.Visible;
            }
            else
            {
                Label_DefPartOfSpeech.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(currentDef.Topic))
            {
                Label_DefTopic.Text = currentDef.Topic;
                Label_DefTopic.Visibility = Visibility.Visible;
            }
            else
            {
                Label_DefTopic.Visibility = Visibility.Collapsed;
            }

            if (!string.IsNullOrEmpty(currentDef.Level))
            {
                Label_DefLevel.Text = currentDef.Level;
                Label_DefLevel.Visibility = Visibility.Visible;
            }
            else
            {
                Label_DefLevel.Visibility = Visibility.Collapsed;
            }
            
            // Update the index display
            Label_DefinitionIndex.Text = $"{_currentDefinitionIndex + 1}/{_vocabulary.JsonData.Definitions.Count}";
            
            // Reset translation and phonetics when definition changes
            Label_ExampleTranslation.Text = string.Empty;
            Label_ExampleTranslation.Visibility = Visibility.Collapsed;
            Label_ExamplePhonetic.Text = string.Empty;
            Label_ExamplePhonetic.Visibility = Visibility.Collapsed;
        }

        private void MappingDisplay()
        {
            this.Label_Word.Content = this._vocabulary.Word?.ToUpper();

            this.Label_IPA.Content = $"{this._vocabulary.Ipa}";
            this.Label_IPA2.Content = string.IsNullOrEmpty(this._vocabulary.Ipa2) || this._vocabulary.Ipa2 == this._vocabulary.Ipa
                ? "-"
                : $"{this._vocabulary.Ipa2}";
            
            this.Label_Type.Content = this._vocabulary.Type;
            
            // Only show level if it exists
            if (!string.IsNullOrEmpty(this._vocabulary.JsonData?.Level))
            {
                this.Label_Level.Content = this._vocabulary.JsonData.Level;
                this.Label_Level.Visibility = Visibility.Visible;
            }
            else
            {
                this.Label_Level.Visibility = Visibility.Collapsed;
            }
            this.Label_Translate1.Text = this._vocabulary.Translate;
            
            // Reset definition index
            _currentDefinitionIndex = 0;
            UpdateDefinitionDisplay();
            
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
