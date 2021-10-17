using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace VocabularyReminder.DataAccessLibrary
{
    public class DataAccess
    {
        //private static readonly string ConnectionString = "Data Source =.\\MSSQLSERVER01;Initial Catalog = VocabularyReminder; Integrated Security = True";
        private static readonly string ConnectionString = "Data Source=pypvd.database.windows.net;Initial Catalog=VocabularyReminder;Persist Security Info=True;User ID=pvdadmin;Password=pvd@2020";
        public static void InitializeDatabase()
        {
            return;
        }

        public static async Task<int> AddVocabularyAsync(string inputText, int index = 0)
        {
            if (String.IsNullOrEmpty(inputText)) return 0;
            
            int inserted = 0;
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                SqlCommand insertCommand = new SqlCommand();
                insertCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                insertCommand.CommandText = "INSERT INTO Vocabulary (Word, UserId, Idx) VALUES (@Entry, @UserId, @Index); SELECT SCOPE_IDENTITY();";
                insertCommand.Parameters.AddWithValue("@Entry", inputText);
                insertCommand.Parameters.AddWithValue("@UserId", App.User.UserId);
                insertCommand.Parameters.AddWithValue("@Index", index);
                var result = await insertCommand.ExecuteScalarAsync();
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

        public static async Task UpdateVocabularyAsync(Vocabulary item)
        {
            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                SqlCommand updateCommand = new SqlCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                updateCommand.CommandText = "UPDATE Vocabulary WITH (ROWLOCK) SET Type = @Type, Translate = @Translate WHERE Id = @Id";

                updateCommand.Parameters.AddWithValue("@Id", SqliteType.Text).Value = item.Id;
                updateCommand.Parameters.AddWithValue("@Type", SqliteType.Text).Value = item.Type;
                //updateCommand.Parameters.Add("@Ipa", SqliteType.Text).Value = item.Ipa;
                updateCommand.Parameters.AddWithValue("@Translate", SqliteType.Text).Value = item.Translate;

                await updateCommand.ExecuteNonQueryAsync();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }


        public static async Task UpdateWordBasic(Vocabulary item)
        {
            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                SqlCommand updateCommand = new SqlCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                updateCommand.CommandText = "UPDATE Vocabulary WITH (ROWLOCK) SET Ipa = @Ipa, Define = @Define, Ipa2 = @Ipa2, PlayURL = @PlayURL, PlayURL2 = @PlayURL2, Example = @Example, Example2 = @Example2 WHERE Id = @Id";
                updateCommand.Parameters.AddWithValue("@Id", SqliteType.Integer).Value = item.Id;

                updateCommand.Parameters.AddWithValue("@Define", SqliteType.Text).Value = item.Define;

                updateCommand.Parameters.AddWithValue("@Ipa", SqliteType.Text).Value = item.Ipa;
                updateCommand.Parameters.AddWithValue("@Ipa2", SqliteType.Text).Value = item.Ipa2;

                updateCommand.Parameters.AddWithValue("@Example", SqliteType.Text).Value = item.Example;
                updateCommand.Parameters.AddWithValue("@Example2", SqliteType.Text).Value = item.Example2;

                updateCommand.Parameters.AddWithValue("@PlayURL", SqliteType.Text).Value = item.PlayURL;
                updateCommand.Parameters.AddWithValue("@PlayURL2", SqliteType.Text).Value = item.PlayURL2;
                await updateCommand.ExecuteNonQueryAsync();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }

        public static async Task UpdateStatusAsync(int _Id, int _Status = 0)
        {
            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                SqlCommand updateCommand = new SqlCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                updateCommand.CommandText = "UPDATE Vocabulary WITH (ROWLOCK) SET Status = @Status WHERE Id = @Id";
                updateCommand.Parameters.AddWithValue("@Id", SqliteType.Integer).Value = _Id;
                updateCommand.Parameters.AddWithValue("@Status", SqliteType.Integer).Value = _Status;
                await updateCommand.ExecuteNonQueryAsync();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }

        public static async Task RemoveVocabularyAsync(int _Id)
        {
            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                SqlCommand updateCommand = new SqlCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                updateCommand.CommandText = "DELETE FROM Vocabulary WHERE Id = @Id";
                updateCommand.Parameters.AddWithValue("@Id", SqliteType.Integer).Value = _Id;
                await updateCommand.ExecuteNonQueryAsync();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }

        public static async Task UpdateRelatedAsync(Vocabulary item)
        {
            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                SqlCommand updateCommand = new SqlCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                updateCommand.CommandText = "UPDATE Vocabulary WITH (ROWLOCK) SET Related = @Related WHERE Id = @Id";
                updateCommand.Parameters.AddWithValue("@Id", SqliteType.Integer).Value = item.Id;

                updateCommand.Parameters.AddWithValue("@Related", SqliteType.Text).Value = item.Related;
                await updateCommand.ExecuteNonQueryAsync();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }


        public static async Task<Vocabulary> GetVocabularyByIdAsync(int Id)
        {
            Vocabulary _item = new Vocabulary();

            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();

                SqlCommand selectCommand = new SqlCommand
                    ("SELECT TOP 1 * from Vocabulary WITH (NOLOCK) WHERE Id = " + Id + ";", db);

                SqlDataReader query = selectCommand.ExecuteReader();

                while (await query.ReadAsync())
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

        public static async Task<Vocabulary> GetNextVocabularyAsync(int Id)
        {
            Vocabulary _item = new Vocabulary();
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                var qs = "SELECT TOP 1 * from Vocabulary WITH (NOLOCK) WHERE Idx > (SELECT Idx FROM Vocabulary WITH (NOLOCK) WHERE Id = " + Id + ") AND Status = 1 AND UserId = '{0}' ORDER BY Idx;";
                qs = string.Format(qs, App.User.UserId);
                SqlCommand selectCommand = new SqlCommand(qs, db);

                SqlDataReader query = selectCommand.ExecuteReader();

                while (await query.ReadAsync())
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

        public static async Task<Vocabulary> GetRandomVocabularyAsync(int Id)
        {
            Vocabulary _item = new Vocabulary();

            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                string qs = "SELECT TOP 1 * from Vocabulary WITH (NOLOCK) WHERE Id <> " + Id + " AND Status = 1 AND UserId = '{0}' ORDER BY newid();";
                qs = string.Format(qs, App.User.UserId);
                SqlCommand selectCommand = new SqlCommand(qs, db);

                SqlDataReader query = selectCommand.ExecuteReader();

                while (await query.ReadAsync())
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

        public static async Task<Vocabulary> GetFirstVocabularyAsync()
        {
            Vocabulary _item = new Vocabulary();

            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();

                string qs = "SELECT TOP 1 * from Vocabulary WITH (NOLOCK) WHERE Status = 1 AND UserId = '{0}' ORDER BY Idx;";
                qs = string.Format(qs, App.User.UserId);
                SqlCommand selectCommand = new SqlCommand(qs, db);

                SqlDataReader query = selectCommand.ExecuteReader();

                while (await query.ReadAsync())
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

        public static async Task<int> GetFirstWordIdAsync()
        {
            int _WordId = 1;

            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();

                string qs = "SELECT TOP 1 Id from Vocabulary WITH (NOLOCK) WHERE Status = 1 AND UserId = '{0}' ORDER BY Idx;";
                qs = string.Format(qs, App.User.UserId);
                SqlCommand selectCommand = new SqlCommand(qs, db);

                SqlDataReader query = selectCommand.ExecuteReader();

                while (await query.ReadAsync())
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

        public static async Task<int> GetVocabularyMaxIndexAsync()
        {
            int _maxIndex = 0;
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();

                SqlCommand selectCommand = new SqlCommand
                    ("SELECT MAX(Idx) FROM Vocabulary WITH (NOLOCK);", db);

                SqlDataReader query = selectCommand.ExecuteReader();

                if (query.HasRows)
                {
                    while (await query.ReadAsync())
                    {
                        if (!query.IsDBNull(0))
                            _maxIndex = query.GetInt32(0);
                    }
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }
            return _maxIndex;
        }

        public static async Task<Stats> GetStatsAsync()
        {
            
            Stats _Stats = new Stats();
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                string cmd = @" SELECT COUNT(*) as Total,
                                (SELECT COUNT(*) FROM Vocabulary WITH (NOLOCK) WHERE Status = 0 AND UserId = '{0}') as Remembered
                                FROM Vocabulary WITH (NOLOCK) WHERE UserId = '{0}' ";
                cmd = string.Format(cmd, App.User.UserId);
                db.Open();
                SqlCommand selectCommand = new SqlCommand(cmd, db);
                SqlDataReader query = selectCommand.ExecuteReader();

                
                while (await query.ReadAsync())
                {
                    _Stats.Total = query.GetInt32(0);
                    _Stats.Remembered = query.GetInt32(1);
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }
            return _Stats;
        }

        public static async Task<List<Vocabulary>> GetListVocabularyToPreloadMp3Async()
        {
            List<Vocabulary> entries = new List<Vocabulary>();

            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();

                string qs = "SELECT * from Vocabulary WITH (NOLOCK) WHERE UserId = '{0}' AND PlayURL IS NOT NULL";
                qs = string.Format(qs, App.User.UserId);
                SqlCommand selectCommand = new SqlCommand(qs, db);

                SqlDataReader query = selectCommand.ExecuteReader();

                while (await query.ReadAsync())
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

        private static Vocabulary GetItemFromRead(SqlDataReader query)
        {
            if (query.IsDBNull(0))
                return null;

            Vocabulary _item = new Vocabulary();

            _item.Id = query.IsDBNull(0) ? 0 : query.GetInt32(0);
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
            _item.Idx = query.IsDBNull(13) ? 0 : query.GetInt32(13);
            _item.UserId = query.IsDBNull(14) ? "" : query.GetString(14);

            return _item;
        }


        public static async Task<List<Vocabulary>> GetListVocabularyToTranslateAsync()
        {
            List<Vocabulary> entries = new List<Vocabulary>();

            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();

                string qs = "SELECT * from Vocabulary WITH (NOLOCK) WHERE UserId = '{0}' AND Translate IS NULL OR Translate = ''";
                qs = string.Format(qs, App.User.UserId);
                SqlCommand selectCommand = new SqlCommand(qs, db);

                SqlDataReader query = selectCommand.ExecuteReader();

                while (await query.ReadAsync())
                {
                    var item = GetItemFromRead(query);
                    if (item != null)
                        entries.Add(item);
                }

                selectCommand.Dispose();
                query.Close();
                db.Close();
                db.Dispose();
            }

            return entries;
        }


        public static async Task<List<Vocabulary>> GetListVocabularyToGetDefineExampleMp3URLAsync()
        {
            List<Vocabulary> entries = new List<Vocabulary>();

            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();

                string qs = "SELECT * from Vocabulary WITH (NOLOCK) WHERE UserId = '{0}' AND PlayURL IS NULL OR Translate = ''";
                qs = string.Format(qs, App.User.UserId);
                SqlCommand selectCommand = new SqlCommand(qs, db);

                SqlDataReader query = selectCommand.ExecuteReader();

                while (await query.ReadAsync())
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

        public static async Task<List<Vocabulary>> GetListVocabularyToGetRelatedWordsAsync()
        {
            List<Vocabulary> entries = new List<Vocabulary>();

            
            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                string qs = "SELECT * from Vocabulary WITH (NOLOCK) WHERE UserId = '{0}' AND Related IS NULL OR Related = ''";
                qs = string.Format(qs, App.User.UserId);
                SqlCommand selectCommand = new SqlCommand(qs, db);

                SqlDataReader query = selectCommand.ExecuteReader();

                while (await query.ReadAsync())
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

        public static async Task<List<Vocabulary>> GetListLearndedAsync(ShowListType showListType)
        {
            List<Vocabulary> entries = new List<Vocabulary>();

            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();

                string qs = "SELECT * from Vocabulary WITH (NOLOCK) WHERE UserId = '{0}'";
                switch (showListType)
                {
                    case ShowListType.NEW_WORD:
                        qs += " AND Status = 1";
                        break;
                    case ShowListType.LEARN_WORD:
                        qs += " AND Status = 0";
                        break;
                }
                qs = string.Format(qs, App.User.UserId);
                SqlCommand selectCommand = new SqlCommand(qs, db);
                using (var query = selectCommand.ExecuteReader())
                {
                    if (query.HasRows)
                    {
                        while (await query.ReadAsync())
                        {
                            var voca = GetItemFromRead(query);
                            if (voca != null)
                                entries.Add(voca);
                        }
                    }

                    selectCommand.Dispose();
                    db.Close();
                }
            }

            return entries;
        }

        public static async Task ClearAllVocabularyAsync()
        {

            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                SqlCommand updateCommand = new SqlCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                string qs = "DELETE FROM Vocabulary WHERE UserId = '{0}'";
                qs = string.Format(qs, App.User.UserId);
                updateCommand.CommandText = qs;
                // TODO: add UserId
                //updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = _Id;
                await updateCommand.ExecuteNonQueryAsync();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }

        public static async Task Import3000WordsAsync()
        {

            using (SqlConnection db = new SqlConnection(ConnectionString))
            {
                db.Open();
                SqlCommand updateCommand = new SqlCommand();
                updateCommand.Connection = db;

                // Use parameterized query to prevent SQL injection attacks
                string qs = @"INSERT INTO [dbo].[Vocabulary]
                                    ([Word]
                                    ,[Type]
                                    ,[Ipa]
                                    ,[Ipa2]
                                    ,[Translate]
                                    ,[Define]
                                    ,[Example]
                                    ,[Example2]
                                    ,[PlayURL]
                                    ,[PlayURL2]
                                    ,[Related]
                                    ,[Status]
                                    ,[Idx]
                                    ,[UserId])
                            SELECT [Word]
			                            ,[Type]
			                            ,[Ipa]
			                            ,[Ipa2]
			                            ,[Translate]
			                            ,[Define]
			                            ,[Example]
			                            ,[Example2]
			                            ,[PlayURL]
			                            ,[PlayURL2]
			                            ,[Related]
			                            ,[Status]
			                            ,[Idx]
			                            ,'{0}'
			                            FROM [dbo].[Vocabulary] WHERE UserId = '{1}'";
                qs = string.Format(qs, App.User.UserId, "0dd1ce9e-93aa-4452-8875-9cd73609e369");
                updateCommand.CommandText = qs;
                // TODO: add UserId
                //updateCommand.Parameters.Add("@Id", SqliteType.Integer).Value = _Id;
                await updateCommand.ExecuteNonQueryAsync();

                updateCommand.Dispose();
                db.Close();
                db.Dispose();
            }
        }
    }


}
