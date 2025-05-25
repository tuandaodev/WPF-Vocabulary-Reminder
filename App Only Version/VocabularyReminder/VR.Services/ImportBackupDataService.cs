using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VR.Infrastructure;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace VR.Services
{
    public class ImportBackupDataService
    {
        public async Task ImportDemo3000WordsAsync()
        {
            try
            {
                string demoUrl = "https://github.com/tuandaodev/VocabularyReminder/raw/master/Data/3000CommonWords.db";
                string filename = Path.GetFileName(demoUrl);
                string dbPath = ApplicationIO.GetDatabasePath();

                Backup();
                if (System.IO.File.Exists(dbPath))
                    System.IO.File.Delete(dbPath);

                HttpClient c = new HttpClient();
                using (var stream = await c.GetStreamAsync(demoUrl))
                {
                    using (var fileStream = System.IO.File.OpenWrite(dbPath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }
            catch {
            }
        }

        public string Backup()
        {
            string dbPath = ApplicationIO.GetDatabasePath();
            string backupName = $".bak_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
            System.IO.File.Copy(dbPath, dbPath + backupName, true);
            return dbPath + backupName;
        }

        /// <summary>
        /// Login with Google and upload the backup file to Google Drive.
        /// Requires a client_secrets.json file from Google Cloud Console in the Data directory.
        /// </summary>
        public async Task BackupToGoogleDriveAsync(string backupFilePath)
        {
            string[] Scopes = { DriveService.Scope.DriveFile };
            string ApplicationName = "VocabularyReminderBackup";

            UserCredential credential;
            using (var stream =
                new FileStream(Path.Combine("Data", "client_secrets.json"), FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(backupFilePath)
            };
            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(backupFilePath, FileMode.Open))
            {
                request = service.Files.Create(
                    fileMetadata, stream, "application/octet-stream");
                request.Fields = "id";
                await request.UploadAsync();
            }
        }
/// <summary>
        /// Lists backup files from Google Drive (by extension .bak or name pattern).
        /// </summary>
        public async Task<IList<Google.Apis.Drive.v3.Data.File>> ListBackupFilesOnGoogleDriveAsync()
        {
            string[] Scopes = { DriveService.Scope.DriveFile };
            string ApplicationName = "VocabularyReminderBackup";
            UserCredential credential;
            using (var stream =
                new FileStream(Path.Combine("Data", "client_secrets.json"), FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // List files with .bak in the name
            var listRequest = service.Files.List();
            listRequest.Q = "name contains '.bak_'";
            listRequest.Fields = "files(id, name, createdTime)";
            var result = await listRequest.ExecuteAsync();
            return result.Files;
        }

        /// <summary>
        /// Downloads the selected backup file from Google Drive and restores the local database.
        /// </summary>
        public async Task RestoreFromGoogleDriveAsync(string fileId, string localRestorePath)
        {
            string[] Scopes = { DriveService.Scope.DriveFile };
            string ApplicationName = "VocabularyReminderBackup";
            UserCredential credential;
            using (var stream =
                new FileStream(Path.Combine("Data", "client_secrets.json"), FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var request = service.Files.Get(fileId);
            using (var stream = new FileStream(localRestorePath, FileMode.Create, FileAccess.Write))
            {
                await request.DownloadAsync(stream);
            }

            // Replace the current database with the downloaded backup
            string dbPath = ApplicationIO.GetDatabasePath();
            if (System.IO.File.Exists(dbPath))
                System.IO.File.Delete(dbPath);
            System.IO.File.Copy(localRestorePath, dbPath);
        }
        /// <summary>
        /// Logs out from Google Drive by removing the stored credentials.
        /// </summary>
        public void LogoutGoogle()
        {
            string credPath = Path.Combine(Environment.CurrentDirectory, "token.json");
            if (Directory.Exists(credPath))
            {
                Directory.Delete(credPath, true);
            }
        }
    }
}
