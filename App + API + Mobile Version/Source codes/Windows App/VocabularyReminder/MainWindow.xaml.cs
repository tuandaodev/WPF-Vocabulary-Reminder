using DesktopNotifications.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
            _source.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID + 1, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.F1.GetHashCode());  // Show Current Toast

            RegisterHotKey(_windowHandle, HOTKEY_ID + 2, (int)KeyModifier.Shift, (uint)System.Windows.Forms.Keys.Escape.GetHashCode());  // Hide Toast

            RegisterHotKey(_windowHandle, HOTKEY_ID + 3, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.F7.GetHashCode());      // Play Sound 1

            RegisterHotKey(_windowHandle, HOTKEY_ID + 4, (int)KeyModifier.None, (uint)System.Windows.Forms.Keys.F8.GetHashCode());      // Play Sound 2

            RegisterHotKey(_windowHandle, HOTKEY_ID + 5, (int)KeyModifier.Shift, (uint)System.Windows.Forms.Keys.PrintScreen.GetHashCode());  // Delete

            RegisterHotKey(_windowHandle, HOTKEY_ID + 6, (int)KeyModifier.Shift, (uint)System.Windows.Forms.Keys.Scroll.GetHashCode());  // Next
            RegisterHotKey(_windowHandle, HOTKEY_ID + 6, (int)KeyModifier.Shift, (uint)System.Windows.Forms.Keys.F1.GetHashCode());  // Next

            RegisterHotKey(_windowHandle, HOTKEY_ID + 7, (int)KeyModifier.Shift, (uint)System.Windows.Forms.Keys.Pause.GetHashCode());  // Next and Delete
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
                            BackgroundService.showCurrentToastAsync().Wait();
                            handled = true;
                            break;
                        case HOTKEY_ID + 2:
                            if (App.isShowPopup)
                            {
                                App.isShowPopup = false;
                                BackgroundService.HideToast();
                                handled = true;
                            }
                            break;
                        case HOTKEY_ID + 3:
                            BackgroundService.ActionPlayAsync(1).Wait();
                            handled = true;
                            break;
                        case HOTKEY_ID + 4:
                            BackgroundService.ActionPlayAsync(2).Wait();
                            handled = true;
                            break;
                        case HOTKEY_ID + 5:
                            BackgroundService.DeleteVocabularyAsync().Wait();
                            handled = true;
                            break;
                        case HOTKEY_ID + 6:
                            BackgroundService.NextVocabularyAsync().Wait();
                            handled = true;
                            break;
                        case HOTKEY_ID + 7:
                            BackgroundService.NextAndDeleteVocabularyAsync().Wait();
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

            App.isRandomWords = (Inp_RandomOption.IsChecked == true);
            App.isAdvancedMode = (Inp_AdvancedMode.IsChecked == true);

            Status_Reset();

            labelHiAccount.Content = "Hi, " + App.User.UserName;
            // IMPORTANT: Look at App.xaml.cs for required registration and activation steps

        }

        private void Status_Reset()
        {
            Dispatcher.Invoke(() =>
            {
                this.Status_Message.Text = String.Empty;
                Status_UpdateProgressBar();
            });
        }

        public void Status_UpdateMessage(string _message)
        {
            Dispatcher.Invoke(() =>
            {
                this.Status_Message.Text = _message;
            });
        }

        public void Status_UpdateProgressBar(int value = 0, int max = 100)
        {
            Dispatcher.Invoke(() =>
            {
                this.Status_ProgessBar.Value = value;
                this.Status_ProgessBar.Maximum = max;
            });
        }

        private void Btn_Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tempInp = this.Inp_ListWord.Text;
                if (tempInp == placeHolder)
                {
                    MessageBox.Show("You need to enter vocabulary words before Import...");
                    return;
                }

                List<string> ListWord = Regex.Split(tempInp, "\r\n|\r|\n").ToList();
                ListWord.RemoveAll(x => string.IsNullOrEmpty(x));
                int TotalWords = ListWord.Count;

                Dispatcher.Invoke(() => this.Btn_Import.IsEnabled = false);

                Task.Factory.StartNew(async () =>
                {
                    Status_UpdateMessage("Start Importing...");
                    int Count = 0;
                    int CountSuccess = 0;
                    int MaxVocabularyMaxIndex = await DataAccess.GetVocabularyMaxIndexAsync();

                    ParallelOptions parallelOptions = new ParallelOptions();
                    parallelOptions.MaxDegreeOfParallelism = (int)(Environment.ProcessorCount / 2);
                    Parallel.ForEach(ListWord, parallelOptions, (_item, state, idx) =>
                    {
                        int newIndex = MaxVocabularyMaxIndex + (int)idx + 1;
                        if (DataAccess.AddVocabularyAsync(_item, newIndex).Result > 0)
                        {
                            CountSuccess++;
                        }
                        Status_UpdateProgressBar(++Count, TotalWords);
                    });

                    await BackgroundCrawl();

                    Status_UpdateMessage("Imported Success " + CountSuccess + "/" + Count + " entered vocabulary.");
                    Reload_Stats();
                    Dispatcher.Invoke(() => this.Btn_Import.IsEnabled = true);
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
                ProcessBackgroundTranslateAsync().Wait();
                Status_UpdateMessage("[1/3] Finished Get Translate.");
            });   // wait to process all

            await Task.Run(() =>
            {
                Status_UpdateMessage("[2/3] Start Get Vocabulary Information: Define, Example, Ipa...");
                ProcessBackgroundGetWordDefineInformationAsync().Wait();
                Status_UpdateMessage("[2/3] Finished Get Vocabulary Information: Define, Example, Ipa.");
            });   // wait to process all

            await Task.Run(() =>
            {
                Status_UpdateMessage("[3/3] Start Get Related Words...");
                ProcessBackgroundGetRelatedWordsAsync().Wait();
                Status_UpdateMessage("[3/3] Finished Get Related Words.");
            });   // wait to process all

            Status_UpdateMessage("All of Crawling Finished. Enjoy the Learning Journey Now!.");
        }

        private void Btn_ProcessDeleteData_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure to delete all data?", "Confirm Delete", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    Task.Run(async () =>
                    {
                        await ProcessDeleteDataAsync();
                    });
                    break;
            }
        }


        private async Task ProcessDeleteDataAsync()
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

                await DataAccess.ClearAllVocabularyAsync();
                
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

        public async Task ProcessBackgroundTranslateAsync()
        {
            try
            {
                var ListVocabulary = await DataAccess.GetListVocabularyToTranslateAsync();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = (int)(Environment.ProcessorCount / 2);
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    try
                    {
                        TranslateService.GetVocabularyTranslate(_item).Wait();
                        Status_UpdateProgressBar(++Count, TotalItems);
                    } catch (Exception) { }
                });
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Crawling: Process Background Translate Failed: " + ex.Message);
            }
        }

        public async Task ProcessBackgroundGetWordDefineInformationAsync()
        {
            try
            {
                var ListVocabulary = await DataAccess.GetListVocabularyToGetDefineExampleMp3URLAsync();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = (int)(Environment.ProcessorCount / 2);
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    try
                    {
                        TranslateService.GetWordDefineInformation(_item).Wait();
                        Status_UpdateProgressBar(++Count, TotalItems);
                    } catch (Exception) { }
                });
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Crawling: Process Background Get English Define, Ipa, Type, Example, MP3 URL Fail: " + ex.Message);
            }

        }

        public async Task ProcessBackgroundGetRelatedWordsAsync()
        {
            try
            {
                var ListVocabulary = await DataAccess.GetListVocabularyToGetRelatedWordsAsync();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = (int)(Environment.ProcessorCount / 2);
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    try
                    {
                        TranslateService.GetRelatedWord(_item).Wait();
                        Status_UpdateProgressBar(++Count, TotalItems);
                    } catch (Exception) { }
                });
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Crawling: Process Background Get Related Words Fail: " + ex.Message);
            }
        }

        private void Reload_Stats()
        {
            Dispatcher.Invoke(async () =>
            {
                if (this.IsActive)
                {
                    Stats _Stats = await DataAccess.GetStatsAsync();
                    this.Label_Stats_ImportedWords.Content = _Stats.Total.ToString();
                    this.Label_Stats_RememberedWords.Content = _Stats.Remembered.ToString();
                }
            });
        }


        private void Btn_StartLearning_Click(object sender, RoutedEventArgs e)
        {
            if (!IsStarted)
            {
                IsStarted = true;
                App.isRandomWords = (Inp_RandomOption.IsChecked == true);
                App.isAdvancedMode = (Inp_AdvancedMode.IsChecked == true);

                this.Btn_StartLearning.Content = "Stop Learning";
                // Init value
                _TokenSource = new CancellationTokenSource();
                _CancelToken = _TokenSource.Token;

                int TimeRepeat;
                int.TryParse(this.Inp_TimeRepeat.Text, out TimeRepeat);

                if (TimeRepeat < 0) { TimeRepeat = 1; };
                TimeRepeat = TimeRepeat * 1000;

                App.LastReaction = new DateTime();

                Task.Factory.StartNew(async () =>
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

                        await LoadVocabularyAsync();

                        if (_CancelToken.IsCancellationRequested)
                        {
                            Console.WriteLine("task canceled");
                            VocabularyToast.ClearApplicationToast();
                            break;
                        }
                        Thread.Sleep(TimeRepeat);
                    }
                }, _CancelToken);

                this.WindowState = System.Windows.WindowState.Minimized;
            }
            else
            {
                IsStarted = false;
                this.Btn_StartLearning.Content = "Start Learning";

                _TokenSource.Cancel();
                Console.WriteLine("Stop and active Cancel Token");
            }
        }

        public void StopLearning()
        {
            IsStarted = false;
            this.Btn_StartLearning.Content = "Start Learning";
            _TokenSource.Cancel();
        }

        public async Task LoadVocabularyAsync()
        {
            Vocabulary _item;
            if (App.isRandomWords)
            {
                _item = await DataAccess.GetRandomVocabularyAsync(App.GlobalWordId);
            }
            else
            {
                _item = await DataAccess.GetNextVocabularyAsync(App.GlobalWordId);
            }

            // Điều kiện bù
            if (_item.Id == 0)
            {
                _item = await DataAccess.GetFirstVocabularyAsync();
            }
            VocabularyToast.ShowToastByVocabularyItem(_item);
            App.GlobalWordId = _item.Id;
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
                ProcessBackgroundDownloadMp3Async().Wait();
                Dispatcher.Invoke(() => this.Btn_PreloadMp3.IsEnabled = true);
            });
        }

        private async Task ProcessBackgroundDownloadMp3Async()
        {
            try
            {
                var ListVocabulary = await DataAccess.GetListVocabularyToPreloadMp3Async();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = (int)(Environment.ProcessorCount / 2);
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

        private void Btn_Import_Auto_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure to import 3000 common words? It will overwrite all of current data", "Confirm Import", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    Dispatcher.Invoke(() => this.Btn_Import_Auto.IsEnabled = false);
                    Task.Run(() =>
                    {
                        ProcessImportAsync().Wait();
                        Dispatcher.Invoke(() => this.Btn_Import_Auto.IsEnabled = true);
                    });
                    
                    break;
            }
        }

        private async Task ProcessImportAsync()
        {
            Status_UpdateMessage("Downloading 3000 common words....");
            var ImportService = new Import();

            await DataAccess.Import3000WordsAsync();

            //ImportService.ImportDemo3000Words();
            Reload_Stats();
            Status_UpdateMessage("Downloaded 3000 common words success.");
            MessageBox.Show("Downloaded 3000 common words success.");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_TokenSource != null) _TokenSource.Cancel();
            VocabularyToast.ClearApplicationToast();
            _source.RemoveHook(HwndHook);
            for (int i = HOTKEY_ID; i <= HOTKEY_ID + 7; i++)
            {
                UnregisterHotKey(_windowHandle, i);
            }
            base.OnClosed(e);
        }

        private void Frm_MainWindow_Activated(object sender, EventArgs e)
        {
            Reload_Stats();
        }


        private void Btn_ShowLearnedList_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var frm = new LearnedWordsWindow();
                frm.Show();
            });
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

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var frm = new Login();
            frm.Show();

            Close();
            App.User = null;
        }
    }
}
