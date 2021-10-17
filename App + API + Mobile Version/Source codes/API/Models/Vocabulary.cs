using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VocabularyReminderAPI.Models
{
    public partial class Vocabulary
    {
        public int Id { get; set; }
        [StringLength(255)]
        public string Word { get; set; }
        [StringLength(100)]
        public string Type { get; set; }
        [StringLength(100)]
        public string Ipa { get; set; }
        [StringLength(100)]
        public string Ipa2 { get; set; }
        [StringLength(2000)]
        public string Translate { get; set; }
        [StringLength(2000)]
        public string Define { get; set; }
        [StringLength(2000)]
        public string Example { get; set; }
        [StringLength(2000)]
        public string Example2 { get; set; }
        [Column("PlayURL")]
        [StringLength(2000)]
        public string PlayUrl { get; set; }
        [Column("PlayURL2")]
        [StringLength(2000)]
        public string PlayUrl2 { get; set; }
        [StringLength(2000)]
        public string Related { get; set; }
        public int Status { get; set; }
        public int Idx { get; set; }
        [Required]
        [StringLength(450)]
        public string UserId { get; set; }
    }
}
