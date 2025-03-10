using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using VR.Domain;
using VR.Domain.Models;
using VR.Dto;
using VR.Utils;

namespace VR.Services
{
    public static class SpacedRepetitionService
    {
        // Quality ratings from 1-4:
        // 1: Again (Complete blackout - Start over)
        // 2: Hard (Significant difficulty - Reduce interval)
        // 3: Good (Some hesitation but correct - Normal progression)
        // 4: Easy (Perfect recall - Increase interval)

        private const double MIN_EASE = 1.3;
        private const int GRADUATING_INTERVAL = 1;  // 1 day (SM-2 default graduation interval)
        private static readonly int[] LEARNING_STEPS = { 1, 10 };  // Minutes: 1min, 10min

        public static async Task<List<Vocabulary>> LoadVocabulariesForReview(int dictionaryId = 0)
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

                // Get cards that:
                // 1. Have a next review date that's due (less than or equal to current time)
                // 2. Have been started in the SRS system (have an interval)
                // 3. Are not marked as learned (status = 1)
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var dueVocabularies = await query
                    .Where(v => v.NextReviewDate <= currentTime
                           && v.Interval != null
                           && v.Status == 1)
                    .OrderBy(v => v.NextReviewDate)
                    .ToListAsync();

                return dueVocabularies;
            }
        }

        public static bool IsDueForReview(VocabularyDisplayDto vocabulary)
        {
            if (vocabulary == null)
                return false;

            if (vocabulary.NextReviewDate == null || vocabulary.Interval == null)
                return true;

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return vocabulary.NextReviewDate <= currentTime && vocabulary.Status == 1;
        }

        public static void ProcessReview(Vocabulary vocabulary, int quality)
        {
            if (vocabulary.Interval == null || vocabulary.Interval == 0)
            {
                ProcessNewCard(vocabulary, quality);
            }
            else
            {
                ProcessReviewCard(vocabulary, quality);
            }
        }

        private static void ProcessNewCard(Vocabulary vocabulary, int quality)
        {
            // Initialize if first review
            if (vocabulary.ReviewCount == null) vocabulary.ReviewCount = 0;
            if (vocabulary.EaseFactor == null) vocabulary.EaseFactor = 2.5;
            if (vocabulary.LapseCount == null) vocabulary.LapseCount = 0;

            vocabulary.ReviewCount++;

            if (quality < 3)  // If rating is Again(1) or Hard(2)
            {
                // Reset to first learning step
                vocabulary.Interval = 0;
                vocabulary.NextReviewDate = DateTime.Now.AddMinutes(LEARNING_STEPS[0]).ToUnixTimeInSeconds();
            }
            else // Good(3) or Easy(4)
            {
                // Graduate with 1 day interval per SM-2
                vocabulary.Interval = GRADUATING_INTERVAL;
                vocabulary.NextReviewDate = DateTime.Now.AddDays(GRADUATING_INTERVAL).ToUnixTimeInSeconds();
                
                // Initialize EF according to quality per SM-2
                vocabulary.EaseFactor = 2.5 + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
            }
        }

        private static void ProcessReviewCard(Vocabulary vocabulary, int quality)
        {
            vocabulary.ReviewCount++;

            if (quality < 3)  // Failed card (Again or Hard)
            {
                // Card lapses - return to learning steps
                vocabulary.LapseCount++;
                vocabulary.Interval = 0;  // Reset interval
                vocabulary.NextReviewDate = DateTime.Now.AddMinutes(LEARNING_STEPS[0]).ToUnixTimeInSeconds();
                
                // Decrease EF but not below minimum
                vocabulary.EaseFactor = Math.Max(MIN_EASE, vocabulary.EaseFactor.Value - 0.2);
            }
            else  // Successful recall (Good or Easy)
            {
                // Update EF according to SM-2 formula
                double newEF = vocabulary.EaseFactor.Value + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
                vocabulary.EaseFactor = Math.Max(MIN_EASE, newEF);

                // Calculate new interval: I(n) = I(n-1) * EF
                vocabulary.Interval = (int)Math.Round(vocabulary.Interval.Value * vocabulary.EaseFactor.Value);
                
                // Set next review date
                vocabulary.NextReviewDate = DateTime.Now.AddDays(vocabulary.Interval.Value).ToUnixTimeInSeconds();
            }
        }
    }
}