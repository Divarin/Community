namespace miniBBS.Core.Interfaces
{
    public interface IDependencyResolver
    {
        T Get<T>();

        IRepository<T> GetRepository<T>()
            where T : class, IDataModel;
    }
}
