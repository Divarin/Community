using miniBBS.Basic.Interfaces;
using System;
using System.Collections.Generic;

namespace miniBBS.Basic.Executors
{
    public abstract class Scoped : IScoped
    {
        public IDictionary<string, string> LocalVariables { get; protected set; }

        public Scoped()
        {
            LocalVariables = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
        }
    }
}
