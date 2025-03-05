using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VR.Domain.Models
{
    [Table("VocabularyMappings")]
    public class VocabularyMapping
    {
        [Key, Column(Order = 0)]
        public int DictionaryId { get; set; }

        [Key, Column(Order = 1)]
        public int VocabularyId { get; set; }

        [ForeignKey("DictionaryId")]
        public virtual Dictionary Dictionary { get; set; }

        [ForeignKey("VocabularyId")]
        public virtual Vocabulary Vocabulary { get; set; }
    }
}