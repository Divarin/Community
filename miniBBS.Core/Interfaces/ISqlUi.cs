using miniBBS.Core.Models.Control;

namespace miniBBS.Core.Interfaces
{
    /// <summary>
    /// Interface for a SQL User Interface
    /// </summary>
    public interface ISqlUi
    {
        void Execute(BbsSession session, string databaseFilename);
    }
}
