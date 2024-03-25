using System;
using System.Collections.Generic;

namespace miniBBS.Core.Extensions
{
    public static class EnumExtensions
    {
        public static string FriendlyName(this Enum enumValue)
        {
            string name = enumValue.ToString();
            var list = new List<char>();
            list.Add(name[0]);
            for (int i = 1; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                    list.Add(' ');
                list.Add(c);
            }
            return new string(list.ToArray());
        }
    }
}
