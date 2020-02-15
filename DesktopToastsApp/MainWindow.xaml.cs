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

using DataAccessLibrary;
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
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace DesktopToastsApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        public MainWindow()
        {
            InitializeComponent();

            // IMPORTANT: Look at App.xaml.cs for required registration and activation steps
        }

        
        internal void ShowConversation()
        {
            //ContentBody.Content = new TextBlock()
            //{
            //    Text = "You've just opened a conversation!",
            //    FontWeight = FontWeights.Bold
            //};
        }

        internal void ShowImage(string imageUrl)
        {
            //ContentBody.Content = new Image()
            //{
            //    Source = new BitmapImage(new Uri(imageUrl))
            //};
        }

        private void ButtonClearToasts_Click(object sender, RoutedEventArgs e)
        {
            DesktopNotificationManagerCompat.History.Clear();
        }

        private void HideStatusBar()
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
                var ListWord = Regex.Split(tempInp, "\r\n|\r|\n");
                int TotalWords = ListWord.Length;
                Dispatcher.Invoke(() =>
                {
                    this.Btn_Import.IsEnabled = false;
                });
                Task.Factory.StartNew(() =>
                {
                    Status_UpdateMessage("Start Importing...");
                    int Count = 0;
                    int CountSuccess = 0;
                    foreach (var item in ListWord)
                    {
                        if (!String.IsNullOrEmpty(item))
                        {
                            Count++;
                            if (DataAccess.AddVocabulary(item) > 0)
                            {
                                CountSuccess++;
                            }
                        }
                        Status_UpdateProgressBar(Count, TotalWords);
                    }
                    Status_UpdateMessage("Imported Success " + CountSuccess + "/" + Count + " entered vocabulary.");
                    Dispatcher.Invoke(() =>
                    {
                        this.Btn_Import.IsEnabled = true;
                    });
                });
            }
            catch (Exception ex)
            {
                Helper.ShowToast("Import Failed: " + ex.Message);
                Dispatcher.Invoke(() =>
                {
                    this.Btn_Import.IsEnabled = true;
                });
            }
        }

        private async void Btn_ProcessCrawl_Click(object sender, RoutedEventArgs e)
        {
            Status_UpdateMessage("Start Crawling...");

            await Task.Run(() =>
            {
                Helper.ShowToast("[1/3] Start Get Translate...");
                ProcessBackgroundTranslate();
                Helper.ShowToast("[1/3] Finished Get Translate.");
            });   // wait to process all

            await Task.Run(() =>
            {
                Helper.ShowToast("[2/3] Start Get Vocabulary Information: Define, Example, Ipa...");
                ProcessBackgroundGetWordDefineInformation();
                Helper.ShowToast("[2/3] Finished Get Vocabulary Information: Define, Example, Ipa.");
            });   // wait to process all

            await Task.Run(() =>
            {
                Helper.ShowToast("[3/3] Start Get Related Words...");
                ProcessBackgroundGetRelatedWords();
                Helper.ShowToast("[3/3] Finished Get Related Words.");
            });   // wait to process all

            Helper.ShowToast("All of Crawling Finished. Enjoy the Learning Journey Now!.");
        }

        private void Btn_ProcessDeleteData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Helper.ShowToast("Start Deleting...");

                if (File.Exists(DataAccess.GetDatabasePath()))
                {
                    File.Delete(DataAccess.GetDatabasePath());
                }

                if (!File.Exists(DataAccess.GetDatabasePath()))
                {
                    var file = File.Create(DataAccess.GetDatabasePath());
                    file.Close();
                }
                DataAccess.InitializeDatabase();
                Helper.ShowToast("Delete Data Success");
            }
            catch (Exception ex)
            {
                Helper.ShowToast("Delete Data Fail: " + ex.Message);
            }
        }

        public void ProcessBackgroundTranslate()
        {
            try
            {
                var ListVocabulary = DataAccess.GetListVocabularyToTranslate();
                int TotalItems = ListVocabulary.Count;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * 2;
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    TranslateService.GetVocabularyTranslate(_item).Wait();
                });
            }
            catch (Exception ex)
            {
                Helper.ShowToast("Crawling: Process Background Translate Failed: " + ex.Message);
            }
        }

        public void ProcessBackgroundGetWordDefineInformation()
        {
            try
            {
                var ListVocabulary = DataAccess.GetListVocabularyToGetPlayURL();
                int count = 0;
                int total = ListVocabulary.Count;

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * 2;
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    TranslateService.GetWordDefineInformation(_item).Wait();
                    count++;
                });
            }
            catch (Exception ex)
            {
                Helper.ShowToast("Crawling: Process Background Get English Define, Ipa, Type, Example, MP3 URL Fail: " + ex.Message);
            }

        }

        public void ProcessBackgroundGetRelatedWords()
        {
            try
            {
                var ListVocabulary = DataAccess.GetListVocabularyToGetRelatedWords();

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * 2;
                Parallel.ForEach(ListVocabulary, parallelOptions, _item =>
                {
                    TranslateService.GetRelatedWord(_item).Wait();
                });

                foreach (var _item in ListVocabulary)
                {

                }
            }
            catch (Exception ex)
            {
                Helper.ShowToast("Crawling: Process Background Get Related Words Fail: " + ex.Message);
            }
        }

        private void ProcessBackgroundPreLoadMp3()
        {
            //try
            //{
            //    var ListVocabulary = DataAccess.GetListVocabularyToPreloadMp3();

            //    ParallelOptions parallelOptions = new ParallelOptions();
            //    parallelOptions.MaxDegreeOfParallelism = (int)Environment.ProcessorCount / 2;    // TODO
            //    await Task.Run(() => Parallel.ForEach(ListVocabulary, parallelOptions, async _item =>
            //    {
            //        await Mp3.preloadMp3Multiple(_item);
            //    }));

            //    Helper.ShowToast("Crawling: Process Background Download MP3 Files Finished.");

            //}
            //catch (Exception ex)
            //{
            //    Helper.ShowToast("Crawling: Process Background Get Play URL Fail: " + ex.Message);
            //}

        }

        private void Btn_StartLearning_Click(object sender, RoutedEventArgs e)
        {
            Vocabulary _item = DataAccess.GetVocabularyById(App.GlobalWordId);
            if (_item.Id == 0)
            {
                App.GlobalWordId = DataAccess.GetFirstWordId();
                _item = DataAccess.GetVocabularyById(App.GlobalWordId);
            }
            VocabularyToast.loadByVocabulary(_item);
            App.GlobalWordId++;
        }
    }
}
