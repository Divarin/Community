using miniBBS.Core;
using System.Data.SQLite;
using System.IO;

namespace miniBBS.Persistence
{
    public class DatabaseInitializer
    {
        private static readonly string[] _bulletinBoards = new[]
        {
            "General",
            "Market",
        };

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
                CreateBulletinsTable(db);
                CreateUserChannelFlagsTable(db);
                CreateNotificationsTable(db);
                CreateCalendarTable(db);
                CreateIpBansTable(db);
                CreateMailTable(db);
                CreateBlurbsTable(db);
                CreatePinnedMessagesTable(db);
                CreateGopherBookmarksTable(db);
                CreatePollTables(db);
                CreateMetadataTable(db);
                CreateBbsListTable(db);

                db.Close();
            }
        }

        private void Exec(SQLiteConnection db, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, db))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateLogsTable(SQLiteConnection db)
        {
            const string sql = "CREATE TABLE Logs (Id integer primary key autoincrement, SessionId TEXT null, IpAddress TEXT null, UserId integer null, TimestampUtc TEXT not null, Message TEXT not null)";
            Exec(db, sql);
        }

        private void CreateUsersTable(SQLiteConnection db)
        {
            const string sql = "CREATE TABLE Users (Id integer primary key autoincrement, Name TEXT not null, PasswordHash TEXT not null, DateAddedUtc TEXT not null, LastLogonUtc TEXT not null, TotalLogons integer not null, Access TEXT not null, Cols integer not null default 40, Rows integer not null default 24, Emulation string not null default 'Ascii', Timezone integer not null default 0, InternetEmail TEXT null)";
            Exec(db, sql);
        }

        private void CreateChannelsTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE Channels (Id integer primary key autoincrement, Name TEXT not null, RequiresInvite TEXT not null default 'False', RequiresVoice TEXT not null default 'False', DateCreatedUtc TEXT null)";
            Exec(db, sql);

            sql = $"INSERT INTO Channels (Name) values ('{Constants.DefaultChannelName}')";
            Exec(db, sql);
        }

        private void CreateChatTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE Chat (Id integer primary key autoincrement, ResponseToId integer null, ChannelId integer not null, FromUserId integer not null, DateUtc TEXT not null, Message TEXT not null)";
            Exec(db, sql);

            sql = "create index Chat_ChannelId_Idx on Chat (ChannelId)";
            Exec(db, sql);
        }

        private void CreateBulletinsTable(SQLiteConnection db)
        {
            var sql = "CREATE TABLE BulletinBoards (Id integer primary key autoincrement, Name TEXT not null)";
            Exec(db, sql);

            foreach (var bb in _bulletinBoards)
            {
                sql = $"INSERT INTO BulletinBoards (Name) values ('{bb}')";
                Exec(db, sql);
            }

            sql = "CREATE TABLE Bulletins (Id integer primary key autoincrement, FromUserId integer not null, ResponseToId integer null, OriginalId integer null, ToUserId integer null, DateUtc TEXT not null, Subject TEXT not null, Message TEXT not null, BoardId integer not null)";
            Exec(db, sql);
        }

        private void CreateUserChannelFlagsTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE UserChannelFlags (Id integer primary key autoincrement, UserId integer not null, ChannelId integer not null, Flags TEXT not null, LastReadMessageNumber integer not null default 0)";
            Exec(db, sql);

            sql = "create index UserChannelFlags_ChannelId_Idx on UserChannelFlags (UserId, ChannelId)";
            Exec(db, sql);
        }

        private void CreateNotificationsTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE Notifications (Id integer primary key autoincrement, UserId integer not null, DateSentUtc TEXT not null, Message TEXT not null)";
            Exec(db, sql);

            sql = "create index Notifications_UserId_Idx on Notifications (UserId)";
            Exec(db, sql);
        }

        private void CreateCalendarTable(SQLiteConnection db)
        {
            const string sql = "CREATE TABLE CalendarItems (Id integer primary key autoincrement, UserId integer not null, EventTime TEXT not null, ChannelId integer null, Topic TEXT null, DateCreatedUtc TEXT not null)";
            Exec(db, sql);
        }

        private void CreateIpBansTable(SQLiteConnection db)
        {
            const string sql = "CREATE TABLE IpBans (Id integer primary key autoincrement, IpMask TEXT not null)";
            Exec(db, sql);
        }

        private void CreateMailTable(SQLiteConnection db)
        {
            const string sql = "CREATE TABLE Mail (Id integer primary key autoincrement, FromUserId integer not null, ToUserId integer not null, SentUtc TEXT not null, Subject TEXT not null, Message TEXT not null, Read TEXT not null default 'False')";
            Exec(db, sql);
        }

        private void CreateBlurbsTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE Blurbs (Id integer primary key autoincrement, UserId integer not null, DateAddedUtc TEXT not null, BlurbText TEXT not null)";
            Exec(db, sql);
        }

        private void CreatePinnedMessagesTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE PinnedMessages (Id integer primary key autoincrement, MessageId integer not null, ChannelId integer not null, PinnedByUserId integer not null, Private TEXT not null, DatePinnedUtc TEXT not null)";
            Exec(db, sql);
        }

        private void CreateGopherBookmarksTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE GopherBookmarks (Id integer primary key autoincrement, UserID integer not null, Private TEXT not null, DateCreatedUtc TEXT not null, Selector TEXT not null, Title TEXT not null, Tags TEXT null)";
            Exec(db, sql);
        }

        private void CreatePollTables(SQLiteConnection db)
        {
            string sql = "CREATE TABLE PollQuestions (Id integer primary key autoincrement, UserId integer not null, Question TEXT not null, DateAddedUtc TEXT not null, Answers TEXT not null)";
            Exec(db, sql);

            sql = "CREATE TABLE PollVotes (Id integer primary key autoincrement, QuestionId integer not null, UserId integer not null, DateAddedUtc TEXT not null, Answer TEXT not null)";
            Exec(db, sql);
        }

        private void CreateMetadataTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE Metadata (Id integer primary key autoincrement, Type TEXT not null, UserId integer null, ChannelId integer null, Data TEXT not null, DateAddedUtc TEXT null)";
            Exec(db, sql);
        }

        private void CreateBbsListTable(SQLiteConnection db)
        {
            string sql = "CREATE TABLE BbsList (Id integer primary key autoincrement, AddedByUserId integer not null, Name TEXT not null, Address TEXT not null, Port TEXT null, Sysop TEXT null, Software TEXT null, Emulations TEXT null, Description TEXT null, DateAddedUtc TEXT null)";
            Exec(db, sql);
        }
    }
}
