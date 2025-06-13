using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VocabularyReminder.VR.Common;
using VocabularyReminder.VR.Services;
using VR.Domain.Models;
using VR.Services;

namespace VR
{
    /// <summary>
    /// Interaction logic for FloatingDictionary.xaml
    /// </summary>
    public partial class FloatingDictionary : Window
    {
        #region Constants
        
        private const int MIN_WORD_LENGTH = 2;
        private const int MAX_WORD_LENGTH = 50;
        private const int CLIPBOARD_CHECK_INTERVAL = 500;
        private const double LETTER_THRESHOLD = 0.75;
        private const int MAX_WORD_COUNT = 3;
        private const string PLACEHOLDER_TEXT = "Type word here or select text from any application...";
        
        // Regex patterns for better word detection
        private static readonly Regex VALID_WORD_PATTERN = new Regex(@"^[a-zA-Z]([a-zA-Z\-'\.]*[a-zA-Z])?$", RegexOptions.Compiled);
        private static readonly Regex COMMON_PREFIXES = new Regex(@"^(un|re|pre|dis|over|under|out|up|in|im|non|anti|de|pro|sub|super|trans|inter|multi|auto|co|ex|post|semi)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex COMMON_SUFFIXES = new Regex(@"(ing|ed|er|est|ly|tion|sion|ness|ment|able|ible|ful|less|ous|ive|al|ic|acy|ity|ism|ist|ship|ward|wise|like)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Words to exclude from auto-lookup
        private static readonly string[] EXCLUDED_WORDS = { "the", "and", "or", "but", "for", "nor", "so", "yet", "a", "an", "to", "of", "in", "on", "at", "by", "with", "from", "as", "is", "was", "are", "were", "be", "been", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "can", "must", "shall", "i", "you", "he", "she", "it", "we", "they", "me", "him", "her", "us", "them", "my", "your", "his", "our", "their", "this", "that", "these", "those" };
        
        #endregion

        #region Private Fields
        
        private bool _isPinned;
        private string _lastClipboardText = string.Empty;
        private DispatcherTimer _clipboardTimer;
        private Vocabulary _currentVocabulary;
        private string _originalGrammarText = string.Empty;
        private string _correctedGrammarText = string.Empty;
        private double _originalWindowHeight = 400;
        private double _grammarWindowHeight = 600;
        
        #endregion

        public FloatingDictionary()
        {
            InitializeComponent();
            InitializeWindow();
            SetupClipboardMonitoring();
            
            // Setup grammar placeholder text when loaded
            this.Loaded += (s, e) =>
            {
                SetupGrammarPlaceholderText();
                SetupTabEventHandlers();
                _originalWindowHeight = this.Height;
            };
            
            // Start/stop clipboard monitoring based on window visibility
            this.IsVisibleChanged += FloatingDictionary_IsVisibleChanged;
        }

        private void FloatingDictionary_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                _clipboardTimer?.Start();
            }
            else
            {
                _clipboardTimer?.Stop();
            }
        }

        private void InitializeWindow()
        {
            SetWindowPosition();
            SetWindowProperties();
            SetupPlaceholderText();
        }

