namespace miniBBS.Core.Interfaces
{
    public interface IWebLogger
    {
        void UpdateWebLog(IDependencyResolver di);
        void StartContinuousRefresh(IDependencyResolver di);
        void StopContinuousRefresh();
        bool ContinuousRefresh { get; }
    }
}
