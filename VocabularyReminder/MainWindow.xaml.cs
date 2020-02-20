// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using DesktopNotifications;
using DesktopNotifications.Services;
using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VocabularyReminder.DataAccessLibrary;
using VocabularyReminder.Services;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace VocabularyReminder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CancellationTokenSource _TokenSource;
        CancellationToken _CancelToken;

        private bool IsStarted = false;
        

        private static int Core = 3;

        const string placeHolder = "Enter your vocabulary list here....";

        public MainWindow()
        {
            InitializeComponent();
            this.Inp_ListWord.Text = placeHolder;

            App.isRandomWords = (Inp_RandomOption.IsChecked == true);

            Status_Reset();
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
                var ListWord = Regex.Split(tempInp, "\r\n|\r|\n").ToList();
                ListWord.RemoveAll(x => string.IsNullOrEmpty(x));
                int TotalWords = ListWord.Count;

                Dispatcher.Invoke(() => this.Btn_Import.IsEnabled = false);

                Task.Factory.StartNew(async () =>
                {
                    Status_UpdateMessage("Start Importing...");
                    int Count = 0;
                    int CountSuccess = 0;

                    ParallelOptions parallelOptions = new ParallelOptions();
                    parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * Core;
                    Parallel.ForEach(ListWord, parallelOptions, _item =>
                    {
                        if (DataAccess.AddVocabulary(_item) > 0)
                        {
                            CountSuccess++;
                        }
                        Status_UpdateProgressBar(++Count, TotalWords);
                    });

                    BackgroundCrawl().Wait();

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


        public void ProcessBackgroundTranslate()
        {
            try
            {
                var ListVocabulary = DataAccess.GetListVocabularyToTranslate();
                
                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * Core;
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

        public void ProcessBackgroundGetWordDefineInformation()
        {
            try
            {
                var ListVocabulary = DataAccess.GetListVocabularyToGetDefineExampleMp3URL();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * Core;
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

        public void ProcessBackgroundGetRelatedWords()
        {
            try
            {
                var ListVocabulary = DataAccess.GetListVocabularyToGetRelatedWords();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * Core;
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
                if (this.IsActive)
                {
                    Stats _Stats = DataAccess.GetStats();
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
                this.Btn_StartLearning.Content = "Stop Learning";
                // Init value
                _TokenSource = new CancellationTokenSource();
                _CancelToken = _TokenSource.Token;

                int TimeRepeat;
                int.TryParse(this.Inp_TimeRepeat.Text, out TimeRepeat);

                if (TimeRepeat < 0) { TimeRepeat = 1; };
                TimeRepeat = TimeRepeat * 1000;

                App.LastReaction = new DateTime();

                Task.Factory.StartNew(() =>
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
                        
                        LoadVocabulary();

                        if (_CancelToken.IsCancellationRequested)
                        {
                            Console.WriteLine("task canceled");
                            VocabularyToast.ClearApplicationToast();
                            break;
                        }
                        Thread.Sleep(TimeRepeat);
                    }
                }, _CancelToken);
            } else
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

        public void LoadVocabulary()
        {
            Vocabulary _item;
            if (App.isRandomWords)
            {
                _item = DataAccess.GetRandomVocabulary(App.GlobalWordId);
            } else
            {
                _item = DataAccess.GetNextVocabulary(App.GlobalWordId);
            }
            
            if (_item.Id == 0)
            {
                _item = DataAccess.GetFirstVocabulary();
            }
            VocabularyToast.showToastByVocabularyItem(_item);
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
            
            Dispatcher.Invoke(() => this.Btn_PreloadMp3.IsEnabled = false );
            Task.Run(() => {
                ProcessBackgroundDownloadMp3();
                Dispatcher.Invoke(() => this.Btn_PreloadMp3.IsEnabled = true);
            });
        }

        private void ProcessBackgroundDownloadMp3()
        {
            try
            {
                var ListVocabulary = DataAccess.GetListVocabularyToPreloadMp3();

                int TotalItems = ListVocabulary.Count;
                int Count = 0;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = (int)Environment.ProcessorCount * Core;    // TODO
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
    }
}
