using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Commands
{
    public static class OneTimeQuestions
    {
        public static void Execute(BbsSession session)
        {
            var repo = DI.GetRepository<Metadata>();
            var meta = repo.Get(new Dictionary<string, object>
            {
                {nameof(Metadata.UserId), session.User.Id},
                {nameof(Metadata.Type), MetadataType.OneTimeQuestionAnswered.ToString()}
            }).PruneAllButMostRecent(repo);

            Dictionary<OneTimeQuestion, string> answers =
                meta != null ?
                JsonConvert.DeserializeObject<Dictionary<OneTimeQuestion, string>>(meta.Data) :
                new Dictionary<OneTimeQuestion, string>();

            bool anyAnswered = false;

            foreach (OneTimeQuestion q in Enum.GetValues(typeof(OneTimeQuestion)))
            {
                if (!answers.ContainsKey(q) && _questions.ContainsKey(q))
                {
                    var k = Ask(session, q);
                    answers[q] = k.ToString();
                    anyAnswered = true;
                }
            }

            if (anyAnswered)
            {
                var data = JsonConvert.SerializeObject(answers);
                if (meta == null)
                {
                    meta = new Metadata
                    {
                        UserId = session.User.Id,
                        Type = MetadataType.OneTimeQuestionAnswered
                    };
                }
                meta.Data = data;
                repo.InsertOrUpdate(meta);
            }
        }

        private static char Ask(BbsSession session, OneTimeQuestion question)
        {
            var proc = _questions[question];
            session.Io.OutputLine("*** One-Time Question ***".Color(ConsoleColor.Blue));
            session.Io.OutputLine("Listen very carefully I shall say this only once!".Color(ConsoleColor.DarkGray));
            var k = session.Io.Ask(proc.TheQuestion);
            session.Io.OutputLine(proc.Followup);
            proc.Action?.Invoke(session, k);
            return k;
        }

        private static readonly Dictionary<OneTimeQuestion, QuestionProc> _questions = new Dictionary<OneTimeQuestion, QuestionProc>
        {
            {OneTimeQuestion.LoginStartupMode, new QuestionProc
            {
                TheQuestion =
                    "When you log in do you want to go to A) the Main Menu or B) the Chat Rooms.  " +
                    Environment.NewLine +
                    "What to do when you log in?".Color(ConsoleColor.Red),
                Followup = "If you want to change this preference use the command (from chat rooms) '/pref'",
                Action = (_s, _k) =>
                {
                    var metaRepo = DI.GetRepository<Metadata>();
                    var existing = metaRepo.Get(new Dictionary<string, object>
                    {
                        {nameof(Metadata.UserId), _s.User.Id},
                        {nameof(Metadata.Type), MetadataType.LoginStartupMode}
                    });
                    if (true == existing?.Any())
                        metaRepo.DeleteRange(existing);
                    var meta = new Metadata
                    {
                        Type = MetadataType.LoginStartupMode,
                        UserId = _s.User.Id,
                        DateAddedUtc = DateTime.UtcNow,
                        Data = _k == 'B' ? LoginStartupMode.ChatRooms.ToString() : LoginStartupMode.MainMenu.ToString()
                    };
                    metaRepo.Insert(meta);
                }
            } }
        };


        private class QuestionProc
        {
            public string TheQuestion { get; set; }
            public string Followup { get; set; }
            public Action<BbsSession, char> Action { get; set; }
        }
    }
}
