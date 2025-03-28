using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;

namespace miniBBS.Core.Interfaces
{
    public interface IMenuFileLoader
    {
        bool TryShow(BbsSession session, MenuFileType menuType, params object[] templateValues);
        void ClearCache();
    }
}
