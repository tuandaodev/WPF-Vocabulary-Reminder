using FAI.Core.Utilities.Linq;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace VocabularyReminder.DataAccessLibrary
{
    public class DataAccess
    {
        public static void InitializeDatabase()
        {
            string appFolder = ApplicationIO.GetApplicationFolderPath();
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            string dbFilePath = ApplicationIO.GetDatabasePath();
            if (!File.Exists(dbFilePath))
            {
                var file = File.Create(dbFilePath);
                file.Close();
            }

            using (SqliteConnection db = new SqliteConnection($"Filename={dbFilePath}"))
            {
                db.Open();
                String tableCommand = @"CREATE TABLE IF NOT EXISTS Dictionary (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                    Name NVARCHAR(2048) NULL, 
                    Description NVARCHAR(2048) NULL, 
                    Status INTEGER NULL DEFAULT 0)";
                SqliteCommand createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteReader();
                createTable.Dispose();

                tableCommand = @"CREATE TABLE IF NOT 
                    EXISTS Vocabulary (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Word NVARCHAR(2048) NOT NULL UNIQUE, 
                    Type NVARCHAR(100) NULL, 
                    Ipa NVARCHAR(100) NULL, 
                    Ipa2 NVARCHAR(100) NULL, 
                    Translate NVARCHAR(2048) NULL, 
                    Define NVARCHAR(2048) NULL, 
                    Example NVARCHAR(2048) NULL, 
                    Example2 NVARCHAR(2048) NULL, 
                    PlayURL NVARCHAR(2048) NULL, 
                    PlayURL2 NVARCHAR(2048) NULL, 
                    Related NVARCHAR(2048) NULL, 
                    Status INTEGER NULL DEFAULT 1 )";
                createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteReader();

                createTable.Dispose();
                db.Close();
                db.Dispose();
            }
        }

        public static async Task<int> AddVocabularyAsync(string inputText)
        {
            if (String.IsNullOrEmpty(inputText)) return 0;
            using (var context = new VocaDbContext())
            {
                var voca = new Vocabulary()
                {
                    Word = inputText.Trim()
                };
                context.Vocabularies.Add(voca);
                await context.SaveChangesAsync();
                return voca.Id;
            }
        }

        public static async Task UpdateVocabularyAsync(Vocabulary item)
        {
            using (var context = new VocaDbContext())
            {
                await context.SingleUpdateAsync(item);
            }
        }

        public static async Task UpdateStatusAsync(int _Id, int _Status = 0)
        {
            using (var context = new VocaDbContext())
            {
                await context.Vocabularies
                    .Where(e => e.Id == _Id)
                    .UpdateFromQueryAsync(x => new Vocabulary()
                    {
                        Status = _Status
                    });
            }
        }

        public static async Task<Vocabulary> GetVocabularyByIdAsync(int Id)
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.FindAsync(Id);
            }
        }

        public static async Task<Vocabulary> GetNextVocabularyAsync(int Id)
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => e.Id > Id && e.Status == 1).FirstOrDefaultAsync();
            }
        }

        public static async Task<Vocabulary> GetRandomVocabularyAsync(int Id)
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => e.Id > Id && e.Status == 1).OrderBy(e => Guid.NewGuid()).FirstOrDefaultAsync();
            }
        }

        public static async Task<Vocabulary> GetFirstVocabularyAsync()
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => e.Status == 1).OrderBy(e => e.Id).FirstOrDefaultAsync();
            }
        }

        public static Stats GetStats()
        {
            string dbpath = ApplicationIO.GetDatabasePath();
            Stats _Stats = new Stats();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                string cmd = @" SELECT COUNT(*) as Total,
                                (SELECT COUNT(*) FROM Vocabulary WHERE Status = 0) as Remembered
                                FROM Vocabulary";

                db.Open();
                SqliteCommand selectCommand = new SqliteCommand(cmd, db);
                SqliteDataReader query = selectCommand.ExecuteReader();

                
                while (query.Read())
                {
                    _Stats.Total = int.Parse(query.GetString(0));
                    _Stats.Remembered = int.Parse(query.GetString(1));
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }
            return _Stats;
        }

        public static async Task<List<Vocabulary>> GetListVocabularyToPreloadMp3Async()
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => !string.IsNullOrEmpty(e.PlayURL)).ToListAsync();
            }
        }

        public static async Task<List<Vocabulary>> GetListVocabularyToTranslateAsync()
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => string.IsNullOrEmpty(e.Translate)).ToListAsync();
            }
        }


        public static async Task<List<Vocabulary>> GetListVocabularyToGetDefineExampleMp3URLAsync()
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => e.PlayURL == null || e.Translate == string.Empty).ToListAsync();
            }
        }

        public static async Task<List<Vocabulary>> GetListVocabularyToGetRelatedWordsAsync()
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => string.IsNullOrEmpty(e.Related)).ToListAsync();
            }
        }

        public static async Task<List<Vocabulary>> GetListLearndedAsync(bool? isRead, string searchContent)
        {
            using (var context = new VocaDbContext())
            {
                Expression<Func<Vocabulary, bool>> exp = x => true;
                if (isRead.HasValue)
                    exp = exp.And(e => e.Status == (isRead.Value ? 0 : 1));
                if (!string.IsNullOrEmpty(searchContent))
                    exp = exp.And(e => e.Word.Contains(searchContent.Trim()));

                return await context.Vocabularies.Where(exp).ToListAsync();
            }
        }

        public static async Task<Vocabulary> GetVocabularyByWordAsync(string word)
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => e.Word == word.Trim()).FirstOrDefaultAsync();
            }
        }

        public static async Task CleanUnableToGetAsync()
        {
            using (var context = new VocaDbContext())
            {
                await context.Vocabularies.Where(e => e.Type == string.Empty && e.Ipa == null && e.Translate == string.Empty).DeleteFromQueryAsync();
            }
        }
    }
}
