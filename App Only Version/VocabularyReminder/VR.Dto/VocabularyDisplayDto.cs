using Newtonsoft.Json;
using VR.Domain.Models;

namespace VR.Dto
{
    public class VocabularyDisplayDto
    {
        public int Id { get; set; }
        public string Word { get; set; }
        
        private string _data;
        public string Data
        {
            get => _data;
            set
            {
                if (_data != value)
                {
                    _data = value;
                    JsonData = JsonConvert.DeserializeObject<ExtendedWordData>(_data);
                }
            }
        }
        public ExtendedWordData JsonData { get; private set; }
        public string Type { get; set; }
        public string Ipa { get; set; }
        public string Ipa2 { get; set; }
        public string Translate { get; set; }
        public string Define { get; set; }
        public string Example { get; set; }
        public string Example2 { get; set; }
        public string PlayURL { get; set; }
        public string PlayURL2 { get; set; }
        public string Related { get; set; }
        public int? Status { get; set; } = 1;  // default value
        public long? ViewedDate { get; set; }  // Unix timestamp in seconds
        public long? LearnedDate { get; set; }  // Unix timestamp in seconds
        public long? CreatedDate { get; set; }  // Unix timestamp in seconds
        public long? NextReviewDate { get; set; }
        public double? EaseFactor { get; set; } = 2.5;  // Starting ease factor
        public int? Interval { get; set; } = 0;  // Current interval in days
        public int? ReviewCount { get; set; } = 0;  // Number of reviews
        public int? LapseCount { get; set; } = 0;  // Number of times card went from learned to unlearned
        public bool IsDueForReview { get; set; }
        public string NextReviewDateDisplay { get; set; }
    }
}