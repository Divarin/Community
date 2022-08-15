//using miniBBS.Basic.Interfaces;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.SQLite;
//using System.Linq;
//using System.Linq.Expressions;

//namespace miniBBS.Basic.Persistence
//{
//    public class SqliteRepository<T> : IRepository<T>
//            where T : class, IDataModel
//    {
//        private static readonly string _connectionString = $"Data Source={GlobalConstants.DatabaseFilename}";

//        public void Delete(T entity)
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();
//                Delete(entity, connection);
//                connection.Close();
//            }
//        }

//        private void Delete(T entity, SQLiteConnection connection)
//        {
//            var sq = new StructuredQuery()
//                .DeleteFrom<T>()
//                .Where().Equal(nameof(IDataModel.Id), entity.Id);

//            using (var command = new SQLiteCommand(sq.Query, connection))
//            {
//                command.ExecuteNonQuery();
//            }
//        }

//        public IEnumerable<T> Get(IDictionary<string, object> filter)
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();
//                IEnumerable<T> results = Get(filter, connection);
//                connection.Close();
//                return results;
//            }
//        }

//        private IEnumerable<T> Get(IDictionary<string, object> filter, SQLiteConnection connection)
//        {
//            var sq = new StructuredQuery()
//                .Select("* ").From<T>().Where();

//            for (int i = 0; i < filter.Keys.Count; i++)
//            {
//                string column = filter.Keys.ElementAt(i);
//                object value = filter[column];
//                if (i > 0) sq.And();
//                sq.Equal(column, value);
//            }

//            using (var adapter = new SQLiteDataAdapter(sq.Query, connection))
//            {
//                DataSet set = new DataSet();
//                adapter.Fill(set);
//                if (set.Tables.Count > 0)
//                {
//                    var results = from DataRow row in set.Tables[0].Rows
//                                  select Map(row);
//                    return results.ToArray();
//                }
//            }

//            return new T[] { };
//        }

//        public T Get(int id)
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();
//                T result = GetById(id, connection);
//                connection.Close();
//                return result;
//            }
//        }

//        private T GetById(int id, SQLiteConnection connection)
//        {
//            var results = GetByProperty(nameof(IDataModel.Id), id, connection);
//            return results.FirstOrDefault();
//        }

//        public IEnumerable<T> Get(int[] ids)
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();
//                IEnumerable<T> results = GetMultipleById(ids, connection);
//                connection.Close();
//                return results;
//            }
//        }

//        private IEnumerable<T> GetMultipleById(int[] ids, SQLiteConnection connection)
//        {
//            var sq = new StructuredQuery()
//                .Select("* ").From<T>().Where()
//                .ItemIn(nameof(IDataModel.Id), ids);

//            using (var adapter = new SQLiteDataAdapter(sq.Query, connection))
//            {
//                DataSet set = new DataSet();
//                adapter.Fill(set);
//                var results = from DataRow row in set.Tables[0].Rows
//                              select Map(row);
//                return results.ToArray();
//            }
//        }

//        public IEnumerable<T> Get()
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();
//                var results = GetAll(connection);
//                connection.Close();
//                return results;
//            }
//        }

//        private IEnumerable<T> GetAll(SQLiteConnection connection)
//        {
//            var sq = new StructuredQuery()
//                .Select("* ").From<T>();

//            using (var adapter = new SQLiteDataAdapter(sq.Query, connection))
//            {
//                DataSet set = new DataSet();
//                adapter.Fill(set);
//                var results = from DataRow row in set.Tables[0].Rows
//                              select Map(row);
//                return results.ToArray();
//            }
//        }

//        public IEnumerable<T> Get<TProp>(Expression<Func<T, TProp>> propFunc, object value)
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();
//                string propName = (propFunc.Body as MemberExpression).Member.Name;
//                var results = GetByProperty(propName, value, connection);
//                connection.Close();
//                return results;
//            }
//        }

//        private IEnumerable<T> GetByProperty(string propName, object value, SQLiteConnection connection)
//        {
//            var sql = new StructuredQuery()
//                .Select("* ")
//                .From<T>()
//                .Where().Equal(propName, value)
//                .Query;

//            using (var adapter = new SQLiteDataAdapter(sql, connection))
//            {
//                DataSet set = new DataSet();
//                adapter.Fill(set);
//                if (set != null && set.Tables != null && set.Tables.Count >= 1)
//                {
//                    DataTable table = set.Tables[0];
//                    if (table.Rows.Count > 0)
//                    {
//                        IEnumerable<T> results = from DataRow row in table.Rows
//                                                 select Map(row);
//                        return results.ToArray();
//                    }
//                }
//            }

