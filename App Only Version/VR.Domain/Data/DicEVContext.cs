using System.Data.Entity;
using System.Data.SQLite;
using VR.Domain.Models;

namespace VR.Domain.Data
{
    //This Context is used to READ-ONLY from existing vocabulary
    public class DicEVContext : DbContext
    {
        public DicEVContext(string databasePath) : base(new SQLiteConnection()
        {
            ConnectionString = new SQLiteConnectionStringBuilder()
            {
                DataSource = databasePath,
                ForeignKeys = true
            }.ConnectionString,
        }, true)
        {
            Database.SetInitializer<DicEVContext>(null);
            Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
        }

        public DbSet<EVVocabulary> Vocabularies { get; set; }
    }
}