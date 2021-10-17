using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VocabularyReminderAPI.Models
{
    public partial class Dictionary
    {
        public int DictionaryId { get; set; }
        [Required]
        [StringLength(50)]
        public string DictionaryCode { get; set; }
        [Required]
        [StringLength(255)]
        public string DictionaryName { get; set; }
        public bool? IsActive { get; set; }
        public int CreatedBy { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime CreatedDate { get; set; }
    }
}
