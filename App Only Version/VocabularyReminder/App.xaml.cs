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

        protected override void OnExit(ExitEventArgs e)
        {
            // Clear cached data when application exits
            CacheService.Clear();
            
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

        protected override void OnStartup(StartupEventArgs e)
        {
            ForceRestoreIfNeeded();
            DataService.InitializeDatabase();
            
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
