using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VR.Domain.Models
{
    [Table("av")]
    public class EVVocabulary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [MaxLength(100)]
        [Column("word")]
        public string Word { get; set; }

        [MaxLength(100)]
        [Column("Pronounce")]
        public string Pronounce { get; set; }

        [MaxLength(2048)]
        [Column("html")]
        public string Html { get; set; }

        [MaxLength(2048)]
        [Column("description")]
        public string Description { get; set; }
    }
}