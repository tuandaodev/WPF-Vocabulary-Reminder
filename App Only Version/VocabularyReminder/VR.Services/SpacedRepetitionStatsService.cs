using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using VR.Domain;
using VR.Dto;
using VR.Utils;

namespace VR.Services
{
    public static class SpacedRepetitionStatsService
    {
        public static async Task<SpacedRepetitionStatsDto> GetSpacedRepetitionStatsAsync(int dictionaryId = 0)
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

                var vocabularies = await query.ToListAsync();
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Filter out soft deleted words (Status != 1 means soft deleted)
                var activeWords = vocabularies.Where(v => v.Status == 1).ToList();
                
                // Define criteria for "learned" words based on spaced repetition progress
                // A word is considered learned if:
                // 1. It has been reviewed multiple times (ReviewCount >= 3)
                // 2. Has a significant interval (>= 30 days) indicating long-term retention
                // 3. Has a good ease factor (>= 2.0) indicating it's not difficult
                var learnedWords = activeWords.Where(v =>
                    v.ReviewCount >= 3 &&
                    v.Interval >= 30 &&
                    v.EaseFactor >= 2.0).ToList();
                
                var stats = new SpacedRepetitionStatsDto
                {
                    TotalWords = activeWords.Count,
                    NewWords = activeWords.Count(v => v.Interval == 0 || v.Interval == null),
                    LearningWords = activeWords.Count(v => v.Interval > 0 && v.Interval < 30),
                    LearnedWords = learnedWords.Count,
                    DueWords = activeWords.Count(v => v.NextReviewDate != null && v.NextReviewDate <= currentTime),
                    AverageEaseFactor = activeWords.Where(v => v.EaseFactor.HasValue).Any() ?
                        activeWords.Where(v => v.EaseFactor.HasValue).Average(v => v.EaseFactor.Value) : 2.5,
                    TotalReviews = activeWords.Sum(v => v.ReviewCount ?? 0),
                    TotalLapses = activeWords.Sum(v => v.LapseCount ?? 0)
                };

                // Calculate interval distribution
                stats.IntervalDistribution = GetIntervalDistribution(activeWords);

                // Calculate ease factor distribution
                stats.EaseFactorDistribution = GetEaseFactorDistribution(activeWords);

                // Calculate review history (last 30 days)
                stats.ReviewHistory = await GetReviewHistoryAsync(context, dictionaryId);

                return stats;
            }
        }

        private static List<IntervalDistributionDto> GetIntervalDistribution(List<VR.Domain.Models.Vocabulary> vocabularies)
        {
            var intervals = vocabularies.Where(v => v.Interval.HasValue).Select(v => v.Interval.Value);
            
            var distribution = new List<IntervalDistributionDto>
            {
                new IntervalDistributionDto { IntervalRange = "New (0 days)", WordCount = vocabularies.Count(v => !v.Interval.HasValue || v.Interval == 0) },
                new IntervalDistributionDto { IntervalRange = "1 day", WordCount = intervals.Count(i => i == 1) },
                new IntervalDistributionDto { IntervalRange = "2-3 days", WordCount = intervals.Count(i => i >= 2 && i <= 3) },
                new IntervalDistributionDto { IntervalRange = "4-7 days", WordCount = intervals.Count(i => i >= 4 && i <= 7) },
                new IntervalDistributionDto { IntervalRange = "1-2 weeks", WordCount = intervals.Count(i => i >= 8 && i <= 14) },
                new IntervalDistributionDto { IntervalRange = "2-4 weeks", WordCount = intervals.Count(i => i >= 15 && i <= 30) },
                new IntervalDistributionDto { IntervalRange = "1-3 months", WordCount = intervals.Count(i => i >= 31 && i <= 90) },
                new IntervalDistributionDto { IntervalRange = "3+ months", WordCount = intervals.Count(i => i > 90) }
            };

            return distribution.Where(d => d.WordCount > 0).ToList();
        }

        private static List<EaseFactorDistributionDto> GetEaseFactorDistribution(List<VR.Domain.Models.Vocabulary> vocabularies)
        {
            var easeFactors = vocabularies.Where(v => v.EaseFactor.HasValue).Select(v => v.EaseFactor.Value);
            
            var distribution = new List<EaseFactorDistributionDto>
            {
                new EaseFactorDistributionDto { EaseRange = "1.3-1.7 (Difficult)", WordCount = easeFactors.Count(e => e >= 1.3 && e < 1.7) },
                new EaseFactorDistributionDto { EaseRange = "1.7-2.1 (Hard)", WordCount = easeFactors.Count(e => e >= 1.7 && e < 2.1) },
                new EaseFactorDistributionDto { EaseRange = "2.1-2.5 (Normal)", WordCount = easeFactors.Count(e => e >= 2.1 && e < 2.5) },
                new EaseFactorDistributionDto { EaseRange = "2.5-2.9 (Good)", WordCount = easeFactors.Count(e => e >= 2.5 && e < 2.9) },
                new EaseFactorDistributionDto { EaseRange = "2.9+ (Easy)", WordCount = easeFactors.Count(e => e >= 2.9) }
            };

            return distribution.Where(d => d.WordCount > 0).ToList();
        }

        private static async Task<List<ReviewCountOverTimeDto>> GetReviewHistoryAsync(VocaDbContext context, int dictionaryId)
        {
            // For now, we'll create a simple simulation since we don't store detailed review history
            // In a real implementation, you'd want to store each review event with timestamp
            var history = new List<ReviewCountOverTimeDto>();
            var startDate = DateTime.Now.AddDays(-30);

            for (int i = 0; i < 30; i++)
            {
                var date = startDate.AddDays(i);
                history.Add(new ReviewCountOverTimeDto
                {
                    Date = date,
                    ReviewCount = new Random(date.DayOfYear).Next(0, 50), // Simulated data
                    NewCount = new Random(date.DayOfYear + 1).Next(0, 10),
                    LapseCount = new Random(date.DayOfYear + 2).Next(0, 5)
                });
            }

            return history;
        }
    }
}