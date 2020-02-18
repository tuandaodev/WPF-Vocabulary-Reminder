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
using VocabularyReminder.DataAccessLibrary;
using Windows.Data.Xml.Dom;
//using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace VocabularyReminder
{
    public class VocabularyToast
    {
        const string viewDicOnlineUrl = "https://www.oxfordlearnersdictionaries.com/definition/english/";
        
        public static void ClearApplicationToast()
        {
            DesktopNotificationManagerCompat.History.Clear();
        }

        public static async void showToastByVocabularyItem(Vocabulary _item)
        {
            if (_item.Id == 0)
            {
                Helper.ShowToast("Chưa có dữ liệu từ điển. Vui lòng import.");
                return;
            }
            ToastContent content;
            if (String.IsNullOrEmpty(_item.PlayURL))
            {
                content = await getToastContentWithoutPlayAsync(_item);
            }
            else
            {
                Mp3.preloadMp3FileSingle(_item);
                content = await getToastContent(_item);
            }

            Mp3.preloadMp3FileSingle(_item);
            //content = await getToastContent(_item);

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(content.GetContent());
            var _toastItem = new ToastNotification(xmlDoc)
            {
                Tag = "Vocabulary",
                Group = "Reminder",
            };

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
                Launch = new QueryString() {
                                { "action", "view" },
                                { "WordId", _item.Id.ToString() },
                                { "url", viewDicOnlineUrl + _item.Word }
                            }.ToString(),
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
                            Source = await DownloadImageToDisk("https://picsum.photos/364/180?image=1043"),
                        },
                    }
                },
                //Scenario = ToastScenario.Reminder,
                Actions = new ToastActionsCustom()
                {
                    //ContextMenuItems =
                    //{
                    //    new ToastContextMenuItem("Reload", "action=reload&WordId=" + _item.Id.ToString())
                    //},
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
                            //new ToastButton("View", new QueryString()
                            //{
                            //    { "action", "view" },
                            //    { "url", viewDicOnlineUrl + _item.Word }
                            //}.ToString()),
                            new ToastButton("Delete", new QueryString()
                            {
                                { "action", "delete" },
                                { "WordId", _item.Id.ToString() },
                            }.ToString()),
                            new ToastButton("N&Del", new QueryString()
                            {
                                { "action", "nextdelete" },
                                { "WordId", _item.Id.ToString() },
                            }.ToString()){
                                ActivationType = ToastActivationType.Background,
                                ActivationOptions = new ToastActivationOptions()
                                {
                                    AfterActivationBehavior = ToastAfterActivationBehavior.PendingUpdate
                                }
                            },
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

                var directory = Directory.CreateDirectory(ApplicationIO.GetImageFolder());

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

        private static async Task<ToastContent> getToastContentWithoutPlayAsync(Vocabulary _item)
        {
            string _Ipa = _item.Ipa;
            if (_item.Ipa != _item.Ipa2)
            {
                _Ipa = _item.Ipa + " " + _item.Ipa2;
            }

            ToastContent content = new ToastContent()
            {

                Duration = ToastDuration.Long,
                Launch = new QueryString() {
                                { "action", "view" },
                                { "WordId", _item.Id.ToString() },
                                { "url", viewDicOnlineUrl + _item.Word }
                            }.ToString(),
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
                            Source = await DownloadImageToDisk("https://picsum.photos/364/180?image=1043"),
                        },
                    }
                },
                //Scenario = ToastScenario.Reminder,
                Actions = new ToastActionsCustom()
                {
                    //ContextMenuItems =
                    //{
                    //    new ToastContextMenuItem("Reload", "action=reload&WordId=" + _item.Id.ToString())
                    //},
                    Buttons =
                        {
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
                            //new ToastButton("View", new QueryString()
                            //{
                            //    { "action", "view" },
                            //    { "url", viewDicOnlineUrl + _item.Word }
                            //}.ToString()),
                            new ToastButton("Delete", new QueryString()
                            {
                                { "action", "delete" },
                                { "WordId", _item.Id.ToString() },
                            }.ToString()),
                            new ToastButton("N&Del", new QueryString()
                            {
                                { "action", "nextdelete" },
                                { "WordId", _item.Id.ToString() },
                            }.ToString()){
                                ActivationType = ToastActivationType.Background,
                                ActivationOptions = new ToastActivationOptions()
                                {
                                    AfterActivationBehavior = ToastAfterActivationBehavior.PendingUpdate
                                }
                            },
                        }
                },

            };

            return content;
        }
    }
}
