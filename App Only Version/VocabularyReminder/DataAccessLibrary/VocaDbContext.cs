using System.Collections.Generic;
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
            }.ConnectionString,
        }, true)
        {
            Database.SetInitializer<VocaDbContext>(null);
            Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
        }

        public DbSet<Vocabulary> Vocabularies { get; set; }
        public DbSet<Dictionary> Dictionaries { get; set; }
        public DbSet<VocabularyMapping> VocabularyMappings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VocabularyMapping>()
                .HasKey(vm => new { vm.DictionaryId, vm.VocabularyId });

            modelBuilder.Entity<Vocabulary>()
                .HasMany(e => e.Dictionaries)
                .WithMany(e => e.Vocabularies)
                .Map(cs =>
                {
                    cs.MapLeftKey("VocabularyId");
                    cs.MapRightKey("DictionaryId");
                });

            base.OnModelCreating(modelBuilder);
        }
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

        public virtual ICollection<Dictionary> Dictionaries { get; set; }
    }

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