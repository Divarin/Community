using miniBBS.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;

namespace miniBBS.Services.Persistence
{
    public class StructuredQuery : IStructuredQuery
    {
        protected StringBuilder _builder = new StringBuilder();
        protected bool _firstUpdateSet = false; // used when multiple calls to the Set() method are used

        public StructuredQuery()
        { }

        public StructuredQuery Where()
        {
            _builder.Append("WHERE ");
            return this;
        }

        public StructuredQuery And()
        {
            _builder.Append("AND ");
            return this;
        }

        public StructuredQuery Or()
        {
            _builder.Append("OR ");
            return this;
        }

        public StructuredQuery Equal(string column, object value)
        {
            if (value == null)
                return Append($"{column} is null");
            else if (value is Unquoted)
                return Append($"{column} = {CleanValue(value)}");
            else
                return Append($"lower({column}) = '{CleanValue(value).ToLower()}'");
        }

        public StructuredQuery NotEqual(string column, object value)
        {
            if (value == null)
                return Append($"{column} is not null");
            else if (value is Unquoted)
                return Append($"{column} != {CleanValue(value)}");
            else
                return Append($"{column} != '{CleanValue(value)}'");
        }

        public StructuredQuery Equal(string column, Action<StructuredQuery> subselect)
        {
            StructuredQuery ss = new StructuredQuery();
            subselect?.Invoke(ss);

            return Append($"{column} = ({ss.Query})");
        }
        
        public StructuredQuery GreaterThan(string column, int value)
        {
            return Append($"{column} > {value}");
        }

        public StructuredQuery LessThan(string column, int value)
        {
            return Append($"{column} < {value}");
        }

        public StructuredQuery Union()
        { return Append("UNION "); }

        public StructuredQuery UnionAll()
        { return Append("UNION ALL "); }

        public StructuredQuery OrderBy(params string[] orderbys)
        { return Append($"ORDER BY {String.Join(", ", orderbys)} "); }

        public StructuredQuery Update<T>()
        {
            string table = GetTableFor<T>();
            _builder.AppendLine($"UPDATE {table} SET ");
            _firstUpdateSet = false;
            return this;
        }

        public StructuredQuery Set(string column, object value)
        {
            if (_firstUpdateSet)
                _builder.Append(", ");

            if (value == null)
                _builder.AppendLine($"{column} = null ");
            else if (value is Unquoted)
                _builder.AppendLine($"{column} = {CleanValue(value)} ");
            else
                _builder.AppendLine($"{column} = '{CleanValue(value)}' ");

            _firstUpdateSet = true;
            return this;
        }

        /// <summary>
        /// Used to select columns, can be mixed with literal values but values are not enclosed in single quotes so if 
        /// necessary add them yourself, such as "Select(Id, Name, 'Foobar')"
        /// </summary>
        public StructuredQuery Select(params string[] columns)
        { return Append($"SELECT {String.Join(", ", columns)} "); }

        public StructuredQuery Select(int top, params string[] columns)
        { return Append($"SELECT TOP {top} {String.Join(", ", columns)} "); }

        /// <summary>
        /// Used to select only literal values, each value is enclosed in single quotes 
        /// unless it's NULL or of type Unquoted
        /// </summary>
        public StructuredQuery SelectLiteral(params object[] literals)
        {
            _builder.Append("SELECT ");
            for (int i = 0; i < literals.Length; i++)
            {
                object literal = literals[i];

                if (i > 0)
                    _builder.Append(", ");

                if (literal is null)
                    _builder.Append("NULL ");
                else if (literal is Unquoted)
                    _builder.Append(literal.ToString());
                else
                    _builder.Append($"'{literal}' ");
            }
            _builder.AppendLine();

            return this;
        }

        public StructuredQuery DeleteFrom<T>()
        {
            string table = GetTableFor<T>();
            return Append($"DELETE FROM {table} ");
        }

