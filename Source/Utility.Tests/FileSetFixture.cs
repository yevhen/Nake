using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace Nake.Utility
{
    [TestFixture]
    public class FileSetFixture
    {
        FileSet files;

        [SetUp]
        public void SetUp()
        {
            Location.CurrentDirectory = BaseDirectory;

            files = new FileSet();
        }

        [Test]
        public void Should_resolve_all_include_patterns()
        {
            Include(@"**\*.txt");
            Include(@"**\*.lg");

            var expected = new List<string>
            {
                File(@"A\A1.txt"),
                File(@"A\A2.txt"),
                File(@"A\C\AC1.txt"),
                File(@"A\C\AC2.txt"),
                File(@"B\B1.txt"),
                File(@"B\B2.txt"),
                File(@"B\C\BC1.txt"),
                File(@"B\C\BC2.txt"),
                File(@"A\A3.lg"),
                File(@"A\C\AC3.lg"),
                File(@"B\B3.lg"),
                File(@"B\C\BC3.lg"),
            };

            Assert.That(Result(), Is.EquivalentTo(expected));
        }

        [Test]
        public void Should_respect_exclude_patterns()
        {
            Include(@"**\*.*");
            Exclude(@"*.txt");

            var expected = new List<string>
            {
                File(@"A\C\AC3.lg"),
                File(@"A\A3.lg"),
                File(@"B\C\BC3.lg"),
                File(@"B\B3.lg"),
            };

            Assert.That(Result(), Is.EquivalentTo(expected));
        }

        [Test]
        public void Should_return_distinct_paths()
        {
            Include(@"A\A3.lg");
            Include(@"A\**\A3.lg");

            var expected = new List<string>
            {
                File(@"A\A3.lg")
            };

            Assert.That(Result(), Is.EquivalentTo(expected));
        }

        [Test]
        public void Should_handle_multiple_double_star_specifications()
        {
            Include(@"**\A\**\*.lg");

            var expected = new List<string>
            {
                File(@"A\A3.lg"),
                File(@"A\C\AC3.lg")
            };

            Assert.That(Result(), Is.EquivalentTo(expected));
        }

        [Test] 
        public void Should_be_able_to_handle_absolute_and_relative_file_paths()
        {
            Include(@"A\A3.lg");
            Include(File(@"A\A3.lg"));

            var expected = new List<string>
            {
                File(@"A\A3.lg")
            };

            Assert.That(Result(), Is.EquivalentTo(expected));
        }

        [Test]
        public void Should_treat_everything_starting_from_doublestar_as_part_of_recursive_path()
        {
            files.Include(@"A\**\*.lg");
            files.Include(@"B\**\*.lg");

            var result = files.Resolve().ToArray();

            Assert.That(result[0].RecursivePath, Is.EqualTo(""));
            Assert.That(result[1].RecursivePath, Is.EqualTo("C"));
            Assert.That(result[2].RecursivePath, Is.EqualTo(""));
            Assert.That(result[3].RecursivePath, Is.EqualTo("C"));
        }

        void Include(string pattern)
        {
            files.Include(pattern);
        }

        void Exclude(string pattern)
        {
            files.Exclude(pattern);
        }

        List<string> Result()
        {
            return files.ToList();
        }

        static string File(string fileName)
        {
            return Path.Combine(BaseDirectory(), fileName);
        }

        static string BaseDirectory()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.Assert(assemblyLocation != null);

            return Path.Combine(assemblyLocation, @"Testing\FileList");
        }
    }
}