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
{    public static class SpacedRepetitionService
    {
        // Quality ratings from 1-4:
        // 1: Again (Complete blackout - Start over)
        // 2: Hard (Significant difficulty - Reduce interval)
        // 3: Good (Some hesitation but correct - Normal progression)
        // 4: Easy (Perfect recall - Increase interval)
        private const double MIN_EASE = 1.3;
        private const double HARD_FACTOR = 1.2;     // Multiplier for Hard rating
        private const double GOOD_FACTOR = 2.5;     // Multiplier for Good rating
        private const double EASY_FACTOR = 3.5;     // Multiplier for Easy rating
        private const int GRADUATING_INTERVAL = 1;  // 1 day (SM-2 default graduation interval)
        private const int REVIEW_LAPSE_DELAY = 20;  // Minutes to wait after a review card lapses
        private static readonly int[] LEARNING_STEPS = { 2, 10 };  // Minutes: 1min, 10min

        public static async Task<Vocabulary> LoadVocabulariesForReview(int dictionaryId = 0)
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

                // Get the most overdue card that:
                // 1. Has a next review date that's due (less than or equal to current time)
                // 2. Has been started in the SRS system (has an interval)
                // 3. Is not marked as learned (status = 1)
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return await query
                    .Where(v => v.NextReviewDate <= currentTime
                           && v.Interval != null
                           && v.Status == 1)
                    .OrderBy(v => v.NextReviewDate)
                    .FirstOrDefaultAsync();
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

            // Anki-like handling of new cards
            switch (quality)
            {
                case 1: // Again - Complete blackout
                    // Reset to first learning step (1 minute)
                    vocabulary.Interval = 0;
                    vocabulary.NextReviewDate = DateTime.Now.AddMinutes(LEARNING_STEPS[0]).ToUnixTimeInSeconds();
                    break;
                    
                case 2: // Hard - Significant difficulty
                    // Move to second learning step (10 minutes)
                    vocabulary.Interval = 0;
                    vocabulary.NextReviewDate = DateTime.Now.AddMinutes(LEARNING_STEPS[1]).ToUnixTimeInSeconds();
                    break;
                    
                case 3: // Good - Normal progression
                    // Graduate to review queue with 1 day interval
                    vocabulary.Interval = GRADUATING_INTERVAL;
                    vocabulary.NextReviewDate = DateTime.Now.AddDays(GRADUATING_INTERVAL).ToUnixTimeInSeconds();
                    // Use default ease factor (2.5)
                    break;
                    
                case 4: // Easy - Perfect recall
                    // Graduate directly to review queue with longer interval (default 4 days in Anki)
                    vocabulary.Interval = GRADUATING_INTERVAL * 4;
                    vocabulary.NextReviewDate = DateTime.Now.AddDays(vocabulary.Interval.Value).ToUnixTimeInSeconds();
                    // Increase ease factor slightly for easy cards
                    vocabulary.EaseFactor = 2.5 + 0.15;
                    break;
            }
            
            // Update ease factor based on performance (except for "Again" which doesn't change EF for new cards)
            if (quality >= 2)
            {
                // Initialize EF according to quality per SM-2, but only for ratings 2+
                vocabulary.EaseFactor = Math.Max(MIN_EASE, vocabulary.EaseFactor.Value + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)));
            }
        }        
        
        private static void ProcessReviewCard(Vocabulary vocabulary, int quality)
        {
            vocabulary.ReviewCount++;

            switch (quality)
            {       
                case 1: // Again - Complete blackout
                    // Card lapses - but use a longer delay for review cards (30 minutes instead of 1 minute)
                    vocabulary.LapseCount++;
                    vocabulary.Interval = 0;  // Reset interval
                    vocabulary.NextReviewDate = DateTime.Now.AddMinutes(REVIEW_LAPSE_DELAY).ToUnixTimeInSeconds();
                    
                    // Decrease EF but not below minimum (Anki default: -0.20)
                    vocabulary.EaseFactor = Math.Max(MIN_EASE, vocabulary.EaseFactor.Value - 0.20);
                    break;
                    
                case 2: // Hard - Significant difficulty
                    // Anki applies 1.2x the previous interval for "Hard"
                    int hardInterval = Math.Max(1, (int)Math.Ceiling(vocabulary.Interval.Value * HARD_FACTOR));
                    vocabulary.Interval = hardInterval;
                    vocabulary.NextReviewDate = DateTime.Now.AddDays(hardInterval).ToUnixTimeInSeconds();
                    
                    // Decrease EF slightly but not below minimum (Anki default: -0.15)
                    vocabulary.EaseFactor = Math.Max(MIN_EASE, vocabulary.EaseFactor.Value - 0.15);
                    break;
                    
                case 3: // Good - Normal progression
                    // Update ease factor using SM-2 formula
                    double goodEF = vocabulary.EaseFactor.Value + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
                    vocabulary.EaseFactor = Math.Max(MIN_EASE, goodEF);
                    
                    // Calculate new interval: current interval * EF
                    int goodInterval = (int)Math.Ceiling(vocabulary.Interval.Value * GOOD_FACTOR);
                    vocabulary.Interval = goodInterval;
                    vocabulary.NextReviewDate = DateTime.Now.AddDays(goodInterval).ToUnixTimeInSeconds();
                    break;
                    
                case 4: // Easy - Perfect recall
                    // Update ease factor and increase it more for "Easy" ratings
                    double easyEF = vocabulary.EaseFactor.Value + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02)) + 0.15;
                    vocabulary.EaseFactor = Math.Max(MIN_EASE, easyEF);
                    
                    // Calculate new interval: current interval * larger multiplier
                    int easyInterval = (int)Math.Ceiling(vocabulary.Interval.Value * EASY_FACTOR);
                    vocabulary.Interval = easyInterval;
                    vocabulary.NextReviewDate = DateTime.Now.AddDays(easyInterval).ToUnixTimeInSeconds();
                    break;
            }
        }
    }
}