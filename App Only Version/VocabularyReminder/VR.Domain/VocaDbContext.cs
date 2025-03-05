using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using VR.Domain.Models;
using VR.Infrastructure;

namespace VR.Domain
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
}