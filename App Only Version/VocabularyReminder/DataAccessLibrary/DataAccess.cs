using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace VocabularyReminder.DataAccessLibrary
{
    public class DataAccess
    {

        public static void InitializeDatabase()
        {
            string appFolder = ApplicationIO.GetApplicationFolderPath();
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            string dbFilePath = ApplicationIO.GetDatabasePath();
            if (!File.Exists(dbFilePath))
            {
                var file = File.Create(dbFilePath);
                file.Close();
            }

            using (SqliteConnection db = new SqliteConnection($"Filename={dbFilePath}"))
            {
                db.Open();
                String tableCommand = @"CREATE TABLE IF NOT EXISTS Dictionary (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                    Name NVARCHAR(2048) NULL, 
                    Description NVARCHAR(2048) NULL, 
                    Status INTEGER NULL DEFAULT 0)";
                SqliteCommand createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteReader();
                createTable.Dispose();

                tableCommand = @"CREATE TABLE IF NOT 
                    EXISTS Vocabulary (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Word NVARCHAR(2048) NOT NULL UNIQUE, 
                    Type NVARCHAR(100) NULL, 
                    Ipa NVARCHAR(100) NULL, 
                    Ipa2 NVARCHAR(100) NULL, 
                    Translate NVARCHAR(2048) NULL, 
                    Define NVARCHAR(2048) NULL, 
                    Example NVARCHAR(2048) NULL, 
                    Example2 NVARCHAR(2048) NULL, 
                    PlayURL NVARCHAR(2048) NULL, 
                    PlayURL2 NVARCHAR(2048) NULL, 
                    Related NVARCHAR(2048) NULL, 
                    Status INTEGER NULL DEFAULT 1 )";
                createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteReader();

                createTable.Dispose();
                db.Close();
                db.Dispose();
            }
        }

        //public static void ResetDatabase()
        //{
        //    string dbpath = ApplicationIO.GetDatabasePath();
        //    using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
        //    {
        //        db.Open();
        //        String tableCommand = "DELETE FROM Dictionary;";
        //        SqliteCommand truncTable = new SqliteCommand(tableCommand, db);
        //        truncTable.ExecuteNonQuery();
        //        truncTable.Dispose();

        //        tableCommand = "DELETE FROM Vocabulary;";
        //        truncTable = new SqliteCommand(tableCommand, db);
        //        truncTable.ExecuteNonQuery();
        //        truncTable.Dispose();

        //        tableCommand = "delete from sqlite_sequence where name = 'Dictionary'; delete from sqlite_sequence where name = 'Vocabulary';";
        //        truncTable = new SqliteCommand(tableCommand, db);
        //        truncTable.ExecuteNonQuery();

        //        truncTable.Dispose();
        //        db.Close();
        //        db.Dispose();
        //    }
        //}

        public static int AddVocabulary(string inputText)
        {
            if (String.IsNullOrEmpty(inputText)) return 0;
            string dbpath = ApplicationIO.GetDatabasePath();
            int inserted = 0;
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                SqliteCommand insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                insertCommand.CommandText = "INSERT OR IGNORE INTO Vocabulary (Word) VALUES (@Entry); SELECT last_insert_rowid();";
                insertCommand.Parameters.AddWithValue("@Entry", inputText);
                var result = insertCommand.ExecuteScalar();
                db.Close();
                db.Dispose();
                insertCommand.Dispose();

                if (result != null)
                {
                    int.TryParse(result.ToString(), out inserted);
                }
            }

            return inserted;
        }

        public static void UpdateVocabulary(Vocabulary item)
        {
            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                SqliteCommand updateCommand = new SqliteCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                updateCommand.CommandText = "UPDATE Vocabulary SET Type = @Type, Translate = @Translate WHERE Id = @Id";

                updateCommand.Parameters.Add("@Id", SqliteType.Text).Value = item.Id;
                updateCommand.Parameters.Add("@Type", SqliteType.Text).Value = item.Type;
                //updateCommand.Parameters.Add("@Ipa", SqliteType.Text).Value = item.Ipa;
                updateCommand.Parameters.Add("@Translate", SqliteType.Text).Value = item.Translate;

                updateCommand.ExecuteNonQuery();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }


        public static void UpdatePlayURL(Vocabulary item)
        {
            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                SqliteCommand updateCommand = new SqliteCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                updateCommand.CommandText = "UPDATE Vocabulary SET Ipa = @Ipa, Define = @Define, Ipa2 = @Ipa2, PlayURL = @PlayURL, PlayURL2 = @PlayURL2, Example = @Example, Example2 = @Example2 WHERE Id = @Id";
                updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = item.Id;

                updateCommand.Parameters.Add("@Define", SqliteType.Text).Value = item.Define;

                updateCommand.Parameters.Add("@Ipa", SqliteType.Text).Value = item.Ipa;
                updateCommand.Parameters.Add("@Ipa2", SqliteType.Text).Value = item.Ipa2;

                updateCommand.Parameters.Add("@Example", SqliteType.Text).Value = item.Example;
                updateCommand.Parameters.Add("@Example2", SqliteType.Text).Value = item.Example2;

                updateCommand.Parameters.Add("@PlayURL", SqliteType.Text).Value = item.PlayURL;
                updateCommand.Parameters.Add("@PlayURL2", SqliteType.Text).Value = item.PlayURL2;
                updateCommand.ExecuteNonQuery();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }

        public static void UpdateStatus(int _Id, int _Status = 0)
        {
            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                SqliteCommand updateCommand = new SqliteCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                updateCommand.CommandText = "UPDATE Vocabulary SET Status = @Status WHERE Id = @Id";
                updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = _Id;
                updateCommand.Parameters.Add("@Status", SqliteType.Integer).Value = _Status;
                updateCommand.ExecuteNonQuery();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }

        public static void RemoveVocabulary(int _Id)
        {
            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                SqliteCommand updateCommand = new SqliteCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                updateCommand.CommandText = "DELETE FROM Vocabulary WHERE Id = @Id";
                updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = _Id;
                updateCommand.ExecuteNonQuery();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }

        public static void UpdateRelated(Vocabulary item)
        {
            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();
                SqliteCommand updateCommand = new SqliteCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                updateCommand.CommandText = "UPDATE Vocabulary SET Related = @Related WHERE Id = @Id";
                updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = item.Id;

                updateCommand.Parameters.Add("@Related", SqliteType.Text).Value = item.Related;
                updateCommand.ExecuteNonQuery();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }


        public static Vocabulary GetVocabularyById(int Id)
        {
            Vocabulary _item = new Vocabulary();

            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db =
               new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT * from Vocabulary WHERE Id = " + Id + " LIMIT 1;", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    _item = GetItemFromRead(query);
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }

            return _item;
        }

        public static Vocabulary GetNextVocabulary(int Id)
        {
            Vocabulary _item = new Vocabulary();

            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT * from Vocabulary WHERE Id > " + Id + " AND Status = 1 LIMIT 1;", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    _item = GetItemFromRead(query);
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }

            return _item;
        }

        public static Vocabulary GetRandomVocabulary(int Id)
        {
            Vocabulary _item = new Vocabulary();

            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT * from Vocabulary WHERE Id <> " + Id + " AND Status = 1 ORDER BY RANDOM() LIMIT 1;", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    _item = GetItemFromRead(query);
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }

            return _item;
        }

        public static Vocabulary GetFirstVocabulary()
        {
            Vocabulary _item = new Vocabulary();

            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT * from Vocabulary WHERE Status = 1 LIMIT 1;", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    _item = GetItemFromRead(query);
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }

            return _item;
        }

        public static int GetFirstWordId()
        {
            int _WordId = 1;
            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db =
               new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT Id from Vocabulary WHERE Status = 1 LIMIT 1;", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    _WordId = int.Parse(query.GetString(0));
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }
            return _WordId;
        }

        public static Stats GetStats()
        {
            string dbpath = ApplicationIO.GetDatabasePath();
            Stats _Stats = new Stats();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                string cmd = @" SELECT COUNT(*) as Total,
                                (SELECT COUNT(*) FROM Vocabulary WHERE Status = 0) as Remembered
                                FROM Vocabulary";

                db.Open();
                SqliteCommand selectCommand = new SqliteCommand(cmd, db);
                SqliteDataReader query = selectCommand.ExecuteReader();

                
                while (query.Read())
                {
                    _Stats.Total = int.Parse(query.GetString(0));
                    _Stats.Remembered = int.Parse(query.GetString(1));
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }
            return _Stats;
        }

        public static List<Vocabulary> GetListVocabularyToPreloadMp3()
        {
            List<Vocabulary> entries = new List<Vocabulary>();

            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT * from Vocabulary WHERE PlayURL IS NOT NULL", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    entries.Add(GetItemFromRead(query));
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }

            return entries;
        }

        private static Vocabulary GetItemFromRead(SqliteDataReader query)
        {
            Vocabulary _item = new Vocabulary();

            _item.Id = int.Parse(query.GetString(0));
            _item.Word = query.IsDBNull(1) ? "" : query.GetString(1);
            _item.Type = query.IsDBNull(2) ? "" : query.GetString(2);
            _item.Ipa = query.IsDBNull(3) ? "" : query.GetString(3);
            _item.Ipa2 = query.IsDBNull(4) ? "" : query.GetString(4);
            _item.Translate = query.IsDBNull(5) ? "" : query.GetString(5);
            _item.Define = query.IsDBNull(6) ? "" : query.GetString(6);
            _item.Example = query.IsDBNull(7) ? "" : query.GetString(7);
            _item.Example2 = query.IsDBNull(8) ? "" : query.GetString(8);
            _item.PlayURL = query.IsDBNull(9) ? "" : query.GetString(9);
            _item.PlayURL2 = query.IsDBNull(10) ? "" : query.GetString(10);
            _item.Related = query.IsDBNull(11) ? "" : query.GetString(11);
            _item.Status = query.IsDBNull(12) ? 0 : query.GetInt32(12);

            return _item;
        }


        public static List<Vocabulary> GetListVocabularyToTranslate()
        {
            List<Vocabulary> entries = new List<Vocabulary>();

            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT * from Vocabulary WHERE Translate IS NULL OR Translate = ''", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    entries.Add(GetItemFromRead(query));
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }

            return entries;
        }


        public static List<Vocabulary> GetListVocabularyToGetDefineExampleMp3URL()
        {
            List<Vocabulary> entries = new List<Vocabulary>();

            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT * from Vocabulary WHERE PlayURL IS NULL OR Translate = ''", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    entries.Add(GetItemFromRead(query));
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }

            return entries;
        }

        public static List<Vocabulary> GetListVocabularyToGetRelatedWords()
        {
            List<Vocabulary> entries = new List<Vocabulary>();

            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT * from Vocabulary WHERE Related IS NULL OR Related = ''", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    entries.Add(GetItemFromRead(query));
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }

            return entries;
        }

        public static List<Vocabulary> GetListLearnded()
        {
            List<Vocabulary> entries = new List<Vocabulary>();

            string dbpath = ApplicationIO.GetDatabasePath();
            using (SqliteConnection db = new SqliteConnection($"Filename={dbpath}"))
            {
                db.Open();

                SqliteCommand selectCommand = new SqliteCommand
                    ("SELECT * from Vocabulary WHERE Status = 0", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    entries.Add(GetItemFromRead(query));
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }

            return entries;
        }
    }


}
