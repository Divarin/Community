using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Core.Models.Messages;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace miniBBS.Helpers
{
    public static class VoiceRequestQueueManager
    {
        private static readonly IDictionary<int, VoiceRequestQueue> _channelQueues = new Dictionary<int, VoiceRequestQueue>();
        private static readonly IList<WeakReference> _weakRefs = new List<WeakReference>();

        private static Thread _queueWatcher = null;

        public static VoiceRequestQueue CreateQueue(int channelId, TimeSpan duration)
        {
            RemoveQueue(channelId);

            var queue = new VoiceRequestQueue(channelId, duration);
            _weakRefs.Add(new WeakReference(queue));

            if (_queueWatcher == null)
            {
                _queueWatcher = new Thread(new ThreadStart(WatchQueues));
                _queueWatcher.Start(queue);
            }
            _channelQueues[channelId] = queue;
            
            return queue;
        }

        public static void RemoveQueue(int channelId)
        {
            if (_channelQueues.ContainsKey(channelId))
                RemoveQueue(_channelQueues[channelId]);
        }

        private static void RemoveQueue(VoiceRequestQueue q)
        { 
            q?.OnExpire?.Invoke();
            _channelQueues.Remove(q.ChannelId);

            var wrs = _weakRefs
                .Where(r => ReferenceEquals(q, r.Target))
                ?.ToList();

            if (true == wrs?.Any())
            {
                foreach (var wr in wrs)
                    _weakRefs.Remove(wr);
            }

            if (_weakRefs.Count < 1 && _queueWatcher != null)
            {
                _queueWatcher.Abort();
                _queueWatcher = null;
            }                
        }

        public static VoiceRequestQueue GetQueue(int channelId)
        {
            if (_channelQueues.ContainsKey(channelId))
                return _channelQueues[channelId];
            return null;
        }

        public static void RequestVoice(BbsSession session)
        {
            var q = GetQueue(session.Channel.Id);
            if (q != null)
            {
                q.Enqueue(session.User.Id);
                var msg = $"{session.User.Name} has been added to the voice request queue in position #{q.Count}";
                session.Io.OutputLine(msg);
                session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg, disturb: false, predicate: _s =>
                {
                    return
                        true == _s.User?.Access.HasFlag(AccessFlag.Administrator) ||
                        true == _s.User?.Access.HasFlag(AccessFlag.GlobalModerator) ||
                        true == _s.UcFlag?.Flags.HasFlag(UCFlag.Moderator);
                }));
            }
            else
            {
                session.Io.OutputLine("Voice request submitted.");
                var msg = $"{session.User.Name} has requested voice for this channel, use '/ch +v {session.User.Name}' to give it.";
                session.Messager.Publish(session, new ChannelMessage(session.Id, session.Channel.Id, msg, disturb: false, predicate: _s =>
                {
                    return
                        true == _s.User?.Access.HasFlag(AccessFlag.Administrator) ||
                        true == _s.User?.Access.HasFlag(AccessFlag.GlobalModerator) ||
                        true == _s.UcFlag?.Flags.HasFlag(UCFlag.Moderator);
                }));
                var moderators = session.Channel.GetModerators(session, true);
                var now = DateTime.UtcNow;
                foreach (var mod in moderators)
                {
                    DI.GetRepository<Notification>().Insert(new Notification
                    {
                        DateSentUtc = now,
                        Message = msg,
                        UserId = mod.Id
                    });
                }
            }
        }

        private static void WatchQueues()
        {
            while (_queueWatcher != null && true == _queueWatcher?.IsAlive)
            {
                var now = DateTime.UtcNow;

                for (int i=_weakRefs.Count-1; i >= 0; i--)
                {
                    if (_weakRefs.Count < 1) break;
                    var wr = _weakRefs[i];
                    if (!wr.IsAlive || (wr.Target as VoiceRequestQueue)== null)
                        _weakRefs.Remove(wr);
                    else
                    {
                        var q = wr.Target as VoiceRequestQueue;
                        if (now >= q.ExpiresAtUtc)
                        {
                            RemoveQueue(q);
                        }
                    }
                }

                if (_weakRefs.Count < 1)
                    break;

                Thread.Sleep(1000 * 60); // sleep for one minute
            }
        }

    }
}
