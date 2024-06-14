using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.SQLite;
using System.Data.SQLite.EF6;

namespace VocabularyReminder.DataAccessLibrary
{
    public class VocaDbContext : DbContext
    {
        public VocaDbContext() : base(new SQLiteConnection()
        {
            ConnectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = ApplicationIO.GetDatabasePath(),
                ForeignKeys = true
            }.ConnectionString
        }, true)
        {
            Database.SetInitializer<VocaDbContext>(null);
        }

        //public DbSet<CategoryModel> Categories { get; set; }
        public DbSet<Vocabulary> Vocabularies { get; set; }
    }

    public class SQLiteConfiguration : DbConfiguration
    {
        public SQLiteConfiguration()
        {
            SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
            SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
            SetProviderServices("System.Data.SQLite", (DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices)));
        }
    }

    [Table("Vocabulary")]
    public class Vocabulary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(2048)]
        public string Word { get; set; }

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
    }

    //public class VocabularyModel
    //{
    //    public int Id { get; set; }
    //    public string Word { get; set; }
    //    public string Type { get; set; }
    //    public string Ipa { get; set; }
    //    public string Ipa2 { get; set; }
    //    public string Translate { get; set; }
    //    public string Define { get; set; }
    //    public string Example { get; set; }
    //    public string Example2 { get; set; }
    //    public string PlayURL { get; set; }
    //    public string PlayURL2 { get; set; }
    //    public string Related { get; set; }
    //    public int Status { get; set; }
    //}

    //public class CategoryModel
    //{
    //    public int CategoryId { get; set; }
    //    public string CategoryName { get; set; }
    //    public DateTime CreatedDate { get; set; }
    //}
}