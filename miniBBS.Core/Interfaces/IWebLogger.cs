namespace miniBBS.Core.Interfaces
{
    public interface IWebLogger
    {
        void UpdateWebLog(IDependencyResolver di);
        void StartContinuousRefresh(IDependencyResolver di);
        void StopContinuousRefresh();
        bool ContinuousRefresh { get; }

        /// <summary>
        /// Sets a flag that will cause the automatic compilation to compile even if there appears to be no changes.  Do this 
        /// whenever a message web flag was altered as the count of messages will not be changed.
        /// </summary>
        void SetForceCompile();
    }
}
