using System;
using System.Data.Entity;
using System.Data.SQLite;
using VR.Domain.Models;

namespace VR.Domain.Data
{
    public class VocaDbContext : DbContext
    {
        public VocaDbContext(string databasePath) : base(new SQLiteConnection()
        {
            ConnectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = databasePath,
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
}