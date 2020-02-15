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
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
//using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace VocabularyReminderApp
{
    public class VocabularyToast
    {
        const string viewDicOnlineUrl = "https://www.oxfordlearnersdictionaries.com/definition/english/";
        public static async void loadByVocabulary(Vocabulary _item)
        {
            if (_item.Id == 0)
            {
                Helper.ShowToast("Chưa có dữ liệu từ điển. Vui lòng import.");
                return;
            }
            ToastContent content;
            //if (String.IsNullOrEmpty(_item.PlayURL))
            //{
            //    content = getToastContentWithoutPlay(_item);
            //}
            //else
            //{
            //    Mp3.preloadMp3FileSingle(_item);
            //    content = getToastContent(_item);
            //}

            Mp3.preloadMp3FileSingle(_item);
            content = await getToastContent(_item);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(content.GetContent());
            var _toastItem = new ToastNotification(xmlDoc)
            {
                Tag = "Vocabulary",
                Group = "Reminder",
            };

            //_toastItem.Activated += ToastActivated;
            //_toastItem.Dismissed += ToastDismissed;
            //_toastItem.Failed += ToastFailed;
            //_toastItem.Priority = ToastNotificationPriority.High;


            //ToastNotificationManager.CreateToastNotifier(MainWindow.APP_ID).Show(_toastItem);
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(_toastItem);
        }


        public static bool reloadLastToast()
        {
            //var _history = ToastNotificationManager.History.GetHistory();
            var _history = DesktopNotificationManagerCompat.History.GetHistory();
            if (_history.Count() > 0)
            {
                DesktopNotificationManagerCompat.CreateToastNotifier().Show(_history.Last());
                return true;
            }
            return false;
        }

        private static void ToastActivated(ToastNotification sender, object e)
        {
            if (e is ToastActivatedEventArgs)
            {
                ToastActivatedEventArgs args = (ToastActivatedEventArgs)e;
                QueryString qs = QueryString.Parse(args.Arguments);
                if (qs.TryGetValue("action", out string action))
                {
                    processCustomAction(action, qs);
                }
            }
        }

        //public void ToastActivated()
        //{
        //    //Dispatcher.Invoke(() =>
        //    //{
        //    //    processCustomAction();
        //    //});
        //}

        private static void processCustomAction(string main_action, QueryString args)
        {
            try
            {
                switch (main_action)
                {
                    case "reload":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        var _item = DataAccess.GetVocabularyById(App.GlobalWordId);
                        VocabularyToast.loadByVocabulary(_item);
                        _item = null;
                        break;
                    case "play":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        int playId = int.Parse(args["PlayId"]);
                        if (App.GlobalWordId > 0)
                        {
                            string _mp3Url;
                            if (VocabularyToast.reloadLastToast())
                            {
                                _mp3Url = args["PlayUrl"];
                            }
                            else
                            {
                                _item = DataAccess.GetVocabularyById(App.GlobalWordId);
                                VocabularyToast.loadByVocabulary(_item);
                                if (playId == 2)
                                {
                                    _mp3Url = _item.PlayURL2;
                                }
                                else
                                {
                                    _mp3Url = _item.PlayURL;
                                }
                            }

                            if (!String.IsNullOrEmpty(_mp3Url))
                            {
                                Mp3.PlayFile(_mp3Url);
                                //if (MainWindow.mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                                //{
                                //    listPlayMp3.Enqueue(_mp3Url);
                                //}
                                //else
                                //{
                                //    checkMediaPlayer();
                                //    Mp3.play(_mp3Url);
                                //}
                            }
                        }
                        break;
                    case "next":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        if (App.GlobalWordId > 0)
                        {
                            App.GlobalWordId++;
                        }
                        else
                        {
                            App.GlobalWordId = DataAccess.GetFirstWordId();
                        }
                        var _item2 = DataAccess.GetVocabularyById(App.GlobalWordId);
                        VocabularyToast.loadByVocabulary(_item2);
                        break;
                    case "view":
                        string SearchUrl = args["url"];
                        // The URI to launch
                        var success = System.Diagnostics.Process.Start(SearchUrl);
                        break;
                }
            }
            catch (Exception ex)
            {
                Helper.ShowToast("Action Background Error: " + ex.Message);
            }
        }

        private static void ToastDismissed(object source, ToastDismissedEventArgs e)
        {
            switch (e.Reason)
            {
                case ToastDismissalReason.ApplicationHidden:
                    // Application hid the toast with ToastNotifier.Hide
                    Console.WriteLine("Application Hidden");
                    break;
                case ToastDismissalReason.UserCanceled:
                    Console.WriteLine("User dismissed the toast");
                    break;
                case ToastDismissalReason.TimedOut:
                    Console.WriteLine("Toast timeout elapsed");
                    break;
            }

            Console.WriteLine("Toast Dismissed: " + e.Reason.ToString());
        }

        private static void ToastFailed(object source, ToastFailedEventArgs e)
        {
            // Check the error code
            var errorCode = e.ErrorCode;
            Console.WriteLine("Error code:{0}", errorCode);
        }

        private static async Task<ToastContent> getToastContent(Vocabulary _item)
        {
            string _Ipa = _item.Ipa;
            if (_item.Ipa != _item.Ipa2)
            {
                _Ipa = _item.Ipa + " " + _item.Ipa2;
            }

            ToastContent content = new ToastContent()
            {

                Duration = ToastDuration.Long,
                Launch = "vocabulary-reminder",
                Audio = new ToastAudio() { Silent = true },
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Attribution = new ToastGenericAttributionText()
                        {
                            Text = _item.Type
                        },
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _item.Define,
                            },

                            new AdaptiveText()
                            {
                                Text = _item.Example,
                            },

                            new AdaptiveText()
                            {
                                Text = _item.Example2,
                            },

                            new AdaptiveGroup()
                            {
                                Children =
                                {
                                    new AdaptiveSubgroup()
                                    {
                                        Children =
                                        {
                                            new AdaptiveText()
                                            {
                                                Text = _item.Word + " " + _Ipa,
                                                HintStyle = AdaptiveTextStyle.Subtitle,
                                            },
                                            new AdaptiveText()
                                            {
                                                Text = _item.Translate,
                                                HintStyle = AdaptiveTextStyle.Base,
                                            },
                                            new AdaptiveText()
                                            {
                                                Text = _item.Related,
                                                HintStyle = AdaptiveTextStyle.CaptionSubtle
                                            }
                                        }
                                    },
                                }
                            }
                        },
                        HeroImage = new ToastGenericHeroImage()
                        {
                            //Source = "https://picsum.photos/364/180?image=1043",
                            Source = await DownloadImageToDisk("https://picsum.photos/364/180?image=1043"),
                        },
                    }
                },
                Scenario = ToastScenario.Reminder,
                Actions = new ToastActionsCustom()
                {
                    ContextMenuItems =
                    {
                        new ToastContextMenuItem("Reload", "action=reload&WordId=" + _item.Id.ToString())
                    },
                    Buttons =
                        {
                            new ToastButton("\u25B6", new QueryString()
                            {
                                { "action", "play" },
                                { "WordId", _item.Id.ToString() },
                                { "PlayId", "1" },
                                { "PlayUrl", _item.PlayURL },
                            }.ToString()) {
                                ActivationType = ToastActivationType.Background,
                                ActivationOptions = new ToastActivationOptions()
                                {
                                    AfterActivationBehavior = ToastAfterActivationBehavior.PendingUpdate
                                }
                            },
                            new ToastButton("\u25B7", new QueryString()
                            {
                                { "action", "play" },
                                { "WordId", _item.Id.ToString() },
                                { "PlayId", "2" },
                                { "PlayUrl", _item.PlayURL2 },
                            }.ToString())
                            {
                                ActivationType = ToastActivationType.Background,
                                ActivationOptions = new ToastActivationOptions()
                                {
                                    AfterActivationBehavior = ToastAfterActivationBehavior.PendingUpdate
                                }
                            },
                            new ToastButton("Next", new QueryString()
                            {
                                { "action", "next" },
                                { "WordId", _item.Id.ToString() },
                            }.ToString())
                            {
                                ActivationType = ToastActivationType.Background,
                                ActivationOptions = new ToastActivationOptions()
                                {
                                    AfterActivationBehavior = ToastAfterActivationBehavior.PendingUpdate
                                }
                            },
                            new ToastButton("View", new QueryString()
                            {
                                { "action", "view" },
                                { "url", viewDicOnlineUrl + _item.Word }
                            }.ToString()),
                            //new ToastButton("Close", "dismiss")
                            //{
                            //    ActivationType = ToastActivationType.Background
                            //},
                        }
                },

            };

            return content;
        }


        private static bool _hasPerformedCleanup;
        private static async Task<string> DownloadImageToDisk(string httpImage)
        {
            // Toasts can live for up to 3 days, so we cache images for up to 3 days.
            // Note that this is a very simple cache that doesn't account for space usage, so
            // this could easily consume a lot of space within the span of 3 days.

            try
            {
                if (DesktopNotificationManagerCompat.CanUseHttpImages)
                {
                    return httpImage;
                }

                var directory = Directory.CreateDirectory(DataAccess.GetImageFolder());

                if (!_hasPerformedCleanup)
                {
                    // First time we run, we'll perform cleanup of old images
                    _hasPerformedCleanup = true;

                    foreach (var d in directory.EnumerateDirectories())
                    {
                        if (d.CreationTimeUtc.Date < DateTime.UtcNow.Date.AddDays(-3))
                        {
                            d.Delete(true);
                        }
                    }
                }

                var dayDirectory = directory.CreateSubdirectory(DateTime.UtcNow.Day.ToString());
                string imagePath = dayDirectory.FullName + "\\" + (uint)httpImage.GetHashCode();

                if (File.Exists(imagePath))
                {
                    return imagePath;
                }

                HttpClient c = new HttpClient();
                using (var stream = await c.GetStreamAsync(httpImage))
                {
                    using (var fileStream = File.OpenWrite(imagePath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }

                return imagePath;
            }
            catch { return ""; }
        }

        private static ToastContent getToastContentWithoutPlay(Vocabulary _item)
        {
            ToastContent content = new ToastContent()
            {
                Launch = "vocabulary-reminder",
                Audio = new ToastAudio() { Silent = true },
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = _item.Word,
                                HintMaxLines = 1
                            },

                            new AdaptiveText()
                            {
                                Text = _item.Ipa,
                            },

                            new AdaptiveText()
                            {
                                Text = _item.Translate
                            }
                        },
                        HeroImage = new ToastGenericHeroImage()
                        {
                            Source = "https://picsum.photos/364/180?image=1043"
                        },

                    }
                },
                Scenario = ToastScenario.Reminder,
                Actions = new ToastActionsCustom()
                {
                    Buttons =
                        {
                            new ToastButton("Next", new QueryString()
                            {
                                { "WordId", _item.Id.ToString() },
                                { "action", "next" },
                            }.ToString())
                            {
                                ActivationType = ToastActivationType.Background,
                                ActivationOptions = new ToastActivationOptions()
                                {
                                    AfterActivationBehavior = ToastAfterActivationBehavior.PendingUpdate
                                }
                            },
                            new ToastButton("View", new QueryString()
                                {
                                    { "action", "view" },
                                    { "url", viewDicOnlineUrl + _item.Word }

                                }.ToString()),
                            new ToastButton("Skip", "dismiss")
                            {
                                ActivationType = ToastActivationType.Background
                            },
                        }
                },

            };

            return content;
        }
    }
}
