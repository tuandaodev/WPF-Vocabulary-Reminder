using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VR.Domain.Models
{
    [Table("Vocabulary")]
    public class Vocabulary : INotifyPropertyChanged
    {
        private bool _isDueForReview;
        private long? _nextReviewDate;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [NotMapped]
        public bool IsDueForReview
        {
            get => _isDueForReview;
            set
            {
                if (_isDueForReview != value)
                {
                    _isDueForReview = value;
                    OnPropertyChanged(nameof(IsDueForReview));
                }
            }
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(2048)]
        public string Word { get; set; }

        public string Data { get; set; }

        [MaxLength(100)]
        public string Type { get; set; }

        [MaxLength(100)]
        public string Ipa { get; set; }

        [MaxLength(100)]
        public string Ipa2 { get; set; }

        [MaxLength(2048)]
        public string Translate { get; set; }

        [MaxLength(2048)]
        public string Define { get; set; }

        [MaxLength(2048)]
        public string Example { get; set; }

        [MaxLength(2048)]
        public string Example2 { get; set; }

        [MaxLength(2048)]
        public string PlayURL { get; set; }

        [MaxLength(2048)]
        public string PlayURL2 { get; set; }

        [MaxLength(2048)]
        public string Related { get; set; }

        public int? Status { get; set; } = 1;
        public long? ViewedDate { get; set; }
        public long? LearnedDate { get; set; }
        public long? CreatedDate { get; set; }

        public long? NextReviewDate
        {
            get => _nextReviewDate;
            set
            {
                if (_nextReviewDate != value)
                {
                    _nextReviewDate = value;
                    NextReviewDateDisplay = _nextReviewDate.HasValue
                        ? DateTimeOffset.FromUnixTimeSeconds(_nextReviewDate.Value).LocalDateTime.ToString("yyyy-MM-dd HH:mm")
                        : "";
                    OnPropertyChanged(nameof(NextReviewDate));
                    OnPropertyChanged(nameof(NextReviewDateDisplay));
                }
            }
        }

        [NotMapped]
        public string NextReviewDateDisplay { get; private set; }
        public double? EaseFactor { get; set; } = 2.5;
        public int? Interval { get; set; } = 0;
        public int? ReviewCount { get; set; } = 0;
        public int? LapseCount { get; set; } = 0;

        public virtual ICollection<Dictionary> Dictionaries { get; set; }
    }
}