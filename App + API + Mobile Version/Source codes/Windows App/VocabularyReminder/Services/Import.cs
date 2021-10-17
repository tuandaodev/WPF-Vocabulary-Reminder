using System;
using System.IO;
using VocabularyReminder.DataAccessLibrary;

namespace VocabularyReminder.Services
{
    public class Import
    {
        public void ImportDemo3000Words()
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "Data", "3000CommonWords.db");
                string dbPath = ApplicationIO.GetDatabasePath();

                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var fileStream = File.OpenWrite(dbPath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }
            catch
            {
            }
        }
    }
}
