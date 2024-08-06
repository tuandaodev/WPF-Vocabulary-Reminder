using DesktopNotifications.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
                            //if (App.isShowPopup)
                            //{
                            //    App.isShowPopup = false;
                            //    BackgroundService.HideToast();
                            //    handled = true;
                            //}
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

        //private IntPtr HwndHook2(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //{
        //    const int WM_HOTKEY = 0x0312;
        //    switch (msg)
        //    {
        //        case WM_HOTKEY:
        //            switch (wParam.ToInt32())
        //            {
        //                case HOTKEY_ID+1:
        //                    int vkey = (((int)lParam >> 16) & 0xFFFF);
        //                    // Detect keys and modifier
        //                    //System.Windows.Forms.Keys key = ((System.Windows.Forms.Keys)(((int)lParam) >> 0x10)) & System.Windows.Forms.Keys.KeyCode;
        //                    ModifierKeys modifier = ((ModifierKeys)((int)lParam)) & ((ModifierKeys)0xffff);

        //                    if (vkey == System.Windows.Forms.Keys.F7.GetHashCode()) // Play Sound 1
        //                    {
        //                        //Console.WriteLine("play 1");
        //                        BackgroundService.ActionPlay(1);
        //                    } else if (vkey == System.Windows.Forms.Keys.F8.GetHashCode())  // Play Sound 2
        //                    {
        //                        //Console.WriteLine("play 2");
        //                        BackgroundService.ActionPlay(2);
        //                    } else if (vkey == System.Windows.Forms.Keys.Scroll.GetHashCode())  // Next
        //                    {
        //                        //Console.WriteLine("next");
        //                        BackgroundService.NextVocabulary();
        //                    }
        //                    else if (vkey == System.Windows.Forms.Keys.Pause.GetHashCode())  // Next and Delete
        //                    {
        //                        //Console.WriteLine("next and delete");
        //                        BackgroundService.NextAndDeleteVocabulary();
        //                    }
        //                    else if (modifier == ModifierKeys.Shift && vkey == System.Windows.Forms.Keys.Escape.GetHashCode())  // Show Current
        //                    {
        //                        //Console.WriteLine("show current toast");
        //                        BackgroundService.showCurrentToast();
        //                    }
        //                    else if (vkey == System.Windows.Forms.Keys.Escape.GetHashCode())  // Close Toast
        //                    {
        //                        //Console.WriteLine("ESC Close toast");
        //                        BackgroundService.HideToast();
        //                    }
        //                    else if (vkey == System.Windows.Forms.Keys.PrintScreen.GetHashCode()) // Delete
        //                    {
        //                        //Console.WriteLine("delete current toast");
        //                        BackgroundService.DeleteVocabulary();
        //                    }
        //                    handled = true;
        //                    break;
        //            }
        //            break;
        //    }
        //    return IntPtr.Zero;
        //}

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

            App.isRandomWords = Inp_RandomOption.IsChecked.GetValueOrDefault();
            App.isAutoPlaySounds = Inp_AutoPlayOption.IsChecked.GetValueOrDefault();

            Load_Dictionaries();
            Status_Reset();
            // IMPORTANT: Look at App.xaml.cs for required registration and activation steps

        }

        private void Load_Dictionaries()
        {
            Dispatcher.Invoke(() =>
            {
                List<Dictionary> dictionaries = DataAccess.GetDictionariesAsync().Result;
                this.Inp_GlobalDictionaryId.ItemsSource = dictionaries;
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

            //var ListWord = Regex.Split(tempInp, "\r\n|\r|\n").ToList();

            // Read the dictionary CSV file
            var (dictionary, maxWordLength) = StaticDataAccess.ReadDictionaryCSV(ApplicationIO.GetDictionaryCSV());

            tempInp = CleanParagraph(tempInp);

            // Parse the paragraph into words using the dictionary
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

                // Only add existing not-learning words
                foreach (var word in existWords.Where(x => x.Status == 1))
                {
                    await DataAccess.AddVocabularyMappingAsync(dicId, word.Id);
                }

                if (!newWords.Any())
                {
                    MessageBox.Show("All vocabulary already in database, please Start to show in this list only");
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
                        MaxDegreeOfParallelism = Environment.ProcessorCount * CoreMultipleThread
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
                Status_UpdateMessage("[1/3] Start Get Translate...");
                ProcessBackgroundTranslate();
                Status_UpdateMessage("[1/3] Finished Get Translate.");
            });   // wait to process all

            await Task.Run(() =>
            {
                Status_UpdateMessage("[2/3] Start Get Vocabulary Information: Define, Example, Ipa...");
                ProcessBackgroundGetWordDefineInformation();
                Status_UpdateMessage("[2/3] Finished Get Vocabulary Information: Define, Example, Ipa.");
            });   // wait to process all

            await Task.Run(() =>
            {
                Status_UpdateMessage("[3/3] Start Get Related Words...");
                ProcessBackgroundGetRelatedWords();
                Status_UpdateMessage("[3/3] Finished Get Related Words.");
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

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * CoreMultipleThread;
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    TranslateService.GetVocabularyTranslate(_item).Wait();
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

        private void Reload_Stats()
        {
            Dispatcher.Invoke(() =>
            {
                if (IsActive)
                {
                    Stats _Stats = DataAccess.GetStats();
                    this.Label_Stats_ImportedWords.Content = _Stats.Total.ToString();
                    this.Label_Stats_RememberedWords.Content = _Stats.Remembered.ToString();
                }
            });
        }


        private void Btn_StartLearning_Click(object sender, RoutedEventArgs e)
        {
            //Popup codePopup = new Popup();
            //TextBlock popupText = new TextBlock();
            //popupText.Text = "Popup Text";
            //popupText.Background = Brushes.LightBlue;
            //popupText.Foreground = Brushes.Blue;
            //codePopup.Child = popupText;

            //codePopup.IsOpen = true;
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

                          VocabularyToast.ClearApplicationToast();
                          await LoadVocabulary();

                          if (_CancelToken.IsCancellationRequested)
                          {
                              Console.WriteLine("task canceled");
                              VocabularyToast.ClearApplicationToast();
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

            _TokenSource.Cancel();
            UnRegisterHotKeys();
            Console.WriteLine("Stop and active Cancel Token");
        }

        public async Task LoadVocabulary()
        {
            Vocabulary _item = null;
            var vocabulary = await GetVocabulary(_item);
            VocabularyToast.ShowToastByVocabularyItem(vocabulary);
            if (App.isAutoPlaySounds)
            {
                _ = Task.Run(() => Mp3.PlayFile(vocabulary));
            }

            App.GlobalWordId = vocabulary.Id;
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
            // Use SelectionStart property to find the caret position.
            // Insert the previewed text into the existing text in the textbox.
            var fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            double val;
            // If parsing is successful, set Handled to false
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
                parallelOptions.MaxDegreeOfParallelism = (int)Environment.ProcessorCount * CoreMultipleThread;    // TODO
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
            VocabularyToast.ClearApplicationToast();
            UnRegisterHotKeys();
            base.OnClosed(e);
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
    }
}
