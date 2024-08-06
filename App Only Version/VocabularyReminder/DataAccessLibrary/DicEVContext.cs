using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SQLite;

namespace VocabularyReminder.DataAccessLibrary
{
    public class DicEVContext : DbContext
    {
        public DicEVContext() : base(new SQLiteConnection()
        {
            ConnectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = ApplicationIO.GetEVDatabasePath(),
                ForeignKeys = true
            }.ConnectionString,
        }, true)
        {
            Database.SetInitializer<DicEVContext>(null);
            Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
        }

        public DbSet<EVVocabulary> Vocabularies { get; set; }
    }

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