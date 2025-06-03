using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Documents;
using System.Windows.Media;
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
        private Vocabulary _vocabulary { get; set; }
        private System.Windows.Forms.Timer autoCloseTimer;
        private int _currentDefinitionIndex = 0;
        private int _currentJsonDataIndex = 0;
        private int _currentTypeIndex = 0;
        private int _currentExampleIndex = 0;
        private string[] _typeArray = null;

        public VocaPopup()
        {
            App.GlobalJsonDataId = null;

            InitializeComponent();

            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Topmost = true;
            this.ShowActivated = false; // Prevent stealing focus from current window
            this.Opacity = 0;
            this.KeyDown += VocaPopup_KeyDown;
            this.PreviewKeyDown += VocaPopup_KeyPreviewDown;
            this.MouseEnter += (s, e) => ResetAutoCloseTimer();
            this.MouseMove += (s, e) => ResetAutoCloseTimer();
            this.GotFocus += (s, e) => ResetAutoCloseTimer();

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

        private void ResetAutoCloseTimer()
        {
            if (autoCloseTimer != null)
            {
                autoCloseTimer.Stop();
                autoCloseTimer.Start();
            }
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
            ResetAutoCloseTimer();
            switch (e.Key)
            {
                case Key.Left:
                        Btn_PrevDefinition_Click(null, null);
                    break;
                case Key.Right:
                        Btn_NextDefinition_Click(null, null);
                    break;
                case Key.Up:
                    Btn_PrevExample_Click(null, null);
                    break;
                case Key.Down:
                    Btn_NextExample_Click(null, null);
                    break;
            }
        }

        private void VocaPopup_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ResetAutoCloseTimer();
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
            ResetAutoCloseTimer(); // Reset and resume auto-close when user moves away
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.ClosePopup();
        }

        private async void Btn_PlaySound1_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundService.ActionPlay(ActionPlayEnum.US);
        }

        private async void Btn_PlaySound2_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundService.ActionPlay(ActionPlayEnum.UK);
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
                await GetLLMTranslateAsync();
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

        private async Task GetLLMTranslateAsync()
        {
            if (string.IsNullOrEmpty(Label_Example.Text))
                return;

            try
            {
                // Disable button while processing
                Btn_TranslateExample.IsEnabled = false;

                // Check if LLM provider is configured
                if (!LLMProviderFactory.IsCurrentConfigurationValid())
                {
                    MessageBox.Show("LLM provider is not properly configured. Please check your settings.",
                        "LLM Configuration Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get LLM service and translate the text
                var llmService = LLMProviderFactory.GetLLMService();
                var translation = await llmService.TranslateAsync(Label_Example.Text, "Vietnamese");

                if (!string.IsNullOrEmpty(translation) && translation != Label_Example.Text)
                {
                    Label_ExampleTranslation.Text = translation;
                    Label_ExampleTranslation.Visibility = Visibility.Visible;
                }
                else
                {
                    Label_ExampleTranslation.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // Fallback to Google Translate if LLM fails
                MessageBox.Show($"LLM translation failed: {ex.Message}\nFalling back to Google Translate.", "LLM Translation Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // Use existing Google Translate method as fallback
                await GetTranslateAsync();
                return;
            }
            finally
            {
                Btn_TranslateExample.IsEnabled = true;
            }
        }

        private void Btn_ReadExample_Click(object sender, RoutedEventArgs e)
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

        private void Btn_OpenGTranslate_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_vocabulary?.Word))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Helper.GetGoogleTranslateUrl(_vocabulary.Word),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening Google Translate: {ex.Message}", "Browser Error",
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

        private async void Btn_GenerateExample_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_vocabulary?.Word))
                return;

            try
            {
                // Disable button while processing
                Btn_GenerateExample.IsEnabled = false;
                Btn_GenerateExample.Content = "Generating...";

                // Check if LLM provider is configured
                if (!LLMProviderFactory.IsCurrentConfigurationValid())
                {
                    MessageBox.Show("LLM provider is not properly configured. Please check your settings.",
                        "LLM Configuration Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get current meaning/definition from the displayed text
                string currentMeaning = Label_EngDefination.Text;

                // Generate new example using LLM with current meaning context
                var llmService = LLMProviderFactory.GetLLMService();
                var example = await llmService.GetExampleAsync(_vocabulary.Word, currentMeaning);

                if (!string.IsNullOrEmpty(example))
                {
                    // Update the example display directly (no parsing needed)
                    Label_Example.Text = example;
                    
                    // Hide previous translation and phonetic results
                    Label_ExampleTranslation.Visibility = Visibility.Collapsed;
                    Label_ExamplePhonetic.Visibility = Visibility.Collapsed;
                }
                else
                {
                    MessageBox.Show("Failed to generate example. Please try again.",
                        "Generation Error",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating example: {ex.Message}",
                    "Generation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                // Re-enable button
                Btn_GenerateExample.IsEnabled = true;
                Btn_GenerateExample.Content = "Generate Example";
            }
        }


        public void SetVocabulary(Vocabulary item)
        {
            _vocabulary = item ?? throw new ArgumentNullException(nameof(item));
            MappingDisplay();
        }

        private void ProcessAfterChangeDef()
        {
            App.GlobalJsonDataId = _vocabulary.JsonData[_currentJsonDataIndex]?.ID ?? null;
            UpdateDefinitionDisplay();
        }

        private void Btn_PrevDefinition_Click(object sender, RoutedEventArgs e)
        {
            if (_vocabulary?.JsonData == null || _vocabulary.JsonData.Count == 0) return;

            // First try to go to previous definition within current JsonData entry
            if (_currentJsonDataIndex < _vocabulary.JsonData.Count &&
                _vocabulary.JsonData[_currentJsonDataIndex]?.Definitions != null &&
                _vocabulary.JsonData[_currentJsonDataIndex].Definitions.Count > 0)
            {
                _currentDefinitionIndex--;
                if (_currentDefinitionIndex >= 0)
                {
                    UpdateDefinitionDisplay();
                    return;
                }
            }

            // If we're at the first definition or no definitions, go to previous JsonData entry
            _currentJsonDataIndex--;
            if (_currentJsonDataIndex < 0)
                _currentJsonDataIndex = _vocabulary.JsonData.Count - 1;

            // Set to last definition of the new JsonData entry
            if (_vocabulary.JsonData[_currentJsonDataIndex]?.Definitions != null &&
                _vocabulary.JsonData[_currentJsonDataIndex].Definitions.Count > 0)
            {
                _currentDefinitionIndex = _vocabulary.JsonData[_currentJsonDataIndex].Definitions.Count - 1;
            }
            else
            {
                _currentDefinitionIndex = 0;
            }

            ProcessAfterChangeDef();
            UpdateTypeHighlighting();
        }

        private void Btn_NextDefinition_Click(object sender, RoutedEventArgs e)
        {
            if (_vocabulary?.JsonData == null || _vocabulary.JsonData.Count == 0) return;

            // First try to go to next definition within current JsonData entry
            if (_currentJsonDataIndex < _vocabulary.JsonData.Count &&
                _vocabulary.JsonData[_currentJsonDataIndex]?.Definitions != null &&
                _vocabulary.JsonData[_currentJsonDataIndex].Definitions.Count > 0)
            {
                _currentDefinitionIndex++;
                if (_currentDefinitionIndex < _vocabulary.JsonData[_currentJsonDataIndex].Definitions.Count)
                {
                    UpdateDefinitionDisplay();
                    return;
                }
            }

            // If we're at the last definition or no definitions, go to next JsonData entry
            _currentJsonDataIndex++;
            if (_currentJsonDataIndex >= _vocabulary.JsonData.Count)
                _currentJsonDataIndex = 0;

            // Set to first definition of the new JsonData entry
            _currentDefinitionIndex = 0;

            ProcessAfterChangeDef();
            UpdateTypeHighlighting();
        }

        private void Btn_PrevExample_Click(object sender, RoutedEventArgs e)
        {
            if (_vocabulary?.JsonData == null || _vocabulary.JsonData.Count == 0 ||
                _currentJsonDataIndex >= _vocabulary.JsonData.Count ||
                _vocabulary.JsonData[_currentJsonDataIndex]?.Definitions == null ||
                _vocabulary.JsonData[_currentJsonDataIndex].Definitions.Count == 0) return;

            var currentDef = _vocabulary.JsonData[_currentJsonDataIndex].Definitions[_currentDefinitionIndex];
            if (currentDef?.Examples == null || currentDef.Examples.Count == 0) return;

            _currentExampleIndex--;
            if (_currentExampleIndex < 0)
                _currentExampleIndex = currentDef.Examples.Count - 1;

            UpdateExampleDisplay();
        }

        private void Btn_NextExample_Click(object sender, RoutedEventArgs e)
        {
            if (_vocabulary?.JsonData == null || _vocabulary.JsonData.Count == 0 ||
                _currentJsonDataIndex >= _vocabulary.JsonData.Count ||
                _vocabulary.JsonData[_currentJsonDataIndex]?.Definitions == null ||
                _vocabulary.JsonData[_currentJsonDataIndex].Definitions.Count == 0) return;

            var currentDef = _vocabulary.JsonData[_currentJsonDataIndex].Definitions[_currentDefinitionIndex];
            if (currentDef?.Examples == null || currentDef.Examples.Count == 0) return;

            _currentExampleIndex++;
            if (_currentExampleIndex >= currentDef.Examples.Count)
                _currentExampleIndex = 0;

            UpdateExampleDisplay();
        }

        private void UpdateExampleDisplay()
        {
            if (_vocabulary?.JsonData == null || _vocabulary.JsonData.Count == 0 ||
                _currentJsonDataIndex >= _vocabulary.JsonData.Count ||
                _vocabulary.JsonData[_currentJsonDataIndex]?.Definitions == null ||
                _vocabulary.JsonData[_currentJsonDataIndex].Definitions.Count == 0) return;

            var currentDef = _vocabulary.JsonData[_currentJsonDataIndex].Definitions[_currentDefinitionIndex];
            if (currentDef?.Examples == null || currentDef.Examples.Count == 0)
            {
                Label_Example.Text = "";
                Label_ExampleIndex.Text = "0/0";
                Label_ExampleStruct.Text = "";
                Label_ExampleStruct.Visibility = Visibility.Collapsed;
                return;
            }

            // Update example text and struct
            if (_currentExampleIndex < currentDef.Examples.Count)
            {
                var currentExample = currentDef.Examples[_currentExampleIndex];
                Label_Example.Text = currentExample?.Example ?? "";
                
                // Show struct if available
                if (!string.IsNullOrEmpty(currentExample?.Struct))
                {
                    Label_ExampleStruct.Text = currentExample.Struct;
                    Label_ExampleStruct.Visibility = Visibility.Visible;
                }
                else
                {
                    Label_ExampleStruct.Text = "";
                    Label_ExampleStruct.Visibility = Visibility.Collapsed;
                }
            }

            // Update example index display
            Label_ExampleIndex.Text = $"{_currentExampleIndex + 1}/{currentDef.Examples.Count}";

            // Reset translation and phonetics when example changes
            Label_ExampleTranslation.Text = string.Empty;
            Label_ExampleTranslation.Visibility = Visibility.Collapsed;
            Label_ExamplePhonetic.Text = string.Empty;
            Label_ExamplePhonetic.Visibility = Visibility.Collapsed;
        }

        private void UpdateDefinitionDisplay()
        {
            if (_vocabulary?.JsonData == null || _vocabulary.JsonData.Count == 0 ||
                _currentJsonDataIndex >= _vocabulary.JsonData.Count ||
                _vocabulary.JsonData[_currentJsonDataIndex]?.Definitions == null ||
                _vocabulary.JsonData[_currentJsonDataIndex].Definitions.Count == 0) return;

            var currentJsonData = _vocabulary.JsonData[_currentJsonDataIndex];
            var currentDef = currentJsonData.Definitions[_currentDefinitionIndex];
            
            // Update definition
            Label_EngDefination.Text = currentDef.Definition;
            Label_DefType.Text = Helper.GetShortFormType(currentJsonData?.Type);

            // Reset example index when definition changes and update example display
            _currentExampleIndex = 0;
            UpdateExampleDisplay();

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
            
            // Calculate total definitions across all JsonData entries
            int totalDefinitions = _vocabulary.JsonData.Sum(jsonData => jsonData?.Definitions?.Count ?? 0);
            
            // Calculate current definition position across all JsonData entries
            int currentDefinitionPosition = 0;
            for (int i = 0; i < _currentJsonDataIndex; i++)
                currentDefinitionPosition += _vocabulary.JsonData[i]?.Definitions?.Count ?? 0;

            currentDefinitionPosition += _currentDefinitionIndex + 1;
            
            // Update the index display to show current position out of total definitions
            Label_DefinitionIndex.Text = $"{currentDefinitionPosition}/{totalDefinitions}";
            
            // Reset translation and phonetics when definition changes
            Label_ExampleTranslation.Text = string.Empty;
            Label_ExampleTranslation.Visibility = Visibility.Collapsed;
            Label_ExamplePhonetic.Text = string.Empty;
            Label_ExamplePhonetic.Visibility = Visibility.Collapsed;
        }

        private void UpdateTypeHighlighting()
        {
            if (_vocabulary == null || string.IsNullOrEmpty(_vocabulary.Type))
                return;

            // Parse the types from the vocabulary type string (comma-separated)
            _typeArray = _vocabulary.Type.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            if (_typeArray.Length == 0)
                return;

            // Calculate current type index based on current definition
            _currentTypeIndex = _currentJsonDataIndex % _typeArray.Length;

            // Clear existing inlines
            Label_Type.Inlines.Clear();

            // Create runs for each type
            for (int i = 0; i < _typeArray.Length; i++)
            {
                var run = new Run(_typeArray[i]);
                
                // Highlight the current type
                if (i == _currentTypeIndex)
                {
                    run.Foreground = new SolidColorBrush(Colors.Yellow);
                    run.FontWeight = FontWeights.Bold;
                }
                else
                {
                    run.Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)); // #999999
                }

                Label_Type.Inlines.Add(run);

                // Add comma separator if not the last item
                if (i < _typeArray.Length - 1)
                {
                    var comma = new Run(", ");
                    comma.Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)); // #999999
                    Label_Type.Inlines.Add(comma);
                }
            }
        }

        private void MappingDisplayForWord()
        {
            Label_Word.Content = _vocabulary.Word?.ToUpper();

            // Only show level if it exists
            var currentDef = _vocabulary.JsonData?.ElementAtOrDefault(_currentJsonDataIndex);
            if (!string.IsNullOrEmpty(currentDef?.Level))
            {
                Label_Level.Content = currentDef.Level;
                Label_Level.Visibility = Visibility.Visible;
            }
            else
            {
                Label_Level.Visibility = Visibility.Collapsed;
            }

            // Load info from main vocabulary
            Label_IPA.Content = $"/{_vocabulary.Ipa2}/";
            Label_IPA2.Content = string.IsNullOrEmpty(_vocabulary.Ipa) || _vocabulary.Ipa == _vocabulary.Ipa2
                ? "-" : $"/{_vocabulary.Ipa}/";

            // Initialize type highlighting instead of setting simple content
            UpdateTypeHighlighting();

            // Overwrite data from DEF
            if (!string.IsNullOrEmpty(currentDef?.Ipa2) && !string.IsNullOrEmpty(currentDef?.Ipa))
            {
                Label_IPA.Content = $"/{currentDef?.Ipa2}/";
                Label_IPA2.Content = string.IsNullOrEmpty(currentDef?.Ipa) || currentDef?.Ipa == currentDef?.Ipa2
                ? "-" : $"/{currentDef?.Ipa}/";
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
            
            // Reset both indices when setting new vocabulary
            _currentJsonDataIndex = 0;
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

        private async void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.XButton1://Back button
                    await BackgroundService.ActionPlay(ActionPlayEnum.US);
                    break;
                case MouseButton.XButton2://forward button
                    _ = TextToSpeechService.SpeakTextAsync(Label_Example.Text);
                    break;
                default:
                    break;
            }
        }

        private async void Btn_TranslateDefinition_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if there's a definition to translate
                if (string.IsNullOrEmpty(Label_EngDefination.Text))
                {
                    MessageBox.Show("No definition available to translate.", "Translation",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Disable button during translation
                Btn_TranslateDefinition.IsEnabled = false;
                
                // Get LLM service
                var llmService = LLMProviderFactory.GetLLMService();
                if (llmService == null)
                {
                    MessageBox.Show("LLM service is not configured. Please check your AI provider settings.",
                        "Translation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Translate the definition using LLM
                var translatedText = await llmService.TranslateAsync(Label_EngDefination.Text, "Vietnamese");
                
                if (!string.IsNullOrEmpty(translatedText) && translatedText != Label_EngDefination.Text)
                {
                    Label_VietnameseDefinition.Text = translatedText;
                    Label_VietnameseDefinition.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Translation failed or returned empty result.", "Translation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Translation failed: {ex.Message}", "Translation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Label_VietnameseDefinition.Visibility = Visibility.Collapsed;
            }
            finally
            {
                // Re-enable button
                Btn_TranslateDefinition.IsEnabled = true;
            }
        }
    }
}
