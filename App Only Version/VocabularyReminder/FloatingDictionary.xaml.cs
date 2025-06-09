using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using VocabularyReminder.VR.Common;
using VocabularyReminder.VR.Services;
using VR.Domain.Models;
using VR.Services;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace VR
{
    /// <summary>
    /// Interaction logic for FloatingDictionary.xaml
    /// </summary>
    public partial class FloatingDictionary : Window
    {
        #region Win32 API for Global Hotkey and Clipboard
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        private const int HOTKEY_ID = 9000;
        private const uint MOD_CTRL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_Q = 0x51; // 'Q' key
        private const uint WM_COPY = 0x0301;

        #endregion

        private bool _isPinned = false;
        private string _lastClipboardText = "";
        private DispatcherTimer _clipboardTimer;
        private Vocabulary _currentVocabulary;

        public FloatingDictionary()
        {
            InitializeComponent();
            InitializeWindow();
            SetupClipboardMonitoring();
            
            // Register hotkey after window is loaded
            this.Loaded += FloatingDictionary_Loaded;
            
            // Start/stop clipboard monitoring based on window visibility
            this.IsVisibleChanged += FloatingDictionary_IsVisibleChanged;
        }

        private void FloatingDictionary_Loaded(object sender, RoutedEventArgs e)
        {
            RegisterGlobalHotkey();
        }

        private void FloatingDictionary_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                // Start clipboard monitoring when window becomes visible
                _clipboardTimer?.Start();
            }
            else
            {
                // Stop clipboard monitoring when window is hidden to save resources
                _clipboardTimer?.Stop();
            }
        }

        private void InitializeWindow()
        {
            // Position window at bottom-right corner
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 20;
            this.Top = SystemParameters.PrimaryScreenHeight - this.Height - 60;
            
            // Set initial state
            this.Topmost = true;
            this.ShowActivated = false;
            
            // Setup placeholder text behavior
            Txt_Input.GotFocus += (s, e) =>
            {
                if (Txt_Input.Text == "Type word here or select text from any application...")
                {
                    Txt_Input.Text = "";
                    Txt_Input.Foreground = System.Windows.Media.Brushes.White;
                }
            };
            
            Txt_Input.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(Txt_Input.Text))
                {
                    Txt_Input.Text = "Type word here or select text from any application...";
                    Txt_Input.Foreground = System.Windows.Media.Brushes.Gray;
                }
            };
        }

        private void SetupClipboardMonitoring()
        {
            // Monitor clipboard changes
            _clipboardTimer = new DispatcherTimer();
            _clipboardTimer.Interval = TimeSpan.FromMilliseconds(500);
            _clipboardTimer.Tick += ClipboardTimer_Tick;
            // Don't start timer here - it will be started when window becomes visible
        }

        private void ClipboardTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    string clipboardText = System.Windows.Clipboard.GetText().Trim();
                    
                    // Check if it's a single word or short phrase and different from last time
                    if (!string.IsNullOrEmpty(clipboardText) && 
                        clipboardText != _lastClipboardText &&
                        clipboardText.Length <= 50 && 
                        !clipboardText.Contains("\n") &&
                        IsLikelyWord(clipboardText))
                    {
                        _lastClipboardText = clipboardText;
                        
                        // Auto-lookup if it's a single word
                        if (clipboardText.Split(' ').Length <= 2)
                        {
                            Txt_Input.Text = clipboardText;
                            _ = LookupWordAsync(clipboardText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently handle clipboard access errors
                Debug.WriteLine($"Clipboard monitoring error: {ex.Message}");
            }
        }

        private bool IsLikelyWord(string text)
        {
            // Simple check to see if text looks like a word
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            // Should contain mostly letters
            int letterCount = 0;
            foreach (char c in text)
            {
                if (char.IsLetter(c)) letterCount++;
            }
            
            return letterCount > text.Length * 0.7; // At least 70% letters
        }

        private void RegisterGlobalHotkey()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                var source = System.Windows.Interop.HwndSource.FromHwnd(helper.Handle);
                source.AddHook(WndProc);
                
                // Register Ctrl+Shift+D hotkey
                RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CTRL | MOD_SHIFT, VK_Q);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register hotkey: {ex.Message}");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                // Hotkey pressed - toggle window visibility
                if (this.IsVisible)
                {
                    this.Hide();
                }
                else
                {
                    ShowAndFocusWindow();
                    _ = CaptureSelectedText();
                }
                handled = true;
            }
            
            return IntPtr.Zero;
        }

        private async Task CaptureSelectedText()
        {
            try
            {
                // Send Ctrl+C to copy selected text
                System.Windows.Forms.SendKeys.SendWait("^c");
                
                // Wait a bit for clipboard to update
                await Task.Delay(100);
                
                if (System.Windows.Clipboard.ContainsText())
                {
                    string selectedText = System.Windows.Clipboard.GetText().Trim();
                    if (!string.IsNullOrEmpty(selectedText) && selectedText.Length <= 50)
                    {
                        Txt_Input.Text = selectedText;
                        await LookupWordAsync(selectedText);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to capture selected text: {ex.Message}");
            }
        }

        private void ShowAndFocusWindow()
        {
            this.Show();
            this.Activate();
            this.Focus();
            
            // Start clipboard monitoring when showing window
            _clipboardTimer?.Start();
            
            // Focus and select text input (these will work once XAML is properly compiled)
            try
            {
                // Use FindName to get controls if direct references don't work
                var txtInput = this.FindName("Txt_Input") as System.Windows.Controls.TextBox;
                if (txtInput != null)
                {
                    txtInput.Focus();
                    txtInput.SelectAll();
                }
            }
            catch { }
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

            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                UnregisterHotKey(helper.Handle, HOTKEY_ID);
            }
            catch { }

            base.OnClosed(e);
        }
    }
}