using miniBBS.Core.Enums;
using System;

namespace miniBBS.Core
{
    public static class Constants
    {
        public const string Version = "5.3d - 2024.01.06";
        public const string SysopName = "Divarin";

        public const int MinutesUntilMessageIsUndeletable = 60*3;
        public const int MinimumPasswordLength = 5;
        public const int MaximumPasswordLength = 30;

        public const string DatabaseFilename = "community.db";

        public static bool IsLocal { private get; set; } = false;
        public static readonly char[] LegitOneCharacterCommands = new[]
        { '[', ']', '{', '}', '<', '>', ',', '.', '?' };

        public static readonly string[] InvalidChannelNames = new[]
        { "del", "ren" };

        public static string UploadDirectory
        {
            get
            {
                return IsLocal ? local_UploadDirectory : @"c:\sbbs\data\dirs\";
            }
        }
        public static string TextFileRootDirectory
        {
            get
            {
                return IsLocal ? local_TextFileRootDirectory : @"c:\textfiles\";
            }
        }

        public const CrossChannelNotificationMode DefaultCrossChannelNotificationMode = CrossChannelNotificationMode.Any | CrossChannelNotificationMode.OncePerChannel;

        public const string DefaultChatHeaderFormat = "[%mn%:%y%-%mm%-%dd% %hh%:%min%] <%un%> (re:%re%)";

        /// <summary>
        /// used for emotes or any other input wtih handling flag MaxLength
        /// </summary>
        public const int MaxInputLength = 64;

        public const int TutorLogins = 10;

        public const int MaxVoteQuestionsPerUser = 5;
        public const int MaxVoteQuestionsPerDay = 1;

        public const int MaxBlurbLength = 256;
        public const int MaxBlurbs = 256;

        public const int MaxPublicPinsPerUser = 25;

        public const string BasicSourceProtectedFlag = "(protect)";
        
        public const int MaxFileBackups = 9;

        public const string local_TextFileRootDirectory = @"C:\code\textfiles\";
        public const string local_UploadDirectory = @"c:\work\";

        public static readonly string[] IllegalUsernames = new[]
        {
            "Sysop", "Administrator", "Admin", "Root", "Owner", "New", "Me", "On", "Off",
            "Guest", "Anon", "Anonymous", "Flag"
        };

        /// <summary>
        /// If a non-administrator is attempting to delete a channel, this may succeed if 
        /// a) they are a moderator and b) the channel was created within 60 minutes.
        /// </summary>
        public const int MaxMinutesToDeleteChannel = 60;
        public const int NumberOfLogEntriesUntilWriteToDatabase = 20;

        public const string DefaultChannelName = "General";

        public const int MaxSessions = 100;
        public const int MaxSessionsPerUser = 5;
        public const int MaxSessionsPerIpAddress = 5;
        public const int MaxSearchResults = 50;

        public const int MaxUsernameLength = 15;
        public const int MinUsernameLength = 2;
        public const int MaxChannelNameLength = 25;

        /// <summary>
        /// A placeholder for a space character.  This is used because the SplitAndWrap extension trims off spaces at the start/end of a line 
        /// but sometimes we explicitly want to add a space there (such as for returning a blank line) so this placeholder can sit there 
        /// and will be replaced with an actual space at the last second.
        /// </summary>
        public const char Spaceholder = (char)168;

        /// <summary>
        /// A character which indicates the beginning of, or ending of, a color code that's embedded within text.  This is used 
        /// if you want to stream a block of text with pagination but with color changes.  The format to use this would be:
        /// $"{Constants.InlineColorizer}{(int)ConsoleColor.Red}{Constants.InlineColorizer}this is red!{Constants.InlineColorizer}-1{Constants.InlineColorizer} This isn't red!"
        /// Instead of a specific color you can use the int value "-1" to mean "return to the current foreground color" since 
        /// the inline colorizer doesn't change what the Io *thinks* is the foreground color.  Because of this you should always 
        /// return to the current foreground.
        /// </summary>
        public const char InlineColorizer = (char)130;

        /// <summary>
        /// A character which indicated that inverse atascii should be toggled on/off.
        /// For all other emulation times this is filtered out.
        /// </summary>
        public const char Inverser = '¡';

        /// <summary>
        /// How long to wait, in millisecond, before hanging up.  This delay allows any remaining data in the stream to be 
        /// written out.  Without this the user may not see the last message, which may be why the user is being hung-up on.
        /// </summary>
        public const long HangupDelay = 1000;

        /// <summary>
        /// 2 minutes to complete a login or you get disconnected
        /// </summary>
        public const int MaxLoginTimeMin = 2;

        public const int MaxAfkReasonLength = 30;

        public const double MaxCalendarItemDays = 30;

        // used for search results when searching chat history
        public const int MaxSnippetLength = 25;

        /// <summary>
        /// While typing a message, if a new message comes in, the notification about that new message will be 
        /// delayed until the user is done typing or this timespan has elapsed.
        /// </summary>
        public static readonly TimeSpan DelayedNotificationsMaxWaitTime = TimeSpan.FromMinutes(2);

        public static readonly InputHandlingFlag ChatInputHandling = 
            InputHandlingFlag.InterceptSingleCharacterCommand | 
            InputHandlingFlag.UseLastLine | 
            InputHandlingFlag.DoNotEchoNewlines | 
            InputHandlingFlag.AllowCtrlEnterToAddNewLine |
            InputHandlingFlag.MaxLengthIfEmote;

        public const int BasicMaxRuntimeMin = 60;
        public const int DefaultPingPongDelayMin = 5;

        /// <summary>
        /// Maximum length of file or directory names for user generated text files
        /// </summary>
        public const int MaxUserFilenameLength = 50;

        public const int MaxLinesToInsertInLineEditor = 50;

        public const string MaintTime = "0200"; // 2 AM local time
        public const int MaintDurationMin = 60; // 1 hour

        public static class Files
        {
            public const string NewUser = "newuser.txt";
        }

        public static class Basic
        {
            public const char QuoteSubstitute = (char)159;
            public const char Quote = '"';
        }
    }
}
