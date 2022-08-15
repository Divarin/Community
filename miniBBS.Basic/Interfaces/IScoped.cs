using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Basic.Interfaces
{
    public interface IScoped
    {
        IDictionary<string, string> LocalVariables { get; }
    }
}
