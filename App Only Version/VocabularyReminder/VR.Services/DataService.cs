using FAI.Core.Utilities.Linq;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VR.Domain;
using VR.Domain.Models;
using VR.Dto;
using VR.Infrastructure;

namespace VR.Services
{
    public class DataService
    {
        public static Vocabulary CurrentVocabulary { get; set; }

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

                tableCommand = @"CREATE TABLE IF NOT EXISTS Vocabulary (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Word NVARCHAR(2048) NOT NULL, 
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

                tableCommand = @"CREATE TABLE IF NOT EXISTS VocabularyMappings (
	                    DictionaryId INTEGER,
	                    VocabularyId INTEGER,
	                    PRIMARY KEY (DictionaryId, VocabularyId)
                    )";
                createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteReader();
                createTable.Dispose();

                db.Close();
                db.Dispose();
            }
        }

        /// <summary>
        /// Add new empty vocabulary to process
        /// </summary>
        /// <param name="inputText"></param>
        /// <param name="wordId">To distinguish between meanings</param>
        /// <returns></returns>
        public static async Task<int> AddVocabularyAsync(string inputText, string wordId = null)
        {
            if (String.IsNullOrEmpty(inputText)) return 0;
            using (var context = new VocaDbContext())
            {
                var voca = new Vocabulary()
                {
                    Word = inputText.Trim()
                };
                if (!string.IsNullOrEmpty(wordId))
                    voca.WordId = wordId;

                context.Vocabularies.Add(voca);
                await context.SaveChangesAsync();
                return voca.Id;
            }
        }

        public static async Task<bool> AddVocabularyMappingAsync(int dicId, int vocaId)
        {
            using (var context = new VocaDbContext())
            {
                var voca = new VocabularyMapping()
                {
                    VocabularyId = vocaId,
                    DictionaryId = dicId
                };
                context.VocabularyMappings.AddOrUpdate(voca);
                return await context.SaveChangesAsync() > 0;
            }
        }

        public static async Task UpdateVocabularyAsync(Vocabulary item)
        {
            using (var context = new VocaDbContext())
            {
                await context.SingleUpdateAsync(item);
            }
        }

        public static async Task UpdateViewDateAsync(int _Id)
        {
            using (var context = new VocaDbContext())
            {
                var result = await context.Vocabularies
                    .Where(e => e.Id == _Id)
                    .UpdateFromQueryAsync(x => new Vocabulary()
                    {
                        ViewedDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    });
            }
        }

        public static async Task UpdateStatusAsync(int _Id, int _Status = 0)
        {
            if (CurrentVocabulary != null
                && CurrentVocabulary.Id == _Id
                && CurrentVocabulary.Status == _Status)
                return;

            using (var context = new VocaDbContext())
            {
                var result = await context.Vocabularies
                    .Where(e => e.Id == _Id)
                    .UpdateFromQueryAsync(x => new Vocabulary()
                    {
                        Status = _Status,
                        LearnedDate = _Status == 0 ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : 0,
                    });
                if (result > 0 && CurrentVocabulary != null && CurrentVocabulary.Id == _Id)
                {
                    CurrentVocabulary.Status = _Status;
                }
            }
        }

        public static async Task<Vocabulary> GetVocabularyByIdAsync(int Id)
        {
            if (CurrentVocabulary != null && CurrentVocabulary.Id == Id)
                return CurrentVocabulary;

            using (var context = new VocaDbContext())
            {
                CurrentVocabulary = await context.Vocabularies.FindAsync(Id);
                return CurrentVocabulary;
            }
        }

        public static async Task<List<Vocabulary>> GetVocabularyByIdsAsync(List<int> ids)
        {
            using (var context = new VocaDbContext())
                return await context.Vocabularies.Where(e => ids.Contains(e.Id)).ToListAsync();
        }

        public static async Task<Vocabulary> GetNextVocabularyAsync(int dicId, int Id)
        {
            using (var context = new VocaDbContext())
            {
                return CurrentVocabulary = await context.VocabularyMappings
                    .Where(e => e.DictionaryId == dicId && e.VocabularyId > Id && e.Vocabulary.Status == 1)
                    .OrderBy(e => e.VocabularyId)
                    .Select(x => x.Vocabulary)
                    .FirstOrDefaultAsync();
            }
        }

        public static async Task<Vocabulary> GetRandomVocabularyAsync(int dicId, int Id)
        {
            using (var context = new VocaDbContext())
            {
                return CurrentVocabulary = await context.VocabularyMappings
                    .Where(e => e.DictionaryId == dicId && e.VocabularyId != Id && e.Vocabulary.Status == 1)
                    .Select(x => x.Vocabulary)
                    .OrderBy(e => Guid.NewGuid())
                    .FirstOrDefaultAsync();
            }
        }

        public static async Task<Vocabulary> GetFirstVocabularyAsync(int dicId)
        {
            using (var context = new VocaDbContext())
            {
                return CurrentVocabulary = await context.VocabularyMappings
                    .Where(e => e.DictionaryId == dicId && e.Vocabulary.Status == 1)
                    .OrderBy(e => e.VocabularyId)
                    .Select(x => x.Vocabulary)
                    .FirstOrDefaultAsync();
            }
        }

        public static StatDtos GetStats(int dictionaryId = 0)
        {
            string dbpath = ApplicationIO.GetDatabasePath();
            StatDtos _Stats = new StatDtos();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                string cmd = @" SELECT
                                (SELECT COUNT(*) FROM Vocabulary) as Total,
                                (SELECT COUNT(*) FROM Vocabulary WHERE Status = 0) as Remembered,
                                (SELECT COUNT(*) FROM Vocabulary v
                                INNER JOIN VocabularyMappings vm ON v.Id = vm.VocabularyId
                                WHERE vm.DictionaryId = @dicId AND v.Status = 0) as DictionaryLearned,
                                (SELECT COUNT(*) FROM Vocabulary v
                                INNER JOIN VocabularyMappings vm ON v.Id = vm.VocabularyId
                                WHERE vm.DictionaryId = @dicId AND v.Status = 1) as DictionaryNotLearned
                                FROM Vocabulary LIMIT 1";

                db.Open();
                SqliteCommand selectCommand = new SqliteCommand(cmd, db);
                selectCommand.Parameters.AddWithValue("@dicId", dictionaryId);
                SqliteDataReader query = selectCommand.ExecuteReader();

                
                while (query.Read())
                {
                    _Stats.Total = int.Parse(query.GetString(0));
                    _Stats.Remembered = int.Parse(query.GetString(1));
                    _Stats.DictionaryLearned = int.Parse(query.GetString(2));
                    _Stats.DictionaryNotLearned = int.Parse(query.GetString(3));
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }
            return _Stats;
        }

        public static async Task<List<Dictionary>> GetDictionariesAsync()
        {
            using (var context = new VocaDbContext())
            {
                return await context.Dictionaries.Where(e => e.Status == 1).ToListAsync();
            }
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

        public static async Task<List<Vocabulary>> GetVocabulariesDueForReviewAsync(int dictionaryId = 0)
        {
            using (var context = new VocaDbContext())
            {
                var query = context.Vocabularies.AsQueryable();
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Apply dictionary filter if specified
                if (dictionaryId > 0)
                {
                    query = query.Where(v => context.VocabularyMappings
                        .Any(m => m.VocabularyId == v.Id && m.DictionaryId == dictionaryId));
                }

                // Get cards that:
                // 1. Have a next review date that's due (less than or equal to current time)
                // 2. Have been started in the SRS system (have an interval)
                // 3. Are not marked as learned (status = 1)
                return await query
                    .Where(v => v.NextReviewDate <= currentTime
                           && v.Interval != null
                           && v.Status == 1)
                    .OrderBy(v => v.NextReviewDate)
                    .ToListAsync();
            }
        }

        public static async Task<List<Vocabulary>> GetListLearndedAsync(bool? isRead, string searchContent, int dictionaryId = 0)
        {
            using (var context = new VocaDbContext())
            {
                var query = context.Vocabularies.AsQueryable();

                // Apply dictionary filter if specified
                if (dictionaryId > 0)
                {
                    query = query.Where(v => context.VocabularyMappings
                        .Any(m => m.VocabularyId == v.Id && m.DictionaryId == dictionaryId));
                }

                Expression<Func<Vocabulary, bool>> exp = x => true;
                if (isRead.HasValue)
                    exp = exp.And(e => e.Status == (isRead.Value ? 0 : 1));
                if (!string.IsNullOrEmpty(searchContent))
                    exp = exp.And(e => e.Word.Contains(searchContent.Trim()));

                return await query.Where(exp).ToListAsync();
            }
        }

        public static async Task<Vocabulary> GetVocabularyByWordAsync(string word)
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => e.Word == word.Trim()).FirstOrDefaultAsync();
            }
        }

        public static async Task<List<Vocabulary>> GetUnprocessVocabulariesAsync()
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => e.Ipa == null || e.Translate == string.Empty).ToListAsync();
            }
        }

        public static async Task<int> GetDictionaryIdByVocabularyIdAsync(int vocabularyId)
        {
            using (var context = new VocaDbContext())
            {
                return await context.VocabularyMappings.Where(e => e.VocabularyId == vocabularyId).Select(x => x.DictionaryId).FirstOrDefaultAsync();
            }
        }

        public static async Task<Vocabulary> GetVocabularyByWordIdAsync(string wordId)
        {
            using (var context = new VocaDbContext())
            {
                return await context.Vocabularies.Where(e => e.WordId == wordId).FirstOrDefaultAsync();
            }
        }

        public static async Task<List<EVVocabulary>> GetEVVocabulariesAsync()
        {
            using (var context = new DicEVContext())
            {
                return await context.Vocabularies.ToListAsync();
            }
        }

        public static async Task CleanUnableToGetAsync()
        {
            using (var context = new VocaDbContext())
            {
                var cleanWords = await context.Vocabularies.Where(e => e.Type == string.Empty && e.Ipa == null && (e.Translate == string.Empty)).ToListAsync();
                if (cleanWords.Any())
                {
                    context.Vocabularies.RemoveRange(cleanWords);
                    await context.SaveChangesAsync();
                }

                var orphanedMappings = await context.VocabularyMappings
                        .Where(vm => !context.Vocabularies.Any(v => v.Id == vm.VocabularyId))
                        .ToListAsync();
                if (orphanedMappings.Any())
                {
                    context.VocabularyMappings.RemoveRange(orphanedMappings);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
