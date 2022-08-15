using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public static class Words
    {
        private static readonly HashSet<string> _usedWords = new HashSet<string>();
        private static Lazy<HashSet<string>> _wordsLazy = new Lazy<HashSet<string>>(() => LoadWords());

        private static HashSet<string> LoadWords()
        {
            HashSet<string> set = new HashSet<string>();

            using (FileStream fs = new FileStream("words.txt", FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine()?.Trim();

                    // skip lines with uppercase first letters as they are proper names
                    if (!string.IsNullOrWhiteSpace(line) && char.IsLower(line.First()))
                        set.Add(line.ToLower());
                }
            }

            return set;
        }
        
        public static bool IsWord(string word)
        {
            word = word?.Trim()?.ToLower();
            return !string.IsNullOrEmpty(word) && _wordsLazy.Value.Contains(word);
        }

        public static string GetWord(bool unique=false)
        {
            return GetWordInternal(unique, _wordsLazy.Value);
        }

        private static string GetWordInternal(bool unique, ICollection<string> collection)
        {
            if (true != collection?.Any())
                return string.Empty;

            int count = collection.Count;
            int index;

            do
            {
                index = (int)(Rnd.Execute() * count);
            } while (index > count);

            int tries = 0;
            string word;
            do
            {
                word = collection.ElementAt(index);
            } while (tries++ < 10000 && unique && !_usedWords.Add(word));

            return word;
        }

        public static string GetWord(int length, bool unique = false)
        {
            var collection = _wordsLazy.Value
                .Where(x => x.Length == length)
                .ToList();

            string word = GetWordInternal(unique, collection);

            return word;
        }

        public static string GetWord(int minLength, int maxLength, bool unique = false)
        {
            var collection = _wordsLazy.Value
                .Where(x => x.Length >= minLength && x.Length <= maxLength)
                .ToList();

            string word = GetWordInternal(unique, collection);

            return word;
        }

        public static string GetWord(string startsWith, bool unique = false)
        {
            var collection = _wordsLazy.Value
                .Where(x => x.StartsWith(startsWith))
                .ToArray();

            string word = GetWordInternal(unique, collection);

            return word;
        }

        public static string GetWordContains(string contains, bool unique=false)
        {
            var collection = _wordsLazy.Value
                .Where(x => x.Contains(contains))
                .ToArray();

            string word = GetWordInternal(unique, collection);

            return word;
        }

        public static void ResetNextWord()
        {
            _usedWords.Clear();
        }
    }
}