        public StructuredQuery InsertInto<T>(params string[] columns)
        {
            string table = GetTableFor<T>();

            if (true == columns?.Any())
                return Append($"INSERT INTO {table} ({String.Join(", ", columns)})");
            else
                return Append($"INSERT INTO {table}");
        }

        public StructuredQuery Values(params object[] values)
        {
            _builder.AppendLine("VALUES (");
            for (int i = 0; i < values.Length; i++)
            {
                object value = values[i];
                if (value == null)
                    _builder.Append("NULL ");
                else
                {
                    if (value is Unquoted)
                        _builder.Append(CleanValue(value));
                    else
                        _builder.Append($"'{CleanValue(value)}' ");
                }

                if (i < values.Length - 1)
                    _builder.AppendLine(", ");
                else
                    _builder.AppendLine();
            }
            _builder.AppendLine(") ");
            return this;
        }

        public StructuredQuery From<T>(string alias = null)
        {
            string table = GetTableFor<T>();
            return Append($"FROM {table} {alias} ");
        }

        public StructuredQuery InnerJoin<T>(string alias, string on)
        {
            string table = GetTableFor<T>();
            return Append($"INNER JOIN {table} {alias} on {on} ");
        }

        public StructuredQuery LeftJoin<T>(string alias, string on)
        {
            string table = GetTableFor<T>();
            return Append($"LEFT JOIN {table} {alias} on {on} ");
        }

        public StructuredQuery ItemIn<T>(string item, IEnumerable<T> collection)
        {
            var array = collection.Select(x => CleanValue(x)).ToArray();
            return Append($"{item} IN ('{String.Join("', '", array)}') ");
        }

        public StructuredQuery ItemNotIn<T>(string item, IEnumerable<T> collection)
        {
            var array = collection.Select(x => CleanValue(x)).ToArray();
            return Append($"{item} NOT IN ('{String.Join("', '", array)}') ");
        }

        public string Query { get { return _builder.ToString(); } }

        protected StructuredQuery Append(string statement)
        {
            _builder.AppendLine(statement);
            return this;
        }

        public override string ToString()
        {
            return _builder.ToString();
        }

        //public enum DBTable
        //{
        //    InfoTypes,
        //    UserGrants,
        //    UserInfo,
        //    Users
        //}

        private string GetTableFor<T>()
        {
            Type type = typeof(T);
            var tableAttribute = type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() as TableAttribute;
            if (tableAttribute?.Name != null)
            {
                return tableAttribute.Name;
            }

            throw new Exception($"No table defined for type '{type.Name}'");
        }

        private string CleanValue(object dirty)
        {
            if (dirty == null)
                return null;

            string strDirty = dirty.ToString();
            if (!strDirty.Contains("'"))
                return strDirty;

            return String.Join("''", strDirty.Split(new char[] { '\'' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }

    /// <summary>
    /// Typically a value passed to a Structured Query is enclosed in single quotes.  This class allows you to 
    /// pass a value which will not be enclosed in quotes.
    /// </summary>
    public class Unquoted
    {
        public static Unquoted Value(object value)
        {
            return new Unquoted
            {
                _value = value
            };
        }

        /// <summary>
        /// 0 if False, 1 if True
        /// </summary>
        public static Unquoted Boolean(bool value)
        {
            return new Unquoted()
            {
                _value = value ? "1" : "0"
            };
        }

        /// <summary>
        /// GETUTCDATE()
        /// </summary>
        public static readonly Unquoted GetUtcDate = Value("GETUTCDATE()");

        /// <summary>
        /// 1
        /// </summary>
        public static readonly Unquoted BooleanTrue = Value("1");

        /// <summary>
        /// 0
        /// </summary>
        public static readonly Unquoted BooleanFalse = Value("0");

        /// <summary>
        /// NULL
        /// </summary>
        public static readonly Unquoted Null = Value("null");

        private object _value;

        public override string ToString()
        {
            return _value?.ToString() ?? "NULL";
        }

    }
}
