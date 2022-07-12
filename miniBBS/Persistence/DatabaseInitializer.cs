using miniBBS.Core;
using System;
using System.Data.SQLite;
using System.IO;

namespace miniBBS.Persistence
{
    public class DatabaseInitializer
    {
        public void InitializeDatabase()
        {
            if (File.Exists(Constants.DatabaseFilename))
                return;

            using (var db = new SQLiteConnection($"Data Source={Constants.DatabaseFilename};datetimeformat=CurrentCulture"))
            {
                db.Open();

                CreateLogsTable(db);
                CreateUsersTable(db);
                CreateChannelsTable(db);
                CreateChatTable(db);
                CreateUserChannelFlagsTable(db);
                CreateNotificationsTable(db);
                CreateCalendarTable(db);
                CreateIpBansTable(db);
                CreateMailTable(db);

                db.Close();
            }
        }

        private void CreateLogsTable(SQLiteConnection db)
        {
            const string sql = "CREATE TABLE Logs (Id integer primary key autoincrement, SessionId TEXT null, IpAddress TEXT null, UserId integer null, TimestampUtc TEXT not null, Message TEXT not null)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateUsersTable(SQLiteConnection db)
        {
            const string sql = "CREATE TABLE Users (Id integer primary key autoincrement, Name TEXT not null, PasswordHash TEXT not null, DateAddedUtc TEXT not null, LastLogonUtc TEXT not null, TotalLogons integer not null, Access TEXT not null, Cols integer not null default 40, Rows integer not null default 24, Emulation string not null default 'Ascii', Timezone integer not null default 0)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateChannelsTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE Channels (Id integer primary key autoincrement, Name TEXT not null, RequiresInvite TEXT not null default 'False', DateCreatedUtc TEXT null)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }

            sql = $"INSERT INTO Channels (Name) values ('{Constants.DefaultChannelName}')";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateChatTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE Chat (Id integer primary key autoincrement, ResponseToId integer null, ChannelId integer not null, FromUserId integer not null, DateUtc TEXT not null, Message TEXT not null)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }

            sql = "create index Chat_ChannelId_Idx on Chat (ChannelId)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateUserChannelFlagsTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE UserChannelFlags (Id integer primary key autoincrement, UserId integer not null, ChannelId integer not null, Flags TEXT not null, LastReadMessageNumber integer not null default 0)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }

            sql = "create index UserChannelFlags_ChannelId_Idx on UserChannelFlags (UserId, ChannelId)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateNotificationsTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE Notifications (Id integer primary key autoincrement, UserId integer not null, DateSentUtc TEXT not null, Message TEXT not null)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }

            sql = "create index Notifications_UserId_Idx on Notifications (UserId)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateCalendarTable(SQLiteConnection db)
        {
            const string sql = "CREATE TABLE CalendarItems (Id integer primary key autoincrement, UserId integer not null, EventTime TEXT not null, ChannelId integer null, Topic TEXT null, DateCreatedUtc TEXT not null)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateIpBansTable(SQLiteConnection db)
        {
            const string sql = "CREATE TABLE IpBans (Id integer primary key autoincrement, IpMask TEXT not null)";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateMailTable(SQLiteConnection db)
        {
            const string sql = "CREATE TABLE Mail (Id integer primary key autoincrement, FromUserId integer not null, ToUserId integer not null, SentUtc TEXT not null, Subject TEXT not null, Message TEXT not null, Read TEXT not null default 'False')";
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }
        }

    }
}
