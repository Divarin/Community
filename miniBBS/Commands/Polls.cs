using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using miniBBS.Services.GlobalCommands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Polls
    {
        public static void Execute(BbsSession session)
        {
            var originalLocation = session.CurrentLocation;
            var originalDnd = session.DoNotDisturb;

            session.CurrentLocation = Core.Enums.Module.Polls;
            session.DoNotDisturb = true;

            try
            {
                var questionRepo = DI.GetRepository<PollQuestion>();
                var voteRepo = DI.GetRepository<PollVote>();
                while (Menu(session, questionRepo, voteRepo))
                { }
            }
            finally
            {
                session.CurrentLocation = originalLocation;
                session.DoNotDisturb = originalDnd;
            }
        }

        private static bool Menu(BbsSession session, IRepository<PollQuestion> questionRepo, IRepository<PollVote> voteRepo)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Cyan))
            {
                var questions = questionRepo.Get()
                    .OrderByDescending(q => q.DateAddedUtc)
                    .ToList();

                var votes = voteRepo.Get()
                    .GroupBy(v => v.QuestionId)
                    .ToDictionary(k => k.Key, v => v.ToList());

                var builder = new StringBuilder();
                var star = UserIoExtensions.WrapInColor("*", ConsoleColor.Red);

                for (int i = 0; i < questions.Count; i++)
                {
                    var q = questions[i];
                    var question = q.Question;
                    var voted = votes.ContainsKey(q.Id) && votes[q.Id].Any(v => v.UserId == session.User.Id);
                    if (question.Length > session.Cols - 7)
                        question = question.Substring(0, session.Cols - 7);
                    builder.Append(UserIoExtensions.WrapInColor($"{i + 1,3} ", ConsoleColor.White));
                    builder.Append($"{(voted ? star : " ")} ");
                    builder.AppendLine(UserIoExtensions.WrapInColor($"{question}".MaxLength(session.Cols - 5), ConsoleColor.Blue));
                }

                builder.AppendLine("* = You voted on this.");
                builder.AppendLine("#   : Vote on Question #");
                builder.AppendLine("A   : Add Question");
                builder.AppendLine("D # : Delete Question #");
                builder.AppendLine("P # : Print Question # to Channel");
                builder.AppendLine("Q   : Quit");
                session.Io.Output(builder.ToString());
                session.Io.SetForeground(ConsoleColor.Yellow);
                session.Io.Output("[Polls] > ");
                var line = session.Io.InputLine();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("q", StringComparison.CurrentCultureIgnoreCase))
                    return false;
                else if (line.StartsWith("a", StringComparison.CurrentCultureIgnoreCase))
                    AddQuestion(session, questionRepo);
                else if (line.StartsWith("d", StringComparison.CurrentCultureIgnoreCase))
                {
                    // delete
                    int n = 0;
                    bool canDelete =
                        line.Length > 1 &&
                        int.TryParse(line.Substring(1), out n) &&
                        n >= 1 &&
                        n <= questions.Count &&
                        questions.Count > 0 &&
                        (questions[n - 1].UserId == session.User.Id ||
                         session.User.Access.HasFlag(AccessFlag.Administrator) ||
                         session.User.Access.HasFlag(AccessFlag.GlobalModerator));

                    if (canDelete)
                        DeleteQuestion(session, questionRepo, voteRepo, questions[n - 1]);
                    else
                        session.Io.Error("Invalid question or access denied to delete it.");
                }
                else if (line.StartsWith("p", StringComparison.CurrentCultureIgnoreCase))
                {
                    int n = 0;
                    bool canPost =
                        line.Length > 1 &&
                        int.TryParse(line.Substring(1), out n) &&
                        n >= 1 &&
                        n <= questions.Count &&
                        questions.Count > 0;
                    if (canPost)
                        PostQuestionToChannel(session, questions[n - 1]);
                    else
                        session.Io.Error("Invalid question number.");
                }
                else if (int.TryParse(line, out int n) && n >= 1 && n <= questions.Count && questions.Count > 0)
                {
                    var _q = questions[n - 1];
                    var _vs = votes.ContainsKey(_q.Id) ? votes[_q.Id] : new List<PollVote>();
                    VoteQuestion(session, voteRepo, _q, _vs);
                }

                return true;
            }
        }

        private static void PostQuestionToChannel(BbsSession session, PollQuestion question)
        {
            bool includeResults = 'Y' == session.Io.Ask("Include results?");
            string post = GetQuestionText(question, includeResults);
            
            if (!includeResults)
                post += $"{Environment.NewLine}Use '{UserIoExtensions.WrapInColor("/polls", ConsoleColor.Green)}' to vote on this and other topics.";

            AddToChatLog.Execute(session, DI.GetRepository<Chat>(), post, isNewTopic: true);
        }

        private static string GetQuestionText(PollQuestion question, bool includeResults)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[Poll Question]: {UserIoExtensions.WrapInColor(question.Question, ConsoleColor.White)}");

            if (includeResults)
            {
                var unanswered = DeserializeAnswers(question.Answers).ToList();
                var votes = DI.GetRepository<PollVote>()
                    .Get(v => v.QuestionId, question.Id)
                    .GroupBy(v => v.Answer)
                    .OrderByDescending(g => g.Count());
                double totalVotes = votes.SelectMany(x => x.ToList()).Count();
                foreach (var vote in votes)
                {
                    if (unanswered.Contains(vote.Key))
                        unanswered.Remove(vote.Key);
                    var percent = Math.Round(100.0 * (vote.Count() / totalVotes), 0);
                    builder.Append(percent.ToString().PadLeft(3, Constants.Spaceholder));
                    builder.Append("% ");
                    builder.Append(vote.Count().ToString().PadLeft(3, Constants.Spaceholder));
                    builder.AppendLine($"  {vote.Key.Color(ConsoleColor.Blue)}");
                }

                foreach (var a in unanswered)
                    builder.AppendLine($"{Constants.Spaceholder.Repeat(2)}0%   0  {a.Color(ConsoleColor.Blue)}");
            }

            string text = builder.ToString();
            return text;
        }

        private static void AddQuestion(BbsSession session, IRepository<PollQuestion> questionRepo)
        {
            if (!session.User.Access.HasFlag(AccessFlag.Administrator))
            {
                var userQuestions = questionRepo.Get(q => q.UserId, session.User.Id).ToList();
                if (userQuestions.Count > Constants.MaxVoteQuestionsPerUser)
                {
                    session.Io.Error($"Sorry you already have {Constants.MaxVoteQuestionsPerUser} questions!");
                    return;
                }
            }
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Green))
            {
                session.Io.Output("Enter Question: ");
                var q = session.Io.InputLine();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(q))
                    return;
                var answers = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                do
                {
                    session.Io.Output($"Enter Answer #{answers.Count + 1} (enter=done): ");
                    var a = session.Io.InputLine();
                    session.Io.OutputLine();
                    if (string.IsNullOrWhiteSpace(a))
                        break;
                    answers.Add(a);
                } while (true);
                
                if (true != answers?.Any())
                    return;

                if (answers.Count < 2)
                {
                    session.Io.Error("Must have at least 2 answers!");
                    return;
                }

                var now = DateTime.UtcNow;
                
                questionRepo.Insert(new PollQuestion
                {
                    DateAddedUtc = now,
                    Question = q,
                    UserId = session.User.Id,
                    Answers = SerializeAnswers(answers)
                });

                session.Messager.Publish(session, new GlobalMessage(session.Id, $"{session.User.Name} added new poll question: '{q}'{Environment.NewLine}Type /poll from chat prompt to cast your vote!"));
            }
        }

        private static void DeleteQuestion(BbsSession session, IRepository<PollQuestion> questionRepo, IRepository<PollVote> voteRepo, PollQuestion question)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.OutputLine(question.Question);
            }

            if ('Y' == session.Io.Ask("Are you sure you want to delete this question?"))
            {
                questionRepo.Delete(question);
                var votesToDelete = voteRepo.Get(v => v.QuestionId, question.Id);
                if (true == votesToDelete?.Any())
                    voteRepo.DeleteRange(votesToDelete);

                session.Messager.Publish(session, new GlobalMessage(session.Id, $"{session.User.Name} has deleted the poll question '{question.Question}'."));
            }
        }

        private static void VoteQuestion(BbsSession session, IRepository<PollVote> voteRepo, PollQuestion question, List<PollVote> votes)
        {
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Blue))
            {
                string asker = session.Usernames.ContainsKey(question.UserId) ? session.Usernames[question.UserId] : "Unknown";
                session.Io.OutputLine($"Asked by {asker} on {question.DateAddedUtc.AddHours(session.TimeZone):yy-MM-dd}");
                session.Io.SetForeground(ConsoleColor.Green);
                session.Io.OutputLine(question.Question);
                session.Io.SetForeground(ConsoleColor.Yellow);
                var answers = DeserializeAnswers(question.Answers).ToList();
                var builder = new StringBuilder();
                var star = UserIoExtensions.WrapInColor("*", ConsoleColor.Red);
                for (int i=0; i < answers.Count; i++)
                {
                    var usersAnswer = votes.Any(v => v.Answer == answers[i] && v.UserId == session.User.Id);
                    builder.Append(UserIoExtensions.WrapInColor($"{i + 1,3} ", ConsoleColor.White));
                    builder.Append($"{(usersAnswer ? star : " ")}");
                    builder.AppendLine(UserIoExtensions.WrapInColor(answers[i], ConsoleColor.Cyan));
                }
                builder.AppendLine("* = Your vote.");
                builder.AppendLine("# : Vote for #");
                builder.AppendLine("A : Add a new option");
                builder.AppendLine("V : View results");
                builder.AppendLine("Q : Quit");
                session.Io.Output(builder.ToString());
                session.Io.Output("[Poll Qustion] > ");
                var line = session.Io.InputLine();
                session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("q", StringComparison.CurrentCultureIgnoreCase))
                    return;
                else if (line.StartsWith("a", StringComparison.CurrentCultureIgnoreCase))
                {
                    // add new option
                    session.Io.Output("Enter new option: ");
                    var opn = session.Io.InputLine();
                    if (!string.IsNullOrWhiteSpace(opn) && true != answers.Any(a => a.Equals(opn, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        // insert new option
                        answers.Add(opn);
                        question.Answers = SerializeAnswers(answers);
                        DI.GetRepository<PollQuestion>().Update(question);
                        if ('Y' == session.Io.Ask("Announce that you added this option?"))
                            session.Messager.Publish(session, new GlobalMessage(session.Id, $"{session.User.Name} added new option to poll question '{question.Question}': '{opn}'"));
                        VoteQuestion(session, voteRepo, question, votes);
                    }
                }
                else if (line.StartsWith("v", StringComparison.CurrentCultureIgnoreCase))
                {
                    // view results
                    var results = GetQuestionText(question, true);
                    session.Io.OutputLine(results);
                    session.Io.Pause();
                }
                else if (int.TryParse(line, out int n) && n >= 1 && n <= answers.Count && answers.Count > 0)
                {
                    // vote

                    // remove any existing votes for this user/question
                    var votesToRemove = voteRepo.Get(new Dictionary<string, object>
                    {
                        {nameof(PollVote.QuestionId), question.Id},
                        {nameof(PollVote.UserId), session.User.Id}
                    });
                    if (true == votesToRemove?.Any())
                        voteRepo.DeleteRange(votesToRemove);
                    // add new vote
                    voteRepo.Insert(new PollVote
                    {
                        QuestionId = question.Id,
                        UserId = session.User.Id,
                        DateAddedUtc = DateTime.UtcNow,
                        Answer = answers[n - 1]
                    });
                    
                    session.Io.Error("Vote added!");

                    var k = session.Io.Ask($"Announce that you voted?{Environment.NewLine}(Y)es and show what I selected{Environment.NewLine}Yes but (D)on't show my vote{Environment.NewLine}(N)o, don't announce{Environment.NewLine}(Y, D, or N): ");
                    if (k == 'Y' || k == 'D')
                    {
                        var announcement = $"{session.User.Name} voted on the poll question '{question.Question}'";
                        if (k == 'Y')
                            announcement += $": '{answers[n - 1]}'";
                        session.Messager.Publish(session, new GlobalMessage(session.Id, announcement));
                    }

                    // view results
                    var results = GetQuestionText(question, true);
                    session.Io.OutputLine(results);
                    session.Io.Pause();
                }
            }
        }

        private static string SerializeAnswers(IEnumerable<string> answers)
        {
            if (true != answers?.Any())
                return string.Empty;

            var json = JsonConvert.SerializeObject(answers);
            var comp = DI.Get<ICompressor>();
            var result = comp.Compress(json);
            return result;
        }

        private static IEnumerable<string> DeserializeAnswers(string compressed)
        {
            if (string.IsNullOrWhiteSpace(compressed))
                return new string[] { };

            var comp = DI.Get<ICompressor>();
            var json = comp.Decompress(compressed);
            var result = JsonConvert.DeserializeObject<IEnumerable<string>>(json);
            return result;
        }
    }
}
