//using miniBBS.Core.Enums;
//using miniBBS.Core.Models.Control;
//using miniBBS.Core.Models.Data;
//using miniBBS.Extensions;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;

//namespace miniBBS.Commands
//{
//    public static class OneTimeQuestions
//    {
//        public static void Execute(BbsSession session)
//        {
//            var repo = DI.GetRepository<Metadata>();
//            var meta = repo.Get(new Dictionary<string, object>
//            {
//                {nameof(Metadata.UserId), session.User.Id},
//                {nameof(Metadata.Type), MetadataType.OneTimeQuestionAnswered.ToString()}
//            }).PruneAllButMostRecent(repo);

//            Dictionary<OneTimeQuestion, string> answers = 
//                meta != null ? 
//                JsonConvert.DeserializeObject<Dictionary<OneTimeQuestion, string>>(meta.Data) : 
//                new Dictionary<OneTimeQuestion, string>();

//            bool anyAnswered = false;

//            foreach (OneTimeQuestion q in Enum.GetValues(typeof(OneTimeQuestion)))
//            {
//                if (!answers.ContainsKey(q))
//                {
//                    var k = Ask(session, q);
//                    answers[q] = k.ToString();
//                    anyAnswered = true;
//                }
//            }

//            if (anyAnswered)
//            {
//                var data = JsonConvert.SerializeObject(answers);
//                if (meta == null)
//                {
//                    meta = new Metadata
//                    {
//                        UserId = session.User.Id,
//                        Type = MetadataType.OneTimeQuestionAnswered
//                    };
//                }
//                meta.Data = data;
//                repo.InsertOrUpdate(meta);                
//            }
//        }

//        private static char Ask(BbsSession session, OneTimeQuestion question)
//        {
//            var proc = _questions[question];
//            session.Io.OutputLine("*** One-Time Question ***".Color(ConsoleColor.Blue));
//            session.Io.OutputLine("Listen very carefully I shall say this only once!".Color(ConsoleColor.DarkGray));
//            var k = session.Io.Ask(proc.TheQuestion);
//            session.Io.OutputLine(proc.Followup);
//            proc.Action?.Invoke(session, k);
//            return k;
//        }

//        private static readonly Dictionary<OneTimeQuestion, QuestionProc> _questions = new Dictionary<OneTimeQuestion, QuestionProc>
//        {
//            {OneTimeQuestion.SetUserWebPref, new QuestionProc
//            {
//                TheQuestion = 
//                    "Do you want to help Community's user-base grow by allowing your future messages to be visible on the web site?  " + 
//                    "Keep in mind you can change this later if you want.  " + 
//                    Environment.NewLine +
//                    "Want your posts on our web site?".Color(ConsoleColor.Red),
//                Followup = "If you want to change this preference use the command /webpref",
//                Action = (_s, _k) => WebFlags.SetUserChatWebVisibility(_s, _k == 'Y')
//            } }
//        };

    
//        private class QuestionProc
//        {
//            public string TheQuestion { get; set; }
//            public string Followup { get; set; }
//            public Action<BbsSession, char> Action { get; set; }
//        }
//    }
//}
