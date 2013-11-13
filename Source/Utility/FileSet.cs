using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using GlobDir;
using Microsoft.Build.Framework;

namespace Nake
{
    public class FileSet : IEnumerable<string>
    {
        static readonly string[] patternSeparator = {"|"};        

        readonly List<Inclusion> includes = new List<Inclusion>();
        readonly List<Exclusion> excludes = new List<Exclusion>();
        readonly HashSet<Item> absolutes = new HashSet<Item>();

        HashSet<Item> resolved;

        public FileSet(params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                Add(pattern);
            }
        }

        public FileSet Add(string pattern)
        {
            foreach (var part in pattern.Split(patternSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("-:"))
                {
                    Exclude(part.Remove(0, 2));
                    continue;
                }

                Include(part);
            }

            return this;
        }

        public FileSet Include(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be null or contain whitespace only", "pattern");

            if (ContainsForwardSlashes(pattern))
                throw new ArgumentException("Forward slashes are not allowed in include pattern: " + pattern);

            foreach (var part in pattern.Split(patternSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("-:"))
                    throw new ArgumentException("Include does not accept patterns with exclusion markers : " + part, pattern);

                if (!ContainsWildcards(part))
                    Include(new Item(Location.GetFullPath(part)));

                includes.Add(new Inclusion(Location.GetRootedPath(part)));
            }

            return this;
        }

        internal FileSet Include(Item item)
        {
            absolutes.Add(item);
            return this;
        }

        public FileSet Exclude(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be null or contain whitespace only", "pattern");

            if (ContainsForwardSlashes(pattern))
                throw new ArgumentException("Forward slashes are not allowed in exclude pattern: " + pattern);

            foreach (var part in pattern.Split(patternSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("-:"))
                    throw new ArgumentException("Exclude does not accept patterns with exclusion markers : " + part, pattern);

                Add(Exclusion.By(part));
            }

            return this;
        }

        public FileSet Exclude(Regex regex)
        {
            if (regex == null)
                throw new ArgumentNullException("regex");

            return Add(Exclusion.By(regex));
        }

        public FileSet Exclude(Func<string, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            return Add(Exclusion.By(predicate));
        }

        FileSet Add(Exclusion exclusion)
        {
            excludes.Add(exclusion);
            return this;
        }

        static bool ContainsForwardSlashes(string arg)
        {
            return arg.Contains("/");
        }

        static bool ContainsWildcards(string pattern)
        {
            return pattern.Contains("*") || pattern.Contains("?");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Resolve().Select(item => (string) item).GetEnumerator();
        }

        public IEnumerable<Item> Resolve()
        {
            if (resolved != null)
                return resolved;

            resolved = new HashSet<Item>();

            foreach (var each in includes)
            {
                var inclusion = each;

                var matches = Glob
                    .GetMatches(inclusion.Pattern)
                    .Where(file => !excludes.Any(exclusion => exclusion.Match(file)))
                    .Select(inclusion.Create);

                foreach (var item in matches)
                {
                    resolved.Add(item);
                }
            }

            foreach (var item in absolutes)
            {
                resolved.Add(item);
            }

            return resolved;
        }       
        
        public static implicit operator FileSet(string[] arg)
        {
            return new FileSet(arg);
        }

        public static implicit operator FileSet(string arg)
        {
            return new FileSet(arg);
        }

        public static implicit operator string[](FileSet arg)
        {
            return arg.ToArray();
        }

        public static implicit operator ITaskItem[](FileSet arg)
        {
            return arg.ToArray().AsTaskItems();
        }

        public static implicit operator string(FileSet arg)
        {
            return string.Join(" ", arg);
        }

        class Inclusion
        {
            readonly string pattern = "";
            readonly string basePath = "";

            public Inclusion(string pattern)
            {
                var baseDirectory = Path.GetDirectoryName(pattern);

                if (!string.IsNullOrEmpty(baseDirectory))
                {
                    var recursivePathIndex = baseDirectory.IndexOf(@"\**", StringComparison.Ordinal);
                    basePath = baseDirectory.Substring(0, recursivePathIndex != -1 ? recursivePathIndex : baseDirectory.Length);
                }

                this.pattern = pattern.Replace(@"\", "/");
            }

            public string Pattern
            {
                get { return pattern; }
            }

            public Item Create(string file)
            {
                return new Item(basePath, file.Replace("/", @"\"));
            }
        }

        class Exclusion
        {
            public static Exclusion By(string pattern)
            {
                var regex = new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                return By(regex);
            }

            public static Exclusion By(Regex regex)
            {            
                return By(regex.IsMatch);
            }

            public static Exclusion By(Func<string, bool> predicate)
            {
                return new Exclusion(predicate);
            }

            readonly Func<string, bool> predicate;

            Exclusion(Func<string, bool> predicate)
            {                
                this.predicate = predicate;
            }

            public bool Match(string file)
            {
                return predicate(file.Replace("/", @"\"));
            }
        }

        public struct Item
        {
            public readonly string BasePath;
            public readonly string FullPath;            
            public readonly string RecursivePath;
            public readonly string FullName;
            public readonly string Name;
            public readonly string Extension;

            readonly string fullPathCaseInsensitive;

            internal Item(string fullPath)
                :this(fullPath, fullPath)
            {}

            internal Item(string basePath, string fullPath)
            {
                if (fullPath.Length < basePath.Length)
                    throw new ArgumentOutOfRangeException("fullPath", "Full path is shorter than base path");
                
                if (!Path.IsPathRooted(basePath))
                    throw new ArgumentException("Base path should be an absolute path", "basePath");

                if (!Path.IsPathRooted(fullPath))
                    throw new ArgumentException("Full path should be an absolute path", "fullPath");

                BasePath = basePath;
                FullPath = fullPath;

                RecursivePath = BasePath != FullPath 
                    ? Path.GetDirectoryName(FullPath.Remove(0, basePath.Length + 1)) 
                    : "";

                FullName    = Path.GetFileName(fullPath);
                Extension   = Path.GetExtension(fullPath);
                Name        = Path.GetFileNameWithoutExtension(fullPath);

                fullPathCaseInsensitive = fullPath.ToLowerInvariant();
            }

            public static explicit operator string(Item arg)
            {
                return arg.FullPath;
            }

            public bool Equals(Item other)
            {
                return string.Equals(fullPathCaseInsensitive, other.fullPathCaseInsensitive);
            }

            public override bool Equals(object obj)
            {
                return !ReferenceEquals(null, obj) && (obj is Item && Equals((Item) obj));
            }

            public override int GetHashCode()
            {
                return fullPathCaseInsensitive.GetHashCode();
            }

            public static bool operator ==(Item left, Item right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Item left, Item right)
            {
                return !left.Equals(right);
            }
        }
    }
}