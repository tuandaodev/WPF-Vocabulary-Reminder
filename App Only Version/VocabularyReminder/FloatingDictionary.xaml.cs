using System;
using System.Diagnostics;
using System.Linq;
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
        
        private const int MIN_WORD_LENGTH = 1;
        private const int MAX_WORD_LENGTH = 50;
        private const int CLIPBOARD_CHECK_INTERVAL = 500;
        private const double LETTER_THRESHOLD = 0.7;
        private const int MAX_WORD_COUNT = 2;
        private const string PLACEHOLDER_TEXT = "Type word here or select text from any application...";
        
        #endregion

        #region Private Fields
        
        private bool _isPinned;
        private string _lastClipboardText = string.Empty;
        private DispatcherTimer _clipboardTimer;
        private Vocabulary _currentVocabulary;
        
        #endregion

        public FloatingDictionary()
        {
            InitializeComponent();
            InitializeWindow();
            SetupClipboardMonitoring();
            
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
            return !string.IsNullOrEmpty(text) &&
                   text != _lastClipboardText &&
                   text.Length <= MAX_WORD_LENGTH &&
                   !text.Contains("\n") &&
                   IsLikelyWord(text);
        }

        private static bool IsAutoLookupCandidate(string text)
        {
            return text.Split(' ').Length <= MAX_WORD_COUNT;
        }

        private static bool IsLikelyWord(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            int letterCount = text.Count(char.IsLetter);
            return letterCount > text.Length * LETTER_THRESHOLD;
        }



        private void ShowAndFocusWindow()
        {
            Show();
            Activate();
            Focus();
            
            _clipboardTimer?.Start();
            
            try
            {
                Txt_Input.Focus();
                Txt_Input.SelectAll();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to focus input: {ex.Message}");
            }
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

                        System.Windows.MessageBox.Show("Word added to dictionary!", "Success", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Word already exists in dictionary.", "Info", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error adding word: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
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

        protected override void OnClosed(EventArgs e)
        {
            // Cleanup
            _clipboardTimer?.Stop();

            base.OnClosed(e);
        }
    }
}