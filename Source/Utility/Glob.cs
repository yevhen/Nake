/*
 * Original file on IronRuby Project, licensed under Apache 2: https://github.com/IronLanguages/main/blob/master/Languages/Ruby/Ruby/Builtins/Glob.cs
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GlobDir
{
    static class Glob
    {
        [Flags]
        public enum Constants
        {
            IgnoreCase = 0x08,
            DotMatch = 0x04,
            NoEscape = 0x01,
            PathName = 0x02
        }

        private static void AppendExplicitRegexChar(StringBuilder builder, char c)
        {
            builder.Append('[');
            if (c == '^' || c == '\\')
            {
                builder.Append('\\');
            }
            builder.Append(c);
            builder.Append(']');
        }

        private static string PatternToRegex(string pattern, bool pathName, bool noEscape)
        {
            var result = new StringBuilder(pattern.Length);
            result.Append("\\G");

            var inEscape = false;
            CharClass charClass = null;

            foreach (char c in pattern)
            {
                if (inEscape)
                {
                    if (charClass != null)
                    {
                        charClass.Add(c);
                    }
                    else
                    {
                        AppendExplicitRegexChar(result, c);
                    }
                    inEscape = false;
                    continue;
                }
                if (c == '\\' && !noEscape)
                {
                    inEscape = true;
                    continue;
                }

                if (charClass != null)
                {
                    if (c == ']')
                    {
                        var set = charClass.MakeString();
                        if (set == null)
                        {
                            return String.Empty;
                        }
                        result.Append(set);
                        charClass = null;
                    }
                    else
                    {
                        charClass.Add(c);
                    }
                    continue;
                }
                switch (c)
                {
                    case '*':
                        result.Append(pathName ? "[^/]*" : ".*");
                        break;

                    case '?':
                        result.Append('.');
                        break;

                    case '[':
                        charClass = new CharClass();
                        break;

                    default:
                        AppendExplicitRegexChar(result, c);
                        break;
                }
            }

            return (charClass == null) ? result.ToString() : String.Empty;
        }

        private static bool FnMatch(string pattern, string path, Constants flags)
        {
            if (pattern.Length == 0)
            {
                return path.Length == 0;
            }

            var pathName = ((flags & Constants.PathName) != 0);
            var noEscape = ((flags & Constants.NoEscape) != 0);
            var regexPattern = PatternToRegex(pattern, pathName, noEscape);
            if (regexPattern.Length == 0)
            {
                return false;
            }

            if (((flags & Constants.DotMatch) == 0) && path.Length > 0 && path[0] == '.')
            {
                // Starting dot requires an explicit dot in the pattern
                if (regexPattern.Length < 4 || regexPattern[2] != '[' || regexPattern[3] != '.')
                {
                    return false;
                }
            }

            var options = RegexOptions.None;
            if ((flags & Constants.IgnoreCase) != 0)
            {
                options |= RegexOptions.IgnoreCase;
            }
            var match = Regex.Match(path, regexPattern, options);
            return match != null && match.Success && (match.Length == path.Length);
        }

        private static string[] UngroupGlobs(string pattern, bool noEscape)
        {
            var ungrouper = new GlobUngrouper();

            bool inEscape = false;
            foreach (char c in pattern)
            {
                if (inEscape)
                {
                    if (c != ',' && c != '{' && c != '}')
                    {
                        ungrouper.AddChar('\\');
                    }
                    ungrouper.AddChar(c);
                    inEscape = false;
                    continue;
                }
                if (c == '\\' && !noEscape)
                {
                    inEscape = true;
                    continue;
                }

                switch (c)
                {
                    case '{':
                        ungrouper.StartLevel();
                        break;

                    case ',':
                        if (ungrouper.Level < 1)
                        {
                            ungrouper.AddChar(c);
                        }
                        else
                        {
                            ungrouper.AddGroup();
                        }
                        break;

                    case '}':
                        if (ungrouper.Level < 1)
                        {
                            // Unbalanced closing bracket matches nothing
                            return new string[] {};
                        }
                        ungrouper.FinishLevel();
                        break;

                    default:
                        ungrouper.AddChar(c);
                        break;
                }
            }
            return ungrouper.Flatten();
        }

        private static IEnumerable<string> GetMatches(PlatformAdaptationLayer pal, string pattern, Constants flags)
        {
            if (pattern.Length == 0)
            {
                yield break;
            }
            var noEscape = ((flags & Constants.NoEscape) != 0);
            var groups = UngroupGlobs(pattern, noEscape);
            if (groups.Length == 0)
            {
                yield break;
            }

            foreach (var group in groups)
            {
                var matcher = new GlobMatcher(pal, group, flags);
                foreach (var filename in matcher.DoGlob())
                {
                    yield return filename.Replace("//", "/");
                }
            }
        }

        public static IEnumerable<string> GetMatches(string pattern, Constants flags = Constants.IgnoreCase | Constants.PathName | Constants.NoEscape)
        {
            return GetMatches(new PlatformAdaptationLayer(), pattern, flags);
        }

        private class CharClass
        {
            private readonly StringBuilder chars = new StringBuilder();

            internal void Add(char c)
            {
                if (c == ']' || c == '\\')
                {
                    chars.Append('\\');
                }
                chars.Append(c);
            }

            internal string MakeString()
            {
                if (chars.Length == 0)
                {
                    return null;
                }
                if (chars.Length == 1 && chars[0] == '^')
                {
                    chars.Insert(0, "\\");
                }
                chars.Insert(0, "[");
                chars.Append(']');
                return chars.ToString();
            }
        }

        private sealed class GlobMatcher
        {
            private readonly bool dirOnly;
            private readonly Constants flags;
            private readonly PlatformAdaptationLayer pal;
            private readonly string pattern;
            private readonly List<string> result;
            private bool stripTwo;

            internal GlobMatcher(PlatformAdaptationLayer pal, string pattern, Constants flags)
            {
                this.pal = pal;
                this.pattern = (pattern == "**") ? "*" : pattern;
                this.flags = flags | Constants.IgnoreCase;
                result = new List<string>();
                dirOnly = this.pattern.Substring(this.pattern.Length - 1, 1) == "/";
                stripTwo = false;
            }

            private bool NoEscapes
            {
                get { return ((flags & Constants.NoEscape) != 0); }
            }

            private int FindNextSeparator(int position, bool allowWildcard, out bool containsWildcard)
            {
                int lastSlash = -1;
                bool inEscape = false;
                containsWildcard = false;
                for (int i = position; i < pattern.Length; i++)
                {
                    if (inEscape)
                    {
                        inEscape = false;
                        continue;
                    }
                    char c = pattern[i];
                    if (c == '\\')
                    {
                        inEscape = true;
                        continue;
                    }
                    if (c == '*' || c == '?' || c == '[')
                    {
                        if (!allowWildcard)
                        {
                            return lastSlash + 1;
                        }
                        if (lastSlash >= 0)
                        {
                            return lastSlash;
                        }
                        containsWildcard = true;
                    }
                    else if (c == '/' || c == ':')
                    {
                        if (containsWildcard)
                        {
                            return i;
                        }
                        lastSlash = i;
                    }
                }
                return pattern.Length;
            }

            private void TestPath(string path, int patternEnd, bool isLastPathSegment)
            {
                if (!isLastPathSegment)
                {
                    DoGlob(path, patternEnd, false);
                    return;
                }

                if (!NoEscapes)
                {
                    path = Unescape(path, stripTwo ? 2 : 0);
                }
                else if (stripTwo)
                {
                    path = path.Substring(2);
                }

                if (pal.DirectoryExists(path))
                {
                    result.Add(path);
                }
                else if (!dirOnly && pal.FileExists(path))
                {
                    result.Add(path);
                }
            }

            private static string Unescape(string path, int start)
            {
                var unescaped = new StringBuilder();
                var inEscape = false;
                for (int i = start; i < path.Length; i++)
                {
                    char c = path[i];
                    if (inEscape)
                    {
                        inEscape = false;
                    }
                    else if (c == '\\')
                    {
                        inEscape = true;
                        continue;
                    }
                    unescaped.Append(c);
                }

                if (inEscape)
                {
                    unescaped.Append('\\');
                }

                return unescaped.ToString();
            }

            internal IEnumerable<string> DoGlob()
            {
                if (pattern.Length == 0)
                {
                    return new string[] {};
                }

                var pos = 0;
                var baseDirectory = ".";
                if (pattern[0] == '/' || pattern.IndexOf(':') >= 0)
                {
                    bool containsWildcard;
                    pos = FindNextSeparator(0, false, out containsWildcard);
                    if (pos == pattern.Length)
                    {
                        TestPath(pattern, pos, true);
                        return result;
                    }
                    if (pos > 0 || pattern[0] == '/')
                    {
                        baseDirectory = pattern.Substring(0, pos);
                    }
                }

                stripTwo = (baseDirectory == ".");

                DoGlob(baseDirectory, pos, false);
                return result;
            }

            private void DoGlob(string baseDirectory, int position, bool isPreviousDoubleStar)
            {
                if (!pal.DirectoryExists(baseDirectory))
                {
                    return;
                }

                bool containsWildcard;
                var patternEnd = FindNextSeparator(position, true, out containsWildcard);
                var isLastPathSegment = (patternEnd == pattern.Length);
                var dirSegment = pattern.Substring(position, patternEnd - position);

                if (!isLastPathSegment)
                {
                    patternEnd++;
                }

                if (!containsWildcard)
                {
                    var path = baseDirectory + "/" + dirSegment;
                    TestPath(path, patternEnd, isLastPathSegment);
                    return;
                }

                var doubleStar = dirSegment.Equals("**");
                if (doubleStar && !isPreviousDoubleStar)
                {
                    DoGlob(baseDirectory, patternEnd, true);
                }

                foreach (string file in pal.GetFileSystemEntries(baseDirectory, "*"))
                {
                    var objectName = Path.GetFileName(file);
                    if (FnMatch(dirSegment, objectName, flags))
                    {
                        var canon = file.Replace('\\', '/');
                        TestPath(canon, patternEnd, isLastPathSegment);
                        if (doubleStar)
                        {
                            DoGlob(canon, position, true);
                        }
                    }
                }
                if ((!isLastPathSegment || (flags & Constants.DotMatch) == 0) && dirSegment[0] != '.') return;
                if (FnMatch(dirSegment, ".", flags))
                {
                    var directory = baseDirectory + "/.";
                    if (dirOnly)
                    {
                        directory += '/';
                    }
                    TestPath(directory, patternEnd, true);
                }
                if (FnMatch(dirSegment, "..", flags))
                {
                    var directory = baseDirectory + "/..";
                    if (dirOnly)
                    {
                        directory += '/';
                    }
                    TestPath(directory, patternEnd, true);
                }
            }
        }

        private class GlobUngrouper
        {
            private readonly SequenceNode rootNode;
            private GlobNode currentNode;
            private int level;

            internal GlobUngrouper()
            {
                rootNode = new SequenceNode(null);
                currentNode = rootNode;
                level = 0;
            }

            internal int Level
            {
                get { return level; }
            }

            internal void AddChar(char c)
            {
                currentNode = currentNode.AddChar(c);
            }

            internal void StartLevel()
            {
                currentNode = currentNode.StartLevel();
                level++;
            }

            internal void AddGroup()
            {
                currentNode = currentNode.AddGroup();
            }

            internal void FinishLevel()
            {
                currentNode = currentNode.FinishLevel();
                level--;
            }

            internal string[] Flatten()
            {
                if (level != 0)
                {
                    return new string[] {};
                }
                var list = rootNode.Flatten();
                var result = new string[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    result[i] = list[i].ToString();
                }
                return result;
            }

            private class ChoiceNode : GlobNode
            {
                private readonly List<SequenceNode> nodes;

                internal ChoiceNode(GlobNode parentNode)
                    : base(parentNode)
                {
                    nodes = new List<SequenceNode>();
                }

                internal override GlobNode AddChar(char c)
                {
                    var node = new SequenceNode(this);
                    nodes.Add(node);
                    return node.AddChar(c);
                }

                internal override GlobNode StartLevel()
                {
                    var node = new SequenceNode(this);
                    nodes.Add(node);
                    return node.StartLevel();
                }

                internal override GlobNode AddGroup()
                {
                    AddChar('\0');
                    return this;
                }

                internal override GlobNode FinishLevel()
                {
                    AddChar('\0');
                    return parent;
                }

                internal override List<StringBuilder> Flatten()
                {
                    return nodes.SelectMany(node => node.Flatten()).ToList();
                }
            }

            private abstract class GlobNode
            {
                internal readonly GlobNode parent;

                protected GlobNode(GlobNode parentNode)
                {
                    parent = parentNode ?? this;
                }

                internal abstract GlobNode AddChar(char c);
                internal abstract GlobNode StartLevel();
                internal abstract GlobNode AddGroup();
                internal abstract GlobNode FinishLevel();
                internal abstract List<StringBuilder> Flatten();
            }

            private class SequenceNode : GlobNode
            {
                private readonly List<GlobNode> nodes;

                internal SequenceNode(GlobNode parentNode) : base(parentNode)
                {
                    nodes = new List<GlobNode>();
                }

                internal override GlobNode AddChar(char c)
                {
                    var node = new TextNode(this);
                    nodes.Add(node);
                    return node.AddChar(c);
                }

                internal override GlobNode StartLevel()
                {
                    var node = new ChoiceNode(this);
                    nodes.Add(node);
                    return node;
                }

                internal override GlobNode AddGroup()
                {
                    return parent;
                }

                internal override GlobNode FinishLevel()
                {
                    return parent.parent;
                }

                internal override List<StringBuilder> Flatten()
                {
                    var result = new List<StringBuilder> {new StringBuilder()};
                    foreach (var node in nodes)
                    {
                        var tmp = new List<StringBuilder>();
                        foreach (var builder in node.Flatten())
                        {
                            foreach (var sb in result)
                            {
                                var newsb = new StringBuilder(sb.ToString());
                                newsb.Append(builder);
                                tmp.Add(newsb);
                            }
                        }
                        result = tmp;
                    }
                    return result;
                }
            }

            private class TextNode : GlobNode
            {
                private readonly StringBuilder builder;

                internal TextNode(GlobNode parentNode) : base(parentNode)
                {
                    builder = new StringBuilder();
                }

                internal override GlobNode AddChar(char c)
                {
                    if (c != 0)
                    {
                        builder.Append(c);
                    }
                    return this;
                }

                internal override GlobNode StartLevel()
                {
                    return parent.StartLevel();
                }

                internal override GlobNode AddGroup()
                {
                    return parent.AddGroup();
                }

                internal override GlobNode FinishLevel()
                {
                    return parent.FinishLevel();
                }

                internal override List<StringBuilder> Flatten()
                {
                    var result = new List<StringBuilder>(1) {builder};
                    return result;
                }
            }
        }

        private class PlatformAdaptationLayer
        {
            public IEnumerable<string> GetFileSystemEntries(string path, string searchPattern)
            {
                return Directory.GetFileSystemEntries(path, searchPattern);
            }

            public bool DirectoryExists(string path)
            {
                return Directory.Exists(path);
            }

            public bool FileExists(string path)
            {
                return File.Exists(path);
            }
        }
    }
}