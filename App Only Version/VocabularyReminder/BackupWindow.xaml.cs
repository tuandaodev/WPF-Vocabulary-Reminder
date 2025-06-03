using System;
using System.Windows;
using VR.Infrastructure;
using VR.Services;

namespace VR
{
    /// <summary>
    /// Interaction logic for BackupWindow.xaml
    /// </summary>
    public partial class BackupWindow : Window
    {
        private MainWindow _parentWindow;

        public BackupWindow(MainWindow parentWindow)
        {
            InitializeComponent();
            _parentWindow = parentWindow;
            UpdateGoogleButtonsVisibility();
        }

        private void UpdateGoogleButtonsVisibility()
        {
            var backupService = new ImportBackupDataService();
            var isLoggedIn = backupService.IsLoggedIn();
            
            Btn_RestoreFromGoogleDrive.Visibility = isLoggedIn ? Visibility.Visible : Visibility.Collapsed;
            Btn_LogoutGoogle.Visibility = isLoggedIn ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Status_UpdateMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                Status_Message.Text = message;
            });
        }

        private void Status_UpdateProgressBar(int value = 0, int max = 100)
        {
            Dispatcher.Invoke(() =>
            {
                Status_ProgressBar.Value = value;
                Status_ProgressBar.Maximum = max;
            });
        }

        private async void Btn_BackupToGoogleDrive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Status_UpdateMessage("Backing up to Google Drive...");
                var backupService = new ImportBackupDataService();
                var backupFileName = backupService.Backup();
                await backupService.BackupToGoogleDriveAsync(System.IO.Path.Combine("Data", backupFileName));
                UpdateGoogleButtonsVisibility();
                Status_UpdateMessage("Backup to Google Drive completed.");
                MessageBox.Show("Backup to Google Drive completed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Update parent window's button visibility
                _parentWindow?.UpdateGoogleButtonVisibility();
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Backup to Google Drive failed: " + ex.Message);
                MessageBox.Show("Backup to Google Drive failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Btn_RestoreFromGoogleDrive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Status_UpdateMessage("Listing backup files on Google Drive...");
                var backupService = new ImportBackupDataService();
                var files = await backupService.ListBackupFilesOnGoogleDriveAsync();

                if (files == null || files.Count == 0)
                {
                    MessageBox.Show("No backup files found on Google Drive.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Use WPF dialog for file selection
                var selectWindow = new SelectBackupFileWindow(files);
                selectWindow.Owner = this;
                if (selectWindow.ShowDialog() != true || selectWindow.SelectedFile == null)
                {
                    Status_UpdateMessage("Restore cancelled.");
                    return;
                }
                var selectedFile = selectWindow.SelectedFile;

                Status_UpdateMessage("Downloading and restoring backup...");
                string restorePath = ApplicationIO.GetRestoreDatabasePath();
                await backupService.RestoreFromGoogleDriveAsync(selectedFile.Id, restorePath);
                
                // Force application restart
                MessageBox.Show("Restore completed. The application will now restart.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Status_UpdateMessage("Restore from Google Drive failed: " + ex.Message);
                MessageBox.Show("Restore from Google Drive failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Btn_LogoutGoogle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Status_UpdateMessage("Logging out from Google Drive...");
                var backupService = new ImportBackupDataService();
                backupService.LogoutGoogle();
                UpdateGoogleButtonsVisibility();
                Status_UpdateMessage("Successfully logged out from Google Drive");
                MessageBox.Show("Successfully logged out from Google Drive.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Update parent window's button visibility
                _parentWindow?.UpdateGoogleButtonVisibility();
            }
            catch (Exception ex)
            {
                Status_UpdateMessage($"Logout failed: {ex.Message}");
                MessageBox.Show($"Failed to logout: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}