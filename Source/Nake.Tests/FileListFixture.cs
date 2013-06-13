using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace Nake.Tests
{
	[TestFixture]
	public class FileListFixture
	{
		FileList files;

		[SetUp]
		public void SetUp()
		{
			Location.CurrentDirectory = BaseDirectory;

			files = new FileList();
		}

		[TearDown]
		public void TearDown()
		{
			Location.CurrentDirectory = () => Environment.CurrentDirectory;
		}
		
		[Test]
		public void Should_resolve_all_include_patterns_when_enumerated()
		{
			Include(@"**\*.txt");
			Include(@"**\*.lg");

			var expected = new List<string>
			{
				File(@"A\AF1.txt"),
				File(@"A\AF2.txt"),
				File(@"A\C\CF1.txt"),
				File(@"A\C\CF2.txt"),
				File(@"B\BF1.txt"),
				File(@"B\BF2.txt"),
				File(@"B\C\CF4.txt"),
				File(@"B\C\CF5.txt"),
				File(@"A\AF3.lg"),
				File(@"A\C\CF3.lg"),
				File(@"B\BF3.lg"),
				File(@"B\C\CF6.lg"),
			};

			Assert.That(Enumerated(), Is.EquivalentTo(expected));
		}

		[Test]
		public void Should_respect_exclude_patterns_when_enumerated()
		{
			Include(@"**\*.*");
			Exclude(@"*.txt");

			var expected = new List<string>
			{
				File(@"A\C\CF3.lg"),
				File(@"A\AF3.lg"),
				File(@"B\C\CF6.lg"),
				File(@"B\BF3.lg"),
			};

			Assert.That(Enumerated(), Is.EquivalentTo(expected));
		}

		[Test]
		public void Should_return_distinct_paths()
		{
			Include(@"**\AF1.txt");
			Include(@"A\AF1.txt");
			Include(@"A\**\AF1.txt");

			var expected = new List<string>
			{
				File(@"A\AF1.txt")
			};

			Assert.That(Enumerated(), Is.EquivalentTo(expected));
		}

		void Include(string pattern)
		{
			files.Include(pattern);
		}

		void Exclude(string pattern)
		{
			files.Exclude(pattern);
		}

		List<string> Enumerated()
		{
			var actual = files.ToList(); return actual;
		}

		static string File(string fileName)
		{
			return Path.Combine(BaseDirectory(), fileName);
		}

		static string BaseDirectory()
		{
			return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Testing\FileList");
		}
	}
}