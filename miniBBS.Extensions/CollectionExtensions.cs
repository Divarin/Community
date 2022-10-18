using System;
using System.Collections.Generic;

namespace miniBBS.Extensions
{
    public static class CollectionExtensions
    {
        public static int? ItemNumber<T>(this SortedList<int, T> list, int? key)
        {
            if (!key.HasValue)
                return null;
            if (true == list?.ContainsKey(key.Value))
                return list.IndexOfKey(key.Value);
            return null;
        }

        public static int? ItemKey<T>(this SortedList<int, T> list, int index)
        {
            if (index >= 0 && index < list.Keys.Count)
                return list.Keys[index];
            return null;
        }

        public static int IndexOf<T>(this IEnumerable<T> enumerable, Func<T, bool> selector)
        {
            int i = 0;
            foreach (var t in enumerable)
            {
                if (selector(t))
                    return i;
                i++;
            }
            return -1;
        }

    }
}
