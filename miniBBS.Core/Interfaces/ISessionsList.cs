using miniBBS.Core.Models.Control;
using System.Collections.Generic;

namespace miniBBS.Core.Interfaces
{
    public interface ISessionsList
    {
        IEnumerable<BbsSession> Sessions { get; }
        void AddSession(BbsSession session);
        void RemoveSession(BbsSession session);
    }
}
