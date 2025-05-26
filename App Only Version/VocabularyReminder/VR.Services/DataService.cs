using FAI.Core.Utilities.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VocabularyReminder.VR.Consts;
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

            using (var context = new VocaDbContext())
            {
                // Execute initialization script from application resources
                string scriptPath = ApplicationIO.GetInitDBScript();
                if (!File.Exists(scriptPath))
                {
                    throw new FileNotFoundException("Database initialization script not found", scriptPath);
                }

                // Create database
                context.Database.CreateIfNotExists();

                // Enable foreign keys
                context.Database.ExecuteSqlCommand("PRAGMA foreign_keys = ON");

                string[] commands = File.ReadAllText(scriptPath)
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string command in commands)
                {
                    if (!string.IsNullOrWhiteSpace(command))
                    {
                        context.Database.ExecuteSqlCommand(command);
                    }
                }
            }
        }

        ///// <summary>
        ///// Add new empty vocabulary to process
        ///// </summary>
        ///// <param name="inputText"></param>
        ///// <param name="wordId">To distinguish between meanings</param>
        ///// <returns></returns>
        //public static async Task<int> AddVocabularyAsync(string inputText, string wordId = null)
        //{
        //    if (String.IsNullOrEmpty(inputText)) return 0;
        //    using (var context = new VocaDbContext())
        //    {
        //        var voca = new Vocabulary()
        //        {
        //            Word = inputText.Trim()
        //        };
        //        if (!string.IsNullOrEmpty(wordId))
        //            voca.WordId = wordId;

        //        context.Vocabularies.Add(voca);
        //        await context.SaveChangesAsync();
        //        return voca.Id;
        //    }
        //}

        /// <summary>
        /// Add new vocabulary with extended properties (for sentences, translations, etc.)
        /// </summary>
        /// <param name="inputText">The word or sentence</param>
        /// <param name="translation">Translation of the word/sentence</param>
        /// <param name="type">Type of vocabulary (word, sentence, etc.)</param>
        /// <param name="wordId">To distinguish between meanings</param>
        /// <returns>Vocabulary ID if successful, 0 if failed</returns>
        public static async Task<int> AddVocabularyAsync(string inputText, string translation = null, string type = null, string wordId = null)
        {
            if (String.IsNullOrEmpty(inputText)) return 0;
            using (var context = new VocaDbContext())
            {
                var voca = new Vocabulary()
                {
                    Word = inputText.Trim(),
                    Type = type ?? "word",
                    Translate = translation?.Trim(),
                    CreatedDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
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
            // Set default dic to "Default"
            if (dicId < 1)
                dicId = (int)DictionaryConsts.Default;

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
                var query = context.Vocabularies.AsQueryable();
                // Apply dictionary filter if specified
                if (dicId > 0)
                {
                    query = query.Where(v => context.VocabularyMappings
                        .Any(m => m.VocabularyId == v.Id && m.DictionaryId == dicId));
                }

                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return CurrentVocabulary = await query
                    .Where(v => v.Id != Id && v.Status == 1
                        && (v.NextReviewDate == null || v.Interval == null))
                    .OrderBy(e => Guid.NewGuid())
                    .FirstOrDefaultAsync();
            }
        }

        public static async Task<Vocabulary> GetFirstVocabularyAsync(int dicId)
        {
            using (var context = new VocaDbContext())
            {
                var query = context.Vocabularies.AsQueryable();
                // Apply dictionary filter if specified
                if (dicId > 0)
                {
                    query = query.Where(v => context.VocabularyMappings
                        .Any(m => m.VocabularyId == v.Id && m.DictionaryId == dicId));
                }

                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return CurrentVocabulary = await query
                    .Where(v => v.Status == 1
                        && (v.NextReviewDate == null || v.Interval == null))
                    .OrderBy(e => e.Id)
                    .FirstOrDefaultAsync();
            }
        }

        public static StatDtos GetStats(int dictionaryId = 0)
        {
            try
            {
                using (var context = new VocaDbContext())
                {
                    var stats = new StatDtos
                    {
                        Total = context.Vocabularies.Where(e => e.Status == 1).Count(),
                        Remembered = context.Vocabularies.Count(v => v.Status == 1 && v.ReviewCount > 0)
                    };

                    if (dictionaryId > 0)
                    {
                        stats.DictionaryLearned = context.VocabularyMappings
                            .Count(vm => vm.DictionaryId == dictionaryId && vm.Vocabulary.Status == 1 && vm.Vocabulary.ReviewCount > 0);
                        var totalInDic = context.VocabularyMappings
                            .Count(vm => vm.DictionaryId == dictionaryId && vm.Vocabulary.Status == 1);
                        stats.DictionaryNotLearned = totalInDic - stats.DictionaryLearned;
                    } else
                    {
                        stats.DictionaryLearned = stats.Remembered;
                        stats.DictionaryNotLearned = stats.Total - stats.DictionaryLearned;
                    }

                    return stats;
                }
            }
            catch (Exception) { }

            return new StatDtos {
                Total = 0,
                Remembered = 0,
                DictionaryLearned = 0,
                DictionaryNotLearned = 0
            };
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

        public static async Task<List<Vocabulary>> GetListVocabularyToTranslateAsync(int dicId)
        {
            using (var context = new VocaDbContext())
            {
                var query = context.Vocabularies.AsQueryable();
                // Apply dictionary filter if specified
                if (dicId > 0)
                {
                    query = query.Where(v => context.VocabularyMappings
                        .Any(m => m.VocabularyId == v.Id && m.DictionaryId == dicId));
                }
                return await query.Where(e => string.IsNullOrEmpty(e.Translate)).ToListAsync();
            }
        }


        public static async Task<List<Vocabulary>> GetListVocabularyToGetDefineExampleMp3URLAsync(int dicId)
        {
            using (var context = new VocaDbContext())
            {
                var query = context.Vocabularies.AsQueryable();
                // Apply dictionary filter if specified
                if (dicId > 0)
                {
                    query = query.Where(v => context.VocabularyMappings
                        .Any(m => m.VocabularyId == v.Id && m.DictionaryId == dicId));
                }
                return await query.Where(e => e.PlayURL == null || e.Translate == string.Empty).ToListAsync();
            }
        }

        public static async Task<List<Vocabulary>> GetListVocabularyToGetRelatedWordsAsync(int dicId)
        {
            using (var context = new VocaDbContext())
            {
                var query = context.Vocabularies.AsQueryable();
                // Apply dictionary filter if specified
                if (dicId > 0)
                {
                    query = query.Where(v => context.VocabularyMappings
                        .Any(m => m.VocabularyId == v.Id && m.DictionaryId == dicId));
                }
                return await query.Where(e => string.IsNullOrEmpty(e.Related)).ToListAsync();
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

        public static async Task<List<Vocabulary>> GetBackupAsync()
        {
            using (var context = new VocaDbContext())
            {
                var query = context.Vocabularies.AsQueryable();

                Expression<Func<Vocabulary, bool>> exp = x => true;
                exp = exp.And(e => e.Status == 0 || e.NextReviewDate.HasValue || e.Interval.HasValue || e.ReviewCount.HasValue || e.LapseCount.HasValue || e.EaseFactor.HasValue);
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
