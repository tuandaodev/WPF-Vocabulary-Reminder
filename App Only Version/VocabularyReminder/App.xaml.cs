using System;
using System.IO;
using System.Windows;
using VR.Services;
using VR.Infrastructure;

namespace VR
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Single instance of FloatingDictionary for global hotkey access
        public static FloatingDictionary GlobalFloatingDictionary { get; private set; }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clear cached data when application exits
            CacheService.Clear();
            
            // Clean up FloatingDictionary if it exists
            GlobalFloatingDictionary?.Close();
            
            base.OnExit(e);
        }

        public static int GlobalDicId = 0;
        public static int GlobalWordId = 0;
        public static string GlobalJsonDataId = string.Empty;

        //public static Vocabulary GlobalVocabulary = null;
        public static bool isRandomWords = false;
        public static bool isAutoPlaySounds = false;
        public static bool isShowPopup = false;
        public static bool showNextOnEasy = true;
        public static bool isUseCustomPopup = false;
        public static DateTime LastReaction;

        /// <summary>
        /// Gets or creates the global FloatingDictionary instance
        /// </summary>
        public static FloatingDictionary GetFloatingDictionary()
        {
            if (GlobalFloatingDictionary == null || !GlobalFloatingDictionary.IsLoaded)
            {
                GlobalFloatingDictionary = new FloatingDictionary();
            }
            return GlobalFloatingDictionary;
        }

        /// <summary>
        /// Toggles the FloatingDictionary window visibility
        /// </summary>
        public static void ToggleFloatingDictionary()
        {
            var floatingDict = GetFloatingDictionary();
            if (floatingDict.IsVisible)
            {
                floatingDict.Hide();
            }
            else
            {
                floatingDict.Show();
                floatingDict.Activate();
                floatingDict.Focus();
            }
        }

        /// <summary>
        /// Shows the FloatingDictionary window and focuses it
        /// </summary>
        public static void ShowFloatingDictionary()
        {
            var floatingDict = GetFloatingDictionary();
            if (!floatingDict.IsVisible)
            {
                floatingDict.Show();
            }
            floatingDict.Activate();
            floatingDict.Focus();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ForceRestoreIfNeeded();
            DataService.InitializeDatabase();
            
            // Initialize the global FloatingDictionary instance early
            // This ensures the global hotkey (Ctrl+Shift+Q) is registered
            GetFloatingDictionary();
            
            new MainWindow().Show();
            base.OnStartup(e);
        }

        private void ForceRestoreIfNeeded()
        {
            // Check for restore file
            string restorePath = ApplicationIO.GetRestoreDatabasePath();
            string dbPath = ApplicationIO.GetDatabasePath();
            
            if (File.Exists(restorePath))
            {
                try
                {
                    // Backup current database if it exists
                    if (File.Exists(dbPath))
                    {
                        string backupPath = dbPath + $".bak_{DateTime.Now:yyyyMMdd_HHmmss}";
                        File.Copy(dbPath, backupPath);
                    }

                    // Replace database with restore file
                    File.Copy(restorePath, dbPath, true);
                    File.Delete(restorePath);
                    MessageBox.Show($"Database restored successfully.", "Restore Database", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to restore database: {ex.Message}");
                }
            }
        }
    }
}
