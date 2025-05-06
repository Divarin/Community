using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace miniBBS.Core.Interfaces
{
    public interface IRepository<T>
        where T : class, IDataModel
    {
        IEnumerable<T> Get();
        IEnumerable<T> Get<TProp>(Expression<Func<T, TProp>> propFunc, object value);
        T Get(int id);
        IEnumerable<T> Get(IDictionary<string, object> filter);
        IEnumerable<T> Get(int[] ids);
        IEnumerable<TProp> GetDistinct<TProp>(Expression<Func<T, TProp>> propFunc);
        int GetCount();
        int GetCount<TProp>(Expression<Func<T, TProp>> propFunc, object value);
        int GetCount(IDictionary<string, object> filter);
        int GetCountWhereProp1EqualsAndProp2IsGreaterThan<TProp1, TProp2>(Expression<Func<T, TProp2>> prop1Func, int prop1Value, Expression<Func<T, TProp2>> prop2Func, int prop2Value);

        /// <summary>
        /// [<typeparamref name="TProp"/>] = count of records with each property value.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="propFunc"></param>
        /// <returns></returns>
        IDictionary<TProp, int> GetAggregate<TProp>(Expression<Func<T, TProp>> propFunc);
        T Insert(T entity);
        T Update(T entity);
        T InsertOrUpdate(T entity);
        void Delete(T entity);
        void DeleteRange(IEnumerable<T> toBeDeleted);
        /// <summary>
        /// Gets the highest Id number of any record in the table
        /// </summary>
        int HighId { get; }

        
    }
}
