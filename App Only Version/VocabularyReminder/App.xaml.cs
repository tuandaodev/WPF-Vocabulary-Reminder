using System;
using System.Windows;
using VR.Services;

namespace VR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            // Clear cached data when application exits
            CacheService.Clear();
            base.OnExit(e);
        }

        public static int GlobalDicId = 0;
        public static int GlobalWordId = 0;
        public static bool isRandomWords = false;
        public static bool isAutoPlaySounds = false;
        public static bool isShowPopup = false;
        public static bool isUseCustomPopup = false;
        public static DateTime LastReaction;

        protected override void OnStartup(StartupEventArgs e)
        {
            DataService.InitializeDatabase();

            new MainWindow().Show();

            base.OnStartup(e);
        }
    }
}
