using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VR.Domain.Models
{
    [Table("Dictionary")]
    public class Dictionary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [MaxLength(2048)]
        public string Name { get; set; }
        [MaxLength(2048)]
        public string Description { get; set; }
        public int? Status { get; set; }

        public virtual ICollection<Vocabulary> Vocabularies { get; set; }
    }
}
