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
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VocabularyReminder.DataAccessLibrary;

namespace VocabularyReminder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static int GlobalDicId = 0;
        public static int GlobalWordId = 0;
        public static bool isRandomWords = false;
        public static bool isAutoPlaySounds = false;
        public static bool isShowPopup = false;
        public static bool isUseCustomPopup = false;
        public static DateTime LastReaction;

        protected override void OnStartup(StartupEventArgs e)
        {
            DataAccess.InitializeDatabase();
            // Register AUMID, COM server, and activator
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<MyNotificationActivator>("FreelancerHCM.VocabularyReminder");
            DesktopNotificationManagerCompat.RegisterActivator<MyNotificationActivator>();

            // If launched from a toast
            // This launch arg was specified in our WiX installer where we register the LocalServer32 exe path.
            if (e.Args.Contains(DesktopNotificationManagerCompat.TOAST_ACTIVATED_LAUNCH_ARG))
            {
                // Our NotificationActivator code will run after this completes,
                // and will show a window if necessary.
            }

            else
            {
                // Show the window
                // In App.xaml, be sure to remove the StartupUri so that a window doesn't
                // get created by default, since we're creating windows ourselves (and sometimes we
                // don't want to create a window if handling a background activation).
                new MainWindow().Show();
            }

            base.OnStartup(e);
        }
    }
}
