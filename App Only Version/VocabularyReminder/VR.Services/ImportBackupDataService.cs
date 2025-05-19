using System;
using System.IO;
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
    }
}
