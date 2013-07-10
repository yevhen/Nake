using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nake
{
    public static class StringExtensions
    {
        public static string TrimVerbatim(this string s)
        {
            var r = new Regex(@"^\s+", RegexOptions.Multiline);
            return r.Replace(s, string.Empty);
        }

        public static string JoinVerbatim(this string s)
        {
            return string.Join("", s.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(x => x.TrimVerbatim()));
        }
    }
}
