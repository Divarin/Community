﻿using miniBBS.Core.Interfaces;
using miniBBS.Helpers;
using miniBBS.Services;
using System;
using System.Collections.Generic;

namespace miniBBS
{
    public static class DI 
    {
        private static Dictionary<Type, object> _singletons = new Dictionary<Type, object>();

        private static Dictionary<Type, Func<object>> _dictionary = new Dictionary<Type, Func<object>>()
        {
            {typeof(IHasher), () => GetOrSetSingleton(() => new Hasher())},
            {typeof(ITextFilesBrowser), () =>  new TextFiles.TextFilesBrowser()},
            {typeof(IGopherServer), () => new TextFiles.GopherServer()},
        };

        public static IRepository<T> GetRepository<T>()
            where T : class, IDataModel
        {
            return GlobalDependencyResolver.Default.GetRepository<T>();
        }

        public static T Get<T>()
        {
            Type type = typeof(T);

            if (_dictionary.ContainsKey(type))
                return (T)_dictionary[type]();
            else
                return GlobalDependencyResolver.Default.Get<T>();
        }

        private static object GetOrSetSingleton<T>(Func<T> instantiator)
        {
            Type type = typeof(T);
            if (_singletons.ContainsKey(type))
                return (T)_singletons[type];
            return GlobalDependencyResolver.GetOrSetSingleton<T>(instantiator);
        }
    }
}
