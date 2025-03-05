using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using VR.Utils;
using VR.Domain;
using VR.Domain.Models;

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
        private const int GRADUATING_INTERVAL = 1;  // 1 day
        private const int EASY_INTERVAL = 4;        // 4 days
        private const int MINIMUM_INTERVAL = 1;     // 1 day
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

        public static bool IsDueForReview(Vocabulary vocabulary)
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

            if (quality == 1)  // Again
            {
                // Reset to first learning step
                vocabulary.NextReviewDate = DateTime.Now.AddMinutes(LEARNING_STEPS[0]).ToUnixTimeInSeconds();
            }
            else if (quality == 2)  // Hard
            {
                // Graduate but with shorter interval
                vocabulary.Interval = GRADUATING_INTERVAL;
                vocabulary.NextReviewDate = DateTime.Now.AddDays(GRADUATING_INTERVAL).ToUnixTimeInSeconds();
            }
            else if (quality == 3)  // Good
            {
                // Normal graduation
                vocabulary.Interval = GRADUATING_INTERVAL;
                vocabulary.NextReviewDate = DateTime.Now.AddDays(GRADUATING_INTERVAL).ToUnixTimeInSeconds();
            }
            else if (quality == 4)  // Easy
            {
                // Skip learning steps, go straight to easy interval
                vocabulary.Interval = EASY_INTERVAL;
                vocabulary.NextReviewDate = DateTime.Now.AddDays(EASY_INTERVAL).ToUnixTimeInSeconds();
            }
        }

        private static void ProcessReviewCard(Vocabulary vocabulary, int quality)
        {
            vocabulary.ReviewCount++;

            if (quality == 1)  // Again
            {
                // Card lapses
                vocabulary.LapseCount++;
                vocabulary.Interval = MINIMUM_INTERVAL;
                vocabulary.EaseFactor = Math.Max(MIN_EASE, vocabulary.EaseFactor.Value - 0.2);
                vocabulary.NextReviewDate = DateTime.Now.AddDays(MINIMUM_INTERVAL).ToUnixTimeInSeconds();
            }
            else 
            {
                // Calculate new interval
                double intervalModifier = 1.0;
                if (quality == 2) intervalModifier = 0.8;  // Hard reduces interval
                if (quality == 4) intervalModifier = 1.3;  // Easy increases interval

                // Update ease factor
                if (quality == 2)  // Hard
                    vocabulary.EaseFactor = Math.Max(MIN_EASE, vocabulary.EaseFactor.Value - 0.15);
                else if (quality == 3)  // Good
                    vocabulary.EaseFactor = vocabulary.EaseFactor;  // No change
                else if (quality == 4)  // Easy
                    vocabulary.EaseFactor = Math.Max(MIN_EASE, vocabulary.EaseFactor.Value + 0.15);

                // Calculate new interval
                double newInterval = vocabulary.Interval.Value * vocabulary.EaseFactor.Value * intervalModifier;
                vocabulary.Interval = (int)Math.Round(newInterval);

                // Set next review date
                vocabulary.NextReviewDate = DateTime.Now.AddDays(vocabulary.Interval.Value).ToUnixTimeInSeconds();
            }
        }
    }
}