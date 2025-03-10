using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VR.Domain.Models
{
    [Table("Vocabulary")]
    public class Vocabulary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(2048)]
        public string Word { get; set; }

        //public string Data { get; set; }

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

        [NotMapped]
        public ExtendedWordData JsonData { get; private set; }


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

        public int? Status { get; set; } = 1;  // default value
        public long? ViewedDate { get; set; }  // Unix timestamp in seconds
        public long? LearnedDate { get; set; }  // Unix timestamp in seconds
        public long? CreatedDate { get; set; }  // Unix timestamp in seconds
        public long? NextReviewDate { get; set; }
        public double? EaseFactor { get; set; } = 2.5;  // Starting ease factor
        public int? Interval { get; set; } = 0;  // Current interval in days
        public int? ReviewCount { get; set; } = 0;  // Number of reviews
        public int? LapseCount { get; set; } = 0;  // Number of times card went from learned to unlearned

        public virtual ICollection<Dictionary> Dictionaries { get; set; }
    }

    public class ExtendedWordData
    {
        public string ID { get; set; }
        public string Source { get; set; }
        public string Level { get; set; }
        public string Type { get; set; }
        public List<DefinitionDto> Definitions { get; set; } = new List<DefinitionDto>();
        public List<IdiomDataDto> Idioms { get; set; }
        public string Ipa { get; set; }
        public string Ipa2 { get; set; }
        public string Audio { get; set; }
        public string Audio2 { get; set; }
    }

    public class DefinitionDto
    {
        public string PartOfSpeech { get; set; }
        public string Level { get; set; }
        public string Definition { get; set; }
        public List<ExampleDto> Examples { get; set; }
        public string Topic { get; set; }
    }

    public class ExampleDto
    {
        public string Struct { get; set; }
        public string Example { get; set; }
    }

    public class IdiomDataDto
    {
        public string Phrase { get; set; }
        public string Level { get; set; }
        public string Definition { get; set; }
        public List<string> Examples { get; set; }
        public List<string> Labels { get; set; }
    }
}