        private void SetWindowPosition()
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = SystemParameters.PrimaryScreenWidth - Width - 20;
            Top = SystemParameters.PrimaryScreenHeight - Height - 60;
        }

        private void SetWindowProperties()
        {
            Topmost = true;
            ShowActivated = false;
        }

        private void SetupPlaceholderText()
        {
            Txt_Input.GotFocus += OnInputGotFocus;
            Txt_Input.LostFocus += OnInputLostFocus;
        }

        private void OnInputGotFocus(object sender, RoutedEventArgs e)
        {
            if (Txt_Input.Text == PLACEHOLDER_TEXT)
            {
                Txt_Input.Text = string.Empty;
                Txt_Input.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void OnInputLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Txt_Input.Text))
            {
                Txt_Input.Text = PLACEHOLDER_TEXT;
                Txt_Input.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void SetupClipboardMonitoring()
        {
            _clipboardTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(CLIPBOARD_CHECK_INTERVAL)
            };
            _clipboardTimer.Tick += ClipboardTimer_Tick;
        }

        private async void ClipboardTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!System.Windows.Clipboard.ContainsText()) return;

                string clipboardText = System.Windows.Clipboard.GetText().Trim();
                
                if (IsValidClipboardText(clipboardText))
                {
                    _lastClipboardText = clipboardText;
                    
                    if (IsAutoLookupCandidate(clipboardText))
                    {
                        Txt_Input.Text = clipboardText;
                        await LookupWordAsync(clipboardText);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Clipboard monitoring error: {ex.Message}");
            }
        }

        private bool IsValidClipboardText(string text)
        {
            if (string.IsNullOrEmpty(text) || text == _lastClipboardText)
                return false;

            // Basic length and format checks
            if (text.Length < MIN_WORD_LENGTH || text.Length > MAX_WORD_LENGTH)
                return false;

            // Remove common issues with clipboard text
            text = CleanClipboardText(text);

            // Check for multi-line text or excessive whitespace
            if (text.Contains("\n") || text.Contains("\r") || text.Split(' ').Length > MAX_WORD_COUNT)
                return false;

            // Check if it's likely a word or phrase
            return IsLikelyWord(text);
        }

        private static bool IsAutoLookupCandidate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var words = text.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Check word count
            if (words.Length > MAX_WORD_COUNT)
                return false;

            // For single words, check if it's not a common word
            if (words.Length == 1)
            {
                string word = words[0].ToLower();
                return !EXCLUDED_WORDS.Contains(word) && IsValidWordStructure(word);
            }

            // For phrases, ensure all words are valid
            return words.All(w => IsValidWordStructure(w) && w.Length >= MIN_WORD_LENGTH);
        }

        private static bool IsLikelyWord(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Split into words for phrase analysis
            var words = text.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length == 0)
                return false;

            // Check each word individually
            foreach (var word in words)
            {
                if (!IsValidWordStructure(word))
                    return false;
            }

            return true;
        }

        private static bool IsValidWordStructure(string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length < MIN_WORD_LENGTH)
                return false;

            // Check letter ratio
            int letterCount = word.Count(char.IsLetter);
            if (letterCount < word.Length * LETTER_THRESHOLD)
                return false;

            // Use regex pattern for basic word structure
            if (!VALID_WORD_PATTERN.IsMatch(word))
                return false;

            // Additional checks for word-like patterns
            return HasValidWordPattern(word);
        }

        private static bool HasValidWordPattern(string word)
        {
            // Check for common word patterns
            string lowerWord = word.ToLower();

            // Allow words with common prefixes or suffixes
            if (COMMON_PREFIXES.IsMatch(lowerWord) || COMMON_SUFFIXES.IsMatch(lowerWord))
                return true;

            // Check for reasonable vowel distribution
            int vowelCount = lowerWord.Count(c => "aeiou".Contains(c));
            double vowelRatio = (double)vowelCount / word.Length;
            
            // Words should have reasonable vowel distribution (20-60%)
            if (vowelRatio < 0.2 || vowelRatio > 0.6)
                return false;

            // Check for excessive repeated characters
            for (int i = 0; i < word.Length - 2; i++)
            {
                if (word[i] == word[i + 1] && word[i] == word[i + 2])
                    return false; // Three consecutive identical characters
            }

            return true;
        }

        private static string CleanClipboardText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove leading/trailing whitespace and common formatting
            text = text.Trim();

            // Remove common punctuation that might be accidentally selected
            text = text.Trim(new char[] { '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '|', '*', '+', '=', '_', '~', '`' });

            // Remove extra whitespace
            text = Regex.Replace(text, @"\s+", " ");

            return text.Trim();
        }

        private async Task LookupWordAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return;
            
            // Clean the word
            word = word.Trim().ToLower();
            
            // Show loading
            ShowLoading(true);
            HideAllResults();
            
            try
            {
                // First check if word exists in database
                var existingVocab = await DataService.GetVocabularyByWordAsync(word);
                
                if (existingVocab != null)
                {
                    _currentVocabulary = existingVocab;
                    DisplayVocabulary(existingVocab);
                }
                else
                {
                    // Create new vocabulary entry and get data
                    var newVocab = new Vocabulary
                    {
                        Word = word
                    };
                    
                    // Get translation
                    string translation = await TranslateService.GetGoogleTranslate(word);
                    newVocab.Translate = translation;
                    
                    // Try to get detailed information
                    await TranslateService.GetWordDefineInformationAsync(newVocab);
                    
                    _currentVocabulary = newVocab;
                    DisplayVocabulary(newVocab);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error looking up word: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void DisplayVocabulary(Vocabulary vocab)
        {
            // Display word info
            Lbl_Word.Text = vocab.Word;
            Lbl_Type.Text = vocab.Type ?? "";
            Lbl_IPA.Text = vocab.Ipa ?? "";
            Lbl_Example.Text = "";
            Panel_WordInfo.Visibility = Visibility.Visible;
            
            // Display translation
            if (!string.IsNullOrEmpty(vocab.Translate))
            {
                Lbl_Translation.Text = vocab.Translate;
                Panel_Translation.Visibility = Visibility.Visible;
            }

            // Get First Definition from JsonData
            var firstDef = vocab.JsonData?.FirstOrDefault()?.Definitions?.FirstOrDefault();

            // Display definition
            if (!string.IsNullOrEmpty(vocab.Define))
            {
                Lbl_Definition.Text = vocab.Define;
                Panel_Definition.Visibility = Visibility.Visible;
            } else
            {
                if (firstDef != null)
                {
                    Lbl_Definition.Text = firstDef.Definition;
                    Panel_Definition.Visibility = Visibility.Visible;
                }
            }

            // Display example
            if (!string.IsNullOrEmpty(vocab.Example))
            {
                Lbl_Example.Text = vocab.Example;
                Panel_Example.Visibility = Visibility.Visible;
            }
            else
            {
                // Get exampl from json data if available
                if (firstDef != null)
                {
                    var exampleData = firstDef.Examples?.FirstOrDefault();
                    if (exampleData != null)
                    {
                        Lbl_Example.Text = exampleData.Example;
                        Panel_Example.Visibility = Visibility.Visible;
                    }
                }
            }
            
            // Show action buttons
            Btn_AddToDict.Visibility = Visibility.Visible;
            Btn_OpenFull.Visibility = Visibility.Visible;
            Btn_Speak.Visibility = Visibility.Visible;
        }

        private void ShowLoading(bool show)
        {
            Panel_Loading.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HideAllResults()
        {
            Panel_WordInfo.Visibility = Visibility.Collapsed;
            Panel_Translation.Visibility = Visibility.Collapsed;
            Panel_Definition.Visibility = Visibility.Collapsed;
            Panel_Example.Visibility = Visibility.Collapsed;
            Panel_NoResults.Visibility = Visibility.Collapsed;
            Btn_AddToDict.Visibility = Visibility.Collapsed;
            Btn_OpenFull.Visibility = Visibility.Collapsed;
            Btn_Speak.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            Panel_NoResults.Visibility = Visibility.Visible;
            // Could enhance this to show actual error message
        }

        #region Event Handlers

        private async void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    this.DragMove();
                    break;
                case MouseButton.XButton1://Back button
                    // Check if Shift key is pressed
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    {
                        // Play example text when Shift+XButton1 is pressed
                        _ = TextToSpeechService.SpeakTextAsync(Lbl_Example.Text);
                    }
                    else
                    {
                        // Normal XButton1 behavior - play US pronunciation
                        await PlaySoundAsync();
                    }
                    break;
                case MouseButton.XButton2://forward button
                    _ = TextToSpeechService.SpeakTextAsync(Lbl_Example.Text);
                    break;
                default:
                    break;
            }
        }

        private void Btn_Pin_Click(object sender, RoutedEventArgs e)
        {
            _isPinned = !_isPinned;
            this.Topmost = _isPinned;
            Btn_Pin.Content = _isPinned ? "📍" : "📌";
        }

        private void Btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            // Open settings window
            var settingsWindow = new SettingsWindow();
            settingsWindow.Show();
        }

        private void Btn_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private async void Txt_Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await LookupWordAsync(Txt_Input.Text);
            }
        }

        private async void Btn_Lookup_Click(object sender, RoutedEventArgs e)
        {
            await LookupWordAsync(Txt_Input.Text);
        }

        private async void Btn_AddToDict_Click(object sender, RoutedEventArgs e)
        {
            if (_currentVocabulary != null)
            {
                try
                {
                    // Check if already exists
                    var existing = await DataService.GetVocabularyByWordAsync(_currentVocabulary.Word);
                    if (existing == null)
                    {
                        var vocaId = await DataService.AddVocabularyAsync(_currentVocabulary.Word);
                        if (vocaId > 0)
                            await DataService.AddVocabularyMappingAsync((int)DictionaryConsts.Uncategorized, vocaId);

                        // Change button text to indicate success
                        Btn_AddToDict.Content = "Added";
                    }
                    else
                    {
                        // Change button text to indicate word already exists
                        Btn_AddToDict.Content = "Already Exists, Ignored";
                    }
                }
                catch (Exception ex)
                {
                    // Keep original text on error
                    Debug.WriteLine($"Error adding word: {ex.Message}");
                }
            }
        }

        private void Btn_OpenFull_Click(object sender, RoutedEventArgs e)
        {
            if (_currentVocabulary != null)
            {
                var popup = new VocaPopup();
                popup.SetVocabulary(_currentVocabulary);
                popup.Show();
            }
        }

        private async void Btn_Speak_Click(object sender, RoutedEventArgs e)
        {
            await PlaySoundAsync();
        }

        private async Task PlaySoundAsync()
        {
            if (_currentVocabulary != null && !string.IsNullOrEmpty(_currentVocabulary.Word))
            {
                try
                {
                    await GlobalVocabularyService.PlaySoundAsync(_currentVocabulary);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error speaking word: {ex.Message}");
                }
            }
        }

        #endregion

        #region Grammar Check Event Handlers

        private void SetupGrammarPlaceholderText()
        {
            if (Txt_GrammarInput != null)
            {
                Txt_GrammarInput.GotFocus += OnGrammarInputGotFocus;
                Txt_GrammarInput.LostFocus += OnGrammarInputLostFocus;
            }
        }

        private void SetupTabEventHandlers()
        {
            // Find the TabControl and add selection changed event
            var tabControl = this.FindName("MainTabControl") as System.Windows.Controls.TabControl;
            if (tabControl != null)
            {
                tabControl.SelectionChanged += TabControl_SelectionChanged;
            }
        }

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TabControl tabControl)
            {
                var selectedTab = tabControl.SelectedItem as System.Windows.Controls.TabItem;
                if (selectedTab != null)
                {
                    // Check if Grammar tab is selected
                    if (selectedTab.Name == "Tab_Grammar")
                    {
                        // Calculate height difference
                        double heightDifference = _grammarWindowHeight - this.Height;
                        
                        // Adjust window position to grow upward instead of downward
                        this.Top = Math.Max(0, this.Top - heightDifference);
                        
                        // Increase window height for grammar tab
                        this.Height = _grammarWindowHeight;
                    }
                    else if (selectedTab.Name == "Tab_Dictionary")
                    {
                        // Calculate height difference
                        double heightDifference = this.Height - _originalWindowHeight;
                        
                        // Reset to original height for dictionary tab
                        this.Height = _originalWindowHeight;
                        
                        // Adjust window position back down
                        this.Top = this.Top + heightDifference;
                    }
                }
            }
        }

        private void OnGrammarInputGotFocus(object sender, RoutedEventArgs e)
        {
            if (Txt_GrammarInput?.Text == "Type or paste your text here for grammar checking...")
            {
                Txt_GrammarInput.Text = string.Empty;
                Txt_GrammarInput.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void OnGrammarInputLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Txt_GrammarInput?.Text))
            {
                Txt_GrammarInput.Text = "Type or paste your text here for grammar checking...";
                Txt_GrammarInput.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private async void Txt_GrammarInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                await CheckGrammarAsync(Txt_GrammarInput?.Text);
            }
        }

        private async void Btn_CheckGrammar_Click(object sender, RoutedEventArgs e)
        {
            await CheckGrammarAsync(Txt_GrammarInput?.Text);
        }

        private void Btn_CopyOriginal_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_originalGrammarText))
            {
                try
                {
                    System.Windows.Clipboard.SetText(_originalGrammarText);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error copying original text: {ex.Message}");
                }
            }
        }

        private void Btn_CopyCorrected_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_correctedGrammarText))
            {
                try
                {
                    System.Windows.Clipboard.SetText(_correctedGrammarText);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error copying corrected text: {ex.Message}");
                }
            }
        }

        private void Btn_ClearGrammar_Click(object sender, RoutedEventArgs e)
        {
            Txt_GrammarInput.Text = "Type or paste your text here for grammar checking...";
            Txt_GrammarInput.Foreground = System.Windows.Media.Brushes.Gray;
            HideAllGrammarResults();
            _originalGrammarText = string.Empty;
            _correctedGrammarText = string.Empty;
        }

        #endregion

        #region Grammar Check Methods

        private async Task CheckGrammarAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "Type or paste your text here for grammar checking...")
                return;

            // Show loading and hide previous results
            ShowGrammarLoading(true);
            HideAllGrammarResults();

            try
            {
                _originalGrammarText = text;

                // Create LLM service instance
                if (!LLMProviderFactory.IsCurrentConfigurationValid())
                {
                    ShowGrammarError("AI provider not configured. Please check settings.");
                    return;
                }

                var llmService = LLMProviderFactory.GetLLMService();

                // Create grammar check prompt
                var grammarPrompt = $@"Please check the following text for grammar, spelling, punctuation, and style issues.

Original text:
""{text}""

Please provide:
1. A corrected version of the text
2. A brief explanation of the main issues found (if any)

If the text is already correct, just say ""No issues found"" and repeat the original text.

Format your response as:
CORRECTED: [corrected text here]
ANALYSIS: [brief explanation of changes made or ""No issues found""]";

                var response = await llmService.GenerateTextAsync(grammarPrompt);
                
                if (!string.IsNullOrEmpty(response))
                {
                    ParseGrammarResponse(response);
                    DisplayGrammarResults();
                }
                else
                {
                    ShowGrammarError("No response received from grammar checker.");
                }
            }
            catch (Exception ex)
            {
                ShowGrammarError($"Error checking grammar: {ex.Message}");
            }
            finally
            {
                ShowGrammarLoading(false);
            }
        }

        private void ParseGrammarResponse(string response)
        {
            try
            {
                var lines = response.Split('\n');
                var correctedText = "";
                var analysisText = "";
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("CORRECTED:", StringComparison.OrdinalIgnoreCase))
                    {
                        correctedText = line.Substring(10).Trim();
                    }
                    else if (line.StartsWith("ANALYSIS:", StringComparison.OrdinalIgnoreCase))
                    {
                        analysisText = line.Substring(9).Trim();
                    }
                }

                // If parsing failed, use the entire response as corrected text
                if (string.IsNullOrEmpty(correctedText))
                {
                    correctedText = response.Trim();
                    analysisText = "Grammar check completed.";
                }

                _correctedGrammarText = correctedText;
                
                // Update UI elements safely
                if (Lbl_CorrectedText != null)
                    Lbl_CorrectedText.Text = correctedText;
                
                if (Lbl_GrammarSuggestions != null)
                    Lbl_GrammarSuggestions.Text = analysisText;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing grammar response: {ex.Message}");
                _correctedGrammarText = response;
                if (Lbl_CorrectedText != null)
                    Lbl_CorrectedText.Text = response;
                if (Lbl_GrammarSuggestions != null)
                    Lbl_GrammarSuggestions.Text = "Grammar check completed.";
            }
        }

        private void DisplayGrammarResults()
        {
            // Display corrected text
            if (!string.IsNullOrEmpty(_correctedGrammarText))
            {
                if (Panel_CorrectedText != null)
                    Panel_CorrectedText.Visibility = Visibility.Visible;
            }

            // Display analysis
            if (Panel_GrammarSuggestions != null)
                Panel_GrammarSuggestions.Visibility = Visibility.Visible;

            // Show action buttons
            if (Btn_CopyOriginal != null)
                Btn_CopyOriginal.Visibility = Visibility.Visible;
            if (Btn_CopyCorrected != null)
                Btn_CopyCorrected.Visibility = Visibility.Visible;
            if (Btn_ClearGrammar != null)
                Btn_ClearGrammar.Visibility = Visibility.Visible;
        }

        private void ShowGrammarLoading(bool show)
        {
            if (Panel_GrammarLoading != null)
                Panel_GrammarLoading.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HideAllGrammarResults()
        {
            if (Panel_CorrectedText != null)
                Panel_CorrectedText.Visibility = Visibility.Collapsed;
            if (Panel_GrammarSuggestions != null)
                Panel_GrammarSuggestions.Visibility = Visibility.Collapsed;
            if (Panel_GrammarNoResults != null)
                Panel_GrammarNoResults.Visibility = Visibility.Collapsed;
            if (Btn_CopyOriginal != null)
                Btn_CopyOriginal.Visibility = Visibility.Collapsed;
            if (Btn_CopyCorrected != null)
                Btn_CopyCorrected.Visibility = Visibility.Collapsed;
            if (Btn_ClearGrammar != null)
                Btn_ClearGrammar.Visibility = Visibility.Collapsed;
        }

        private void ShowGrammarError(string message)
        {
            if (Panel_GrammarNoResults != null)
            {
                Panel_GrammarNoResults.Visibility = Visibility.Visible;
                // Could enhance this to show actual error message
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            // Cleanup
            _clipboardTimer?.Stop();

            base.OnClosed(e);
        }
    }
}