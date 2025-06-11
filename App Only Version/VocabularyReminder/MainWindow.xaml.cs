using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using VocabularyReminder.VR.Common;
using VR.Domain;
using VR.Domain.Models;
using VR.Dto;
using VR.Infrastructure;
using VR.Services;

namespace VR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /* Start HotKey */

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const int MAX_TASKS = 4;
        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        private IntPtr _windowHandle;
        private HwndSource _source;
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            
            // Register FloatingDictionary hotkey immediately when app starts
            RegisterFloatingDictionaryHotKey();
        }

        private bool _isHotKeyRegister = false;
        private List<Vocabulary> _vocabularies = new List<Vocabulary>();
        private FloatingDictionary _floatingDictionary;

        private void RegisterLearningHotKeys()
        {
            if (_windowHandle == IntPtr.Zero) return;
            
            // Set up the hook if not already done
            if (!_isHotKeyRegister)
            {
                _isHotKeyRegister = true;
                _source.AddHook(HwndHook);
            }

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 1, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.F1.GetHashCode());  // Show Current Toast

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 2, (int)KeyModifier.Shift, (uint)System.Windows.Forms.Keys.F1.GetHashCode());  // Toggle Start

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 3, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.F8.GetHashCode());      // Play Sound 1

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 4, (int)KeyModifier.Shift, (uint)System.Windows.Forms.Keys.F8.GetHashCode());      // Play Sound 2

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 5, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.PrintScreen.GetHashCode());  // Delete

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 6, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.Scroll.GetHashCode());  // Next

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 7, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.Pause.GetHashCode());  // Next and Delete
        }

        private void RegisterFloatingDictionaryHotKey()
        {
            if (_windowHandle == IntPtr.Zero) return;
            
            // Set up the hook if not already done
            if (!_isHotKeyRegister)
            {
                _isHotKeyRegister = true;
                _source.AddHook(HwndHook);
            }
            
            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 8, (int)(KeyModifier.Control | KeyModifier.Shift), 0x51);  // Toggle FloatingDictionary (Q key = 0x51)
        }

        private void UnRegisterLearningHotKeys()
        {
            if (_windowHandle == IntPtr.Zero) return;

            // Only unregister the learning hotkeys (HOTKEY_ID + 1 to HOTKEY_ID + 7)
            for (int i = HOTKEY_ID + 1; i <= HOTKEY_ID + 7; i++)
            {
                UnregisterHotKey(_windowHandle, i);
            }
            
            // Don't remove the hook or set _isHotKeyRegister to false
            // because FloatingDictionary hotkey (HOTKEY_ID + 8) still needs them
        }

        private void UnRegisterFloatingDictionaryHotKey()
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID + 8);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID + 1:
                            App.LastReaction = DateTime.Now;
                            if (App.isShowPopup)
                            {
                                App.isShowPopup = false;
                                BackgroundService.HideToast();
                            }
                            else
                            {
                                _ = BackgroundService.ShowCurrentToast();
                            }
                            handled = true;
                            break;
                        case HOTKEY_ID + 2:
                            App.LastReaction = DateTime.Now;
                            ToggleLearning();
                            handled = true;
                            break;
                        case HOTKEY_ID + 3: // F8
                            App.LastReaction = DateTime.Now;
                            _ = BackgroundService.ActionPlay(ActionPlayEnum.US);
                            handled = true;
                            break;
                        case HOTKEY_ID + 4: // SHIFT + F8
                            App.LastReaction = DateTime.Now;
                            _ = BackgroundService.ActionPlay(ActionPlayEnum.UK);
                            handled = true;
                            break;
                        //case HOTKEY_ID + 5:
                        //    App.LastReaction = DateTime.Now;
                        //    _ = BackgroundService.DeleteVocabularyAsync();
                        //    handled = true;
                        //    break;
                        case HOTKEY_ID + 6:
                            App.LastReaction = DateTime.Now;
                            _ = BackgroundService.NextVocabularyAsync();
                            handled = true;
                            break;
                        //case HOTKEY_ID + 7:
                        //    App.LastReaction = DateTime.Now;
                        //    _ = BackgroundService.NextAndDeleteVocabulary();
                        //    handled = true;
                        //    break;
                        case HOTKEY_ID + 8: // Ctrl+Shift+Q
                            App.LastReaction = DateTime.Now;
                            ToggleFloatingDictionary();
                            handled = true;
                            break;
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        /* End HotKey */

        CancellationTokenSource _TokenSource;
        CancellationToken _CancelToken;

        private bool IsStarted = false;

        const int CoreMultipleThread = 3;

        const string placeHolder = "Enter your vocabulary list here.... \nThen click \"Import\" to auto get content.\n\nFor sentences with translations, use format:\nEnglish sentence | Vietnamese translation\n\nExample:\nHello world | Xin chào thế giới\nI love programming | Tôi yêu lập trình";

        public MainWindow()
        {
            InitializeComponent();
            this.Inp_ListWord.Text = placeHolder;
            // Add event handlers
            this.Inp_GlobalDictionaryId.SelectionChanged += Inp_GlobalDictionaryId_SelectionChanged;
            this.Inp_RandomOption.Checked += Settings_Changed;
            this.Inp_RandomOption.Unchecked += Settings_Changed;
            this.Inp_AutoPlayOption.Checked += Settings_Changed;
            this.Inp_AutoPlayOption.Unchecked += Settings_Changed;
            this.Inp_ShowNextOnEasyOption.Checked += Settings_Changed;
            this.Inp_ShowNextOnEasyOption.Unchecked += Settings_Changed;
            this.Inp_HideIPAFirstOption.Checked += Settings_Changed;
            this.Inp_HideIPAFirstOption.Unchecked += Settings_Changed;
            this.Inp_TimeRepeat.TextChanged += Settings_Changed;

            Load_Dictionaries();
            Status_Reset();
        }

        public void UpdateGoogleButtonVisibility()
        {
            // This method is now public so the GoogleDriveWindow can call it
            // The Google Drive button is always visible
        }

        private void Load_Dictionaries()
        {
            Dispatcher.Invoke(() =>
            {
                List<Dictionary> dictionaries = DataService.GetDictionariesAsync().Result;
                
                // Add "All" option at the beginning
                var allDictionaries = new List<Dictionary>
                {
                    new Dictionary { Id = (int)DictionaryConsts.All, Name = "All", Description = "All Dictionaries" }
                };
                allDictionaries.AddRange(dictionaries);
                
                this.Inp_GlobalDictionaryId.ItemsSource = allDictionaries;
                
                string settingsPath = ApplicationIO.GetSettingsPath();
                if (File.Exists(settingsPath))
                {
                    try
                    {
                        var json = File.ReadAllText(settingsPath);
                        var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                        
                        // Load dictionary ID
                        if (settings.ContainsKey("lastDictionaryId"))
                        {
                            int lastId = ((JsonElement)settings["lastDictionaryId"]).GetInt32();
                            if (dictionaries.Any(d => d.Id == lastId))
                                Inp_GlobalDictionaryId.SelectedValue = lastId;
                            else
                                Inp_GlobalDictionaryId.SelectedValue = (int)DictionaryConsts.All;

                            // Load other settings
                            if (settings.ContainsKey("isRandomWords"))
                            {
                                Inp_RandomOption.IsChecked = ((JsonElement)settings["isRandomWords"]).GetBoolean();
                            }
                            if (settings.ContainsKey("isAutoPlaySounds"))
                            {
                                Inp_AutoPlayOption.IsChecked = ((JsonElement)settings["isAutoPlaySounds"]).GetBoolean();
                            }
                            if (settings.ContainsKey("timeRepeat"))
                            {
                                Inp_TimeRepeat.Text = ((JsonElement)settings["timeRepeat"]).GetInt32().ToString();
                            }

                            App.isRandomWords = Inp_RandomOption.IsChecked.GetValueOrDefault();
                            App.isAutoPlaySounds = Inp_AutoPlayOption.IsChecked.GetValueOrDefault();

                            if (settings.ContainsKey("showNextOnEasy"))
                            {
                                Inp_ShowNextOnEasyOption.IsChecked = ((JsonElement)settings["showNextOnEasy"]).GetBoolean();
                                App.showNextOnEasy = Inp_ShowNextOnEasyOption.IsChecked.GetValueOrDefault();
                            }
                
                            if (settings.ContainsKey("hideIPAFirst"))
                            {
                                Inp_HideIPAFirstOption.IsChecked = ((JsonElement)settings["hideIPAFirst"]).GetBoolean();
                                App.hideIPAFirst = Inp_HideIPAFirstOption.IsChecked.GetValueOrDefault();
                            }
                
                            return;
                        }
                    }
                    catch { }
                }
                
                // Default to last dictionary if no saved setting
                this.Inp_GlobalDictionaryId.SelectedIndex = dictionaries.Any() ? dictionaries.Max(e => e.Id) - 1 : 0;
            });
        }

        private void Status_Reset()
        {
            Dispatcher.Invoke(() =>
            {
                Status_Message.Text = String.Empty;
                Status_UpdateProgressBar();
            });
        }

        public void Status_UpdateMessage(string _message)
        {
            Dispatcher.Invoke(() =>
            {
                Status_Message.Text = _message;
            });
        }

        public void Status_UpdateProgressBar(int value = 0, int max = 100)
        {
            Dispatcher.Invoke(() =>
            {
                Status_ProgessBar.Value = value;
                Status_ProgessBar.Maximum = max;
            });
        }

        // Parse Input from UI to list of words by filter them via vocabularies in csv file
        private List<string> GetListWords() {

            string tempInp = Inp_ListWord.Text;
            if (tempInp == placeHolder)
            {
                MessageBox.Show("You need to enter vocabulary words before Import...");
                return default;
            }

            // Check if input contains sentence format (with | separator)
            if (ContainsSentenceFormat(tempInp))
            {
                return GetSentencesFromInput(tempInp);
            }

            var (dictionary, maxWordLength) = StaticDataAccess.ReadDictionaryCSV(ApplicationIO.GetDictionaryCSV());

            var ListWord = ParseParagraph(tempInp);
            ListWord.RemoveAll(x => string.IsNullOrEmpty(x));

            return ListWord;
        }

        // Check if input contains sentence format with pipe separator
        private bool ContainsSentenceFormat(string input)
        {
            var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("|") && line.Split('|').Length == 2)
                {
                    return true;
                }
            }
            return false;
        }

        // Extract sentences from input in format "English sentence | Vietnamese translation"
        private List<string> GetSentencesFromInput(string input)
        {
            var sentences = new List<string>();
            var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;
                
                if (trimmedLine.Contains("|"))
                {
                    var parts = trimmedLine.Split('|');
                    if (parts.Length == 2)
                    {
                        var englishSentence = parts[0].Trim();
                        var translation = parts[1].Trim();
                        
                        if (!string.IsNullOrEmpty(englishSentence) && !string.IsNullOrEmpty(translation))
                        {
                            // Store the sentence with a special marker to indicate it has translation
                            sentences.Add($"SENTENCE:{englishSentence}|{translation}");
                        }
                    }
                }
                else
                {
                    // Regular word without translation
                    sentences.Add(trimmedLine);
                }
            }
            
            return sentences;
        }

        private List<string> ParseParagraph(string paragraph)
        {
            var wordArray = paragraph.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return wordArray.Distinct().ToList();
        }

        private async void Btn_Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inputWords = GetListWords();
                if (inputWords == default) return;

                var dicId = (int)this.Inp_GlobalDictionaryId.SelectedValue;

                List<Vocabulary> existWords = new List<Vocabulary>();
                List<string> newWords = new List<string>();
                foreach (var word in inputWords)
                {
                    string wordToCheck = word;
                    
                    // Extract the actual word/sentence for checking if it's a sentence format
                    if (word.StartsWith("SENTENCE:"))
                    {
                        var sentenceData = word.Substring(9); // Remove "SENTENCE:" prefix
                        var parts = sentenceData.Split('|');
                        if (parts.Length == 2)
                        {
                            wordToCheck = parts[0].Trim(); // Use the English sentence for checking
                        }
                    }
                    
                    var _item = await DataService.GetVocabularyByWordAsync(wordToCheck);
                    if (_item != null && _item.Id > 0)
                        existWords.Add(_item);
                    else
                        newWords.Add(word);
                }

                foreach (var word in existWords.Where(x => x.Status == 1))
                {
                    // Check if mapping already exists before adding
                    if (!await DataService.VocabularyMappingExistsAsync(dicId, word.Id))
                    {
                        await DataService.AddVocabularyMappingAsync(dicId, word.Id);
                    }
                }

                if (!newWords.Any())
                {
                    string message = "All vocabulary already in database, please Start to show in this list only";
                    if (existWords.Count > 0)
                    {
                        message += ". There are " + existWords.Count.ToString() + " words that are learnt.";
                    }
                    MessageBox.Show(message);
                    return;
                }

                var TotalWords = newWords.Count;

                Dispatcher.Invoke(() => Btn_Import.IsEnabled = false);
                _ = Task.Factory.StartNew(() =>
                {
                    Status_UpdateMessage("Start Importing...");
                    int Count = 0;
                    int CountSuccess = 0;

                    ParallelOptions parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 1
                    };
                    Parallel.ForEach(newWords, parallelOptions, _item =>
                    {
                        Task.Run(async () =>
                        {
                            int newVocaId = 0;
                            
                            // Check if this is a sentence with translation
                            if (_item.StartsWith("SENTENCE:"))
                            {
                                var sentenceData = _item.Substring(9); // Remove "SENTENCE:" prefix
                                var parts = sentenceData.Split('|');
                                if (parts.Length == 2)
                                {
                                    var englishSentence = parts[0].Trim();
                                    var translation = parts[1].Trim();
                                    
                                    // Add sentence with translation and type
                                    newVocaId = await DataService.AddVocabularyAsync(englishSentence, translation, VocaType.Sentence);
                                }
                            }
                            else
                            {
                                // Regular word import
                                newVocaId = await DataService.AddVocabularyAsync(_item);
                            }
                            
                            if (newVocaId > 0)
                            {
                                // Check if mapping already exists before adding
                                if (!await DataService.VocabularyMappingExistsAsync(dicId, newVocaId))
                                {
                                    await DataService.AddVocabularyMappingAsync(dicId, newVocaId);
                                }
                                CountSuccess++;
                            }
                            Status_UpdateProgressBar(++Count, TotalWords);
                        }).Wait();
                    });

                    BackgroundCrawl().Wait();

                    Status_UpdateMessage("Imported Success " + CountSuccess + "/" + Count + " entered vocabulary.");
                    Reload_Stats();
                    Dispatcher.Invoke(() => Btn_Import.IsEnabled = true);

                    if (TotalWords > 5)
                        MessageBox.Show("Imported Success " + CountSuccess + "/" + Count + " entered vocabulary.");
                });
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Import Failed: " + ex.Message);
                Reload_Stats();
                Dispatcher.Invoke(() => this.Btn_Import.IsEnabled = true);
                MessageBox.Show("Import Failed");
            }

        }

        private async Task BackgroundCrawl()
        {
            Status_UpdateMessage("Start Crawling...");
            await Task.Run(async () =>
            {
                Status_UpdateMessage("[1/4] Start Getting Translate...");
                await ProcessBackgroundTranslate().ConfigureAwait(true);
                Status_UpdateMessage("[1/4] Finished Getting Translate.");
            });   // wait to process all

            await Task.Run(async () =>
            {
                Status_UpdateMessage("[2/4] Start Getting Vocabulary Information: Define, Example, Ipa...");
                await ProcessBackgroundGetWordDefineInformation().ConfigureAwait(true);
                Status_UpdateMessage("[2/4] Finished Getting Vocabulary Information: Define, Example, Ipa.");
            });   // wait to process all

            await Task.Run(async () =>
            {
                Status_UpdateMessage("[3/4] Start Getting Related Words...");
                await ProcessBackgroundGetRelatedWords().ConfigureAwait(true);
                Status_UpdateMessage("[3/4] Finished Getting Related Words.");
            });   // wait to process all

            //await Task.Run(async () =>
            //{
            //    Status_UpdateMessage("[4/4] Start Getting from local dictionary for unprocess Words...");
            //    await ProcessBackgroundUnprocessWords().ConfigureAwait(true);
            //    Status_UpdateMessage("[4/4] Finished Getting from local dictionary for unprocess Words.");
            //});   // wait to process all

            Status_UpdateMessage("All of Crawling Finished. Enjoy the Learning Journey Now!.");
        }

        private async void Btn_ProcessDeleteData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Status_UpdateMessage("Start Deleting...");

                // Delete MP3 and Images folders
                var appFolder = Directory.CreateDirectory(ApplicationIO.GetApplicationFolderPath());
                foreach (var d in appFolder.EnumerateDirectories())
                {
                    d.Delete(true);
                }
                Status_UpdateMessage("Deleted MP3 and Images Folders");

                // Delete and recreate database
                using (var context = new VocaDbContext())
                {
                    // Drop all tables
                    await context.Database.ExecuteSqlCommandAsync("DROP TABLE IF EXISTS VocabularyMappings");
                    await context.Database.ExecuteSqlCommandAsync("DROP TABLE IF EXISTS Vocabulary");
                    await context.Database.ExecuteSqlCommandAsync("DROP TABLE IF EXISTS Dictionary");
                    
                    // Remove the database file
                    if (File.Exists(ApplicationIO.GetDatabasePath()))
                    {
                        context.Database.Connection.Close();
                        File.Delete(ApplicationIO.GetDatabasePath());
                    }
                }

                // Initialize new database
                DataService.InitializeDatabase();
                App.GlobalWordId = 0;

                Status_UpdateMessage("Deleted MP3, Images, and Database Successfully.");
                MessageBox.Show("Delete Data Completed.");
                Reload_Stats();
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Delete Data Failed: " + ex.Message);
                Reload_Stats();
                MessageBox.Show("Delete Data Failed.");
            }
        }


        public async Task ProcessBackgroundTranslate()
        {
            try
            {
                var listVocabulary = await DataService.GetListVocabularyToTranslateAsync(App.GlobalDicId).ConfigureAwait(true);

                int totalItems = listVocabulary.Count;
                int count = 0;

                var pluralizationService = PluralizationService.CreateService(new System.Globalization.CultureInfo("en-US"));

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount * CoreMultipleThread, MAX_TASKS);
                Parallel.ForEach(listVocabulary, parallelOptions, _item =>
                {
                    Task.Run(async () =>
                    {
                        var trimmedWord = _item.Word.Trim(" ()".ToCharArray());
                        if (_item.Word != trimmedWord)
                            _item.Word = trimmedWord;

                        var voca = await TranslateService.GetVocabularyVietnameseTranslateAsync(_item).ConfigureAwait(true);
                        if (string.IsNullOrEmpty(voca.Translate))
                        {
                            if (pluralizationService.IsPlural(_item.Word))
                            {
                                _item.Word = pluralizationService.Singularize(_item.Word);
                                await TranslateService.GetVocabularyVietnameseTranslateAsync(_item).ConfigureAwait(true);
                            }
                        }

                        Status_UpdateProgressBar(++count, totalItems);
                    }).Wait();
                });
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Crawling: Process Background Translate Failed: " + ex.Message);
            }
        }

        public async Task ProcessBackgroundGetWordDefineInformation()
        {
            try
            {
                var ListVocabulary = await DataService.GetListVocabularyToGetDefineExampleMp3URLAsync(App.GlobalDicId).ConfigureAwait(true);

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount * CoreMultipleThread, MAX_TASKS);

                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    Task.Run(async () =>
                    {
                        await SyncVocaService.SyncVocabularyAsync(_item).ConfigureAwait(true);
                        Status_UpdateProgressBar(++Count, TotalItems);
                    }).Wait();
                });
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Crawling: Process Background Get English Define, Ipa, Type, Example, MP3 URL Fail: " + ex.Message);
            }

        }

        public async Task ProcessBackgroundGetRelatedWords()
        {
            try
            {
                var ListVocabulary = await DataService.GetListVocabularyToGetRelatedWordsAsync(App.GlobalDicId).ConfigureAwait(true);

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount * CoreMultipleThread, MAX_TASKS);
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    Task.Run(async () =>
                    {
                        await TranslateService.GetRelatedWord(_item).ConfigureAwait(true);
                        Status_UpdateProgressBar(++Count, TotalItems);
                    });
                });
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Crawling: Process Background Get Related Words Fail: " + ex.Message);
            }
        }

        private void Reload_Stats()
        {
            Dispatcher.Invoke(() =>
            {
                if (IsActive)
                {
                    var dictionaryId = (int)this.Inp_GlobalDictionaryId.SelectedValue;
                    StatDtos _Stats = DataService.GetStats(dictionaryId);
                    this.Label_Stats_ImportedWords.Content = _Stats.Total.ToString();
                    this.Label_Stats_RememberedWords.Content = _Stats.Remembered.ToString();
                    this.Label_LearnedCount.Content = _Stats.DictionaryLearned.ToString();
                    this.Label_NotLearnedCount.Content = _Stats.DictionaryNotLearned.ToString();
                }
            });
        }


        private void Btn_StartLearning_Click(object sender, RoutedEventArgs e)
        {
            _vocabularies.Clear();
            ToggleLearning();
        }

        private void ToggleLearning()
        {
            if (!IsStarted)
            {
                RegisterLearningHotKeys();

                IsStarted = true;
                //App.GlobalDicId = (int)Inp_GlobalDictionaryId.SelectedValue;
                App.isRandomWords = Inp_RandomOption.IsChecked.GetValueOrDefault();
                App.isAutoPlaySounds = Inp_AutoPlayOption.IsChecked.GetValueOrDefault();
                App.showNextOnEasy = Inp_ShowNextOnEasyOption.IsChecked.GetValueOrDefault();
                App.hideIPAFirst = Inp_HideIPAFirstOption.IsChecked.GetValueOrDefault();

                Btn_StartLearning.Content = "Stop Learning";
                // Init value
                _TokenSource = new CancellationTokenSource();
                _CancelToken = _TokenSource.Token;

                _ = int.TryParse(Inp_TimeRepeat.Text, out int TimeRepeat);

                if (TimeRepeat < 0) { TimeRepeat = 1; };
                TimeRepeat *= 1000;

                App.LastReaction = new DateTime();

                _ = Task.Factory.StartNew(async () =>
                  {
                      while (true)
                      {
                          int _waitMore = 0;
                          while ((DateTime.Now - App.LastReaction).TotalMilliseconds < TimeRepeat)
                          {
                              _waitMore = (int)(TimeRepeat - (DateTime.Now - App.LastReaction).TotalMilliseconds);
                              Console.WriteLine(String.Format("Last Reation {0} -> wait more {1} ms", App.LastReaction.ToShortTimeString(), _waitMore));
                              Thread.Sleep(_waitMore);
                          }

                          if ((DateTime.Now - App.LastReaction).TotalMilliseconds < TimeRepeat)
                            continue;

                          if (_CancelToken.IsCancellationRequested)
                              break;

                          VocabularyDisplayService.Hide();
                          await BackgroundService.NextVocabularyAsync(_vocabularies);
                          await Task.Delay(TimeRepeat, _CancelToken);
                      }
                  }, _CancelToken);

                WindowState = WindowState.Minimized;
            }
            else
            {
                StopLearning();
            }
        }

        private void StopLearning()
        {
            IsStarted = false;
            Btn_StartLearning.Content = "Start Learning";

            VocabularyDisplayService.Hide();
            _TokenSource.Cancel();
            UnRegisterLearningHotKeys();
            Console.WriteLine("Stop and active Cancel Token");
        }

        private void Inp_TimeRepeat_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            var fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            double val;
            e.Handled = !double.TryParse(fullText, out val);
        }

        private void Btn_PreloadMp3_Click(object sender, RoutedEventArgs e)
        {
            Status_UpdateMessage("Downloading Mp3...");

            Dispatcher.Invoke(() => this.Btn_PreloadMp3.IsEnabled = false);
            Task.Run(() =>
            {
                ProcessBackgroundDownloadMp3();
                Dispatcher.Invoke(() => this.Btn_PreloadMp3.IsEnabled = true);
            });
        }

        private async void ProcessBackgroundDownloadMp3()
        {
            try
            {
                var ListVocabulary = await DataService.GetListVocabularyToPreloadMp3Async();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount * CoreMultipleThread, MAX_TASKS);
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    Mp3Service.preloadMp3MultipleAsync(_item).Wait();
                    Status_UpdateProgressBar(++Count, TotalItems);
                });

                Status_UpdateMessage("Downloading MP3 Files Finished.");
                MessageBox.Show("Downloading MP3 Files Finished");
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Downloading MP3 Files Failed: " + ex.Message);
            }
        }

        private async void Btn_Import_Auto_Click(object sender, RoutedEventArgs e)
        {
            throw new ApplicationException("Database is out of date");

            //Status_UpdateMessage("Downloading 3000 common words....");
            //var ImportService = new ImportBackupDataService();
            //await ImportService.ImportDemo3000WordsAsync();
            //Reload_Stats();
            //Status_UpdateMessage("Downloaded 3000 common words success.");
            //MessageBox.Show("Downloaded 3000 common words success.");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_TokenSource != null) _TokenSource.Cancel();
            VocabularyDisplayService.Hide();
            
            // When window is closing, clean up everything properly
            if (_isHotKeyRegister)
            {
                _isHotKeyRegister = false;
                _source.RemoveHook(HwndHook);
                
                // Unregister all hotkeys including learning hotkeys and FloatingDictionary
                for (int i = HOTKEY_ID + 1; i <= HOTKEY_ID + 8; i++)
                {
                    UnregisterHotKey(_windowHandle, i);
                }
            }

            // Clean up FloatingDictionary
            _floatingDictionary?.Close();
            
            // Close all other windows when MainWindow is closing
            foreach (Window window in Application.Current.Windows)
            {
                if (window != this && window.IsLoaded)
                {
                    window.Close();
                }
            }
            
            base.OnClosed(e);
        }

        private void Inp_GlobalDictionaryId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Inp_GlobalDictionaryId.SelectedValue == null) return;
            App.GlobalDicId = (int)Inp_GlobalDictionaryId.SelectedValue;
            SaveSettings();
            Reload_Stats();
        }

        private void Settings_Changed(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            var settings = new Dictionary<string, object>();
            string settingsPath = ApplicationIO.GetSettingsPath();

            // Load existing settings if any
            if (File.Exists(settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(settingsPath);
                    settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                }
                catch { }
            }

            // Update settings
            settings["lastDictionaryId"] = Inp_GlobalDictionaryId.SelectedValue;
            settings["isRandomWords"] = Inp_RandomOption.IsChecked;
            settings["isAutoPlaySounds"] = Inp_AutoPlayOption.IsChecked;
            settings["showNextOnEasy"] = Inp_ShowNextOnEasyOption.IsChecked;
            settings["hideIPAFirst"] = Inp_HideIPAFirstOption.IsChecked;
            settings["timeRepeat"] = int.Parse(Inp_TimeRepeat.Text);

            // Save settings
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                Status_UpdateMessage($"Failed to save settings: {ex.Message}");
            }
        }

        private void Frm_MainWindow_Activated(object sender, EventArgs e)
        {
            Reload_Stats();
        }

        private void Btn_ShowLearnedList_Click(object sender, RoutedEventArgs e)
        {
            var frm = new LearnedWordsWindow();
            frm.Show();
        }

        private void Inp_ListWord_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(this.Inp_ListWord.Text))
            {
                this.Inp_ListWord.Text = placeHolder;
            }
        }

        private void Inp_ListWord_GotFocus(object sender, RoutedEventArgs e)
        {
            if (this.Inp_ListWord.Text == placeHolder)
            {
                this.Inp_ListWord.Text = "";
            }
        }

        private async void Btn_Start_Custom_Click(object sender, RoutedEventArgs e)
        {
            var words = GetListWords();
            if (words == default)
                return;

            _vocabularies = new List<Vocabulary>();
            foreach (var word in words)
            {
                string wordToCheck = word;
                
                // Extract the actual word/sentence for checking if it's a sentence format
                if (word.StartsWith("SENTENCE:"))
                {
                    var sentenceData = word.Substring(9); // Remove "SENTENCE:" prefix
                    var parts = sentenceData.Split('|');
                    if (parts.Length == 2)
                    {
                        wordToCheck = parts[0].Trim(); // Use the English sentence for checking
                    }
                }
                
                var _item = await DataService.GetVocabularyByWordAsync(wordToCheck);
                if (_item != null)
                    _vocabularies.Add(_item);
            }

            ToggleLearning();
        }

        private async void Btn_Sync_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundCrawl();
        }

        private async void Btn_Cleanup_Click(object sender, RoutedEventArgs e)
        {
            await DataService.CleanUnableToGetAsync();
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            bool requiresQuoting = field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n");
            if (!requiresQuoting)
                return field;

            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        private void Btn_ManageDictionary_Click(object sender, RoutedEventArgs e)
        {
            var window = new DictionaryManagementWindow();
            window.Closed += (s, args) => Load_Dictionaries();
            window.Show();
        }

        private void Btn_BackupRestore_Click(object sender, RoutedEventArgs e)
        {
            var backupWindow = new BackupWindow(this);
            backupWindow.Owner = this;
            backupWindow.ShowDialog();
        }

        private async void Btn_TestDefinition_Click(object sender, RoutedEventArgs e)
        {
            await DataService.MergeDuplicateVocabulariesAsync().ConfigureAwait(true);

            //var voca = await DataService.GetVocabularyByWordAsync("bank");
            //await TranslateService.GetWordDefineInformationAsync(voca);
            //Console.WriteLine(voca);

            //voca = await DataService.GetVocabularyByWordAsync("translate");
            //await TranslateService.GetWordDefineInformationAsync(voca);
            //Console.WriteLine(voca);

            //voca = await DataService.GetVocabularyByWordAsync("study");
            //await TranslateService.GetWordDefineInformationAsync(voca);
            //Console.WriteLine(voca);
        }

        private void Btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void Btn_Stats_Click(object sender, RoutedEventArgs e)
        {
            var statsWindow = new StatsWindow();
            statsWindow.ShowDialog();
        }

        private void Btn_FloatingDict_Click(object sender, RoutedEventArgs e)
        {
            ToggleFloatingDictionary();
        }

        /// <summary>
        /// Gets or creates the FloatingDictionary instance
        /// </summary>
        private FloatingDictionary GetFloatingDictionary()
        {
            if (_floatingDictionary == null || !_floatingDictionary.IsLoaded)
            {
                _floatingDictionary = new FloatingDictionary();
            }
            return _floatingDictionary;
        }

        /// <summary>
        /// Toggles the FloatingDictionary window visibility
        /// </summary>
        private void ToggleFloatingDictionary()
        {
            var floatingDict = GetFloatingDictionary();
            if (floatingDict.IsVisible)
            {
                floatingDict.Hide();
            }
            else
            {
                floatingDict.Show();
                floatingDict.Activate();
                floatingDict.Focus();
            }
        }
    }

}
