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
    /// <summary>
    /// Helper class to deal with file selections
    /// </summary>
    public class FileSet : IEnumerable<string>
    {
        static readonly string[] patternSeparator = {"|"};        

        readonly List<Inclusion> includes = new List<Inclusion>();
        readonly List<Exclusion> excludes = new List<Exclusion>();
        readonly HashSet<Item> absolutes = new HashSet<Item>();
        
        readonly string basePath;
        HashSet<Item> resolved;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSet"/> with the given base path.
        /// </summary>
        /// <param name="basePath">The base path to be used for relative paths, instead of CurrentDirectory.</param>
        public FileSet(string basePath = null)
        {
            this.basePath = basePath ?? Location.CurrentDirectory();
        }

        /// <summary>
        /// Adds specified file patterns.
        /// </summary>
        /// <param name="patterns">The patterns.</param>
        /// <returns>This instance</returns>
        public FileSet Add(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
                Add(pattern);

            return this;
        }

        /// <summary>
        /// Adds specified file pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns>This instance</returns>
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

        /// <summary>
        /// Adds the specified file inclusion pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns>This instance</returns>
        /// <exception cref="System.ArgumentException">
        /// Pattern cannot be null or contain whitespace only, contain forward slashes of exclusion pattern markers
        /// </exception>
        public FileSet Include(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be null or contain whitespace only", "pattern");

            foreach (var part in pattern.Split(patternSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("-:"))
                    throw new ArgumentException("Include does not accept patterns with exclusion markers : " + part, pattern);

                if (!ContainsWildcards(part))
                    Include(new Item(Location.GetFullPath(FilePath.From(part), FilePath.From(basePath))));

                includes.Add(new Inclusion(Location.GetRootedPath(FilePath.From(part), FilePath.From(basePath))));
            }

            return this;
        }

        void Include(Item item)
        {
            absolutes.Add(item);
        }

        /// <summary>
        /// Adds the specified file exclusion pattern.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns>This instance</returns>
        /// <exception cref="System.ArgumentException">
        /// Pattern cannot be null or contain whitespace only, contain forward slashes of inclusion pattern markers
        /// </exception>
        public FileSet Exclude(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be null or contain whitespace only", "pattern");

            foreach (var part in pattern.Split(patternSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("-:"))
                    throw new ArgumentException("Exclude does not accept patterns with exclusion markers : " + part, pattern);

                Add(Exclusion.By(FilePath.From(part)));
            }

            return this;
        }

        /// <summary>
        /// Registers the specified regex to be used as exclusion matcher.
        /// </summary>
        /// <param name="regex">The regex.</param>
        /// <returns>This insance</returns>
        /// <exception cref="System.ArgumentNullException">regex is null</exception>
        public FileSet Exclude(Regex regex)
        {
            if (regex == null)
                throw new ArgumentNullException("regex");

            return Add(Exclusion.By(regex));
        }

        /// <summary>
        /// Registers the specified predicate to be used as exclusion matcher.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns>This instance</returns>
        /// <exception cref="System.ArgumentNullException">predicate is null</exception>
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

        static bool ContainsWildcards(string pattern) => pattern.Contains("*") || pattern.Contains("?");

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<string> GetEnumerator()
        {
            return Resolve().Select(item => (string) item).GetEnumerator();
        }

        /// <summary>
        /// Resolves all file patterns in this file set to absolute paths.
        /// </summary>
        /// <returns>A sequence of file items with additional information</returns>
        public IEnumerable<Item> Resolve()
        {
            if (resolved != null)
                return resolved;

            resolved = new HashSet<Item>();

            foreach (var each in includes)
            {
                // replace backward slash to play nicely with Glob
                var globPattern = each.Pattern.Replace(@"\", "/");

                var matches = Glob
                    .GetMatches(globPattern)
                    .Where(file => !excludes.Any(exclusion => exclusion.Match(file)))
                    .Select(each.Create);

                foreach (var item in matches)
                    resolved.Add(item);
            }

            foreach (var item in absolutes)
                resolved.Add(item);

            return resolved;
        }

        /// <summary>
        /// Mirrors this file set onto detination path.
        /// </summary>
        /// <param name="destination">The destination path.</param>
        /// <returns>New set of paths</returns>
        public string[] Mirror(string destination)
        {
            destination = Location.GetFullPath(FilePath.From(destination));
            return Transform(item => Path.Combine(destination, item.RecursivePath, item.FileName));
        }

        /// <summary>
        /// Flattens this file set onto destination path.
        /// </summary>
        /// <param name="destination">The destination path.</param>
        /// <returns>New set of paths</returns>
        public string[] Flatten(string destination)
        {
            destination = Location.GetFullPath(FilePath.From(destination));
            return Transform(item => Path.Combine(destination, item.FileName));
        }

        /// <summary>
        /// Transforms this file set using the given transform function.
        /// </summary>
        /// <param name="transform">The transform function.</param>
        /// <returns>New set of paths</returns>
        /// <exception cref="System.ArgumentNullException">Transform function is <c>null</c></exception>
        public string[] Transform(Func<Item, string> transform)
        {
            if (transform == null)
                throw new ArgumentNullException("transform");

            return Transform(item => new Item(FilePath.From(transform(item))));
        }

        FileSet Transform(Func<Item, Item> transform)
        {
            var result = new FileSet();

            foreach (var item in Resolve())
                result.Include(transform(item));

            return result;
        }

        /// <summary>
        /// Performs conversion from file set to array of <see cref="ITaskItem"/>.
        /// </summary>
        /// <returns> The sequence of MSBuild task items </returns>
        public ITaskItem[] AsTaskItems()
        {
            return ((IEnumerable<string>)this).AsTaskItems();
        }

        /// <summary>
        /// Performs an implicit conversion from string array to <see cref="FileSet"/>.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator FileSet(string[] arg)
        {
            return new FileSet{arg};
        }

        /// <summary>
        /// Performs an implicit conversion from string to file set.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns> The new file set </returns>
        public static implicit operator FileSet(string arg)
        {
            return new FileSet{arg};
        }

        /// <summary>
        /// Performs an implicit conversion from file set to string array.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <returns> The array of the resolved file set paths </returns>
        public static implicit operator string[](FileSet arg)
        {
            return arg.ToArray();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return ToString(" ");
        }        
        
        /// <summary>
        /// Returns a string of resolved concatenated file paths using given separator.
        /// </summary>
        /// <param name="separator">Path separator</param>
        /// <returns>  The string with resolved file set paths separated by given  separator </returns>
        public string ToString(string separator)
        {
            return string.Join(separator, this);
        }

        class Inclusion
        {
            readonly string basePath = "";

            public Inclusion(string pattern)
            {
                var baseDirectory = Path.GetDirectoryName(FilePath.From(pattern));

                if (!string.IsNullOrEmpty(baseDirectory))
                {
                    var recursivePathIndex = baseDirectory.IndexOf($"{Path.DirectorySeparatorChar}**", StringComparison.Ordinal);
                    basePath = baseDirectory.Substring(0, recursivePathIndex != -1 ? recursivePathIndex : baseDirectory.Length);
                }

                Pattern = pattern;
            }

            public string Pattern { get; }

            public Item Create(string file) => new Item(FilePath.From(basePath), FilePath.From(file));
        }

        class Exclusion
        {
            public static Exclusion By(string pattern)
            {
                var regex = new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                return By(regex);
            }

            public static Exclusion By(Regex regex) => By(regex.IsMatch);
            public static Exclusion By(Func<string, bool> predicate) => new Exclusion(predicate);

            readonly Func<string, bool> predicate;

            Exclusion(Func<string, bool> predicate) => 
                this.predicate = predicate;

            public bool Match(string file) => predicate(file);
        }

        /// <summary>
        /// Represent fully resolved file set item
        /// </summary>
        public struct Item
        {
            /// <summary>
            /// The base path
            /// </summary>
            public readonly string BasePath;

            /// <summary>
            /// The full path
            /// </summary>
            public readonly string FullPath;

            /// <summary>
            /// The recursive path
            /// </summary>
            public readonly string RecursivePath;

            /// <summary>
            /// The full file name
            /// </summary>
            public readonly string FileName;

            /// <summary>
            /// The file name
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// The file extension
            /// </summary>
            public readonly string Extension;

            readonly string fullPathCaseInsensitive;

            internal Item(FilePath fullPath)
                :this(fullPath, fullPath)
            {}

            internal Item(FilePath basePath, FilePath fullPath)
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

                FileName    = Path.GetFileName(fullPath);
                Extension   = Path.GetExtension(fullPath);
                Name        = Path.GetFileNameWithoutExtension(fullPath);

                fullPathCaseInsensitive = fullPath.CaseInsensitive();
            }

            /// <summary>
            /// Performs an explicit conversion from <see cref="Item"/> to <see cref="System.String"/>.
            /// </summary>
            /// <param name="arg">The argument.</param>
            /// <returns>
            /// The full path of the item.
            /// </returns>
            public static explicit operator string(Item arg)
            {
                return arg.FullPath;
            }

            /// <summary>
            /// Checks whether this item is equal to other item, by performing case-insensitive comparison of full path.
            /// </summary>
            /// <param name="other">The other item.</param>
            /// <returns><c>true</c> if items are equal by full path</returns>
            public bool Equals(Item other)
            {
                return string.Equals(fullPathCaseInsensitive, other.fullPathCaseInsensitive);
            }

            /// <summary>
            /// Indicates whether this instance and a specified object are equal.
            /// </summary>
            /// <returns>
            /// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
            /// </returns>
            /// <param name="obj">Another object to compare to. </param><filterpriority>2</filterpriority>
            public override bool Equals(object obj)
            {
                return !ReferenceEquals(null, obj) && (obj is Item && Equals((Item) obj));
            }

            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            /// <returns>
            /// A 32-bit signed integer that is the hash code for this instance.
            /// </returns>
            /// <filterpriority>2</filterpriority>
            public override int GetHashCode()
            {
                return fullPathCaseInsensitive.GetHashCode();
            }

            /// <summary>
            /// Checks whether this item is equal to other item, by performing case-insensitive comparison of full path.
            /// </summary>
            /// <param name="left">The left.</param>
            /// <param name="right">The right.</param>
            /// <returns><c>true</c> if items are equal by full path</returns>
            public static bool operator ==(Item left, Item right)
            {
                return left.Equals(right);
            }

            /// <summary>
            /// Checks whether this item is not equal to other item, by performing case-insensitive comparison of full path.
            /// </summary>
            /// <param name="left">The left.</param>
            /// <param name="right">The right.</param>
            /// <returns><c>true</c> if items are not equal by full path</returns>
            public static bool operator !=(Item left, Item right)
            {
                return !left.Equals(right);
            }
        }
    }
}