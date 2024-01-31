using miniBBS.Core.Interfaces;
using miniBBS.Services.Persistence;
using miniBBS.Services.Services;
using System;
using System.Collections.Generic;

namespace miniBBS.Services
{
    public class GlobalDependencyResolver : IDependencyResolver
    {
        private static IDependencyResolver _default;
        public static IDependencyResolver Default
        {
            get
            {
                if (_default == null)
                    _default = new GlobalDependencyResolver();
                return _default;
            }
        }

        public GlobalDependencyResolver() { }

        private static readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        private static readonly Dictionary<Type, Func<object>> _dictionary = new Dictionary<Type, Func<object>>()
        {
            {typeof(IDependencyResolver), () => Default},
            {typeof(ITextEditor), () => new LineEditor()},
            {typeof(ISqlUi), () => new SqlUi()},
            {typeof(IFileTransferProtocol), () => new Xmodem()},
            {typeof(ICompressor), () => GetOrSetSingleton(() => new Compressor())},
            {typeof(ISessionsList), () => GetOrSetSingleton(() => new SessionsList())},
            {typeof(IMessager), () => GetOrSetSingleton(() => new Messager())},
            {typeof(ILogger), () => GetOrSetSingleton(() => new Logger())},
            {typeof(IChatCache), () => GetOrSetSingleton(() => new ChatCache())},
            {typeof(INotificationHandler), () => GetOrSetSingleton(() => new NotificationHandler())},
        };

        public IRepository<T> GetRepository<T>()
            where T : class, IDataModel
        {
            return new SqliteRepository<T>();
        }

        public T Get<T>()
        {
            Type type = typeof(T);

            if (_singletons.ContainsKey(type))
                return (T)_singletons[type];
            else if (_dictionary.ContainsKey(type))
                return (T)_dictionary[type]();

            return default(T);
        }

        public static object GetOrSetSingleton<T>(Func<T> instantiator)
        {
            Type type = typeof(T);
            if (_singletons.ContainsKey(type))
                return (T)_singletons[type];
            else
            {
                T instance = instantiator();
                _singletons[type] = instance;
                return instance;
            }
        }
    }
}
