using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace miniBBS.Services.Services
{
    public class WebLogger : IWebLogger
    {
        private const string _templateFilename = "chatstemplate.html";
        private static readonly string _outputFilename = Constants.WebLogOutputFile;
        private static readonly Random _random = new Random((int)DateTime.Now.Ticks);

        private Thread _refreshThread = null;
        public bool ContinuousRefresh { get; private set; }
        private int? _lastCount = null;
        private bool _forceCompile = false;

        public void SetForceCompile()
        {
            _forceCompile = true;
        }

        public void StartContinuousRefresh(IDependencyResolver di)
        {
            ContinuousRefresh = true;
            var start = new ParameterizedThreadStart(ContinuousUpdateWebLog);
            _refreshThread = new Thread(start);
            _refreshThread.Start(di);
        }

        public void StopContinuousRefresh()
        {
            ContinuousRefresh = false;
        }

        public void UpdateWebLog(IDependencyResolver di)
        {
            var repo = di.GetRepository<Chat>();
            var count = repo.GetCount();
            if (!_forceCompile && _lastCount.HasValue && count == _lastCount.Value)
                return;
            _lastCount = count;
            _forceCompile = false;
            var dbSet = GetDbSet(di, repo);
            var html = LoadHtml();
            var splits = html.Split(separator: new[] { "<div id=\"mainContent\">" }, options: StringSplitOptions.RemoveEmptyEntries);
            var upToMainContent = splits[0];
            var items = GenerateItemsHtml(dbSet, di).ToList();
            var mainContent = string.Join(Environment.NewLine, items);
            var afterMainContent = splits[1];
            var newHtml = string.Join("", upToMainContent, mainContent, afterMainContent);
            if (newHtml != html)
                SaveHtml(newHtml, di);
        }

        private void ContinuousUpdateWebLog(object di)
        {
            while (ContinuousRefresh)
            {
                UpdateWebLog((IDependencyResolver)di);
                Thread.Sleep(Constants.WebLogRefreshDelay);
            }
        }

        private IEnumerable<string> GenerateItemsHtml(IEnumerable<Chat> chats, IDependencyResolver di)
        {
            var chans = di.GetRepository<Channel>().Get().ToDictionary(k => k.Id, v => v.Name);
            var users = di.GetRepository<User>().Get().ToDictionary(k => k.Id, v => v.Name);
            users[0] = "???";

            var dict = chats
                .Where(c => c.Id > 0)
                .ToDictionary(k => k.Id);

            return chats
                .Where(c => chans.ContainsKey(c.ChannelId) && users.ContainsKey(c.FromUserId))
                .Select(c =>
                {
                    var re = c.ResponseToId.HasValue && dict.ContainsKey(c.ResponseToId.Value) ?
                        $"<a href='#chat{c.ResponseToId}' title='{dict[c.ResponseToId.Value].Message.HtmlSafe()}'>re:</a>" :
                        "re:";

                    var msg = c.Message;
                    if (c.Id > 0)
                        msg = msg.HtmlSafe();

                    return $"<div id='chat{c.Id}' class='chat'><span class='chatHead'>[{chans[c.ChannelId]}] [{c.DateUtc:yy-MM-dd HH:mm}] &lt;<span class='chatUser'>{users[c.FromUserId]}</span>&gt; {re} - </span><span class='chatBody'>{msg}</span></div>"; }
                );
        }

        private static string LoadHtml()
        {
            using (FileStream fs = new FileStream(_templateFilename, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                string contents = reader.ReadToEnd();
                return contents;
            }
        }

        private static void SaveHtml(string html, IDependencyResolver di)
        {
            int delTries = 0;
            while (delTries++ < 100 && File.Exists(_outputFilename))
            {
                File.Delete(_outputFilename);
                Thread.Sleep(_random.Next(50, 1000));
            }

            if (File.Exists(_outputFilename))
            {
                di.Get<ILogger>().Log(null, "Cannot overwrite web chat log file", LoggingOptions.ToConsole);
                return;
            }

            using (FileStream fs = new FileStream(_outputFilename, FileMode.CreateNew, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(html);
            }
        }

        private static List<Chat> GetDbSet(IDependencyResolver di, IRepository<Chat> chatRepo)
        {
            var openChannels = di.GetRepository<Channel>()
                .Get(x => x.RequiresInvite, false)
                .Select(x => x.Id)
                .ToArray();

            var dbSet = chatRepo
                .Get()
                .Where(c => openChannels.Contains(c.ChannelId))
                .GroupBy(x => x.ChannelId)
                .ToDictionary(k => k.Key, v => v.OrderBy(x => x.DateUtc).ToList());

            var resultSet = new List<Chat>();

            void AddInvisCount(int _chanId, DateTime? _date, int _count)
            {
                if (_count > 0 && _date.HasValue)
                {
                    resultSet.Add(new Chat
                    {
                        ChannelId = _chanId,
                        DateUtc = _date.Value,
                        WebVisible = true,
                        Message = $"({_count} hidden message{(_count > 1 ? "s" : "")}, <a href='http://mutinybbs.com/community.html' target='_blank'>Log in to Community</a> to read!)"
                    });
                }
            }

            foreach (int chanId in dbSet.Keys)
            {
                var list = dbSet[chanId];
                int invisCount = 0;
                DateTime? invisDate = null;
                foreach (var chat in list)
                {
                    if (chat.WebVisible)
                    {
                        AddInvisCount(chanId, invisDate, invisCount);
                        invisCount = 0;
                        invisDate = null;
                        resultSet.Add(chat);
                    }
                    else
                    {
                        invisCount++;
                        if (!invisDate.HasValue)
                            invisDate = chat.DateUtc;
                    }
                }
                AddInvisCount(chanId, invisDate, invisCount);
            }

            resultSet = resultSet
                .OrderBy(x => x.DateUtc)
                .ToList();

            return resultSet;
        }

    }
}
