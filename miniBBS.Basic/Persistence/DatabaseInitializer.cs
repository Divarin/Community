//using System.Data.SQLite;
//using System.IO;

//namespace miniBBS.Basic.Persistence
//{
//    public class DatabaseInitializer
//    {
//        public void InitializeDatabase()
//        {
//            if (File.Exists(GlobalConstants.DatabaseFilename))
//                return;

//            using (var db = new SQLiteConnection($"Data Source={GlobalConstants.DatabaseFilename};datetimeformat=CurrentCulture"))
//            {
//                db.Open();

//                CreateUsersTable(db);
//                CreateDisksTable(db);
//                CreateProgramsTable(db);

//                db.Close();
//            }
//        }

//        private void CreateProgramsTable(SQLiteConnection db)
//        {
//            string sql = "CREATE TABLE Programs (Id integer primary key autoincrement, Name TEXT not null, DiskId integer not null, Data TEXT null, Published INT not null, SourceVisible INT not null, Tags TEXT null, Rating INT not null)";
//            using (var cmd = new SQLiteCommand(sql, db))
//            {
//                cmd.ExecuteNonQuery();
//            }
//        }

//        private void CreateDisksTable(SQLiteConnection db)
//        {
//            string sql = "CREATE TABLE Disks (Id integer primary key autoincrement, Name TEXT not null, UserId integer not null)";
//            using (var cmd = new SQLiteCommand(sql, db))
//            {
//                cmd.ExecuteNonQuery();
//            }
//        }

//        private void CreateUsersTable(SQLiteConnection db)
//        {
//            string sql = "CREATE TABLE Users (Id integer primary key autoincrement, Name TEXT not null, Ansi integer not null, DateAddedUtc TEXT not null, Baud integer null)";
//            using (var cmd = new SQLiteCommand(sql, db))
//            {
//                cmd.ExecuteNonQuery();
//            }
//        }

//    }
//}
