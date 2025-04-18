﻿using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Executors;
using miniBBS.Basic.Interfaces;
using miniBBS.Core;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using miniBBS.Services;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Basic.Models
{
    public class Variables : IDictionary<string, string>
    {
        public Variables(IDictionary<string, Func<string>> environmentVariables)
        {
            EnvironmentVariables = environmentVariables;
            Data = new Data();
            Functions = new List<Function>();
        }

        public string this[string key]
        {
            get
            {
                key = key
                    .Replace('\t', ',')
                    .Replace(" ", "");

                key = EvaluateArrayExpressions(null, key);

                if (_environmentVariableValues.ContainsKey(key))
                    return _environmentVariableValues[key];
                if (true == EnvironmentVariables?.ContainsKey(key))
                    return EnvironmentVariables[key]();
                if (_globals.ContainsKey(key))
                    return _globals[key];
                else
                    return null;
            }
            set
            {
                key = EvaluateArrayExpressions(null, key);
                if (true == EnvironmentVariables?.ContainsKey(key))
                    throw new RuntimeException($"'{key}' is an Environment Variable");

                if (value == null)
                {
                    if (_globals.ContainsKey(key))
                        _globals.Remove(key);
                    return;
                }

                if (value != null && key.Contains("$"))
                {
                    if (!value.StartsWith("\"") && !value.EndsWith("\""))
                        value = '"' + value + '"';
                    value = SubstituteInteriorQuotes(value);
                }
                else if(!double.TryParse(value, out double _))
                    throw new RuntimeException("type mismatch");

                _globals[key] = value;
            }
        }

        private string SubstituteInteriorQuotes(string value)
        {
            if (value.Length < 3)
                return value;

            char[] arr = new char[value.Length];
            arr[0] = value[0];
            arr[arr.Length - 1] = value[value.Length - 1];
            for (int i = 1; i < value.Length - 1; i++)
            {
                if (value[i] == Constants.Basic.Quote)
                    arr[i] = Constants.Basic.QuoteSubstitute;
                else
                    arr[i] = value[i];
            }

            value = new string(arr);
            return value;
        }

        private readonly IDictionary<string, string> _globals = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
        private IDictionary<string, Func<string>> EnvironmentVariables { get; set; }
        
        public Data Data { get; set; }
        public IList<Function> Functions { get; private set; }

        private readonly Stack<IScoped> _scopedStack = new Stack<IScoped>();
        private readonly IDictionary<string, string> _environmentVariableValues = new Dictionary<string, string>();

        public void SetEnvironmentVariable(string name, string value)
        {
            if (!EnvironmentVariables.ContainsKey(name))
                EnvironmentVariables[name] = () => _environmentVariableValues[name];
            _environmentVariableValues[name] = value;
        }

        public Dictionary<string, int> Labels { get; internal set; }

        public void Clear()
        {
            _globals.Clear();
        }

        public void PushScoped(IScoped scoped)
        {
            _scopedStack.Push(scoped);
        }

        public IScoped PopScoped()
        {
            if (_scopedStack.Count > 0)
                return _scopedStack.Pop();
            else
                return null;
        }

        public IScoped PeekScoped()
        {
            if (_scopedStack.Count > 0)
                return _scopedStack.Peek();
            else
                return null;
        }

        public IEnumerable<IScoped> PeekAllScoped()
        {
            if (_scopedStack.Count > 0)
            {
                for (int i = _scopedStack.Count - 1; i >= 0; i--)
                    yield return _scopedStack.ElementAt(i);
            }
        }

        public void ClearScoped()
        {
            _scopedStack.Clear();
        }

        public string GetState()
        {
            var json = JsonConvert.SerializeObject(_globals);
            var stateData = GlobalDependencyResolver.Default.Get<ICompressor>().Compress(json);
            return stateData;
        }

        public void SetState(string stateData, params string[] varsToLoad)
        {
            var json = GlobalDependencyResolver.Default.Get<ICompressor>().Decompress(stateData);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            foreach (var v in dict)
            {
                var loadThisState = true != varsToLoad?.Any() ||
                    varsToLoad.Any(x =>
                    {
                        if (x.Contains('('))
                            x = x.Substring(0, x.IndexOf('('));
                        var v1 = v.Key;
                        if (v1.Contains('('))
                            v1 = v1.Substring(0, v1.IndexOf('('));
                        return x.Equals(v1, StringComparison.CurrentCultureIgnoreCase);
                    });
                
                if (loadThisState)
                    _globals[v.Key] = v.Value;
            }
        }

        public string EvaluateArrayExpressions(BbsSession session, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return key;
            int pos = key.IndexOf('(');
            if (pos < 1)
                return key;
            int end = key.IndexOf(')', pos);
            if (end <= pos)
                return key;

            StringBuilder builder = new StringBuilder();
            StringBuilder expBuilder = new StringBuilder();
            bool str = false;
            for (int i=0; i < key.Length; i++)
            {
                char c = key[i];
                if (i <= pos || i > end)
                {
                    builder.Append(c);
                    continue;
                }

                if (!str && c != '\'')
                {
                    if (c == ',' || i == end)
                    {
                        string exp = expBuilder.ToString();
                        expBuilder.Clear();

                        string value = exp;
                        if (!string.IsNullOrWhiteSpace(value) && !int.TryParse(value, out int _i))
                            value = Evaluate.Execute(session, exp, this);
                        if (exp.EndsWith("$"))
                            value = '\'' + value + '\'';

                        builder.Append(value);
                        builder.Append(c);
                    }
                    else
                    {
                        expBuilder.Append(c);
                    }
                }
                
                if (c == '\'')
                    str = !str;

                if (str || c == '\'')
                    builder.Append(c);
            }

            return builder.ToString();
        }

        #region IDictionary wrappers
        public ICollection<string> Keys => _globals.Keys.Union(EnvironmentVariables.Keys).ToList();

        public ICollection<string> Values => _globals.Values.Union(EnvironmentVariables.Values.Select(v => v())).ToList();

        public int Count => _globals.Count;

        public bool IsReadOnly => _globals.IsReadOnly;

        public bool ContainsKey(string key)
        {
            key = EvaluateArrayExpressions(null, key);
            return _globals.ContainsKey(key) || EnvironmentVariables.ContainsKey(key);
        }

        public void Add(string key, string value)
        {
            _globals.Add(key, value);
        }

        public bool Remove(string key)
        {
            return _globals.Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _globals.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            _globals.Add(item);
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _globals.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _globals.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return _globals.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _globals.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _globals.GetEnumerator();
        }

        public bool IsDefined(string variableName, IEqualityComparer<string> comparer = null)
        {
            return
                _globals.Keys.Contains(variableName, comparer) ||
                _environmentVariableValues.Keys.Contains(variableName, comparer) ||
                _scopedStack.Any(x => x.LocalVariables.Keys.Contains(variableName, comparer));
        }
        #endregion
    }
}
