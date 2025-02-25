﻿﻿﻿using DesktopNotifications.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Text.Json;
using VocabularyReminder.DataAccessLibrary;
using VocabularyReminder.Services;

namespace VocabularyReminder
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
        }

        private bool _isHotKeyRegister = false;
        private List<Vocabulary> _vocabularies = new List<Vocabulary>();

        private void RegisterHotKeys()
        {
            if (_isHotKeyRegister) return;
            
            _isHotKeyRegister = true;
            _source.AddHook(HwndHook);

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 1, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.F1.GetHashCode());  // Show Current Toast

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 2, (int)KeyModifier.Shift, (uint)System.Windows.Forms.Keys.F1.GetHashCode());  // Toggle Start

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 3, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.F8.GetHashCode());      // Play Sound 1

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 4, (int)KeyModifier.Shift, (uint)System.Windows.Forms.Keys.F8.GetHashCode());      // Play Sound 2

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 5, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.PrintScreen.GetHashCode());  // Delete

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 6, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.Scroll.GetHashCode());  // Next

            _ = RegisterHotKey(_windowHandle, HOTKEY_ID + 7, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.Pause.GetHashCode());  // Next and Delete
        }

        private void UnRegisterHotKeys()
        {
            if (!_isHotKeyRegister) return;

            _isHotKeyRegister = false;
            _source.RemoveHook(HwndHook);
            for (int i = HOTKEY_ID; i <= HOTKEY_ID + 7; i++)
            {
                UnregisterHotKey(_windowHandle, i);
            }
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
                        case HOTKEY_ID + 3:
                            App.LastReaction = DateTime.Now;
                            _ = BackgroundService.ActionPlay(1);
                            handled = true;
                            break;
                        case HOTKEY_ID + 4:
                            App.LastReaction = DateTime.Now;
                            _ = BackgroundService.ActionPlay(2);
                            handled = true;
                            break;
                        case HOTKEY_ID + 5:
                            App.LastReaction = DateTime.Now;
                            _ = BackgroundService.DeleteVocabularyAsync();
                            handled = true;
                            break;
                        case HOTKEY_ID + 6:
                            App.LastReaction = DateTime.Now;
                            _ = BackgroundService.NextVocabulary();
                            handled = true;
                            break;
                        case HOTKEY_ID + 7:
                            App.LastReaction = DateTime.Now;
                            _ = BackgroundService.NextAndDeleteVocabulary();
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

        const string placeHolder = "Enter your vocabulary list here.... \nThen click \"Import\" to auto get content.";

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
            this.Inp_UseCustomPopup.Checked += Settings_Changed;
            this.Inp_UseCustomPopup.Unchecked += Settings_Changed;
            this.Inp_TimeRepeat.TextChanged += Settings_Changed;

            Load_Dictionaries();
            Status_Reset();
        }

        private void Load_Dictionaries()
        {
            Dispatcher.Invoke(() =>
            {
                List<Dictionary> dictionaries = DataAccess.GetDictionariesAsync().Result;
                this.Inp_GlobalDictionaryId.ItemsSource = dictionaries;
                
                string settingsPath = ApplicationIO.GetSettingsPath();
                if (File.Exists(settingsPath))
                {
                    try
                    {
                        var json = File.ReadAllText(settingsPath);
                        var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                        
                        // Load dictionary ID
                        if (settings.ContainsKey("lastDictionaryId"))
                        {
                            int lastId = ((JsonElement)settings["lastDictionaryId"]).GetInt32();
                            if (dictionaries.Any(d => d.Id == lastId))
                            {
                                this.Inp_GlobalDictionaryId.SelectedValue = lastId;
                                
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
                                if (settings.ContainsKey("isUseCustomPopup"))
                                {
                                    Inp_UseCustomPopup.IsChecked = ((JsonElement)settings["isUseCustomPopup"]).GetBoolean();
                                }

                                App.isRandomWords = Inp_RandomOption.IsChecked.GetValueOrDefault();
                                App.isAutoPlaySounds = Inp_AutoPlayOption.IsChecked.GetValueOrDefault();
                                App.isUseCustomPopup = Inp_UseCustomPopup.IsChecked.GetValueOrDefault();
                                
                                return;
                            }
                        }
                    }
                    catch { }
                }
                
                // Default to last dictionary if no saved setting
                this.Inp_GlobalDictionaryId.SelectedIndex = dictionaries.Max(e => e.Id) - 1;
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

        private List<string> GetListWords() {

            string tempInp = Inp_ListWord.Text;
            if (tempInp == placeHolder)
            {
                MessageBox.Show("You need to enter vocabulary words before Import...");
                return default;
            }

            var (dictionary, maxWordLength) = StaticDataAccess.ReadDictionaryCSV(ApplicationIO.GetDictionaryCSV());

            tempInp = CleanParagraph(tempInp);

            var ListWord = ParseParagraph(tempInp, dictionary, maxWordLength);
            ListWord.RemoveAll(x => string.IsNullOrEmpty(x));

            return ListWord;
        }

        private string CleanParagraph(string paragraph)
        {
            return paragraph.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Replace("  ", " ").Trim();
        }

        private List<string> ParseParagraph(string paragraph, HashSet<string> dictionary, int maxWordLength)
        {
            var wordSet = new HashSet<string>();
            var wordArray = paragraph.Split(new char[] { ' ', ',', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;

            while (i < wordArray.Length)
            {
                bool foundCompound = false;
                for (int length = maxWordLength; length > 0; length--)
                {
                    if (i + length <= wordArray.Length)
                    {
                        string word = string.Join(" ", wordArray.Skip(i).Take(length)).ToLower().Trim();
                        if (dictionary.Contains(word))
                        {
                            wordSet.Add(word);
                            i += length;
                            foundCompound = true;
                            break;
                        }
                    }
                }

                if (!foundCompound)
                {
                    wordSet.Add(wordArray[i].ToLower().Trim());
                    i++;
                }
            }

            return wordSet.ToList();
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
                    var _item = await DataAccess.GetVocabularyByWordAsync(word);
                    if (_item != null && _item.Id > 0)
                        existWords.Add(_item);
                    else
                        newWords.Add(word);
                }

                foreach (var word in existWords.Where(x => x.Status == 1))
                {
                    await DataAccess.AddVocabularyMappingAsync(dicId, word.Id);
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
                    Parallel.ForEach(newWords, parallelOptions, async _item =>
                    {
                        var newVocaId = await DataAccess.AddVocabularyAsync(_item);
                        if (newVocaId > 0)
                        {
                            await DataAccess.AddVocabularyMappingAsync(dicId, newVocaId);
                            CountSuccess++;
                        }
                        Status_UpdateProgressBar(++Count, TotalWords);
                    });

                    BackgroundCrawl().Wait();

                    Status_UpdateMessage("Imported Success " + CountSuccess + "/" + Count + " entered vocabulary.");
                    Reload_Stats();
                    Dispatcher.Invoke(() => Btn_Import.IsEnabled = true);
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
            await Task.Run(() =>
            {
                Status_UpdateMessage("[1/4] Start Getting Translate...");
                ProcessBackgroundTranslate();
                Status_UpdateMessage("[1/4] Finished Getting Translate.");
            });   // wait to process all

            await Task.Run(() =>
            {
                Status_UpdateMessage("[2/4] Start Getting Vocabulary Information: Define, Example, Ipa...");
                ProcessBackgroundGetWordDefineInformation();
                Status_UpdateMessage("[2/4] Finished Getting Vocabulary Information: Define, Example, Ipa.");
            });   // wait to process all

            await Task.Run(() =>
            {
                Status_UpdateMessage("[3/4] Start Getting Related Words...");
                ProcessBackgroundGetRelatedWords();
                Status_UpdateMessage("[3/4] Finished Getting Related Words.");
            });   // wait to process all

            await Task.Run(() =>
            {
                Status_UpdateMessage("[4/4] Start Getting from local dictionary for unprocess Words...");
                ProcessBackgroundUnprocessWords();
                Status_UpdateMessage("[4/4] Finished Getting from local dictionary for unprocess Words.");
            });   // wait to process all

            Status_UpdateMessage("All of Crawling Finished. Enjoy the Learning Journey Now!.");
        }

        private void Btn_ProcessDeleteData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Status_UpdateMessage("Start Deleting...");

                var directoryMp3 = Directory.CreateDirectory(ApplicationIO.GetApplicationFolderPath());
                foreach (var d in directoryMp3.EnumerateDirectories())
                {
                    d.Delete(true);
                }

                Status_UpdateMessage("Deleted Mp3 and Images Folder");

                if (File.Exists(ApplicationIO.GetDatabasePath()))
                {
                    File.Delete(ApplicationIO.GetDatabasePath());
                }

                if (!File.Exists(ApplicationIO.GetDatabasePath()))
                {
                    var file = File.Create(ApplicationIO.GetDatabasePath());
                    file.Close();
                }
                DataAccess.InitializeDatabase();
                App.GlobalWordId = 0;
                Status_UpdateMessage("Deleted Mp3, Images and Database Success.");
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


        public async void ProcessBackgroundTranslate()
        {
            try
            {
                var ListVocabulary = await DataAccess.GetListVocabularyToTranslateAsync();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                var service = PluralizationService.CreateService(new System.Globalization.CultureInfo("en-US"));

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * CoreMultipleThread;
                Parallel.ForEach(ListVocabulary, parallelOptions, async _item =>
                {
                    var voca = await TranslateService.GetVocabularyTranslate(_item);
                    if (string.IsNullOrEmpty(voca.Translate))
                    {
                        if (service.IsPlural(_item.Word))
                        {
                            _item.Word = service.Singularize(_item.Word);
                            await TranslateService.GetVocabularyTranslate(_item);
                        }
                    }

                    Status_UpdateProgressBar(++Count, TotalItems);
                });
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Crawling: Process Background Translate Failed: " + ex.Message);
            }
        }

        public async void ProcessBackgroundGetWordDefineInformation()
        {
            try
            {
                var ListVocabulary = await DataAccess.GetListVocabularyToGetDefineExampleMp3URLAsync();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * CoreMultipleThread;
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    TranslateService.GetWordDefineInformation(_item).Wait();
                    Status_UpdateProgressBar(++Count, TotalItems);
                });
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Crawling: Process Background Get English Define, Ipa, Type, Example, MP3 URL Fail: " + ex.Message);
            }

        }

        public async void ProcessBackgroundGetRelatedWords()
        {
            try
            {
                var ListVocabulary = await DataAccess.GetListVocabularyToGetRelatedWordsAsync();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * CoreMultipleThread;
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    TranslateService.GetRelatedWord(_item).Wait();
                    Status_UpdateProgressBar(++Count, TotalItems);
                });
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Crawling: Process Background Get Related Words Fail: " + ex.Message);
            }
        }

        public async void ProcessBackgroundUnprocessWords()
        {
            try
            {
                var listVocabulary = await DataAccess.GetUnprocessVocabulariesAsync();
                if (!listVocabulary.Any())
                    return;

                var evDic = await DataAccess.GetEVVocabulariesAsync();

                int TotalItems = listVocabulary.Count;
                int Count = 0;

                var service = PluralizationService.CreateService(new System.Globalization.CultureInfo("en-US"));

                foreach (var item in listVocabulary)
                {
                    var exist = evDic.FirstOrDefault(e => e.Word.Equals(item.Word, StringComparison.OrdinalIgnoreCase));
                    if (exist == null)
                    {
                        if (service.IsPlural(item.Word))
                        {
                            item.Word = service.Singularize(item.Word);
                            exist = evDic.FirstOrDefault(e => e.Word.Equals(item.Word, StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    if (exist != null)
                    {
                        if (string.IsNullOrEmpty(item.Ipa)) item.Ipa = exist.Pronounce;
                        if (string.IsNullOrEmpty(item.Translate)) item.Ipa = exist.Description;
                        await DataAccess.UpdateVocabularyAsync(item);
                    }

                    Status_UpdateProgressBar(++Count, TotalItems);
                }
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Crawling: Process Background Unprocess words Fail: " + ex.Message);
            }
        }

        private void Reload_Stats()
        {
            Dispatcher.Invoke(() =>
            {
                if (IsActive)
                {
                    var dictionaryId = (int)this.Inp_GlobalDictionaryId.SelectedValue;
                    Stats _Stats = DataAccess.GetStats(dictionaryId);
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
                RegisterHotKeys();

                IsStarted = true;
                App.GlobalDicId = (int)Inp_GlobalDictionaryId.SelectedValue;
                App.isRandomWords = Inp_RandomOption.IsChecked.GetValueOrDefault();
                App.isAutoPlaySounds = Inp_AutoPlayOption.IsChecked.GetValueOrDefault();
                App.isUseCustomPopup = Inp_UseCustomPopup.IsChecked.GetValueOrDefault();

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

                          //VocabularyToast.ClearApplicationToast();
                          VocabularyDisplay.Hide();
                          await LoadVocabulary();

                          if (_CancelToken.IsCancellationRequested)
                          {
                              Console.WriteLine("task canceled");
                              //VocabularyToast.ClearApplicationToast();
                              VocabularyDisplay.Hide();
                              break;
                          }
                          Thread.Sleep(TimeRepeat);
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

            VocabularyDisplay.Hide();
            _TokenSource.Cancel();
            UnRegisterHotKeys();
            Console.WriteLine("Stop and active Cancel Token");
        }

        public async Task LoadVocabulary()
        {
            Vocabulary _item = null;
            var vocabulary = await GetVocabulary(_item);
            VocabularyDisplay.ShowVocabulary(vocabulary);
            //VocabularyToast.ShowToastByVocabularyItem(vocabulary);
            if (App.isAutoPlaySounds)
            {
                _ = Task.Run(async () =>
                {
                    await Mp3.PlayFile(vocabulary);
                    await DataAccess.UpdateViewDateAsync(vocabulary?.Id ?? 0);
                });
            }

            App.GlobalWordId = vocabulary?.Id ?? 0;
        }

        private async Task<Vocabulary> GetVocabulary(Vocabulary _item = null)
        {
            if (_vocabularies.Any())
                return GetVocabularyFromExistList(_item);
            else
                return await GetVocabularyFromDatabase(_item);
        }

        private async Task<Vocabulary> GetVocabularyFromDatabase(Vocabulary _item = null)
        {
            if (_item != null) return _item;
            if (App.isRandomWords)
                _item = await DataAccess.GetRandomVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
            else
                _item = await DataAccess.GetNextVocabularyAsync(App.GlobalDicId, App.GlobalWordId);

            if (_item == null || _item.Id == 0)
                _item = await DataAccess.GetFirstVocabularyAsync(App.GlobalDicId);

            return _item;
        }

        private Vocabulary GetVocabularyFromExistList(Vocabulary _item = null)
        {
            if (_item != null) return _item;
            if (App.isRandomWords)
            {
                var random = new Random();
                var index = random.Next(_vocabularies.Count);
                _item = _vocabularies.ElementAt(index);
            }
            else
            {
                var index = _vocabularies.IndexOf(_item);
                index += 1;
                if (index >= _vocabularies.Count) index = 0;
                _item = _vocabularies.ElementAt(index);
            }

            if (_item == null || _item.Id == 0)
            {
                _item = _vocabularies.FirstOrDefault();
            }

            return _item;
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
                var ListVocabulary = await DataAccess.GetListVocabularyToPreloadMp3Async();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = (int)Environment.ProcessorCount * CoreMultipleThread;    
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    Mp3.preloadMp3MultipleAsync(_item).Wait();
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
            Status_UpdateMessage("Downloading 3000 common words....");
            var ImportService = new Import();
            await ImportService.ImportDemo3000Words();
            Reload_Stats();
            Status_UpdateMessage("Downloaded 3000 common words success.");
            MessageBox.Show("Downloaded 3000 common words success.");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_TokenSource != null) _TokenSource.Cancel();
            VocabularyDisplay.Hide();
            UnRegisterHotKeys();
            base.OnClosed(e);
        }

        private void Inp_GlobalDictionaryId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Inp_GlobalDictionaryId.SelectedValue == null) return;
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
            settings["isUseCustomPopup"] = Inp_UseCustomPopup.IsChecked;
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
                var _item = await DataAccess.GetVocabularyByWordAsync(word);
                if (_item != null)
                    _vocabularies.Add(_item);
            }

            ToggleLearning();
        }

        private void Btn_Backup_Click(object sender, RoutedEventArgs e)
        {
            var ImportService = new Import();
            var backupName = ImportService.Backup();

            MessageBox.Show($"Backup completed: {backupName}.");
        }

        private async void Btn_Sync_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundCrawl();
        }

        private async void Btn_Cleanup_Click(object sender, RoutedEventArgs e)
        {
            await DataAccess.CleanUnableToGetAsync();
        }

        private void Btn_ManageDictionary_Click(object sender, RoutedEventArgs e)
        {
            var window = new DictionaryManagementWindow();
            window.Closed += (s, args) => Load_Dictionaries();
            window.Show();
        }
    }
}
