using System;
using System.Collections.Generic;

namespace VR.Dto
{
    public class SpacedRepetitionStatsDto
    {
        public int TotalWords { get; set; }
        public int NewWords { get; set; }  // Words with Interval = 0 or null
        public int LearningWords { get; set; }  // Words with Interval > 0 and Status = 1
        public int LearnedWords { get; set; }  // Words with Status = 2
        public int DueWords { get; set; }  // Words due for review
        public double AverageEaseFactor { get; set; }
        public int TotalReviews { get; set; }
        public int TotalLapses { get; set; }
        public List<IntervalDistributionDto> IntervalDistribution { get; set; } = new List<IntervalDistributionDto>();
        public List<ReviewCountOverTimeDto> ReviewHistory { get; set; } = new List<ReviewCountOverTimeDto>();
        public List<EaseFactorDistributionDto> EaseFactorDistribution { get; set; } = new List<EaseFactorDistributionDto>();
    }

    public class IntervalDistributionDto
    {
        public string IntervalRange { get; set; }
        public int WordCount { get; set; }
    }

    public class ReviewCountOverTimeDto
    {
        public DateTime Date { get; set; }
        public int ReviewCount { get; set; }
        public int NewCount { get; set; }
        public int LapseCount { get; set; }
    }

    public class EaseFactorDistributionDto
    {
        public string EaseRange { get; set; }
        public int WordCount { get; set; }
    }
}