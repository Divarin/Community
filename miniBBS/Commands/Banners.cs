using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class Banners
    {
        private static List<string> _banners = null;
        private static readonly Random _random = new Random((int)DateTime.Now.Ticks);

        public static void Show(BbsSession session, string arg = null)
        {
            var loadBanners = _banners == null || _banners.Count < 1;
            loadBanners |= true == session?.User?.Access.HasFlag(AccessFlag.Administrator) && "reload".Equals(arg, StringComparison.CurrentCultureIgnoreCase);

            if (loadBanners)
            {
                _banners = LoadBanners().ToList();
            }

            var banners = _banners
                .Where(x => MaxLength(x) <= session.Cols)
                ?.ToList();

            if (banners?.Any() != true)
                return;

            string banner = null;
            var i = _random.Next(0, banners.Count);
            if (arg != null && int.TryParse(arg, out var b))
            {
                if (b >= 0 && b < _banners.Count)
                    banner = _banners[b];
                else
                    session.Io.Error("Banner number out of range.");
            }
            else
                banner = banners[i];

            if (string.IsNullOrWhiteSpace(banner))
                return;

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Magenta))
            {
                session.Io.OutputLine(banner, OutputHandlingFlag.NoWordWrap);
            }
        }

        private static IEnumerable<string> LoadBanners()
        {
            if (!File.Exists(Constants.BannerFile))
                yield break;

            var builder = new StringBuilder();
            using (var fileStream = new FileStream(Constants.BannerFile, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(fileStream))
            {
                var contents = reader.ReadToEnd();
                var lines = contents.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Length == 1 && line[0] == '!')
                    {
                        var banner = builder.ToString();
                        builder.Clear();
                        if (!string.IsNullOrWhiteSpace(banner))
                            yield return banner;
                    }
                    else
                        builder.AppendLine(line);
                }
                if (builder.Length > 0)
                {
                    var banner = builder.ToString();
                    if (!string.IsNullOrWhiteSpace(banner))
                        yield return banner;
                }
            }
        }

        private static int MaxLength(string str)
        {
            var lines = str.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Max(l => l.Length);
        }
    }
}
