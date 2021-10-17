﻿// ******************************************************************
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VocabularyReminder.DataAccessLibrary;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace VocabularyReminder
{
    // The GUID CLSID must be unique to your app. Create a new GUID if copying this code.
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("3b185435-0bd1-4437-b481-0734438718e0"), ComVisible(true)]
    public class MyNotificationActivator : NotificationActivator
    {
        public override void OnActivated(string arguments, NotificationUserInput userInput, string appUserModelId)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (arguments.Length == 0)
                {
                    //OpenWindowIfNeeded();
                    return;
                }

                // Parse the query string (using NuGet package QueryString.NET)
                QueryString args = QueryString.Parse(arguments);
                
                string main_action;
                args.TryGetValue("action", out main_action);
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
                            if (VocabularyToast.reloadLastToast())
                            {
                                _mp3Url = args["PlayUrl"];
                            }
                            else
                            {
                                _item = DataAccess.GetVocabularyById(App.GlobalWordId);
                                VocabularyToast.showToastByVocabularyItem(_item);
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
                                Task.Run(() => Mp3.PlayFile(_mp3Url));
                            }
                        }
                        break;

                    case "next":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        if (App.isRandomWords)
                        {
                            _item = DataAccess.GetRandomVocabulary(App.GlobalWordId);
                        } else {
                            _item = DataAccess.GetNextVocabulary(App.GlobalWordId);
                        }
                        
                        if (_item.Id == 0)
                        {
                            _item = DataAccess.GetFirstVocabulary();
                        }
                        App.GlobalWordId = _item.Id;
                        VocabularyToast.showToastByVocabularyItem(_item);
                        _item = null;
                        break;

                    case "view":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        string url = args["url"];
                        System.Diagnostics.Process.Start(url);
                        break;

                    case "delete":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        DataAccess.UpdateStatus(App.GlobalWordId, 0);  // skip this word
                        break;

                    case "nextdelete":
                        App.GlobalWordId = int.Parse(args["WordId"]);
                        DataAccess.UpdateStatus(App.GlobalWordId, 0);  // skip this word

                        if (App.isRandomWords)
                        {
                            _item = DataAccess.GetRandomVocabulary(App.GlobalWordId);
                        }
                        else
                        {
                            _item = DataAccess.GetNextVocabulary(App.GlobalWordId);
                        }

                        if (_item.Id == 0)
                        {
                            _item = DataAccess.GetFirstVocabulary();
                        }
                        App.GlobalWordId = _item.Id;
                        VocabularyToast.showToastByVocabularyItem(_item);
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
