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
using Microsoft.QueryStringDotNET;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using VR.Domain.Models;
using VR.Services;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace VR
{
    // The GUID CLSID must be unique to your app. Create a new GUID if copying this code.
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("3b185435-0bd1-4437-b481-0734438718e0"), ComVisible(true)]
    public class MyNotificationActivator : NotificationActivator
    {
        public override void OnActivated(string arguments, NotificationUserInput userInput, string appUserModelId)
        {
            Application.Current.Dispatcher.Invoke(async delegate
            {
                if (arguments.Length == 0)
                {
                    //OpenWindowIfNeeded();
                    return;
                }

                // Parse the query string (using NuGet package QueryString.NET)
                QueryString args = QueryString.Parse(arguments);

                _ = args.TryGetValue("action", out string main_action);
                // See what action is being requested 
                App.LastReaction = DateTime.Now;
                Vocabulary _item;
                switch (main_action)
                {
                    //case "reload":
                    //    App.GlobalWordId = int.Parse(args["WordId"]);
                    //    var _item = DataAccess.GetVocabularyById(App.GlobalWordId);
                    //    VocabularyToast.loadByVocabulary(_item);
                    //    _item = null;
                    //    break;
                    case "play":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        int playId = int.Parse(args["PlayId"]);
                        if (App.GlobalWordId > 0)
                        {
                            string _mp3Url;
                            if (VocabularyToastService.ReloadLastToast())
                            {
                                _mp3Url = args["PlayUrl"];
                            }
                            else
                            {
                                _item = await DataService.GetVocabularyByIdAsync(App.GlobalWordId);
                                VocabularyToastService.ShowToastByVocabularyItem(_item);
                                if (playId == 2)
                                {
                                    _mp3Url = _item.PlayURL;
                                }
                                else
                                {
                                    _mp3Url = _item.PlayURL2;
                                }
                            }

                            if (!string.IsNullOrEmpty(_mp3Url))
                            {
                                _ = Task.Run(() => Mp3Service.PlayFileAsync(_mp3Url));
                            }
                        }
                        break;

                    case "next":
                        DesktopNotificationManagerCompat.History.Clear();
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        if (App.isRandomWords)
                        {
                            _item = await DataService.GetRandomVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
                        }
                        else
                        {
                            _item = await DataService.GetNextVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
                        }

                        if (_item == null || _item.Id == 0)
                        {
                            _item = await DataService.GetFirstVocabularyAsync(App.GlobalDicId);
                        }
                        App.GlobalWordId = _item != null ? _item.Id : 0;

                        _ = Task.Run(async () =>
                          {
                              await Task.Delay(1000);
                              VocabularyToastService.ShowToastByVocabularyItem(_item);
                              if (App.isAutoPlaySounds)
                              {
                                  await Mp3Service.PlayFileAsync(_item);
                              }
                              _item = null;
                          });

                        break;

                    case "view":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        string url = args["url"];
                        System.Diagnostics.Process.Start(url);
                        break;

                    case "delete":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        await DataService.UpdateStatusAsync(App.GlobalWordId, 0);  // skip this word
                        break;

                    case "nextdelete":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        await DataService.UpdateStatusAsync(App.GlobalWordId, 0);  // skip this word

                        if (App.isRandomWords)
                        {
                            _item = await DataService.GetRandomVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
                        }
                        else
                        {
                            _item = await DataService.GetNextVocabularyAsync(App.GlobalDicId, App.GlobalWordId);
                        }

                        if (_item == null || _item.Id == 0)
                        {
                            _item = await DataService.GetFirstVocabularyAsync(App.GlobalDicId);
                        }
                        App.GlobalWordId = _item != null ? _item.Id : 0;
                        VocabularyToastService.ShowToastByVocabularyItem(_item);
                        if (App.isAutoPlaySounds)
                        {
                            _ = Task.Run(() => Mp3Service.PlayFileAsync(_item));
                        }
                        _item = null;
                        break;
                }
            });
        }

        private void OpenWindowIfNeeded()
        {
            // Make sure we have a window open (in case user clicked toast while app closed)
            if (App.Current.Windows.Count == 0)
            {
                new MainWindow().Show();
            }

            // Activate the window, bringing it to focus
            App.Current.Windows[0].Activate();

            // And make sure to maximize the window too, in case it was currently minimized
            App.Current.Windows[0].WindowState = WindowState.Normal;
        }

        private void ShowToast(string msg)
        {
            // Construct the visuals of the toast
            ToastContent toastContent = new ToastContent()
            {
                // Arguments when the user taps body of toast
                Launch = "action=ok",

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = msg
                            }
                        }
                    }
                }
            };

            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());

            // And create the toast notification
            var toast = new ToastNotification(doc);

            // And then show it
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast);
        }
    }
}
