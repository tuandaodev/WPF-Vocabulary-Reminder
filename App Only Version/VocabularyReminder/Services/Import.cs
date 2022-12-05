using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using VocabularyReminder.DataAccessLibrary;

namespace VocabularyReminder.Services
{
    public class Import
    {
        public async Task ImportDemo3000Words()
        {
            try
            {
                string demoUrl = "https://github.com/tuandaodev/VocabularyReminder/raw/master/Data/3000CommonWords.db";
                string filename = Path.GetFileName(demoUrl);
                string dbPath = ApplicationIO.GetDatabasePath();

                Backup();
                if (File.Exists(dbPath))
                    File.Delete(dbPath);

                HttpClient c = new HttpClient();
                using (var stream = await c.GetStreamAsync(demoUrl))
                {
                    using (var fileStream = File.OpenWrite(dbPath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }
            catch {
            }
        }

        public void Backup()
        {
            string dbPath = ApplicationIO.GetDatabasePath();
            File.Copy(dbPath, dbPath + $".bak_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}", true);
        }
    }
}
