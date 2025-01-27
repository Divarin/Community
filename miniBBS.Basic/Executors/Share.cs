using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class Share
    {
        public static void Execute(BbsSession session, Variables variables, BasicStateInfo callingState, string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                session.Io.Error("Did not pass one or more varaible names to 'Share' command.");
                return;
            }

            var varsNames = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var query = from vn in varsNames
                        let v = variables.ContainsKey(vn) ? variables[vn] : null
                        where v != null
                        select new
                        {
                            Key = vn,
                            Value = v,
                        };

            var varsToShare = query.ToDictionary(k => k.Key, v => v.Value);
            
            var targetVars = MutantBasic.GetVariablesOfOtherSessionsRunningThisProgram(callingState);
            foreach (var v in targetVars)
            {
                foreach (var val in varsToShare)
                {
                    v[val.Key] = val.Value;
                }
            }
        }
    }
}
