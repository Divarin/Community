using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.Services.Services
{
    public class SessionsList : ISessionsList
    {
        private List<WeakReference> _sessions = new List<WeakReference>();
        private object _lock = new object();

        public SessionsList()
        {

        }

        public IEnumerable<BbsSession> Sessions
        {
            get
            {
                List<BbsSession> result;
                lock (_lock)
                {
                    result = _sessions
                        .Select(wr => wr.Target as BbsSession)
                        .Where(x => x != null)
                        .ToList();
                }
                return result;
            }
        }

        public void AddSession(BbsSession session)
        {
            lock (_lock)
            {
                _sessions.Add(new WeakReference(session));
            }
        }

        public void RemoveSession(BbsSession session)
        {
            lock (_lock)
            {
                var wr = _sessions.FirstOrDefault(x => x.Target == session);
                if (wr != null)
                    _sessions.Remove(wr);
            }
        }
    }
}
