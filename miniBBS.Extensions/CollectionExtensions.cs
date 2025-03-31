using System;
using System.Collections.Generic;

namespace miniBBS.Extensions
{
    public static class CollectionExtensions
    {
        private static readonly Random _random = new Random((int)DateTime.Now.Ticks);

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
            {
                var key = list.Keys[index];
                return key;
            }

            return null;
        }

        public static int IndexOf<T>(this IEnumerable<T> enumerable, Func<T, bool> selector)
        {
            int i = 0;
            if (enumerable != null)
            {
                foreach (var t in enumerable)
                {
                    if (selector(t))
                        return i;
                    i++;
                }
            }
            return -1;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            if (true == dict?.ContainsKey(key))
                return dict[key];
            return defaultValue;
        }

        public static T[] Shuffle<T>(this T[] arr)
        {
            var orderedList = new List<T>(arr);
            var shuffledList = new List<T>(arr.Length);

            while (orderedList.Count > 0)
            {
                var n = _random.Next(0, orderedList.Count);
                T item = orderedList[n];
                orderedList.RemoveAt(n);
                shuffledList.Add(item);
            }

            return shuffledList.ToArray();
        }
    }
}
