using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using VocabularyReminder.VR.Common;
using VR.Domain;
using VR.Domain.Models;
using VR.Services;
using VR.Utils;

namespace VR
{
    /// <summary>
    /// Interaction logic for VocaPopup.xaml
    /// </summary>
    public partial class VocaPopup : Window
    {
        private static IPAService _ipaService;
        private static int _easyClickCount = 0;
        private Vocabulary _vocabulary { get; set; }
        private System.Windows.Forms.Timer autoCloseTimer;
        private int _currentDefinitionIndex = 0;

        public VocaPopup()
        {
            _easyClickCount = 0;

            InitializeComponent();

            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Topmost = true;
            this.Opacity = 0;
            this.KeyDown += VocaPopup_KeyDown;
            this.PreviewKeyDown += VocaPopup_KeyPreviewDown;

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

                // Add subtle fade-in animation after positioning
                var fadeIn = new DoubleAnimation
                {
                    From = 0.95,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(500)
                };
                this.BeginAnimation(Window.OpacityProperty, fadeIn);

                this.Close();
            };
            autoCloseTimer.Interval = (int)TimeSpan.FromSeconds(20).TotalMilliseconds;
            autoCloseTimer.Start();
        }

        private void ClosePopup()
        {
            // Add subtle fade-in animation after positioning
            var fadeIn = new DoubleAnimation
            {
                From = 0.95,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500)
            };
            this.BeginAnimation(Window.OpacityProperty, fadeIn);
            this.Close();
        }

        private void VocaPopup_KeyPreviewDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    Btn_PrevDefinition_Click(null, null);
                    break;
                case Key.Right:
                    Btn_NextDefinition_Click(null, null);
                    break;
            }
        }

        private void VocaPopup_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    this.ClosePopup();
                    break;
                case Key.D1:
                case Key.NumPad1:
                    Btn_Again_Click(null, null);
                    break;
                case Key.D2:
                case Key.NumPad2:
                    Btn_Hard_Click(null, null);
                    break;
                case Key.D3:
                case Key.NumPad3:
                    Btn_Good_Click(null, null);
                    break;
                case Key.D4:
                case Key.NumPad4:
                    Btn_Easy_Click(null, null);
                    break;
                case Key.Oem3:
                    Btn_ReadExample_Click(null, null);
                    break;
                case Key.Delete:
                    Btn_Delete_Click(null, null);
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

            App.LastReaction = DateTime.Now;

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
            this.ClosePopup();
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
            this.ClosePopup();
        }

        private async void Btn_Next_Click(object sender, RoutedEventArgs e)
        {
            await NextVocabularyAsync();
        }

        private async Task NextVocabularyAsync()
        {
            _easyClickCount = 0;
            await BackgroundService.NextVocabularyAsync();
            App.LastReaction = DateTime.Now;
            this.ClosePopup();
        }

        //private async void Btn_NextAndDelete_Click(object sender, RoutedEventArgs e)
        //{
        //    await BackgroundService.NextAndDeleteVocabulary();
        //    this.Close();
        //}

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

        private async void Btn_Easy_Click(object sender, RoutedEventArgs e)
        {
            await ProcessEasyButtonAsync();
        }

        private async Task ProcessEasyButtonAsync()
        {
            _easyClickCount++;
            ProcessReview(4);

            // Check if next review is > 20 days
            if (_vocabulary.NextReviewDate.HasValue)
            {
                var nextReview = DateTimeOffset.FromUnixTimeSeconds(_vocabulary.NextReviewDate.Value);
                var daysUntilReview = (nextReview - DateTimeOffset.Now).TotalDays;
                if (daysUntilReview > 20)
                {
                    // Check conditions for auto-close
                    if (App.showNextOnEasy)
                    {
                        await NextVocabularyAsync();
                    }
                    else
                    {
                        ClosePopup();
                    }
                    return;
                }
            }
        }

        private async void Btn_TranslateExample_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await GetTranslateAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error translating text: " + ex.Message, "Translation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Btn_TranslateExample.IsEnabled = true;
            }
        }

        private async Task GetTranslateAsync()
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

        private async void Btn_ReadExample_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = TextToSpeechService.SpeakTextAsync(Label_Example.Text);
                _ = GetTranslateAsync();
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
                        FileName = Helper.GetOxfordWordUrl(!string.IsNullOrEmpty(_vocabulary.WordId) ? _vocabulary.WordId : _vocabulary.Word),
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

        private void MappingDisplayForWord()
        {
            Label_Word.Content = _vocabulary.Word?.ToUpper();

            Label_IPA.Content = $"/{_vocabulary.Ipa2}/";
            Label_IPA2.Content = string.IsNullOrEmpty(_vocabulary.Ipa) || _vocabulary.Ipa == _vocabulary.Ipa2
                ? "-" : $"/{_vocabulary.Ipa}/";

            Label_Type.Content = _vocabulary.Type;

            // Only show level if it exists
            if (!string.IsNullOrEmpty(_vocabulary.JsonData?.Level))
            {
                Label_Level.Content = _vocabulary.JsonData.Level;
                Label_Level.Visibility = Visibility.Visible;
            }
            else
            {
                Label_Level.Visibility = Visibility.Collapsed;
            }
            Label_Translate1.Text = _vocabulary.Translate;
        }

        private void MappingDisplayForSentence()
        {
            //Label_Word.Content = _vocabulary.Word?.ToUpper();

            //Label_IPA.Content = $"/{_vocabulary.Ipa2}/";
            //Label_IPA2.Content = string.IsNullOrEmpty(_vocabulary.Ipa) || _vocabulary.Ipa == _vocabulary.Ipa2
            //    ? "-" : $"/{_vocabulary.Ipa}/";

            //Label_Type.Content = _vocabulary.Type;

            //// Only show level if it exists
            //if (!string.IsNullOrEmpty(_vocabulary.JsonData?.Level))
            //{
            //    Label_Level.Content = _vocabulary.JsonData.Level;
            //    Label_Level.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    Label_Level.Visibility = Visibility.Collapsed;
            //}
            //Label_Translate1.Text = _vocabulary.Translate;

            SectionWord.Visibility = Visibility.Collapsed;
            //SectionDef.Visibility = Visibility.Collapsed;

            Label_Translate1.Text = _vocabulary.Translate;
            Label_Example.Text = _vocabulary.Word;
        }

        private void MappingDisplay()
        {
            if (_vocabulary.Type == VocaType.Sentence)
            {
                MappingDisplayForSentence();
            } else
            {
                MappingDisplayForWord();
            }
            
            // Reset definition index
            _currentDefinitionIndex = 0;
            UpdateDefinitionDisplay();
            
            var relatedWords = string.IsNullOrEmpty(this._vocabulary.Related)
                ? "None"
                : _vocabulary.Related;
            this.Label_Same.Text = relatedWords;

            // Disable play buttons if their corresponding URLs are empty/null
            //this.Btn_PlaySound1.IsEnabled = !string.IsNullOrEmpty(this._vocabulary.PlayURL2);
            //this.Btn_PlaySound2.IsEnabled = !string.IsNullOrEmpty(this._vocabulary.PlayURL);

            // Update SRS information
            UpdateSrsInfo();
        }

        private async void Border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.XButton1://Back button
                    await BackgroundService.ActionPlay(2);
                    break;
                case MouseButton.XButton2://forward button
                    await NextVocabularyAsync();
                    break;
                default:
                    break;
            }
        }
    }
}
