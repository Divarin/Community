//using System;
//using System.Collections.Generic;
//using System.Linq.Expressions;

//namespace miniBBS.Basic.Interfaces
//{
//    public interface IRepository<T>
//        where T : class, IDataModel
//    {
//        IEnumerable<T> Get();
//        IEnumerable<T> Get<TProp>(Expression<Func<T, TProp>> propFunc, object value);
//        T Get(int id);
//        IEnumerable<T> Get(IDictionary<string, object> filter);
//        IEnumerable<T> Get(int[] ids);
//        //IEnumerable<T> WithContext(Func<CapersContext, IEnumerable<T>> contextAction);
//        T Insert(T entity);
//        T Update(T entity);
//        T InsertOrUpdate(T entity);
//        void Delete(T entity);
//        /// <summary>
//        /// Gets the highest Id number of any record in the table
//        /// </summary>
//        int HighId { get; }
//    }
//}