//            return new T[] { };
//        }

//        public T Insert(T entity)
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();
//                T result = Insert(entity, connection);
//                connection.Close();
//                return result;
//            }
//        }

//        private T Insert(T entity, SQLiteConnection connection)
//        {
//            Type entityType = typeof(T);
//            var dict = entityType.GetProperties()
//                .Where(x => !nameof(IDataModel.Id).Equals(x.Name, StringComparison.CurrentCultureIgnoreCase))
//                .ToDictionary(k => k.Name, v => v.GetValue(entity, null));


//            var sq = new StructuredQuery();
//            sq.InsertInto<T>(dict.Keys.ToArray()).Values(dict.Values.ToArray());

//            using (var command = new SQLiteCommand(sq.Query, connection))
//            {
//                command.ExecuteNonQuery();
//            }

//            var select = new StructuredQuery()
//                .Select("* ")
//                .From<T>()
//                .Where();

//            for (int i = 0; i < dict.Keys.Count; i++)
//            {
//                string key = dict.Keys.ElementAt(i);
//                if (i > 0)
//                    select.And();
//                select.Equal(key, dict[key]);
//            }

//            using (var command = new SQLiteCommand(select.Query, connection))
//            using (var adapter = new SQLiteDataAdapter(select.Query, connection))
//            {
//                DataSet dataset = new DataSet();
//                adapter.Fill(dataset);
//                DataRow resultRow = dataset.Tables[0].Rows[0];
//                T result = Map(resultRow);

//                return result;
//            }
//        }

//        public T Update(T entity)
//        {
//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();
//                T result = Update(entity, connection);
//                connection.Close();
//                return result;
//            }
//        }

//        private T Update(T entity, SQLiteConnection connection)
//        {
//            var sq = new StructuredQuery().Update<T>();

//            Type entityType = typeof(T);
//            entityType.GetProperties()
//                .Where(x => !nameof(IDataModel.Id).Equals(x.Name, StringComparison.CurrentCultureIgnoreCase))
//                .ToList()
//                .ForEach(p => sq.Set(p.Name, p.GetValue(entity, null)));

//            sq.Where().Equal(nameof(IDataModel.Id), entity.Id);

//            using (var command = new SQLiteCommand(sq.Query, connection))
//            {
//                command.ExecuteNonQuery();
//            }

//            return entity;
//        }

//        private T Map(DataRow row)
//        {
//            DataTable table = row.Table;
//            Dictionary<string, object> dictionary = new Dictionary<string, object>();
//            foreach (DataColumn column in table.Columns)
//            {
//                object value = row[column];
//                dictionary[column.ColumnName] = value;
//            }
//            string json = JsonConvert.SerializeObject(dictionary);
//            T result = JsonConvert.DeserializeObject<T>(json);
//            return result;
//        }

//        public T InsertOrUpdate(T entity)
//        {
//            T result = null;

//            using (var connection = new SQLiteConnection(_connectionString))
//            {
//                connection.Open();
//                if (entity.Id != default(int))
//                {
//                    T existing = GetById(entity.Id, connection);
//                    if (existing != null)
//                    {
//                        result = Update(entity, connection);
//                    }
//                }

//                if (result == null)
//                {
//                    result = Insert(entity, connection);
//                }

//                connection.Close();
//            }

//            return result;
//        }

//        public int HighId
//        {
//            get
//            {
//                int result = -1;
//                using (var connection = new SQLiteConnection(_connectionString))
//                {
//                    connection.Open();
//                    var sq = new StructuredQuery()
//                       .Select("MAX(Id) ")
//                       .From<T>();

//                    using (var command = new SQLiteCommand(sq.Query, connection))
//                    {
//                        var objResult = command.ExecuteScalar();
//                        if (objResult is int)
//                            result = (int)objResult;
//                        else if (objResult != null && !(objResult is DBNull))
//                            result = int.Parse(objResult.ToString());
//                    }

//                    connection.Close();
//                }
//                return result;
//            }
//        }

//        //public IEnumerable<T> WithContext(Func<CapersContext, IEnumerable<T>> contextAction)
//        //{
//        //    using (var context = new CapersContext())
//        //    {
//        //        IEnumerable<T> results = contextAction(context);
//        //        return results;
//        //    }
//        //}

//    }
//}
