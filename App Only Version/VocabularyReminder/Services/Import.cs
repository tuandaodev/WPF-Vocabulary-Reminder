﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
                string filename = System.IO.Path.GetFileName(demoUrl);
                string dbPath = ApplicationIO.GetDatabasePath();

                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

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
    }
}
