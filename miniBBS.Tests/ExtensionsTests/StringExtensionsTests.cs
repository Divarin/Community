using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using miniBBS.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace miniBBS.Tests.ExtensionsTests
{
    public class StringExtensionsTests
    {
        private const string _longText = 
            "Okay so this is going to just be long run-on sentence.  There'll be a mixture of punctuation, spaces, and various possible " +
            "line-breaks.  In theory it should only break on spaces,    and tabs.  So I'll just keep typing stuff so that it is nice and " +
            "long and will work for multiple tests to see if the extension method is properly splitting and word-wrapping stuff and things.";

        private const string _shortText = "this is a pretty short text";

        [Theory]
        [InlineData(160)]
        [InlineData(80)]
        [InlineData(40)]
        [InlineData(22)]
        [InlineData(20)]
        [InlineData(10)]
        public void SplitAndWrap_LongText(int cols)
        {
            var session = new BbsSession(new SessionsList(), null)
            {
                Cols = cols
            };

            List<string> lines = new List<string>();

            foreach (var line in StringExtensions.SplitAndWrap(_longText, session))
            {
                lines.Add(line);
                Assert.False(line.Length > cols+2); // allow 2 over for Environment.NewLine
            }

            int longest = lines.Max(l => l.Length);
            Assert.True(longest <= cols+2);
        }

        [Theory]
        [InlineData(160)]
        [InlineData(80)]
        [InlineData(40)]
        [InlineData(22)]
        [InlineData(20)]
        [InlineData(10)]
        public void SplitAndWrap_ShortText(int cols)
        {
            var session = new BbsSession(new SessionsList(), null)
            {
                Cols = cols
            };

            List<string> lines = new List<string>();

            foreach (var line in StringExtensions.SplitAndWrap(_shortText, session))
            {
                lines.Add(line);
                Assert.False(line.Length > cols+2); // allow 2 over for Environment.NewLine
            }

            int longest = lines.Max(l => l.Length);
            Assert.True(longest <= cols+2);
        }
    }
}
